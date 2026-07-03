using System;
using ThreadRace.App;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Infrastructure.Config;
using ThreadRace.Presentation.Presenters;
using ThreadRace.Presentation.Signals;
using ThreadRace.Presentation.Views;
using UnityEngine;
using Zenject;

namespace ThreadRace.App.Installers
{
    public sealed class ThreadRaceSceneInstaller : MonoInstaller
    {
        [SerializeField] private RaceEventConfigAsset _raceEventConfigAsset;
        [SerializeField] private MainMenuView _mainMenuView;
        [SerializeField] private EntryPopupView _entryPopupView;
        [SerializeField] private RaceHudView _raceHudView;
        [SerializeField] private PlaceholderLevelView _placeholderLevelView;
        [SerializeField] private RaceResultView _raceResultView;
        [SerializeField] private RaceApplicationLifecycleObserver _lifecycleObserver;

        public override void InstallBindings()
        {
            ValidateReferences();

            var settings = _raceEventConfigAsset.ToRuntimeSettings();

            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<RaceSnapshotChangedSignal>();
            Container.DeclareSignal<RaceCountdownChangedSignal>();
            Container.DeclareSignal<HostGameplayStartedSignal>();
            Container.DeclareSignal<HostLevelChangedSignal>();
            Container.DeclareSignal<HostGameplayScreenChangedSignal>();
            Container.DeclareSignal<HostGameplayBackHomeClickedSignal>();
            Container.DeclareSignal<HostGameplayCompletedSignal>();

            Container.Bind<RaceEventSettings>().FromInstance(settings).AsSingle();
            Container.Bind<RaceSaveDataMapper>().AsSingle();
            Container.Bind<RaceEventController>().AsSingle();

            if (_mainMenuView != null)
            {
                Container.Bind<IMainMenuView>().FromInstance(_mainMenuView).AsSingle();
            }
            else
            {
                Container.Bind<IMainMenuView>().FromInstance(NullMainMenuView.Instance).AsSingle();
            }

            Container.Bind<IEntryPopupView>().FromInstance(_entryPopupView).AsSingle();
            Container.Bind<IRaceHudView>().FromInstance(_raceHudView).AsSingle();
            Container.Bind<IPlaceholderLevelView>().FromInstance(_placeholderLevelView).AsSingle();
            Container.Bind<IRaceResultView>().FromInstance(_raceResultView).AsSingle();

            Container.Bind<IRaceSnapshotPublisher>().To<RaceSnapshotPublisher>().AsSingle();
            Container.Bind<IRaceCountdownPublisher>().To<RaceCountdownPublisher>().AsSingle();
            Container.Bind<RaceUiCommandRouter>().AsSingle();
            Container.Bind<IRaceEventCommandHandler>().To<RaceUiCommandRouter>().FromResolve();
            Container.Bind<IRaceSnapshotProvider>().To<RaceUiCommandRouter>().FromResolve();
            Container.Bind<IRaceCountdownProvider>().To<RaceUiCommandRouter>().FromResolve();
            Container.Bind<LevelResultSource>().AsSingle();
            Container.Bind<ILevelResultSource>().To<LevelResultSource>().FromResolve();
            Container.Bind<ILevelResultReporter>().To<LevelResultSource>().FromResolve();
            Container.Bind<RaceApplicationLifecycleObserver>().FromInstance(_lifecycleObserver).AsSingle();

            Container.BindInterfacesTo<RaceFlowPresenter>().AsSingle();
            Container.BindInterfacesTo<EntryPopupPresenter>().AsSingle();
            Container.BindInterfacesTo<RaceHudPresenter>().AsSingle();
            Container.BindInterfacesTo<PlaceholderLevelPresenter>().AsSingle();
            Container.BindInterfacesTo<RaceResultPresenter>().AsSingle();
            Container.BindInterfacesTo<RaceAudioPresenter>().AsSingle();
            Container.BindInterfacesTo<RaceLevelResultListener>().AsSingle();
            Container.BindInterfacesTo<RacePresentationWarmup>().AsSingle();
            Container.BindInterfacesTo<RacePresentationBootstrap>().AsSingle();

            Container.BindExecutionOrder<RacePresentationWarmup>(-100);
            Container.BindExecutionOrder<RaceFlowPresenter>(-50);
            Container.BindExecutionOrder<EntryPopupPresenter>(-40);
            Container.BindExecutionOrder<RaceHudPresenter>(-30);
            Container.BindExecutionOrder<PlaceholderLevelPresenter>(-20);
            Container.BindExecutionOrder<RaceResultPresenter>(-10);
            Container.BindExecutionOrder<RaceAudioPresenter>(-5);
            Container.BindExecutionOrder<RaceLevelResultListener>(0);
            Container.BindExecutionOrder<RacePresentationBootstrap>(20);

            Container.BindInterfacesTo<RaceSimulationDriver>().AsSingle();
            Container.BindInterfacesTo<RaceEventTimeDriver>().AsSingle();
        }

        private void ValidateReferences()
        {
            if (_raceEventConfigAsset == null)
            {
                throw new InvalidOperationException($"{nameof(ThreadRaceSceneInstaller)} requires a RaceEventConfigAsset reference.");
            }

            if (_entryPopupView == null)
            {
                throw new InvalidOperationException($"{nameof(ThreadRaceSceneInstaller)} requires an EntryPopupView reference.");
            }

            if (_raceHudView == null)
            {
                throw new InvalidOperationException($"{nameof(ThreadRaceSceneInstaller)} requires a RaceHudView reference.");
            }

            if (_placeholderLevelView == null)
            {
                throw new InvalidOperationException($"{nameof(ThreadRaceSceneInstaller)} requires a PlaceholderLevelView reference.");
            }

            if (_raceResultView == null)
            {
                throw new InvalidOperationException($"{nameof(ThreadRaceSceneInstaller)} requires a RaceResultView reference.");
            }

            if (_lifecycleObserver == null)
            {
                throw new InvalidOperationException($"{nameof(ThreadRaceSceneInstaller)} requires a RaceApplicationLifecycleObserver reference.");
            }
        }

        private sealed class NullMainMenuView : IMainMenuView
        {
            public static readonly NullMainMenuView Instance = new NullMainMenuView();

            private NullMainMenuView()
            {
            }

            event Action IMainMenuView.PlayRequested
            {
                add { }
                remove { }
            }

            event Action IMainMenuView.ThreadRaceRequested
            {
                add { }
                remove { }
            }

            event Action IMainMenuView.NavigationItemClicked
            {
                add { }
                remove { }
            }

            public bool IsVisible => false;

            public bool IsInteractive => false;

            public bool IsAvailable => false;

            public void SetVisible(bool visible, bool interactive)
            {
            }

            public void SetThreadRaceCountdown(string countdownText)
            {
            }

            public void SetPlayButtonLabel(string label)
            {
            }
        }
    }
}
