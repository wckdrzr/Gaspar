﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WCKDRZR.CSharpExporter.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OutputType
    {
        Angular,
        CSharp,
        Ocelot,
        TypeScript
    }


    public class Configuration
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


    public class ConfigurationType
    {
        public List<string> Include { get; set; }
        public List<string> Exclude { get; set; }
        public List<ConfigurationTypeOutput> Output { get; set; }
    }

    public class ModelTypeConfiguration : ConfigurationType
    {
        public bool CamelCaseEnums { get; set; }
        public bool NumericEnums { get; set; }
        public bool StringLiteralTypesInsteadOfEnums { get; set; }
    }

    public class ControllerTypeConfiguration : ConfigurationType
    {
        public string ServiceName { get; set; }
        public string ServiceHost { get; set; }
        public int ServicePort { get; set; }
    }


    public class ConfigurationTypeOutput
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

    public class ConfigurationTypeOutputAngular { }
    public class ConfigurationTypeOutputCSharp { }
    public class ConfigurationTypeOutputOcelot { }
}