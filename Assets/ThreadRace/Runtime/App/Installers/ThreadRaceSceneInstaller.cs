using System;
using ThreadRace.App;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Infrastructure.Config;
using UnityEngine;
using Zenject;

namespace ThreadRace.App.Installers
{
    public sealed class ThreadRaceSceneInstaller : MonoInstaller
    {
        [SerializeField] private RaceEventConfigAsset _raceEventConfigAsset;

        public override void InstallBindings()
        {
            if (_raceEventConfigAsset == null)
            {
                throw new InvalidOperationException($"{nameof(ThreadRaceSceneInstaller)} requires a RaceEventConfigAsset reference.");
            }

            var settings = _raceEventConfigAsset.ToRuntimeSettings();

            Container.Bind<RaceEventSettings>().FromInstance(settings).AsSingle();
            Container.Bind<RaceSaveDataMapper>().AsSingle();
            Container.Bind<RaceEventController>().AsSingle();
            Container.Bind<ILevelResultHandler>().To<RaceEventController>().FromResolve();
            Container.BindInterfacesTo<RaceSimulationDriver>().AsSingle();
        }
    }
}
