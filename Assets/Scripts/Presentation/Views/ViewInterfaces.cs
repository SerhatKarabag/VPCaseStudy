using System;
using ThreadRace.Presentation.Models;

namespace ThreadRace.Presentation.Views
{
    public interface IMainMenuView
    {
        event Action PlayRequested;

        event Action ThreadRaceRequested;

        event Action NavigationItemClicked;

        void SetVisible(bool visible, bool interactive);

        void SetThreadRaceCountdown(string countdownText);

        void SetPlayButtonLabel(string label);

        bool IsVisible { get; }

        bool IsInteractive { get; }

        bool IsAvailable { get; }
    }

    public interface IPhaseView
    {
        void SetVisible(bool visible);

        bool IsVisible { get; }

        bool IsInteractive { get; }
    }

    public interface IEntryPopupView : IPhaseView
    {
        event Action StartRequested;

        event Action CloseRequested;

        void SetContent(string title, string body, string rule, string durationLine);

        void SetStartEnabled(bool enabled);
    }

    public interface IRaceHudView : IPhaseView
    {
        event Action CloseRequested;

        void Render(RaceHudModel model);

        void SetCountdownText(string countdownText);
    }

    public interface IPlaceholderLevelView : IPhaseView
    {
        event Action SuccessRequested;

        event Action FailRequested;

        event Action LevelWinClaimRequested;

        event Action LevelFailReturnRequested;

        void Render(PlaceholderLevelModel model);
    }

    public interface IRaceResultView : IPhaseView
    {
        event Action ContinueRequested;

        void Render(RaceResultModel model);
    }
}
