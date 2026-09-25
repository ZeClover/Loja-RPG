using LojinhaRPG.Core.Services;
using NAudio.Wave;

namespace LojinhaRPG.App.Audio;

/// <summary>Toca wav/mp3 usando NAudio (WASAPI/WinMM). Só funciona no Windows.</summary>
public sealed class WindowsAudioPlayer : IAudioPlayer
{
    private IWavePlayer? _output;
    private AudioFileReader? _reader;
    private readonly object _lock = new();

    public void Play(string absoluteFilePath)
    {
        if (!File.Exists(absoluteFilePath)) return;

        lock (_lock)
        {
            StopInternal();

            _reader = new AudioFileReader(absoluteFilePath);
            _output = new WaveOutEvent();
            _output.Init(_reader);
            _output.PlaybackStopped += (_, _) =>
            {
                lock (_lock) StopInternal();
            };
            _output.Play();
        }
    }

    public void Stop()
    {
        lock (_lock) StopInternal();
    }

    private void StopInternal()
    {
        _output?.Stop();
        _output?.Dispose();
        _output = null;
        _reader?.Dispose();
        _reader = null;
    }
}
