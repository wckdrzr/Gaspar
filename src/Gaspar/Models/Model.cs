using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Models
{
    internal class Model
    {
        public string ModelName { get; set; }
        public List<Property> Fields { get; set; }
        public List<Property> Properties { get; set; }
        public List<string> BaseClasses { get; set; }
        public Dictionary<string, object> Enumerations { get; set; }
        public bool IsInterface => Type == "interface";
        public OutputType ExportFor { get; set; }
        public string Type { get; set; }
    }

    internal class Property
    {
        public string Identifier { get; set; }
        public string Type { get; set; }
        public OutputType ExportFor { get; set; }
    }
}