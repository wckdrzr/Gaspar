using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.CSharpExporter.Extensions;

namespace WCKDRZR.CSharpExporter.Models
{
    internal class OutputTypeGroupAttribute : Attribute
    {
        public CSharpExportType Types { get; set; }
        public OutputTypeGroupAttribute(CSharpExportType types)
        {
            Types = types;
        }
    }

    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum OutputType
    {
        [OutputTypeGroup(CSharpExportType.All | CSharpExportType.CSharp)]
        CSharp = 1 << 0,

        [OutputTypeGroup(CSharpExportType.All | CSharpExportType.FrontEnd | CSharpExportType.Angular)]
        Angular = 1 << 1,

        [OutputTypeGroup(CSharpExportType.All | CSharpExportType.FrontEnd | CSharpExportType.Ocelot)]
        Ocelot = 1 << 2,

        [OutputTypeGroup(CSharpExportType.All | CSharpExportType.FrontEnd | CSharpExportType.TypeScript)]
        TypeScript = 1 << 3
    }

    internal static class OutputTypeConverter
    {
        public static OutputType GetExportType(this MemberDeclarationSyntax node, OutputType parentTypes = 0)
        {
            OutputType exportForTypes = parentTypes;

            string exportForArgument = node.AttributeLists.GetAttribute("ExportFor")?.ArgumentList.Arguments[0].ToString();
            if (exportForArgument != null)
            {
                foreach (string type in Regex.Split(exportForArgument, "[|&]"))
                {
                    bool not = type.Trim().StartsWith("~");
                    int pos = type.IndexOf(".");
                    if (pos >= 0)
                    {
                        if (Enum.TryParse<CSharpExportType>(type.Substring(pos + 1).Trim(), out CSharpExportType parsedEnum))
                        {
                            foreach (OutputType outputType in Enum.GetValues(typeof(OutputType)))
                            {
                                if (outputType.GetAttributeOfType<OutputTypeGroupAttribute>().Types.HasFlag(parsedEnum))
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