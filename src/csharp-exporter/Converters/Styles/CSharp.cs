using System;
using System.Collections.Generic;
using CSharpExporter.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExporter.Converters
{
	public class CSharpConverter : IConverter
	{
        public Configuration Config { get; set; }

        public CSharpConverter(Configuration config)
        {
            Config = config;
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"//{comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> ControllerHelperFile()
        {
            throw new NotImplementedException();
        }

        public List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes)
        {
            throw new NotImplementedException();
        }

        public List<string> ControllerFooter()
        {
            throw new NotImplementedException();
        }

        public List<string> ConvertController(Controller controller, ConfigurationTypeOutput outputConfig, bool lastController)
        {
            throw new NotImplementedException();
        }

        public List<string> ConvertEnum(EnumModel enumModel)
        {
            throw new NotImplementedException();
        }

        public List<string> ConvertModel(Model model)
        {
            throw new NotImplementedException();
        }
    }
}

