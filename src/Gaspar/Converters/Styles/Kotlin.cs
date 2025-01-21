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
            { "IFormFile", "ByteArray" },
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
            string baseClasses = model.BaseClasses.Count > 0 ? $": {string.Join(", ", model.BaseClasses)}()" : "";

            lines.Add($"{Indent()}@Serializable");
            lines.Add($"{Indent()}open class {model.ModelName}{(model.Enumerations.Count > 0 ? "_Properties" : "")}{baseClasses} {{");

            if (model.Enumerations.Count > 0)
            {
                lines.Add($"{Indent(1)}var id: Int = 0");
                lines.Add($"{Indent(1)}lateinit var name: String");
            }

            if (indexSignature != null)
            {
                lines.Add($"{Indent(1)}{ConvertIndexType(indexSignature, outputConfig)};");
            }

            foreach (Property member in model.Fields.Concat(model.Properties))
            {
                if (member.JsonPropertyName != null)
                {
                    lines.Add($"{Indent(1)}@SerialName(\"{member.JsonPropertyName}\")");    
                }
                
                string property = ConvertProperty(member, outputConfig);
                bool lateinit = LateInit(member, outputConfig);
                string defaultValue = "";
                if (!lateinit || member.DefaultValue != null)
                {
                    defaultValue = property.EndsWith('?') ? "null" : DefaultValue(member, outputConfig);
                    if (defaultValue != "") { defaultValue = $" = {defaultValue}"; }
                }

                lines.Add($"{Indent(1)}{(lateinit ? "lateinit var" : "var")} {property}{defaultValue}");
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

            if (valueType != null)
            {
                lines.Add($"@Serializable(with = {enumModel.Identifier}Serializer::class)");
            }
            lines.Add($"enum class {enumModel.Identifier}{(valueType != null ? $"(val value: {valueType})" : "")} {{");

            foreach (KeyValuePair<string, object?> value in enumModel.Values)
            {
                if (valueType != null)
                {
                    lines.Add($"    {value.Key}({(numeric ? $"{value.Value}f" : $"\"{value.Value}\"")}),");
                }
                else
                {
                    lines.Add($"    {value.Key},");
                }
            }
            lines.Add("}");

            if (valueType != null)
            {
                lines.Add($"object {enumModel.Identifier}Serializer : kotlinx.serialization.KSerializer<{enumModel.Identifier}> {{");
                lines.Add($"    override val descriptor: kotlinx.serialization.descriptors.SerialDescriptor = kotlinx.serialization.descriptors.PrimitiveSerialDescriptor(\"{enumModel.Identifier}Serializer\", kotlinx.serialization.descriptors.PrimitiveKind.{valueType.ToUpper()})");
                lines.Add($"    override fun deserialize(decoder: kotlinx.serialization.encoding.Decoder): {enumModel.Identifier} {{");
                lines.Add($"        val v = decoder.decode{valueType}()");
                lines.Add($"        return {enumModel.Identifier}.entries.first {{ it.value == v }}");
                lines.Add($"    }}");
                lines.Add($"    override fun serialize(encoder: kotlinx.serialization.encoding.Encoder, value: {enumModel.Identifier}) {{ return encoder.encodeString(value.name) }}");
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

        public List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            return new();
        }

        public List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes)
        {
            return new();
        }

        public List<string> ControllerFooter()
        {
            return new();
        }

        public List<string> ConvertController(List<ControllerAction> actions, string outputClassName, ConfigurationTypeOutput outputConfig, bool lastController)
        {
            return new();
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

        private bool LateInit(Property property, ConfigurationTypeOutput outputConfig)
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
            return true;
        }

        private string DefaultValue(Property property, ConfigurationTypeOutput outputConfig)
        {
            if (property.DefaultValue != null && !property.DefaultValue.StartsWith("new") && property.DefaultValue != "[]" && !property.DefaultValue.EndsWith("()"))
            {
                return property.DefaultValue;
            }

            string? type = property.Type != null ? ParseType(property.Type, outputConfig) : null;
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