using System;
using System.IO;
using System.Threading.Tasks;

namespace AsyncWorkshop.UsagePatterns
{
    public static class FileCopier
    {
        public static async Task<string> CopyFileAsync(
            string sourceFilePath,
            string destinationFolderPath,
            IProgress<ValueTuple<string, int>> progress)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var copiedFileName = Path.Combine(destinationFolderPath, fileName);
            var tempFileName = copiedFileName + ".tmp";

            var fileInfo = new FileInfo(sourceFilePath);
            var fileLength = fileInfo.Length;

            var buffer = new byte[4096 * 16];

            using (var sourceStream = File.OpenRead(sourceFilePath))
            using (var destinationStream = File.Create(tempFileName))
            {
                int bytesRead;
                int alreadyReadCount = 0;
                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    alreadyReadCount += bytesRead;
                    await destinationStream.WriteAsync(buffer, 0, buffer.Length);

                    var progressPercentage = (int)((decimal)alreadyReadCount / fileLength * 100);
                    progress.Report((fileName, progressPercentage));
                }
            }

            File.Move(tempFileName, copiedFileName);

            return copiedFileName;
        }
    }
}
