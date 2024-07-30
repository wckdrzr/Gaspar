using System.Linq;
using System.Security.Policy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.Gaspar.Models;

namespace WCKDRZR.Gaspar.Extensions
{
    internal static class ConfigurationExtensions
    {
        public static string AddUrlPrefix(this ConfigurationTypeOutput conf, string url)
        {
            string prefix = conf.UrlPrefix ?? "";
            if (!url.StartsWith("/")) { url = $"/{url}"; }
            if (prefix.EndsWith("/")) { prefix = prefix[..^1]; }

            return $"{prefix}{url}";
        }
    }
}
