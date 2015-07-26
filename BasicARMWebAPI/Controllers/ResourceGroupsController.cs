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
    public class ResourceGroupsController : ApiController
    {
        [Metadata("List resource groups", "List all of the resource groups in the subscription.")]
        [HttpGet, Route("api/ResourceGroups")]
        public async Task<IEnumerable<Microsoft.Azure.Management.Resources.Models.ResourceGroup>> ListResourceGroups()
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var parameters = new Microsoft.Azure.Management.Resources.Models.ResourceGroupListParameters();
            var result = await client.ResourceGroups.ListAsync(parameters, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return result.ResourceGroups;
        }

        [Metadata("Get resource group", "Get a resource group by its id.", VisibilityType.Advanced)]
        [HttpGet, Route("api/ResourceGroupById")]
        public async Task<Microsoft.Azure.Management.Resources.Models.ResourceGroup> GetResourceGroup(
                        [Metadata("Id", "The Id of the resource group")]string id)
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var result = await client.ResourceGroups.GetAsync(ResourceUtilities.GetResourceGroupFromId(id), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return result.ResourceGroup;
        }

        [Metadata("Create resource group", "Create or update a resource group.", VisibilityType.Advanced)]
        [HttpPut, Route("api/ResourceGroupByName")]
        public async Task<Microsoft.Azure.Management.Resources.Models.ResourceGroup> CreateResourceGroup(
                        [Metadata("Name", "The name of the resource group")]string name,
                        [Metadata("Location", "The location of the resource group")]string location
            )
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var parameters = new ResourceGroup()
            {
                Location = location
            };
            var result = await client.ResourceGroups.CreateOrUpdateAsync(name, parameters, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return result.ResourceGroup;
        }

        [Metadata("Delete resource group", "Delete a resource group.", VisibilityType.Advanced)]
        [HttpDelete, Route("api/ResourceGroupById")]
        public async Task<HttpResponseMessage> DeleteResourceGroup(
                        [Metadata("Id", "The Id of the resource group")]string id
            )
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var result = await client.ResourceGroups.DeleteAsync(ResourceUtilities.GetResourceGroupFromId(id), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            var response = new HttpResponseMessage();

            response.StatusCode = result.StatusCode;

            return response;
        }

    }

}
