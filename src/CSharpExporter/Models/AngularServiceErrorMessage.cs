using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WCKDRZR.CSharpExporter.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum AngularServiceErrorMessage
    {
        None,
        Generic,
        ServerResponse
    }
}