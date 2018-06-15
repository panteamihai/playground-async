using System;
using System.Collections.Generic;
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

using AsyncWorkshop.UsagePatterns.Commands;

using CumulatedProgressByFile = System.Collections.Concurrent.ConcurrentDictionary<System.IO.FileInfo, (decimal percentage, bool hasFinished)>;
using FileCopyProgress = System.Progress<(System.IO.FileInfo fileInfo, decimal percentage, bool hasFinished)>;

namespace AsyncWorkshop.UsagePatterns
{
    public class UsagePatternsViewModel : INotifyPropertyChanged, IPlayableViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private const int BatchLevel = 4;

        private SequentialRelayCommandAsync _executeWhenAllCommand;
        private SequentialBoundRelayCommand _cancelWhenAllCommand;
        private SequentialRelayCommandAsync _executeWhenAnyThrottledCommand;
        private SequentialBoundRelayCommand _cancelWhenAnyThrottledCommand;

        private readonly Subject<string> _play = new Subject<string>();
        private readonly Subject<decimal> _overallProgressPercent = new Subject<decimal>();
        private readonly Subject<decimal> _1ProgressPercent = new Subject<decimal>();
        private readonly Subject<decimal> _2ProgressPercent = new Subject<decimal>();
        private readonly Subject<decimal> _3ProgressPercent = new Subject<decimal>();
        private readonly Subject<decimal> _4ProgressPercent = new Subject<decimal>();

        private readonly CancellationTokenSource _whenAllCancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _whenAnyThrottledCancellationTokenSource = new CancellationTokenSource();

        private readonly CumulatedProgressByFile _cumulatedProgressByFile = new CumulatedProgressByFile();

        public ICommand ExecuteWhenAllCommand =>
            _executeWhenAllCommand ?? (_executeWhenAllCommand = new SequentialRelayCommandAsync(ExecuteWhenAll, CanExecuteWhenAll));

        public ICommand CancelWhenAllCommand =>
            _cancelWhenAllCommand ?? (_cancelWhenAllCommand = new SequentialBoundRelayCommand(ExecuteWhenAllCommand, CancelWhenAll));

        public ICommand ExecuteWhenAnyThrottledCommand =>
            _executeWhenAnyThrottledCommand ?? (_executeWhenAnyThrottledCommand = new SequentialRelayCommandAsync(ExecuteWhenAnyThrottled, CanExecuteWhenAnyThrottled));

        public ICommand CancelWhenAnyThrottledCommand =>
            _cancelWhenAnyThrottledCommand ?? (_cancelWhenAnyThrottledCommand = new SequentialBoundRelayCommand(ExecuteWhenAnyThrottledCommand, CancelWhenAnyThrottled));

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

        private int _whenAnyThrottled1ProgressPercentage;
        public int WhenAnyThrottled1ProgressPercentage
        {
            get => _whenAnyThrottled1ProgressPercentage;
            private set
            {
                _whenAnyThrottled1ProgressPercentage = value;
                OnPropertyChanged();
            }
        }

        private int _whenAnyThrottled2ProgressPercentage;
        public int WhenAnyThrottled2ProgressPercentage
        {
            get => _whenAnyThrottled2ProgressPercentage;
            private set
            {
                _whenAnyThrottled2ProgressPercentage = value;
                OnPropertyChanged();
            }
        }

        private int _whenAnyThrottled3ProgressPercentage;
        public int WhenAnyThrottled3ProgressPercentage
        {
            get => _whenAnyThrottled3ProgressPercentage;
            private set
            {
                _whenAnyThrottled3ProgressPercentage = value;
                OnPropertyChanged();
            }
        }

        private int _whenAnyThrottled4ProgressPercentage;
        public int WhenAnyThrottled4ProgressPercentage
        {
            get => _whenAnyThrottled4ProgressPercentage;
            private set
            {
                _whenAnyThrottled4ProgressPercentage = value;
                OnPropertyChanged();
            }
        }

        private List<(string filePath, int index, bool finished, decimal percent)> _currentlyReportingFiles = new List<(string filePath, int index, bool finished, decimal percent)>();

        public ObservableCollection<string> FileProgressInformation { get; } = new ObservableCollection<string>();

        public IObservable<string> PlaySignals => _play.AsObservable();

        public UsagePatternsViewModel()
        {
            _overallProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAllProgressPercentage = (int)p);
            _1ProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAnyThrottled1ProgressPercentage = (int)p);
            _2ProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAnyThrottled2ProgressPercentage = (int)p);
            _3ProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAnyThrottled3ProgressPercentage = (int)p);
            _4ProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAnyThrottled4ProgressPercentage = (int)p);

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

        private bool CanExecuteWhenAnyThrottled()
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
                    var progress = new FileCopyProgress(ReportProgressWhenAll);
                    return FileCopier.CopyFileAsync(filePath, MediaDestinationPath, progress, _whenAllCancellationTokenSource.Token);
                }).ToArray();

            var copiedFilePaths = await Task.WhenAll(copyTasks);

            var currentFileBeingPlayed = copiedFilePaths.First(name => name.EndsWith(".mp3"));
            FileProgressInformation.Add("Playing: " + new FileInfo(currentFileBeingPlayed).Name);
            _play.OnNext(currentFileBeingPlayed);
        }

        private async Task ExecuteWhenAnyThrottled()
        {
            try
            {
                await ExecuteWhenAnyThrottledInternal();
            }
            catch (OperationCanceledException)
            {
                FileProgressInformation.Clear();
                FileProgressInformation.Add("You've canceled the task, and you're now left with the following fully copied files:");
                var alreadyCopiedFilePaths = FileRetriever.GetFilePathsRecursively(MediaDestinationPath);
                alreadyCopiedFilePaths.ForEach(path => FileProgressInformation.Add(new FileInfo(path).Name));
            }
        }

        private async Task ExecuteWhenAnyThrottledInternal()
        {
            Task<string> BuildTask(string filePath)
            {
                var index = _currentlyReportingFiles.Count;
                if (_currentlyReportingFiles.Count == 4)
                {
                    var removable = _currentlyReportingFiles.Single(rf => rf.finished);
                    index = removable.index;
                    _currentlyReportingFiles.Remove(removable);
                }

                _currentlyReportingFiles.Add((filePath, index, false, 0));

                var progress = new FileCopyProgress(ReportProgressWhenAnyThrottled);
                return FileCopier.CopyFileAsync(filePath, MediaDestinationPath, progress, _whenAllCancellationTokenSource.Token);
            }

            ClearOutDestinationFolder();
            Directory.CreateDirectory(MediaDestinationPath);

            var filePaths = new Queue<string>(FileRetriever.GetSortedFilePathsRecursively(_mediaSourcePath));
            var copyTasks = filePaths.Select(BuildTask)
                                     .Take(BatchLevel)
                                     .ToList();

            var finishedTask = Task.FromResult(string.Empty);
            while (copyTasks.Any())
            {
                finishedTask = await Task.WhenAny(copyTasks);
                copyTasks.Remove(finishedTask);

                if(filePaths.Any())
                    copyTasks.Add(BuildTask(filePaths.Dequeue()));
            }

            var currentFileBeingPlayed = finishedTask.Result;
            FileProgressInformation.Add("Playing: " + new FileInfo(currentFileBeingPlayed).Name);
            _play.OnNext(currentFileBeingPlayed);
        }

        private void CancelWhenAll()
        {
            _whenAllCancellationTokenSource.Cancel();
        }

        private void CancelWhenAnyThrottled()
        {
            _whenAnyThrottledCancellationTokenSource.Cancel();
        }

        private void ReportProgressWhenAll((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress)
        {
            _cumulatedProgressByFile.AddOrUpdate(
                progress.fileInfo,
                (progress.filePercentageCompleteIncrement, false),
                (key, existing) => (existing.percentage + progress.filePercentageCompleteIncrement, progress.finished));

            var allFinished = _cumulatedProgressByFile.All(kv => kv.Value.hasFinished);
            var value = allFinished ? 100 : _cumulatedProgressByFile.Average(kv => kv.Value.percentage);
            UpdateWhenAllProgress(value);

            UpdateFileProgressInformation(progress);
        }

        private void UpdateWhenAllProgress(decimal value)
        {
            _overallProgressPercent.OnNext(value);
        }

        private void ReportProgressWhenAnyThrottled((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress)
        {
            _cumulatedProgressByFile.AddOrUpdate(
                progress.fileInfo,
                (progress.filePercentageCompleteIncrement, false),
                (key, existing) => (existing.percentage + progress.filePercentageCompleteIncrement, progress.finished));

            var value = progress.finished ? 100 : _cumulatedProgressByFile.Average(kv => kv.Value.percentage);
            UpdateWhenAnyThrottledProgress(progress.fileInfo, value);

            UpdateFileProgressInformation(progress);
        }

        private void UpdateWhenAnyThrottledProgress(FileInfo fileInfo, decimal value)
        {
            var reportingFile = _currentlyReportingFiles.Single(t => t.filePath == fileInfo.FullName);
            var index = reportingFile.index;

            switch (index + 1)
            {
                case 1:
                    _1ProgressPercent.OnNext(value);
                    break;
                case 2:
                    _2ProgressPercent.OnNext(value);
                    break;
                case 3:
                    _3ProgressPercent.OnNext(value);
                    break;
                case 4:
                    _4ProgressPercent.OnNext(value);
                    break;
            }

            if (value == 100)
                reportingFile.finished = true;

            _currentlyReportingFiles.Remove(reportingFile);
            _currentlyReportingFiles.Add((reportingFile.filePath, reportingFile.index, reportingFile.finished, value));
        }

        private void UpdateFileProgressInformation((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress)
        {
            var fileProgress = _cumulatedProgressByFile[progress.fileInfo];
            var currentFileProgressInformation =
                (progress.finished
                    ? "Finished downloading: "
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
