using Microsoft.Azure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BasicARMWebAPI.Models
{
    public class JsonBaseResource : ResourceBase
    {
        public JObject Properties { get; set; }

        public Microsoft.Azure.Management.Resources.Models.GenericResource GetGenericResource()
        {
            var result = new Microsoft.Azure.Management.Resources.Models.GenericResource();

            result.Location = this.Location;
            result.Tags = this.Tags;
            if (this.Properties != null)
            {
                result.Properties = this.Properties.ToString();
            }
            else
            {
                result.Properties = "{}";
            }

            return result;
        }
    }
}