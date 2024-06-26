﻿using System;
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
        private readonly Configuration Config;

        public ModelWalker(Configuration config)
        {
            Config = config;
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
                OutputType nodeOutputType = node.GetExportType();

                Models.Add(new()
                {
                    ModelName = $"{node.Identifier.ToString()}{node.TypeParameterList?.ToString()}",
                    Fields = node.ParameterList?.Parameters
                            .Where(field => field.Modifiers.IsAccessible())
                            .Where(property => !property.AttributeLists.JsonIgnore())
                            .Select(field => new Property
                            {
                                Identifier = field.Identifier.ToString(),
                                Type = field.Type?.ToString(),
                                ExportFor = field.GetExportType(nodeOutputType),
                            }).ToList() ?? new(),
                    Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                            .Where(property => property.Modifiers.IsAccessible())
                            .Where(property => !property.AttributeLists.JsonIgnore())
                            .Select(p => new Property
                            {
                                Identifier = p.Identifier.ToString(),
                                Type = p.Type.ToString(),
                                JsonPropertyName = p.AttributeLists.StringValueOfAttribute("JsonPropertyName"),
                                ExportFor = p.GetExportType(nodeOutputType),
                            }).ToList(),
                    ParentClasses = new(),
                    BaseClasses = new List<string>(),
                    ExportFor = nodeOutputType
                });
            }
            foreach (MemberDeclarationSyntax m in node.Members) { Visit(m); }
        }

        private static bool ShouldCreate(TypeDeclarationSyntax node)
        {
            List<string> baseClasses = node.BaseList?.Types.Select(s => s.ToString()).ToList() ?? new();
            return node.IsPublic()
                && !baseClasses.Any(s => s.StartsWith("Controller"));
        }

        private static Model CreateModel(TypeDeclarationSyntax node)
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

            OutputType nodeOutputType = node.GetExportType();

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
                                    ExportFor = f.GetExportType(nodeOutputType),
                                }).ToList(),
                Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                                .Where(property => property.Modifiers.IsAccessible())
                                .Where(property => !property.AttributeLists.JsonIgnore())
                                .Select(p => new Property
                                {
                                    Identifier = p.Identifier.ToString(),
                                    Type = p.Type.ToString(),
                                    JsonPropertyName = p.AttributeLists.StringValueOfAttribute("JsonPropertyName"),
                                    ExportFor = p.GetExportType(nodeOutputType),
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