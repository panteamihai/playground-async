using AsyncWorkshop.UsagePatterns.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class ConfigurationViewModel : INotifyPropertyChanged
    {
        private readonly IMediaPathService _mediaPathService;

        public event PropertyChangedEventHandler PropertyChanged;

        public string MediaSourcePath
        {
            get => _mediaPathService.Source;
            set
            {
                _mediaPathService.Source = value;
                OnPropertyChanged();
            }
        }

        public string MediaDestinationPath => _mediaPathService.Destination;

        public ConfigurationViewModel() : this(null) { }

        public ConfigurationViewModel(IMediaPathService mediaPathService = null)
        {
            _mediaPathService = mediaPathService ?? new MediaPathService();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
