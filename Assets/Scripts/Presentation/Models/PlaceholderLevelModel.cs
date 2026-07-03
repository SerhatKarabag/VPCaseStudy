namespace ThreadRace.Presentation.Models
{
    public enum PlaceholderLevelScreen
    {
        Challenge = 0,
        LevelWin = 1,
        LevelFail = 2
    }

    public sealed class PlaceholderLevelModel
    {
        public PlaceholderLevelModel(string title, string instruction, bool buttonsEnabled)
            : this(title, instruction, buttonsEnabled, PlaceholderLevelScreen.Challenge, string.Empty)
        {
        }

        public PlaceholderLevelModel(
            string title,
            string instruction,
            bool buttonsEnabled,
            PlaceholderLevelScreen screen,
            string coinRewardText)
        {
            Title = title;
            Instruction = instruction;
            ButtonsEnabled = buttonsEnabled;
            Screen = screen;
            CoinRewardText = coinRewardText ?? string.Empty;
        }

        public string Title { get; }

        public string Instruction { get; }

        public bool ButtonsEnabled { get; }

        public PlaceholderLevelScreen Screen { get; }

        public string CoinRewardText { get; }
    }
}
