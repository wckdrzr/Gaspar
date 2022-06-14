using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WCKDRZR.Gaspar.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum AngularServiceErrorMessage
    {
        None,
        Generic,
        ServerResponse
    }
}