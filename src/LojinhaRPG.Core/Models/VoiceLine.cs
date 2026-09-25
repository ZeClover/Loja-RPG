namespace LojinhaRPG.Core.Models;

/// <summary>
/// Uma fala opcional (chegada, venda ou saída): legenda de texto e/ou arquivo de áudio.
/// </summary>
public class VoiceLine
{
    public string Caption { get; set; } = string.Empty;

    /// <summary>Caminho relativo dentro da pasta "media" do set, ex: "sale.mp3". Vazio = sem áudio.</summary>
    public string AudioFile { get; set; } = string.Empty;

    public bool HasCaption => !string.IsNullOrWhiteSpace(Caption);
    public bool HasAudio => !string.IsNullOrWhiteSpace(AudioFile);
    public bool HasContent => HasCaption || HasAudio;
}
