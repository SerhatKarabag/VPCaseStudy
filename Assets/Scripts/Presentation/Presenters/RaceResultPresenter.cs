using System;
using ThreadRace.Gameplay.Config;
using ThreadRace.Gameplay.Contracts;
using ThreadRace.Gameplay.Domain;
using ThreadRace.Presentation.Models;
using ThreadRace.Presentation.Signals;
using ThreadRace.Presentation.Views;
using Zenject;

namespace ThreadRace.Presentation.Presenters
{
    public sealed class RaceResultPresenter : IInitializable, IDisposable
    {
        private readonly IRaceResultView _view;
        private readonly IRaceEventCommandHandler _commandHandler;
        private readonly IRaceSnapshotProvider _snapshotProvider;
        private readonly SignalBus _signalBus;

        private bool _initialized;

        public RaceResultPresenter(
            IRaceResultView view,
            IRaceEventCommandHandler commandHandler,
            IRaceSnapshotProvider snapshotProvider,
            SignalBus signalBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
            _signalBus = signalBus ?? throw new ArgumentNullException(nameof(signalBus));
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _view.ContinueRequested += OnContinueRequested;
            _signalBus.Subscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _initialized = true;
            ApplySnapshot(_snapshotProvider.CurrentSnapshot);
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            _view.ContinueRequested -= OnContinueRequested;
            _signalBus.Unsubscribe<RaceSnapshotChangedSignal>(OnSnapshotChanged);
            _initialized = false;
        }

        public void ApplySnapshot(RaceSnapshot snapshot)
        {
            _view.Render(BuildModel(snapshot));
        }

        public static RaceResultModel BuildModel(RaceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var outcome = snapshot.PlayerOutcome;
            var expired = outcome != null && outcome.CompletionReason == RaceCompletionReason.EventExpired;
            var didFinish = outcome != null && outcome.DidFinish;
            var rewardEligible = outcome != null && outcome.IsRewardEligible;
            var rewardTier = rewardEligible && outcome.FinishPlacement.HasValue
                ? FindRewardTier(snapshot, outcome.FinishPlacement.Value)
                : null;
            var placementText = expired
                ? "YOU DID NOT FINISH"
                : didFinish && outcome.FinishPlacement.HasValue
                ? "YOU FINISHED #" + outcome.FinishPlacement.Value.ToString()
                : "YOU DID NOT FINISH";
            var rewardText = BuildRewardStatusText(rewardEligible, rewardTier, snapshot.RewardClaimed);

            var slotCount = snapshot.RewardedPositionCount;
            var slots = new ResultPodiumSlotModel[slotCount];
            for (var i = 0; i < slotCount; i++)
            {
                var placement = i + 1;
                var finisher = FindFinisher(snapshot, placement);
                var slotRewardTier = FindRewardTier(snapshot, placement);
                if (finisher == null)
                {
                    slots[i] = new ResultPodiumSlotModel(
                        placement,
                        "-",
                        false,
                        false,
                        slotRewardTier == null ? string.Empty : slotRewardTier.DisplayText,
                        slotRewardTier == null ? string.Empty : slotRewardTier.IconId);
                    continue;
                }

                var racer = FindRacer(snapshot, finisher.RacerId);
                slots[i] = new ResultPodiumSlotModel(
                    placement,
                    racer == null ? finisher.RacerId.Value : racer.DisplayName,
                    true,
                    racer != null && racer.RacerType == RacerType.Player,
                    slotRewardTier == null ? string.Empty : slotRewardTier.DisplayText,
                    slotRewardTier == null ? string.Empty : slotRewardTier.IconId);
            }

            return new RaceResultModel(
                expired ? "TIME'S UP" : "RACE COMPLETE",
                placementText,
                rewardText,
                rewardEligible,
                rewardTier == null ? string.Empty : rewardTier.RewardId,
                rewardTier == null ? RewardType.Custom : rewardTier.RewardType,
                rewardTier == null ? 0 : rewardTier.Amount,
                rewardTier == null ? string.Empty : rewardTier.DisplayText,
                rewardTier == null ? string.Empty : rewardTier.IconId,
                snapshot.RewardClaimed,
                slots);
        }

        private void OnContinueRequested()
        {
            _commandHandler.ClaimReward();
        }

        private void OnSnapshotChanged(RaceSnapshotChangedSignal signal)
        {
            ApplySnapshot(signal.Snapshot);
        }

        private static FinishedRacerResult FindFinisher(RaceSnapshot snapshot, int placement)
        {
            for (var i = 0; i < snapshot.Finishers.Count; i++)
            {
                if (snapshot.Finishers[i].FinishPlacement == placement)
                {
                    return snapshot.Finishers[i];
                }
            }

            return null;
        }

        private static RacerSnapshot FindRacer(RaceSnapshot snapshot, RacerId racerId)
        {
            for (var i = 0; i < snapshot.Racers.Count; i++)
            {
                if (snapshot.Racers[i].Id == racerId)
                {
                    return snapshot.Racers[i];
                }
            }

            return null;
        }

        private static RewardTierDefinition FindRewardTier(RaceSnapshot snapshot, int rank)
        {
            for (var i = 0; i < snapshot.RewardTiers.Count; i++)
            {
                if (snapshot.RewardTiers[i].Rank == rank)
                {
                    return snapshot.RewardTiers[i];
                }
            }

            return null;
        }

        private static string BuildRewardStatusText(bool rewardEligible, RewardTierDefinition rewardTier, bool rewardClaimed)
        {
            if (!rewardEligible)
            {
                return "NO REWARD";
            }

            if (rewardTier == null)
            {
                return "REWARD CONFIG MISSING";
            }

            return rewardClaimed
                ? "REWARD CLAIMED\n" + rewardTier.DisplayText
                : "REWARD\n" + rewardTier.DisplayText;
        }
    }
}
