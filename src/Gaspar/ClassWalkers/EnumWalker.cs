using System.Collections.Generic;
using System.Linq;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.ClassWalkers
{
    internal class EnumWalker : CSharpSyntaxWalker
    {
        public readonly List<EnumModel> Enums = new List<EnumModel>();
        private readonly Configuration _config;

        public EnumWalker(Configuration config)
        {
            _config = config;
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (node.IsPublic())
            {
                var values = new Dictionary<string, object?>();

                List<ClassDeclarationSyntax> parentClasses = new();
                SyntaxNode? parent = node.Parent;
                while (parent != null)
                {
                    if (parent.GetType() == typeof(ClassDeclarationSyntax))
                    {
                        parentClasses.Add((ClassDeclarationSyntax)parent);
                        parent = parent.Parent;
                    }
                    else
                    {
                        parent = null;
                    }
                }

                foreach (var member in node.Members)
                {
                    values[member.Identifier.ToString()] =
                        member.AttributeLists.StringAttributeValue("Value")
                        ?? member.AttributeLists.StringAttributeValue("Name")
                        ?? (
                            member.EqualsValue != null
                                ? member.EqualsValue.Value.ToString()
                                : null
                        );
                }

                this.Enums.Add(new EnumModel()
                {
                    Identifier = node.Identifier.ToString(),
                    Values = values,
                    ParentClasses = parentClasses,
                    ExportFor = node.GetExportType(_config)
                });
            }
            foreach (MemberDeclarationSyntax m in node.Members) { Visit(m); }
        }
    }
}
