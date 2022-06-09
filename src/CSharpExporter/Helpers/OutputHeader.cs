using System;
using System.Collections.Generic;
using System.IO;
using WCKDRZR.CSharpExporter.Models;
using WCKDRZR.CSharpExporter.Converters;
using Ganss.IO;

namespace WCKDRZR.CSharpExporter.Helpers
{
    internal static class OutputHeader
	{
        public static List<string> Models(IConverter converter, string outputPath)
        {
            return Header(converter, converter.Config.Models, "models and enums", outputPath);
        }

        public static List<string> Controllers(IConverter converter, string outputPath)
        {
            return Header(converter, converter.Config.Controllers, "controllers", outputPath);
        }

        public static List<string> ControllerHelper(IConverter converter)
        {
            List<string> lines = new();

            lines.AddRange(GeneralMessage(converter));
            lines.Add(converter.Comment("** It supports the other auto-generated files in this location"));
            lines.Add(converter.Comment("**", 1));
            
            return lines;
        }

        private static List<string> GeneralMessage(IConverter converter)
        {
            List<string> lines = new();
            lines.Add(converter.Comment("**"));
            lines.Add(converter.Comment("** This file was written by a tool"));
            lines.Add(converter.Comment("**"));
            return lines;
        }

        private static List<string> Header(IConverter converter, ConfigurationType configFiles, string fileType, string outputPath)
        {
            List<string> lines = new();

            lines.AddRange(GeneralMessage(converter));
            lines.Add(converter.Comment($"** It contains all {fileType} in:"));
            foreach (string path in configFiles.Include)
            {
                lines.Add(converter.Comment($"**     {FileHelper.RelativePath(outputPath, path)}"));
            }
            if (converter.Config.UseAttribute)
            {
                lines.Add(converter.Comment($"**     only if they are attributed: [{converter.Config.OnlyWhenAttributed}]"));
            }
            lines.Add(converter.Comment("**"));
            lines.Add(converter.Comment($"** full configuration in: {FileHelper.RelativePath(outputPath, converter.Config.ConfigFilePath)}"));
            lines.Add(converter.Comment("**", 1));

            return lines;
        }
    }
}

