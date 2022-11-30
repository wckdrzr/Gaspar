using System;
using System.Collections.Generic;
using WCKDRZR.Gaspar;
using WCKDRZR.Gaspar.Helpers;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace WCKDRZR.Gaspar.Converters
{
    internal interface IConverter
    {
        Configuration Config { get; set; }

        List<string> ModelHeader(ConfigurationTypeOutput outputConfig);
        List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig);
        List<string> ConvertEnum(EnumModel enumModel);

        List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig);
        List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes);
        List<string> ControllerFooter();
        List<string> ConvertController(List<ControllerAction> Actions, string outputClassName, ConfigurationTypeOutput outputConfig, bool lastController);

        string Comment(string comment, int followingBlankLines = 0);

        void PreProcess(CSharpFiles files);

        public virtual List<string> ConvertModels(List<Model> models, ConfigurationTypeOutput outputConfig)
        {
            List<string> lines = new List<string>();
            foreach (Model model in models)
            {
                lines.AddRange(this.ConvertModel(model, outputConfig));
            }
            return lines;
        }

        public virtual List<string> ConvertEnums(List<EnumModel> enumModels)
        {
            List<string> lines = new List<string>();
            foreach (EnumModel enumModel in enumModels)
            {
                lines.AddRange(this.ConvertEnum(enumModel));
            }
            return lines;
        }
    }

    internal class Converter
    {
        private Configuration _config;
        private bool _allTypes => _config.IgnoreAnnotations;

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
                case OutputType.Proto: return new ProtoConverter(_config);
                default: throw new NotImplementedException();
            }
        }

        public string BuildModelsFile(ConfigurationTypeOutput outputConfig, CSharpFiles files)
        {
            IConverter converter = GetConverter(outputConfig);
            converter.PreProcess(files);
            List<string> lines = new();

            lines.AddRange(OutputHeader.Models(converter, outputConfig, outputConfig.Location));

            int modelCount = 0;
            foreach (CSharpFile file in files)
            {
                if (file.HasModels)
                {
                    List<Model> modelsForType = _allTypes ? file.Models : file.ModelsForType(outputConfig.Type);
                    List<EnumModel> enumsForType = _allTypes ? file.Enums : file.EnumsForType(outputConfig.Type);

                    if (modelsForType.Count > 0 || enumsForType.Count > 0)
                    {
                        lines.Add(converter.Comment("File: " + FileHelper.RelativePath(outputConfig.Location, file.Path), 1));

                        modelCount += modelsForType.Count + enumsForType.Count;

                        lines.AddRange(converter.ConvertModels(modelsForType, outputConfig));
                        lines.AddRange(converter.ConvertEnums(enumsForType));
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

            files.DeDuplicateControllerAndActionNames(outputConfig.Type, _allTypes, _config.Controllers.ServiceName);
            lines.AddRange(converter.ControllerHeader(outputConfig, files.CustomTypes(outputConfig.Type, _allTypes)));

            int actionCount = 0;
            int fileIterator = 0;
            List<CSharpFile> filesToIterate = _allTypes ? files.ToList() : files.FilesWithControllersWithActionsForType(outputConfig.Type);
            foreach (CSharpFile file in filesToIterate)
            {
                List<Controller> controllersForType = _allTypes ? file.Controllers : file.ControllersWithActionsForType(outputConfig.Type);
                lines.Add(converter.Comment("File: " + FileHelper.RelativePath(outputConfig.Location, file.Path), 1));

                int controllerIterator = 0;
                foreach (Controller controller in controllersForType)
                {
                    List<ControllerAction> actionsForType = _allTypes ? controller.Actions : controller.ActionsForType(outputConfig.Type);
                    actionCount += actionsForType.Count;

                    bool lastController = fileIterator == filesToIterate.Count - 1 && controllerIterator == controllersForType.Count - 1;
                    lines.AddRange(converter.ConvertController(actionsForType, controller.OutputClassName, outputConfig, lastController));
                    controllerIterator++;
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

