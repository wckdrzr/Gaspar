using System.Collections.Generic;

namespace WCKDRZR.CSharpExporter.Models
{
    public class EnumModel
    {
        public string Identifier { get; set; }
        public Dictionary<string, object> Values { get; set; }
    }
}
