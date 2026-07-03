using System;
using ThreadRace.Infrastructure.Audio;
using Zenject;

namespace ThreadRace.App
{
    public sealed class RaceAudioDriver : IInitializable, ITickable, IDisposable
    {
        private readonly RaceAudioService _audioService;

        public RaceAudioDriver(RaceAudioService audioService)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }

        public void Initialize()
        {
            _audioService.Initialize();
        }

        public void Tick()
        {
            _audioService.Tick();
        }

        public void Dispose()
        {
            _audioService.Dispose();
        }
    }
}
