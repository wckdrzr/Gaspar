using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.Gaspar.Helpers;

namespace WCKDRZR.Gaspar.Converters
{   
    internal class KotlinConverter : IConverter
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
            { "Int32", "Int" },
            { "Int64", "Long" },
            { "short", "Short" },
            { "ushort", "UShort" },
            { "long", "Long" },
            { "ulong", "ULong" },
            { "decimal", "Double" },
            { "DateTime", "String" },
            { "DateOnly", "String" },
            { "TimeOnly", "String" },
            { "DateTimeOffset", "String" },
            { "bool", "Boolean" },
            { "DataTable", "Object" },
            { "Guid", "String" },
            { "byte[]", "ByteArray" },
            { "ContentResult", "String" },
            { "IFormFile", "File" },
            { "dynamic", "kotlin.Any" },
            { "object", "kotlin.Any" },
            { "JsonResult", "kotlin.Any" },
        };
        public Dictionary<string, string> KotlinTypeTranslations => Config.TypeTranslations != null && Config.TypeTranslations.ContainsKey(OutputType.Kotlin.ToString()) ? Config.TypeTranslations[OutputType.Kotlin.ToString()] : new();
        public Dictionary<string, string> TypeTranslations => DefaultTypeTranslations.Union(KotlinTypeTranslations).Union(Config.GlobalTypeTranslations ?? new()).ToDictionary(k => k.Key, v => v.Value);
        public string ConvertType(string type) => TypeTranslations.ContainsKey(type) ? TypeTranslations[type] : type;

        public string arrayRegex = /*language=regex*/ @"^(.+)\[\]$";
        public string simpleCollectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)\s*<([\w\d]+)>\??$";
        public string collectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)\s*<(.+)>\??$";
        public string simpleDictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|OrderedDictionary|SortedDictionary|IReadOnlyDictionary)\s*<([\w\d]+)\s*,\s*([\w\d]+)>\??$";
        public string dictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|OrderedDictionary|SortedDictionary|IReadOnlyDictionary)\s*<([\w\d]+)\s*,\s*(.+)>\??$";
        public string keyValuePairRegex = /*language=regex*/ @"^KeyValuePair<([\w\d]+)\s*,\s*(.+)>\??$";

        public KotlinConverter(Configuration config)
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
                $"package {outputConfig.PackageNamespace}",
                $"",
                $"import kotlinx.serialization.SerialName",
                $"import kotlinx.serialization.Serializable",
                $""
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
                        lines.Add($"{Indent()}open class {ns[i]} {{\n");
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

            List<EnumModel> allEnums = allFiles.EnumsForType(OutputType.Kotlin);
            List<Model> allInterfaces = allFiles.InterfacesForType(OutputType.Kotlin);

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
            List<string> baseClassStrings = [];
            List<string> classInterfaceMembers = [];
            if (model.BaseClasses.Count > 0)
            {
                indexSignature = model.BaseClasses.FirstOrDefault(type => Regex.Matches(type, dictionaryRegex).HasMatch());
                model.BaseClasses = model.BaseClasses.Where(type => !Regex.Matches(type, dictionaryRegex).HasMatch()).ToList();
                for (int i = 0; i < model.BaseClasses.Count; i++)
                {
                    string type = ConvertType(model.BaseClasses[i]);
                    Model? interfaceModel = allInterfaces.FirstOrDefault(m => ConvertType(m.FullName) == type);
                    baseClassStrings.Add($"{type}{(interfaceModel != null ? "" : "()")}");

                    if (interfaceModel != null)
                    {
                        classInterfaceMembers.AddRange(interfaceModel.Properties.Select(p => ConvertProperty(p, outputConfig)));
                    }
                }
            }
            string baseClasses = baseClassStrings.Count > 0 ? $": {string.Join(", ", baseClassStrings)}" : "";

            string classType = model.IsInterface ? "interface" : "open class";
            if (!model.IsInterface) { lines.Add($"{Indent()}@Serializable"); }
            lines.Add($"{Indent()}{classType} {model.ModelName}{(model.Enumerations.Count > 0 ? "_Properties" : "")}{baseClasses} {{");

            if (model.Enumerations.Count > 0)
            {
                lines.Add($"{Indent(1)}var id: Int = 0");
                lines.Add($"{Indent(1)}lateinit var name: String");
            }

            if (indexSignature != null)
            {
                lines.Add($"{Indent(1)}{ConvertIndexType(indexSignature, outputConfig)}");
            }

            foreach (Property member in model.Fields.Concat(model.Properties))
            {
                if (member.JsonPropertyName != null)
                {
                    lines.Add($"{Indent(1)}@SerialName(\"{member.JsonPropertyName}\")");    
                }
                
                string property = ConvertProperty(member, outputConfig);
                bool lateinit = !model.IsInterface && LateInit(member, outputConfig, allEnums);
                string defaultValue = "";
                if (!model.IsInterface && (!lateinit || member.DefaultValue != null))
                {
                    defaultValue = property.EndsWith('?') ? "null" : DefaultValue(member, outputConfig, allEnums);
                    if (defaultValue != "") { defaultValue = $" = {defaultValue}"; }
                }

                string @override = classInterfaceMembers.Contains(property) ? "override " : "";
                lines.Add($"{Indent(1)}{@override}{(lateinit ? "lateinit var" : "var")} {property}{defaultValue}");
            }

            previousModelClass = model.FullName;

            bool closeClass = true;
            int modelIndex = file.Models.FindIndex(m => m.FullName == model.FullName);
            if (modelIndex >= 0)
            {
                int nextModel = file.Models.FindIndex(modelIndex + 1, m => m.ExportFor.HasFlag(OutputType.Kotlin));
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
            
            bool numeric = enumModel.Values.Any(e => double.TryParse(e.Value?.ToString(), out _));
            string? valueType = enumModel.Values.Any(e => e.Value != null) ? (numeric ? "Float" : "String") : null;

            if (valueType != null || Config.Models?.UseEnumValue == true)
            {
                lines.Add($"@Serializable(with = {enumModel.Identifier}Serializer::class)");
            }
            lines.Add($"enum class {enumModel.Identifier}{(valueType != null ? $"(val value: {valueType})" : (Config.Models?.UseEnumValue == true ? $"(val value: Float)" : ""))} {{");

            int i = 0;
            foreach (KeyValuePair<string, object?> value in enumModel.Values)
            {
                if (valueType != null)
                {
                    lines.Add($"    {value.Key}({(numeric ? $"{value.Value}f" : $"\"{value.Value}\"")}),");
                }
                else if (Config.Models?.UseEnumValue == true)
                {
                    lines.Add($"    {value.Key}({i}f),");
                    i++;
                }
                else
                {
                    lines.Add($"    {value.Key},");
                }
            }
            lines.Add("}");

            if (valueType == null && Config.Models?.UseEnumValue == true)
            {
                valueType = "Float";
            }

            if (valueType != null)
            {
                lines.Add($"object {enumModel.Identifier}Serializer : kotlinx.serialization.KSerializer<{enumModel.Identifier}> {{");
                lines.Add($"    override val descriptor: kotlinx.serialization.descriptors.SerialDescriptor = kotlinx.serialization.descriptors.PrimitiveSerialDescriptor(\"{enumModel.Identifier}Serializer\", kotlinx.serialization.descriptors.PrimitiveKind.{valueType.ToUpper()})");
                lines.Add($"    override fun deserialize(decoder: kotlinx.serialization.encoding.Decoder): {enumModel.Identifier} {{");
                lines.Add($"        val v = decoder.decode{valueType}()");
                lines.Add($"        return {enumModel.Identifier}.entries.first {{ it.value == v }}");
                lines.Add($"    }}");
                lines.Add($"    override fun serialize(encoder: kotlinx.serialization.encoding.Encoder, value: {enumModel.Identifier}) {{ return encoder.encode{valueType}(value.value) }}");
                lines.Add($"}}");
            }

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

        private List<string> ControllerPackages(ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = [
                "import android.util.Log",
                "import kotlinx.serialization.Serializable",
                "import okhttp3.Headers",
                "import okhttp3.OkHttpClient",
                "import okhttp3.Request",
                "import okhttp3.MultipartBody",
                "import okhttp3.RequestBody",
                "import okhttp3.Response",
                "import okhttp3.MediaType.Companion.toMediaTypeOrNull",
                "import okhttp3.RequestBody.Companion.toRequestBody",
                "import java.time.Duration",
                "import kotlinx.serialization.ExperimentalSerializationApi",
                "import kotlinx.serialization.json.Json",
                "import kotlinx.serialization.serializer",
                "import kotlinx.coroutines.Dispatchers",
                "import kotlinx.coroutines.withContext",
            ];
            if (outputConfig.ModelPath != null) {
                lines.Add($"import {outputConfig.ModelPath}.*");
            }

            lines.Add("");
            return lines;            
        }

        public List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = [
                $"package {outputConfig.PackageNamespace}",
                "",
                ..ControllerPackages(outputConfig)
            ];
            
            lines.Add("@Serializable");
            lines.Add("class ActionResultError {");
            lines.Add("    var detail: String?");
            lines.Add("    var instance: String? = null");
            lines.Add("    var status: Int");
            lines.Add("    var title: String");
            lines.Add("    var traceId: String? = null");
            lines.Add("    var type: String? = null");
            lines.Add("    constructor(title: String, detail: String?, status: Int) {");
            lines.Add("        this.title = title");
            lines.Add("        this.detail = detail");
            lines.Add("        this.status = status");
            lines.Add("    }");
            lines.Add("}");
            lines.Add("@Serializable");
            lines.Add("class ServiceResponse<T> {");
            lines.Add("    var data: T?");
            lines.Add("    var error: ActionResultError?");
            lines.Add("    val success: Boolean get() { return error == null }");
            lines.Add("    val hasError: Boolean get() { return error != null }");
            lines.Add("    constructor(data: T?, error: ActionResultError?) {");
            lines.Add("        this.data = data");
            lines.Add("        this.error = error");
            lines.Add("    }");
            lines.Add("}");
            lines.Add("@Serializable class VoidObject");
            lines.Add("@Serializable class File(val name: String, val data: ByteArray)");
            lines.Add("");
            lines.Add("class GasparServiceHelper {");
            lines.Add("    private val client: OkHttpClient = OkHttpClient.Builder()");
            lines.Add("        .callTimeout(Duration.ofSeconds(30))");
            lines.Add("        .build()");
            lines.Add("    fun fetchVoid(method: String, url: String, body: Any? = null, headers: Headers? = null): ServiceResponse<VoidObject> {");
            lines.Add("        return fetch<VoidObject>(method, url, body, headers)");
            lines.Add("    }");
            lines.Add("    inline fun <reified T> fetch(method: String, url: String, body: Any? = null, headers: Headers? = null): ServiceResponse<T> {");
            lines.Add("        try {");
            lines.Add("            val response = load(method, url, body, headers)");
            lines.Add("            response.use {");
            lines.Add("                return if (it.code in 200..299) {");
            lines.Add("                    success(it, url)");
            lines.Add("                } else {");
            lines.Add("                    serverError(it, url)");
            lines.Add("                }");
            lines.Add("            }");
            lines.Add("        } catch (e: Exception) {");
            lines.Add("            return loadException(e, url)");
            lines.Add("        }");
            lines.Add("    }");
            lines.Add("    @OptIn(ExperimentalSerializationApi::class)");
            lines.Add("    @PublishedApi internal fun load(method: String, url: String, body: Any?, headers: Headers?): Response {");
            lines.Add("        val jsonMediaType = \"application/json; charset=utf-8\".toMediaTypeOrNull()");
            lines.Add("        var bodyData: RequestBody? = null");
            lines.Add("        body?.let { body ->");
            lines.Add("            bodyData = when (body) {");
            lines.Add("                is RequestBody -> body");
            lines.Add("                is String -> Json.encodeToString(body).toRequestBody(jsonMediaType)");
            lines.Add("                else -> {");
            lines.Add("                    try {");
            lines.Add("                        val bodySerializer = serializer(body::class, emptyList(), false)");
            lines.Add("                        Json.encodeToString(bodySerializer, body).toRequestBody(jsonMediaType)");
            lines.Add("                    } catch (_: Exception) {");
            lines.Add("                        body.toString().toRequestBody(jsonMediaType)");
            lines.Add("                    }");
            lines.Add("                }");
            lines.Add("            }");
            lines.Add("        }");
            lines.Add("        if (bodyData == null && method in setOf(\"POST\", \"PUT\", \"PATCH\")) {");
            lines.Add("            bodyData = ByteArray(0).toRequestBody(null)");
            lines.Add("        }");
            lines.Add("        val request = Request.Builder()");
            lines.Add("            .method(method, bodyData)");
            lines.Add("            .url(url)");
            lines.Add("            .headers(headers ?: Headers.Builder().build())");
            lines.Add("            .build()");
            lines.Add("        return client.newCall(request).execute()");
            lines.Add("    }");
            lines.Add("    @PublishedApi internal inline fun <reified T> success(httpResponse: Response, url: String): ServiceResponse<T> {");
            lines.Add("        try {");
            lines.Add("            val responseBody = httpResponse.body.string()");
            lines.Add("            if (T::class == String::class) {");
            lines.Add("                return ServiceResponse(responseBody as? T, null)");
            lines.Add("            }");
            lines.Add("            if (responseBody.isBlank()) {");
            lines.Add("                return ServiceResponse(null, null)");
            lines.Add("            }");
            lines.Add("            val response = Json.decodeFromString<T>(responseBody)");
            lines.Add("            return ServiceResponse(");
            lines.Add("                response,");
            lines.Add("                null");
            lines.Add("            )");
            lines.Add("        } catch (e: Exception) {");
            lines.Add("            logMessage(\"Gaspar: Unable to deserialize successful response from $url\\n${e.message}\")");
            lines.Add("            return error(\"Gaspar: Unable to deserialize successful response from $url\", e.message, 0)");
            lines.Add("        }");
            lines.Add("    }");
            lines.Add("    @PublishedApi internal fun <T> serverError(httpResponse: Response, url: String): ServiceResponse<T> {");
            lines.Add("        var response = ServiceResponse<T>(null, null)");
            lines.Add("        val responseBody = httpResponse.body.string()");
            lines.Add("        try {");
            lines.Add("            response.error = Json.decodeFromString<ActionResultError>(responseBody)");
            lines.Add("        } catch (_: Exception) {");
            lines.Add("            response = error(\"Gaspar: Service call to $url failed to connect\", responseBody, httpResponse.code)");
            lines.Add("        }");
            lines.Add("        logMessage(\"Gaspar: Service call to $url failed with status code ${httpResponse.code}\" +");
            lines.Add("            (if (response.error != null && response.error?.detail?.isNotEmpty() == true) \"\\n${response.error?.detail ?: \"\"}\" else \"\"))");
            lines.Add("        return response");
            lines.Add("    }");
            lines.Add("    @PublishedApi internal fun <T> loadException(exception: Exception, url: String): ServiceResponse<T> {");
            lines.Add("        val message = exception.message");
            lines.Add("        logMessage(\"Gaspar: Service call to $url failed to connect\\n$message\")");
            lines.Add("        return error(\"Gaspar: Service call to $url failed to connect\", message, 0)");
            lines.Add("    }");
            lines.Add("    @PublishedApi internal fun <T> error(title: String, detail: String?, status: Int): ServiceResponse<T> {");
            lines.Add("        return ServiceResponse(");
            lines.Add("            null,");
            lines.Add("            ActionResultError(title, detail, status)");
            lines.Add("        )");
            lines.Add("    }");
            lines.Add("    @PublishedApi internal fun logMessage(message: String) {");
            lines.Add("        Log.d(\"Gaspar\", message)");
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
            else
            {
                lines.Add($"package {outputConfig.PackageNamespace}");
                lines.Add("");
                lines.AddRange(ControllerPackages(outputConfig));
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

            lines.Add($"class {outputClassName}Service {{");

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
                    lines.Add($"    @Deprecated(\"{action.BadMethodReason}\", level = DeprecationLevel.ERROR)");
                    lines.Add($"    fun {action.ActionName}({string.Join(", ", parameters)}) {{ }}");
                    lines.Add($"    @Deprecated(\"{action.BadMethodReason}\", level = DeprecationLevel.ERROR)");
                    lines.Add($"    suspend fun {action.ActionName}Async({string.Join(", ", parameters)}) {{ }}");
                }
                else
                {
                    string httpMethod = action.HttpMethod.ToUpper();

                    string url = outputConfig.AddUrlPrefix(action.Route).Replace("{", "${").Replace("{param:", "{");
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
                        parameters.Add($"headers: {action.ActionName}Headers");
                    }

                    string? bodyParameter = action.Parameters.FirstOrDefault(p => p.Source == ParameterSource.Body)?.Identifier;

                    List<string> formParamsBuilder = new();
                    IEnumerable<Parameter> formParameters = action.Parameters.Where(p => p.Source == ParameterSource.Form);
                    if (formParameters.Any())
                    {
                        formParamsBuilder.Add("val data = MultipartBody.Builder()");
                        formParamsBuilder.Add("    .setType(MultipartBody.FORM)");
                        bodyParameter = "data";
                        foreach (Parameter parameter in formParameters)
                        {
                            string type = ConvertType($"{parameter.Type}");
                            if (type.StartsWith("ByteArray"))
                            {
                                formParamsBuilder.Add($"    .addFormDataPart(\"{parameter.Identifier}\", \"{parameter.Identifier}\", {parameter.Identifier}.toRequestBody(\"application/octet-stream\".toMediaTypeOrNull()))");
                            }
                            else if (type.StartsWith("File"))
                            {
                                formParamsBuilder.Add($"    .addFormDataPart(\"{parameter.Identifier}\", {parameter.Identifier}.name, {parameter.Identifier}.data.toRequestBody(\"application/octet-stream\".toMediaTypeOrNull()))");
                            }
                            else
                            {
                                formParamsBuilder.Add($"    .addFormDataPart(\"{parameter.Identifier}\", {parameter.Identifier}{(parameter.Type?.ToString() != "string" ? ".toString()" : "")})");
                            }
                        }
                        formParamsBuilder.Add("    .build()");
                    }

                    List<string> headerParamBuilder = new();
                    string headersParam = "null";
                    IEnumerable<Parameter> headerParameters = action.Parameters.Where(p => p.Source == ParameterSource.Header);
                    if (headerParameters.Any() || action.Headers != null)
                    {
                        headersParam = "headersToSend";
                        headerParamBuilder.Add($"val headersToSend = Headers.Builder()");
                        foreach (string header in action.Headers?.ToList() ?? new())
                        {
                            headerParamBuilder.Add($"    .add(\"{header}\", headers.{header})");
                        }
                        foreach (Parameter parameter in headerParameters)
                        {
                            headerParamBuilder.Add($"    .add(\"{parameter.Identifier}\", {parameter.Identifier}{(parameter.Type?.ToString() != "string" ? ".toString()" : "")})");
                        }
                        headerParamBuilder.Add("    .build()");
                    }

                    List<string> asyncArguments = action.Parameters.Select(p => p.Identifier).ToList();
                    if (outputConfig.UrlPrefix != null)
                    {
                        asyncArguments.AddRange(Regex.Matches(outputConfig.UrlPrefix, "{param:(.*?)}").Select(m => m.Groups[1].Value).ToList());
                    }
                    if (action.Headers != null)
                    {
                        asyncArguments.Add("headers");
                    }

                    if (action.Headers != null)
                    {
                        lines.Add($"    data class {action.ActionName}Headers({string.Join(", ", action.Headers.Select(h => $"val {h}: String"))})");
                    }
                    lines.Add($"    fun {ConvertIdentifier(action.ActionName)}({string.Join(", ", parameters)}): ServiceResponse{returnTypeString} {{");
                    lines.AddRange(formParamsBuilder.Select(f => $"        {f}"));
                    lines.AddRange(headerParamBuilder.Select(f => $"        {f}"));
                    lines.Add($"        return GasparServiceHelper().{fetchMethodName}{returnTypeString}(\"{httpMethod}\", \"{url}\", {bodyParameter ?? "null"}, {headersParam})");
                    lines.Add($"    }}");
                    lines.Add($"    suspend fun {ConvertIdentifier(action.ActionName)}Async({string.Join(", ", parameters)}): ServiceResponse{returnTypeString} {{");
                    lines.Add($"        return withContext(Dispatchers.IO) {{ {ConvertIdentifier(action.ActionName)}({string.Join(", ", asyncArguments)}) }}");
                    lines.Add($"    }}");
                }
            }
            lines.Add($"}}");
            lines.Add("");

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
                if (IsOptional(propType, outputConfig)) { propType += "?"; }
            }
            else
            {
                propType = ConvertType(ParseType(d.At(2), outputConfig));
            }

            return $"kotlin.collections.Map<{ConvertType(d.At(1))}, {propType}>";
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
                type = $"List<{ConvertType(tmpType)}>";
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

        private bool LateInit(Property property, ConfigurationTypeOutput outputConfig, List<EnumModel> allEnums)
        {
            if (property.DefaultValue != null && !property.DefaultValue.StartsWith("new") && property.DefaultValue != "[]")
            {
                return false;
            }

            string? type = property.Type != null ? ParseType(property.Type, outputConfig) : null;
            if (type?.EndsWith('?') == true)
            {
                return false;
            }
            if (type != null && new List<string>() {"Int", "UInt", "Double", "Float", "Long", "ULong", "Short", "UShort", "Boolean"}.Contains(type))
            {
                return false;
            }
            if (allEnums.FirstOrDefault(e => e.Identifier == type) != null)
            {
                return false;
            }
            return true;
        }

        private string DefaultValue(Property property, ConfigurationTypeOutput outputConfig, List<EnumModel> allEnums)
        {
            string? type = property.Type != null ? ParseType(property.Type, outputConfig) : "";
            if (property.DefaultValue != null && !property.DefaultValue.StartsWith("new") && property.DefaultValue != "[]" && !property.DefaultValue.EndsWith("()"))
            {
                return property.DefaultValue;
            }

            EnumModel? matchingEnum = allEnums.FirstOrDefault(e => e.Identifier == type);
            if (matchingEnum != null && matchingEnum.Values.Count > 0)
            {
                return $"{matchingEnum.Identifier}.{matchingEnum.Values.First().Key}";
            }

            switch (type)
            {
                case "String": return "\"\"";
                case "Int": return "0";
                case "UInt": return "0u";
                case "Double": return "0.0";
                case "Float": return "0f";
                case "Long": return "0";
                case "Short": return "0";
                case "UShort": return "0u";
                case "ULong": return "0u";
                case "Boolean": return "false";
            }
            return "";
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