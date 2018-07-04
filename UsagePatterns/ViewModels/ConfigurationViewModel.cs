using AsyncWorkshop.UsagePatterns.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public class ConfigurationViewModel : INotifyPropertyChanged
    {
        private readonly IPathService _pathService;

        public event PropertyChangedEventHandler PropertyChanged;

        public string MediaSourcePath
        {
            get => _pathService.Source;
            set
            {
                _pathService.Source = value;
                OnPropertyChanged();
            }
        }

        public string MediaDestinationPath => _pathService.Destination;

        public string UtilityPath => _pathService.Utility;

        public ConfigurationViewModel() : this(null) { }

        public ConfigurationViewModel(IPathService pathService = null)
        {
            _pathService = pathService ?? new PathService();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
