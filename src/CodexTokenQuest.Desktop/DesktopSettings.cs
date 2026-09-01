using System.Text.Json;

namespace CodexTokenQuest.Desktop;

internal sealed record DesktopSettings
{
    internal const int DefaultRefreshMinutes = 5;
    internal const int MinimumRefreshMinutes = 1;
    internal const int MaximumRefreshMinutes = 1440;

    public int RefreshMinutes { get; init; } = DefaultRefreshMinutes;
    public bool CompactMode { get; init; } = true;
    public int CharacterIndex { get; init; }

    private static readonly string SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexTokenQuest");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    internal static DesktopSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<DesktopSettings>(File.ReadAllText(SettingsPath));
                if (settings is not null) return settings.Normalize();
            }
        }
        catch (IOException) { }
        catch (JsonException) { }
        return new DesktopSettings();
    }

    internal void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Normalize(), new JsonSerializerOptions { WriteIndented = true }));
    }

    internal DesktopSettings Normalize() => this with
    {
        RefreshMinutes = Math.Clamp(RefreshMinutes, MinimumRefreshMinutes, MaximumRefreshMinutes),
        CharacterIndex = Math.Clamp(CharacterIndex, 0, RpgHeroPanel.Characters.Count - 1)
    };
}
