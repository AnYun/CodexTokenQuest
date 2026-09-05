using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodexTokenQuest.Desktop;

internal sealed record DesktopSettings
{
    internal const int DefaultRefreshMinutes = 5;
    internal const int MinimumRefreshMinutes = 1;
    internal const int MaximumRefreshMinutes = 1440;
    internal const int DefaultHudScalePercent = 100;
    internal const double DefaultHudScaleFactor = 0.8;
    internal const int MinimumHudScalePercent = 50;
    internal const int MaximumHudScalePercent = 300;
    internal const int DefaultMargin = 16;
    internal const int MinimumMargin = 0;
    internal const int MaximumMargin = 100;
    internal const long DefaultExperienceBase = 1_000;
    internal const long MinimumExperienceBase = 1_000;
    internal const long MaximumExperienceBase = 1_000_000_000_000;
    internal const int DefaultOpacityPercent = 100;
    internal const int MinimumOpacityPercent = 20;
    internal const int MaximumOpacityPercent = 100;

    public int RefreshMinutes { get; init; } = DefaultRefreshMinutes;
    public int HudScalePercent { get; init; } = DefaultHudScalePercent;
    public int HudScaleVersion { get; init; } = 1;
    [JsonIgnore]
    internal double HudScale => DefaultHudScaleFactor * HudScalePercent / 100d;
    public int Margin { get; init; } = DefaultMargin;
    public long ExperienceBase { get; init; } = DefaultExperienceBase;
    public int OpacityPercent { get; init; } = DefaultOpacityPercent;
    public string Language { get; init; } = UiText.DefaultLanguage;
    public bool MinimizedMode { get; init; }
    public int CharacterIndex { get; init; }
    public int ThemeIndex { get; init; }
    public string SelectedPanel { get; init; } = "CAMP";

    private static readonly string SettingsDirectory = AppPaths.StateDirectory;
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    internal static DesktopSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return Deserialize(File.ReadAllText(SettingsPath));
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (JsonException) { }
        return new DesktopSettings();
    }

    internal static DesktopSettings Deserialize(string json)
    {
        using var document = JsonDocument.Parse(json);
        var settings = document.RootElement.Deserialize<DesktopSettings>()?.Normalize() ?? new DesktopSettings();
        // Before version 1, 80% was the size now called 100%. Preserve saved sizes
        // while expressing them against the new baseline; save the version with them.
        if (document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty(nameof(HudScalePercent), out _)
            && (!document.RootElement.TryGetProperty(nameof(HudScaleVersion), out _) || settings.HudScaleVersion < 1))
            settings = settings with { HudScalePercent = (int)Math.Round(settings.HudScalePercent / DefaultHudScaleFactor), HudScaleVersion = 1 };
        return settings.Normalize();
    }

    internal void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var temporary = SettingsPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(Normalize(), new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, SettingsPath, overwrite: true);
    }

    internal DesktopSettings Normalize() => this with
    {
        RefreshMinutes = Math.Clamp(RefreshMinutes, MinimumRefreshMinutes, MaximumRefreshMinutes),
        HudScalePercent = Math.Clamp(HudScalePercent, MinimumHudScalePercent, MaximumHudScalePercent),
        Margin = Math.Clamp(Margin, MinimumMargin, MaximumMargin),
        ExperienceBase = Math.Clamp(ExperienceBase, MinimumExperienceBase, MaximumExperienceBase),
        OpacityPercent = Math.Clamp(OpacityPercent, MinimumOpacityPercent, MaximumOpacityPercent),
        Language = UiText.NormalizeLanguage(Language),
        CharacterIndex = Math.Clamp(CharacterIndex, 0, Characters.All.Length - 1),
        ThemeIndex = Math.Clamp(ThemeIndex, 0, Enum.GetValues<HudTheme>().Length - 1),
        SelectedPanel = SelectedPanel is "CAMP" or "QUESTS" or "HISTORY" ? SelectedPanel : "CAMP"
    };
}
