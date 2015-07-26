using Microsoft.Azure;
using Microsoft.Azure.Management.Resources;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;

namespace BasicARMWebAPI.Utils
{
    public class ResourceUtilities
    {
        public async static Task<ResourceManagementClient> GetClient()
        {
            var authentication = await GetAuthentication().ConfigureAwait(continueOnCapturedContext: false);

            return new ResourceManagementClient(new TokenCloudCredentials(ConfigurationManager.AppSettings["AzureResourceConnector_SubscriptionId"], authentication.AccessToken));
        }

        public async static Task<AuthenticationResult> GetAuthentication()
        {
            AuthenticationResult result = null;
            var context = new AuthenticationContext("https://login.windows.net/" + ConfigurationManager.AppSettings["AzureResourceConnector_TenantId"]);

            result = await context.AcquireTokenAsync(
                  "https://management.azure.com/",
                  new ClientCredential(ConfigurationManager.AppSettings["AzureResourceConnector_ClientId"], ConfigurationManager.AppSettings["AzureResourceConnector_ClientSecret"])
                  ).ConfigureAwait(continueOnCapturedContext: false);

            if (result == null)
            {
                throw new InvalidOperationException("Invaid attempt to obtain the JWT token");
            }

            return result;
        }

        public async static Task<string> GetLatestAPIVersion(ResourceManagementClient client, string resourceId, string apiVersion = null)
        {
            if (apiVersion == null)
            {
                var parameters = new Microsoft.Azure.Management.Resources.Models.ProviderListParameters();
                var listProvidersResponse = await client.Providers.ListAsync(parameters).ConfigureAwait(continueOnCapturedContext: false);

                var identity = GetIdentityFromId(resourceId, apiVersion);

                var provider = listProvidersResponse.Providers.Where(x => x.Namespace.Equals(identity.ResourceProviderNamespace)).FirstOrDefault();
                if (provider == null)
                {
                    throw new InvalidOperationException("This is not a valid resource provider namespace");
                }

                var type = provider.ResourceTypes.Where(x => x.Name.Equals(identity.ResourceType)).FirstOrDefault();
                if (type == null)
                {
                    throw new InvalidOperationException("This is not a valid resource type");
                }

                apiVersion = type.ApiVersions.OrderByDescending(x => x).FirstOrDefault();

            }
            return apiVersion;
        }

        public static ResourceIdentity GetIdentityFromId(string id, string apiVersion)
        {
            var segments = id.Split('/');

            if (segments.Count() < 9)
            {
                throw new InvalidOperationException("This is not a valid resource Id, there are missing segemnts");
            }

            var identity = new ResourceIdentity();

            identity.ResourceProviderApiVersion = apiVersion;
            identity.ResourceProviderNamespace = segments[6];
            identity.ResourceType = segments[7];
            identity.ResourceName = segments[8];

            return identity;
        }

        public static string GetDeploymentFromId(string id)
        {
            var segments = id.Split('/');

            if (segments.Count() < 6 || !(segments[5].Equals("deployments", StringComparison.InvariantCultureIgnoreCase)) || segments[6].Length == 0)
            {
                throw new InvalidOperationException("There is not a vaild deployment present in this Id");
            }

            return segments[6];
        }

        public static string GetResourceGroupFromId(string id)
        {
            var segments = id.Split('/');

            if (segments.Count() < 4 || !(segments[3].Equals("resourceGroups", StringComparison.InvariantCultureIgnoreCase)) || segments[4].Length == 0)
            {
                throw new InvalidOperationException("There is not a vaild resource group present in this Id");
            }

            return segments[4];
        }
    }
}