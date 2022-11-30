using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WCKDRZR.Gaspar;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualBasic;
using WCKDRZR.Gaspar.Helpers;

namespace WCKDRZR.Gaspar.Converters
{
    internal class ProtoConverter : IConverter
	{
        public Configuration Config { get; set; }
        
        public Dictionary<string, string> DefaultTypeTranslations = new() {
            { "string", "string" },
            { "String", "string" },
            { "double", "double" },
            { "float", "float" },
            { "int", "int32" },
            { "long", "int64" },
            { "uint", "uint32" },
            { "ulong", "uint64" },
            { "bool", "bool" }
        };
        public Dictionary<string, string> TypeTranslations => DefaultTypeTranslations.Union(Config.CustomTypeTranslations ?? new()).ToDictionary(k => k.Key, v => v.Value);
        public string ConvertType(string type) => TypeTranslations.ContainsKey(type) ? TypeTranslations[type] : type;

        public string arrayRegex = /*language=regex*/ @"^(.+)\[\]$";
        public string simpleCollectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)<([\w\d]+)>\??$";
        public string collectionRegex = /*language=regex*/ @"^(?:I?List|IReadOnlyList|IEnumerable|ICollection|IReadOnlyCollection|HashSet)<(.+)>\??$";
        public string simpleDictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|SortedDictionary|IReadOnlyDictionary)<([\w\d]+)\s*,\s*([\w\d]+)>\??$";
        public string dictionaryRegex = /*language=regex*/ @"^(?:I?Dictionary|SortedDictionary|IReadOnlyDictionary)<([\w\d]+)\s*,\s*(.+)>\??$";

        private Dictionary<string, HashSet<string>> InterfaceImplementations = new Dictionary<string, HashSet<string>>();

        public ProtoConverter(Configuration config)
        {
            Config = config;
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"//{comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> ModelHeader(ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new();
            lines.Add("syntax = \"proto3\";");
            if(!string.IsNullOrEmpty(outputConfig.PackageNamespace))
            {
                lines.Add($"package {outputConfig.PackageNamespace};");
            }
            lines.Add("");
            return lines;
        }

        public List<string> ConvertModels(List<Model> models, ConfigurationTypeOutput outputConfig)
        {
            // lines to return for building the .proto file
            List<string> lines = new List<string>();

            foreach(Model model in models)
            {
                lines.AddRange(this.ConvertModel(model, outputConfig));
            }
            return lines;
        }

        public List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new();

            if (model.Enumerations != null)
            {
                throw new NotImplementedException();
            }

            lines.Add($"message {model.ModelName} {{");

            int count = 1;
            foreach (Property property in model.Fields.Concat(model.Properties))
            {
                lines.Add($"    {ConvertProperty(property, count++)};");
            }
            if(InterfaceImplementations.ContainsKey(model.ModelName))
            {
                
                lines.Add($"    oneof subtype {{");
                foreach(string implementingClass in InterfaceImplementations[model.ModelName])
                {
                    lines.Add($"      {implementingClass} {implementingClass} = {count++};");
                }
                lines.Add("    }");
            }

            lines.Add("}\n");

            return lines;
        }

        public List<string> ConvertEnum(EnumModel enumModel)
        {
            List<string> lines = new();

            if (Config.Models.StringLiteralTypesInsteadOfEnums)
            {
                throw new NotSupportedException("String literals instead of enums is not supported in proto3.");
            }
            else
            {
                lines.Add($"enum {enumModel.Identifier} {{");

                int i = 0;
                foreach (KeyValuePair<string, object> value in enumModel.Values)
                {
                    if (Config.Models.NumericEnums)
                    {
                        // include enum identifier as prefix since proto3 enforces uniqueness across enums
                        lines.Add($"    {enumModel.Identifier}_{value.Key} = {(value.Value != null ? value.Value : i)};");
                    }
                    else
                    {
                        throw new NotSupportedException("Not numeric enums not supported in proto3");
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



        public string ConvertProperty(Property property, int count)
        {
            string identifier = ConvertIdentifier(property.Identifier.Split(" ")[0]);

            string type = ParseType(property.Type);

            return $"{type} {identifier} = {count}";
        }

        public static string ConvertIdentifier(string identifier) => JsonNamingPolicy.CamelCase.ConvertName(identifier);

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

            MatchCollection array = Regex.Matches(propType, arrayRegex);
            MatchCollection collection = Regex.Matches(propType, collectionRegex);
            if (array.HasMatch())
            {
                propType = array.At(1);
                return $"repeated {propType}";
            } else if (collection.HasMatch())
            {
                MatchCollection simpleCollection = Regex.Matches(propType, simpleCollectionRegex);
                propType = simpleCollection.HasMatch() ? collection.At(1) : ParseType(collection.At(1));
                return $"repeated {propType}";
            }

            MatchCollection dictionary = Regex.Matches(propType, dictionaryRegex);

            if (dictionary.HasMatch())
            {
                string key = dictionary.At(1);
                string value = dictionary.At(2);
                return $"map<{key}, {value}>";
            }
            return ConvertType(propType); ;
        }

        public void PreProcess(CSharpFiles files)
        {
            foreach (CSharpFile file in files)
            {
                if (file.HasModels)
                {
                    // extract implementations of each interface to be used later
                    List<Model> interfaces = file.Models.Where(m => m.IsInterface).ToList();
                    foreach (Model model in file.Models.Except(interfaces))
                    {
                        // populate our interfaceImplementation map
                        foreach (string baseClass in model.BaseClasses)
                        {
                            if (!InterfaceImplementations.ContainsKey(baseClass))
                            {
                                InterfaceImplementations.Add(baseClass, new HashSet<string>());
                            }
                            InterfaceImplementations[baseClass].Add(model.ModelName);
                        }
                    }
                }
            }
        }
    }
}