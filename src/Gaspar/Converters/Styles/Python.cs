using System;
using System.Collections.Generic;
using System.Linq;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Converters
{
    internal class PythonConverter : IConverter
	{
        public Configuration Config { get; set; }
        private int currentIndent = 0;

        public PythonConverter(Configuration config)
        {
            Config = config;
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"{new String(' ', currentIndent * 4)}# {comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new();

            lines.Add("import os");
            lines.Add("import functools");
            lines.Add("import inspect");
            lines.Add("import warnings");
            lines.Add("");
            lines.Add("string_types = (type(b''), type(u''))");
            lines.Add("");
            lines.Add("def gaspar_error_catch(response, url):");
            lines.Add("    if response.status_code == 200:");
            lines.Add("        try:");
            lines.Add("            return response.json()");
            lines.Add("        except:");
            lines.Add("            return response");
            lines.Add("    print (f\"Gaspar: Service call to {url} failed with status code {response.status_code}\")");
            lines.Add("    os.error");
            lines.Add("");
            lines.Add("# https://stackoverflow.com/a/40301488/404459");
            lines.Add("def invalid(reason):");
            lines.Add("    if isinstance(reason, string_types):");
            lines.Add("        def decorator(func1):");
            lines.Add("            if inspect.isclass(func1):");
            lines.Add("                fmt1 = \"Call to invalid class {name} ({reason}).\"");
            lines.Add("            else:");
            lines.Add("                fmt1 = \"Call to invalid function {name} ({reason}).\"");
            lines.Add("");
            lines.Add("            @functools.wraps(func1)");
            lines.Add("            def new_func1(*args, **kwargs):");
            lines.Add("                warnings.simplefilter('always', DeprecationWarning)");
            lines.Add("                warnings.warn(");
            lines.Add("                    fmt1.format(name=func1.__name__, reason=reason),");
            lines.Add("                    category=DeprecationWarning,");
            lines.Add("                    stacklevel=2");
            lines.Add("                )");
            lines.Add("                warnings.simplefilter('default', DeprecationWarning)");
            lines.Add("                return func1(*args, **kwargs)");
            lines.Add("            return new_func1");
            lines.Add("        return decorator");
            lines.Add("");
            lines.Add("    elif inspect.isclass(reason) or inspect.isfunction(reason):");
            lines.Add("        func2 = reason");
            lines.Add("        if inspect.isclass(func2):");
            lines.Add("            fmt2 = \"Call to invalid class {name}.\"");
            lines.Add("        else:");
            lines.Add("            fmt2 = \"Call to invalid function {name}.\"");
            lines.Add("");
            lines.Add("        @functools.wraps(func2)");
            lines.Add("        def new_func2(*args, **kwargs):");
            lines.Add("            warnings.simplefilter('always', DeprecationWarning)");
            lines.Add("            warnings.warn(");
            lines.Add("                fmt2.format(name=func2.__name__),");
            lines.Add("                category=DeprecationWarning,");
            lines.Add("                stacklevel=2");
            lines.Add("            )");
            lines.Add("            warnings.simplefilter('default', DeprecationWarning)");
            lines.Add("            return func2(*args, **kwargs)");
            lines.Add("        return new_func2");
            lines.Add("");
            lines.Add("    else:");
            lines.Add("        raise TypeError(repr(type(reason)))");

            return lines;
        }

        public List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes)
        {
            List<string> lines = new();
            lines.Add($"import requests");
            foreach (string key in outputConfig.Imports.Keys)
            {
                lines.Add($"from {key} import {outputConfig.Imports[key]};");
            }
            lines.Add($"from {outputConfig.HelperFile[..outputConfig.HelperFile.LastIndexOf(".")]} import *");
            lines.Add("");
            return lines;
        }

        public List<string> ControllerFooter()
        {
            return new();
        }

        public List<string> ConvertController(List<ControllerAction> actions, string outputClassName, ConfigurationTypeOutput outputConfig, bool lastController)
        {
            List<string> lines = new();

            lines.Add($"class {outputClassName}Service:");

            foreach (ControllerAction action in actions)
            {
                List<string> parameters = new();
                foreach (Parameter parameter in action.Parameters)
                {
                    parameters.Add($"{parameter.Identifier}");
                }

                string bodyParam = "";
                string httpMethod = action.HttpMethod.ToLower();
                if (action.BodyParameter != null && (httpMethod == "post" || httpMethod == "put" || httpMethod == "delete"))
                {
                    bodyParam = $", data = {action.BodyParameter?.Identifier ?? "null"}";
                }

                if (action.BadMethodReason != null)
                {
                    lines.Add($"    @invalid(\"{action.BadMethodReason}\")");
                    lines.Add($"    def {action.ActionName}({string.Join(", ", parameters)}) -> dict:");
                    lines.Add($"        return");
                }
                else
                {
                    string url = $"{outputConfig.UrlPrefix}/{action.Route}";
                    url += action.Parameters.QueryString(OutputType.Python);

                    lines.Add($"    def {action.ActionName}({string.Join(", ", parameters)}):");
                    lines.Add($"        response = requests.{httpMethod}(f'{url}'{bodyParam})");
                    lines.Add($"        return gaspar_error_catch(response, f'{url}')");
                }
            }
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

