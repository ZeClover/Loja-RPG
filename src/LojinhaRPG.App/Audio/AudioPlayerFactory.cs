using LojinhaRPG.Core.Services;

namespace LojinhaRPG.App.Audio;

public static class AudioPlayerFactory
{
    public static IAudioPlayer Create() => OperatingSystem.IsWindows()
        ? new WindowsAudioPlayer()
        : new DevFallbackAudioPlayer();
}
