using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace WCKDRZR.Gaspar.GasparSystemJson
{
    public static class GasparJson
    {
        public static string SerializeIfExportsFor<TValue>(TValue value, int gasparType)
        {
            return JsonSerializer.Serialize(value, IfExportsForJsonSerializerOptions(gasparType));
        }

        public static JsonSerializerOptions IfExportsForJsonSerializerOptions(int gasparType)
        {
            return new()
                {
                    TypeInfoResolver = IfExportsForResolver(gasparType)
                };
        }

        public static IJsonTypeInfoResolver IfExportsForResolver(int gasparType)
        {
            return new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { IfExportsForModifier(gasparType) }
                };
        }

        public static Action<JsonTypeInfo> IfExportsForModifier(int gasparTypes)
        {
            return (JsonTypeInfo typeInfo) => {
                for (int i = 0; i < typeInfo.Properties.Count; i++)
                {
                    if (!typeInfo.Properties[i].ExportsFor(gasparTypes))
                    {
                        typeInfo.Properties.RemoveAt(i--);
                    }
                }
            };
        }
    }
}