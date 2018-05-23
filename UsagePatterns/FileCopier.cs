using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using IFileCopyProgress = System.IProgress<(System.IO.FileInfo fileInfo, decimal percentage, bool hasFinished)>;

namespace AsyncWorkshop.UsagePatterns
{
    public static class FileCopier
    {
        public static async Task<string> CopyFileAsync(
            string sourceFilePath,
            string destinationFolderPath,
            IFileCopyProgress progress,
            CancellationToken token)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            var copiedFilePath = Path.Combine(destinationFolderPath, fileName);
            var tempFilePath = copiedFilePath + ".tmp";

            var fileInfo = new FileInfo(sourceFilePath);
            var fileLength = fileInfo.Length;

            var buffer = new byte[4096 * 16];

            using (var sourceStream = File.OpenRead(sourceFilePath))
            using (var destinationStream = File.Create(tempFilePath))
            {
                try
                {
                    int bytesRead;
                    var alreadyReadCount = 0;
                    var cummulatedProgressPercentage = 0m;

                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        alreadyReadCount += bytesRead;
                        var progressPercentageIncrement = (decimal)bytesRead / fileLength * 100;
                        cummulatedProgressPercentage += progressPercentageIncrement;

                        var isFinished = alreadyReadCount == fileLength;
                        var reportedPercentage = isFinished ? 100 - cummulatedProgressPercentage : progressPercentageIncrement;
                        progress.Report((fileInfo, reportedPercentage, isFinished));

                        await destinationStream.WriteAsync(buffer, 0, buffer.Length, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    sourceStream.Close();
                    destinationStream.Close();
                    File.Delete(tempFilePath);
                    throw;
                }
            }

            File.Move(tempFilePath, copiedFilePath);

            return copiedFilePath;
        }
    }
}
