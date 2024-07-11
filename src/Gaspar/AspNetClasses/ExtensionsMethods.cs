using System;
using System.Linq;
using System.Reflection;

namespace WCKDRZR.Gaspar
{
    public static class GasparExtensions
    {
        public static bool ExportsFor(this TypeInfo typeInfo, int gasparType, bool includeParent = true, bool anyChildrenMatch = false)
        {
            return ExportsFor((MemberInfo)typeInfo, gasparType, includeParent, anyChildrenMatch);
        }

        public static bool ExportsFor(this Type type, int gasparType, bool includeParent = true, bool anyChildrenMatch = false)
        {
            return ExportsFor((MemberInfo)type, gasparType, includeParent, anyChildrenMatch);
        }

        public static bool ExportsFor(this MemberInfo member, int gasparType, bool includeParent = true, bool anyChildrenMatch = false)
        {
            CustomAttributeData? customAttribute = member.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(ExportForAttribute));
            if (customAttribute != null)
            {
                bool isGasparExport = customAttribute.AttributeType == typeof(ExportForAttribute);
                if (isGasparExport
                    && customAttribute.ConstructorArguments.Count == 1
                    && int.TryParse(customAttribute.ConstructorArguments[0].Value?.ToString(), out int argValue)
                    && (argValue == GasparType.All || (argValue & gasparType) != 0)
                ) {
                    return true;
                }
            }

            //Check parent
            if (includeParent
                && member.DeclaringType != null
                && member.DeclaringType.ExportsFor(gasparType))
            {
                return true;
            }

            //Check children
            if (anyChildrenMatch)
            {
                try
                {
                    foreach (MemberInfo child in ((TypeInfo)member).DeclaredMembers)
                    {
                        if (child.ExportsFor(gasparType, includeParent: false))
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }

            return false;
        }
    }
}