using AsyncWorkshop.UsagePatterns.Commands;
using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System;
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

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class WhenAllViewModel : INotifyPropertyChanged
    {
        private readonly IMediaPathService _mediaPathService;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Subject<string> _play = new Subject<string>();
        private readonly Subject<Notifcation> _info = new Subject<Notifcation>();
        private readonly Subject<decimal> _overallProgressPercent = new Subject<decimal>();

        private readonly CumulatedProgressByFile _cumulatedProgressByFile = new CumulatedProgressByFile();

        private SequentialRelayCommandAsync _executeWhenAllCommand;
        private SequentialBoundRelayCommand _cancelWhenAllCommand;

        private readonly CancellationTokenSource _whenAllCancellationTokenSource = new CancellationTokenSource();

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

        public IObservable<Notifcation> Info => _info.AsObservable();

        public IObservable<string> PlaySignals => _play.AsObservable();

        public ICommand ExecuteWhenAllCommand =>
            _executeWhenAllCommand ?? (_executeWhenAllCommand = new SequentialRelayCommandAsync(ExecuteWhenAll, CanExecuteWhenAll));

        public ICommand CancelWhenAllCommand =>
            _cancelWhenAllCommand ?? (_cancelWhenAllCommand = new SequentialBoundRelayCommand(ExecuteWhenAllCommand, CancelWhenAll));

        public WhenAllViewModel() : this(null) { }

        public WhenAllViewModel(IMediaPathService mediaPathService = null)
        {
            _mediaPathService = mediaPathService ?? new MediaPathService();
            _overallProgressPercent.Sample(TimeSpan.FromMilliseconds(200)).Subscribe(p => WhenAllProgressPercentage = (int)p);
        }

        private bool CanExecuteWhenAll()
        {
            return !string.IsNullOrEmpty(_mediaPathService.Source);
        }

        private async Task ExecuteWhenAll()
        {
            try
            {
                await ExecuteWhenAllInternal();
            }
            catch (OperationCanceledException)
            {
                _info.OnNext(Notifcation.Clear);
                _info.OnNext(Notifcation.Append("You've canceled the task, and you're now left with the following fully copied files:"));
                var alreadyCopiedFilePaths = FileRetriever.GetFilePathsRecursively(_mediaPathService.Destination);
                alreadyCopiedFilePaths.ForEach(path => _info.OnNext(Notifcation.Append(new FileInfo(path).Name)));
            }
        }

        private async Task ExecuteWhenAllInternal()
        {
            _mediaPathService.ClearDestination();
            Directory.CreateDirectory(_mediaPathService.Destination);

            var filePaths = FileRetriever.GetFilePathsRecursively(_mediaPathService.Source);
            var copyTasks = filePaths.Select(
                filePath =>
                {
                    var progress = new FileCopyProgress(ReportProgressWhenAll);
                    return FileCopier.CopyFileAsync(filePath, _mediaPathService.Destination, progress, _whenAllCancellationTokenSource.Token);
                }).ToArray();

            var copiedFilePaths = await Task.WhenAll(copyTasks);

            var currentFileBeingPlayed = copiedFilePaths.First(name => name.EndsWith(".mp3"));
            _info.OnNext(Notifcation.Append("Playing: " + new FileInfo(currentFileBeingPlayed).Name));
            _play.OnNext(currentFileBeingPlayed);
        }

        private void CancelWhenAll()
        {
            _whenAllCancellationTokenSource.Cancel();
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

        private void UpdateWhenAllProgress(decimal value)
        {
            _overallProgressPercent.OnNext(value);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
