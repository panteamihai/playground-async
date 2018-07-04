using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class WhenAnyFirstWinsViewModel : WhenAnyViewModel
    {
        public WhenAnyFirstWinsViewModel() : this(null) { }
        public WhenAnyFirstWinsViewModel(IPathService pathService = null) : base(pathService) { }

        protected override async Task<string> ExecuteInternal()
        {
            var filePaths = new Queue<string>(FileRetriever.GetSortedFilePathsRecursively(PathService.Source)
                                                           .Where(fp => fp.Contains("Silents - Demo - 02") && !fp.EndsWith(".md5")));
            var copyTasks = CreateFixedBatchOfCopyTasks(filePaths);

            var firstTaskToFinish = Task.FromResult(string.Empty);
            while (copyTasks.Any())
            {
                firstTaskToFinish = await Task.WhenAny(copyTasks);
                CancellationTokenSource.Cancel();
                break;
            }

            //Return the result of the last task to finish.
            //NB: awaiting does not wrap the OperationCanceledException in an AggreagateException,
            //while calling finishedTask.Result does so, and prevents the error handling on OCE in the base class from running
            return await firstTaskToFinish;
        }
    }
}
