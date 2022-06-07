using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WCKDRZR.CSharpExporter.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.Models
{
    public class Controller
    {
        public string ControllerName { get; set; }
        public List<ControllerAction> Actions { get; set; }

        public string OutputClassName { get; set; }

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

    public class ControllerAction
    {
        public string HttpMethod { get; set; }
        public string ActionName { get; set; }
        public string Route { get; set; }

        public TypeSyntax ReturnType { get; set; }
        public string ReturnTypeOverride { get; set; }

        public TypeSyntax BodyType { get; set; }
        public List<Parameter> Parameters { get; set; }

        public string BadMethodReason { get; set; }
        
        public ControllerAction(string name)
        {
            ActionName = name;
            ReturnType = null;
            Parameters = new();
        }
    }

    public class Parameter
    {
        public string Identifier { get; set; }
        public TypeSyntax Type { get; set; }
        public string DefaultValue { get; set; }
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
