using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Text.RegularExpressions;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Drawing;
using WCKDRZR.Gaspar.Helpers;

namespace WCKDRZR.Gaspar.Converters
{
    using KeyMap = Dictionary<string, (string k, string? m)>;
    
    internal class TypeScriptConverter : IConverter
	{
        public Configuration Config { get; set; }
        public string CurrentFile { get; set; } = "";
        
        private List<string> currentNamespace = new();
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
            { "byte[]", "string" },
            { "ContentResult", "string" },
            { "JsonResult", "object" },
            { "IFormFile", "File" },
        };
        public Dictionary<string, string> TypeScriptTypeTranslations => Config.TypeTranslations != null && Config.TypeTranslations.ContainsKey(OutputType.TypeScript.ToString()) ? Config.TypeTranslations[OutputType.TypeScript.ToString()] : new();
        public Dictionary<string, string> TypeTranslations => DefaultTypeTranslations.Union(TypeScriptTypeTranslations).Union(Config.GlobalTypeTranslations ?? new()).ToDictionary(k => k.Key, v => v.Value);
        public string ConvertType(string type) => TypeTranslations.ContainsKey(type) ? TypeTranslations[type] : type;

        public string arrayRegex = /*language=regex*/ @"^(.+)\[\]$";
        public string simpleCollectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)\s*<([\w\d]+)>\??$";
        public string collectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)\s*<(.+)>\??$";
        public string simpleDictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|OrderedDictionary|SortedDictionary|IReadOnlyDictionary)\s*<([\w\d]+)\s*,\s*([\w\d]+)>\??$";
        public string dictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|OrderedDictionary|SortedDictionary|IReadOnlyDictionary)\s*<([\w\d]+)\s*,\s*(.+)>\??$";
        public string keyValuePairRegex = /*language=regex*/ @"^KeyValuePair<([\w\d]+)\s*,\s*(.+)>\??$";

        private Dictionary<string, KeyMap> _jsonPropertyKeys = new();


        public TypeScriptConverter(Configuration config, List<Model> allModels)
        {
            Config = config;

            if (config.Models != null)
            {
                foreach (Model model in allModels)
                {
                    KeyMap jsonKeys = new();
                    GetJsonPropertyKeys(model, allModels, config.Models.Output.FirstOrDefault(c => c.Type == OutputType.TypeScript) ?? config.Models.Output[0], ref jsonKeys);
                    if (jsonKeys.Any()) { _jsonPropertyKeys.Add(model.FullName, jsonKeys); }
                }
                AddPropertyMapsToJsonPropertyKeys();
            }
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"{Indent()}//{comment}{new String('\n', followingBlankLines)}";
        }
        private string Indent(int offset = 0) => new(' ', (currentIndent + offset) * 4);

        public List<string> FileComment(ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            if (file.Path != CurrentFile)
            {
                CurrentFile = file.Path;
                return new() { Comment("File: " + FileHelper.RelativePath(outputConfig.Location, file.Path), 1) };
            }
            return new();
        }

        public List<string> ModelHeader(ConfigurationTypeOutput outputConfig)
        {
            return new();
        }

        public List<string> ModelNamespace(List<ClassDeclarationSyntax> parentClasses)
        {
            List<string> lines = new();

            List<string> ns = parentClasses.Select(c => c.Identifier.ToString()).Reverse().ToList();

            int matchingPath = 0;
            for (int i = 0; i < currentNamespace.Count(); i++)
            {
                if (ns.Count > i && ns[i] == currentNamespace[i])
                {
                    matchingPath++;
                }
            }
            for (int i = 0; i < currentNamespace.Count() - matchingPath; i++)
            {
                lines.Add($"{Indent(-1)}}}\n");
                currentIndent--;
            }

            for (int i = 0; i < ns.Count(); i++)
            {
                if (currentNamespace.Count <= i || currentNamespace[i] != ns[i])
                {
                    lines.Add($"{Indent()}export namespace {ns[i]} {{\n");
                    currentIndent++;
                }
            }
            currentNamespace = ns;

            return lines;
        }

        public List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            List<string> lines = new();

            lines.AddRange(ModelNamespace(model.ParentClasses));
            lines.AddRange(FileComment(outputConfig, file));

            if (model.Enumerations.Count > 0)
            {
                lines.AddRange(ConvertEnum(new EnumModel { Identifier = model.ModelName, Values = model.Enumerations }, outputConfig, file));
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

            lines.Add($"{Indent()}export interface {model.ModelName}{(model.Enumerations.Count > 0 ? "_Properties" : "")}{baseClasses} {{");

            if (model.Enumerations.Count > 0)
            {
                lines.Add($"{Indent(1)}id: number;");
                lines.Add($"{Indent(1)}name: string;");
            }

            if (indexSignature != null)
            {
                lines.Add($"{Indent(1)}{ConvertIndexType(indexSignature, outputConfig)};");
            }

            foreach (Property member in model.Fields.Concat(model.Properties))
            {
                lines.Add($"{Indent(1)}{ConvertProperty(member, outputConfig)};");
            }

            lines.Add($"{Indent()}}}\n");

            return lines;
        }

        public List<string> ConvertEnum(EnumModel enumModel, ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            List<string> lines = new();

            lines.AddRange(ModelNamespace(enumModel.ParentClasses));
            lines.AddRange(FileComment(outputConfig, file));

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

        public List<string> ModelFooter()
        {
            List<string> lines = new();
            
            for (int i = 0; i < currentNamespace.Count(); i++)
            {
                lines.Add($"{Indent(-1)}}}\n");
                currentIndent--;
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
            lines.Add($"    {TypeScriptServiceErrorMessage.None},");
            lines.Add($"    {TypeScriptServiceErrorMessage.Generic},");
            lines.Add($"    {TypeScriptServiceErrorMessage.ServerResponse},");
            lines.Add("}");
            lines.Add("export type JsonProperyKeyMap = Record<string, { k: string, m?: JsonProperyKeyMap }>;");

            lines.Add("export class GasparServiceHelper {");
            lines.Add("    async fetch<T>(url: string, options: RequestInit, responseIsString: boolean, returnKeyMap: JsonProperyKeyMap | null, showError: ServiceErrorMessage): Promise<ServiceResponse<T>> {");
            lines.Add("        return fetch(url, options).then(async response => {");
            lines.Add("            if (response.ok) {");
            lines.Add("                try {");
            lines.Add("                    let data = await (responseIsString ? (response.status == 200 ? response.text() : null) : response.json());");
            lines.Add("                    if (returnKeyMap) { data = JsonProperyKeys.fromJson(data, returnKeyMap); }");
            lines.Add("                    return new ServiceResponse<T>(data, null);");
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

            if (_jsonPropertyKeys.Any())
            {
                lines.Add("export namespace JsonProperyKeys {");
                foreach (var propertyKey in _jsonPropertyKeys)
                {
                    string keys = string.Join(", ", propertyKey.Value.Select(k => $"'{ConvertIdentifier(k.Key)}': {{ k: '{k.Value.k}'{(k.Value.m != null ? $", m: JsonProperyKeys.{k.Value.m}()" : "")} }}"));
                    lines.Add($"    export function {propertyKey.Key.Replace(".", "_")}(): JsonProperyKeyMap {{ return {{ {keys} }} }}");
                }
                lines.Add("");
                lines.Add("    export function toJson<T>(obj: T, map: JsonProperyKeyMap): T {");
                lines.Add("        if (obj === null) {");
                lines.Add("            return obj");
                lines.Add("        }");
                lines.Add("        if (Array.isArray(obj)) {");
                lines.Add("            let workingArr = [...obj] as any");
                lines.Add("            for (let i = 0; i < workingArr.length; i++) {");
                lines.Add("                workingArr[i] = toJson(workingArr[i], map);");
                lines.Add("            }");
                lines.Add("            return workingArr");
                lines.Add("        } else {");
                lines.Add("            let workingObj = {...obj} as any");
                lines.Add("            Object.keys(map).forEach(key => {");
                lines.Add("                if (workingObj[key] !== undefined) {");
                lines.Add("                    workingObj[map[key].k] = map[key].m ? toJson(workingObj[key], map[key].m!) : workingObj[key]");
                lines.Add("                    if (key != map[key].k) {");
                lines.Add("                        delete workingObj[key]");
                lines.Add("                    }");
                lines.Add("                }");
                lines.Add("            })");
                lines.Add("            return workingObj");
                lines.Add("        }");
                lines.Add("    }");
                lines.Add("    export function fromJson<T>(obj: T, map: JsonProperyKeyMap): T {");
                lines.Add("        if (obj === null) {");
                lines.Add("            return obj");
                lines.Add("        }");
                lines.Add("        if (Array.isArray(obj)) {");
                lines.Add("            let workingArr = [...obj] as any");
                lines.Add("            for (let i = 0; i < workingArr.length; i++) {");
                lines.Add("                workingArr[i] = fromJson(workingArr[i], map);");
                lines.Add("            }");
                lines.Add("            return workingArr");
                lines.Add("        } else {");
                lines.Add("            let workingObj = {...obj} as any");
                lines.Add("            Object.keys(map).forEach(key => {");
                lines.Add("                if (workingObj[map[key].k] !== undefined) {");
                lines.Add("                    workingObj[key] = map[key].m ? fromJson(workingObj[map[key].k], map[key].m!) : workingObj[map[key].k]");
                lines.Add("                    if (key != map[key].k) {");
                lines.Add("                        delete workingObj[map[key].k]");
                lines.Add("                    }");
                lines.Add("                }");
                lines.Add("            })");
                lines.Add("            return workingObj");
                lines.Add("        }");
                lines.Add("    }");
                lines.Add("}");
            }

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
                string parsed = ParseType(type, outputConfig, allowAddNull: false);
                if (!parsedCustomTypes.Contains(parsed) && !disallowList.Contains(parsed))
                {
                    parsedCustomTypes.Add(parsed);
                }
            }

            lines.Add($"import {{ {string.Join(", ", parsedCustomTypes.Except(outputConfig.Imports.Keys))} }} from \"{outputConfig.ModelPath}\";");
            foreach (string key in outputConfig.Imports.Keys)
            {
                lines.Add($"import {{ {key} }} from \"{outputConfig.Imports[key]}\";");
            }

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
            lines.Add($"    export class {outputClassName}Controller {{");
            
            foreach (ControllerAction action in actions)
            {
                string actionName = ConvertIdentifier(action.OutputActionName);

                List<string> parameters = new();
                List<string> parameterKeyMaps = new();

                foreach (Parameter parameter in action.Parameters)
                {
                    string? type = parameter.Type != null ? ParseType(parameter.Type.ToString(), outputConfig) : null;

                    string newParam = $"{parameter.Identifier}: {type}";
                    if (parameter.DefaultValue != null)
                    {
                        if (parameter.DefaultValue == "null" && !newParam.Contains("null"))
                        {
                            newParam += " | null";
                        }
                        newParam += $" = {parameter.DefaultValue.Replace("\"", "'")}";
                    }
                    parameters.Add(newParam);

                    string? parameterKeyMap = KeyMapForProperty(type);
                    if (parameterKeyMap != null)
                    {
                        parameterKeyMaps.Add($"{parameter.Identifier} = JsonProperyKeys.toJson({parameter.Identifier}, {parameterKeyMap})");
                    }
                }
                if (action.Headers != null)
                {
                    if (action.Headers.Length == 0)
                    {
                        parameters.Add("headers: Record<string, string>");
                    }
                    else
                    {
                        parameters.Add($"headers: {{ {string.Join(", ", action.Headers.Select(h => $"{h}: string"))} }}");
                    }
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
                    string url = outputConfig.AddUrlPrefix(action.Route.Replace("{", "${"));
                    url += action.Parameters.QueryString(OutputType.TypeScript, "$");
                    if (!string.IsNullOrEmpty(outputConfig.UrlHandlerFunction))
                    {
                        url = $"{outputConfig.UrlHandlerFunction}(`{url}`)";
                    }
                    else {
                        url = $"`{url}`";
                    }

                    string bodyParam = "";
                    List<string> formParams = new();
                    List<string> headerParams = new();

                    string httpMethod = action.HttpMethod.ToUpper();
                    if (httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "DELETE")
                    {
                        Parameter? bodyParameter = action.Parameters.FirstOrDefault(p => p.Source == ParameterSource.Body);
                        if (bodyParameter != null)
                        {
                            bodyParam = $", body: JSON.stringify({bodyParameter.Identifier}), headers: {{ 'Content-Type': 'application/json' }}";
                        }
                        IEnumerable<Parameter> formParameters = action.Parameters.Where(p => p.Source == ParameterSource.Form);
                        if (formParameters.Any())
                        {
                            formParams.Add("let data = new FormData()");
                            bodyParam = ", body: data";
                            foreach (Parameter parameter in formParameters)
                            {
                                string value = $"JSON.stringify({parameter.Identifier})";
                                string? type = parameter.Type != null ? ParseType(parameter.Type.ToString(), outputConfig) : null;
                                if (type == "File" || type == "File | null" || type == "string" || type == "string | null")
                                {
                                    value = parameter.Identifier;
                                }
                                formParams.Add($"if ({parameter.Identifier}) {{ data.append('{parameter.Identifier}', {value}) }}");
                            }
                        }
                    }
                    IEnumerable<Parameter> headerParameters = action.Parameters.Where(p => p.Source == ParameterSource.Header);
                    if (headerParameters.Any() || action.Headers != null)
                    {
                        headerParams.Add($"let headersToSend: Record<string, string> = {(action.Headers == null ? "{}" : "headers")}");
                        bodyParam += ", headers: headersToSend";
                        foreach (Parameter parameter in headerParameters)
                        {
                            headerParams.Add($"if ({parameter.Identifier}) {{ headersToSend['{parameter.Identifier}'] = {parameter.Identifier}.toString(); }}");
                        }
                    }

                    string returnType = ParseType(action.ReturnTypeOverride ?? action.ReturnType?.ToString() ?? "null", outputConfig);

                    string returnTypeIsString = returnType == "string" || returnType.StartsWith("string |") ? "true" : "false";

                    string returnKeyMap = KeyMapForProperty(returnType) ?? "null";

                    lines.Add($"        {actionName}({string.Join(", ", parameters)}): Promise<ServiceResponse<{returnType}>> {{");
                    lines.AddRange(parameterKeyMaps.Select(m => $"            {m}"));
                    lines.AddRange(formParams.Select(f => $"            {f}"));
                    lines.AddRange(headerParams.Select(f => $"            {f}"));
                    lines.Add($"            return new GasparServiceHelper().fetch({url}, {{ method: '{httpMethod}'{bodyParam} }}, {returnTypeIsString}, {returnKeyMap}, {(string.IsNullOrEmpty(outputConfig.ErrorHandlerPath) ? "ServiceErrorMessage.None" : "showError")});");
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

        public string ConvertDictionary(string dictionary, ConfigurationTypeOutput outputConfig)
        {
            MatchCollection d = Regex.Matches(dictionary, dictionaryRegex);
            MatchCollection simpleDictionary = Regex.Matches(dictionary, simpleDictionaryRegex);

            string propType = "";
            if (simpleDictionary.HasMatch())
            {
                propType = ConvertType(d.At(2));
                if (IsOptional(propType, outputConfig)) { propType += NullSuffix(outputConfig); }
            }
            else
            {
                propType = ConvertType(ParseType(d.At(2), outputConfig));
            }

            

            return $"Record<{ConvertType(d.At(1))}, {propType}>";
        }

        public string ConvertKeyValue(string keyValue)
        {
            MatchCollection kv = Regex.Matches(keyValue, keyValuePairRegex);
            return $"{{ key: {ConvertType(kv.At(1))}, value: {ConvertType(kv.At(2))} }}";
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
                string tmpType = simpleCollection.HasMatch() ? collection.At(1) : ParseType(collection.At(1), outputConfig, allowAddNull: false);
                type = $"{ConvertType(tmpType)}[]";
            }
            else if (dictionary.HasMatch())
            {
                type = $"{ConvertDictionary(propType, outputConfig)}";
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
                type += NullSuffix(outputConfig);
            }
            return type;
        }

        private string NullSuffix(ConfigurationTypeOutput outputConfig)
        {
            string prefix = " | null";
            if (outputConfig.NullablesAlsoUndefinded)
            {
                prefix += " | undefined";
            }
            return prefix;
        }

        private bool IsOptional(string propertyName, ConfigurationTypeOutput outputConfig)
        {
            List<string> explicitlyNulled = new() { "number", "boolean", "any" };
            return propertyName.EndsWith("?")
                || (outputConfig.AddInferredNullables && !explicitlyNulled.Contains(propertyName));
        }

        private void GetJsonPropertyKeys(Model model, List<Model> allModels, ConfigurationTypeOutput outputConfig, ref KeyMap jsonKeys)
        {
            foreach (Property property in model.Properties)
            {
                if (property.JsonPropertyName != null)
                {
                    string type = ParseType(property.Type?.ToString() ?? "", outputConfig, false);
                    if (type.EndsWith("[]")) { type = type[..^2]; }
                    jsonKeys.Add(property.Identifier, ( property.JsonPropertyName, type ));
                }
            }
            foreach (string baseClass in model.BaseClasses)
            {
                Model? baseClassModel = allModels.FirstOrDefault(m => m.FullName == baseClass);
                if (baseClassModel != null)
                {
                    GetJsonPropertyKeys(baseClassModel, allModels, outputConfig, ref jsonKeys);
                }
            }
        }

        private void AddPropertyMapsToJsonPropertyKeys()
        {
            foreach (string mapKey in _jsonPropertyKeys.Keys)
            {
                foreach (string propertyKey in _jsonPropertyKeys[mapKey].Keys)
                {
                    string key = _jsonPropertyKeys[mapKey][propertyKey].k;
                    string? type = _jsonPropertyKeys[mapKey][propertyKey].m;

                    string qualifitedType = string.Join('.', mapKey.Split('.')[..^1]) + '.' + type;
                    string? matchingJsonKey = _jsonPropertyKeys.FirstOrDefault(k => k.Key == qualifitedType).Key;
                    if (matchingJsonKey == null)
                    {
                        matchingJsonKey = _jsonPropertyKeys.FirstOrDefault(k => k.Key == mapKey + '.' + type).Key;
                    }
                    if (matchingJsonKey == null)
                    {
                        matchingJsonKey = _jsonPropertyKeys.FirstOrDefault(k => k.Key == type).Key;
                    }
                    if (matchingJsonKey != null) { matchingJsonKey = matchingJsonKey.Replace(".", "_"); }
                    
                    _jsonPropertyKeys[mapKey][propertyKey] = (key, matchingJsonKey);
                }
            }
        }

        private string? KeyMapForProperty(string? propertyName)
        {
            if (propertyName != null)
            {
                if (_jsonPropertyKeys.TryGetValue(propertyName, out _))
                {
                    return $"JsonProperyKeys.{propertyName.Replace('.', '_')}()";
                }
                else if (_jsonPropertyKeys.TryGetValue(propertyName[..^2], out _))
                {
                    return $"JsonProperyKeys.{propertyName[..^2].Replace('.', '_')}()";
                }
            }
            return null;
        }

        public void PreProcess(CSharpFiles files)
        {
            return;
        }
    }
}