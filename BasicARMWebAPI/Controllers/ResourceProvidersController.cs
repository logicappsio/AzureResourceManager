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
    public class ResourceProvidersController : ApiController
    {
        [Metadata("List resource providers", "List all of the available resource providers in the subscription.", VisibilityType.Advanced)]
        [HttpGet]
        public async Task<IEnumerable<Provider>> ResourceProviders()
        {
            var client = await ResourceUtilities.GetClient().ConfigureAwait(continueOnCapturedContext: false);

            var parameters = new Microsoft.Azure.Management.Resources.Models.ProviderListParameters();

            var result = await client.Providers.ListAsync(parameters, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);

            return result.Providers;
        }
    }

}
