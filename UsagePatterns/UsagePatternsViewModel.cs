using AsyncWorkshop.UsagePatterns.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using CumulatedProgressByFile = System.Collections.Concurrent.ConcurrentDictionary<System.IO.FileInfo, (decimal percentage, bool hasFinished)>;
using FileCopyProgress = System.Progress<(System.IO.FileInfo fileInfo, decimal percentage, bool hasFinished)>;

namespace AsyncWorkshop.UsagePatterns
{
    public class UsagePatternsViewModel : INotifyPropertyChanged, IPlayableViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SequentialRelayCommandAsync _executeWhenAllCommand;
        private SequentialBoundRelayCommand _cancelWhenAllCommand;

        private readonly Subject<string> _play = new Subject<string>();
        private readonly Subject<decimal> _overallProgressPercent = new Subject<decimal>();

        private readonly CancellationTokenSource _whenAllCancellationTokenSource = new CancellationTokenSource();
        private readonly CumulatedProgressByFile _cumulatedProgressByFile = new CumulatedProgressByFile();

        public ICommand ExecuteWhenAllCommand =>
            _executeWhenAllCommand ?? (_executeWhenAllCommand = new SequentialRelayCommandAsync(ExecuteWhenAll, CanExecuteWhenAll));

        public ICommand CancelWhenAllCommand =>
            _cancelWhenAllCommand ?? (_cancelWhenAllCommand = new SequentialBoundRelayCommand(ExecuteWhenAllCommand, CancelWhenAll));

        private string _mediaSourcePath = @"D:\Projects - Extra\Workshop\workshop-async\media";
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

        private int _whenAllProgressPercentage;
        public int WhenAllProgressPercentage
        {
            get => _whenAllProgressPercentage;
            private set
            {
                _whenAllProgressPercentage = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> FileProgressInformation { get; } = new ObservableCollection<string>();

        public IObservable<string> PlaySignals => _play.AsObservable();

        public UsagePatternsViewModel()
        {
            _overallProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAllProgressPercentage = (int)p);

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
            try
            {
                await ExecuteWhenAllInternal();
            }
            catch (OperationCanceledException)
            {
                FileProgressInformation.Clear();
                FileProgressInformation.Add("You've canceled the task, and you're now left with the following fully copied files:");
                var alreadyCopiedFilePaths = FileRetriever.GetFilePathsRecursively(MediaDestinationPath);
                alreadyCopiedFilePaths.ForEach(path => FileProgressInformation.Add(new FileInfo(path).Name));
            }
        }

        private async Task ExecuteWhenAllInternal()
        {
            ClearOutDestinationFolder();
            Directory.CreateDirectory(MediaDestinationPath);

            var filePaths = FileRetriever.GetFilePathsRecursively(_mediaSourcePath);
            var copyTasks = filePaths.Select(
                filePath =>
                {
                    var progress = new FileCopyProgress(ReportProgress);
                    return FileCopier.CopyFileAsync(filePath, MediaDestinationPath, progress, _whenAllCancellationTokenSource.Token);
                }).ToArray();

            var copiedFilePaths = await Task.WhenAll(copyTasks);

            var currentFileBeingPlayed = copiedFilePaths.First(name => name.EndsWith(".mp3"));
            FileProgressInformation.Add("Playing: " + new FileInfo(currentFileBeingPlayed).Name);
            _play.OnNext(currentFileBeingPlayed);
        }

        private void CancelWhenAll()
        {
            _whenAllCancellationTokenSource.Cancel();
        }

        private void ReportProgress((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress)
        {
            _cumulatedProgressByFile.AddOrUpdate(
                progress.fileInfo,
                (progress.filePercentageCompleteIncrement, false),
                (key, existing) => (existing.percentage + progress.filePercentageCompleteIncrement, progress.finished));

            var allFinished = _cumulatedProgressByFile.All(kv => kv.Value.hasFinished);
            _overallProgressPercent.OnNext(allFinished ? 100 : _cumulatedProgressByFile.Average(kv => kv.Value.percentage));

            var fileProgress = _cumulatedProgressByFile[progress.fileInfo];
            var currentFileProgressInformation =
                (progress.finished ? "Finished downloading: "
                                   : $"Downloading ({fileProgress.percentage:N1} %): ")
                + progress.fileInfo.Name;

            for (var i = 0; i < FileProgressInformation.Count; i++)
            {
                var fileProgressInformation = FileProgressInformation[i];
                if (fileProgressInformation.Contains(progress.fileInfo.Name))
                {
                    FileProgressInformation.RemoveAt(i);
                    FileProgressInformation.Insert(i, currentFileProgressInformation);
                    return;
                }
            }

            FileProgressInformation.Add(currentFileProgressInformation);
        }

        private void ClearOutDestinationFolder()
        {
            if (string.IsNullOrEmpty(MediaDestinationPath) || !Directory.Exists(MediaDestinationPath))
                return;

            foreach (var file in Directory.GetFiles(MediaDestinationPath))
            {
                File.Delete(file);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
