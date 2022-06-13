using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WCKDRZR.CSharpExporter.Extensions;
using WCKDRZR.CSharpExporter.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.ClassWalkers
{
    //Rules:
    //Must be class derived from Controller
    //Action must return ActionResult<T>
    //Action parameters must be [FromBody] or in the route ({xx})

    //optionally specify onlyWhenAttributed

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
            Controller existingController = Controllers.SingleOrDefault(f => f.ControllerName == controller.ControllerName);
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
                OutputType nodeOutputType = nodeClass.GetExportType();

                if (nodeClass.IsController())
                {
                    Controller controller = new Controller(nodeClass);
                    ControllerAction action = new(node, node.GetExportType(nodeOutputType));

                    AttributeSyntax httpAttribute = node.AttributeLists.GetAttribute("Http", true);
                    AttributeSyntax routeAttribute = node.AttributeLists.GetAttribute("Route");
                    if (httpAttribute != null)
                    { 
                        action.HttpMethod = httpAttribute.Name.ToString()[4..].ToUpper();
                    }
                    if (routeAttribute != null || httpAttribute?.ArgumentList?.Arguments[0] != null)
                    {
                        AttributeArgumentSyntax argument = routeAttribute?.ArgumentList?.Arguments[0] ?? httpAttribute.ArgumentList.Arguments[0];
                        action.Route = argument.ToString()[1..^1].Replace("?", "");
                        action.Route = action.Route.Replace(("[controller]"), controller.ControllerName);
                        action.Route = action.Route.Replace(("[action]"), action.ActionName);
                        action.Route = Regex.Replace(action.Route, "({.*?)(:.*?)}", "$1}");
                    }
                    if (action.Route == null)
                    {
                        action.Route = controller.ControllerName + "/" + action.ActionName;
                    }

                    if (node.ReturnType is GenericNameSyntax)
                    {
                        GenericNameSyntax returnGenericName = (GenericNameSyntax)node.ReturnType;
                        if (returnGenericName.Identifier.ToString() == "ActionResult")
                        {
                            action.ReturnType = returnGenericName.TypeArgumentList.Arguments[0];
                        }
                    }
                    action.ReturnTypeOverride = node.AttributeLists.AttributeValue("ReturnTypeOverride");
                    if (action.ReturnTypeOverride == null && action.ReturnType == null)
                    {
                        action.BadMethodReason = "Action should return ActionResult<T>";
                    }

                    action.CustomSerializer = node.AttributeLists.AttributeValue("Serializer");

                    List<string> routeParameters = Regex.Matches(action.Route, "{(.*?)}").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                    foreach (ParameterSyntax parameter in node.ParameterList.Parameters)
                    {
                        if (parameter.AttributeLists.ContainsAttribute("FromBody"))
                        {
                            action.BodyType = parameter.Type;
                            if (action.HttpMethod == "GET")
                            {
                                action.BadMethodReason = $"HttpGet has [FromBody] attribute; remove parameter or change from HttpGet";
                            }
                        }
                        else
                        {
                            bool onQueryString = false;
                            if (!routeParameters.Contains(parameter.Identifier.ToString()))
                            {
                                if (action.HttpMethod == "GET")
                                {
                                    onQueryString = true;
                                }
                                else
                                {
                                    action.BadMethodReason = $"Parameter '{parameter.Identifier}' not declared in route; include in route or change to HttpGet";
                                }
                            }
                            action.Parameters.Add(new(parameter, onQueryString));
                        }
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
