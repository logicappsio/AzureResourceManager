using BasicARMWebAPI.Filters;
using BasicARMWebAPI.Utils;
using Microsoft.Azure;
using Microsoft.Azure.AppService.ApiApps.Service;
using Microsoft.Azure.Insights.Models;
using Microsoft.Azure.Management.Insights.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using TRex.Metadata;


namespace BasicARMWebAPI.Controllers
{
    [CloudExceptionFilter]
    public class MetricsController : ApiController
    {

        [Metadata("Get metrics", "Get a metric for a resource Id.", VisibilityType.Advanced)]
        [HttpGet, Route("api/MetricById")]
        public async Task<IEnumerable<MetricValue>> GetMetrics(
                        [Metadata("Resource Id", "Provide a resource Id that you want metrics from.")]string resource,
                        [Metadata("Metric name", "Which metric you want to trigger on.")] string metricName,
                        [Metadata("Duration (minutes)", "Optional. The number of minutes of metric data you want. Default is one hour.")]  double minutes = 60,
                        [Metadata("End time", "Optional. When you want the data to go up to. Default is utcnow().", VisibilityType.Advanced)]  string endTimestring = ""
            )
        {
            var endTimestamp = new DateTime();
            if (!DateTime.TryParseExact(
                            endTimestring,
                            "o",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal, out endTimestamp))
            {
                endTimestamp = DateTime.UtcNow;
            }

            return await GetMetricData(resource, metricName, endTimestamp.AddMinutes(-1 * minutes), endTimestamp).ConfigureAwait(continueOnCapturedContext: false);
        }

        [Trigger(TriggerType.Poll, typeof(Microsoft.Azure.Insights.Models.MetricValue))]
        [Metadata("Metric crosses threshold", "Trigger when a metric meets a certain threshold.")]
        [HttpGet, Route("api/MetricThreshold")]
        public async Task<HttpResponseMessage> MetricThreshold(string triggerState,
                                        [Metadata("Resource Id", "Provide a resource Id that you want metrics from.")] string resource,
                                        [Metadata("Metric name", "Which metric you want to trigger on.")] string metricName,
                                        [Metadata("Threshold", "The threshold for the metric.")]  double threshold,
                                        [Metadata("Duration (minutes)", "Optional. The number of minutes of metric data you want. Default is 15 minutes.", VisibilityType.Advanced)]  double minutes = 15,
                                        [Metadata("Time aggregation", "Optional. How data is combined over the time window. Default is average.", VisibilityType.Advanced)] TimeAggregationType timeAggregation = TimeAggregationType.Average,
                                        [Metadata("Operator", "Optional. The operation to use when comparing the metrics with the threshold. Default is greater than.", VisibilityType.Advanced)] ComparisonOperationType operation = ComparisonOperationType.GreaterThan
            )
        {
            var data = await GetMetricData(resource, metricName, DateTime.UtcNow.AddMinutes(-1 * minutes), DateTime.UtcNow).ConfigureAwait(continueOnCapturedContext: false);

            var outValue = new Microsoft.Azure.Insights.Models.MetricValue()
            {
                Average = data.Select(x => x.Average).Average(),
                Maximum = data.Select(x => x.Maximum).Max(),
                Minimum = data.Select(x => x.Minimum).Min(),
                Count = data.Select(x => x.Count).Sum(),
                Total = data.Select(x => x.Total).Sum(),
                Timestamp = data.Select(x => x.Timestamp).Max()
            };

            var outAggregation = 0.0;
            switch (timeAggregation)
            {
                case TimeAggregationType.Count:
                    outAggregation = Convert.ToDouble(outValue.Count);
                    break;
                case TimeAggregationType.Maximum:
                    outAggregation = Convert.ToDouble(outValue.Maximum);
                    break;
                case TimeAggregationType.Minimum:
                    outAggregation = Convert.ToDouble(outValue.Minimum);
                    break;
                case TimeAggregationType.Total:
                    outAggregation = Convert.ToDouble(outValue.Total);
                    break;
                default:
                    outAggregation = Convert.ToDouble(outValue.Average);
                    break;
            }

            var thresholdPassed = false;
            switch (operation)
            {
                case ComparisonOperationType.Equals:
                    thresholdPassed = (outAggregation == threshold);
                    break;
                case ComparisonOperationType.GreaterThanOrEqual:
                    thresholdPassed = (outAggregation >= threshold);
                    break;
                case ComparisonOperationType.LessThan:
                    thresholdPassed = (outAggregation < threshold);
                    break;
                case ComparisonOperationType.LessThanOrEqual:
                    thresholdPassed = (outAggregation <= threshold);
                    break;
                case ComparisonOperationType.NotEquals:
                    thresholdPassed = (outAggregation != threshold);
                    break;
                default:
                    thresholdPassed = (outAggregation > threshold);
                    break;
            }

            if (thresholdPassed && (triggerState.Equals("0") || triggerState.Equals("")))
            {
                // If there are other events to process tell the engine to get an new event every second
                return Request.EventTriggered(outValue,
                                                triggerState: "1",
                                                pollAgain: null);
            }

            // Let the Logic App know we don't have any data for it
            return Request.EventWaitPoll(retryDelay: null, triggerState: thresholdPassed ? "1" : "0");
        }

        private static async Task<IEnumerable<MetricValue>> GetMetricData(string resource, string metricName, DateTime startTimestamp, DateTime endTimestamp)
        {
            var timeWindow = endTimestamp - startTimestamp;

            if (timeWindow <= TimeSpan.FromMinutes(0) || timeWindow > TimeSpan.FromDays(31))
            {
                throw new InvalidOperationException("The number of minutes must be above 0 and less than one month.");
            }

            var authentication = await ResourceUtilities.GetAuthentication().ConfigureAwait(continueOnCapturedContext: false);
            var client = new Microsoft.Azure.Insights.InsightsClient(new TokenCloudCredentials(ConfigurationManager.AppSettings["AzureResourceConnector_SubscriptionId"], authentication.AccessToken));

            var metricDefinitions = await client.MetricDefinitionOperations.GetMetricDefinitionsAsync(resource, "name.value eq '" + metricName + "'", CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            var definitionResponse = metricDefinitions.MetricDefinitionCollection.Value;

            if (definitionResponse.FirstOrDefault() == null)
            {
                throw new InvalidOperationException("The metric you provided does not exist.");
            }

            var timeGrain = definitionResponse.FirstOrDefault().MetricAvailabilities.OrderBy(x => x.TimeGrain).FirstOrDefault();


            if (timeGrain == null || timeGrain.TimeGrain > timeWindow)
            {
                throw new InvalidOperationException("The metric does not have a time grain small enough for the window you provided. The smallest timegrain is: " + timeGrain.TimeGrain.Minutes);
            }

            var filter = "startTime eq " + startTimestamp.ToString("o") + " and endTime eq " + endTimestamp.ToString("o") + " and timeGrain eq duration'PT" + timeGrain.TimeGrain.Minutes + "M'";


            var metricResult = await client.MetricOperations.GetMetricsAsync(resource, filter, definitionResponse, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            if (metricResult.MetricCollection.Value.FirstOrDefault() == null)
            {
                throw new InvalidOperationException("There are not any data points for this metric in the time window you provided.");
            }

            return metricResult.MetricCollection.Value.FirstOrDefault().MetricValues;

        }
    }

}
