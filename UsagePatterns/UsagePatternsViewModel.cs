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

        private readonly object _locker = new object();

        private SequentialRelayCommandAsync _executeWhenAllCommand;

        private readonly Subject<string> _play = new Subject<string>();
        private readonly Subject<int> _progressPercent = new Subject<int>();

        private string _mediaSourcePath = @"D:\Projects - Extra\Workshop\workshop-async\media";
        private readonly string _mediaDestinationPath;

        private int _whenAllProgress;
        private int _cummulatedWhenAllProgress;
        private ConcurrentBag<IProgress<ValueTuple<string, int>>> _whenAllProgresses;

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

        public string MediaDestinationPath => _mediaDestinationPath;

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
            _progressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAllProgress = p);

            _mediaDestinationPath = Path.Combine(Path.GetTempPath(), "CopiedMediaForAsyncWorkshop");
            if (!Directory.Exists(_mediaDestinationPath))
                Directory.CreateDirectory(_mediaDestinationPath);
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
            Directory.CreateDirectory(_mediaDestinationPath);

            var filePaths = FileRetriever.GetFilePathsRecursively(_mediaSourcePath);
            _whenAllProgresses = new ConcurrentBag<IProgress<ValueTuple<string, int>>>();

            var copyTasks = filePaths.Select(f =>
            {
                var progress = new Progress<ValueTuple<string, int>>(ReportProgress);
                _whenAllProgresses.Add(progress);
                return FileCopier.CopyFileAsync(f, _mediaDestinationPath, progress);
            }).ToArray();
            var copiedFilePaths = await Task.WhenAll(copyTasks);

            var currentFileBeingPlayed = copiedFilePaths.First(name => name.EndsWith(".mp3"));
            var fileInfo = new FileInfo(currentFileBeingPlayed);

            ProgressReporting.Add("Playing: " + fileInfo.Name);
            _play.OnNext(currentFileBeingPlayed);
        }

        private void ReportProgress((string FileName, int FilePercentageComplete) progress)
        {
            _cummulatedWhenAllProgress += progress.FilePercentageComplete;
            _progressPercent.OnNext(_cummulatedWhenAllProgress / _whenAllProgresses.Count);

            ProgressReporting.Add(
                (progress.FilePercentageComplete == 100
                    ? "Finished downloading: "
                    : $"Downloading ({progress.FilePercentageComplete} %): ")
                + progress.FileName);
        }

        private void ClearOutDestinationFolder()
        {
            if (!string.IsNullOrEmpty(_mediaDestinationPath) && Directory.Exists(_mediaDestinationPath))
            {
                foreach (var file in Directory.GetFiles(_mediaDestinationPath))
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
