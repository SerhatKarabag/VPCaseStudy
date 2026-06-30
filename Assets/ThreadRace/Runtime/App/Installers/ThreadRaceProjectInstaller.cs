using Zenject;
using ThreadRace.Core.Random;
using ThreadRace.Core.Time;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Infrastructure.Persistence;
using ThreadRace.Infrastructure.Randomness;
using ThreadRace.Infrastructure.Time;

namespace ThreadRace.App.Installers
{
    public sealed class ThreadRaceProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IRaceTimeProvider>().To<UnityRaceTimeProvider>().AsSingle();
            Container.Bind<IDeterministicRandomSourceFactory>().To<SeededRandomSourceFactory>().AsSingle();
            Container.Bind<IRaceSaveRepository>().To<PlayerPrefsRaceSaveRepository>().AsSingle();
        }
    }
}
