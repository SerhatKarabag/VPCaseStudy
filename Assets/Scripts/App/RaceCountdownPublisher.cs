using System;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Signals;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RaceCountdownPublisher : IRaceCountdownPublisher
    {
        private readonly SignalBus _signalBus;

        public RaceCountdownPublisher(SignalBus signalBus)
        {
            _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        }

        public void Publish(RaceCountdownSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            _signalBus.Fire(new RaceCountdownChangedSignal(snapshot));
        }
    }
}
