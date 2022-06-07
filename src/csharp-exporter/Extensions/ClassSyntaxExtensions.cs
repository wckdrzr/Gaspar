using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.Extensions
{
    public static class ClassSyntaxExtensions
    {
        public static bool IsController(this ClassDeclarationSyntax propertyClass) =>
            propertyClass.BaseList != null && propertyClass.BaseList.Types.Any(t =>
                t.ToString().Equals("Controller") || t.ToString().Equals("ControllerBase"));
    }
}
