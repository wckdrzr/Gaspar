using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WCKDRZR.Gaspar.Models
{
    internal class Configuration
    {
        public string ConfigFilePath { get; set; }

        public ModelTypeConfiguration Models { get; set; }
        public ControllerTypeConfiguration Controllers { get; set; }

        public Dictionary<string, string> CustomTypeTranslations { get; set; }

        public bool IgnoreMissingOutputLocations { get; set; }
        public bool IgnoreAnnotations { get; set; }

        public Configuration()
        {
            IgnoreMissingOutputLocations = false;
            IgnoreAnnotations = false;
        }
    }


    internal class ConfigurationType
    {
        public List<string> Include { get; set; }
        public List<string> Exclude { get; set; }
        public List<ConfigurationTypeOutput> Output { get; set; }

        public ConfigurationType()
        {
            Include = new() { "./**/*.cs" };
        }
    }

    internal class ModelTypeConfiguration : ConfigurationType
    {
        public bool NumericEnums { get; set; }
        public bool StringLiteralTypesInsteadOfEnums { get; set; }

        public ModelTypeConfiguration()
        {
            NumericEnums = false;
            StringLiteralTypesInsteadOfEnums = false;
        }
    }

    internal class ControllerTypeConfiguration : ConfigurationType
    {
        public string ServiceName { get; set; }
        public string ServiceHost { get; set; }
        public int ServicePort { get; set; }
    }


    internal class ConfigurationTypeOutput
    {
        public OutputType Type { get; set; }
        public string Location { get; set; }

        //For Controllers
        public string UrlPrefix { get; set; }

        //For Angular Controllers
        public string HelperFile { get; set; }
        public string ModelPath { get; set; }
        public string ErrorHandlerPath { get; set; }
        public AngularServiceErrorMessage DefaultErrorMessage { get; set; }

        //For CSharp Controllers
        public string UrlHandlerFunction { get; set; }
        public string LoggingReceiver { get; set; }
        public List<string> ModelNamespaces { get; set; }

        //For Ocelot Controllers
        public bool NoAuth { get; set; }
        public bool ExcludeScopes { get; set; }

        public ConfigurationTypeOutput()
        {
            NoAuth = false;
            ExcludeScopes = false;
        }
    }

    internal class ConfigurationTypeOutputAngular { }
    internal class ConfigurationTypeOutputCSharp { }
    internal class ConfigurationTypeOutputOcelot { }
}