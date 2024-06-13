using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WCKDRZR.Gaspar.Extensions
{
    internal static class SyntaxListExtensions
    {
        public static bool IsAccessible(this SyntaxTokenList modifiers) =>
            modifiers.All(modifier =>
                modifier.ToString() != "const" &&
                modifier.ToString() != "static" &&
                modifier.ToString() != "private"
            );

        public static bool JsonIgnore(this SyntaxList<AttributeListSyntax> propertyAttributeLists) =>
            propertyAttributeLists.Any(attributeList =>
                attributeList.Attributes.Any(attribute =>
                    attribute.Name.ToString().Equals("JsonIgnore")));

        public static bool ContainsAttribute(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName) =>
                propertyAttributeList.Any(attributeList =>
                    attributeList.Attributes.Any(attribute =>
                        attribute.Name.ToString().Equals(attributeName)));

        public static bool HasAttribute(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName, bool startsWith = false)
        {
            return propertyAttributeList.GetAttribute(attributeName, startsWith) != null;
        }

        public static AttributeSyntax? GetAttribute(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName, bool startsWith = false)
        {
            foreach (AttributeListSyntax attributeListSyntax in propertyAttributeList)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    string name = attributeSyntax.Name.ToString();
                    if (name.Equals(attributeName) || (startsWith && name.StartsWith(attributeName)))
                    {
                        return attributeSyntax;
                    }
                }
            }
            return null;
        }

        public static string? StringValueOfAttribute(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName)
        {
            foreach (AttributeListSyntax attributeListSyntax in propertyAttributeList)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.Name.ToString() == attributeName)
                    {
                        string? argument = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.ToString();
                        if (argument != null && argument.StartsWith("\""))
                        {
                            return argument[1..^1];
                        }
                    }
                }
            }
            return null;
        }

        public static string? StringAttributeValue(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName)
        {
            foreach (AttributeListSyntax attributeListSyntax in propertyAttributeList)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.ArgumentList != null)
                    {
                        foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList.Arguments)
                        {
                            if (argument.NameEquals?.Name?.ToString() == attributeName)
                            {
                                string expression = argument.Expression.ToString();
                                if (expression.StartsWith("\""))
                                {
                                    return expression[1..^1];
                                }
                                if (expression.StartsWith("nameof("))
                                {
                                    return expression[7..^1];
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static string[]? StringArrayAttributeValue(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName)
        {
            foreach (AttributeListSyntax attributeListSyntax in propertyAttributeList)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.ArgumentList != null)
                    {
                        foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList.Arguments)
                        {
                            if (argument.NameEquals?.Name?.ToString() == attributeName)
                            {
                                string expression = argument.Expression.ToString();
                                return Regex.Matches(expression, @"""(.*?[^\\])""")
                                    .Cast<Match>()
                                    .Select(m => m.Groups[1].Value)
                                    .ToArray();
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static bool BoolAttributeValue(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName)
        {
            foreach (AttributeListSyntax attributeListSyntax in propertyAttributeList)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.ArgumentList != null)
                    {
                        foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList.Arguments)
                        {
                            if (argument.NameEquals?.Name?.ToString() == attributeName)
                            {
                                return argument.Expression.ToString().Trim() == "true";
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static long? IntAttributeValue(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName)
        {
            foreach (AttributeListSyntax attributeListSyntax in propertyAttributeList)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.ArgumentList != null)
                    {
                        foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList.Arguments)
                        {
                            if (argument.NameEquals?.Name?.ToString() == attributeName)
                            {
                                if (long.TryParse(argument.Expression.ToString().Trim(), out long value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

    }
}
