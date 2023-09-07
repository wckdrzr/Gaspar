using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Models
{
    internal class Model
    {
        public required string ModelName { get; set; }
        public List<Property> Fields { get; set; } = new();
        public List<Property> Properties { get; set; } = new();
        public List<string> BaseClasses { get; set; } = new();
        public Dictionary<string, object?> Enumerations { get; set; } = new();
        public bool IsInterface { get; set; } = false;
        public OutputType ExportFor { get; set; }
    }

    internal class Property
    {
        public required string Identifier { get; set; }
        public string? Type { get; set; }
        public OutputType ExportFor { get; set; }
    }
}