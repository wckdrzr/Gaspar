using System;
using System.Collections.Generic;
using System.IO;
using WCKDRZR.CSharpExporter.Extensions;
using WCKDRZR.CSharpExporter.Models;

namespace WCKDRZR.CSharpExporter.Converters
{
    internal class AngularConverter : IConverter
	{
        public Configuration Config { get; set; }
        private int currentIndent = 0;

        TypeScriptConverter TypeScriptConverter { get; set; }

        public AngularConverter(Configuration config)
        {
            Config = config;
            TypeScriptConverter = new(config);
        }


        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"{new String(' ', currentIndent * 4)}//{comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> ConvertModel(Model model)
        {
            return TypeScriptConverter.ConvertModel(model);
        }

        public List<string> ConvertEnum(EnumModel enumModel)
        {
            return TypeScriptConverter.ConvertEnum(enumModel);
        }

        public List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new();

            lines.Add("import { Injectable } from \"@angular/core\";");
            lines.Add("import { of } from \"rxjs\";");
            if (!string.IsNullOrEmpty(outputConfig.ErrorHandlerPath))
            {
                lines.Add($"import {{ ServiceErrorHandler }} from \"{outputConfig.ErrorHandlerPath}\";");
            }
            lines.Add("");
            lines.Add("export interface IServiceResponse<T> {");
            lines.Add("    data?: T,");
            lines.Add("    error?: ActionResultError,");
            lines.Add("}");
            lines.Add("export interface ActionResultError {");
            lines.Add("    detail: string,");
            lines.Add("    instance: string,");
            lines.Add("    status: number,");
            lines.Add("    title: string,");
            lines.Add("    traceId: string,");
            lines.Add("    type: string,");
            lines.Add("}");
            lines.Add("export enum ServiceErrorMessage {");
            lines.Add($"    {AngularServiceErrorMessage.None},");
            lines.Add($"    {AngularServiceErrorMessage.Generic},");
            lines.Add($"    {AngularServiceErrorMessage.ServerResponse},");
            lines.Add("}");
            lines.Add("");
            lines.Add("@Injectable({ providedIn: 'root' })");
            lines.Add("export class ServiceErrorHelper {");
            if (!string.IsNullOrEmpty(outputConfig.ErrorHandlerPath))
            {
                lines.Add("    constructor(private errorHandler: ServiceErrorHandler) {");
                lines.Add("    }");
            }
            lines.Add("    handler(error: ActionResultError, showError: ServiceErrorMessage) {");
            if (!string.IsNullOrEmpty(outputConfig.ErrorHandlerPath))
            {
                lines.Add("        if (showError != ServiceErrorMessage.None) {");
                lines.Add("            this.errorHandler.showError(showError == ServiceErrorMessage.ServerResponse && error?.detail ? error.detail : null);");
                lines.Add("        }");
            }
            lines.Add("        return of({ error: error || {} });");
            lines.Add("    }");
            lines.Add("}");
            lines.Add("");

            return lines;
        }

        public List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes)
        {
            List<string> lines = new();

            List<string> parsedCustomTypes = new();
            foreach (string type in customTypes)
            {
                string parsed = TypeScriptConverter.ParseType(type);
                if (parsed != "string | null" && !parsedCustomTypes.Contains(parsed))
                {
                    parsedCustomTypes.Add(parsed);
                }
            }

            lines.Add("import { HttpClient } from \"@angular/common/http\";");
            lines.Add("import { catchError, map } from \"rxjs/operators\";");
            lines.Add($"import {{ {string.Join(", ", parsedCustomTypes)} }} from \"{outputConfig.ModelPath}\";");

            if (outputConfig.HelperFile == null)
            {
                lines.Add("import { Observable } from \"rxjs\";");
                lines.AddRange(ControllerHelperFile(outputConfig));
            }
            else
            {
                string helperFilePath = "./" + outputConfig.HelperFile.Replace(Path.GetExtension(outputConfig.HelperFile), "");

                lines.Add("import { Injectable } from \"@angular/core\";");
                lines.Add("import { Observable } from \"rxjs\";");
                lines.Add($"import {{ IServiceResponse, ServiceErrorHelper, ServiceErrorMessage }} from \"{helperFilePath}\"");
                lines.Add("");
            }

            lines.Add($"export namespace {Config.Controllers.ServiceName.ToProper()}Service {{");
            lines.Add("");
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

            lines.Add($"    @Injectable({{ providedIn: 'root' }})");
            lines.Add($"    export class {controller.OutputClassName}Controller {{");
            lines.Add($"        constructor(private http: HttpClient, private errorHelper: ServiceErrorHelper) {{");
            lines.Add($"        }}");

            foreach (ControllerAction action in controller.Actions)
            {
                string actionName = TypeScriptConverter.ConvertIdentifier(action.ActionName);

                List<string> parameters = new();
                foreach (Parameter parameter in action.Parameters)
                {
                    string newParam = $"{parameter.Identifier}: {TypeScriptConverter.ParseType(parameter.Type.ToString())}";
                    if (parameter.DefaultValue != null)
                    {
                        newParam += $" = {parameter.DefaultValue.Replace("\"", "'")}";
                    }
                    parameters.Add(newParam);
                }
                if (action.BodyType != null)
                {
                    parameters.Add($"body: {action.BodyType}");
                }
                if (!string.IsNullOrEmpty(outputConfig.ErrorHandlerPath))
                {
                    parameters.Add($"showError = ServiceErrorMessage.{outputConfig.DefaultErrorMessage}");
                }

                if (action.BadMethodReason != null) {

                    lines.Add($"        /** @deprecated This method is broken: {action.BadMethodReason} */");
                    lines.Add($"        {actionName}({string.Join(", ", parameters)}) {{");
                    lines.Add($"        }}");
                }
                else
                {
                    string url = $"{outputConfig.UrlPrefix}/{action.Route.Replace("{", "${")}";
                    url += action.Parameters.QueryString("$");

                    string bodyParam = "";
                    string httpMethod = action.HttpMethod.ToLower();
                    if (httpMethod == "post" || httpMethod == "put")
                    {
                        bodyParam = $", {(action.BodyType != null ? "body" : "null")}";
                    }
                    if (httpMethod == "delete" && action.BodyType != null)
                    {
                        bodyParam = ", { body: body }";
                    }

                    string returnType = action.ReturnTypeOverride ?? action.ReturnType.ToString();

                    lines.Add($"        {actionName}({string.Join(", ", parameters)}): Observable<IServiceResponse<{TypeScriptConverter.ParseType(returnType)}>> {{");
                    lines.Add($"            return this.http.{httpMethod}<{TypeScriptConverter.ParseType(returnType)}>(`{url}`{bodyParam}).pipe(");
                    lines.Add($"                map(data => ({{ data }})),");
                    lines.Add($"                catchError(error => this.errorHelper.handler(error, {(string.IsNullOrEmpty(outputConfig.ErrorHandlerPath) ? "ServiceErrorMessage.None" : "showError")}))");
                    lines.Add($"            );");
                    lines.Add($"        }}");
                }
            }

            lines.Add("    }");
            lines.Add("    ");

            return lines;
        }
    }
}