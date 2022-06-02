using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExporter.Extensions
{
    public static class SyntaxListExtensions
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

        public static AttributeSyntax GetAttribute(this SyntaxList<AttributeListSyntax> propertyAttributeList, string attributeName, bool startsWith = false)
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

        public static string ReturnTypeOveride(this SyntaxList<AttributeListSyntax> propertyAttributeList)
        {
            foreach (AttributeListSyntax attributeListSyntax in propertyAttributeList)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.ArgumentList != null)
                    {
                        foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList.Arguments)
                        {
                            if (argument.NameEquals?.Name?.ToString() == "ReturnTypeOverride")
                            {
                                return argument.Expression.ToString()[1..^1];
                            }
                        }
                    }
                }
            }
            return null;
        }

    }
}
