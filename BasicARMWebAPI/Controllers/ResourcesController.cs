using BasicARMWebAPI.Filters;
using BasicARMWebAPI.Models;
using BasicARMWebAPI.Utils;
using Microsoft.Azure.Management.Resources.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using TRex.Metadata;

namespace BasicARMWebAPI.Controllers
{
    [CloudExceptionFilter]
    public class ResourcesController : ApiController
    {
        [Metadata("List resources", "List resources in your subscirption by different types of filters.")]
        [HttpGet]
        public async Task<IEnumerable<JsonExtendedResource>> List(
            [Metadata("Resource group", "Optional. Only return resources that are inside this resource group.")]string resourceGroup = null,
            [Metadata("Resource type", "Optional. Only return resources of this type. You need to include the provider namespace as well, for example: Microsoft.Logic/workflows.")]string resourceType = null,
            [Metadata("Tag name", "Optional. Only return resources with this tag name.")]string tagName = null,
            [Metadata("Tag value", "Optional. Only return resources with this tag value. You must also specify a tag name to specify a value.")]string tagValue = null
            )
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var parameters = new Microsoft.Azure.Management.Resources.Models.ResourceListParameters();

            parameters.ResourceGroupName = resourceGroup;
            parameters.ResourceType = resourceType;
            parameters.TagName = tagName;
            parameters.TagValue = tagValue;

            var resourceListResult = new ResourceListResult();

            resourceListResult = await client.Resources.ListAsync(parameters, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return resourceListResult.Resources.Select(x => new JsonExtendedResource(x));
        }

        [Metadata("Get resource", "Get a single resource by its resource Id.")]
        [HttpGet, Route("api/ResourceById")]
        public async Task<JsonExtendedResource> Get(
            [Metadata("Resource Id", "The Id of the resource")]string id,
            [Metadata("API version", "The version of the API you would like to call to get the resource properties.", VisibilityType.Advanced)]string apiVersion = null)
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            apiVersion = await ResourceUtilities.GetLatestAPIVersion(client, id, apiVersion).ConfigureAwait(continueOnCapturedContext: false);

            var resource = await client.Resources.GetAsync(ResourceUtilities.GetResourceGroupFromId(id), ResourceUtilities.GetIdentityFromId(id, apiVersion), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
            return new JsonExtendedResource(resource.Resource);
        }

        [Metadata("Create or update resource", "Create a resource, or, update an existing resource. You must provide all of the properties for that resource.")]
        [HttpPut, Route("api/ResourceById")]
        public async Task<JsonExtendedResource> Put(
            [Metadata("Resource Id", "The Id of the resource")]string id,
            [Metadata("Properties", "Each resource has a different set of properties, you must provide the specific set that this resource requires")][FromBody]JsonBaseResource body,
            [Metadata("API version", "The version of the API you would like to call to get the resource properties.", VisibilityType.Advanced)]string apiVersion = null)
        {
            if (body == null)
            {
                throw new InvalidOperationException("You need to provide a vaild resource payload");
            }

            if (body.Location == null)
            {
                throw new InvalidOperationException("You need to provide a location for the resource");
            }

            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            apiVersion = await ResourceUtilities.GetLatestAPIVersion(client, id, apiVersion).ConfigureAwait(continueOnCapturedContext: false);

            var result = await client.Resources.CreateOrUpdateAsync(ResourceUtilities.GetResourceGroupFromId(id), ResourceUtilities.GetIdentityFromId(id, apiVersion), body.GetGenericResource(), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return new JsonExtendedResource(result.Resource);
        }

        [Metadata("Resource action", "Perform any other action on a resource. You need to know the action name and the payload that this action takes (if any).")]
        [HttpPost, Route("api/ResourceById")]
        public async Task<HttpResponseMessage> Action(
            [Metadata("Resource Id", "The Id of the resource")]string id,
            [Metadata("Action name", "The name of the action you want to call.")]string actionName,
            [Metadata("Body", "Each action may have parameters here. You must provide the parameters this action requires.")][FromBody]JObject body,
            [Metadata("API version", "The version of the API you would like to call to get the resource properties.", VisibilityType.Advanced)]string apiVersion = null)
        {
            using (var client = new HttpClient())
            {
                var authentication = await ResourceUtilities.GetAuthentication().ConfigureAwait(continueOnCapturedContext: false);

                client.BaseAddress = new Uri("https://management.azure.com");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication.AccessToken);

                if (body == null)
                {
                    body = new JObject();
                }

                if (apiVersion == null)
                {
                    var azureClient = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);
                    apiVersion = await ResourceUtilities.GetLatestAPIVersion(azureClient, id, apiVersion).ConfigureAwait(continueOnCapturedContext: false);
                }

                return await client.PostAsJsonAsync(id + "/" + actionName + "?api-version=" + apiVersion, body).ConfigureAwait(continueOnCapturedContext: false);
            }

        }

        [Metadata("Delete resource", "Delete a resource.", VisibilityType.Advanced)]
        [HttpDelete, Route("api/ResourceById")]
        public async Task<HttpResponseMessage> Delete(
            [Metadata("Resource Id", "The Id of the resource")]string id,
            [Metadata("API version", "The version of the API you would like to call to delete the resource.", VisibilityType.Advanced)]string apiVersion = null)
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            apiVersion = await ResourceUtilities.GetLatestAPIVersion(client, id, apiVersion).ConfigureAwait(continueOnCapturedContext: false);

            var result = await client.Resources.DeleteAsync(ResourceUtilities.GetResourceGroupFromId(id), ResourceUtilities.GetIdentityFromId(id, apiVersion), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            var response = new HttpResponseMessage();

            response.StatusCode = result.StatusCode;

            return response;
        }

    }

}
