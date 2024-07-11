using System;
using System.Collections.Generic;
using System.Linq;
using WCKDRZR.Gaspar.Extensions;
using WCKDRZR.Gaspar.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.ClassWalkers
{
    internal class ModelWalker : CSharpSyntaxWalker
    {
        public readonly List<Model> Models = new List<Model>();
        private readonly Configuration _config;

        public ModelWalker(Configuration config)
        {
            _config = config;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (ShouldCreate(node))
            {
                Models.Add(CreateModel(node));
            }
            foreach (MemberDeclarationSyntax m in node.Members) { Visit(m); }
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            if (ShouldCreate(node))
            {
                Models.Add(CreateModel(node));
            }
            foreach (MemberDeclarationSyntax m in node.Members) { Visit(m); }
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            if (node.IsPublic())
            {
                OutputType nodeOutputType = node.GetExportType(_config);

                Models.Add(new()
                {
                    ModelName = $"{node.Identifier}{node.TypeParameterList?.ToString()}",
                    Fields = node.ParameterList?.Parameters
                            .Where(field => field.Modifiers.IsAccessible())
                            .Where(property => !property.AttributeLists.JsonIgnore())
                            .Select(field => new Property
                            {
                                Identifier = field.Identifier.ToString(),
                                Type = field.Type?.ToString(),
                                ExportFor = field.GetExportType(_config, nodeOutputType),
                            }).ToList() ?? new(),
                    Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                            .Where(property => property.Modifiers.IsAccessible())
                            .Where(property => !property.AttributeLists.JsonIgnore())
                            .Select(p => new Property
                            {
                                Identifier = p.Identifier.ToString(),
                                Type = p.Type.ToString(),
                                JsonPropertyName = p.AttributeLists.StringValueOfAttribute("JsonPropertyName"),
                                ExportFor = p.GetExportType(_config, nodeOutputType),
                            }).ToList(),
                    ParentClasses = new(),
                    BaseClasses = new List<string>(),
                    ExportFor = nodeOutputType
                });
            }
            foreach (MemberDeclarationSyntax m in node.Members) { Visit(m); }
        }

        private bool ShouldCreate(TypeDeclarationSyntax node)
        {
            List<string> baseClasses = node.BaseList?.Types.Select(s => s.ToString()).ToList() ?? new();
            return node.IsPublic()
                && !baseClasses.Any(s => s.StartsWith("Controller"));
        }

        private Model CreateModel(TypeDeclarationSyntax node)
        {
            ExportOptionsAttribute options = new ExportOptionsAttribute();
            bool noBase = node.AttributeLists.HasAttribute(nameof(ExportWithoutInheritance)) || node.AttributeLists.BoolAttributeValue(nameof(options.NoInheritance));
            List<string> baseClasses = noBase ? new() : node.BaseList?.Types.Select(s => s.ToString()).ToList() ?? new();

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

            OutputType nodeOutputType = node.GetExportType(_config);

            return new Model()
            {
                ModelName = $"{node.Identifier.ToString()}{node.TypeParameterList?.ToString()}",
                Fields = node.Members.OfType<FieldDeclarationSyntax>()
                                .Where(field => field.Modifiers.IsAccessible())
                                .Where(property => !property.AttributeLists.JsonIgnore())
                                .Select(f => new Property
                                {
                                    Identifier = f.Declaration.Variables.First().GetText().ToString(),
                                    Type = f.Declaration.Type.ToString(),
                                    JsonPropertyName = f.AttributeLists.StringValueOfAttribute("JsonPropertyName"),
                                    ExportFor = f.GetExportType(_config, nodeOutputType),
                                }).ToList(),
                Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                                .Where(property => property.Modifiers.IsAccessible())
                                .Where(property => !property.AttributeLists.JsonIgnore())
                                .Select(p => new Property
                                {
                                    Identifier = p.Identifier.ToString(),
                                    Type = p.Type.ToString(),
                                    DefaultValue = p.Initializer?.Value.ToString(),
                                    JsonPropertyName = p.AttributeLists.StringValueOfAttribute("JsonPropertyName"),
                                    ExportFor = p.GetExportType(_config, nodeOutputType),
                                }).ToList(),
                BaseClasses = baseClasses ?? new(),
                ParentClasses = parentClasses,
                Enumerations = baseClasses != null && baseClasses.Contains("Enumeration")
                                ? node.Members.OfType<FieldDeclarationSyntax>()
                                    .Where(property => !property.AttributeLists.JsonIgnore()).ConvertEnumerations()
                                : new(),
                IsInterface = node.Keyword.Text == "interface",
                ExportFor = nodeOutputType
            };
        }
    }
}