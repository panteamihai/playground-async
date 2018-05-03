using System;
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

        private string _mediaSourcePath = @"D:\Projects - Extra\Workshop\workshop-async\media";
        private string _mediaDestinationPath = @"D:\Projects - Extra\Workshop\workshop-async\UsagePatterns\bin\Debug\copied";

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

        public string MediaDestinationPath
        {
            get => _mediaDestinationPath;
            set
            {
                ClearOutDestinationFolder();

                _mediaDestinationPath = value;
                Directory.CreateDirectory(_mediaDestinationPath);
                OnPropertyChanged();
            }
        }

        public IObservable<string> PlaySignals => _play.AsObservable();

        private bool CanExecuteWhenAll()
        {
            return !string.IsNullOrEmpty(_mediaSourcePath);
        }

        private async Task ExecuteWhenAll()
        {
            ClearOutDestinationFolder();
            Directory.CreateDirectory(_mediaDestinationPath);

            var filePaths = FileRetriever.GetFilePathsRecursively(_mediaSourcePath);
            var copyTasks = filePaths.Select(f => FileCopier.CopyFileAsync(f, _mediaDestinationPath)).ToArray();
            var copiedFilePaths = await Task.WhenAll(copyTasks);

            var currentFileBeingPlayed = copiedFilePaths.First(name => name.EndsWith(".mp3"));
            _play.OnNext(currentFileBeingPlayed);
        }

        private void ClearOutDestinationFolder()
        {
            if (!string.IsNullOrEmpty(_mediaDestinationPath) && Directory.Exists(_mediaDestinationPath))
            {
                Directory.Delete(_mediaDestinationPath, true);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
