﻿using System.Collections.Generic;
using System.IO;

namespace AsyncWorkshop.UsagePatterns
{
    public static class FileRetriever
    {
        public static List<string> GetFilePathsRecursively(string folderPath)
        {
            var fileNames = new List<string>();
            foreach (var folder in Directory.EnumerateDirectories(folderPath))
            {
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    var extension = Path.GetExtension(file);
                    if (extension != ".png" ||
                        extension == ".png" && !fileNames.Exists(f => f.EndsWith(".png")))
                        fileNames.Add(file);
                }
            }
            return fileNames;
        }
    }
}