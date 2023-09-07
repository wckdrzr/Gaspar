using System.Collections.Generic;

namespace WCKDRZR.Gaspar.Models
{
    internal class EnumModel
    {
        public required string Identifier { get; set; }
        public required Dictionary<string, object?> Values { get; set; }

        public OutputType ExportFor { get; set; }
    }
}
