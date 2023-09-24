using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Text.RegularExpressions;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Converters
{
    internal class TypeScriptConverter : IConverter
	{
        public Configuration Config { get; set; }
        private int currentIndent = 0;

        public Dictionary<string, string> DefaultTypeTranslations = new() {
            { "int", "number" },
            { "uint", "number" },
            { "double", "number" },
            { "float", "number" },
            { "Int32", "number" },
            { "Int64", "number" },
            { "short", "number" },
            { "ushort", "number" },
            { "long", "number" },
            { "ulong", "number" },
            { "decimal", "number" },
            { "bool", "boolean" },
            { "DateTime", "string" },
            { "DateOnly", "string" },
            { "TimeOnly", "string" },
            { "DateTimeOffset", "string" },
            { "DataTable", "Object" },
            { "Guid", "string" },
            { "dynamic", "any" },
            { "object", "any" },
            { "byte[]", "string" }
        };
        public Dictionary<string, string> TypeTranslations => DefaultTypeTranslations.Union(Config.CustomTypeTranslations ?? new()).ToDictionary(k => k.Key, v => v.Value);
        public string ConvertType(string type) => TypeTranslations.ContainsKey(type) ? TypeTranslations[type] : type;

        public string arrayRegex = /*language=regex*/ @"^(.+)\[\]$";
        public string simpleCollectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)\s*<([\w\d]+)>\??$";
        public string collectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)\s*<(.+)>\??$";
        public string simpleDictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|OrderedDictionary|SortedDictionary|IReadOnlyDictionary)\s*<([\w\d]+)\s*,\s*([\w\d]+)>\??$";
        public string dictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|OrderedDictionary|SortedDictionary|IReadOnlyDictionary)\s*<([\w\d]+)\s*,\s*(.+)>\??$";
        public string keyValuePairRegex = /*language=regex*/ @"^KeyValuePair<([\w\d]+)\s*,\s*(.+)>\??$";


        public TypeScriptConverter(Configuration config)
        {
            Config = config;
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"{new String(' ', currentIndent * 4)}//{comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new();

            if (model.Enumerations.Count > 0)
            {
                lines = ConvertEnum(new EnumModel { Identifier = model.ModelName, Values = model.Enumerations });
                model.ModelName += "_Properties";
                if (model.BaseClasses.Count > 0)
                {
                    int enumBaseIndex = model.BaseClasses.IndexOf("Enumeration");
                    model.BaseClasses.RemoveAt(enumBaseIndex);
                }
            }

            string? indexSignature = null;
            if (model.BaseClasses.Count > 0)
            {
                indexSignature = model.BaseClasses.FirstOrDefault(type => Regex.Matches(type, dictionaryRegex).HasMatch());
                model.BaseClasses = model.BaseClasses.Where(type => !Regex.Matches(type, dictionaryRegex).HasMatch()).ToList();
                for (int i = 0; i < model.BaseClasses.Count; i++)
                {
                    model.BaseClasses[i] = ConvertType(model.BaseClasses[i]);
                }
            }
            string baseClasses = model.BaseClasses.Count > 0 ? $" extends {string.Join(", ", model.BaseClasses)}" : "";

            lines.Add($"export interface {model.ModelName}{baseClasses} {{");

            if (model.Enumerations.Count > 0)
            {
                lines.Add($"    id: number;");
                lines.Add($"    name: string;");
            }

            if (indexSignature != null)
            {
                lines.Add($"    {ConvertIndexType(indexSignature, outputConfig)};");
            }

            foreach (Property member in model.Fields.Concat(model.Properties))
            {
                lines.Add($"    {ConvertProperty(member, outputConfig)};");
            }

            lines.Add("}\n");

            return lines;
        }

        public List<string> ConvertEnum(EnumModel enumModel)
        {
            List<string> lines = new();

            if (Config.Models?.StringLiteralTypesInsteadOfEnums == true)
            {
                lines.Add($"export type {enumModel.Identifier} =");

                int i = 0;
                foreach (KeyValuePair<string, object?> value in enumModel.Values)
                {
                    string delimiter = (i == enumModel.Values.Count - 1) ? ";" : " |";
                    lines.Add($"    '{GetEnumStringValue(value.Key)}'{delimiter}");
                    
                    i++;
                }    
                lines.Add("");
            }
            else
            {
                lines.Add($"export enum {enumModel.Identifier} {{");

                int i = 0;
                foreach (KeyValuePair<string, object?> value in enumModel.Values)
                {
                    if (Config.Models?.UseEnumValue == true)
                    {
                        if (value.Value == null || Double.TryParse(value.Value.ToString(), out double n))
                        {
                            lines.Add($"    {value.Key} = {(value.Value != null ? value.Value : i)},");
                        }
                        else
                        {
                            lines.Add($"    {value.Key} = '{value.Value}',");
                        }
                    }
                    else
                    {
                        lines.Add($"    {value.Key} = '{GetEnumStringValue(value.Key)}',");
                    }
                    i++;
                }
                lines.Add("}");
                lines.Add("");
            }

            return lines;
        }

        public List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new();

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
            lines.Add("    None,");
            lines.Add("    Generic,");
            lines.Add("    ServerResponse,");
            lines.Add("}");

            lines.Add("export class GasparServiceHelper {");
            lines.Add("    async fetch<T>(url: string, options: RequestInit, showError: ServiceErrorMessage): Promise<ServiceResponse<T>> {");
            lines.Add("        return fetch(url, options).then(async response => {");
            lines.Add("            if (response.ok) {");
            lines.Add("                try {");
            lines.Add("                    return new ServiceResponse<T>(await response.json(), null);");
            lines.Add("                } catch {}");
            lines.Add("            }");
            lines.Add("            return this.responseErrorHandler<T>(response, showError);");
            lines.Add("        }).catch((reason: Error) => this.caughtErrorHandler<T>(reason, url, showError));");
            lines.Add("    }");
            lines.Add("    async responseErrorHandler<T>(response: Response, showError: ServiceErrorMessage): Promise<ServiceResponse<T>> {");
            lines.Add("        let error: ActionResultError = await response.text().then((body: any) => {");
            lines.Add("            try {");
            lines.Add("                return JSON.parse(body);");
            lines.Add("            }");
            lines.Add("            catch {");
            lines.Add("                return { status: response.status, title: response.statusText, detail: body } as ActionResultError");
            lines.Add("            }");
            lines.Add("        });");
            lines.Add("        return this.actionResultErrorHandler(error, showError);");
            lines.Add("    }");
            lines.Add("    async caughtErrorHandler<T>(caughtError: Error, url: string, showError: ServiceErrorMessage): Promise<ServiceResponse<T>> {");
            lines.Add("        console.error(url, caughtError);");
            lines.Add("        let error = { status: -1, title: caughtError.name, detail: caughtError.message } as ActionResultError;");
            lines.Add("        return this.actionResultErrorHandler(error, showError);");
            lines.Add("    }");
            lines.Add("    async actionResultErrorHandler<T>(error: ActionResultError, showError: ServiceErrorMessage): Promise<ServiceResponse<T>> {");
            if (!string.IsNullOrEmpty(outputConfig.ErrorHandlerPath))
            {
                lines.Add("        if (showError != ServiceErrorMessage.None) {");
                lines.Add("            new ServiceErrorHandler().showError(showError == ServiceErrorMessage.ServerResponse && (error?.detail || error?.title) ? error.detail || error.title : null);");
                lines.Add("        }");
            }
            lines.Add("        return new ServiceResponse<T>(null, error);");
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
                string parsed = ParseType(type, outputConfig, allowAddNull: false);
                if (parsed != "string" && !parsedCustomTypes.Contains(parsed))
                {
                    parsedCustomTypes.Add(parsed);
                }
            }

            lines.Add($"import {{ {string.Join(", ", parsedCustomTypes)} }} from \"{outputConfig.ModelPath}\";");

            if (outputConfig.HelperFile == null)
            {
                lines.AddRange(ControllerHelperFile(outputConfig));
            }
            else
            {
                string helperFilePath = "./" + outputConfig.HelperFile.Replace(Path.GetExtension(outputConfig.HelperFile), "");
                lines.Add($"import {{ GasparServiceHelper, ServiceResponse, ServiceErrorMessage }} from \"{helperFilePath}\"");
                lines.Add("");
            }

            lines.Add($"export namespace {Config.Controllers?.ServiceName.ToProper()}Service {{");
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
            lines.Add($"    export class {outputClassName}Controller {{");
            
            foreach (ControllerAction action in actions)
            {
                string actionName = ConvertIdentifier(action.OutputActionName);

                List<string> parameters = new();
                foreach (Parameter parameter in action.Parameters)
                {
                    string newParam = $"{parameter.Identifier}: {(parameter.Type != null ? ParseType(parameter.Type.ToString(), outputConfig) : null)}";
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
                    string url = $"{outputConfig.UrlPrefix}/{action.Route.Replace("{", "${")}";
                    url += action.Parameters.QueryString(OutputType.Angular, "$");

                    string bodyParam = "";
                    string httpMethod = action.HttpMethod.ToUpper();
                    if (action.BodyParameter?.Identifier != null && (httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "DELETE"))
                    {
                        bodyParam = $", body: JSON.stringify({action.BodyParameter.Identifier}), headers: {{ 'Content-Type': 'application/json' }}";
                    }

                    string returnType = ParseType(action.ReturnTypeOverride ?? action.ReturnType?.ToString() ?? "null", outputConfig);

                    lines.Add($"        {actionName}({string.Join(", ", parameters)}): Promise<ServiceResponse<{returnType}>> {{");
                    lines.Add($"            return new GasparServiceHelper().fetch(`{url}`, {{ method: '{httpMethod}'{bodyParam} }}, showError);");
                    lines.Add($"        }}");
                }
            }

            lines.Add("    }");
            lines.Add("    ");

            return lines;
        }



        public string ConvertProperty(Property property, ConfigurationTypeOutput outputConfig)
        {
            string identifier = ConvertIdentifier(property.Identifier.Split(' ')[0]);
            string? type = property.Type != null ? ParseType(property.Type, outputConfig) : null;

            return $"{identifier}: {type}";
        }

        public string ConvertIndexType(string indexType, ConfigurationTypeOutput outputConfig)
        {
            MatchCollection dictionary = Regex.Matches(indexType, dictionaryRegex);
            MatchCollection simpleDictionary = Regex.Matches(indexType, simpleDictionaryRegex);

            string propType = simpleDictionary.HasMatch() ? dictionary.At(2) : ParseType(dictionary.At(2), outputConfig);

            return $"[key: {ConvertType(dictionary.At(1))}]: {ConvertType(propType)}";
        }

        public string ConvertRecord(string record, ConfigurationTypeOutput outputConfig)
        {
            MatchCollection dictionary = Regex.Matches(record, dictionaryRegex);
            MatchCollection simpleDictionary = Regex.Matches(record, simpleDictionaryRegex);

            string propType = simpleDictionary.HasMatch() ? dictionary.At(2) : ParseType(dictionary.At(2), outputConfig);

            return $"Record<{ConvertType(dictionary.At(1))}, {ConvertType(propType)}>";
        }

        public string ConvertKeyValue(string record)
        {
            MatchCollection keyValue = Regex.Matches(record, keyValuePairRegex);
            return $"{{ key: {ConvertType(keyValue.At(1))}, value: {ConvertType(keyValue.At(2))} }}";
        }

        public string ConvertIdentifier(string identifier) => JsonNamingPolicy.CamelCase.ConvertName(identifier);

        public string GetEnumStringValue(string value) => JsonNamingPolicy.CamelCase.ConvertName(value);

        public string ParseType(TypeSyntax propType, ConfigurationTypeOutput outputConfig)
        {
            return ParseType(propType.ToString(), outputConfig);
        }
        public string ParseType(string propType, ConfigurationTypeOutput outputConfig, bool allowAddNull = true)
        {
            if (TypeTranslations.ContainsKey(propType))
            {
                return ConvertType(propType);
            }

            bool isArray = false;
            MatchCollection array = Regex.Matches(propType, arrayRegex);
            if (array.HasMatch())
            {
                propType = array.At(1);
                isArray = true;
            }

            MatchCollection collection = Regex.Matches(propType, collectionRegex);
            MatchCollection dictionary = Regex.Matches(propType, dictionaryRegex);
            MatchCollection keyvalue = Regex.Matches(propType, keyValuePairRegex);

            string type;

            if (collection.HasMatch())
            {
                MatchCollection simpleCollection = Regex.Matches(propType, simpleCollectionRegex);
                propType = simpleCollection.HasMatch() ? collection.At(1) : ParseType(collection.At(1), outputConfig);
                type = $"{ConvertType(propType)}[]";
            }
            else if (dictionary.HasMatch())
            {
                type = $"{ConvertRecord(propType, outputConfig)}";
            }
            else if (keyvalue.HasMatch())
            {
                type = $"{ConvertKeyValue(propType)}";
            }
            else
            {
                type = ConvertType(propType.EndsWith("?") ? propType[0..^1] : propType);
            }

            if (isArray)
            {
                type += "[]";
            }
            if (allowAddNull && IsOptional(propType, outputConfig))
            {
                type += " | null";
                if (outputConfig.NullablesAlsoUndefinded)
                {
                    type += " | undefined";
                }
            }
            return type;
        }

        private bool IsOptional(string propertyName, ConfigurationTypeOutput outputConfig)
        {
            List<string> explicitlyNulled = new() { "number", "boolean", "any" };
            return propertyName.EndsWith("?")
                || (outputConfig.AddInferredNullables && !explicitlyNulled.Contains(propertyName));
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