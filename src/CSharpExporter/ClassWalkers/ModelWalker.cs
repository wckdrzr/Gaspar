using System;
using System.Collections.Generic;
using System.Linq;
using WCKDRZR.CSharpExporter.Extensions;
using WCKDRZR.CSharpExporter.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.CSharpExporter.ClassWalkers
{
    public class ModelWalker : CSharpSyntaxWalker
    {
        public readonly List<Model> Models = new List<Model>();
        private readonly Configuration Config;

        public ModelWalker(Configuration config)
        {
            Config = config;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (!Config.UseAttribute || node.AttributeLists.ContainsAttribute(Config.OnlyWhenAttributed))
            {
                var model = CreateModel(node);

                Models.Add(model);
            }
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            if (!Config.UseAttribute || node.AttributeLists.ContainsAttribute(Config.OnlyWhenAttributed))
            {
                var model = CreateModel(node);

                Models.Add(model);
            }
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            if (!Config.UseAttribute || node.AttributeLists.ContainsAttribute(Config.OnlyWhenAttributed))
            {
                var model = new Model()
                {
                    ModelName = $"{node.Identifier.ToString()}{node.TypeParameterList?.ToString()}",
                    Fields = node.ParameterList?.Parameters
                                .Where(field => field.Modifiers.IsAccessible())
                                .Where(property => !property.AttributeLists.JsonIgnore())
                                .Select((field) => new Property
                                {
                                    Identifier = field.Identifier.ToString(),
                                    Type = field.Type.ToString(),
                                }).ToList(),
                    Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                                .Where(property => property.Modifiers.IsAccessible())
                                .Where(property => !property.AttributeLists.JsonIgnore())
                                .Select(p => (Property)p).ToList(),
                    BaseClasses = new List<string>(),
                };

                Models.Add(model);
            }
        }

        private static Model CreateModel(TypeDeclarationSyntax node)
        {
            List<string> baseClasses = node.BaseList?.Types.Select(s => s.ToString()).ToList();
            return new Model()
            {
                ModelName = $"{node.Identifier.ToString()}{node.TypeParameterList?.ToString()}",
                Fields = node.Members.OfType<FieldDeclarationSyntax>()
                                .Where(field => field.Modifiers.IsAccessible())
                                .Where(property => !property.AttributeLists.JsonIgnore())
                                .Select(f => (Property)f).ToList(),
                Properties = node.Members.OfType<PropertyDeclarationSyntax>()
                                .Where(property => property.Modifiers.IsAccessible())
                                .Where(property => !property.AttributeLists.JsonIgnore())
                                .Select(p => (Property)p).ToList(),
                BaseClasses = baseClasses ?? new(),
                Enumerations = baseClasses != null && baseClasses.Contains("Enumeration")
                                ? node.Members.OfType<FieldDeclarationSyntax>()
                                    .Where(property => !property.AttributeLists.JsonIgnore()).ConvertEnumerations()
                                : null,
            };
        }
    }
}