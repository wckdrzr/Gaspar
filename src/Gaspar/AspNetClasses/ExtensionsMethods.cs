using System;
using System.Linq;
using System.Reflection;

namespace WCKDRZR.Gaspar
{
    public static class GasparExtensions
    {
        public static bool ExportsFor(this MemberInfo menber, GasparType type)
        {
            CustomAttributeData customAttribute = menber.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(Gaspar.ExportForAttribute));
            if (customAttribute != null)
            {
                bool isGasparExport = customAttribute.AttributeType == typeof(Gaspar.ExportForAttribute);
                if (isGasparExport && customAttribute.ConstructorArguments.Count == 1)
                {
                    if (Int32.TryParse(customAttribute.ConstructorArguments[0].Value.ToString(), out int argValue))
                    {
                        return argValue == (int)type;
                    }
                }
            }
            return false;
        }
    }
}