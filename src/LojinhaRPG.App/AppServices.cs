using LojinhaRPG.App.Audio;
using LojinhaRPG.Core.Services;

namespace LojinhaRPG.App;

/// <summary>Ponto único de acesso aos serviços compartilhados (evita um container de DI para um app deste porte).</summary>
public static class AppServices
{
    public static SetLibraryService Sets { get; } = new();
    public static PresetLibraryService Presets { get; } = new();
    public static SetImportExportService SetImportExport { get; } = new(Sets);
    public static PresetImportExportService PresetImportExport { get; } = new(Presets);
    public static IAudioPlayer Audio { get; } = AudioPlayerFactory.Create();
}
