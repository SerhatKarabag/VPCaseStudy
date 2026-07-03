using System;
using Zenject;
using ThreadRace.Core.Audio;
using ThreadRace.Core.Progress;
using ThreadRace.Core.Random;
using ThreadRace.Core.Time;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Infrastructure.Audio;
using ThreadRace.Infrastructure.Persistence;
using ThreadRace.Infrastructure.Randomness;
using ThreadRace.Infrastructure.Time;
using UnityEngine;

namespace ThreadRace.App.Installers
{
    public sealed class ThreadRaceProjectInstaller : MonoInstaller
    {
        private const string AudioLibraryResourcePath = "ThreadRaceAudioLibrary";

        public override void InstallBindings()
        {
            Container.Bind<IRaceTimeProvider>().To<UnityRaceTimeProvider>().AsSingle();
            Container.Bind<IUtcClock>().To<SystemUtcClock>().AsSingle();
            Container.Bind<IDeterministicRandomSourceFactory>().To<SeededRandomSourceFactory>().AsSingle();
            Container.Bind<IRaceSaveRepository>().To<PlayerPrefsRaceSaveRepository>().AsSingle();
            Container.Bind<IHostLevelProgressRepository>().To<PlayerPrefsHostLevelProgressRepository>().AsSingle();
            Container.Bind<IHostLevelProgressService>().To<HostLevelProgressService>().AsSingle();

            var audioLibraryAsset = Resources.Load<RaceAudioLibraryAsset>(AudioLibraryResourcePath);
            if (audioLibraryAsset == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ThreadRaceProjectInstaller)} requires Resources/{AudioLibraryResourcePath}.asset.");
            }

            Container.Bind<RaceAudioLibrary>().FromInstance(audioLibraryAsset.ToRuntimeLibrary()).AsSingle();
            Container.Bind<RaceAudioService>().AsSingle();
            Container.Bind<IRaceAudioService>().To<RaceAudioService>().FromResolve();
            Container.Bind<IRaceMusicService>().To<RaceAudioService>().FromResolve();
            Container.Bind<IRaceSfxService>().To<RaceAudioService>().FromResolve();
            Container.Bind<IRaceAudioVolumeService>().To<RaceAudioService>().FromResolve();
            Container.Bind<IRaceAudioMuteService>().To<RaceAudioService>().FromResolve();
            Container.BindInterfacesTo<RaceAudioDriver>().AsSingle();
        }
    }
}
