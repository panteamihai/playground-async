using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CurrentlyReportingFiles = System.Collections.Concurrent.ConcurrentDictionary<string, (int index, bool hasFinished, decimal percent)>;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class WhenAnyThrottledViewModel : OperationViewModel, INotifyPropertyChanged
    {
        private const int BatchLevel = 4;

        private int _firstProgressPercentage;
        private int _secondProgressPercentage;
        private int _thirdProgressPercentage;
        private int _fourthProgressPercentage;

        private string _firstBlock;
        private string _secondBlock;
        private string _thirdBlock;
        private string _fourthBlock;

        private readonly CurrentlyReportingFiles _currentlyReportingFiles = new CurrentlyReportingFiles();

        public int FirstProgressPercentage
        {
            get => _firstProgressPercentage;
            private set
            {
                _firstProgressPercentage = value;
                OnPropertyChanged();
            }
        }
        public int SecondProgressPercentage
        {
            get => _secondProgressPercentage;
            private set
            {
                _secondProgressPercentage = value;
                OnPropertyChanged();
            }
        }
        public int ThirdProgressPercentage
        {
            get => _thirdProgressPercentage;
            private set
            {
                _thirdProgressPercentage = value;
                OnPropertyChanged();
            }
        }
        public int FourthProgressPercentage
        {
            get => _fourthProgressPercentage;
            private set
            {
                _fourthProgressPercentage = value;
                OnPropertyChanged();
            }
        }

        public string FirstBlock
        {
            get => _firstBlock;
            set
            {
                _firstBlock = value;
                OnPropertyChanged();
            }
        }

        public string SecondBlock
        {
            get => _secondBlock;
            set
            {
                _secondBlock = value;
                OnPropertyChanged();
            }
        }

        public string ThirdBlock
        {
            get => _thirdBlock;
            set
            {
                _thirdBlock = value;
                OnPropertyChanged();
            }
        }

        public string FourthBlock
        {
            get => _fourthBlock;
            set
            {
                _fourthBlock = value;
                OnPropertyChanged();
            }
        }

        public WhenAnyThrottledViewModel() : this(null) { }

        public WhenAnyThrottledViewModel(IPathService pathService = null) : base(pathService) { }

        protected override async Task<string> ExecuteInternal()
        {
            var filePaths = new Queue<string>(FileRetriever.GetSortedFilePathsRecursively(PathService.Source));
            var copyTasks = filePaths.Select((fp, i) =>
                {
                    _currentlyReportingFiles.TryAdd(fp, (i, hasFinished: false, percent: 0));
                    return BuildTask(fp);
                })
                .Take(BatchLevel)
                .ToList();

            var finishedTask = Task.FromResult(string.Empty);
            while (copyTasks.Any())
            {
                finishedTask = await Task.WhenAny(copyTasks);

                if (finishedTask.IsCanceled)
                    throw new TaskCanceledException();

                copyTasks.Remove(finishedTask);

                if (filePaths.Any())
                    copyTasks.Add(BuildNextTask(filePaths.Dequeue()));
            }

            //Last task to finish
            return finishedTask.Result;
        }

        private Task<string> BuildNextTask(string filePath)
        {
            var index = _currentlyReportingFiles.Count - 1;
            if (_currentlyReportingFiles.Count == BatchLevel)
            {
                var finished = _currentlyReportingFiles.Single(kv => kv.Value.hasFinished);
                index = finished.Value.index;
                _currentlyReportingFiles.TryRemove(finished.Key, out _);
            }

            _currentlyReportingFiles.TryAdd(filePath, (index, hasFinished: false, percent: 0));

            return BuildTask(filePath);
        }

        protected override void ReportProgressInternal((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress)
        {
            var value = progress.finished ? 100 : CumulatedProgressByFile[progress.fileInfo].percentage;
            UpdateProgress(progress.fileInfo, value);
        }

        protected override void ClearProgressInternal()
        {
            _currentlyReportingFiles.Clear();
            FirstProgressPercentage = 0;
            SecondProgressPercentage = 0;
            ThirdProgressPercentage = 0;
            FourthProgressPercentage = 0;
        }

        private void UpdateProgress(FileInfo fileInfo, decimal value)
        {
            var reportingFile = _currentlyReportingFiles[fileInfo.FullName];
            var index = reportingFile.index;

            switch (index + 1)
            {
                case 1:
                    FirstProgressPercentage = (int)value;
                    FirstBlock = fileInfo.Name;
                    break;
                case 2:
                    SecondProgressPercentage = (int)value;
                    SecondBlock = fileInfo.Name;
                    break;
                case 3:
                    ThirdProgressPercentage = (int)value;
                    ThirdBlock = fileInfo.Name;
                    break;
                case 4:
                    FourthProgressPercentage = (int)value;
                    FourthBlock = fileInfo.Name;
                    break;
            }

            if (value == 100)
                reportingFile.hasFinished = true;

            _currentlyReportingFiles.TryRemove(fileInfo.FullName, out _);
            _currentlyReportingFiles.TryAdd(fileInfo.FullName, (reportingFile.index, reportingFile.hasFinished, value));
        }
    }
}
