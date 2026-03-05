using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Extensions
{
    internal static class TypeSyntaxExtensions
    {
        public static bool IsController(this TypeDeclarationSyntax propertyClass) =>
            propertyClass.BaseList != null && propertyClass.BaseList.Types.Any(t =>
                t.ToString().Contains("Controller"));
    }
}
