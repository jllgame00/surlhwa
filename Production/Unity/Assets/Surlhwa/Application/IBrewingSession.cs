using System;
using System.Collections.Generic;
using Surlhwa.Domain;
namespace Surlhwa.Application
{
    public interface IBrewingSession
    {
        bool CanInteract { get; }
        BrewingSession Brew { get; }
        IReadOnlyCollection<string> Supply { get; }
        BrewingTuning Tuning { get; }
        bool ExtendedMethods { get; }
        bool ExecuteBrew(long id,Action<BrewingSession> action);
        bool UpdateBrew(long microseconds,bool fine);
        void ReleaseHeldInput();
    }
}
