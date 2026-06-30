using ThreadRace.Core.Time;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Domain;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RaceSimulationDriver : ITickable
    {
        private readonly RaceEventController _controller;
        private readonly IRaceTimeProvider _timeProvider;

        public RaceSimulationDriver(RaceEventController controller, IRaceTimeProvider timeProvider)
        {
            _controller = controller;
            _timeProvider = timeProvider;
        }

        public void Tick()
        {
            if (_controller.Phase != RacePhase.Running)
            {
                return;
            }

            var deltaTime = _timeProvider.UnscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            _controller.AdvanceAi(deltaTime);
        }
    }
}
