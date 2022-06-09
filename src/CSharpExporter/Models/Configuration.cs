using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WCKDRZR.CSharpExporter.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum OutputType
    {
        Angular,
        CSharp,
        Ocelot,
        TypeScript
    }


    internal class Configuration
    {
        public string ConfigFilePath { get; set; }

        public ModelTypeConfiguration Models { get; set; }
        public ControllerTypeConfiguration Controllers { get; set; }

        public string OnlyWhenAttributed { get; set; }
        public bool UseAttribute => !string.IsNullOrEmpty(OnlyWhenAttributed);

        public Dictionary<string, string> CustomTypeTranslations { get; set; }

        public bool HasModels => Models != null && Models.Include != null && Models.Include.Count > 0;
        public bool HasControllers => Controllers != null && Controllers.Include != null && Controllers.Include.Count > 0;
    }


    internal class ConfigurationType
    {
        public List<string> Include { get; set; }
        public List<string> Exclude { get; set; }
        public List<ConfigurationTypeOutput> Output { get; set; }
    }

    internal class ModelTypeConfiguration : ConfigurationType
    {
        public bool CamelCaseEnums { get; set; }
        public bool NumericEnums { get; set; }
        public bool StringLiteralTypesInsteadOfEnums { get; set; }
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
        public string UrlPrefix { get; set; }

        //For Angular
        public string HelperFile { get; set; }
        public string ModelPath { get; set; }
        public string ErrorHandlerPath { get; set; }
        public AngularServiceErrorMessage DefaultErrorMessage { get; set; }

        //For CSharp
        public string UrlHandlerFunction { get; set; }
        public List<string> ModelNamespaces { get; set; }

        //For Ocelot
        public bool NoAuth { get; set; }
        public bool ExcludeScopes { get; set; }
    }

    internal class ConfigurationTypeOutputAngular { }
    internal class ConfigurationTypeOutputCSharp { }
    internal class ConfigurationTypeOutputOcelot { }
}