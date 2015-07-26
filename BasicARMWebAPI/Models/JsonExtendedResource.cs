using Microsoft.Azure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BasicARMWebAPI.Models
{
    public class JsonExtendedResource : ResourceBaseExtended
    {
        public JsonExtendedResource(Microsoft.Azure.Management.Resources.Models.GenericResourceExtended resource)
        {

            Id = resource.Id;
            Location = resource.Location;
            Name = resource.Name;
            Tags = resource.Tags;
            Type = resource.Type;

            if (resource.Properties != null && resource.Properties.Length != 0)
            {
                try
                {
                    Properties = JObject.Parse(resource.Properties);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("The object is not valid JSON: " + e.Message);
                }
            }

        }

        public JObject Properties { get; set; }

    }
}