using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class WhenAnyThrottledViewModel : WhenAnyViewModel
    {
        public WhenAnyThrottledViewModel() : this(null) { }
        public WhenAnyThrottledViewModel(IPathService pathService = null) : base(pathService) { }

        protected override async Task<string> ExecuteInternal()
        {
            var filePaths = new Queue<string>(FileRetriever.GetSortedFilePathsRecursively(PathService.Source));
            var copyTasks = CreateFixedBatchOfCopyTasks(filePaths);

            var finishedTask = Task.FromResult(string.Empty);
            while (copyTasks.Any())
            {
                finishedTask = await Task.WhenAny(copyTasks);

                if (CancellationTokenSource.IsCancellationRequested)
                    break;

                copyTasks.Remove(finishedTask);

                if (filePaths.Any())
                    copyTasks.Add(BuildNextTask(filePaths.Dequeue()));
            }

            //Return the result of the last task to finish.
            //NB: awaiting does not wrap the OperationCanceledException in an AggreagateException,
            //while calling finishedTask.Result does so, and prevents the error handling on OCE in the base class from running
            return await finishedTask;
        }
    }
}
