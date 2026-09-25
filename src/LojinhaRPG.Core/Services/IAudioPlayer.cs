namespace LojinhaRPG.Core.Services;

/// <summary>Toca um arquivo de áudio (wav/mp3) de forma assíncrona e não bloqueante.</summary>
public interface IAudioPlayer
{
    void Play(string absoluteFilePath);
    void Stop();
}
