using System.Collections.Generic;
using System.Linq;
using CSharpExporter.Extensions;
using CSharpExporter.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExporter.ClassWalkers
{
    class EnumWalker : CSharpSyntaxWalker
    {
        public readonly List<EnumModel> Enums = new List<EnumModel>();
        private readonly Configuration Config;

        public EnumWalker(Configuration config)
        {
            Config = config;
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (!Config.UseAttribute || node.AttributeLists.ContainsAttribute(Config.OnlyWhenAttributed))
            {
                var values = new Dictionary<string, object>();

                foreach (var member in node.Members)
                {
                    values[member.Identifier.ToString()] = member.EqualsValue != null
                        ? member.EqualsValue.Value.ToString()
                        : null;
                }

                this.Enums.Add(new EnumModel()
                {
                    Identifier = node.Identifier.ToString(),
                    Values = values
                });
            }
        }
    }
}
