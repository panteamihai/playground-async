using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class WhenAnyEarlyBailoutViewModel : WhenAnyViewModel
    {
        public WhenAnyEarlyBailoutViewModel() : this(null) { }
        public WhenAnyEarlyBailoutViewModel(IPathService pathService = null) : base(pathService) { }

        protected override async Task<string> ExecuteInternal()
        {
            var filePaths = new Queue<string>(FileRetriever.GetSortedFilePathsRecursively(PathService.Source)
                                                           .Where(fp => fp.Contains(".m4a"))
                                                           .OrderBy(fp => fp.Contains(".md5") ? 0 : 1));
            var copyTasks = CreateFixedBatchOfCopyTasks(filePaths);

            var path = string.Empty;
            while (copyTasks.Any())
            {
                var finishedTask = await Task.WhenAny(copyTasks);

                if (CancellationTokenSource.IsCancellationRequested)
                    break;

                path = await finishedTask;
                if (!IsValidMd5(path))
                {
                    CancellationTokenSource.Cancel();
                    throw new FileFormatException($"Invalid MD5 for {path}");
                }

                copyTasks.Remove(finishedTask);

                if (filePaths.Any())
                    copyTasks.Add(BuildNextTask(filePaths.Dequeue()));
            }

            //Return the result of the last task to finish.
            //NB: awaiting does not wrap the OperationCanceledException in an AggreagateException,
            //while calling finishedTask.Result does so, and prevents the error handling on OCE in the base class from running
            return path;
        }

        private static bool IsValidMd5(string path)
        {
            if (path.EndsWith(".md5"))
                return true;

            var md5File = path + ".md5";
            if (!File.Exists(md5File))
                throw new InvalidOperationException($"No MD5 file was found for {path}");

            var fileMd5 = File.ReadAllText(md5File);
            var computedMd5 = FileHasher.CalculateMd5(path);

            return fileMd5 == computedMd5;
        }
    }
}
