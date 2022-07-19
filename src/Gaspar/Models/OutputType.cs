using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
        Proto = 1 << 4
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
                        if (Enum.TryParse<GasparType>(type.Substring(pos + 1).Trim(), out GasparType parsedEnum))
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