using WCKDRZR.Gaspar.Core;
using Microsoft.Build.Utilities;

namespace WCKDRZR.Gaspar.MSBuild
{
    public class OnBuild : Task
    {
        public required string ConfigFile { get; set; }

        public override bool Execute()
        {
            Exporter.Export(ConfigFile);
            return true;
        }
    }
}
