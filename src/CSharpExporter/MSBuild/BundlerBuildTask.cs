using System;
using System.IO;
using WCKDRZR.CSharpExporter.Core;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace WCKDRZR.CSharpExporter.MSBuild
{
    public class OnBuild : Task
    {
        public string ConfigFile { get; set; }

        public override bool Execute()
        {
            Exporter.Export(ConfigFile);
            return true;
        }
    }
}
