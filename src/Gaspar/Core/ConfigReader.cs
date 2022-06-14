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
using WCKDRZR.Gaspar.Extensions;

namespace WCKDRZR.Gaspar.Core
{
    internal static class ConfigReader
	{
		public static Configuration Read(string configFile) =>
		    ParseConfigurationFile(configFile).ReplaceVariablesInConfig();

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

            if (config.Models == null && config.Controllers == null)
            {
                throw new Exception("Please specify Models and/or Controllers in the config, otherwise there is nothing to generate");
            }
            if (config.Models != null && (config.Models.Output == null || config.Models.Output.Count == 0))
            {
                throw new Exception("Please specify at least one Output in the Models config, otherwise there is no where to but the generated models");
            }
            if (config.Controllers != null && (config.Controllers.Output == null || config.Controllers.Output.Count == 0))
            {
                throw new Exception("Please specify at least one Output in the Controllers config, otherwise there is no where to but the generated controllers");
            }
            
            return config;
        }

        private static Configuration ReplaceVariablesInConfig(this Configuration config)
        {
            if (config.Controllers != null)
            {
                foreach (ConfigurationTypeOutput outputConfig in config.Controllers.Output)
                {
                    string serviceName = config.Controllers.ServiceName;
                    string serviceHost = config.Controllers.ServiceHost;
                    string servicePort = config.Controllers.ServicePort.ToString();

                    if (outputConfig.Type == OutputType.CSharp)
                    {
                        serviceName = serviceName.ToProper();
                    }

                    if (!string.IsNullOrEmpty(outputConfig.Location))
                    {
                        outputConfig.Location = outputConfig.Location
                            .Replace("{ServiceName}", serviceName ?? "", StringComparison.CurrentCultureIgnoreCase)
                            .Replace("{ServiceHost}", serviceHost ?? "", StringComparison.CurrentCultureIgnoreCase)
                            .Replace("{ServicePort}", servicePort ?? "", StringComparison.CurrentCultureIgnoreCase);
                    }
                    if (!string.IsNullOrEmpty(outputConfig.UrlPrefix))
                    {
                        outputConfig.UrlPrefix = outputConfig.UrlPrefix
                            .Replace("{ServiceName}", serviceName ?? "", StringComparison.CurrentCultureIgnoreCase)
                            .Replace("{ServiceHost}", serviceHost ?? "", StringComparison.CurrentCultureIgnoreCase)
                            .Replace("{ServicePort}", servicePort ?? "", StringComparison.CurrentCultureIgnoreCase);
                    }
                }
            }

            return config;
        }
    }
}

