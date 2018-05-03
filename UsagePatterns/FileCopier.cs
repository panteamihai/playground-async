using System;
using System.IO;
using System.Threading.Tasks;

namespace AsyncWorkshop.UsagePatterns
{
    public static class FileCopier
    {
        public static async Task<String> CopyFileAsync(string sourceFilePath,
            string destinationFolderPath,
           // Label progDetails,
           // ProgressBar progBar,
            bool pauseOnCompleteToShowFullProgressBar = false)
        {
            if (pauseOnCompleteToShowFullProgressBar) await Task.Delay(1000);

            var fileName = Path.GetFileName(sourceFilePath);
            var copiedFileName = Path.Combine(destinationFolderPath, fileName);
            var tempFileName = copiedFileName + ".tmp";

            //UpdateProgess(progBar, 0, progDetails, "");
            using (var sourceStream = File.Open(sourceFilePath, FileMode.Open))
            {
                using (var destinationStream = File.Create(tempFileName))
                {
                    //UpdateProgess(progBar, 50, progDetails, "Copying " + Path.GetExtension(sourceFilePath));
                    //UpdateConsole(Console, "Copying started : {0} ", Path.GetFileName(sourceFilePath));

                    await sourceStream.CopyToAsync(destinationStream, 4096);
                }
            }
            //UpdateProgess(progBar, 100, progDetails, "Copy " + Path.GetExtension(sourceFilePath) + " complete.");
            //UpdateConsole(Console, "Copy completed : {0} ", Path.GetFileName(sourceFilePath));

            File.Move(tempFileName, copiedFileName);

            return copiedFileName;
        }
    }
}
