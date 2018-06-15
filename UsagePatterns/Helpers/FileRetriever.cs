using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AsyncWorkshop.UsagePatterns.Helpers
{
    public static class FileRetriever
    {
        public static List<string> GetFilePathsRecursively(string folderPath)
        {
            var filePaths = new List<string>();
            foreach (var folder in Directory.EnumerateDirectories(folderPath).Union(new[] {folderPath}))
            {
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    var extension = Path.GetExtension(file);
                    if (extension != ".png" ||
                        extension == ".png" && !filePaths.Exists(f => f.EndsWith(".png")))
                        filePaths.Add(file);
                }
            }
            return filePaths;
        }

        public static List<string> GetSortedFilePathsRecursively(string folderPath)
        {
            var filePaths = GetFilePathsRecursively(folderPath);
            filePaths.Sort();

            return filePaths;
        }

        public static IEnumerable<string> GetFilePathsByPattern(string path, string searchPattern, SearchOption searchOption)
        {
            var searchPatterns = searchPattern.Split('|');
            var files = new List<string>();
            foreach (var sp in searchPatterns)
            foreach (var file in Directory.GetFiles(path, sp, searchOption))
            {
                yield return file;
            }
        }
    }
}
