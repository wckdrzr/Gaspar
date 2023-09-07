using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.ClassWalkers
{
    internal class ControllerWalker : CSharpSyntaxWalker
    {
        public readonly List<Controller> Controllers = new();
        private readonly Configuration Config;

        public ControllerWalker(Configuration config)
        {
            Controllers = new();
            Config = config;
        }

        public void AddAction(Controller controller, ControllerAction action)
        {
            Controller? existingController = Controllers.SingleOrDefault(f => f.ControllerName == controller.ControllerName);
            if (existingController == null)
            {
                controller.Actions.Add(action);
                Controllers.Add(controller);
            }
            else
            {
                existingController.Actions.Add(action);
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Parent is ClassDeclarationSyntax)
            {
                ClassDeclarationSyntax nodeClass = (ClassDeclarationSyntax)node.Parent;

                OutputType nodeClassOutputType = nodeClass.GetExportType();

                ExportOptionsAttribute options = new ExportOptionsAttribute();
                string? nodeClassReturnTypeOverrider = nodeClass.AttributeLists.StringAttributeValue(nameof(options.ReturnTypeOverride));
                string? nodeClassCustomSerializer = nodeClass.AttributeLists.StringAttributeValue(nameof(options.Serializer));
                string[]? nodeClassScopes = nodeClass.AttributeLists.StringArrayAttributeValue(nameof(options.Scopes));
                string[]? nodeClassAdditionalScopes = nodeClass.AttributeLists.StringArrayAttributeValue(nameof(options.AdditionalScopes));

                if (nodeClass.IsController() && node.IsPublic())
                {
                    Controller controller = new Controller(nodeClass);
                    ControllerAction action = new(node, node.GetExportType(nodeClassOutputType));

                    AttributeSyntax? httpAttribute = node.AttributeLists.GetAttribute("Http", true);
                    AttributeSyntax? routeAttribute = node.AttributeLists.GetAttribute("Route");
                    if (httpAttribute != null)
                    { 
                        action.HttpMethod = httpAttribute.Name.ToString()[4..].ToUpper();
                    }
                    if (action.HttpMethod == null)
                    {
                        action.BadMethodReason = "HTTP Method not spcified";
                    }

                    if (routeAttribute != null || httpAttribute?.ArgumentList?.Arguments[0] != null)
                    {
                        AttributeArgumentSyntax? argument = routeAttribute?.ArgumentList?.Arguments[0] ?? httpAttribute?.ArgumentList?.Arguments[0];
                        if (argument != null)
                        {
                            action.Route = argument.ToString()[1..^1].Replace("?", "");
                            action.Route = action.Route.Replace(("[controller]"), controller.ControllerName);
                            action.Route = action.Route.Replace(("[action]"), action.ActionName);
                            action.Route = Regex.Replace(action.Route, "({.*?)(:.*?)}", "$1}");
                        }
                    }
                    if (action.Route == null)
                    {
                        action.Route = controller.ControllerName + "/" + action.ActionName;
                    }

                    if (node.ReturnType is GenericNameSyntax && ((GenericNameSyntax)node.ReturnType).Identifier.ToString() == "ActionResult")
                    {
                        action.ReturnType = ((GenericNameSyntax)node.ReturnType).TypeArgumentList.Arguments[0];
                    }
                    else if (node.ReturnType.ToString() == "IActionResult" || node.ReturnType.ToString() == "ActionResult")
                    {
                        action.ReturnType = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("string"));
                    }
                    else if (node.ReturnType.ToString() != "void")
                    {
                        action.ReturnType = node.ReturnType;
                    }
                    action.ReturnTypeOverride = node.AttributeLists.StringAttributeValue(nameof(options.ReturnTypeOverride)) ?? nodeClassReturnTypeOverrider;
                    
                    action.CustomSerializer = node.AttributeLists.StringAttributeValue(nameof(options.Serializer)) ?? nodeClassCustomSerializer;

                    action.Scopes = node.AttributeLists.StringArrayAttributeValue(nameof(options.Scopes)) ?? nodeClassScopes;
                    action.AdditionalScopes = node.AttributeLists.StringArrayAttributeValue(nameof(options.AdditionalScopes)) ?? nodeClassAdditionalScopes;

                    List<string> routeParameters = Regex.Matches(action.Route, "{(.*?)}").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                    foreach (ParameterSyntax parameter in node.ParameterList.Parameters)
                    {
                        bool onQueryString = false;
                        if (parameter.AttributeLists.ContainsAttribute("FromBody"))
                        {
                            action.BodyParameter = new(parameter, false);
                            if (action.HttpMethod == "GET")
                            {
                                action.BadMethodReason = $"HttpGet has [FromBody] attribute; remove parameter or change from HttpGet";
                            }
                        }
                        else if (!routeParameters.Contains(parameter.Identifier.ToString()))
                        {
                            onQueryString = true;
                        }

                        action.Parameters.Add(new(parameter, onQueryString));                        
                    }
                    List<string> missingParameters = routeParameters.Except(action.Parameters.Select(ap => ap.Identifier)).ToList();
                    if (missingParameters.Count > 0)
                    {
                        action.BadMethodReason = $"Route parameter '{missingParameters[0]}' not declared on action method";
                    }
                        
                    if (!nodeClass.IsController()) { action.BadMethodReason = "Class must inherit 'Controller' for methods to work here"; }

                    AddAction(controller, action);
                }
            }
        }
    }
}
