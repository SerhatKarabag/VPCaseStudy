using System;
using ThreadRace.Core.Audio;
using UnityEngine;

namespace ThreadRace.Infrastructure.Audio
{
    public sealed class RaceAudioLibrary
    {
        private readonly AudioClip[] _musicClips;
        private readonly AudioClip[] _soundClips;

        public RaceAudioLibrary(
            AudioClip[] musicClips,
            AudioClip[] soundClips,
            float crossfadeDurationSeconds,
            float defaultMusicVolume,
            float defaultSfxVolume,
            RaceMusicCue startupMusicCue)
        {
            _musicClips = musicClips ?? throw new ArgumentNullException(nameof(musicClips));
            _soundClips = soundClips ?? throw new ArgumentNullException(nameof(soundClips));
            CrossfadeDurationSeconds = Math.Max(0f, crossfadeDurationSeconds);
            DefaultMusicVolume = Clamp01(defaultMusicVolume);
            DefaultSfxVolume = Clamp01(defaultSfxVolume);
            StartupMusicCue = startupMusicCue;
        }

        public float CrossfadeDurationSeconds { get; }

        public float DefaultMusicVolume { get; }

        public float DefaultSfxVolume { get; }

        public RaceMusicCue StartupMusicCue { get; }

        public AudioClip GetMusicClip(RaceMusicCue cue)
        {
            var index = (int)cue;
            return index >= 0 && index < _musicClips.Length ? _musicClips[index] : null;
        }

        public AudioClip GetSoundClip(RaceSoundCue cue)
        {
            var index = (int)cue;
            return index >= 0 && index < _soundClips.Length ? _soundClips[index] : null;
        }

        public void PreloadAudioData()
        {
            PreloadAudioData(_musicClips);
            PreloadAudioData(_soundClips);
        }

        public static void PreloadAudioData(AudioClip clip)
        {
            if (clip == null || clip.loadState != AudioDataLoadState.Unloaded)
            {
                return;
            }

            clip.LoadAudioData();
        }

        private static void PreloadAudioData(AudioClip[] clips)
        {
            for (var i = 0; i < clips.Length; i++)
            {
                PreloadAudioData(clips[i]);
            }
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value))
            {
                return 0f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
