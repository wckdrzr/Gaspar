using System.Collections.Generic;
using System.Linq;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Extensions
{
    internal static class ParameterExtensions
    {
        public static string QueryString(this List<Parameter> parameters, OutputType outputType, string variablePrefix = "")
        {
            string qs = "";

            string coalesceMark = "";
            switch (outputType) {
                case OutputType.Angular:
                case OutputType.TypeScript:
                    coalesceMark = "||";
                    break;
                case OutputType.CSharp:
                    coalesceMark = "??";
                    break;
                case OutputType.Python:
                    coalesceMark = "or";
                    break;
            }

            List<Parameter> queryStringParameters = parameters.Where(p => p.Source == ParameterSource.Query).ToList();
            if (queryStringParameters.Count > 0) { qs += "?"; }

            int i = 0;
            foreach (Parameter parameter in queryStringParameters)
            {
                string coalesce = "";
                string? typeName = parameter.Type?.ToString().ToLower();
                
                if (!string.IsNullOrEmpty(coalesceMark))
                {
                    if (typeName == "string")
                    {
                        coalesce = $" {coalesceMark} \"\"";
                    }
                    else if (parameter.IsNullable && typeName != "datetime?") //datetime is odd on a query string; if "null" just allow
                    {
                        coalesce = $" {coalesceMark} " + (parameter.Type?.ToString().ToLower() == "bool?" ? "false" : "0");
                    }
                }

                string parameterIdentifier = parameter.Identifier;

                qs += parameterIdentifier + "=" + variablePrefix + "{" + parameterIdentifier + coalesce + "}" + (i < queryStringParameters.Count - 1 ? "&" : "");

                i++;
            }

            return qs;
        }

        public static string FunctionNameExtension(this List<Parameter> parameters)
        {
            string name = "With";

            int i = 0;
            foreach (Parameter parameter in parameters)
            {
                name += parameter.Identifier.CapitaliseFirst();
                name += i < parameters.Count - 1 ? "And" : "";
                i++;
            }

            return name;
        }
    }
}
