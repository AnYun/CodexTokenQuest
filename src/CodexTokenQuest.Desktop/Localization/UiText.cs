using System.Globalization;

namespace CodexTokenQuest.Desktop;

internal static class UiText
{
    internal const string English = "en";
    internal const string TraditionalChinese = "zh-Hant";

    private static string _language = DefaultLanguage;

    internal static string DefaultLanguage =>
        CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? TraditionalChinese
            : English;

    internal static bool IsTraditionalChinese => _language == TraditionalChinese;

    internal static void SetLanguage(string? language) => _language = NormalizeLanguage(language);

    internal static string NormalizeLanguage(string? language) =>
        language?.StartsWith("zh", StringComparison.OrdinalIgnoreCase) == true
            ? TraditionalChinese
            : English;

    internal static string Pick(string english, string traditionalChinese) =>
        IsTraditionalChinese ? traditionalChinese : english;

    internal static string WindowTitle => Pick("Codex Token Quest", "Codex 代幣任務");
    internal static string GameOptions => Pick("◆ GAME OPTIONS ◆", "◆ 遊戲選項 ◆");
    internal static string RefreshInterval => Pick("AUTO-SAVE INTERVAL", "自動儲存間隔");
    internal static string RefreshDescription => Pick(
        "Refresh Codex usage every 1–1440 minutes",
        "每 1–1440 分鐘重新讀取 Codex 用量");
    internal static string QuickSlots => Pick("QUICK SLOTS", "快速選擇");
    internal static string HudSize => Pick("HUD SIZE", "介面大小");
    internal static string HudSizeDescription => Pick(
        "Drag to resize the interface (50%–300%)",
        "拖曳調整介面大小（50%–300%）");
    internal static string HudMargin => Pick("HUD MARGIN", "視窗邊距");
    internal static string ExperienceBase => Pick("EXP BASE", "經驗值基數");
    internal static string ExperienceBaseDescription => Pick(
        "Higher values slow down level progression",
        "數值越高，等級提升速度越慢");
    internal static string Tokens => Pick("TOKENS", "TOKEN");
    internal static string HudOpacity => Pick("HUD OPACITY", "介面透明度");
    internal static string HudOpacityDescription => Pick(
        "Adjust HUD visibility (20%–100%)",
        "調整介面可見度（20%–100%）");
    internal static string MinimizeHud => Pick("Compact view", "精簡模式");
    internal static string RestoreHud => Pick("Restore full view", "還原完整模式");
    internal static string Cancel => Pick("CANCEL", "取消");
    internal static string Save => Pick("SAVE", "儲存");
    internal static string LanguageButton(string language) =>
        NormalizeLanguage(language) == TraditionalChinese ? "正體中文" : "LANG EN";

    internal static string TrayToggle => Pick("Show / Hide", "顯示 / 隱藏");
    internal static string TrayCamp => Pick("Camp", "營地");
    internal static string TrayQuests => Pick("Quest limits", "任務額度");
    internal static string TrayHistory => Pick("History", "歷史紀錄");
    internal static string TrayTheme => Pick("Change theme", "切換介面主題");
    internal static string TrayRefresh => Pick("Refresh usage", "重新讀取冒險紀錄");
    internal static string TrayOptions => Pick("Game options", "遊戲選項");
    internal static string TrayExit => Pick("Exit", "離開遊戲");
    internal static string ReadFailed => Pick("Usage read failed", "冒險紀錄讀取失敗");
    internal static string TrayReadFailed => Pick("Codex Token Quest: read failed", "Codex 代幣任務：讀取失敗");
    internal static string Level => Pick("LV.", "等級");
    internal static string Stamina => Pick("STA", "耐力");

    internal static string Next => Pick("NEXT", "下次");
    internal static string Unknown => Pick("UNKNOWN", "未知");
    internal static string Syncing => Pick("SYNCING", "同步中");

    internal static string WindowLabel(string value) => value.ToUpperInvariant() switch
    {
        "PRIMARY" => Pick("PRIMARY", "主要"),
        "SECONDARY" => Pick("SECONDARY", "次要"),
        _ => value
    };
}
