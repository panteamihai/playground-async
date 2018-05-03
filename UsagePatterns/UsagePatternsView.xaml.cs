using System;
using System.Windows;

namespace AsyncWorkshop.UsagePatterns
{
    public partial class UsagePatternsView : Window
    {
        public UsagePatternsView()
        {
            InitializeComponent();

            if (DataContext is IPlayableViewModel playableViewModel)
            {
                playableViewModel.PlaySignals.Subscribe(path => player.Play(path));
            }
        }
    }
}
