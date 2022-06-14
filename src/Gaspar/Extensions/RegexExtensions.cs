using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Extensions
{
    internal static class RegexExtensions
    {
        public static bool HasMatch(this MatchCollection matches) =>
            matches.Count > 0;

        public static string At(this MatchCollection matches, int index) =>
            matches.ElementAt(0).Groups[index].Value;
    }
}
