using System.Diagnostics;
using LojinhaRPG.Core.Services;

namespace LojinhaRPG.App.Audio;

/// <summary>
/// Player usado apenas em desenvolvimento/testes fora do Windows (ex: este container Linux).
/// Tenta tocar via ffplay/paplay/aplay se disponíveis; caso contrário apenas registra a chamada,
/// para permitir validar o fluxo (qual arquivo seria tocado, quando) sem travar nem lançar exceção.
/// No executável final para Windows, o WindowsAudioPlayer (NAudio) é usado no lugar deste.
/// </summary>
public sealed class DevFallbackAudioPlayer : IAudioPlayer
{
    private Process? _current;
    public string? LastPlayedFile { get; private set; }
    public int PlayCount { get; private set; }

    private static readonly string[] Candidates = { "ffplay", "paplay", "aplay" };

    public void Play(string absoluteFilePath)
    {
        LastPlayedFile = absoluteFilePath;
        PlayCount++;

        if (!File.Exists(absoluteFilePath)) return;

        var player = Candidates.FirstOrDefault(IsOnPath);
        if (player is null) return; // ambiente sem áudio: só registra, não falha

        try
        {
            Stop();
            var args = player == "ffplay" ? $"-nodisp -autoexit -loglevel quiet \"{absoluteFilePath}\"" : $"\"{absoluteFilePath}\"";
            _current = Process.Start(new ProcessStartInfo(player, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
        }
        catch
        {
            // ambiente de teste sem dispositivo de áudio real: ignora
        }
    }

    public void Stop()
    {
        try { if (_current is { HasExited: false }) _current.Kill(); } catch { /* ignore */ }
        _current = null;
    }

    private static bool IsOnPath(string exe)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        return pathVar.Split(Path.PathSeparator).Any(dir => File.Exists(Path.Combine(dir, exe)));
    }
}
