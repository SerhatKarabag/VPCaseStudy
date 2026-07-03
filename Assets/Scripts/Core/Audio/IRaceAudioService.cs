namespace ThreadRace.Core.Audio
{
    public interface IRaceMusicService
    {
        RaceMusicCue CurrentMusicCue { get; }

        bool IsMusicPlaying { get; }

        void PlayMusic(RaceMusicCue cue, bool crossfade = true);

        void StopMusic(bool fadeOut = true);

        void PauseMusic();

        void ResumeMusic();
    }

    public interface IRaceSfxService
    {
        void PlaySound(RaceSoundCue cue, float volumeScale = 1f);
    }

    public interface IRaceAudioVolumeService
    {
        void SetMusicVolume(float volume);

        void SetSfxVolume(float volume);

        float GetMusicVolume();

        float GetSfxVolume();
    }

    public interface IRaceAudioMuteService
    {
        void ToggleMusicMute();

        void ToggleSfxMute();

        void SetMusicMuted(bool muted);

        void SetSfxMuted(bool muted);

        bool IsMusicMuted();

        bool IsSfxMuted();
    }

    public interface IRaceAudioService :
        IRaceMusicService,
        IRaceSfxService,
        IRaceAudioVolumeService,
        IRaceAudioMuteService
    {
    }
}
