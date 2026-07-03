using System;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Signals;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RaceSnapshotPublisher : IRaceSnapshotPublisher
    {
        private readonly SignalBus _signalBus;

        public RaceSnapshotPublisher(SignalBus signalBus)
        {
            _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        }

        public void Publish(RaceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            _signalBus.Fire(new RaceSnapshotChangedSignal(snapshot));
        }
    }
}
