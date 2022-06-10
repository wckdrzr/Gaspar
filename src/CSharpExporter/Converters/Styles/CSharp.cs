﻿using System;
using System.Collections.Generic;
using System.Linq;
using WCKDRZR.CSharpExporter.Extensions;
using WCKDRZR.CSharpExporter.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.Converters
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
            lines.Add($"using System.Collections.Generic;");
            foreach (string ns in outputConfig.ModelNamespaces)
            {
                lines.Add($"using {ns};");
            }
            lines.Add("");
            lines.Add($"namespace WCKDRZR.CSharpExporter.ServiceCommunciation.{Config.Controllers.ServiceName.ToProper()}Service");
            lines.Add($"{{");
            currentIndent++;
            return lines;
        }

        public List<string> ControllerFooter()
        {
            currentIndent--;
            return new() { "}" };
        }

        public List<string> ConvertController(Controller controller, ConfigurationTypeOutput outputConfig, bool lastController)
        {
            List<string> lines = new();

            lines.Add($"    public static class {controller.OutputClassName}Service");
            lines.Add($"    {{");

            foreach (ControllerAction action in controller.Actions)
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
                if (action.BodyType != null)
                {
                    parameters.Add($"{action.BodyType} body");
                }

                if (action.BadMethodReason != null)
                {
                    lines.Add($"        [System.Obsolete(\"{action.BadMethodReason}\", true)]");
                    lines.Add($"        public static void {action.ActionName}({string.Join(", ", parameters)}) {{ }}");
                }
                else
                {
                    string httpMethod = action.HttpMethod.ToProper();

                    string url = $"{outputConfig.UrlPrefix}/{action.Route}";
                    url += action.Parameters.QueryString();

                    string urlHandler = "";
                    if (!string.IsNullOrEmpty(outputConfig.UrlHandlerFunction)) { urlHandler = $".{outputConfig.UrlHandlerFunction}()"; }

                    string customSerializer = "";
                    if (action.CustomSerializer != null) { customSerializer = $", typeof({action.CustomSerializer})"; }

                    //!! if return type is bool (int/any primative); should be nullable

                    lines.Add($"        public static ServiceResponse<{action.ReturnType}> {action.ActionName}({string.Join(", ", parameters)})");
                    lines.Add($"        {{");
                    lines.Add($"            return new(ServiceHttpMethod.{httpMethod}, $\"{url}\"{urlHandler}, {(action.BodyType != null ? "body" : "null")}{customSerializer});");
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

        public List<string> ConvertModel(Model model)
        {
            throw new NotImplementedException();
        }
    }
}
