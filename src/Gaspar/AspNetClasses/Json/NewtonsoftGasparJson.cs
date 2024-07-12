using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WCKDRZR.Gaspar.GasparNewtonsoftJson
{
    public static class GasparJson
    {
        public static string SerializeIfExportsFor<TValue>(TValue value, int gasparType)
        {
            return JsonConvert.SerializeObject(value, IfExportsForJsonSerializerSettings(gasparType));
        }

        public static JsonSerializerSettings IfExportsForJsonSerializerSettings(int gasparType)
        {
            return new()
                {
                    ContractResolver = new IfExportsForResolver(gasparType)
                };
        }

        public class IfExportsForResolver : DefaultContractResolver
        {
            private readonly int _gasparType;

            public IfExportsForResolver(int gasparType)
            {
                _gasparType = gasparType;
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> properties = new List<JsonProperty>();

                foreach (var property in type.GetProperties())
                {
                    JsonProperty jsonProperty = base.CreateProperty(property, memberSerialization);

                    if (!property.ExportsFor(_gasparType))
                    {
                        jsonProperty.Ignored = true;
                    }

                    properties.Add(jsonProperty);
                }

                return properties;
            }
        }
    }
}