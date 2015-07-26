using Hyak.Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http.Filters;

namespace BasicARMWebAPI.Filters
{
    public class CloudExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is CloudException)
            {
                var except = (CloudException)context.Exception;

                var resp = new HttpResponseMessage()
                {
                    Content = new StringContent(except.Response.Content),
                    StatusCode = except.Response.StatusCode,
                    ReasonPhrase = except.Response.ReasonPhrase
                };

                Trace.WriteLine("Cloud Library Service Exception ---");
                Trace.WriteLine(except.Response.Content);

                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                context.Response = resp;
            }
            else if (context.Exception is AdalServiceException)
            {
                var except = (AdalServiceException)context.Exception;

                var resp = new HttpResponseMessage()
                {
                    Content = new StringContent(except.Message),
                    StatusCode = (HttpStatusCode)except.StatusCode
                };

                Trace.WriteLine("ADAL Service Exception ---");
                Trace.WriteLine(except.Message);

                context.Response = resp;
            }
            else if (context.Exception is InvalidOperationException)
            {
                var resp = new HttpResponseMessage()
                {
                    Content = new StringContent(context.Exception.Message),
                    StatusCode = HttpStatusCode.BadRequest
                };

                Trace.WriteLine("Invalid operation Exception ---");
                Trace.WriteLine(context.Exception.Message);

                context.Response = resp;
            }
        }
    }
}