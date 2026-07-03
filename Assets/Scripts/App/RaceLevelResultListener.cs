using System;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RaceLevelResultListener : IInitializable, IDisposable
    {
        private readonly ILevelResultSource _levelResultSource;
        private readonly IRaceEventCommandHandler _commandHandler;

        private bool _initialized;

        public RaceLevelResultListener(
            ILevelResultSource levelResultSource,
            IRaceEventCommandHandler commandHandler)
        {
            _levelResultSource = levelResultSource ?? throw new ArgumentNullException(nameof(levelResultSource));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _levelResultSource.ResultReported += OnResultReported;
            _initialized = true;
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            _levelResultSource.ResultReported -= OnResultReported;
            _initialized = false;
        }

        private void OnResultReported(LevelResult result)
        {
            _commandHandler.ReportLevelResult(result);
        }
    }
}
