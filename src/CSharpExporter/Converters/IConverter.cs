using System;
using System.Collections.Generic;
using WCKDRZR.CSharpExporter;
using WCKDRZR.CSharpExporter.Helpers;
using WCKDRZR.CSharpExporter.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.Converters
{
    internal interface IConverter
    {
        Configuration Config { get; set; }

        List<string> ConvertModel(Model model);
        List<string> ConvertEnum(EnumModel enumModel);

        List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig);
        List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes);
        List<string> ControllerFooter();
        List<string> ConvertController(Controller controller, ConfigurationTypeOutput outputConfig, bool lastController);

        string Comment(string comment, int followingBlankLines = 0);
    }

    internal class Converter
    {
        private Configuration _config;

        public Converter(Configuration config)
		{
            _config = config;
        }

        private IConverter GetConverter(ConfigurationTypeOutput outputConfig)
        {
            switch (outputConfig.Type)
            {
                case OutputType.Angular: return new AngularConverter(_config);
                case OutputType.CSharp: return new CSharpConverter(_config);
                case OutputType.Ocelot: return new OcelotConverter(_config);
                case OutputType.TypeScript: return new TypeScriptConverter(_config);
                default: throw new NotImplementedException();
            }
        }

        public string BuildModelsFile(ConfigurationTypeOutput outputConfig, CSharpFiles files)
        {
            IConverter converter = GetConverter(outputConfig);
            List<string> lines = new();

            lines.AddRange(OutputHeader.Models(converter, outputConfig.Location));

            foreach (CSharpFile file in files)
            {
                if (file.HasModels)
                {
                    lines.Add(converter.Comment("File: " + FileHelper.RelativePath(outputConfig.Location, file.Path), 1));

                    foreach (Model model in file.Models)
                    {
                        lines.AddRange(converter.ConvertModel(model));
                    }
                    foreach (EnumModel @enum in file.Enums)
                    {
                        lines.AddRange(converter.ConvertEnum(@enum));
                    }
                }
            }

            return string.Join('\n', lines);
        }

        public string BuildControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new();
            IConverter converter = GetConverter(outputConfig);

            lines.AddRange(OutputHeader.ControllerHelper(converter));
            lines.AddRange(converter.ControllerHelperFile(outputConfig));

            return string.Join('\n', lines);
        }

        public string BuildControllersFile(ConfigurationTypeOutput outputConfig, CSharpFiles files)
        {
            IConverter converter = GetConverter(outputConfig);
            List<string> lines = new();

            lines.AddRange(OutputHeader.Controllers(converter, outputConfig.Location));

            files.DeDuplicateControllerNames(_config.Controllers);
            lines.AddRange(converter.ControllerHeader(outputConfig, files.CustomTypes()));

            int fileIterator = 0;
            foreach (CSharpFile file in files)
            {
                if (file.HasControllers)
                {
                    lines.Add(converter.Comment("File: " + FileHelper.RelativePath(outputConfig.Location, file.Path), 1));

                    int controllerIterator = 0;
                    foreach (Controller controller in file.Controllers)
                    {
                        bool lastController = fileIterator == files.Count - 1 && controllerIterator == file.Controllers.Count - 1;
                        lines.AddRange(converter.ConvertController(controller, outputConfig, lastController));
                        controllerIterator++;
                    }
                }

                fileIterator++;
            }

            lines.AddRange(converter.ControllerFooter());

            return string.Join('\n', lines);
        }
    }
}

