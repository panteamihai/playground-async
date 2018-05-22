using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AsyncWorkshop.UsagePatterns
{
    public class UsagePatternsViewModel : INotifyPropertyChanged, IPlayableViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SequentialRelayCommandAsync _executeWhenAllCommand;

        private readonly Subject<string> _play = new Subject<string>();
        private readonly Subject<decimal> _progressPercent = new Subject<decimal>();
        private readonly ConcurrentDictionary<string, ValueTuple<decimal, bool>> _cumulatedProgressByFileName = new ConcurrentDictionary<string, ValueTuple<decimal, bool>>();

        private string _mediaSourcePath = @"D:\Projects - Extra\Workshop\workshop-async\media";
        private int _whenAllProgress;
        private decimal _cummulatedWhenAllProgress;

        private ConcurrentBag<IProgress<ValueTuple<string, decimal, bool>>> _whenAllProgresses;

        public ICommand ExecuteWhenAllCommand =>
            _executeWhenAllCommand ?? (_executeWhenAllCommand = new SequentialRelayCommandAsync(ExecuteWhenAll, CanExecuteWhenAll));

        public string MediaSourcePath
        {
            get => _mediaSourcePath;
            set
            {
                _mediaSourcePath = value;
                OnPropertyChanged();
            }
        }

        public string MediaDestinationPath { get; }

        public int WhenAllProgress
        {
            get => _whenAllProgress;
            private set
            {
                _whenAllProgress = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ProgressReporting { get; } = new ObservableCollection<string>();

        public IObservable<string> PlaySignals => _play.AsObservable();

        public UsagePatternsViewModel()
        {
            _progressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAllProgress = (int)p);

            MediaDestinationPath = Path.Combine(Path.GetTempPath(), "CopiedMediaForAsyncWorkshop");
            if (!Directory.Exists(MediaDestinationPath))
                Directory.CreateDirectory(MediaDestinationPath);
            else
                ClearOutDestinationFolder();
        }

        private bool CanExecuteWhenAll()
        {
            return !string.IsNullOrEmpty(_mediaSourcePath);
        }

        private async Task ExecuteWhenAll()
        {
            ClearOutDestinationFolder();
            Directory.CreateDirectory(MediaDestinationPath);

            var filePaths = FileRetriever.GetFilePathsRecursively(_mediaSourcePath);
            _whenAllProgresses = new ConcurrentBag<IProgress<ValueTuple<string, decimal, bool>>>();

            var copyTasks = filePaths.Select(f =>
            {
                var progress = new Progress<ValueTuple<string, decimal, bool>>(ReportProgress);
                _whenAllProgresses.Add(progress);
                return FileCopier.CopyFileAsync(f, MediaDestinationPath, progress);
            }).ToArray();
            var copiedFilePaths = await Task.WhenAll(copyTasks);

            var currentFileBeingPlayed = copiedFilePaths.First(name => name.EndsWith(".mp3"));
            var fileInfo = new FileInfo(currentFileBeingPlayed);

            ProgressReporting.Add("Playing: " + fileInfo.Name);
            _play.OnNext(currentFileBeingPlayed);
        }

        private void ReportProgress((string FileName, decimal FilePercentageCompleteIncrement, bool Finished) progress)
        {
            _cumulatedProgressByFileName.AddOrUpdate(
                progress.FileName,
                (progress.FilePercentageCompleteIncrement, false),
                (key, existing) => (existing.Item1 + progress.FilePercentageCompleteIncrement, progress.Finished));

            _cummulatedWhenAllProgress += progress.FilePercentageCompleteIncrement;

            var allFinished = _cumulatedProgressByFileName.All(p => p.Value.Item2);
            _progressPercent.OnNext(allFinished ? 100 : _cummulatedWhenAllProgress / _whenAllProgresses.Count);

            var progressOnFileName = _cumulatedProgressByFileName[progress.FileName];
            var newProgressInfoForFile =
                (progress.Finished
                    ? "Finished downloading: "
                    : $"Downloading ({progressOnFileName.Item1:N1} %): ") + progress.FileName;

            var existingProgressInfoForFile = ProgressReporting.FirstOrDefault(f => f.Contains(progress.FileName));
            if (existingProgressInfoForFile != null)
            {
                var indexOfEntry = ProgressReporting.IndexOf(existingProgressInfoForFile);
                ProgressReporting.RemoveAt(indexOfEntry);
                ProgressReporting.Insert(indexOfEntry, newProgressInfoForFile);
            }
            else
            {
                ProgressReporting.Add(newProgressInfoForFile);
            }
        }

        private void ClearOutDestinationFolder()
        {
            if (!string.IsNullOrEmpty(MediaDestinationPath) && Directory.Exists(MediaDestinationPath))
            {
                foreach (var file in Directory.GetFiles(MediaDestinationPath))
                {
                    File.Delete(file);
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
