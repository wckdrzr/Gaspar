using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Extensions
{
    internal static class ClassSyntaxExtensions
    {
        public static bool IsController(this ClassDeclarationSyntax propertyClass) =>
            propertyClass.BaseList != null && propertyClass.BaseList.Types.Any(t =>
                t.ToString().Equals("Controller") || t.ToString().Equals("ControllerBase"));
    }
}
