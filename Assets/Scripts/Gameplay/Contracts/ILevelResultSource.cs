using System;
using ThreadRace.Gameplay.Domain;

namespace ThreadRace.Gameplay.Contracts
{
    public interface ILevelResultSource
    {
        event Action<LevelResult> ResultReported;
    }
}
