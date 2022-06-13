using System.Collections.Generic;

namespace WCKDRZR.CSharpExporter.Models
{
    internal class EnumModel
    {
        public string Identifier { get; set; }
        public Dictionary<string, object> Values { get; set; }

        public OutputType ExportFor { get; set; }
    }
}
