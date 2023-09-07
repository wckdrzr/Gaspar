using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Extensions
{
    internal static class MemberDeclarationSyntaxExtensions
    {
        public static bool IsPublic(this MemberDeclarationSyntax node) =>
            node.Modifiers.Any(m => m.Value?.ToString() == "public");
    }
}
