using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpExporter.ClassWalkers;
using CSharpExporter.Models;
using CSharpExporter.Helpers;
using System;
using System.Text.Json;
using System.IO;
using CSharpExporter.Converters;

namespace CSharpExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            string configFile = args.Length > 0 ? args[0] : null;
            //configFile = "../../../../../csharp-exporter.config.json"; //test file

            if (configFile == null)
            {
                Exit(1, "Please provide a config file as the first argument");
            }
            if (!File.Exists(configFile))
            {
                Exit(2, $"The config file '{configFile}' was not found");
            }

            Configuration config = new();
            try
            {
                config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configFile));
                config.ConfigFilePath = configFile;
            }
            catch (Exception e)
            {
                Exit(3, $"Failed to read the config file\n{e.Message}");
            }

            bool haveModels = config.Models != null && config.Models.Include != null && config.Models.Include.Count > 0;
            bool haveControllers = config.Controllers != null && config.Controllers.Include != null && config.Controllers.Include.Count > 0;

            if (!haveModels && !haveControllers)
            {
                Exit(4, "Please specify IncludeModels and/or IncludeControllers in the config, otherwise there is nothing to generate");
            }
            if (haveModels && (config.Models.Output == null || config.Models.Output.Count == 0))
            {
                Exit(5, "Please specify at least one Output in the Models config, otherwise there is no where to but the generated models");
            }
            if (haveControllers && (config.Controllers.Output == null || config.Controllers.Output.Count == 0))
            {
                Exit(6, "Please specify at least one Output in the Controllers config, otherwise there is no where to but the generated controllers");
            }
            if (haveControllers && config.Controllers.ServiceName == null)
            {
                Exit(7, "Please specify a ServiceName for the conrtollers");
            }


            CSharpFiles files = new();
            Converter converter = new Converter(config);


            if (haveModels)
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

            if (haveControllers)
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


            //string json = JsonSerializer.Serialize(files);
            //Console.WriteLine(json);
        }




        private static void Exit(int code, string message)
        {
            Console.WriteLine(message);
            Environment.Exit(code);
        }
















        static CSharpFile ParseModels(string path, Configuration config)
        {
            CompilationUnitSyntax root = RootAtPath(path);

            ModelWalker modelCollector = new(config);
            modelCollector.Visit(root);

            EnumWalker enumCollector = new(config);
            enumCollector.Visit(root);

            return new CSharpFile() {
                Path = Path.GetFullPath(path),
                Models = modelCollector.Models,
                Enums = enumCollector.Enums,
                Controllers = new()
            };
        }

        static CSharpFile ParseControllers(string path, Configuration config)
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

        static CompilationUnitSyntax RootAtPath(string path)
        {
            string source = System.IO.File.ReadAllText(path);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return (CompilationUnitSyntax)tree.GetRoot();
        }
    }
}