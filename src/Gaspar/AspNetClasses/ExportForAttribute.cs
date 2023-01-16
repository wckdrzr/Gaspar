using System;
using WCKDRZR.Gaspar.Models;

namespace WCKDRZR.Gaspar
{
    public class ExportForAttribute : Attribute
    {
        public ExportForAttribute(GasparType types) { }

        public bool NoInheritance { get; set; }

        public string ReturnTypeOverride { get; set; }

        public string Serializer { get; set; }

        public string[] Scopes { get; set; }

        public string[] AdditionalScopes { get; set; }
    }

    public class ExportOptionsAttribute : Attribute
    {
        public bool NoInheritance { get; set; }

        public string ReturnTypeOverride { get; set; }

        public string Serializer { get; set; }

        public string[] Scopes { get; set; }

        public string[] AdditionalScopes { get; set; }
    }

    public class ExportWithoutInheritance : Attribute
    {

    }

    [Flags]
    public enum GasparType
    {
        All = 1 << 0,
        FrontEnd = 1 << 1,

        Angular = 1 << 2,
        CSharp = 1 << 3,
        Ocelot = 1 << 4,
        TypeScript = 1 << 5,
        Proto = 1 << 6,
        Python = 1 << 7,
    }
}