using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using AsyncWorkshop.UsagePatterns.Commands;
using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;

using CumulatedProgressByFile = System.Collections.Concurrent.ConcurrentDictionary<System.IO.FileInfo, (decimal percentage, bool hasFinished)>;
using FileCopyProgress = System.Progress<(System.IO.FileInfo fileInfo, decimal percentage, bool hasFinished)>;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class WhenAnyThrottledViewModel : INotifyPropertyChanged
    {
        private readonly IMediaPathService _mediaPathService;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly CumulatedProgressByFile _cumulatedProgressByFile = new CumulatedProgressByFile();

        private const int BatchLevel = 4;

        private SequentialRelayCommandAsync _executeWhenAnyThrottledCommand;
        private SequentialBoundRelayCommand _cancelWhenAnyThrottledCommand;

        private readonly Subject<string> _play = new Subject<string>();
        private readonly Subject<Notifcation> _info = new Subject<Notifcation>();

        private readonly Subject<decimal> _1ProgressPercent = new Subject<decimal>();
        private readonly Subject<decimal> _2ProgressPercent = new Subject<decimal>();
        private readonly Subject<decimal> _3ProgressPercent = new Subject<decimal>();
        private readonly Subject<decimal> _4ProgressPercent = new Subject<decimal>();

        private readonly CancellationTokenSource _whenAnyThrottledCancellationTokenSource = new CancellationTokenSource();

        private readonly List<(string filePath, int index, bool finished, decimal percent)> _currentlyReportingFiles = new List<(string filePath, int index, bool finished, decimal percent)>();

        public ICommand ExecuteWhenAnyThrottledCommand =>
            _executeWhenAnyThrottledCommand ?? (_executeWhenAnyThrottledCommand = new SequentialRelayCommandAsync(ExecuteWhenAnyThrottled, CanExecuteWhenAnyThrottled));
        public ICommand CancelWhenAnyThrottledCommand =>
            _cancelWhenAnyThrottledCommand ?? (_cancelWhenAnyThrottledCommand = new SequentialBoundRelayCommand(ExecuteWhenAnyThrottledCommand, CancelWhenAnyThrottled));

        public IObservable<Notifcation> Info => _info.AsObservable();
        public IObservable<string> PlaySignals => _play.AsObservable();

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

        public WhenAnyThrottledViewModel() : this(null) { }

        public WhenAnyThrottledViewModel(IMediaPathService mediaPathService = null)
        {
            _mediaPathService = mediaPathService ?? new MediaPathService();

            _1ProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAnyThrottled1ProgressPercentage = (int)p);
            _2ProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAnyThrottled2ProgressPercentage = (int)p);
            _3ProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAnyThrottled3ProgressPercentage = (int)p);
            _4ProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAnyThrottled4ProgressPercentage = (int)p);
        }

        private async Task ExecuteWhenAnyThrottled()
        {
            try
            {
                await ExecuteWhenAnyThrottledInternal();
            }
            catch (OperationCanceledException)
            {
                _info.OnNext(Notifcation.Clear);
                _info.OnNext(Notifcation.Append("You've canceled the task, and you're now left with the following fully copied files:"));
                var alreadyCopiedFilePaths = FileRetriever.GetFilePathsRecursively(_mediaPathService.Destination);
                alreadyCopiedFilePaths.ForEach(path => _info.OnNext(Notifcation.Append(new FileInfo(path).Name)));
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
                return FileCopier.CopyFileAsync(filePath, _mediaPathService.Destination, progress, _whenAnyThrottledCancellationTokenSource.Token);
            }

            ClearOutDestinationFolder();
            Directory.CreateDirectory(_mediaPathService.Destination);

            var filePaths = new Queue<string>(FileRetriever.GetSortedFilePathsRecursively(_mediaPathService.Source));
            var copyTasks = filePaths.Select(BuildTask)
                                     .Take(BatchLevel)
                                     .ToList();

            var finishedTask = Task.FromResult(string.Empty);
            while (copyTasks.Any())
            {
                finishedTask = await Task.WhenAny(copyTasks);
                copyTasks.Remove(finishedTask);

                if (filePaths.Any())
                    copyTasks.Add(BuildTask(filePaths.Dequeue()));
            }

            var currentFileBeingPlayed = finishedTask.Result;
            _info.OnNext(Notifcation.Append("Playing: " + new FileInfo(currentFileBeingPlayed).Name));
            _play.OnNext(currentFileBeingPlayed);
        }

        private bool CanExecuteWhenAnyThrottled()
        {
            return !string.IsNullOrEmpty(_mediaPathService.Source);
        }

        private void CancelWhenAnyThrottled()
        {
            _whenAnyThrottledCancellationTokenSource.Cancel();
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

        private void UpdateFileProgressInformation((FileInfo fileInfo, decimal percentage, bool hasFinished) progress)
        {
            var fileProgress = _cumulatedProgressByFile[progress.fileInfo];
            var currentFileProgressInformation =
                (progress.hasFinished
                    ? "Finished downloading: "
                    : $"Downloading ({fileProgress.percentage:N1} %): ")
                + progress.fileInfo.Name;

            _info.OnNext(Notifcation.Update(progress.fileInfo.Name, currentFileProgressInformation));
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

        private void ClearOutDestinationFolder()
        {
            if (string.IsNullOrEmpty(_mediaPathService.Destination) || !Directory.Exists(_mediaPathService.Destination))
                return;

            foreach (var file in Directory.GetFiles(_mediaPathService.Destination))
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
