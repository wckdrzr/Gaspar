using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WCKDRZR.Gaspar.Models
{
    internal class Configuration
    {
        public string? ConfigFilePath { get; set; }

        public ModelTypeConfiguration? Models { get; set; }
        public ControllerTypeConfiguration? Controllers { get; set; }

        public Dictionary<string, string>? CustomTypeTranslations { get; set; }

        public bool IgnoreMissingOutputLocations { get; set; } = false;
        public bool IgnoreAnnotations { get; set; } = false;
    }


    internal class ConfigurationType
    {
        public List<string> Include { get; set; } = new() { "./**/*.cs" };
        public List<string> Exclude { get; set; } = new();
        public required List<ConfigurationTypeOutput> Output { get; set; }
    }

    internal class ModelTypeConfiguration : ConfigurationType
    {
        public bool UseEnumValue { get; set; }
        public bool StringLiteralTypesInsteadOfEnums { get; set; }

        public ModelTypeConfiguration()
        {
            UseEnumValue = true;
            StringLiteralTypesInsteadOfEnums = false;
        }
    }

    internal class ControllerTypeConfiguration : ConfigurationType
    {
        public required string ServiceName { get; set; }
        public string? ServiceHost { get; set; }
        public int? ServicePort { get; set; }
    }


    internal class ConfigurationTypeOutput
    {
        public required OutputType Type { get; set; }
        public required string Location { get; set; }

        //For Controllers
        public string? UrlPrefix { get; set; }

        //For Angular Controllers
        public string? HelperFile { get; set; }
        public string? ModelPath { get; set; }
        public string? ErrorHandlerPath { get; set; }
        public AngularServiceErrorMessage? DefaultErrorMessage { get; set; }

        //For CSharp Controllers
        public string? UrlHandlerFunction { get; set; }
        public string? LoggingReceiver { get; set; }
        public List<string> ModelNamespaces { get; set; } = new();

        //For Pyton Controllers
        public Dictionary<string, string> Imports { get; set; } = new();

        //For Ocelot Controllers
        public List<string> DefaultScopes { get; set; } = new();
        public Dictionary<string, string[]> ScopesByHttpMethod { get; set; } = new();
        public bool NoAuth { get; set; } = false;
        public bool ExcludeScopes { get; set; } = false;

        //For Typescript/Angular Models and Controllers
        public bool AddInferredNullables { get; set; } = false;
        public bool NullablesAlsoUndefinded { get; set; } = false;

        //For Proto Models
        public string? PackageNamespace { get; set; }
    }

    internal class ConfigurationTypeOutputAngular { }
    internal class ConfigurationTypeOutputCSharp { }
    internal class ConfigurationTypeOutputOcelot { }
}