using BasicARMWebAPI.Filters;
using BasicARMWebAPI.Utils;
using Microsoft.Azure.Insights.Models;
using Microsoft.Azure.Management.Resources.Models;
using Microsoft.Azure.AppService.ApiApps.Service;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using TRex.Metadata;
using Microsoft.Azure;
using System.Configuration;

namespace BasicARMWebAPI.Controllers
{
    [CloudExceptionFilter]
    public class EventsController : ApiController
    {

        [Metadata("Get events", "Get a events in a subscription or for a resource.", VisibilityType.Advanced)]
        [HttpGet, Route("api/EventsById")]
        public async Task<IEnumerable<EventData>> GetEvents(
                        [Metadata("Resource", "Optional. Provide a resource group Id or resource Id that you want events from.")]string resource = null,
                        [Metadata("Status", "Optional. Provide a status to filter by.")] string status = null,
                        [Metadata("Duration (minutes)", "Optional. The number of minutes of events you want. Default is one hour.")]  double minutes = 60,
                        [Metadata("End time", "Optional. When you want the events to go up to. Default is utcnow().", VisibilityType.Advanced)]  string endTimestring = ""
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

            return await GetEventData(resource, status, endTimestamp.AddMinutes(-1 * minutes), endTimestamp).ConfigureAwait(continueOnCapturedContext: false);
        }

        [Trigger(TriggerType.Poll, typeof(Microsoft.Azure.Insights.Models.EventData))]
        [Metadata("Event occurs", "Trigger when an event occurs to a resource in your subscription.")]
        [HttpGet, Route("api/EventOccurs")]
        public async Task<HttpResponseMessage> EventOccurs(string triggerState,
                                        [Metadata("Resource", "Optional. Provide a resource group Id or resource Id to trigger on.")]
                                    string resource = null,
                                        [Metadata("Status", "Optional. Provide a specific status to trigger on.")]
                                    string status = null)
        {
            var endTimestamp = DateTime.UtcNow;

            var startTimestamp = endTimestamp;
            if (DateTime.TryParseExact(
                triggerState,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out startTimestamp))
            {

                var data = await GetEventData(resource, status, startTimestamp, endTimestamp).ConfigureAwait(continueOnCapturedContext: false);

                foreach (var returnedEvent in data)
                {
                    if (returnedEvent.EventTimestamp > startTimestamp)
                    {
                        // If there are other events to process tell the engine to get an new event every second
                        TimeSpan? pollAgain = null;

                        if (!returnedEvent.Equals(data.Last()))
                        {
                            pollAgain = TimeSpan.FromSeconds(1);
                        }

                        return Request.EventTriggered(returnedEvent,
                                                        triggerState: returnedEvent.EventTimestamp.ToString("o"),
                                                        pollAgain: pollAgain);
                    }
                }
            }
            // Let the Logic App know we don't have any data for it
            return Request.EventWaitPoll(retryDelay: null, triggerState: endTimestamp.ToString("o"));
        }

        private async static Task<IEnumerable<EventData>> GetEventData(string resource, string status, DateTime startTimestamp, DateTime endTimestamp)
        {
            var timeWindow = endTimestamp - startTimestamp;

            if (timeWindow <= TimeSpan.FromMinutes(0) || timeWindow > TimeSpan.FromDays(31))
            {
                throw new InvalidOperationException("The number of minutes must be above 0 and less than one month.");
            }

            var authentication = await ResourceUtilities.GetAuthentication().ConfigureAwait(continueOnCapturedContext: false);
            var client = new Microsoft.Azure.Insights.InsightsClient(new TokenCloudCredentials(ConfigurationManager.AppSettings["AzureResourceConnector_SubscriptionId"], authentication.AccessToken));

            var filter = "eventTimestamp ge '" + startTimestamp + "' and eventTimestamp le '" + endTimestamp + "' and eventChannels eq 'Admin, Operation'";

            if (resource != null)
            {
                var segments = resource.Split('/');
                if (segments.Count() > 7)
                {
                    filter += " and resourceUri eq '" + resource + "'";
                }
                else if (segments.Count() == 4)
                {
                    filter += " and resourceGroupName eq '" + ResourceUtilities.GetResourceGroupFromId(resource) + "'";
                }
            }

            if (status != null)
            {
                filter += " and status eq '" + status + "'";
            }

            var eventResult = await client.EventOperations.ListEventsAsync(filter, null, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return eventResult.EventDataCollection.Value.OrderBy(x => x.EventTimestamp);
        }

    }

}
