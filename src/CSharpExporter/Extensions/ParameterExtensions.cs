using System.Collections.Generic;
using System.Linq;
using WCKDRZR.CSharpExporter.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.Extensions
{
    internal static class ParameterExtensions
    {
        public static string QueryString(this List<Parameter> parameters, string variablePrefix = "")
        {
            string qs = "";

            List<Parameter> queryStringParameters = parameters.Where(p => p.OnQueryString).ToList();
            if (queryStringParameters.Count > 0) { qs += "?"; }

            int i = 0;
            foreach (Parameter parameter in queryStringParameters)
            {
                string coalesce = "";
                if (parameter.Type.ToString().ToLower() == "string")
                {
                    coalesce = " ?? \"\"";
                }
                else if (parameter.IsNullable)
                {
                    coalesce = " ?? " + (parameter.Type.ToString().ToLower() == "bool?" ? "false" : "0");
                }

                string parameterIdentifier = parameter.Identifier;

                qs += parameterIdentifier + "=" + variablePrefix + "{" + parameterIdentifier + coalesce + "}" + (i < queryStringParameters.Count - 1 ? "&" : "");

                i++;
            }

            return qs;
        }
    }
}
