using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WCKDRZR.Gaspar;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Converters
{
    internal class TypeScriptConverter : IConverter
	{
        public Configuration Config { get; set; }

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
        public string simpleCollectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)<([\w\d]+)>\??$";
        public string collectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)<(.+)>\??$";
        public string simpleDictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|SortedDictionary|IReadOnlyDictionary)<([\w\d]+)\s*,\s*([\w\d]+)>\??$";
        public string dictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|SortedDictionary|IReadOnlyDictionary)<([\w\d]+)\s*,\s*(.+)>\??$";


        public TypeScriptConverter(Configuration config)
        {
            Config = config;
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"//{comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> ConvertModel(Model model)
        {
            List<string> lines = new();

            if (model.Enumerations != null)
            {
                lines = ConvertEnum(new EnumModel { Identifier = model.ModelName, Values = model.Enumerations });
                model.ModelName += "_Properties";
                if (model.BaseClasses != null)
                {
                    int enumBaseIndex = model.BaseClasses.IndexOf("Enumeration");
                    model.BaseClasses.RemoveAt(enumBaseIndex);
                }
            }

            string indexSignature = null;
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

            if (model.Enumerations != null)
            {
                lines.Add($"    id: number;");
                lines.Add($"    name: string;");
            }

            if (indexSignature != null)
            {
                lines.Add($"    {ConvertIndexType(indexSignature)};");
            }

            foreach (Property member in model.Fields.Concat(model.Properties))
            {
                lines.Add($"    {ConvertProperty(member)};");
            }

            lines.Add("}\n");

            return lines;
        }

        public List<string> ConvertEnum(EnumModel enumModel)
        {
            List<string> lines = new();

            if (Config.Models.StringLiteralTypesInsteadOfEnums)
            {
                lines.Add($"export type {enumModel.Identifier} =");

                int i = 0;
                foreach (KeyValuePair<string, object> value in enumModel.Values)
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
                foreach (KeyValuePair<string, object> value in enumModel.Values)
                {
                    if (Config.Models.NumericEnums)
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
            throw new NotImplementedException();
        }

        public List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes)
        {
            throw new NotImplementedException();
        }

        public List<string> ControllerFooter()
        {
            throw new NotImplementedException();
        }

        public List<string> ConvertController(List<ControllerAction> actions, string outputClassName, ConfigurationTypeOutput outputConfig, bool lastController)
        {
            throw new NotImplementedException();
        }



        public string ConvertProperty(Property property)
        {
            bool optional = property.Type.EndsWith("?");
            string identifier = ConvertIdentifier(optional ? $"{property.Identifier.Split(' ')[0]}?" : property.Identifier.Split(" ")[0]);

            string type = ParseType(property.Type);

            return $"{identifier}: {type}";
        }

        public string ConvertIndexType(string indexType)
        {
            MatchCollection dictionary = Regex.Matches(indexType, dictionaryRegex);
            MatchCollection simpleDictionary = Regex.Matches(indexType, simpleDictionaryRegex);

            string propType = simpleDictionary.HasMatch() ? dictionary.At(2) : ParseType(dictionary.At(2));

            return $"[key: {ConvertType(dictionary.At(1))}]: {ConvertType(propType)}";
        }

        public string ConvertRecord(string record)
        {
            MatchCollection dictionary = Regex.Matches(record, dictionaryRegex);
            MatchCollection simpleDictionary = Regex.Matches(record, simpleDictionaryRegex);

            string propType = simpleDictionary.HasMatch() ? dictionary.At(2) : ParseType(dictionary.At(2));

            return $"Record<{ConvertType(dictionary.At(1))}, {ConvertType(propType)}>";
        }

        public string ConvertIdentifier(string identifier) => JsonNamingPolicy.CamelCase.ConvertName(identifier);

        public string GetEnumStringValue(string value) => JsonNamingPolicy.CamelCase.ConvertName(value);

        public string ParseType(TypeSyntax propType)
        {
            return ParseType(propType.ToString());
        }
        public string ParseType(string propType)
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

            string type;

            if (collection.HasMatch())
            {
                MatchCollection simpleCollection = Regex.Matches(propType, simpleCollectionRegex);
                propType = simpleCollection.HasMatch() ? collection.At(1) : ParseType(collection.At(1));
                type = $"{ConvertType(propType)}[]";
            }
            else if (dictionary.HasMatch())
            {
                type = $"{ConvertRecord(propType)}";
            }
            else
            {
                bool optional = propType.EndsWith("?");
                type = ConvertType(optional ? propType[0..^1] : propType);
                if (type == "string" && !isArray)
                {
                    type = "string | null";
                }
            }

            return isArray ? $"{type}[]" : type;
        }

    }
}