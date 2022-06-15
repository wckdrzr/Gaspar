using System;
using System.IO;
using System.Text.Json;
using WCKDRZR.Gaspar.ClassWalkers;
using WCKDRZR.Gaspar.Converters;
using WCKDRZR.Gaspar.Helpers;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace WCKDRZR.Gaspar.Core
{
    internal class Exporter
	{
		public static void Export(string configFile)
		{
            Configuration config = ConfigReader.Read(configFile);
            Converter converter = new Converter(config);
            CSharpFiles files;

            List<string> modelFiles = config.Models != null ? FileHelper.GetFiles(config.Models) : new();
            List<string> controllerFiles = config.Controllers != null ? FileHelper.GetFiles(config.Controllers) : new();
            if (config.Models != null && modelFiles.Count == 0)
            {
                throw new Exception("Cannot find any model files to use");
            }
            if (config.Controllers != null && controllerFiles.Count == 0)
            {
                throw new Exception("Cannot find any controller files to use");
            }

            files = new();
            if (config.Models != null)
            {
                foreach (string fileName in modelFiles)
                {
                    files.Add(ParseModels(fileName, config));
                }

                foreach (ConfigurationTypeOutput output in config.Models.Output)
                {
                    if (Directory.Exists(Path.GetDirectoryName(output.Location)))
                    {
                        File.WriteAllText(output.Location, converter.BuildModelsFile(output, files));
                    }
                    else if (!config.IgnoreMissingOutputLocations)
                    {
                        throw new Exception("Cannot find model output folder " + Path.GetDirectoryName(output.Location));
                    }
                }
            }

            files = new();
            if (config.Controllers != null)
            {
                foreach (string fileName in controllerFiles)
                {
                    files.Add(ParseControllers(fileName, config));
                }

                foreach (ConfigurationTypeOutput output in config.Controllers.Output)
                {
                    if (Directory.Exists(Path.GetDirectoryName(output.Location)))
                    {
                        if (output.HelperFile != null)
                        {
                            string helperFilePath = Path.GetDirectoryName(output.Location) + '/' + output.HelperFile;
                            File.WriteAllText(helperFilePath, converter.BuildControllerHelperFile(output));
                        }

                        File.WriteAllText(output.Location, converter.BuildControllersFile(output, files));
                    }
                    else if (!config.IgnoreMissingOutputLocations)
                    {
                        throw new Exception("Cannot find controller output folder " + Path.GetDirectoryName(output.Location));
                    }
                }
            }
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

