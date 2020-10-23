using AsyncWorkshop.UsagePatterns.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CurrentlyReportingFiles = System.Collections.Concurrent.ConcurrentDictionary<string, int>;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public abstract class WhenAnyViewWithoutReportingModel : OperationViewModel, INotifyPropertyChanged
    {
        protected readonly CurrentlyReportingFiles CurrentlyReportingFiles = new CurrentlyReportingFiles();

        private const int BatchLevel = 4;

        private int _firstProgressPercentage;
        private int _secondProgressPercentage;
        private int _thirdProgressPercentage;
        private int _fourthProgressPercentage;

        private string _firstBlock;
        private string _secondBlock;
        private string _thirdBlock;
        private string _fourthBlock;

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

        protected WhenAnyViewWithoutReportingModel(IPathService pathService = null) : base(pathService) { }

        protected override void ReportProgressInternal((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress)
        {
            var value = progress.finished ? 100 : CumulatedProgressByFile[progress.fileInfo].percentage;
            UpdateProgress(progress.fileInfo, value);
        }

        protected override void ClearProgressInternal()
        {
            CurrentlyReportingFiles.Clear();
            FirstProgressPercentage = 0;
            SecondProgressPercentage = 0;
            ThirdProgressPercentage = 0;
            FourthProgressPercentage = 0;
        }

        private void UpdateProgress(FileInfo fileInfo, decimal value)
        {
            var index = CurrentlyReportingFiles[fileInfo.FullName];

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

            CurrentlyReportingFiles.TryRemove(fileInfo.FullName, out _);
            CurrentlyReportingFiles.TryAdd(fileInfo.FullName, index);
        }

        protected List<Task<string>> CreateFixedBatchOfCopyTasks(Queue<string> filePaths)
        {
            return Enumerable.Range(0, BatchLevel)
                .SelectMany(i =>
                {
                    if (!filePaths.Any())
                        return Enumerable.Empty<Task<string>>();

                    var fp = filePaths.Dequeue();
                    CurrentlyReportingFiles.TryAdd(fp, i);
                    return new[] { BuildTask(fp) };

                })
                .ToList();
        }

        protected Task<string> BuildNextTask(string filePath)
        {
            var index = CurrentlyReportingFiles.Count - 1;
            if (CurrentlyReportingFiles.Count == BatchLevel)
            {
                var finished = CurrentlyReportingFiles.First(kv => kv.Value.hasFinished);
                index = finished.Value.index;
                CurrentlyReportingFiles.TryRemove(finished.Key, out _);
            }

            CurrentlyReportingFiles.TryAdd(filePath, (index, hasFinished: false, percent: 0));

            return BuildTask(filePath);
        }
    }
}
