using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WCKDRZR.Gaspar.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace WCKDRZR.Gaspar.Models
{
    internal class Controller
    {
        public string ControllerName { get; set; }
        public string OutputClassName { get; set; }

        public List<ControllerAction> Actions { get; set; }
        public List<ControllerAction> ActionsForType(OutputType type) => Actions.Where(a => a.ExportFor.HasFlag(type)).ToList();

        public Controller(ClassDeclarationSyntax controllerNode)
        {
            ControllerName = controllerNode.Identifier.ToString();
            if (ControllerName.EndsWith("Controller"))
            {
                ControllerName = ControllerName[..^10];
            }

            Actions = new();

            OutputClassName = ControllerName;
        }
    }

    internal class ControllerAction
    {
        public string ActionName { get; set; }
        public string OutputActionName { get; set; }

        public string HttpMethod { get; set; } = "GET";
        public string Route { get; set; } = "/";

        public TypeSyntax? ReturnType { get; set; } = null;
        public string? ReturnTypeOverride { get; set; } = null;

        public List<Parameter> Parameters { get; set; } = new();
        public Parameter? BodyParameter { get; set; } = null;

        public string? CustomSerializer { get; set; } = null;

        public string[]? Scopes { get; set; }
        public string[]? AdditionalScopes { get; set; }

        public string? BadMethodReason { get; set; }

        public OutputType ExportFor { get; set; }

        public ControllerAction(MethodDeclarationSyntax node, OutputType exportFor) : this(node.Identifier.ToString())
        {
            ExportFor = exportFor;
        }
        public ControllerAction(string name)
        {
            ActionName = name;
            OutputActionName = ActionName;
        }
    }

    internal class Parameter
    {
        public string Identifier { get; set; }
        public TypeSyntax? Type { get; set; }
        public string? DefaultValue { get; set; }
        public bool OnQueryString { get; set; }
        public bool IsNullable { get; set; }

        public Parameter(ParameterSyntax parameterSyntax, bool onQueryString)
        {
            Identifier = parameterSyntax.Identifier.ToString();
            Type = parameterSyntax.Type;
            DefaultValue = parameterSyntax.Default == null ? null : parameterSyntax.Default.Value.ToString();
            OnQueryString = onQueryString;
            IsNullable = parameterSyntax.Type is NullableTypeSyntax;
        }
    }
}
