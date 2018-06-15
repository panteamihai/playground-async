using System;

namespace AsyncWorkshop.UsagePatterns.ViewModels
{
    public interface IPlayableViewModel
    {
        IObservable<string> PlaySignals { get; }
    }
}