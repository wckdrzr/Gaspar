using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.Gaspar.Extensions;

namespace WCKDRZR.Gaspar.Models
{
    internal class OutputTypeGroupAttribute : Attribute
    {
        public GasparType Types { get; set; }
        public OutputTypeGroupAttribute(GasparType types)
        {
            Types = types;
        }
    }

    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum OutputType
    {
        [OutputTypeGroup(GasparType.All | GasparType.CSharp)]
        CSharp = 1 << 0,

        [OutputTypeGroup(GasparType.All | GasparType.FrontEnd | GasparType.Angular)]
        Angular = 1 << 1,

        [OutputTypeGroup(GasparType.All | GasparType.FrontEnd | GasparType.Ocelot)]
        Ocelot = 1 << 2,

        [OutputTypeGroup(GasparType.All | GasparType.FrontEnd | GasparType.TypeScript)]
        TypeScript = 1 << 3,

        [OutputTypeGroup(GasparType.All | GasparType.Proto)]
        Proto = 1 << 4,

        [OutputTypeGroup(GasparType.All | GasparType.Python)]
        Python = 1 << 5,
    }

    internal static class OutputTypeConverter
    {
        public static OutputType GetExportType(this ParameterSyntax node, OutputType parentTypes = 0)
        {
            OutputType types = 0;
            if (node.Parent != null && node.Parent.GetType() == typeof(ClassDeclarationSyntax))
            {
                types = ((ClassDeclarationSyntax)node.Parent).GetParentExportTypes();
            }
            return node.AttributeLists.GetExportType(types);
        }

        public static OutputType GetExportType(this MemberDeclarationSyntax node, OutputType parentTypes = 0)
        {
            OutputType types = 0;
            if (node.Parent != null && node.Parent.GetType() == typeof(ClassDeclarationSyntax))
            {
                types = ((ClassDeclarationSyntax)node.Parent).GetParentExportTypes();
            }
            return node.AttributeLists.GetExportType(types);
        }

        private static OutputType GetParentExportTypes(this ClassDeclarationSyntax node)
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
                types = classHierarchy.Pop().AttributeLists.GetExportType(types);
            }

            return types;
        }

        private static OutputType GetExportType(this SyntaxList<AttributeListSyntax> attributes, OutputType parentTypes = 0)
        {
            OutputType exportForTypes = parentTypes;

            string? exportForArgument = attributes.GetAttribute("ExportFor")?.ArgumentList?.Arguments[0].ToString();
            if (exportForArgument != null)
            {
                foreach (string type in Regex.Split(exportForArgument, "[|&]"))
                {
                    bool not = type.Trim().StartsWith("~");
                    int pos = type.IndexOf(".");
                    if (pos >= 0)
                    {
                        if (Enum.TryParse<GasparType>(type.Substring(pos + 1).Trim(), out GasparType parsedEnum))
                        {
                            foreach (OutputType outputType in Enum.GetValues(typeof(OutputType)))
                            {
                                if (outputType.GetAttributeOfType<OutputTypeGroupAttribute>()?.Types.HasFlag(parsedEnum) == true)
                                {
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
            }

            return exportForTypes;
        }
    }
}