using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Helpers;
using WCKDRZR.Gaspar.Models;

namespace WCKDRZR.Gaspar.Converters
{
    internal class AngularConverter : IConverter
	{
        public Configuration Config { get; set; }
        public string CurrentFile { get; set; } = "";
        private int currentIndent = 0;

        TypeScriptConverter TypeScriptConverter { get; set; }

        public AngularConverter(Configuration config, List<Model> allModels)
        {
            Config = config;
            TypeScriptConverter = new(config, allModels);
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

        public List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            return TypeScriptConverter.ConvertModel(model, outputConfig, file);
        }

        public List<string> ConvertEnum(EnumModel enumModel, ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            return TypeScriptConverter.ConvertEnum(enumModel, outputConfig, file);
        }

        public List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new();

            lines.Add("import { HttpErrorResponse } from \"@angular/common/http\";");
            lines.Add("import { Injectable } from \"@angular/core\";");
            lines.Add("import { of } from \"rxjs\";");
            if (!string.IsNullOrEmpty(outputConfig.ErrorHandlerPath))
            {
                lines.Add($"import {{ ServiceErrorHandler }} from \"{outputConfig.ErrorHandlerPath}\";");
            }
            lines.Add("");
            lines.Add("export class ServiceResponse<T> {");
            lines.Add("    data: T | null;");
            lines.Add("    error: ActionResultError | null;");
            lines.Add("    success: boolean;");
            lines.Add("    hasError: boolean;");
            lines.Add("    constructor(data: T | null, error: ActionResultError | null) {");
            lines.Add("        this.data = data;");
            lines.Add("        this.error = error;");
            lines.Add("        this.success = error == null;");
            lines.Add("        this.hasError = error != null;");
            lines.Add("    }");
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
            lines.Add($"    {TypeScriptServiceErrorMessage.None},");
            lines.Add($"    {TypeScriptServiceErrorMessage.Generic},");
            lines.Add($"    {TypeScriptServiceErrorMessage.ServerResponse},");
            lines.Add("}");
            lines.Add("");
            lines.Add("@Injectable({ providedIn: 'root' })");
            lines.Add("export class ServiceErrorHelper {");
            if (!string.IsNullOrEmpty(outputConfig.ErrorHandlerPath))
            {
                lines.Add("    constructor(private errorHandler: ServiceErrorHandler) {");
                lines.Add("    }");
            }
            lines.Add("    handler<T>(error: ActionResultError, showError: ServiceErrorMessage) {");
            lines.Add("        if (error == null) {");
            lines.Add("            error = { status: 404, title: 'Not Found' } as ActionResultError;");
            lines.Add("        }");
            lines.Add("        if (error instanceof HttpErrorResponse) {");
            lines.Add("            let httpError = error as HttpErrorResponse;");
            lines.Add("            if (httpError.error) {");
            lines.Add("                error = httpError.error;");
            lines.Add("            } else {");
            lines.Add("                error = { status: httpError.status, title: httpError.statusText, detail: httpError.message } as ActionResultError;");
            lines.Add("            }");
            lines.Add("        }");
            if (!string.IsNullOrEmpty(outputConfig.ErrorHandlerPath))
            {
                lines.Add("        if (showError != ServiceErrorMessage.None) {");
                lines.Add("            this.errorHandler.showError(showError == ServiceErrorMessage.ServerResponse && (error?.detail || error?.title) ? error.detail || error.title : null);");
                lines.Add("        }");
            }
            lines.Add("        return of(new ServiceResponse<T>(null, error));");
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
                List<string> disallowList = new() { "string", "object", "any", "File" };
                string parsed = TypeScriptConverter.ParseType(type, outputConfig, allowAddNull: false);
                if (!parsedCustomTypes.Contains(parsed) && !disallowList.Contains(parsed))
                {
                    parsedCustomTypes.Add(parsed);
                }
            }

            lines.Add("import { HttpClient } from \"@angular/common/http\";");
            lines.Add("import { catchError, map } from \"rxjs/operators\";");

            lines.Add($"import {{ {string.Join(", ", parsedCustomTypes.Except(outputConfig.Imports.Keys))} }} from \"{outputConfig.ModelPath}\";");
            foreach (string key in outputConfig.Imports.Keys)
            {
                lines.Add($"import {{ {key} }} from \"{outputConfig.Imports[key]}\";");
            }

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
                lines.Add($"import {{ ServiceResponse, ServiceErrorHelper, ServiceErrorMessage }} from \"{helperFilePath}\"");
                lines.Add("");
            }

            lines.Add($"export namespace {Config.Controllers?.ServiceName.CapitaliseFirst()}Service {{");
            lines.Add("");
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

            lines.Add($"    @Injectable({{ providedIn: 'root' }})");
            lines.Add($"    export class {outputClassName}Controller {{");
            lines.Add($"        constructor(private http: HttpClient, private errorHelper: ServiceErrorHelper) {{");
            lines.Add($"        }}");

            foreach (ControllerAction action in actions)
            {
                string actionName = TypeScriptConverter.ConvertIdentifier(action.OutputActionName);

                List<string> parameters = new();
                foreach (Parameter parameter in action.Parameters)
                {
                    string newParam = $"{parameter.Identifier}: {(parameter.Type != null ? TypeScriptConverter.ParseType(parameter.Type.ToString(), outputConfig) : null)}";
                    if (parameter.DefaultValue != null)
                    {
                        if (parameter.DefaultValue == "null" && !newParam.Contains("null"))
                        {
                            newParam += " | null";
                        }
                        newParam += $" = {parameter.DefaultValue.Replace("\"", "'")}";
                    }
                    parameters.Add(newParam);
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
                    string url = $"{outputConfig.UrlPrefix}{(outputConfig.UrlPrefix?.EndsWith("/") == false && !action.Route.StartsWith("/") ? "/" : "")}{action.Route.Replace("{", "${")}";
                    url += action.Parameters.QueryString(OutputType.Angular, "$");

                    string bodyParam = "";
                    List<string> formParams = new();
                    List<string> headerParams = new();

                    string httpMethod = action.HttpMethod.ToLower();
                    if (httpMethod == "post" || httpMethod == "put" || httpMethod == "delete")
                    {
                        bodyParam = "null";

                        Parameter? bodyParameter = action.Parameters.FirstOrDefault(p => p.Source == ParameterSource.Body);
                        if (bodyParameter != null)
                        {
                            bodyParam = BodyParameterFetchObject(bodyParameter);
                        }
                        IEnumerable<Parameter> formParameters = action.Parameters.Where(p => p.Source == ParameterSource.Form);
                        if (formParameters.Any())
                        {
                            formParams.Add("let data = new FormData()");
                            bodyParam = "data";
                            foreach (Parameter parameter in formParameters)
                            {
                                string value = $"JSON.stringify({parameter.Identifier})";
                                string? type = parameter.Type != null ? TypeScriptConverter.ParseType(parameter.Type.ToString(), outputConfig) : null;
                                if (type == "File" || type == "File | null" || type == "string" || type == "string | null")
                                {
                                    value = parameter.Identifier;
                                }
                                formParams.Add($"if ({parameter.Identifier}) {{ data.append('{parameter.Identifier}', {value}) }}");
                            }
                        }
                    }                    
                    IEnumerable<Parameter> headerParameters = action.Parameters.Where(p => p.Source == ParameterSource.Header);
                    if (headerParameters.Any())
                    {
                        headerParams.Add("let headers: Record<string, string> = {}");
                        foreach (Parameter parameter in headerParameters)
                        {
                            headerParams.Add($"if ({parameter.Identifier}) {{ headers['{parameter.Identifier}'] = {parameter.Identifier}.toString(); }}");
                        }
                    }
                    if (httpMethod == "post" || httpMethod == "put")
                    {
                        bodyParam = $", {bodyParam}";
                        if (headerParams.Any()) { bodyParam += $", {{ headers: headers }}"; }
                    }
                    if (httpMethod == "delete")
                    {
                        bodyParam = (bodyParam != "null" || headerParams.Any()) ? $", {{ {(bodyParam != "null" ? $"body: {bodyParam}" : "")}{(headerParams.Any() ? ", headers: headers" : "")} }}" : "";
                    }

                    string returnType = TypeScriptConverter.ParseType(action.ReturnTypeOverride ?? action.ReturnType?.ToString() ?? "null", outputConfig);

                    lines.Add($"        {actionName}({string.Join(", ", parameters)}): Observable<ServiceResponse<{returnType}>> {{");
                    lines.AddRange(formParams.Select(f => $"            {f}"));
                    lines.AddRange(headerParams.Select(f => $"            {f}"));
                    lines.Add($"            return this.http.{httpMethod}<{returnType}>(`{url}`{bodyParam}).pipe(");
                    lines.Add($"                map(data => new ServiceResponse(data, null)),");
                    lines.Add($"                catchError(error => this.errorHelper.handler<{returnType}>(error, {(string.IsNullOrEmpty(outputConfig.ErrorHandlerPath) ? "ServiceErrorMessage.None" : "showError")}))");
                    lines.Add($"            );");
                    lines.Add($"        }}");
                }
            }

            lines.Add("    }");
            lines.Add("    ");

            return lines;
        }

        private string BodyParameterFetchObject(Parameter? parameter)
        {
            if (parameter == null || parameter.Identifier == null)
            {
                return "null";
            }
            if (parameter.Type?.ToString() == "string" || parameter.Type?.ToString() == "string?")
            {
                return $"{parameter.Identifier} == null ? null : `\"${{{parameter.Identifier}}}\"`";
            }
            return parameter.Identifier;
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
    }
}