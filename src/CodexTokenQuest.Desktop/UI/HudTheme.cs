using Avalonia.Media;
namespace CodexTokenQuest.Desktop;

internal enum HudTheme
{
    PixelDungeon,
    ArcaneGlass,
    GuildLedger,
    CodeTerminal
}

internal sealed record HudPalette(
    Color Background,
    Color Panel,
    Color PanelBright,
    Color Ink,
    Color Cream,
    Color Gold,
    Color Cyan,
    Color Green,
    Color Amber,
    Color Red,
    Color Text,
    Color Muted,
    Color Grid);

internal static class HudColors
{
    private static readonly HudPalette[] Palettes =
    [
        new(Color.FromRgb(18, 16, 29), Color.FromRgb(32, 30, 49), Color.FromRgb(46, 42, 66), Color.FromRgb(15, 13, 24), Color.FromRgb(255, 238, 196), Color.FromRgb(255, 203, 79), Color.FromRgb(91, 224, 255), Color.FromRgb(92, 224, 112), Color.FromRgb(255, 162, 72), Color.FromRgb(235, 76, 83), Color.FromRgb(250, 244, 225), Color.FromRgb(167, 154, 174), Color.FromRgb(92, 76, 112)),
        new(Color.FromRgb(10, 10, 30), Color.FromRgb(25, 24, 62), Color.FromRgb(43, 39, 92), Color.FromRgb(7, 7, 24), Color.FromRgb(238, 237, 255), Color.FromRgb(199, 149, 255), Color.FromRgb(88, 205, 255), Color.FromRgb(98, 235, 195), Color.FromRgb(255, 176, 92), Color.FromRgb(255, 95, 137), Color.FromRgb(245, 243, 255), Color.FromRgb(168, 163, 205), Color.FromRgb(91, 75, 158)),
        new(Color.FromRgb(61, 42, 25), Color.FromRgb(224, 197, 143), Color.FromRgb(239, 218, 171), Color.FromRgb(55, 35, 20), Color.FromRgb(62, 40, 22), Color.FromRgb(139, 91, 38), Color.FromRgb(78, 96, 71), Color.FromRgb(75, 111, 55), Color.FromRgb(160, 91, 35), Color.FromRgb(151, 52, 39), Color.FromRgb(55, 35, 20), Color.FromRgb(112, 82, 52), Color.FromRgb(151, 111, 66)),
        new(Color.FromRgb(3, 10, 7), Color.FromRgb(7, 20, 13), Color.FromRgb(12, 35, 21), Color.FromRgb(1, 7, 4), Color.FromRgb(197, 255, 148), Color.FromRgb(255, 192, 53), Color.FromRgb(95, 255, 157), Color.FromRgb(144, 255, 65), Color.FromRgb(255, 187, 51), Color.FromRgb(255, 83, 83), Color.FromRgb(171, 255, 112), Color.FromRgb(91, 156, 80), Color.FromRgb(38, 91, 52))
    ];

    internal static HudTheme Theme { get; private set; }
    private static HudPalette Current => Palettes[(int)Theme];
    internal static Color Background => Current.Background;
    internal static Color Panel => Current.Panel;
    internal static Color PanelBright => Current.PanelBright;
    internal static Color Ink => Current.Ink;
    internal static Color Cream => Current.Cream;
    internal static Color Gold => Current.Gold;
    internal static Color Cyan => Current.Cyan;
    internal static Color Green => Current.Green;
    internal static Color Amber => Current.Amber;
    internal static Color Red => Current.Red;
    internal static Color Text => Current.Text;
    internal static Color Muted => Current.Muted;
    internal static Color Grid => Current.Grid;

    internal static void SetTheme(HudTheme theme) => Theme = theme;
}

internal static class HudCopy
{
    internal static string Brand => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("◆ CODEX TOKEN QUEST ◆", "◆ CODEX 代幣任務 ◆"),
        HudTheme.ArcaneGlass => L("✦ ARCANE TOKEN ORACLE ✦", "✦ 奧術代幣神諭 ✦"),
        HudTheme.GuildLedger => L("◆ ADVENTURERS' LEDGER ◆", "◆ 冒險者公會帳簿 ◆"),
        _ => L("> CODEX_USAGE_MONITOR", "> CODEX_用量監控器")
    };

    internal static (string Camp, string Quests, string History) Tabs => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => (L("CAMP", "營地"), L("QUESTS", "任務"), L("HISTORY", "歷史")),
        HudTheme.ArcaneGlass => (L("SANCTUM", "聖所"), L("RUNES", "符文"), L("ECHOES", "回響")),
        HudTheme.GuildLedger => (L("GUILD", "公會"), L("CONTRACTS", "契約"), L("LEDGER", "帳簿")),
        _ => (L("STATUS", "狀態"), L("LIMITS", "額度"), L("LOGS", "紀錄"))
    };

    internal static string QuestTitle => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("⚔ STAMINA DUNGEON // WEEKLY LIMITS", "⚔ 耐力地城 // 每週額度"),
        HudTheme.ArcaneGlass => L("✦ RESONANCE RUNES // ARCANE LIMITS", "✦ 共鳴符文 // 奧術額度"),
        HudTheme.GuildLedger => L("◆ ACTIVE CONTRACTS // WEEKLY ALLOWANCE", "◆ 進行中契約 // 每週額度"),
        _ => L("> QUOTA_WINDOWS --WEEKLY", "> 額度視窗 --每週")
    };

    internal static string Loading => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("READING QUEST LOG...", "讀取任務紀錄..."),
        HudTheme.ArcaneGlass => L("DIVINING TOKEN ECHOES...", "占卜代幣回響..."),
        HudTheme.GuildLedger => L("OPENING GUILD RECORDS...", "開啟公會紀錄..."),
        _ => L("READING_USAGE_STREAM...", "讀取_用量_資料流...")
    };

    internal static string Ready(DateTimeOffset time) => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L($"SAVE OK ◆ {time:HH:mm:ss}", $"儲存完成 ◆ {time:HH:mm:ss}"),
        HudTheme.ArcaneGlass => L($"ORACLE ATTUNED ✦ {time:HH:mm:ss}", $"神諭同步 ✦ {time:HH:mm:ss}"),
        HudTheme.GuildLedger => L($"LEDGER SEALED ◆ {time:HH:mm:ss}", $"帳簿封存 ◆ {time:HH:mm:ss}"),
        _ => L($"STATUS_OK :: {time:HH:mm:ss}", $"狀態_正常 :: {time:HH:mm:ss}")
    };

    internal static string Lost => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("QUEST LOG LOST ◆ RETRY", "任務紀錄遺失 ◆ 重試"),
        HudTheme.ArcaneGlass => L("DIVINATION BROKEN ✦ RETRY", "占卜中斷 ✦ 重試"),
        HudTheme.GuildLedger => L("LEDGER UNAVAILABLE ◆ RETRY", "帳簿無法使用 ◆ 重試"),
        _ => L("ERR_USAGE_STREAM :: RETRY", "錯誤_用量資料流 :: 重試")
    };

    internal static string Footer(int minutes) => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L($"AUTO-SAVE ◆ SYNC {minutes}M ◆ OPTIONS", $"自動儲存 ◆ 同步 {minutes}分 ◆ 選項"),
        HudTheme.ArcaneGlass => L($"AUTO-ATTUNE ✦ {minutes}M ✦ SETTINGS", $"自動調和 ✦ {minutes}分 ✦ 設定"),
        HudTheme.GuildLedger => L($"AUTO-RECORD ◆ {minutes}M ◆ OPTIONS", $"自動記錄 ◆ {minutes}分 ◆ 選項"),
        _ => L($"AUTO_SYNC={minutes}M :: CONFIG", $"自動_同步={minutes}分 :: 設定")
    };

    internal static string EmptyHistory => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("NO ECHOES DETECTED", "未偵測到回響"),
        HudTheme.GuildLedger => L("NO ENTRIES RECORDED", "尚無帳簿紀錄"),
        HudTheme.CodeTerminal => L("NO_LOG_DATA", "沒有_紀錄_資料"),
        _ => L("NO QUEST RECORD", "尚無任務紀錄")
    };

    internal static string StatusTitle => HudColors.Theme switch
    {
        HudTheme.PixelDungeon => L("ADVENTURER STATUS", "冒險者狀態"),
        HudTheme.ArcaneGlass => L("ARCANE SIGNATURE", "奧術印記"),
        HudTheme.GuildLedger => L("GUILD RECORD", "公會紀錄"),
        _ => L("AGENT STATUS", "代理狀態")
    };

    internal static string Stamina => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("MANA", "魔力"), HudTheme.GuildLedger => L("VIGOR", "活力"),
        HudTheme.CodeTerminal => L("QUOTA", "額度"), _ => L("STA", "耐力")
    };

    internal static string Experience => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("AURA", "靈氣"), HudTheme.GuildLedger => L("RENOWN", "聲望"),
        HudTheme.CodeTerminal => L("LOAD", "負載"), _ => L("EXP", "經驗")
    };

    internal static string Lifetime => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("CRYSTAL MEMORY", "水晶記憶"), HudTheme.GuildLedger => L("LIFETIME RENOWN", "累積聲望"),
        HudTheme.CodeTerminal => L("TOTAL TOKENS", "代幣總數"), _ => L("TOTAL EXP", "總經驗")
    };

    internal static string Today => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("TODAY'S SPARK", "今日星火"), HudTheme.GuildLedger => L("TODAY'S BOUNTY", "今日賞金"),
        HudTheme.CodeTerminal => L("SESSION TOKENS", "工作階段代幣"), _ => L("TODAY QUEST EXP", "今日任務經驗")
    };

    internal static string Reset => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("RECHARGE", "充能"), HudTheme.GuildLedger => L("RENEWAL", "更新"),
        HudTheme.CodeTerminal => L("RESET_AT", "重設時間"), _ => L("RESET", "重設")
    };

    internal static string ChartTitle => HudColors.Theme switch
    {
        HudTheme.ArcaneGlass => L("ECHOES // 7-DAY RESONANCE", "回響 // 7 日共鳴"),
        HudTheme.GuildLedger => L("LEDGER // 7-DAY BOUNTY", "帳簿 // 7 日賞金"),
        HudTheme.CodeTerminal => L("> TOKEN_LOG --LAST=7D", "> 代幣_紀錄 --最近=7日"),
        _ => L("QUEST LOG // 7-DAY EXP", "任務紀錄 // 7 日經驗")
    };

    internal static (string Name, string Class) Hero(RpgCharacter character)
    {
        var index = character.Name switch { "LYRA" => 1, "SYLVI" => 2, "NOVA" => 3, _ => 0 };
        return HudColors.Theme switch
        {
            HudTheme.ArcaneGlass => (
                $"✦ {new[] { "CAEL", "SELENE", "IRIS", "ORION" }[index]} ✦",
                L(
                    new[] { "CRYSTAL WARDEN", "ASTRAL ORACLE", "PRISM RANGER", "RUNE SENTINEL" }[index],
                    new[] { "水晶守衛", "星界神諭", "稜鏡遊俠", "符文哨兵" }[index])),
            HudTheme.GuildLedger => (
                new[] { "ROWAN", "ELSPETH", "BRIAR", "GARRICK" }[index],
                L(
                    new[] { "GUILD SWORD", "LEDGER ARCANIST", "CONTRACT RANGER", "GUILD MARSHAL" }[index],
                    new[] { "公會劍士", "帳簿秘法師", "契約遊俠", "公會元帥" }[index])),
            HudTheme.CodeTerminal => (
                $"[{new[] { "CIPHER", "SYNTAX", "PACKET", "KERNEL" }[index]}]",
                L(
                    $"ROLE::{new[] { "CODE_SENTINEL", "SYNTAX_WITCH", "PACKET_RANGER", "FIREWALL_KNIGHT" }[index]}",
                    $"角色::{new[] { "程式哨兵", "語法巫師", "封包遊俠", "防火牆騎士" }[index]}")),
            _ => ($"◀ {character.Name} ▶", L(character.ClassName, new[] { "劍盾守衛", "星界法師", "荒野遊俠", "符文騎士" }[index]))
        };
    }

    private static string L(string english, string traditionalChinese) => UiText.Pick(english, traditionalChinese);
}

internal sealed record RpgCharacter(string Name, string ClassName, int UnlockLevel, int Column, int Row);


internal static class Characters
{
    internal static readonly RpgCharacter[] All = [
        new("AERON", "SWORD WARDEN", 1, 0, 0), new("LYRA", "ASTRAL MAGE", 10, 1, 0),
        new("SYLVI", "WILD RANGER", 25, 0, 1), new("NOVA", "RUNE KNIGHT", 50, 1, 1)];
}
