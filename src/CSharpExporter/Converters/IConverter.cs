using System;
using System.Collections.Generic;
using WCKDRZR.CSharpExporter;
using WCKDRZR.CSharpExporter.Helpers;
using WCKDRZR.CSharpExporter.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

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
        List<string> ConvertController(List<ControllerAction> Actions, string outputClassName, ConfigurationTypeOutput outputConfig, bool lastController);

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

            lines.AddRange(OutputHeader.Models(converter, outputConfig, outputConfig.Location));

            int modelCount = 0;
            foreach (CSharpFile file in files)
            {
                if (file.HasModels)
                {
                    List<Model> modelsForType = file.ModelsForType(outputConfig.Type);
                    List<EnumModel> enumsForType = file.EnumsForType(outputConfig.Type);

                    if (modelsForType.Count > 0 || enumsForType.Count > 0)
                    {
                        lines.Add(converter.Comment("File: " + FileHelper.RelativePath(outputConfig.Location, file.Path), 1));

                        modelCount += modelsForType.Count + enumsForType.Count;

                        foreach (Model model in modelsForType)
                        {
                            lines.AddRange(converter.ConvertModel(model));
                        }
                        foreach (EnumModel @enum in enumsForType)
                        {
                            lines.AddRange(converter.ConvertEnum(@enum));
                        }
                    }
                }
            }

            if (modelCount == 0)
            {
                lines.Add(converter.Comment($"*** NO MODELS or ENUMS ATTRIBUTED ***"));
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

            lines.AddRange(OutputHeader.Controllers(converter, outputConfig, outputConfig.Location));

            files.DeDuplicateControllerNames(_config.Controllers);
            lines.AddRange(converter.ControllerHeader(outputConfig, files.CustomTypes(outputConfig.Type)));

            int actionCount = 0;
            int fileIterator = 0;
            foreach (CSharpFile file in files)
            {
                if (file.HasControllers)
                {
                    List<Controller> controllersForType = file.ControllersWithActionsForType(outputConfig.Type);
                    if (controllersForType.Count > 0)
                    {
                        lines.Add(converter.Comment("File: " + FileHelper.RelativePath(outputConfig.Location, file.Path), 1));

                        int controllerIterator = 0;
                        foreach (Controller controller in controllersForType)
                        {
                            List<ControllerAction> actionsForType = controller.ActionsForType(outputConfig.Type);
                            actionCount += actionsForType.Count;

                            bool lastController = fileIterator == files.Count - 1 && controllerIterator == file.Controllers.Count - 1;
                            lines.AddRange(converter.ConvertController(actionsForType, controller.OutputClassName, outputConfig, lastController));
                            controllerIterator++;
                        }
                    }
                }

                fileIterator++;
            }

            if (actionCount == 0)
            {
                lines.Add(converter.Comment($"*** NO ACTIONS ATTRIBUTED ***"));
            }

            lines.AddRange(converter.ControllerFooter());

            return string.Join('\n', lines);
        }
    }
}

