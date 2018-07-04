using AsyncWorkshop.UsagePatterns.Commands;
using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System;
using System.ComponentModel;
using System.IO;
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
    public abstract class OperationViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private SequentialRelayCommandAsync _executeCommand;
        private SequentialBoundRelayCommand _cancelCommand;

        private readonly Subject<string> _play = new Subject<string>();
        private readonly Subject<Notification> _info = new Subject<Notification>();

        protected CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        protected readonly IPathService PathService;
        protected readonly CumulatedProgressByFile CumulatedProgressByFile = new CumulatedProgressByFile();

        public IObservable<Notification> Info => _info.AsObservable();

        public IObservable<string> PlaySignals => _play.AsObservable();

        public ICommand ExecuteCommand =>
            _executeCommand ?? (_executeCommand = new SequentialRelayCommandAsync(Execute, CanExecute));

        public ICommand CancelCommand =>
            _cancelCommand ?? (_cancelCommand = new SequentialBoundRelayCommand(ExecuteCommand, Cancel));

        protected OperationViewModel(IPathService pathService = null)
        {
            PathService = pathService ?? new PathService();
        }

        protected async Task Execute()
        {
            try
            {
                ResetState();

                var fileToPlay = await ExecuteInternal();

                StartPlaying(fileToPlay);
            }
            catch (OperationCanceledException)
            {
                HandleCancellation();
            }
            catch(Exception ex)
            {
                _info.OnNext(Notification.Append("Something went really wrong!"));
                _info.OnNext(Notification.Append(ex.Message));
            }
        }

        private void ResetState()
        {
            _info.OnNext(Notification.Clear);
            CancellationTokenSource = new CancellationTokenSource();

            PathService.ClearDestination();
            Directory.CreateDirectory(PathService.Destination);

            PathService.ClearStandByList();
            CumulatedProgressByFile.Clear();
            ClearProgressInternal();
        }

        protected abstract void ClearProgressInternal();

        protected abstract Task<string> ExecuteInternal();

        protected Task<string> BuildTask(string filePath)
        {
            var progress = new FileCopyProgress(ReportProgress);
            return FileCopier.CopyFileAsync(filePath, PathService.Destination, progress, CancellationTokenSource.Token);
        }

        private void HandleCancellation()
        {
            _info.OnNext(Notification.Clear);
            _info.OnNext(Notification.Append("You've canceled the task, and you're now left with the following fully copied files:"));
            var alreadyCopiedFilePaths = FileRetriever.GetFilePathsRecursively(PathService.Destination);
            alreadyCopiedFilePaths.ForEach(path => _info.OnNext(Notification.Append(new FileInfo(path).Name)));
        }

        protected bool CanExecute()
        {
            return !string.IsNullOrEmpty(PathService.Source);
        }

        private void Cancel()
        {
            CancellationTokenSource.Cancel();
        }

        protected void ReportProgress((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress)
        {
            if (CancellationTokenSource.IsCancellationRequested)
                return;

            CumulatedProgressByFile.AddOrUpdate(
                progress.fileInfo,
                (progress.filePercentageCompleteIncrement, false),
                (key, existing) => (existing.percentage + progress.filePercentageCompleteIncrement, progress.finished));

            ReportProgressInternal(progress);
            UpdateFileProgressInformation(progress);
        }

        protected abstract void ReportProgressInternal((FileInfo fileInfo, decimal filePercentageCompleteIncrement, bool finished) progress);

        private void UpdateFileProgressInformation((FileInfo fileInfo, decimal percentage, bool hasFinished) progress)
        {
            var fileProgress = CumulatedProgressByFile[progress.fileInfo];
            var currentFileProgressInformation =
                (progress.hasFinished
                    ? "Finished downloading: "
                    : $"Downloading ({fileProgress.percentage:N1} %): ")
                + progress.fileInfo.Name;

            _info.OnNext(Notification.Update(progress.fileInfo.Name, currentFileProgressInformation));
        }

        private void StartPlaying(string currentFileBeingPlayed)
        {
            _info.OnNext(Notification.Append("Playing: " + new FileInfo(currentFileBeingPlayed).Name));
            _play.OnNext(currentFileBeingPlayed);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
