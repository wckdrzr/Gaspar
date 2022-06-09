using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WCKDRZR.CSharpExporter.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AngularServiceErrorMessage
    {
        None,
        Generic,
        ServerResponse
    }
}