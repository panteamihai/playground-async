using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class UsagePatternsViewModel : INotifyPropertyChanged, IPlayableViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigurationViewModel ConfigurationViewModel { get; }
        public WhenAllViewModel WhenAllViewModel { get; }
        public WhenAnyThrottledViewModel WhenAnyThrottledViewModel { get; }

        public IObservable<string> PlaySignals { get; }
        public ObservableCollection<string> FileProgressInformation { get; } = new ObservableCollection<string>();

        public UsagePatternsViewModel() : this(null) { }

        public UsagePatternsViewModel(IMediaPathService mps = null)
        {
            var mediaPathService = mps ?? new MediaPathService();

            ConfigurationViewModel = new ConfigurationViewModel(mediaPathService);
            WhenAllViewModel = new WhenAllViewModel(mediaPathService);
            WhenAnyThrottledViewModel = new WhenAnyThrottledViewModel(mediaPathService);

            PlaySignals = WhenAllViewModel.PlaySignals.Merge(WhenAnyThrottledViewModel.PlaySignals);
            WhenAllViewModel.Info.Merge(WhenAnyThrottledViewModel.Info).Subscribe(UpdateFileProgressInformation);
        }

        private void UpdateFileProgressInformation(Notifcation notification)
        {
            switch (notification.Type)
            {
                case NotifcationType.Clear:
                    FileProgressInformation.Clear();
                    break;
                case NotifcationType.Append:
                    FileProgressInformation.Add(notification.Message);
                    break;
                case NotifcationType.Update:
                    for (var i = 0; i < FileProgressInformation.Count; i++)
                    {
                        var fileProgressInformation = FileProgressInformation[i];
                        if (fileProgressInformation.Contains(notification.Id))
                        {
                            FileProgressInformation.RemoveAt(i);
                            FileProgressInformation.Insert(i, notification.Message);
                            return;
                        }
                    }
                    FileProgressInformation.Add(notification.Message);
                    break;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
