using System;
using NUnit.Framework;
using ThreadRace.App;
using ThreadRace.Core.Audio;
using ThreadRace.Core.Progress;
using ThreadRace.Gameplay.Application;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Infrastructure.Randomness;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Presenters;
using ThreadRace.Presentation.Signals;
using ThreadRace.Presentation.Views;
using Zenject;

namespace ThreadRace.Tests.EditMode
{
    public sealed class RacePresentationPresenterTests
    {
        [Test]
        public void FlowPresenter_NotStartedShowsMainMenuBeforeEventRequest()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.ApplySnapshot(snapshot);

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_InitializesMainMenuThreadRaceCountdown()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();

            Assert.AreEqual("5m", views.Menu.ThreadRaceCountdownText);
        }

        [Test]
        public void FlowPresenter_CountdownSignalUpdatesMainMenuThreadRaceCountdown()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet();
            var bus = CreateSignalBus();
            var presenter = CreateFlowPresenter(snapshot, views, bus);

            presenter.Initialize();
            bus.Fire(new RaceCountdownChangedSignal(new RaceCountdownSnapshot(true, 299L, false, null)));

            Assert.AreEqual("4m 59s", views.Menu.ThreadRaceCountdownText);
        }

        [Test]
        public void FlowPresenter_HostLevelSignalUpdatesMainMenuPlayLabel()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet();
            var bus = CreateSignalBus();
            var presenter = CreateFlowPresenter(snapshot, views, bus);

            presenter.Initialize();
            bus.Fire(new HostLevelChangedSignal(7));

            Assert.AreEqual("LEVEL 7", views.Menu.PlayButtonLabel);
        }

        [Test]
        public void FlowPresenter_EventRequestShowsEntryOverNonInteractiveMainMenu()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();

            AssertMenuVisibility(views, true, false);
            AssertVisibility(views, true, false, false, false);
        }

        [Test]
        public void FlowPresenter_EntryCloseReturnsToInteractiveMainMenu()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();
            views.Entry.RaiseCloseRequested();

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_StartTransitionShowsRaceHudWithoutLevelPanel()
        {
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(CreateNotStartedSnapshot(), views);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();
            presenter.ApplySnapshot(CreateRunningSnapshot());

            AssertMenuVisibility(views, true, false);
            AssertVisibility(views, false, true, false, false);
        }

        [Test]
        public void FlowPresenter_DisposeUnsubscribesEntryCloseRequest()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();
            presenter.Dispose();
            views.Entry.RaiseCloseRequested();

            AssertMenuVisibility(views, true, false);
            AssertVisibility(views, true, false, false, false);
        }

        [Test]
        public void FlowPresenter_NotStartedWithoutMainMenuFallsBackToEntry()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet(new FakeMainMenuView(false));
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.ApplySnapshot(snapshot);

            AssertMenuVisibility(views, false, false);
            AssertVisibility(views, true, false, false, false);
        }

        [Test]
        public void FlowPresenter_RunningShowsInteractiveMainMenuByDefault()
        {
            var snapshot = CreateRunningSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.ApplySnapshot(snapshot);

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_PlayRequestOpensHostGameplayPanel()
        {
            var snapshot = CreateRunningSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaisePlayRequested();

            AssertMenuVisibility(views, false, false);
            AssertVisibility(views, false, false, true, false);
        }

        [Test]
        public void FlowPresenter_RunningThreadRaceRequestOpensHudPopup()
        {
            var snapshot = CreateRunningSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();

            AssertMenuVisibility(views, true, false);
            AssertVisibility(views, false, true, false, false);
        }

        [Test]
        public void FlowPresenter_ExpiredThreadRaceRequestResolvesAndShowsResult()
        {
            var views = new ViewSet();
            var snapshotProvider = new FakeSnapshotProvider(CreateNotStartedSnapshot());
            var countdownProvider = new FakeCountdownProvider(new RaceCountdownSnapshot(true, 0L, true, null));
            var commandHandler = new FakeCommandHandler();
            commandHandler.ResolveExpiredEventCallback = () =>
            {
                snapshotProvider.CurrentSnapshot = CreateExpiredSnapshot();
                return true;
            };
            var presenter = CreateFlowPresenter(
                snapshotProvider,
                views,
                countdownProvider: countdownProvider,
                commandHandler: commandHandler);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();

            Assert.AreEqual(1, commandHandler.ResolveExpiredEventCount);
            AssertMenuVisibility(views, true, false);
            AssertVisibility(views, false, true, false, true);
        }

        [Test]
        public void FlowPresenter_HudCloseReturnsToInteractiveMainMenu()
        {
            var snapshot = CreateRunningSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();
            views.Hud.RaiseCloseRequested();

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_HostGameplayCompletionOpensRacePopupWhenJoined()
        {
            var snapshot = CreateRunningSnapshot();
            var views = new ViewSet();
            var bus = CreateSignalBus();
            var presenter = CreateFlowPresenter(snapshot, views, bus);

            presenter.Initialize();
            views.Menu.RaisePlayRequested();
            bus.Fire(new HostGameplayCompletedSignal(LevelResult.Success, true));

            AssertMenuVisibility(views, true, false);
            AssertVisibility(views, false, true, false, false);
        }

        [Test]
        public void FlowPresenter_HostGameplayCompletionThatFinishesRaceShowsResult()
        {
            var views = new ViewSet();
            var bus = CreateSignalBus();
            var presenter = CreateFlowPresenter(CreateRunningSnapshot(), views, bus);

            presenter.Initialize();
            views.Menu.RaisePlayRequested();
            bus.Fire(new HostGameplayCompletedSignal(LevelResult.Success, true));
            presenter.ApplySnapshot(CreateCompletedRewardedSnapshot());

            AssertMenuVisibility(views, true, false);
            AssertVisibility(views, false, true, false, true);
        }

        [Test]
        public void FlowPresenter_CompletedDefaultsToInteractiveMainMenu()
        {
            var snapshot = CreateCompletedRewardedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.ApplySnapshot(snapshot);

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_CompletedShowsEndedCountdownOnMainMenu()
        {
            var snapshot = CreateCompletedRewardedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(
                snapshot,
                views,
                countdownProvider: new FakeCountdownProvider(new RaceCountdownSnapshot(false, 0L, false, null)));

            presenter.Initialize();

            Assert.AreEqual("ENDED", views.Menu.ThreadRaceCountdownText);
        }

        [Test]
        public void FlowPresenter_PlayRequestOpensHostGameplayAfterRaceCompleted()
        {
            var snapshot = CreateCompletedRewardedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaisePlayRequested();

            AssertMenuVisibility(views, false, false);
            AssertVisibility(views, false, false, true, false);
        }

        [Test]
        public void FlowPresenter_CompletedThreadRaceRequestShowsResultAndHud()
        {
            var snapshot = CreateCompletedRewardedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();

            AssertMenuVisibility(views, true, false);
            AssertVisibility(views, false, true, false, true);
        }

        [Test]
        public void FlowPresenter_ClaimedCompletedResultContinueReturnsToInteractiveMainMenu()
        {
            var snapshot = CreateClaimedRewardedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();
            views.Menu.RaiseThreadRaceRequested();
            views.Result.RaiseContinueRequested();

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_RunningAutoCompletionDoesNotOpenResultPopup()
        {
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(CreateRunningSnapshot(), views);

            presenter.Initialize();
            presenter.ApplySnapshot(CreateCompletedRewardedSnapshot());

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_RestoredRunningInitializesMainMenuPresentation()
        {
            var snapshot = CreateRunningSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_RestoredCompletedInitializesMainMenuPresentation()
        {
            var snapshot = CreateCompletedDnfSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.Initialize();

            AssertMenuVisibility(views, true, true);
            AssertVisibility(views, false, false, false, false);
        }

        [Test]
        public void FlowPresenter_HiddenViewsAreNonInteractive()
        {
            var snapshot = CreateNotStartedSnapshot();
            var views = new ViewSet();
            var presenter = CreateFlowPresenter(snapshot, views);

            presenter.ApplySnapshot(snapshot);

            Assert.IsTrue(views.Menu.IsInteractive);
            Assert.IsFalse(views.Entry.IsInteractive);
            Assert.IsFalse(views.Hud.IsInteractive);
            Assert.IsFalse(views.Level.IsInteractive);
            Assert.IsFalse(views.Result.IsInteractive);
        }

        [Test]
        public void EntryPresenter_StartRequestInvokesStartCommandOnce()
        {
            var view = new FakeEntryPopupView();
            var commandHandler = new FakeCommandHandler();
            var presenter = new EntryPopupPresenter(
                view,
                commandHandler,
                new FakeSnapshotProvider(CreateNotStartedSnapshot()),
                new FakeCountdownProvider(new RaceCountdownSnapshot(true, 300L, false, null)),
                CreateSignalBus());
            presenter.Initialize();

            view.RaiseStartRequested();

            Assert.AreEqual(1, commandHandler.StartCount);
        }

        [Test]
        public void EntryPresenter_RepeatedInitializeDoesNotDuplicateStartSubscription()
        {
            var view = new FakeEntryPopupView();
            var commandHandler = new FakeCommandHandler();
            var presenter = new EntryPopupPresenter(
                view,
                commandHandler,
                new FakeSnapshotProvider(CreateNotStartedSnapshot()),
                new FakeCountdownProvider(new RaceCountdownSnapshot(true, 300L, false, null)),
                CreateSignalBus());
            presenter.Initialize();
            presenter.Initialize();

            view.RaiseStartRequested();

            Assert.AreEqual(1, commandHandler.StartCount);
        }

        [Test]
        public void EntryPresenter_DisposeUnsubscribesStartRequest()
        {
            var view = new FakeEntryPopupView();
            var commandHandler = new FakeCommandHandler();
            var presenter = new EntryPopupPresenter(
                view,
                commandHandler,
                new FakeSnapshotProvider(CreateNotStartedSnapshot()),
                new FakeCountdownProvider(new RaceCountdownSnapshot(true, 300L, false, null)),
                CreateSignalBus());
            presenter.Initialize();
            presenter.Dispose();

            view.RaiseStartRequested();

            Assert.AreEqual(0, commandHandler.StartCount);
        }

        [Test]
        public void PlaceholderPresenter_SuccessShowsWinAndClaimPublishesLevelResult()
        {
            var view = new FakePlaceholderLevelView();
            var levelResultReporter = new FakeLevelResultReporter();
            var presenter = new PlaceholderLevelPresenter(
                view,
                levelResultReporter,
                new FakeSnapshotProvider(CreateRunningSnapshot()),
                new FakeHostLevelProgressService(),
                CreateSignalBus());
            presenter.Initialize();

            view.RaiseSuccessRequested();

            Assert.AreEqual(PlaceholderLevelScreen.LevelWin, view.LastModel.Screen);
            Assert.AreEqual(0, levelResultReporter.ReportCount);

            view.RaiseLevelWinClaimRequested();

            Assert.AreEqual(1, levelResultReporter.ReportCount);
            Assert.AreEqual(LevelResult.Success, levelResultReporter.ReportedResults[0]);
            Assert.AreEqual(PlaceholderLevelScreen.Challenge, view.LastModel.Screen);
            Assert.AreEqual("LEVEL 2", view.LastModel.Title);
        }

        [Test]
        public void AudioPresenter_SwitchesMusicAndPlaysWinClaimCues()
        {
            var bus = CreateSignalBus();
            var audio = new FakeRaceAudioService();
            var menu = new FakeMainMenuView(true);
            var entry = new FakeEntryPopupView();
            var hud = new FakeRaceHudView();
            var presenter = new RaceAudioPresenter(audio, menu, entry, hud, bus);

            presenter.Initialize();
            bus.Fire(new HostGameplayStartedSignal());
            bus.Fire(new HostGameplayScreenChangedSignal(PlaceholderLevelScreen.LevelWin));
            bus.Fire(new HostGameplayCompletedSignal(LevelResult.Success, true));

            Assert.AreEqual(RaceMusicCue.Menu, audio.MusicHistory[0]);
            Assert.IsFalse(audio.CrossfadeHistory[0]);
            Assert.AreEqual(RaceMusicCue.Gameplay, audio.MusicHistory[1]);
            Assert.IsTrue(audio.CrossfadeHistory[1]);
            Assert.AreEqual(RaceMusicCue.Menu, audio.MusicHistory[2]);
            Assert.IsTrue(audio.CrossfadeHistory[2]);
            Assert.AreEqual(RaceSoundCue.ChallengePopupOpen, audio.SoundHistory[0]);
            Assert.AreEqual(RaceSoundCue.LevelWinPopupOpen, audio.SoundHistory[1]);
            Assert.AreEqual(RaceSoundCue.ClaimButtonClick, audio.SoundHistory[2]);
        }

        [Test]
        public void AudioPresenter_PlaysFailCueWithoutClaimCue()
        {
            var bus = CreateSignalBus();
            var audio = new FakeRaceAudioService();
            var menu = new FakeMainMenuView(true);
            var entry = new FakeEntryPopupView();
            var hud = new FakeRaceHudView();
            var presenter = new RaceAudioPresenter(audio, menu, entry, hud, bus);

            presenter.Initialize();
            bus.Fire(new HostGameplayStartedSignal());
            bus.Fire(new HostGameplayScreenChangedSignal(PlaceholderLevelScreen.LevelFail));
            bus.Fire(new HostGameplayCompletedSignal(LevelResult.Fail, false));

            Assert.AreEqual(2, audio.SoundCount);
            Assert.AreEqual(RaceSoundCue.ChallengePopupOpen, audio.SoundHistory[0]);
            Assert.AreEqual(RaceSoundCue.FailPopupOpen, audio.SoundHistory[1]);
            Assert.AreEqual(RaceMusicCue.Menu, audio.MusicHistory[2]);
        }

        [Test]
        public void AudioPresenter_PlaysButtonClickForNavbarAndFailBackHome()
        {
            var bus = CreateSignalBus();
            var audio = new FakeRaceAudioService();
            var menu = new FakeMainMenuView(true);
            var entry = new FakeEntryPopupView();
            var hud = new FakeRaceHudView();
            var presenter = new RaceAudioPresenter(audio, menu, entry, hud, bus);

            presenter.Initialize();
            menu.RaiseNavigationItemClicked();
            bus.Fire(new HostGameplayBackHomeClickedSignal());

            Assert.AreEqual(RaceSoundCue.ButtonClick, audio.SoundHistory[0]);
            Assert.AreEqual(RaceSoundCue.ButtonClick, audio.SoundHistory[1]);
        }

        [Test]
        public void AudioPresenter_PlaysButtonClickForLiveOpsStartAndPopupCloseButtons()
        {
            var bus = CreateSignalBus();
            var audio = new FakeRaceAudioService();
            var menu = new FakeMainMenuView(true);
            var entry = new FakeEntryPopupView();
            var hud = new FakeRaceHudView();
            var presenter = new RaceAudioPresenter(audio, menu, entry, hud, bus);

            presenter.Initialize();
            menu.RaiseThreadRaceRequested();
            entry.RaiseStartRequested();
            entry.RaiseCloseRequested();
            hud.RaiseCloseRequested();

            Assert.AreEqual(RaceSoundCue.ButtonClick, audio.SoundHistory[0]);
            Assert.AreEqual(RaceSoundCue.ButtonClick, audio.SoundHistory[1]);
            Assert.AreEqual(RaceSoundCue.ButtonClick, audio.SoundHistory[2]);
            Assert.AreEqual(RaceSoundCue.ButtonClick, audio.SoundHistory[3]);
        }

        [Test]
        public void PlaceholderPresenter_FailShowsFailAndReturnPublishesLevelResult()
        {
            var view = new FakePlaceholderLevelView();
            var levelResultReporter = new FakeLevelResultReporter();
            var presenter = new PlaceholderLevelPresenter(
                view,
                levelResultReporter,
                new FakeSnapshotProvider(CreateRunningSnapshot()),
                new FakeHostLevelProgressService(),
                CreateSignalBus());
            presenter.Initialize();

            view.RaiseFailRequested();

            Assert.AreEqual(PlaceholderLevelScreen.LevelFail, view.LastModel.Screen);
            Assert.AreEqual(0, levelResultReporter.ReportCount);

            view.RaiseLevelFailReturnRequested();

            Assert.AreEqual(1, levelResultReporter.ReportCount);
            Assert.AreEqual(LevelResult.Fail, levelResultReporter.ReportedResults[0]);
            Assert.AreEqual(PlaceholderLevelScreen.Challenge, view.LastModel.Screen);
            Assert.AreEqual("LEVEL 1", view.LastModel.Title);
        }

        [Test]
        public void PlaceholderPresenter_LevelResultOutsideRunningStillPublishesToHostSource()
        {
            var view = new FakePlaceholderLevelView();
            var levelResultReporter = new FakeLevelResultReporter();
            var presenter = new PlaceholderLevelPresenter(
                view,
                levelResultReporter,
                new FakeSnapshotProvider(CreateNotStartedSnapshot()),
                new FakeHostLevelProgressService(),
                CreateSignalBus());
            presenter.Initialize();

            view.RaiseSuccessRequested();
            view.RaiseLevelWinClaimRequested();

            Assert.AreEqual(1, levelResultReporter.ReportCount);
            Assert.AreEqual(LevelResult.Success, levelResultReporter.ReportedResults[0]);
            Assert.AreEqual(PlaceholderLevelScreen.Challenge, view.LastModel.Screen);
        }

        [Test]
        public void PlaceholderPresenter_ChallengeButtonsAreHostGameplayControlled()
        {
            var model = PlaceholderLevelPresenter.BuildModel(CreateNotStartedSnapshot());

            Assert.IsTrue(model.ButtonsEnabled);
            Assert.AreEqual(PlaceholderLevelScreen.Challenge, model.Screen);
        }

        [Test]
        public void PlaceholderPresenter_TitleIsIndependentFromRaceProgress()
        {
            var session = CreateStartedSession();
            session.ApplyPlayerResult(LevelResult.Success);
            var model = PlaceholderLevelPresenter.BuildModel(session.GetSnapshot());

            Assert.AreEqual("LEVEL 1", model.Title);
        }

        [Test]
        public void PlaceholderPresenter_TitleDoesNotExposeRaceFinishTarget()
        {
            var snapshot = CreateCompletedRewardedSnapshot();
            var model = PlaceholderLevelPresenter.BuildModel(snapshot);

            Assert.AreEqual("LEVEL 1", model.Title);
            StringAssert.DoesNotContain("/", model.Title);
        }

        [Test]
        public void PlaceholderPresenter_InitializesFromPersistedHostLevel()
        {
            var view = new FakePlaceholderLevelView();
            var levelResultReporter = new FakeLevelResultReporter();
            var presenter = new PlaceholderLevelPresenter(
                view,
                levelResultReporter,
                new FakeSnapshotProvider(CreateRunningSnapshot()),
                new FakeHostLevelProgressService(5),
                CreateSignalBus());

            presenter.Initialize();

            Assert.AreEqual("LEVEL 5", view.LastModel.Title);
        }

        [Test]
        public void PlaceholderPresenter_SuccessPersistsNextHostLevel()
        {
            var view = new FakePlaceholderLevelView();
            var levelResultReporter = new FakeLevelResultReporter();
            var progressService = new FakeHostLevelProgressService(5);
            var presenter = new PlaceholderLevelPresenter(
                view,
                levelResultReporter,
                new FakeSnapshotProvider(CreateRunningSnapshot()),
                progressService,
                CreateSignalBus());

            presenter.Initialize();
            view.RaiseSuccessRequested();
            view.RaiseLevelWinClaimRequested();

            Assert.AreEqual(6, progressService.SavedLevel);
            Assert.AreEqual("LEVEL 6", view.LastModel.Title);
        }

        [Test]
        public void PlaceholderPresenter_DisposeUnsubscribesRequests()
        {
            var view = new FakePlaceholderLevelView();
            var levelResultReporter = new FakeLevelResultReporter();
            var presenter = new PlaceholderLevelPresenter(
                view,
                levelResultReporter,
                new FakeSnapshotProvider(CreateRunningSnapshot()),
                new FakeHostLevelProgressService(),
                CreateSignalBus());
            presenter.Initialize();
            presenter.Dispose();

            view.RaiseSuccessRequested();
            view.RaiseFailRequested();
            view.RaiseLevelWinClaimRequested();
            view.RaiseLevelFailReturnRequested();

            Assert.AreEqual(0, levelResultReporter.ReportCount);
        }

        [Test]
        public void HudPresenter_FiveSnapshotRacersProduceFiveRowsMappedById()
        {
            var model = RaceHudPresenter.BuildModel(CreateRunningSnapshot());

            Assert.AreEqual(5, model.Rows.Count);
            Assert.AreEqual(RaceTestSupport.PlayerId, model.Rows[0].RacerId);
            Assert.AreEqual(RaceTestSupport.Ai1Id, model.Rows[1].RacerId);
        }

        [Test]
        public void HudPresenter_PlayerRowIsMarkedAsPlayer()
        {
            var model = RaceHudPresenter.BuildModel(CreateRunningSnapshot());

            Assert.IsTrue(model.Rows[0].IsPlayer);
            Assert.IsFalse(model.Rows[1].IsPlayer);
        }

        [Test]
        public void HudPresenter_UsesSnapshotRankProgressTextAndNormalizedProgress()
        {
            var session = CreateStartedSession();
            session.ApplyPlayerResult(LevelResult.Success);

            var model = RaceHudPresenter.BuildModel(session.GetSnapshot());

            Assert.AreEqual(1, model.Rows[0].CurrentRank);
            Assert.AreEqual("1 / 10", model.Rows[0].ProgressText);
            Assert.AreEqual(0.1f, model.Rows[0].NormalizedProgress, 0.0001f);
        }

        [Test]
        public void HudPresenter_RepresentsFinishedStateAndPlacement()
        {
            var model = RaceHudPresenter.BuildModel(CreateCompletedRewardedSnapshot());

            Assert.IsTrue(model.Rows[0].IsFinished);
            Assert.AreEqual(1, model.Rows[0].FinishPlacement);
        }

        [Test]
        public void HudPresenter_UpdatedRanksProduceUpdatedTargetSlots()
        {
            var session = CreateStartedSession();
            session.AdvanceAi(1f);
            var model = RaceHudPresenter.BuildModel(session.GetSnapshot());

            Assert.AreEqual(0, model.Rows[1].TargetSlotIndex);
            Assert.AreEqual(1, model.Rows[2].TargetSlotIndex);
            Assert.AreEqual(2, model.Rows[3].TargetSlotIndex);
        }

        [Test]
        public void HudPresenter_DoesNotInventPresentationRanking()
        {
            var snapshot = CreateRunningSnapshot();
            var model = RaceHudPresenter.BuildModel(snapshot);

            for (var i = 0; i < snapshot.Racers.Count; i++)
            {
                Assert.AreEqual(snapshot.Racers[i].CurrentRank, model.Rows[i].CurrentRank);
            }
        }

        [Test]
        public void EntryPresenter_ShowsLiveTimeLeft()
        {
            var view = new FakeEntryPopupView();
            var presenter = new EntryPopupPresenter(
                view,
                new FakeCommandHandler(),
                new FakeSnapshotProvider(CreateNotStartedSnapshot()),
                new FakeCountdownProvider(new RaceCountdownSnapshot(true, RaceEventSettings.DefaultEventDurationSeconds, false, null)),
                CreateSignalBus());

            presenter.Initialize();

            Assert.AreEqual("TIME LEFT: 3d 00h", view.DurationLine);
            Assert.IsTrue(view.StartEnabled);
        }

        [Test]
        public void EntryPresenter_CountdownSignalUpdatesTimeLeftAndDisablesExpiredStart()
        {
            var view = new FakeEntryPopupView();
            var bus = CreateSignalBus();
            var provider = new FakeCountdownProvider(new RaceCountdownSnapshot(true, 60L, false, null));
            var presenter = new EntryPopupPresenter(
                view,
                new FakeCommandHandler(),
                new FakeSnapshotProvider(CreateNotStartedSnapshot()),
                provider,
                bus);

            presenter.Initialize();
            bus.Fire(new RaceCountdownChangedSignal(new RaceCountdownSnapshot(true, 0L, true, null)));

            Assert.AreEqual("TIME LEFT: ENDED", view.DurationLine);
            Assert.IsFalse(view.StartEnabled);
        }

        [Test]
        public void CountdownFormatter_FormatsDaysHoursMinutesAndZero()
        {
            Assert.AreEqual("2d 05h", RaceCountdownFormatter.FormatCountdown(2 * 86400L + 5 * 3600L + 12));
            Assert.AreEqual("05h 32m", RaceCountdownFormatter.FormatCountdown(5 * 3600L + 32 * 60L + 50));
            Assert.AreEqual("32m 18s", RaceCountdownFormatter.FormatCountdown(32 * 60L + 18));
            Assert.AreEqual("5m", RaceCountdownFormatter.FormatCountdown(5 * 60L));
            Assert.AreEqual("59s", RaceCountdownFormatter.FormatCountdown(59L));
            Assert.AreEqual("00:00", RaceCountdownFormatter.FormatCountdown(0));
        }

        [Test]
        public void CountdownFormatter_FormatsExactMinuteDurationWithoutLeadingZeroOrZeroSeconds()
        {
            Assert.AreEqual("EVENT DURATION: 5 MIN", RaceCountdownFormatter.FormatDurationLine(300L));
        }

        [Test]
        public void HudPresenter_UsesCountdownSnapshotText()
        {
            var model = RaceHudPresenter.BuildModel(CreateRunningSnapshot(), new RaceCountdownSnapshot(true, 3720L, false, null));

            Assert.AreEqual("01h 02m", model.CountdownText);
        }

        [Test]
        public void HudPresenter_CompletedSnapshotShowsEndedCountdown()
        {
            var model = RaceHudPresenter.BuildModel(
                CreateCompletedRewardedSnapshot(),
                new RaceCountdownSnapshot(false, 0L, false, null));

            Assert.AreEqual("ENDED", model.CountdownText);
        }

        [Test]
        public void HudPresenter_CountdownSignalUpdatesTextWithoutRebuildingRows()
        {
            var snapshot = CreateRunningSnapshot();
            var view = new FakeRaceHudView();
            var provider = new FakeCountdownProvider(new RaceCountdownSnapshot(true, 3600L, false, null));
            var bus = CreateSignalBus();
            var presenter = new RaceHudPresenter(view, new FakeSnapshotProvider(snapshot), provider, bus);

            presenter.Initialize();
            var initialRenderCount = view.RenderCount;
            var initialCountdownSetCount = view.CountdownTextSetCount;
            bus.Fire(new RaceCountdownChangedSignal(new RaceCountdownSnapshot(true, 3600L, false, null)));
            bus.Fire(new RaceCountdownChangedSignal(new RaceCountdownSnapshot(true, 3599L, false, null)));

            Assert.AreEqual(initialRenderCount, view.RenderCount);
            Assert.AreEqual(initialCountdownSetCount + 1, view.CountdownTextSetCount);
            Assert.AreEqual("59m 59s", view.LastCountdownText);
        }

        [Test]
        public void ResultPresenter_RewardedPlayerFinishShowsActualPlacementAndReward()
        {
            var model = RaceResultPresenter.BuildModel(CreateCompletedRewardedSnapshot());

            Assert.AreEqual("YOU FINISHED #1", model.PlayerPlacementText);
            Assert.IsTrue(model.RewardEligible);
            Assert.AreEqual("REWARD\n1000 Coins", model.RewardStatusText);
            Assert.AreEqual("thread_race_rank_1_coins", model.RewardId);
            Assert.AreEqual(RewardType.Coins, model.RewardType);
            Assert.AreEqual(1000, model.RewardAmount);
            Assert.AreEqual("coin_stack", model.RewardIconId);
        }

        [Test]
        public void ResultPresenter_NonRewardedAndDnfShowNoReward()
        {
            var dnfModel = RaceResultPresenter.BuildModel(CreateCompletedDnfSnapshot());

            Assert.AreEqual("YOU DID NOT FINISH", dnfModel.PlayerPlacementText);
            Assert.IsFalse(dnfModel.RewardEligible);
            Assert.AreEqual("NO REWARD", dnfModel.RewardStatusText);
        }

        [Test]
        public void ResultPresenter_EventExpiredShowsTimeUpDnfAndNoReward()
        {
            var model = RaceResultPresenter.BuildModel(CreateExpiredSnapshot());

            Assert.AreEqual("TIME'S UP", model.Title);
            Assert.AreEqual("YOU DID NOT FINISH", model.PlayerPlacementText);
            Assert.AreEqual("NO REWARD", model.RewardStatusText);
            Assert.IsFalse(model.RewardEligible);
        }

        [Test]
        public void ResultPresenter_ActualFinishersPopulatePodiumSlotsWithoutFabrication()
        {
            var model = RaceResultPresenter.BuildModel(CreateCompletedRewardedSnapshot());

            Assert.AreEqual(3, model.PodiumSlots.Count);
            Assert.IsTrue(model.PodiumSlots[0].IsFilled);
            Assert.AreEqual("Player", model.PodiumSlots[0].DisplayName);
            Assert.IsFalse(model.PodiumSlots[1].IsFilled);
            Assert.AreEqual("-", model.PodiumSlots[1].DisplayName);
        }

        [Test]
        public void ResultPresenter_ContinueClaimsRewardOnceAndDisposeUnsubscribes()
        {
            var view = new FakeRaceResultView();
            var commandHandler = new FakeCommandHandler();
            var presenter = new RaceResultPresenter(view, commandHandler, new FakeSnapshotProvider(CreateCompletedRewardedSnapshot()), CreateSignalBus());
            presenter.Initialize();

            view.RaiseContinueRequested();
            presenter.Dispose();
            view.RaiseContinueRequested();

            Assert.AreEqual(1, commandHandler.ClaimRewardCount);
            Assert.AreEqual(0, commandHandler.ResetCount);
        }

        private static RaceFlowPresenter CreateFlowPresenter(
            RaceSnapshot snapshot,
            ViewSet views,
            SignalBus signalBus = null,
            IRaceCountdownProvider countdownProvider = null,
            IRaceEventCommandHandler commandHandler = null)
        {
            return CreateFlowPresenter(
                new FakeSnapshotProvider(snapshot),
                views,
                signalBus,
                countdownProvider,
                commandHandler);
        }

        private static RaceFlowPresenter CreateFlowPresenter(
            FakeSnapshotProvider snapshotProvider,
            ViewSet views,
            SignalBus signalBus = null,
            IRaceCountdownProvider countdownProvider = null,
            IRaceEventCommandHandler commandHandler = null)
        {
            return new RaceFlowPresenter(
                views.Menu,
                views.Entry,
                views.Hud,
                views.Level,
                views.Result,
                snapshotProvider,
                countdownProvider ?? new FakeCountdownProvider(new RaceCountdownSnapshot(true, 300L, false, null)),
                commandHandler ?? new FakeCommandHandler(),
                signalBus ?? CreateSignalBus());
        }

        private static RaceSession CreateStartedSession()
        {
            var session = RaceTestSupport.CreateSession(RaceTestSupport.CreateSettings());
            session.Start();
            return session;
        }

        private static RaceSnapshot CreateNotStartedSnapshot()
        {
            return RaceTestSupport.CreateSession(RaceTestSupport.CreateSettings()).GetSnapshot();
        }

        private static RaceSnapshot CreateRunningSnapshot()
        {
            return CreateStartedSession().GetSnapshot();
        }

        private static RaceSnapshot CreateCompletedRewardedSnapshot()
        {
            var session = RaceTestSupport.CreateSession(RaceTestSupport.CreateSettings(finishTarget: 1));
            session.Start();
            session.ApplyPlayerResult(LevelResult.Success);
            return session.GetSnapshot();
        }

        private static RaceSnapshot CreateClaimedRewardedSnapshot()
        {
            var session = RaceTestSupport.CreateSession(RaceTestSupport.CreateSettings(finishTarget: 1));
            session.Start();
            session.ApplyPlayerResult(LevelResult.Success);
            session.ClaimReward();
            return session.GetSnapshot();
        }

        private static RaceSnapshot CreateCompletedDnfSnapshot()
        {
            var session = RaceTestSupport.CreateSession(RaceTestSupport.CreateSettings(finishTarget: 1));
            session.Start();
            session.AdvanceAi(1f);
            return session.GetSnapshot();
        }

        private static RaceSnapshot CreateExpiredSnapshot()
        {
            var session = RaceTestSupport.CreateSession(RaceTestSupport.CreateSettings(finishTarget: 10));
            session.Start();
            session.ExpireEvent();
            return session.GetSnapshot();
        }

        private static SignalBus CreateSignalBus()
        {
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<RaceSnapshotChangedSignal>();
            container.DeclareSignal<RaceCountdownChangedSignal>();
            container.DeclareSignal<HostGameplayStartedSignal>();
            container.DeclareSignal<HostLevelChangedSignal>();
            container.DeclareSignal<HostGameplayScreenChangedSignal>();
            container.DeclareSignal<HostGameplayBackHomeClickedSignal>();
            container.DeclareSignal<HostGameplayCompletedSignal>();
            return container.Resolve<SignalBus>();
        }

        private static void AssertVisibility(ViewSet views, bool entry, bool hud, bool level, bool result)
        {
            Assert.AreEqual(entry, views.Entry.IsVisible);
            Assert.AreEqual(hud, views.Hud.IsVisible);
            Assert.AreEqual(level, views.Level.IsVisible);
            Assert.AreEqual(result, views.Result.IsVisible);
        }

        private static void AssertMenuVisibility(ViewSet views, bool visible, bool interactive)
        {
            Assert.AreEqual(visible, views.Menu.IsVisible);
            Assert.AreEqual(interactive, views.Menu.IsInteractive);
        }

        private sealed class ViewSet
        {
            public readonly FakeMainMenuView Menu;
            public readonly FakeEntryPopupView Entry = new FakeEntryPopupView();
            public readonly FakeRaceHudView Hud = new FakeRaceHudView();
            public readonly FakePlaceholderLevelView Level = new FakePlaceholderLevelView();
            public readonly FakeRaceResultView Result = new FakeRaceResultView();

            public ViewSet()
                : this(new FakeMainMenuView(true))
            {
            }

            public ViewSet(FakeMainMenuView menu)
            {
                Menu = menu;
            }
        }

        private sealed class FakeSnapshotProvider : IRaceSnapshotProvider
        {
            public FakeSnapshotProvider(RaceSnapshot snapshot)
            {
                CurrentSnapshot = snapshot;
            }

            public RaceSnapshot CurrentSnapshot { get; set; }
        }

        private sealed class FakeCountdownProvider : IRaceCountdownProvider
        {
            public FakeCountdownProvider(RaceCountdownSnapshot snapshot)
            {
                CurrentCountdown = snapshot;
            }

            public RaceCountdownSnapshot CurrentCountdown { get; set; }
        }

        private sealed class FakeCommandHandler : IRaceEventCommandHandler
        {
            public readonly LevelResult[] ReportedResults = new LevelResult[8];

            public int StartCount { get; private set; }

            public int ReportCount { get; private set; }

            public int ResetCount { get; private set; }

            public int ClaimRewardCount { get; private set; }

            public int ResolveExpiredEventCount { get; private set; }

            public Func<bool> ResolveExpiredEventCallback { get; set; }

            public bool StartRace()
            {
                StartCount++;
                return true;
            }

            public bool ReportLevelResult(LevelResult result)
            {
                ReportedResults[ReportCount] = result;
                ReportCount++;
                return result == LevelResult.Success;
            }

            public bool ResolveExpiredEvent()
            {
                ResolveExpiredEventCount++;
                return ResolveExpiredEventCallback != null && ResolveExpiredEventCallback();
            }

            public bool ClaimReward()
            {
                ClaimRewardCount++;
                return true;
            }

            public void ResetRace()
            {
                ResetCount++;
            }
        }

        private sealed class FakeLevelResultReporter : ILevelResultReporter
        {
            public readonly LevelResult[] ReportedResults = new LevelResult[8];

            public int ReportCount { get; private set; }

            public void Report(LevelResult result)
            {
                ReportedResults[ReportCount] = result;
                ReportCount++;
            }
        }

        private sealed class FakeHostLevelProgressService : IHostLevelProgressService
        {
            private readonly int _loadedLevel;
            private bool _hasLoaded;

            public FakeHostLevelProgressService(int loadedLevel = 1)
            {
                _loadedLevel = loadedLevel;
            }

            public int CurrentLevel { get; private set; } = 1;

            public int SavedLevel { get; private set; }

            public int LoadCurrentLevel()
            {
                CurrentLevel = Math.Max(1, _loadedLevel);
                _hasLoaded = true;
                return CurrentLevel;
            }

            public int AdvanceAfterSuccess()
            {
                if (!_hasLoaded)
                {
                    LoadCurrentLevel();
                }

                CurrentLevel++;
                SavedLevel = CurrentLevel;
                return CurrentLevel;
            }
        }

        private abstract class FakePhaseView : IPhaseView
        {
            public bool IsVisible { get; private set; }

            public bool IsInteractive => IsVisible;

            public void SetVisible(bool visible)
            {
                IsVisible = visible;
            }
        }

        private sealed class FakeMainMenuView : IMainMenuView
        {
            public FakeMainMenuView(bool isAvailable)
            {
                IsAvailable = isAvailable;
            }

            public event Action PlayRequested;

            public event Action ThreadRaceRequested;

            public event Action NavigationItemClicked;

            public bool IsVisible { get; private set; }

            public bool IsInteractive { get; private set; }

            public bool IsAvailable { get; }

            public void SetVisible(bool visible, bool interactive)
            {
                IsVisible = visible;
                IsInteractive = visible && interactive;
            }

            public string ThreadRaceCountdownText { get; private set; }

            public string PlayButtonLabel { get; private set; }

            public void SetThreadRaceCountdown(string countdownText)
            {
                ThreadRaceCountdownText = countdownText;
            }

            public void SetPlayButtonLabel(string label)
            {
                PlayButtonLabel = label;
            }

            public void RaiseThreadRaceRequested()
            {
                ThreadRaceRequested?.Invoke();
            }

            public void RaisePlayRequested()
            {
                PlayRequested?.Invoke();
            }

            public void RaiseNavigationItemClicked()
            {
                NavigationItemClicked?.Invoke();
            }
        }

        private sealed class FakeEntryPopupView : FakePhaseView, IEntryPopupView
        {
            public event Action StartRequested;

            public event Action CloseRequested;

            public bool StartEnabled { get; private set; }

            public string DurationLine { get; private set; }

            public void SetStartEnabled(bool enabled)
            {
                StartEnabled = enabled;
            }

            public void SetContent(string title, string body, string rule, string durationLine)
            {
                DurationLine = durationLine;
            }

            public void RaiseStartRequested()
            {
                StartRequested?.Invoke();
            }

            public void RaiseCloseRequested()
            {
                CloseRequested?.Invoke();
            }
        }

        private sealed class FakeRaceHudView : FakePhaseView, IRaceHudView
        {
            public event Action CloseRequested;

            public RaceHudModel LastModel { get; private set; }

            public int RenderCount { get; private set; }

            public string LastCountdownText { get; private set; }

            public int CountdownTextSetCount { get; private set; }

            public void Render(RaceHudModel model)
            {
                LastModel = model;
                RenderCount++;
                SetCountdownText(model.CountdownText);
            }

            public void SetCountdownText(string countdownText)
            {
                LastCountdownText = countdownText ?? string.Empty;
                CountdownTextSetCount++;
            }

            public void RaiseCloseRequested()
            {
                CloseRequested?.Invoke();
            }
        }

        private sealed class FakePlaceholderLevelView : FakePhaseView, IPlaceholderLevelView
        {
            public event Action SuccessRequested;

            public event Action FailRequested;

            public event Action LevelWinClaimRequested;

            public event Action LevelFailReturnRequested;

            public PlaceholderLevelModel LastModel { get; private set; }

            public void Render(PlaceholderLevelModel model)
            {
                LastModel = model;
            }

            public void RaiseSuccessRequested()
            {
                SuccessRequested?.Invoke();
            }

            public void RaiseFailRequested()
            {
                FailRequested?.Invoke();
            }

            public void RaiseLevelWinClaimRequested()
            {
                LevelWinClaimRequested?.Invoke();
            }

            public void RaiseLevelFailReturnRequested()
            {
                LevelFailReturnRequested?.Invoke();
            }
        }

        private sealed class FakeRaceResultView : FakePhaseView, IRaceResultView
        {
            public event Action ContinueRequested;

            public RaceResultModel LastModel { get; private set; }

            public void Render(RaceResultModel model)
            {
                LastModel = model;
            }

            public void RaiseContinueRequested()
            {
                ContinueRequested?.Invoke();
            }
        }

        private sealed class FakeRaceAudioService : IRaceAudioService
        {
            public readonly RaceMusicCue[] MusicHistory = new RaceMusicCue[8];
            public readonly bool[] CrossfadeHistory = new bool[8];
            public readonly RaceSoundCue[] SoundHistory = new RaceSoundCue[8];

            public int MusicCount { get; private set; }

            public int SoundCount { get; private set; }

            public RaceMusicCue CurrentMusicCue { get; private set; }

            public bool IsMusicPlaying => CurrentMusicCue != RaceMusicCue.None;

            public void PlayMusic(RaceMusicCue cue, bool crossfade = true)
            {
                MusicHistory[MusicCount] = cue;
                CrossfadeHistory[MusicCount] = crossfade;
                MusicCount++;
                CurrentMusicCue = cue;
            }

            public void StopMusic(bool fadeOut = true)
            {
                CurrentMusicCue = RaceMusicCue.None;
            }

            public void PauseMusic()
            {
            }

            public void ResumeMusic()
            {
            }

            public void PlaySound(RaceSoundCue cue, float volumeScale = 1f)
            {
                SoundHistory[SoundCount] = cue;
                SoundCount++;
            }

            public void SetMusicVolume(float volume)
            {
            }

            public void SetSfxVolume(float volume)
            {
            }

            public float GetMusicVolume()
            {
                return 1f;
            }

            public float GetSfxVolume()
            {
                return 1f;
            }

            public void ToggleMusicMute()
            {
            }

            public void ToggleSfxMute()
            {
            }

            public void SetMusicMuted(bool muted)
            {
            }

            public void SetSfxMuted(bool muted)
            {
            }

            public bool IsMusicMuted()
            {
                return false;
            }

            public bool IsSfxMuted()
            {
                return false;
            }
        }
    }
}
