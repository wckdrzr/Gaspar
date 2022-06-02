using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExporter.Extensions
{
    public static class FieldSyntaxExtensions
    {
        public static Dictionary<string, object> ConvertEnumerations(this IEnumerable<FieldDeclarationSyntax> fields)
        {
            var values = new Dictionary<string, object>();

            foreach (FieldDeclarationSyntax field in fields)
            {
                VariableDeclaratorSyntax variable = field.Declaration.Variables.First();
                List<SyntaxToken> tokens = variable.DescendantTokens().ToList();

                string idValue = tokens.Count > 4 ? tokens[4].Value.ToString() : null;
                if (idValue == "id" && tokens.Count > 6)
                {
                    idValue = tokens[6].Value.ToString();
                }

                values[variable.GetFirstToken().ToString()] = idValue;
            }

            return values;
        }
    }
}
