﻿using System;
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
    internal class Exporter
	{
		public static void Export(string configFile)
		{
            Configuration config = ConfigReader.Read(configFile);
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

