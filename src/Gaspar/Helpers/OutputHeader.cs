﻿using System.Collections.Generic;
using WCKDRZR.Gaspar.Models;
using WCKDRZR.Gaspar.Converters;

namespace WCKDRZR.Gaspar.Helpers
{
    internal static class OutputHeader
	{
        public static List<string> Models(IConverter converter, ConfigurationTypeOutput outputConfig, string outputPath)
        {
            List<string> lines = new();
            if (converter.Config.Models != null)
            {
                lines.AddRange(Header(converter, converter.Config.Models, outputConfig, "models and enums", outputPath));
            }
            return lines;
        }

        public static List<string> Controllers(IConverter converter, ConfigurationTypeOutput outputConfig, string outputPath)
        {
            if (converter.Config.Controllers != null)
            {
                return Header(converter, converter.Config.Controllers, outputConfig, "controllers", outputPath);
            }
            return new();
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
            lines.Add(converter.Comment("** This file was written by Gaspar"));
            lines.Add(converter.Comment("**"));
            return lines;
        }

        private static List<string> Header(IConverter converter, ConfigurationType configFiles, ConfigurationTypeOutput outputConfig, string fileType, string outputPath)
        {
            List<string> lines = new();

            lines.AddRange(GeneralMessage(converter));
            lines.Add(converter.Comment($"** It contains all {fileType} in:"));
            foreach (string path in configFiles.Include)
            {
                lines.Add(converter.Comment($"**     {FileHelper.RelativePath(outputPath, path)}"));
            }
            lines.Add(converter.Comment($"**     only if attributed: [ExportFor] with GasparType.{outputConfig.Type} or containing group"));
            lines.Add(converter.Comment("**"));
            lines.Add(converter.Comment($"** full configuration in: {FileHelper.RelativePath(outputPath, converter.Config.ConfigFilePath ?? "")}"));
            lines.Add(converter.Comment("**", 1));
            return lines;
        }
    }
}

