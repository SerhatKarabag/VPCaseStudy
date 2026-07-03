using System;

namespace ThreadRace.Core.Progress
{
    public sealed class HostLevelProgressService : IHostLevelProgressService
    {
        private const int MinimumLevel = 1;

        private readonly IHostLevelProgressRepository _repository;

        private int _currentLevel;
        private bool _hasLoaded;

        public HostLevelProgressService(IHostLevelProgressRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public int CurrentLevel
        {
            get
            {
                EnsureLoaded();
                return _currentLevel;
            }
        }

        public int LoadCurrentLevel()
        {
            _currentLevel = NormalizeLevel(_repository.LoadCurrentLevel());
            _hasLoaded = true;
            return _currentLevel;
        }

        public int AdvanceAfterSuccess()
        {
            EnsureLoaded();
            if (_currentLevel == int.MaxValue)
            {
                throw new InvalidOperationException("Host level cannot advance beyond Int32.MaxValue.");
            }

            var nextLevel = _currentLevel + 1;
            _repository.SaveCurrentLevel(nextLevel);
            _currentLevel = nextLevel;
            return _currentLevel;
        }

        private void EnsureLoaded()
        {
            if (!_hasLoaded)
            {
                LoadCurrentLevel();
            }
        }

        private static int NormalizeLevel(int level)
        {
            return level < MinimumLevel ? MinimumLevel : level;
        }
    }
}
