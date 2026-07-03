using System;
using ThreadRace.Core.Audio;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Signals;
using ThreadRace.Presentation.Views;
using Zenject;

namespace ThreadRace.Presentation.Presenters
{
    public sealed class RaceAudioPresenter : IInitializable, IDisposable
    {
        private readonly IRaceAudioService _audioService;
        private readonly IMainMenuView _mainMenuView;
        private readonly IEntryPopupView _entryPopupView;
        private readonly IRaceHudView _raceHudView;
        private readonly SignalBus _signalBus;

        private bool _initialized;

        public RaceAudioPresenter(
            IRaceAudioService audioService,
            IMainMenuView mainMenuView,
            IEntryPopupView entryPopupView,
            IRaceHudView raceHudView,
            SignalBus signalBus)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _mainMenuView = mainMenuView ?? throw new ArgumentNullException(nameof(mainMenuView));
            _entryPopupView = entryPopupView ?? throw new ArgumentNullException(nameof(entryPopupView));
            _raceHudView = raceHudView ?? throw new ArgumentNullException(nameof(raceHudView));
            _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _signalBus.Subscribe<HostGameplayStartedSignal>(OnHostGameplayStarted);
            _signalBus.Subscribe<HostGameplayScreenChangedSignal>(OnHostGameplayScreenChanged);
            _signalBus.Subscribe<HostGameplayBackHomeClickedSignal>(OnHostGameplayBackHomeClicked);
            _signalBus.Subscribe<HostGameplayCompletedSignal>(OnHostGameplayCompleted);
            _mainMenuView.NavigationItemClicked += OnNavigationItemClicked;
            _mainMenuView.ThreadRaceRequested += PlayButtonClick;
            _entryPopupView.StartRequested += PlayButtonClick;
            _entryPopupView.CloseRequested += PlayButtonClick;
            _raceHudView.CloseRequested += PlayButtonClick;
            _initialized = true;
            _audioService.PlayMusic(RaceMusicCue.Menu, false);
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            _signalBus.Unsubscribe<HostGameplayStartedSignal>(OnHostGameplayStarted);
            _signalBus.Unsubscribe<HostGameplayScreenChangedSignal>(OnHostGameplayScreenChanged);
            _signalBus.Unsubscribe<HostGameplayBackHomeClickedSignal>(OnHostGameplayBackHomeClicked);
            _signalBus.Unsubscribe<HostGameplayCompletedSignal>(OnHostGameplayCompleted);
            _mainMenuView.NavigationItemClicked -= OnNavigationItemClicked;
            _mainMenuView.ThreadRaceRequested -= PlayButtonClick;
            _entryPopupView.StartRequested -= PlayButtonClick;
            _entryPopupView.CloseRequested -= PlayButtonClick;
            _raceHudView.CloseRequested -= PlayButtonClick;
            _initialized = false;
        }

        private void OnNavigationItemClicked()
        {
            PlayButtonClick();
        }

        private void PlayButtonClick()
        {
            _audioService.PlaySound(RaceSoundCue.ButtonClick);
        }

        private void OnHostGameplayStarted(HostGameplayStartedSignal signal)
        {
            _audioService.PlayMusic(RaceMusicCue.Gameplay, true);
            _audioService.PlaySound(RaceSoundCue.ChallengePopupOpen);
        }

        private void OnHostGameplayScreenChanged(HostGameplayScreenChangedSignal signal)
        {
            if (signal.Screen == PlaceholderLevelScreen.LevelWin)
            {
                _audioService.PlaySound(RaceSoundCue.LevelWinPopupOpen);
            }
            else if (signal.Screen == PlaceholderLevelScreen.LevelFail)
            {
                _audioService.PlaySound(RaceSoundCue.FailPopupOpen);
            }
        }

        private void OnHostGameplayBackHomeClicked(HostGameplayBackHomeClickedSignal signal)
        {
            PlayButtonClick();
        }

        private void OnHostGameplayCompleted(HostGameplayCompletedSignal signal)
        {
            if (signal.Result == LevelResult.Success)
            {
                _audioService.PlaySound(RaceSoundCue.ClaimButtonClick);
            }

            _audioService.PlayMusic(RaceMusicCue.Menu, true);
        }
    }
}
