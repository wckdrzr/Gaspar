using WCKDRZR.CSharpExporter.Core;

namespace WCKDRZR.CSharpExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            string configFile = args.Length > 0 ? args[0] : null;
            //configFile = "../../../../../csharp-exporter.config.json"; //test file

            Exporter.Export(configFile);
        }
    }
}