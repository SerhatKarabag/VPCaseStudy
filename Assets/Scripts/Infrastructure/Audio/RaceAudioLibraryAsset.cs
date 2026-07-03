using System;
using ThreadRace.Core.Audio;
using UnityEngine;

namespace ThreadRace.Infrastructure.Audio
{
    [CreateAssetMenu(fileName = "RaceAudioLibraryAsset", menuName = "Thread Race/Audio Library")]
    public sealed class RaceAudioLibraryAsset : ScriptableObject
    {
        [SerializeField] private RaceMusicClipReference[] _musicClips;
        [SerializeField] private RaceSoundClipReference[] _soundClips;
        [SerializeField, Min(0f)] private float _crossfadeDurationSeconds = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _defaultMusicVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _defaultSfxVolume = 1f;
        [SerializeField] private RaceMusicCue _startupMusicCue = RaceMusicCue.Menu;

        public RaceAudioLibrary ToRuntimeLibrary()
        {
            ValidateOrThrow();

            var musicClips = new AudioClip[GetEnumArrayLength<RaceMusicCue>()];
            var soundClips = new AudioClip[GetEnumArrayLength<RaceSoundCue>()];

            for (var i = 0; i < _musicClips.Length; i++)
            {
                musicClips[(int)_musicClips[i].Cue] = _musicClips[i].Clip;
            }

            for (var i = 0; i < _soundClips.Length; i++)
            {
                soundClips[(int)_soundClips[i].Cue] = _soundClips[i].Clip;
            }

            return new RaceAudioLibrary(
                musicClips,
                soundClips,
                _crossfadeDurationSeconds,
                _defaultMusicVolume,
                _defaultSfxVolume,
                _startupMusicCue);
        }

        public void ValidateOrThrow()
        {
            if (float.IsNaN(_crossfadeDurationSeconds) || _crossfadeDurationSeconds < 0f)
            {
                throw new InvalidOperationException($"{name} has an invalid audio crossfade duration.");
            }

            ValidateMusicClips();
            ValidateSoundClips();

            if (_startupMusicCue != RaceMusicCue.None && !HasMusicCue(_startupMusicCue))
            {
                throw new InvalidOperationException($"{name} startup music cue '{_startupMusicCue}' has no clip.");
            }
        }

        private void ValidateMusicClips()
        {
            if (_musicClips == null || _musicClips.Length == 0)
            {
                throw new InvalidOperationException($"{name} requires at least one music clip.");
            }

            var seen = new bool[GetEnumArrayLength<RaceMusicCue>()];
            for (var i = 0; i < _musicClips.Length; i++)
            {
                var entry = _musicClips[i];
                if (entry.Cue == RaceMusicCue.None)
                {
                    throw new InvalidOperationException($"{name} music entry {i.ToString()} uses the None cue.");
                }

                ValidateEnumIndex(entry.Cue, nameof(RaceMusicCue));

                if (entry.Clip == null)
                {
                    throw new InvalidOperationException($"{name} music cue '{entry.Cue}' has no AudioClip.");
                }

                var index = (int)entry.Cue;
                if (seen[index])
                {
                    throw new InvalidOperationException($"{name} contains duplicate music cue '{entry.Cue}'.");
                }

                seen[index] = true;
            }
        }

        private void ValidateSoundClips()
        {
            if (_soundClips == null || _soundClips.Length == 0)
            {
                throw new InvalidOperationException($"{name} requires at least one sound clip.");
            }

            var seen = new bool[GetEnumArrayLength<RaceSoundCue>()];
            for (var i = 0; i < _soundClips.Length; i++)
            {
                var entry = _soundClips[i];
                if (entry.Cue == RaceSoundCue.None)
                {
                    throw new InvalidOperationException($"{name} sound entry {i.ToString()} uses the None cue.");
                }

                ValidateEnumIndex(entry.Cue, nameof(RaceSoundCue));

                if (entry.Clip == null)
                {
                    throw new InvalidOperationException($"{name} sound cue '{entry.Cue}' has no AudioClip.");
                }

                var index = (int)entry.Cue;
                if (seen[index])
                {
                    throw new InvalidOperationException($"{name} contains duplicate sound cue '{entry.Cue}'.");
                }

                seen[index] = true;
            }
        }

        private bool HasMusicCue(RaceMusicCue cue)
        {
            if (_musicClips == null)
            {
                return false;
            }

            for (var i = 0; i < _musicClips.Length; i++)
            {
                if (_musicClips[i].Cue == cue && _musicClips[i].Clip != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetEnumArrayLength<TEnum>() where TEnum : Enum
        {
            var values = Enum.GetValues(typeof(TEnum));
            var max = 0;
            for (var i = 0; i < values.Length; i++)
            {
                var value = Convert.ToInt32(values.GetValue(i));
                if (value > max)
                {
                    max = value;
                }
            }

            return max + 1;
        }

        private static void ValidateEnumIndex<TEnum>(TEnum cue, string enumName) where TEnum : Enum
        {
            var index = Convert.ToInt32(cue);
            var max = GetEnumArrayLength<TEnum>() - 1;
            if (index < 0 || index > max)
            {
                throw new InvalidOperationException($"{enumName} cue value '{cue}' is outside the supported range.");
            }
        }

        [Serializable]
        private struct RaceMusicClipReference
        {
            [SerializeField] private RaceMusicCue _cue;
            [SerializeField] private AudioClip _clip;

            public RaceMusicCue Cue => _cue;

            public AudioClip Clip => _clip;
        }

        [Serializable]
        private struct RaceSoundClipReference
        {
            [SerializeField] private RaceSoundCue _cue;
            [SerializeField] private AudioClip _clip;

            public RaceSoundCue Cue => _cue;

            public AudioClip Clip => _clip;
        }
    }
}
