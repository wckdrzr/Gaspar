using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WCKDRZR.Gaspar.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum TypeScriptServiceErrorMessage
    {
        None,
        Generic,
        ServerResponse
    }
}