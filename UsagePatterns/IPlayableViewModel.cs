using System;

namespace AsyncWorkshop.UsagePatterns
{
    public interface IPlayableViewModel
    {
        IObservable<string> PlaySignals { get; }
    }
}