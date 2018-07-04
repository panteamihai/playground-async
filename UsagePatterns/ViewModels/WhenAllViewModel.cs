using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class WhenAllViewModel : OperationViewModel, INotifyPropertyChanged
    {
        private readonly Subject<decimal> _overallProgressPercent = new Subject<decimal>();

        private int _progressPercentage;
        public int ProgressPercentage
        {
            get => _progressPercentage;
            private set
            {
                _progressPercentage = value;
                OnPropertyChanged();
            }
        }

        public WhenAllViewModel() : this(null) { }

        public WhenAllViewModel(IPathService pathService = null)
            : base(pathService)
        {
            _overallProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => ProgressPercentage = (int)p);
        }

        protected override async Task<string> ExecuteInternal()
        {
            var filePaths = FileRetriever.GetFilePathsRecursively(PathService.Source);
            var copyTasks = filePaths.Select(BuildTask).ToArray();

            var copiedFilePaths = await Task.WhenAll(copyTasks);

            return copiedFilePaths.First(name => name.EndsWith(".mp3"));
        }

        protected override void ReportProgressInternal((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress)
        {
            var allFinished = CumulatedProgressByFile.All(kv => kv.Value.hasFinished);
            var value = allFinished ? 100 : CumulatedProgressByFile.Average(kv => kv.Value.percentage);
            UpdateProgress(value);
        }

        protected override void ClearProgressInternal()
        {
            ProgressPercentage = 0;
        }

        private void UpdateProgress(decimal value)
        {
            _overallProgressPercent.OnNext(value);
        }
    }
}
