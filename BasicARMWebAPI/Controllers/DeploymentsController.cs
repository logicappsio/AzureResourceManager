using BasicARMWebAPI.Filters;
using BasicARMWebAPI.Utils;
using Microsoft.Azure.Management.Resources.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using TRex.Metadata;

namespace BasicARMWebAPI.Controllers
{
   [CloudExceptionFilter]
    public class DeploymentsController : ApiController
    {
        [Metadata("List deployments", "List all of the deployments in a resource group.", VisibilityType.Advanced)]
        [HttpGet]
        public async Task<IEnumerable<DeploymentExtended>> ListDeployments(
                        [Metadata("Resource group name", "The name of resource group to list the deployments for")]string resourceGroupName
            )
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var parameters = new Microsoft.Azure.Management.Resources.Models.DeploymentListParameters();
            var result = await client.Deployments.ListAsync(resourceGroupName, parameters, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return result.Deployments;
        }

        [Metadata("Get deployment", "Get a template deployment by its id.", VisibilityType.Advanced)]
        [HttpGet, Route("api/DeploymentById")]
        public async Task<Microsoft.Azure.Management.Resources.Models.DeploymentExtended> GetDeployment(
                        [Metadata("Id", "The Id of the deployment")]string id
            )
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var result = await client.Deployments.GetAsync(ResourceUtilities.GetResourceGroupFromId(id), ResourceUtilities.GetDeploymentFromId(id), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return result.Deployment;
        }

        [Metadata("Create deployment", "Create a new resource group deployment by providing a template.", VisibilityType.Advanced)]
        [HttpPut, Route("api/DeploymentById")]
        public async Task<Microsoft.Azure.Management.Resources.Models.DeploymentExtended> CreateDeployment(
                        [Metadata("Id", "The Id of the resource group to deploy to.")]string resourceGroupId,
                        [Metadata("Parameters", "The parameters for the template.")]string parameters,
                        [Metadata("Template", "The deployment template to deploy.")]string template,
                        [Metadata("Deployment name", "A custom name for the deployment.", VisibilityType.Advanced)]string deploymentName = null
            )
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var deployment = new Deployment()
            {
                Properties = new DeploymentProperties()
                {
                    Mode = DeploymentMode.Incremental,
                    Parameters = parameters,
                    Template = template
                }
            };

            if (deploymentName == null)
            {
                deploymentName = "AzureResourceConnector-" + Guid.NewGuid().ToString("n");
            }

            var result = await client.Deployments.CreateOrUpdateAsync(ResourceUtilities.GetResourceGroupFromId(resourceGroupId), deploymentName, deployment, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return result.Deployment;
        }
    }
}
