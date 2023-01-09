using WCKDRZR.Gaspar.Core;

namespace WCKDRZR.Gaspar
{
    class Program
    {
        static void Main(string[] args)
        {
            string configFile = args.Length > 0 ? args[0] : null;
            configFile = "../../../../../gaspar.demo-config.json"; //test file

            Exporter.Export(configFile);
        }
    }
}