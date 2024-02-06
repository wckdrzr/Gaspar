﻿using System;
using System.Collections.Generic;
using System.Linq;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Converters
{
    internal class CSharpConverter : IConverter
	{
        public Configuration Config { get; set; }
        private int currentIndent = 0;

        public CSharpConverter(Configuration config)
        {
            Config = config;
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"{new String(' ', currentIndent * 4)}//{comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            return new();
        }

        public List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes)
        {
            List<string> lines = new();
            lines.Add($"using System;");
            lines.Add($"using System.Net.Http;");
            lines.Add($"using System.Collections.Generic;");
            lines.Add($"using System.Threading.Tasks;");
            lines.Add($"using WCKDRZR.Gaspar.Models;");
            foreach (string ns in outputConfig.ModelNamespaces)
            {
                lines.Add($"using {ns};");
            }
            lines.Add("");
            lines.Add($"namespace WCKDRZR.Gaspar.ServiceCommunciation.{Config.Controllers?.ServiceName.CapitaliseFirst()}Service");
            lines.Add($"{{");
            currentIndent++;
            return lines;
        }

        public List<string> ControllerFooter()
        {
            currentIndent--;
            return new() { "}" };
        }

        public List<string> ConvertController(List<ControllerAction> actions, string outputClassName, ConfigurationTypeOutput outputConfig, bool lastController)
        {
            List<string> lines = new();

            lines.Add($"    public static class {outputClassName}Service");
            lines.Add($"    {{");

            foreach (ControllerAction action in actions)
            {
                List<string> parameters = new();
                foreach (Parameter parameter in action.Parameters)
                {
                    string newParam = $"{parameter.Type} {parameter.Identifier}";
                    if (parameter.DefaultValue != null)
                    {
                        newParam += $" = {parameter.DefaultValue}";
                    }
                    parameters.Add(newParam);
                }

                if (action.BadMethodReason != null)
                {
                    lines.Add($"        [System.Obsolete(\"{action.BadMethodReason}\", true)]");
                    lines.Add($"        public static void {action.ActionName}({string.Join(", ", parameters)})\n        {{\n        }}");
                    lines.Add($"        [System.Obsolete(\"{action.BadMethodReason}\", true)]");
                    lines.Add($"        public static void {action.ActionName}Async({string.Join(", ", parameters)})\n        {{\n        }}");
                }
                else
                {
                    string httpMethod = action.HttpMethod.ToProper();

                    string url = $"{outputConfig.UrlPrefix}/{action.Route}";
                    url += action.Parameters.QueryString(OutputType.CSharp);

                    string urlHandler = "";
                    if (!string.IsNullOrEmpty(outputConfig.UrlHandlerFunction)) { urlHandler = $".{outputConfig.UrlHandlerFunction}()"; }

                    string loggingReceiver = (outputConfig.LoggingReceiver == null) ? "null" : $"typeof({outputConfig.LoggingReceiver})";
                    string customSerializer = (action.CustomSerializer == null) ? "null" : $"typeof({action.CustomSerializer})";

                    string returnTypeString = "";
                    string fetchMethodName = "FetchVoidAsync";
                    if (action.ReturnTypeOverride != null)
                    {
                        returnTypeString = $"<{action.ReturnTypeOverride}>";
                        fetchMethodName = "FetchAsync";
                    }
                    else if (action.ReturnType != null)
                    {
                        returnTypeString = action.ReturnType.ToString();
                        if ((action.ReturnType is PredefinedTypeSyntax && action.ReturnType is not NullableTypeSyntax && returnTypeString != "string") || returnTypeString == "DateTime")
                        {
                            returnTypeString += "?";
                        }
                        returnTypeString = $"<{returnTypeString}>";
                        fetchMethodName = "FetchAsync";
                    }

                    if (returnTypeString == "<ContentResult>") { returnTypeString = "<string>"; }
                    if (returnTypeString == "<JsonResult>") { returnTypeString = "<object>"; }

                    lines.Add($"        public static ServiceResponse{returnTypeString} {action.ActionName}({string.Join(", ", parameters)}, TimeSpan? timeout = null)");
                    lines.Add($"        {{");
                    lines.Add($"            return ServiceClient.{fetchMethodName}{returnTypeString}(HttpMethod.{httpMethod}, $\"{url}\"{urlHandler}, {action.BodyParameter?.Identifier ?? "null"}, timeout, {loggingReceiver}, {customSerializer}).Result;");
                    lines.Add($"        }}");
                    lines.Add($"        public static async Task<ServiceResponse{returnTypeString}> {action.ActionName}Async({string.Join(", ", parameters)}, TimeSpan? timeout = null)");
                    lines.Add($"        {{");
                    lines.Add($"            return await ServiceClient.{fetchMethodName}{returnTypeString}(HttpMethod.{httpMethod}, $\"{url}\"{urlHandler}, {action.BodyParameter?.Identifier ?? "null"}, timeout, {loggingReceiver}, {customSerializer});");
                    lines.Add($"        }}");
                }
            }
            lines.Add($"    }}");
            lines.Add("");

            return lines;
        }

        public List<string> ConvertEnum(EnumModel enumModel)
        {
            throw new NotImplementedException();
        }

        public List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig)
        {
            throw new NotImplementedException();
        }

        public List<string> ModelHeader(ConfigurationTypeOutput outputConfig)
        {
            return new();
        }

        public void PreProcess(CSharpFiles files)
        {
            return;
        }
    }
}

