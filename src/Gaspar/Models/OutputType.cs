using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.Gaspar.Extensions;

namespace WCKDRZR.Gaspar.Models
{
    // Internal version of GasparType
    // Kept separate to keep as Enum where GasparType needs to be an int so it can be extended with custom groups
    // and GasparType has and All flag that isn't appropriate here
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum OutputType
    {
        CSharp = 1 << 0,
        Angular = 1 << 1,
        Ocelot = 1 << 2,
        TypeScript = 1 << 3,
        Proto = 1 << 4,
        Python = 1 << 5,
        Swift = 1 << 6,
        Kotlin = 1 << 7,
    }

    internal static class OutputTypeConverter
    {
        public static OutputType GetExportType(this ParameterSyntax node, Configuration config, OutputType parentTypes = 0)
        {
            OutputType types = 0;
            if (node.Parent != null && node.Parent.GetType() == typeof(ClassDeclarationSyntax))
            {
                types = ((ClassDeclarationSyntax)node.Parent).GetParentExportTypes(config);
            }
            return node.AttributeLists.GetExportType(config, types);
        }

        public static OutputType GetExportType(this MemberDeclarationSyntax node, Configuration config, OutputType parentTypes = 0)
        {
            OutputType types = 0;
            if (node.Parent != null && node.Parent.GetType() == typeof(ClassDeclarationSyntax))
            {
                types = ((ClassDeclarationSyntax)node.Parent).GetParentExportTypes(config);
            }
            return node.AttributeLists.GetExportType(config, types);
        }

        private static OutputType GetParentExportTypes(this ClassDeclarationSyntax node, Configuration config)
        {
            Stack<ClassDeclarationSyntax> classHierarchy = new();
            classHierarchy.Push(node);

            SyntaxNode? parent = node.Parent;
            while (parent != null)
            {
                if (parent.GetType() == typeof(ClassDeclarationSyntax))
                {
                    classHierarchy.Push((ClassDeclarationSyntax)parent);
                    parent = parent.Parent;
                }
                else
                {
                    parent = null;
                }
            }

            OutputType types = 0;
            while (classHierarchy.Count > 0)
            {
                types = classHierarchy.Pop().AttributeLists.GetExportType(config, types);
            }

            return types;
        }

        private static OutputType GetExportType(this SyntaxList<AttributeListSyntax> attributes, Configuration config, OutputType parentTypes = 0)
        {
            OutputType exportForTypes = parentTypes;

            string? exportForArgument = attributes.GetAttribute("ExportFor")?.ArgumentList?.Arguments[0].ToString();
            if (exportForArgument != null)
            {
                foreach (string argPart in Regex.Split(exportForArgument, "[|&]"))
                {
                    string type = argPart.Trim();

                    if (type.StartsWith('"') && type.EndsWith('"')) { type = $"GasparType.{type[1..^1]}"; }

                    bool not = type.StartsWith("~");
                    if (not) { type = type[1..]; }
                    
                    int pos = type.IndexOf(".");
                    if (pos >= 0)
                    {
                        type = type[(pos + 1)..];
                        foreach (OutputType outputType in Enum.GetValues(typeof(OutputType)))
                        {
                            if (type == nameof(GasparType.All) 
                                || (config.GroupTypes != null && config.GroupTypes.ContainsKey(type) && config.GroupTypes[type].Contains(outputType.ToString()))
                                || outputType.ToString() == type
                            ) {
                                if (not)
                                {
                                    if (exportForTypes.HasFlag(outputType))
                                    {
                                        exportForTypes ^= outputType;
                                    }
                                }
                                else
                                {
                                    exportForTypes |= outputType;
                                }
                            }
                        }
                    }
                }
            }

            return exportForTypes;
        }
    }
}