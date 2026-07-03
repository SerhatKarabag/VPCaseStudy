using System;
using ThreadRace.Core.Audio;
using ThreadRace.Core.Time;
using UnityEngine;

namespace ThreadRace.Infrastructure.Audio
{
    public sealed class RaceAudioService : IRaceAudioService, IDisposable
    {
        private const string RootName = "ThreadRaceAudioService";
        private const string MusicVolumeKey = "ThreadRace.Audio.MusicVolume";
        private const string SfxVolumeKey = "ThreadRace.Audio.SfxVolume";
        private const string MusicMutedKey = "ThreadRace.Audio.MusicMuted";
        private const string SfxMutedKey = "ThreadRace.Audio.SfxMuted";

        private readonly RaceAudioLibrary _library;
        private readonly IRaceTimeProvider _timeProvider;
        private readonly AudioSource[] _musicSources = new AudioSource[2];

        private GameObject _root;
        private AudioSource _sfxSource;
        private int _activeMusicIndex = -1;
        private FadeMode _fadeMode;
        private AudioSource _fadeOutSource;
        private AudioSource _fadeInSource;
        private float _fadeElapsedSeconds;
        private float _fadeDurationSeconds;
        private float _fadeOutStartVolume;
        private float _musicVolume;
        private float _sfxVolume;
        private bool _isMusicMuted;
        private bool _isSfxMuted;

        public RaceAudioService(RaceAudioLibrary library, IRaceTimeProvider timeProvider)
        {
            _library = library ?? throw new ArgumentNullException(nameof(library));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public RaceMusicCue CurrentMusicCue { get; private set; } = RaceMusicCue.None;

        public bool IsMusicPlaying
        {
            get
            {
                var active = ActiveMusicSource;
                return active != null && active.isPlaying;
            }
        }

        private AudioSource ActiveMusicSource =>
            _activeMusicIndex >= 0 && _activeMusicIndex < _musicSources.Length ? _musicSources[_activeMusicIndex] : null;

        public void Initialize()
        {
            LoadSettings();
            CreateAudioSources();
            _library.PreloadAudioData();

            if (_library.StartupMusicCue != RaceMusicCue.None)
            {
                PlayMusic(_library.StartupMusicCue, false);
            }
        }

        public void Tick()
        {
            if (_fadeMode == FadeMode.None)
            {
                return;
            }

            var deltaTime = Math.Max(0f, _timeProvider.UnscaledDeltaTime);
            _fadeElapsedSeconds += deltaTime;
            var progress = _fadeDurationSeconds <= 0f ? 1f : Clamp01(_fadeElapsedSeconds / _fadeDurationSeconds);

            if (_fadeOutSource != null)
            {
                _fadeOutSource.volume = Mathf.Lerp(_fadeOutStartVolume, 0f, progress);
            }

            if (_fadeMode == FadeMode.Crossfade && _fadeInSource != null)
            {
                _fadeInSource.volume = Mathf.Lerp(0f, EffectiveMusicVolume, progress);
            }

            if (progress < 1f)
            {
                return;
            }

            CompleteFade();
        }

        public void Dispose()
        {
            _fadeMode = FadeMode.None;
            _fadeOutSource = null;
            _fadeInSource = null;

            if (_root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }

            _root = null;
        }

        public void PlayMusic(RaceMusicCue cue, bool crossfade = true)
        {
            EnsureAudioSources();

            if (cue == RaceMusicCue.None)
            {
                StopMusic(crossfade);
                return;
            }

            var clip = _library.GetMusicClip(cue);
            if (clip == null)
            {
                Debug.LogWarning($"[RaceAudioService] Missing music clip for cue '{cue}'.");
                return;
            }

            RaceAudioLibrary.PreloadAudioData(clip);
            CurrentMusicCue = cue;

            if (_isMusicMuted)
            {
                StopAllMusicSources();
                return;
            }

            var active = ActiveMusicSource;
            if (active != null && active.isPlaying && active.clip == clip)
            {
                active.volume = EffectiveMusicVolume;
                return;
            }

            if (!crossfade || active == null || !active.isPlaying || _library.CrossfadeDurationSeconds <= 0f)
            {
                PlayMusicImmediately(clip);
                return;
            }

            StartCrossfade(active, clip);
        }

        public void StopMusic(bool fadeOut = true)
        {
            EnsureAudioSources();
            CurrentMusicCue = RaceMusicCue.None;

            var active = ActiveMusicSource;
            if (active == null || !active.isPlaying)
            {
                StopAllMusicSources();
                return;
            }

            if (!fadeOut || _library.CrossfadeDurationSeconds <= 0f)
            {
                StopAllMusicSources();
                return;
            }

            _fadeMode = FadeMode.FadeOut;
            _fadeOutSource = active;
            _fadeInSource = null;
            _fadeOutStartVolume = active.volume;
            _fadeElapsedSeconds = 0f;
            _fadeDurationSeconds = _library.CrossfadeDurationSeconds;
        }

        public void PauseMusic()
        {
            for (var i = 0; i < _musicSources.Length; i++)
            {
                if (_musicSources[i] != null)
                {
                    _musicSources[i].Pause();
                }
            }
        }

        public void ResumeMusic()
        {
            if (_isMusicMuted)
            {
                return;
            }

            for (var i = 0; i < _musicSources.Length; i++)
            {
                if (_musicSources[i] != null)
                {
                    _musicSources[i].UnPause();
                }
            }
        }

        public void PlaySound(RaceSoundCue cue, float volumeScale = 1f)
        {
            EnsureAudioSources();

            if (cue == RaceSoundCue.None || _isSfxMuted)
            {
                return;
            }

            var clip = _library.GetSoundClip(cue);
            if (clip == null)
            {
                Debug.LogWarning($"[RaceAudioService] Missing SFX clip for cue '{cue}'.");
                return;
            }

            RaceAudioLibrary.PreloadAudioData(clip);
            _sfxSource.PlayOneShot(clip, EffectiveSfxVolume * Math.Max(0f, volumeScale));
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume);
            PlayerPrefs.Save();
            ApplyMusicVolume();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
            PlayerPrefs.Save();
            ApplySfxVolume();
        }

        public float GetMusicVolume()
        {
            return _musicVolume;
        }

        public float GetSfxVolume()
        {
            return _sfxVolume;
        }

        public void ToggleMusicMute()
        {
            SetMusicMuted(!_isMusicMuted);
        }

        public void ToggleSfxMute()
        {
            SetSfxMuted(!_isSfxMuted);
        }

        public void SetMusicMuted(bool muted)
        {
            if (_isMusicMuted == muted)
            {
                return;
            }

            _isMusicMuted = muted;
            PlayerPrefs.SetInt(MusicMutedKey, _isMusicMuted ? 1 : 0);
            PlayerPrefs.Save();

            if (_isMusicMuted)
            {
                StopAllMusicSources();
                return;
            }

            if (CurrentMusicCue != RaceMusicCue.None)
            {
                PlayMusic(CurrentMusicCue, false);
            }
        }

        public void SetSfxMuted(bool muted)
        {
            _isSfxMuted = muted;
            PlayerPrefs.SetInt(SfxMutedKey, _isSfxMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplySfxVolume();
        }

        public bool IsMusicMuted()
        {
            return _isMusicMuted;
        }

        public bool IsSfxMuted()
        {
            return _isSfxMuted;
        }

        private float EffectiveMusicVolume => _isMusicMuted ? 0f : _musicVolume;

        private float EffectiveSfxVolume => _isSfxMuted ? 0f : _sfxVolume;

        private void CreateAudioSources()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject(RootName);
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _musicSources[0] = CreateAudioSource("MusicA", true);
            _musicSources[1] = CreateAudioSource("MusicB", true);
            _sfxSource = CreateAudioSource("Sfx", false);
            ApplyMusicVolume();
            ApplySfxVolume();
        }

        private AudioSource CreateAudioSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(_root.transform, false);

            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = loop ? EffectiveMusicVolume : EffectiveSfxVolume;
            return source;
        }

        private void EnsureAudioSources()
        {
            if (_root == null)
            {
                CreateAudioSources();
            }
        }

        private void PlayMusicImmediately(AudioClip clip)
        {
            StopAllMusicSources();
            _activeMusicIndex = 0;

            var source = _musicSources[_activeMusicIndex];
            source.clip = clip;
            source.volume = EffectiveMusicVolume;
            source.Play();
        }

        private void StartCrossfade(AudioSource active, AudioClip nextClip)
        {
            var nextIndex = _activeMusicIndex == 0 ? 1 : 0;
            var next = _musicSources[nextIndex];
            next.Stop();
            next.clip = nextClip;
            next.volume = 0f;
            next.Play();

            _activeMusicIndex = nextIndex;
            _fadeMode = FadeMode.Crossfade;
            _fadeOutSource = active;
            _fadeInSource = next;
            _fadeOutStartVolume = active.volume;
            _fadeElapsedSeconds = 0f;
            _fadeDurationSeconds = _library.CrossfadeDurationSeconds;
        }

        private void CompleteFade()
        {
            if (_fadeOutSource != null)
            {
                _fadeOutSource.Stop();
                _fadeOutSource.volume = 0f;
            }

            if (_fadeMode == FadeMode.Crossfade && _fadeInSource != null)
            {
                _fadeInSource.volume = EffectiveMusicVolume;
            }

            if (_fadeMode == FadeMode.FadeOut)
            {
                _activeMusicIndex = -1;
            }

            _fadeMode = FadeMode.None;
            _fadeOutSource = null;
            _fadeInSource = null;
            _fadeElapsedSeconds = 0f;
            _fadeDurationSeconds = 0f;
            _fadeOutStartVolume = 0f;
        }

        private void StopAllMusicSources()
        {
            _fadeMode = FadeMode.None;
            _fadeOutSource = null;
            _fadeInSource = null;

            for (var i = 0; i < _musicSources.Length; i++)
            {
                var source = _musicSources[i];
                if (source == null)
                {
                    continue;
                }

                source.Stop();
                source.volume = 0f;
            }

            _activeMusicIndex = -1;
        }

        private void ApplyMusicVolume()
        {
            if (_fadeMode == FadeMode.Crossfade)
            {
                return;
            }

            var active = ActiveMusicSource;
            if (active != null)
            {
                active.volume = EffectiveMusicVolume;
            }
        }

        private void ApplySfxVolume()
        {
            if (_sfxSource != null)
            {
                _sfxSource.volume = EffectiveSfxVolume;
            }
        }

        private void LoadSettings()
        {
            _musicVolume = Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, _library.DefaultMusicVolume));
            _sfxVolume = Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, _library.DefaultSfxVolume));
            _isMusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
            _isSfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
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

        private enum FadeMode
        {
            None,
            Crossfade,
            FadeOut
        }
    }
}
