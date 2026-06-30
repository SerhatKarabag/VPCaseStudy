namespace ThreadRace.Gameplay.Application
{
    public enum RaceControllerInitializationStatus
    {
        NoSave = 0,
        Restored = 1,
        InvalidSave = 2,
        LoadFailed = 3,
        Reset = 4
    }
}
