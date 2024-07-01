using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Models
{
    internal class EnumModel
    {
        public required string Identifier { get; set; }
        public required Dictionary<string, object?> Values { get; set; }
        public List<ClassDeclarationSyntax> ParentClasses { get; set; } = new();

        public OutputType ExportFor { get; set; }
    }
}
