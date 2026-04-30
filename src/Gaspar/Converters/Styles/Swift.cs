using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.Gaspar.Helpers;

//update config spec // add spec def to config demo file, and files in use!
//update readme
//make sure spec is correct/matches readme

namespace WCKDRZR.Gaspar.Converters
{   
    internal class SwiftConverter : IConverter
	{
        public Configuration Config { get; set; }
        public string CurrentFile { get; set; } = "";
        
        private List<string> currentNamespace = new();
        private string previousModelClass = "";
        private int currentIndent = 0;

        public Dictionary<string, string> DefaultTypeTranslations = new() {
            { "string", "String" },
            { "int", "Int" },
            { "uint", "UInt" },
            { "double", "Double" },
            { "float", "Float" },
            { "short", "Int16" },
            { "ushort", "UInt16" },
            { "long", "Int64" },
            { "ulong", "UInt64" },
            { "decimal", "Double" },
            { "DateTime", "String" },
            { "DateOnly", "String" },
            { "TimeOnly", "String" },
            { "DateTimeOffset", "String" },
            { "bool", "Bool" },
            { "Boolean", "Bool" },
            { "DataTable", "Object" },
            { "Guid", "String" },
            { "byte[]", "Data" },
            { "ContentResult", "String" },
            { "IFormFile", "File" },
        };
        public Dictionary<string, string> SwiftTypeTranslations => Config.TypeTranslations != null && Config.TypeTranslations.ContainsKey(OutputType.Swift.ToString()) ? Config.TypeTranslations[OutputType.Swift.ToString()] : new();
        public Dictionary<string, string> TypeTranslations => DefaultTypeTranslations.Union(SwiftTypeTranslations).Union(Config.GlobalTypeTranslations ?? new()).ToDictionary(k => k.Key, v => v.Value);
        public string ConvertType(string type) => TypeTranslations.ContainsKey(type) ? TypeTranslations[type] : type;

        public string arrayRegex = /*language=regex*/ @"^(.+)\[\]$";
        public string simpleCollectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)\s*<([\w\d]+)>\??$";
        public string collectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)\s*<(.+)>\??$";
        public string simpleDictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|OrderedDictionary|SortedDictionary|IReadOnlyDictionary)\s*<([\w\d]+)\s*,\s*([\w\d]+)>\??$";
        public string dictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|OrderedDictionary|SortedDictionary|IReadOnlyDictionary)\s*<([\w\d]+)\s*,\s*(.+)>\??$";
        public string keyValuePairRegex = /*language=regex*/ @"^KeyValuePair<([\w\d]+)\s*,\s*(.+)>\??$";

        public SwiftConverter(Configuration config)
        {
            Config = config;
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
            return new()
            {
                "import Foundation",
                "",
                "class BaseCodable: Codable {}",
                "",
            };
        }

        public List<string> ModelNamespace(List<TypeDeclarationSyntax> parentClasses)
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
                    if (previousModelClass != string.Join('.', ns))
                    {
                        lines.Add($"{Indent()}class {ns[i]} {{\n");
                    }
                    currentIndent++;
                }
            }
            currentNamespace = ns;

            return lines;
        }

        public List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig, CSharpFile file, CSharpFiles allFiles)
        {
            List<string> lines = new();

            lines.AddRange(ModelNamespace(model.ParentClasses));
            lines.AddRange(FileComment(outputConfig, file));

            List<EnumModel> allEnums = allFiles.EnumsForType(OutputType.Swift);

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
            string baseClasses = model.BaseClasses.Count > 0 ? $": {string.Join(", ", model.BaseClasses)}" : (model.IsInterface ? "" : ": BaseCodable");

            string classType = model.IsInterface ? "protocol" : "class";
            lines.Add($"{Indent()}{classType} {model.ModelName}{(model.Enumerations.Count > 0 ? "_Properties" : "")}{baseClasses} {{");

            if (model.Enumerations.Count > 0)
            {
                model.Properties.Insert(0, new Property { Identifier = "name", Type = "string" });
                model.Properties.Insert(0, new Property { Identifier = "id", Type = "int" });
            }

            if (indexSignature != null)
            {
                lines.Add($"{Indent(1)}{ConvertIndexType(indexSignature, outputConfig)}");
            }

            List<string> codingKeys = new();
            List<string> initDecodeLines = new();
            List<string> encodeLines = new();

            foreach (Property member in model.Fields.Concat(model.Properties))
            {
                string identifier = ConvertIdentifier(member.Identifier.Split(' ')[0]);
                string? type = member.Type != null ? ParseType(member.Type, outputConfig) : "";
                string defaultValue = type.EndsWith('?') ? "nil" : DefaultValue(member, outputConfig, allEnums);
                if (defaultValue != "") { defaultValue = $" = {defaultValue}"; }

                if (model.IsInterface)
                {
                    defaultValue = " { get set }";
                }

                codingKeys.Add(member.JsonPropertyName == null ? identifier : $"{identifier} = \"{member.JsonPropertyName}\"");
                initDecodeLines.Add($"{identifier} = try values.decode{(type.EndsWith("?") ? "IfPresent" : "")}({(type.EndsWith("?") ? type[..^1] : type)}.self, forKey: .{identifier})");
                encodeLines.Add($"try container.encodeIfPresent({identifier}, forKey: .{identifier})");
                lines.Add($"{Indent(1)}var {identifier}: {type}{defaultValue}");
            }

            if (!model.IsInterface)
            {
                if (codingKeys.Any())
                {
                    lines.Add($"{Indent(1)}");
                }
                lines.Add($"{Indent(1)}override init() {{");
                lines.Add($"{Indent(1)}    super.init()");
                lines.Add($"{Indent(1)}}}");

                lines.Add($"{Indent(1)}required init(from decoder: Decoder) throws {{");
                lines.Add($"{Indent(1)}    try super.init(from: decoder)");
                if (codingKeys.Any())
                {
                    lines.Add($"{Indent(1)}    let values = try decoder.container(keyedBy: CodingKeys.self)");
                    lines.AddRange(initDecodeLines.Select(line => $"{Indent(2)}{line}"));
                }
                lines.Add($"{Indent(1)}}}");

                lines.Add($"{Indent(1)}override func encode(to encoder: Encoder) throws {{");
                lines.Add($"{Indent(1)}    try super.encode(to: encoder)");
                if (codingKeys.Any())
                {
                    lines.Add($"{Indent(1)}    var container = encoder.container(keyedBy: CodingKeys.self)");
                    lines.AddRange(encodeLines.Select(line => $"{Indent(2)}{line}"));
                }
                lines.Add($"{Indent(1)}}}");

                if (codingKeys.Any())
                {
                    lines.Add($"{Indent(1)}private enum CodingKeys: String, CodingKey {{");
                    lines.Add($"{Indent(1)}    case {string.Join(", ", codingKeys)}");
                    lines.Add($"{Indent(1)}}}");
                }
            }

            previousModelClass = model.FullName;

            bool closeClass = true;
            int modelIndex = file.Models.FindIndex(m => m.FullName == model.FullName);
            if (modelIndex >= 0)
            {
                int nextModel = file.Models.FindIndex(modelIndex + 1, m => m.ExportFor.HasFlag(OutputType.Swift));
                if (nextModel >= 0)
                {
                    if (string.Join('.', file.Models[nextModel].FullName.Split('.')[..^1]) == model.FullName)
                    {
                        closeClass = false;
                    }
                }
            }

            if (closeClass)
            {
                lines.Add($"{Indent()}}}");
            }
            lines.Add("");

            return lines;
        }

        public List<string> ConvertEnum(EnumModel enumModel, ConfigurationTypeOutput outputConfig, CSharpFile file)
        {
            List<string> lines = new();

            lines.AddRange(ModelNamespace(enumModel.ParentClasses));
            lines.AddRange(FileComment(outputConfig, file));

            if (enumModel.Values.Any(e => e.Value != null))
            {
                bool numeric = enumModel.Values.Any(e => double.TryParse(e.Value?.ToString(), out _));
                lines.Add($"enum {enumModel.Identifier}: {(numeric ? "Double" : "String")}, Codable, CaseIterable {{");
                lines.Add($"    case {string.Join(", ", enumModel.Values.Select(v => ConvertIdentifier(v.Key) + $" = {(double.TryParse(v.Value?.ToString(), out double num) ? num : $"\"{v.Value}\"")}"))}");
            }
            else
            {
                lines.Add($"enum {enumModel.Identifier}: Codable {{");
                lines.Add($"    case {string.Join(", ", enumModel.Values.Select(v => ConvertIdentifier(v.Key)))}");
            }

            lines.Add("}");
            lines.Add("");

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
            
            lines.Add("import Foundation");
            lines.Add("");
            lines.Add("struct ActionResultError: Codable {");
            lines.Add("    var detail: String?");
            lines.Add("    var instance: String? = nil");
            lines.Add("    var status: Int");
            lines.Add("    var title: String");
            lines.Add("    var traceId: String? = nil");
            lines.Add("    var type: String? = nil");
            lines.Add("    init(title: String, detail: String?, status: Int) {");
            lines.Add("        self.title = title");
            lines.Add("        self.detail = detail");
            lines.Add("        self.status = status");
            lines.Add("    }");
            lines.Add("}");
            lines.Add("struct ServiceResponse<T: Codable>: Codable {");
            lines.Add("    var data: T?");
            lines.Add("    var error: ActionResultError?");
            lines.Add("    var success: Bool { error == nil }");
            lines.Add("    var hasError: Bool { error != nil }");
            lines.Add("    init(data: T?, error: ActionResultError?) {");
            lines.Add("        self.data = data");
            lines.Add("        self.error = error");
            lines.Add("    }");
            lines.Add("    private enum CodingKeys: String, CodingKey { case data, error, success, hasError }");
            lines.Add("    init(from decoder: Decoder) throws {");
            lines.Add("        let container = try decoder.container(keyedBy: CodingKeys.self)");
            lines.Add("        self.data = try container.decodeIfPresent(T.self, forKey: .data)");
            lines.Add("        self.error = try container.decodeIfPresent(ActionResultError.self, forKey: .error)");
            lines.Add("    }");
            lines.Add("    func encode(to encoder: Encoder) throws {");
            lines.Add("        var container = encoder.container(keyedBy: CodingKeys.self)");
            lines.Add("        try container.encodeIfPresent(data, forKey: .data)");
            lines.Add("        try container.encodeIfPresent(error, forKey: .error)");
            lines.Add("        try container.encode(success, forKey: .success)");
            lines.Add("        try container.encode(hasError, forKey: .hasError)");
            lines.Add("    }");
            lines.Add("}");
            lines.Add("struct VoidObject: Codable { }");
            lines.Add("struct File { let name: String; let data: Data }");
            lines.Add("");
            lines.Add("struct GasparServiceHelper {");
            lines.Add("    public static func fetchVoid(method: String, urlStr: String, body: Encodable? = nil, headers: [String: String]? = nil) async -> ServiceResponse<VoidObject> {");
            lines.Add("        return await fetch(method: method, urlStr: urlStr, body: body, headers: headers)");
            lines.Add("    }");
            lines.Add("    public static func fetch<T>(method: String, urlStr: String, body: Encodable? = nil, headers: [String: String]? = nil) async -> ServiceResponse<T> {");
            lines.Add("        do {");
            lines.Add("            guard let url = URL(string: urlStr) else {");
            lines.Add("                throw URLError(.badURL)");
            lines.Add("            }");
            lines.Add("            let (data, response) = try await load(method: method, url: url, body: body, headers: headers)");
            lines.Add("            if ((response?.statusCode ?? 0) >= 200 && (response?.statusCode ?? 0) < 300) {");
            lines.Add("                return success(data: data, urlResponse: response, url: url)");
            lines.Add("            } else {");
            lines.Add("                return serverError(data: data, urlResponse: response, url: url)");
            lines.Add("            }");
            lines.Add("        } catch let e {");
            lines.Add("            return loadException(exception: e, url: urlStr)");
            lines.Add("        }");
            lines.Add("    }");
            lines.Add("    private static func load(method: String, url: URL, body: Encodable?, headers: [String: String]?) async throws -> (Data, HTTPURLResponse?) {");
            lines.Add("        var request = URLRequest(url: url)");
            lines.Add("        var bodyData: Data? = nil");
            lines.Add("        if let body {");
            lines.Add("            if let rawData = body as? Data {");
            lines.Add("                bodyData = rawData");
            lines.Add("            } else {");
            lines.Add("                bodyData = try JSONEncoder().encode(body)");
            lines.Add("                request.setValue(\"application/json; charset=utf-8\", forHTTPHeaderField: \"Content-Type\")");
            lines.Add("            }");
            lines.Add("        }");
            lines.Add("        request.httpMethod = method");
            lines.Add("        request.httpBody = bodyData");
            lines.Add("        for header in headers ?? [:] {");
            lines.Add("            request.setValue(header.value, forHTTPHeaderField: header.key)");
            lines.Add("        }");
            lines.Add("        let (data, response) = try await URLSession.shared.data(for: request)");
            lines.Add("        return (data, response as? HTTPURLResponse)");
            lines.Add("    }");
            lines.Add("    private static func success<T>(data: Data, urlResponse: HTTPURLResponse?, url: URL) -> ServiceResponse<T> {");
            lines.Add("        do {");
            lines.Add("            if T.self == String.self {");
            lines.Add("                let responseString = String(decoding: data, as: UTF8.self)");
            lines.Add("                return ServiceResponse(data: responseString as? T, error: nil)");
            lines.Add("            }");
            lines.Add("            if data.isEmpty {");
            lines.Add("                return ServiceResponse(data: nil, error: nil)");
            lines.Add("            }");
            lines.Add("            let decoder = JSONDecoder()");
            lines.Add("            let response = try decoder.decode(T.self, from: data)");
            lines.Add("            return ServiceResponse(");
            lines.Add("                data: response,");
            lines.Add("                error: nil");
            lines.Add("            )");
            lines.Add("        } catch let e {");
            lines.Add("            log(\"Gaspar: Unable to deserialize successful response from \\(url)\\n\\(e.localizedDescription)\")");
            lines.Add("            return error(title: \"Gaspar: Unable to deserialize successful response from \\(url)\", detail: e.localizedDescription, status: 0)");
            lines.Add("        }");
            lines.Add("    }");
            lines.Add("    private static func serverError<T>(data: Data, urlResponse: HTTPURLResponse?, url: URL) -> ServiceResponse<T> {");
            lines.Add("        var response = ServiceResponse<T>(data: nil, error: nil)");
            lines.Add("        do {");
            lines.Add("            let decoder = JSONDecoder()");
            lines.Add("            response.error = try decoder.decode(ActionResultError.self, from: data)");
            lines.Add("        } catch {");
            lines.Add("            response = self.error(title: \"Gaspar: Service call to \\(url) failed to connect\", detail: \"\", status: urlResponse?.statusCode ?? 0)");
            lines.Add("        }");
            lines.Add("        log(\"Gaspar: Service call to \\(url) failed with status code \\(urlResponse?.statusCode ?? 0)\" +");
            lines.Add("            \"\\((response.error != nil && response.error?.detail != \"\" ? \"\\n\\(response.error?.detail ?? \"\")\" : \"\"))\")");
            lines.Add("        return response");
            lines.Add("    }");
            lines.Add("    private static func loadException<T>(exception: Error, url: String) -> ServiceResponse<T> {");
            lines.Add("        let message = exception.localizedDescription");
            lines.Add("        log(\"Gaspar: Service call to \\(url) failed to connect\\n\\(message)\")");
            lines.Add("        return error(title: \"Gaspar: Service call to \\(url) failed to connect\", detail: message, status: 0)");
            lines.Add("    }");
            lines.Add("    private static func error<T>(title: String, detail: String?, status: Int) -> ServiceResponse<T> {");
            lines.Add("        return ServiceResponse(");
            lines.Add("            data: nil,");
            lines.Add("            error: .init(title: title, detail: detail, status: status)");
            lines.Add("        )");
            lines.Add("    }");
            lines.Add("    private static func log(_ message: String) {");
            lines.Add("        print(\"Gaspar: \\(message)\")");
            lines.Add("    }");
            lines.Add("}");

            lines.Add("");
            return lines;
        }

        public List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes)
        {
            List<string> lines = new();

            if (outputConfig.HelperFile == null)
            {
                lines.AddRange(ControllerHelperFile(outputConfig));
            }

            return lines;
        }

        public List<string> ControllerFooter()
        {
            return new();
        }

        public List<string> ConvertController(List<ControllerAction> actions, string outputClassName, ConfigurationTypeOutput outputConfig, bool lastController)
        {
            List<string> lines = new();

            lines.Add($"struct {outputClassName}Service {{");

            foreach (ControllerAction action in actions)
            {
                List<string> parameters = new();
                foreach (Parameter parameter in action.Parameters)
                {
                    string newParam = $"{parameter.Identifier}: {ParseType($"{parameter.Type}", outputConfig)}";
                    if (parameter.DefaultValue != null)
                    {
                        newParam += $" = {parameter.DefaultValue}";
                    }
                    parameters.Add(newParam);
                }

                if (outputConfig.UrlPrefix != null)
                {
                    parameters.AddRange(Regex.Matches(outputConfig.UrlPrefix, "{param:(.*?)}").Select(m => $"string {m.Groups[1].Value}").ToList());
                }

                if (action.BadMethodReason != null)
                {
                    lines.Add($"    @available(*, deprecated, message: \"{action.BadMethodReason}\")");
                    lines.Add($"    static func {action.ActionName}({string.Join(", ", parameters)}) {{ }}");
                }
                else
                {
                    string httpMethod = action.HttpMethod.ToUpper();

                    string url = outputConfig.AddUrlPrefix(action.Route).Replace("{param:", "{");
                    url = Regex.Replace(url, "{(.*?)}", "\\($1)");
                    url += action.Parameters.QueryString(OutputType.CSharp);

                    string returnTypeString = "";
                    string fetchMethodName = "fetchVoid";
                    if (action.ReturnTypeOverride != null)
                    {
                        returnTypeString = $"<{ConvertType(action.ReturnTypeOverride)}>";
                        fetchMethodName = "fetch";
                    }
                    else if (action.ReturnType != null)
                    {
                        returnTypeString = ConvertType(action.ReturnType.ToString());
                        if ((action.ReturnType is PredefinedTypeSyntax && action.ReturnType is not NullableTypeSyntax && returnTypeString != "string") || returnTypeString == "DateTime")
                        {
                            returnTypeString += "?";
                        }
                        returnTypeString = $"<{returnTypeString}>";
                        fetchMethodName = "fetch";
                    }

                    if (returnTypeString == "<ContentResult>") { returnTypeString = "<String>"; }
                    if (returnTypeString == "<JsonResult>") { returnTypeString = "<Any>"; }


                    if (action.Headers != null)
                    {
                        if (action.Headers.Length == 0)
                        {
                            parameters.Add("headers: [String: String]");
                        }
                        else if (action.Headers.Length == 1)
                        {
                            parameters.Add($"{action.Headers[0]}_header: String");
                        }
                        else
                        {
                            parameters.Add($"headers: ({string.Join(", ", action.Headers.Select(h => $"{h}: String"))})");
                        }
                    }

                    string? bodyParameter = action.Parameters.FirstOrDefault(p => p.Source == ParameterSource.Body)?.Identifier;

                    List<string> formParamsBuilder = new();
                    IEnumerable<Parameter> formParameters = action.Parameters.Where(p => p.Source == ParameterSource.Form);
                    if (formParameters.Any())
                    {
                        
                        formParamsBuilder.Add($"let boundary = UUID().uuidString");
                        formParamsBuilder.Add($"var data = Data()");

                        bodyParameter = "data";
                        foreach (Parameter parameter in formParameters)
                        {
                            string type = ConvertType($"{parameter.Type}");
                            if (type == "File")
                            {
                                formParamsBuilder.Add($"data.append(\"--\\(boundary)\\r\\nContent-Disposition: form-data; name=\\\"{parameter.Identifier}\\\"; filename=\\\"\\({parameter.Identifier}.name)\\\"\\r\\nContent-Type: application/octet-stream\\r\\n\\r\\n\".data(using: .utf8)!)");
                                formParamsBuilder.Add($"data.append({parameter.Identifier}.data)");
                            }
                            else
                            {
                                formParamsBuilder.Add($"data.append(\"--\\(boundary)\\r\\nContent-Disposition: form-data; name=\\\"{parameter.Identifier}\\\"; filename=\\\"\\({parameter.Identifier})\\\"\\r\\nContent-Type: application/octet-stream\\r\\n\\r\\n\".data(using: .utf8)!)");
                                formParamsBuilder.Add($"data.append({parameter.Identifier}.data(using: .utf8) ?? Data())");
                            }
                            formParamsBuilder.Add("data.append(\"\\r\\n--\\(boundary)--\\r\\n\".data(using: .utf8)!)");
                        }
                    }

                    List<string> headerParamBuilder = new();
                    string headersParam = "nil";
                    IEnumerable<Parameter> headerParameters = action.Parameters.Where(p => p.Source == ParameterSource.Header);
                    if (headerParameters.Any() || action.Headers != null || formParameters.Any())
                    {
                        headersParam = "headersToSend";
                        headerParamBuilder.Add($"let headersToSend: [String: String] = [");
                        if (action.Headers != null && action.Headers.Length == 1)
                        {
                            headerParamBuilder.Add($"    \"{action.Headers[0]}\": {action.Headers[0]}_header,");
                        }
                        else
                        {
                            foreach (string header in action.Headers?.ToList() ?? new())
                            {
                                headerParamBuilder.Add($"    \"{header}\": headers.{header},");
                            }
                        }
                        foreach (Parameter parameter in headerParameters)
                        {
                            headerParamBuilder.Add($"    \"{parameter.Identifier}\": String(describing: {parameter.Identifier}),");
                        }
                        if (formParameters.Any())
                        {
                            headerParamBuilder.Add($"    \"Content-Type\": \"multipart/form-data; boundary=\\(boundary)\"");
                        }
                        headerParamBuilder.Add($"]");
                    }

                    lines.Add($"    static func {action.ActionName}({string.Join(", ", parameters)}) async -> ServiceResponse{returnTypeString} {{");
                    lines.AddRange(formParamsBuilder.Select(f => $"        {f}"));
                    lines.AddRange(headerParamBuilder.Select(f => $"        {f}"));
                    lines.Add($"        return await GasparServiceHelper.{fetchMethodName}(method: \"{httpMethod}\", urlStr: \"{url}\", body: {bodyParameter ?? "nil"}, headers: {headersParam})");
                    lines.Add($"    }}");
                }
            }
            lines.Add($"}}");
            lines.Add("");

            return lines;
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
                if (IsOptional(propType, outputConfig)) { propType += "?"; }
            }
            else
            {
                propType = ConvertType(ParseType(d.At(2), outputConfig));
            }

            return $"[{ConvertType(d.At(1))}:{propType}]";
        }

        public string ConvertKeyValue(string keyValue)
        {
            MatchCollection kv = Regex.Matches(keyValue, keyValuePairRegex);
            return $"{{ key: {ConvertType(kv.At(1))}, value: {ConvertType(kv.At(2))} }}";
        }

        public string ConvertIdentifier(string identifier) => JsonNamingPolicy.CamelCase.ConvertName(identifier);

        public string ParseType(string propType, ConfigurationTypeOutput outputConfig)
        {
            if (TypeTranslations.ContainsKey(propType))
            {
                return ConvertType(propType);
            }

            MatchCollection array = Regex.Matches(propType, arrayRegex);
            if (array.HasMatch())
            {
                propType = array.At(1);
            }

            MatchCollection collection = Regex.Matches(propType, collectionRegex);
            MatchCollection dictionary = Regex.Matches(propType, dictionaryRegex);
            MatchCollection keyvalue = Regex.Matches(propType, keyValuePairRegex);

            string type;

            if (collection.HasMatch())
            {
                MatchCollection simpleCollection = Regex.Matches(propType, simpleCollectionRegex);
                string tmpType = simpleCollection.HasMatch() ? collection.At(1) : ParseType(collection.At(1), outputConfig);
                type = $"[{ConvertType(tmpType)}]";
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

            if (IsOptional(propType, outputConfig))
            {
                type += "?";
            }
            return type;
        }

        private string DefaultValue(Property property, ConfigurationTypeOutput outputConfig, List<EnumModel> allEnums)
        {
            string type = property.Type != null ? ParseType(property.Type, outputConfig) : "";
            bool nullable = type.EndsWith("?");

            if (property.DefaultValue != null && !nullable && !type.StartsWith("["))
            {
                if (!property.DefaultValue.StartsWith("new") && !property.DefaultValue.EndsWith("()"))
                {
                    return property.DefaultValue;
                }
            }

            EnumModel? matchingEnum = allEnums.FirstOrDefault(e => e.Identifier == type);
            if (matchingEnum != null && matchingEnum.Values.Count > 0)
            {
                return $"{matchingEnum.Identifier}.{ConvertIdentifier(matchingEnum.Values.First().Key)}";
            }

            switch (type)
            {
                case "String": return "\"\"";
                case "Int": return "0";
                case "UInt": return "0";
                case "Double": return "0.0";
                case "Float": return "0";
                case "Int64": return "0";
                case "Int16": return "0";
                case "UInt64": return "0";
                case "UInt16": return "0";
                case "Bool": return "false";
            }
            if (type.StartsWith('[') && type.EndsWith(']'))
            {
                if (type.Contains(':')) { return "[:]"; }
                return "[]";   
            }
            if (nullable)
            {
                return "nil";
            }

            return $"{type}()";
        }

        private bool IsOptional(string propertyName, ConfigurationTypeOutput outputConfig)
        {
            List<string> explicitlyNulled = new() { "number", "boolean", "any" };
            return propertyName.EndsWith("?")
                || (outputConfig.AddInferredNullables && !explicitlyNulled.Contains(propertyName));
        }

        public void PreProcess(CSharpFiles files)
        {
            return;
        }
    }
}