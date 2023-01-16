using System;
using System.Collections.Generic;
using System.Linq;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Converters
{
    internal class OcelotConverter : IConverter
	{
        public Configuration Config { get; set; }
        private int currentIndent = 0;

        public OcelotConverter(Configuration config)
        {
            Config = config;
        }

        public string Comment(string comment, int followingBlankLines = 0)
        {
            return $"{new String(' ', currentIndent * 4)}//{comment}{new String('\n', followingBlankLines)}";
        }

        public List<string> ControllerHelperFile(ConfigurationTypeOutput outputConfig)
        {
            return new();
        }

        public List<string> ControllerHeader(ConfigurationTypeOutput outputConfig, List<string> customTypes)
        {
            List<string> lines = new();

            lines.Add("{");
            lines.Add("    \"Routes\": [");
            lines.Add("");
            currentIndent += 2;

            return lines;
        }

        public List<string> ControllerFooter()
        {
            List<string> lines = new();
            lines.Add("    ]");
            lines.Add("}");
            currentIndent -= 2;
            return lines;
        }

        public List<string> ConvertController(List<ControllerAction> actions, string outputClassName, ConfigurationTypeOutput outputConfig, bool lastController)
        {
            List<string> lines = new();

            List<ControllerAction> uniqueOcelotActions = new();
            foreach (ControllerAction action in actions)
            {
                ControllerAction newAction = new(action.ActionName);
                newAction.HttpMethod = action.HttpMethod;
                newAction.Scopes = action.Scopes;
                newAction.AdditionalScopes = action.AdditionalScopes;

                int routeParamaterIndex = action.Route.IndexOf("{");
                newAction.Route = routeParamaterIndex >= 0 ? action.Route[..routeParamaterIndex] + "{url}" : action.Route;

                if (uniqueOcelotActions.SingleOrDefault(a => a.Route == newAction.Route && a.HttpMethod == newAction.HttpMethod) == null)
                {
                    uniqueOcelotActions.Add(newAction);
                }
            }

            int actionIterator = 0;
            foreach (ControllerAction action in uniqueOcelotActions)
            {
                bool lastAction = actionIterator == uniqueOcelotActions.Count - 1;

                string scopes = "";
                if (action.Scopes != null)
                {
                    scopes = $"\"{string.Join("\", \"", action.Scopes)}\"";
                }
                else
                {
                    if (outputConfig.DefaultScopes != null)
                    {
                        scopes = $"\"{string.Join("\", \"", outputConfig.DefaultScopes)}\"";
                    }
                    if (outputConfig.ScopesByHttpMethod != null)
                    {
                        foreach (KeyValuePair<string, string[]> scope in outputConfig.ScopesByHttpMethod)
                        {
                            if (scope.Value.Contains(action.HttpMethod, StringComparer.CurrentCultureIgnoreCase))
                            {
                                if (scopes != "") { scopes += ", "; }
                                scopes += $"\"{scope.Key}\"";
                            }
                        }
                    }
                    if (action.AdditionalScopes != null)
                    {
                        if (scopes != "") { scopes += ", "; }
                        scopes += $"\"{string.Join("\", \"", action.AdditionalScopes)}\"";
                    }
                }

                lines.Add($"        {{");
                lines.Add($"            \"DownstreamPathTemplate\": \"/{action.Route}\",");
                lines.Add($"            \"DownstreamScheme\": \"{Config.Controllers.ServiceHost}\",");
                lines.Add($"            \"DownstreamHostAndPorts\": [{{");
                lines.Add($"                \"Host\": \"{Config.Controllers.ServiceName}\",");
                lines.Add($"                \"Port\": {Config.Controllers.ServicePort}");
                lines.Add($"            }}],");
                lines.Add($"            \"UpstreamPathTemplate\": \"{outputConfig.UrlPrefix}/{action.Route}\",");
                lines.Add($"            \"UpstreamHttpMethod\": [ \"{action.HttpMethod}\" ]{(outputConfig.NoAuth ? "" : ",")}");
                if (!outputConfig.NoAuth && !string.IsNullOrEmpty(scopes))
                {
                    lines.Add($"            \"AuthenticationOptions\": {{");
                    lines.Add($"                \"AuthenticationProviderKey\": \"Bearer\"{(outputConfig.ExcludeScopes ? "" : ",")}");
                    if (!outputConfig.ExcludeScopes) { lines.Add($"                \"AllowedScopes\": [ {scopes} ]"); }
                    lines.Add($"            }}");
                }
                lines.Add($"        }}{(lastAction && lastController ? "" : ",")}");

                actionIterator++;
            }

            lines.Add("");
            return lines;
        }

        public List<string> ConvertEnum(EnumModel enumModel)
        {
            throw new NotImplementedException();
        }

        public List<string> ConvertModel(Model model, ConfigurationTypeOutput outputConfig)
        {
            throw new NotImplementedException();
        }

        public List<string> ModelHeader(ConfigurationTypeOutput outputConfig)
        {
            throw new NotImplementedException();
        }

        public void PreProcess(CSharpFiles files)
        {
            return;
        }
    }
}

