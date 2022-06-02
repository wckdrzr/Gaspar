using System;
using System.Collections.Generic;
using System.IO;
using CSharpExporter.Models;
using Ganss.IO;

namespace CSharpExporter.Helpers
{
	public static class FileHelper
	{
        public static List<string> GetFiles(ConfigurationType configuration)
        {
            List<string> fileNames = new List<string>();

            foreach (var path in ExpandGlobPatterns(configuration.Include))
            {
                fileNames.Add(path);
            }

            foreach (var path in ExpandGlobPatterns(configuration.Exclude))
            {
                fileNames.Remove(path);
            }

            return fileNames;
        }

        public static List<string> ExpandGlobPatterns(List<string> globPatterns)
        {
            List<string> fileNames = new();

            foreach (string pattern in globPatterns ?? new())
            {
                var paths = Glob.Expand(pattern);

                foreach (var path in paths)
                {
                    fileNames.Add(path.FullName);
                }
            }

            return fileNames;
        }

        public static string RelativePath(string outputPath, string fromPath)
        {
            return Path.GetRelativePath(Path.GetDirectoryName(outputPath), fromPath);
        }
    }
}

