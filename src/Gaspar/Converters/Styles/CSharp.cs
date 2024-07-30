using System;
using System.Collections.Generic;
using System.Linq;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.Gaspar.Helpers;

namespace WCKDRZR.Gaspar.Converters
{
    internal class CSharpConverter : IConverter
	{
        public Configuration Config { get; set; }
        public string CurrentFile { get; set; } = "";
        private int currentIndent = 0;

        public CSharpConverter(Configuration config)
        {
            Config = config;
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"{new String(' ', currentIndent * 4)}//{comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> FileComment(ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            if (file.Path != CurrentFile)
            {
                CurrentFile = file.Path;
                return new() { Comment("File: " + FileHelper.RelativePath(outputConfig.Location, file.Path), 1) };
            }
            return new();
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
                    string newParam = $"{ConvertType(parameter.Type)} {parameter.Identifier}";
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

                    string url = outputConfig.AddUrlPrefix(action.Route);
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

                    string defaultTimeout = "";
                    if (action.Timeout != null)
                    {
                        defaultTimeout = $"\n\t\t\tif (timeout == null) {{ timeout = TimeSpan.FromMilliseconds({action.Timeout}); }}";
                    }
                    parameters.Add($"TimeSpan? timeout = null");

                    string? bodyParameter = action.Parameters.FirstOrDefault(p => p.Source == ParameterSource.Body)?.Identifier;

                    List<string> formParamsBuilder = new();
                    IEnumerable<Parameter> formParameters = action.Parameters.Where(p => p.Source == ParameterSource.Form);
                    if (formParameters.Any())
                    {
                        formParamsBuilder.Add("MultipartFormDataContent data = new();");
                        bodyParameter = "data";
                        foreach (Parameter parameter in formParameters)
                        {
                            string value = $"new StringContent({parameter.Identifier}.ToString())";
                            string type = ConvertType(parameter.Type);
                            if (type.StartsWith("byte[]"))
                            {
                                value = $"new ByteArrayContent({parameter.Identifier})";
                            }
                            formParamsBuilder.Add($"data.Add({value}, \"{parameter.Identifier}\");");
                        }
                    }

                    List<string> headerParamBuilder = new();
                    string headersParam = "new()";
                    IEnumerable<Parameter> headerParameters = action.Parameters.Where(p => p.Source == ParameterSource.Header);
                    if (headerParameters.Any())
                    {
                        headerParamBuilder.Add("Dictionary<string, string> headers = new();");
                        headersParam = "headers";
                        foreach (Parameter parameter in headerParameters)
                        {
                            headerParamBuilder.Add($"headers.Add(\"{parameter.Identifier}\", {parameter.Identifier}.ToString());");
                        }
                    }

                    lines.Add($"        public static ServiceResponse{returnTypeString} {action.ActionName}({string.Join(", ", parameters)})");
                    lines.Add($"        {{{defaultTimeout}");
                    lines.AddRange(formParamsBuilder.Select(f => $"            {f}"));
                    lines.AddRange(headerParamBuilder.Select(f => $"            {f}"));
                    lines.Add($"            return ServiceClient.{fetchMethodName}{returnTypeString}(HttpMethod.{httpMethod}, $\"{url}\"{urlHandler}, {bodyParameter ?? "null"}, {headersParam}, timeout, {loggingReceiver}, {customSerializer}).Result;");
                    lines.Add($"        }}");
                    lines.Add($"        public static async Task<ServiceResponse{returnTypeString}> {action.ActionName}Async({string.Join(", ", parameters)})");
                    lines.Add($"        {{{defaultTimeout}");
                    lines.AddRange(formParamsBuilder.Select(f => $"            {f}"));
                    lines.AddRange(headerParamBuilder.Select(f => $"            {f}"));
                    lines.Add($"            return await ServiceClient.{fetchMethodName}{returnTypeString}(HttpMethod.{httpMethod}, $\"{url}\"{urlHandler}, {bodyParameter ?? "null"}, {headersParam}, timeout, {loggingReceiver}, {customSerializer});");
                    lines.Add($"        }}");
                }
            }
            lines.Add($"    }}");
            lines.Add("");

            return lines;
        }

        public List<string> ConvertEnum(EnumModel enumModel, ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            throw new NotImplementedException();
        }

        public List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            throw new NotImplementedException();
        }

        public List<string> ModelHeader(ConfigurationTypeOutput outputConfig)
        {
            return new();
        }

        public List<string> ModelNamespace(List<ClassDeclarationSyntax> parentClasses)
        {
            return new();
        }

        public List<string> ModelFooter()
        {
            return new();
        }

        public void PreProcess(CSharpFiles files)
        {
            return;
        }

        private string ConvertType(TypeSyntax? typeSyntax)
        {
            string type = typeSyntax?.ToString() ?? "";
            if (type == "IFormFile") { type = "byte[]"; }
            if (type == "IFormFile?") { type = "byte[]?"; }
            return type;
        }

        public List<string> ModelNamespace(Model model, ConfigurationTypeOutput outputConfig)
        {
            throw new NotImplementedException();
        }
    }
}

