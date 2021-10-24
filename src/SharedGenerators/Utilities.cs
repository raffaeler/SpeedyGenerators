using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis;

namespace SpeedyGenerators
{
    internal class Utilities
    {
        public List<FileInfo> GetFilesByPrefix(IEnumerable<AdditionalText> additionalFiles,
            string prefix)
        {
            var files = new List<FileInfo>();
            foreach (var additional in additionalFiles)
            {
                var fi = new FileInfo(additional.Path);
                var name = fi.Name;
                if (!name.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                files.Add(fi);
            }

            return files;
        }



    }
}
