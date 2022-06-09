using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.Extensions
{
    internal static class StringExtensions
    {
        public static string ToProper(this string s) =>
            string.IsNullOrEmpty(s) ? s : s[..1].ToUpper() + s[1..].ToLower();
    }
}
