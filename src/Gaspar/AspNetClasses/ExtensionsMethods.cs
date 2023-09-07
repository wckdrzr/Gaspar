using System;
using System.Linq;
using System.Reflection;

namespace WCKDRZR.Gaspar
{
    public static class GasparExtensions
    {
        public static bool ExportsFor(this TypeInfo typeInfo, GasparType gasparType, bool includeParent = true, bool anyChildrenMatch = false)
        {
            return ExportsFor((MemberInfo)typeInfo, gasparType, includeParent, anyChildrenMatch);
        }

        public static bool ExportsFor(this Type type, GasparType gasparType, bool includeParent = true, bool anyChildrenMatch = false)
        {
            return ExportsFor((MemberInfo)type, gasparType, includeParent, anyChildrenMatch);
        }

        public static bool ExportsFor(this MemberInfo member, GasparType gasparType, bool includeParent = true, bool anyChildrenMatch = false)
        {
            CustomAttributeData? customAttribute = member.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(Gaspar.ExportForAttribute));
            if (customAttribute != null)
            {
                bool isGasparExport = customAttribute.AttributeType == typeof(Gaspar.ExportForAttribute);
                if (isGasparExport
                    && customAttribute.ConstructorArguments.Count == 1
                    && Int32.TryParse(customAttribute.ConstructorArguments[0].Value?.ToString(), out int argValue)
                    && argValue == (int)gasparType)
                {
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