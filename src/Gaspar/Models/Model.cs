using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Models
{
    internal class Model
    {
        public required string ModelName { get; set; }
        public List<Property> Fields { get; set; } = new();
        public List<Property> Properties { get; set; } = new();
        public List<string> BaseClasses { get; set; } = new();
        public List<ClassDeclarationSyntax> ParentClasses { get; set; } = new();
        public Dictionary<string, object?> Enumerations { get; set; } = new();
        public bool IsInterface { get; set; } = false;
        public OutputType ExportFor { get; set; }

        public string FullName
        {
            get
            {
                string parents = string.Join(".", ParentClasses.Select(p => p.Identifier).Reverse());
                if (parents != "") { parents += "."; }
                return $"{parents}{ModelName}";
            }
        }
    }

    internal class Property
    {
        public required string Identifier { get; set; }
        public string? Type { get; set; }
        public string? DefaultValue { get; set; }
        public string? JsonPropertyName { get; set; }
        public OutputType ExportFor { get; set; }
    }
}