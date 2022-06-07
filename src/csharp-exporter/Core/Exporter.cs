using System;
using System.IO;
using System.Text.Json;
using WCKDRZR.CSharpExporter.ClassWalkers;
using WCKDRZR.CSharpExporter.Converters;
using WCKDRZR.CSharpExporter.Helpers;
using WCKDRZR.CSharpExporter.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.Core
{
	public class Exporter
	{
		public static void Export(string configFile)
		{
            Configuration config = ParseConfigurationFile(configFile);
            Converter converter = new Converter(config);
            CSharpFiles files = new();

            if (config.HasModels)
            {
                foreach (string fileName in FileHelper.GetFiles(config.Models))
                {
                    files.Add(ParseModels(fileName, config));
                }

                foreach (ConfigurationTypeOutput output in config.Models.Output)
                {
                    File.WriteAllText(output.Location, converter.BuildModelsFile(output, files));
                }
            }

            if (config.HasControllers)
            {
                foreach (string fileName in FileHelper.GetFiles(config.Controllers))
                {
                    files.Add(ParseControllers(fileName, config));
                }

                foreach (ConfigurationTypeOutput output in config.Controllers.Output)
                {
                    if (output.HelperFile != null)
                    {
                        string helperFilePath = Path.GetDirectoryName(output.Location) + '/' + output.HelperFile;
                        File.WriteAllText(helperFilePath, converter.BuildControllerHelperFile(output));
                    }

                    File.WriteAllText(output.Location, converter.BuildControllersFile(output, files));
                }
            }
        }

        private static Configuration ParseConfigurationFile(string configFile)
        {
            if (configFile == null)
            {
                throw new Exception("Please provide a config file as the first argument");
            }

            if (!File.Exists(configFile))
            {
                throw new Exception($"The config file '{configFile}' was not found");
            }

            Configuration config = new();
            try
            {
                config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configFile));
                config.ConfigFilePath = configFile;
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to read the config file\n{e.Message}");
            }

            if (!config.HasModels && !config.HasControllers)
            {
                throw new Exception("Please specify IncludeModels and/or IncludeControllers in the config, otherwise there is nothing to generate");
            }
            if (config.HasModels && (config.Models.Output == null || config.Models.Output.Count == 0))
            {
                throw new Exception("Please specify at least one Output in the Models config, otherwise there is no where to but the generated models");
            }
            if (config.HasControllers && (config.Controllers.Output == null || config.Controllers.Output.Count == 0))
            {
                throw new Exception("Please specify at least one Output in the Controllers config, otherwise there is no where to but the generated controllers");
            }
            if (config.HasControllers && config.Controllers.ServiceName == null)
            {
                throw new Exception("Please specify a ServiceName for the conrtollers");
            }

            return config;
        }

        private static CSharpFile ParseModels(string path, Configuration config)
        {
            CompilationUnitSyntax root = RootAtPath(path);

            ModelWalker modelCollector = new(config);
            modelCollector.Visit(root);

            EnumWalker enumCollector = new(config);
            enumCollector.Visit(root);

            return new CSharpFile()
            {
                Path = Path.GetFullPath(path),
                Models = modelCollector.Models,
                Enums = enumCollector.Enums,
                Controllers = new()
            };
        }

        private static CSharpFile ParseControllers(string path, Configuration config)
        {
            ControllerWalker controllerCollector = new(config);
            controllerCollector.Visit(RootAtPath(path));

            return new CSharpFile()
            {
                Path = Path.GetFullPath(path),
                Models = new(),
                Enums = new(),
                Controllers = controllerCollector.Controllers,
            };
        }

        private static CompilationUnitSyntax RootAtPath(string path)
        {
            string source = System.IO.File.ReadAllText(path);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return (CompilationUnitSyntax)tree.GetRoot();
        }
    }
}

