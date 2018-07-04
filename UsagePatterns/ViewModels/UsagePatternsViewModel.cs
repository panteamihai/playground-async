using AsyncWorkshop.UsagePatterns.Helpers;
using AsyncWorkshop.UsagePatterns.Services;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class UsagePatternsViewModel : IPlayableViewModel
    {
        public ConfigurationViewModel ConfigurationViewModel { get; }
        public WhenAllViewModel WhenAllViewModel { get; }
        public WhenAnyViewModel WhenAnyThrottledViewModel { get; }
        public WhenAnyViewModel WhenAnyFirstWinsViewModel { get; }
        public WhenAnyViewModel WhenAnyEarlyBailoutViewModel { get; }

        public IObservable<string> PlaySignals { get; }
        public ObservableCollection<string> FileProgressInformation { get; } = new ObservableCollection<string>();

        public UsagePatternsViewModel() : this(null) { }

        public UsagePatternsViewModel(IPathService mps = null)
        {
            var mediaPathService = mps ?? new PathService();

            ConfigurationViewModel = new ConfigurationViewModel(mediaPathService);
            WhenAllViewModel = new WhenAllViewModel(mediaPathService);
            WhenAnyThrottledViewModel = new WhenAnyThrottledViewModel(mediaPathService);
            WhenAnyFirstWinsViewModel = new WhenAnyFirstWinsViewModel(mediaPathService);
            WhenAnyEarlyBailoutViewModel = new WhenAnyEarlyBailoutViewModel();

            PlaySignals = Observable.Merge(
                WhenAllViewModel.PlaySignals,
                WhenAnyThrottledViewModel.PlaySignals,
                WhenAnyFirstWinsViewModel.PlaySignals,
                WhenAnyEarlyBailoutViewModel.PlaySignals);
            Observable.Merge(
                        WhenAllViewModel.Info,
                        WhenAnyThrottledViewModel.Info,
                        WhenAnyFirstWinsViewModel.Info,
                        WhenAnyEarlyBailoutViewModel.Info)
                      .Subscribe(UpdateFileProgressInformation);
        }

        private void UpdateFileProgressInformation(Notification notification)
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
    }
}
