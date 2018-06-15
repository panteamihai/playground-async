using System;
using System.Windows;

using AsyncWorkshop.UsagePatterns.ViewModels;

namespace AsyncWorkshop.UsagePatterns.Views
{
    public partial class UsagePatternsView : Window
    {
        public UsagePatternsView()
        {
            InitializeComponent();

            if (DataContext is IPlayableViewModel playableViewModel)
            {
                playableViewModel.PlaySignals.Subscribe(path =>
                {
                    //player.Play(path);
                });
            }
        }
    }
}
