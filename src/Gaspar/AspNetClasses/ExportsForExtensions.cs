using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WCKDRZR.Gaspar
{
    public static class ExportsForExtensions
    {
        public static bool ExportsFor(this System.Text.Json.Serialization.Metadata.JsonTypeInfo typeInfo, int gasparType)
            => ExportsFor((MemberInfo)typeInfo.Type, gasparType);

        public static bool ExportsFor(this System.Text.Json.Serialization.Metadata.JsonPropertyInfo propertyInfo, int gasparType)
            => ExportsFor((MemberInfo?)propertyInfo.AttributeProvider, gasparType);

        public static bool ExportsFor(this Newtonsoft.Json.Serialization.JsonProperty jsonProperty, int gasparType)
            => ExportsFor((MemberInfo?)jsonProperty.PropertyType, gasparType);

        public static bool ExportsFor(this ICustomAttributeProvider attributeProvider, int gasparType)
            => ExportsFor((MemberInfo)attributeProvider, gasparType);

        public static bool ExportsFor(this TypeInfo typeInfo, int gasparType)
            => ExportsFor((MemberInfo)typeInfo, gasparType);

        public static bool ExportsFor(this Type type, int gasparType)
            => ExportsFor((MemberInfo)type, gasparType);

        public static bool ExportsFor(this MemberInfo? member, int gasparType)
        {
            if (member == null)
            {
                return false;
            }

            Stack<MemberInfo> hierarchy = new();
            hierarchy.Push(member);
            while (hierarchy.Peek().DeclaringType != null)
            {
                hierarchy.Push(hierarchy.Peek().DeclaringType!);
            }

            int memberGasparValue = 0;
            while (hierarchy.Any())
            {
                CustomAttributeData? customAttribute = hierarchy.Pop().CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(ExportForAttribute));
                if (customAttribute != null)
                {
                    bool isGasparExport = customAttribute.AttributeType == typeof(ExportForAttribute);
                    if (isGasparExport
                        && customAttribute.ConstructorArguments.Count == 1
                        && int.TryParse(customAttribute.ConstructorArguments[0].Value?.ToString(), out int argValue)
                    ) {
                        if (argValue < 0)
                        {
                            memberGasparValue &= argValue;
                        }
                        else
                        {
                            memberGasparValue |= argValue;
                        }
                    }
                }
            }

            return (memberGasparValue & gasparType) == gasparType;
        }

        
        public static bool AnyChildExportsFor(this System.Text.Json.Serialization.Metadata.JsonTypeInfo typeInfo, int gasparType)
            => AnyChildExportsFor((MemberInfo)typeInfo.Type, gasparType);

        public static bool AnyChildExportsFor(this System.Text.Json.Serialization.Metadata.JsonPropertyInfo propertyInfo, int gasparType)
            => AnyChildExportsFor((MemberInfo?)propertyInfo.AttributeProvider, gasparType);

        public static bool AnyChildExportsFor(this Newtonsoft.Json.Serialization.JsonProperty jsonProperty, int gasparType)
            => AnyChildExportsFor((MemberInfo?)jsonProperty.PropertyType, gasparType);

        public static bool AnyChildExportsFor(this ICustomAttributeProvider attributeProvider, int gasparType)
            => AnyChildExportsFor((MemberInfo)attributeProvider, gasparType);

        public static bool AnyChildExportsFor(this TypeInfo typeInfo, int gasparType)
            => AnyChildExportsFor((MemberInfo)typeInfo, gasparType);

        public static bool AnyChildExportsFor(this Type type, int gasparType)
            => AnyChildExportsFor((MemberInfo)type, gasparType);

        public static bool AnyChildExportsFor(this MemberInfo? member, int gasparType, bool includeThis = true)
        {
            if (includeThis && ExportsFor(member, gasparType))
            {
                return true;
            }

            IEnumerable<MemberInfo>? children = ((TypeInfo?)member)?.DeclaredMembers;
            if (children != null)
            {
                foreach (MemberInfo child in children)
                {
                    if (ExportsFor(child, gasparType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}