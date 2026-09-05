using System.Reflection;
using System.Text.Json;

if (args.FirstOrDefault() == "--read-live")
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await using var reader = new CodexTokenQuest.Core.CodexUsageReader();
    var liveSnapshot = await reader.ReadSnapshotAsync(timeout.Token);
    Console.WriteLine(JsonSerializer.Serialize(new { liveSnapshot.Tokens?.LifetimeTokens, QuotaWindows = liveSnapshot.RateLimits.Count,
        DailyBuckets = liveSnapshot.DailyUsage.Count, liveSnapshot.UsageUnsupported, HasWarning = liveSnapshot.Warning is not null }));
    return 0;
}

if (args.FirstOrDefault() == "--hold-lease")
{
    using var lease = CodexTokenQuest.Core.InstanceLease.TryAcquire(args[1]);
    Console.WriteLine(lease is null ? "BUSY" : "READY");
    Console.ReadLine(); return 0;
}
if (args.FirstOrDefault()?.StartsWith("--fake-codex") == true)
{
    var usageRequests = 0;
    while (Console.ReadLine() is { } line)
    {
        using var message = JsonDocument.Parse(line);
        var root = message.RootElement;
        if (!root.TryGetProperty("id", out var id)) continue;
        var method = root.GetProperty("method").GetString();
        if (args.Length > 1) File.AppendAllText(args[1], method + "\n");
        if (method != "initialize" && args[0] == "--fake-codex-auth")
            Console.WriteLine(JsonSerializer.Serialize(new { id = id.GetInt64(), error = new { code = -32000, message = "authentication required" } }));
        else if (method == "account/usage/read")
        {
            usageRequests++;
            if (args[0] == "--fake-codex-supported" || (args[0] == "--fake-codex-transient" && usageRequests > 1))
                Console.WriteLine(JsonSerializer.Serialize(new { id = id.GetInt64(), result = new { summary = new { lifetimeTokens = 1234567 } } }));
            else
            {
                var (code, error) = args[0] switch
                {
                    "--fake-codex-legacy" => (-32600, "Invalid request: unknown variant `account/usage/read`, expected one of " + string.Concat(Enumerable.Repeat("`thread/start`, `thread/read`, ", 200))),
                    "--fake-codex-transient" => (-32600, "Invalid request: usage cache temporarily unavailable"),
                    _ => (-32601, "Method not found")
                };
                Console.WriteLine(JsonSerializer.Serialize(new { id = id.GetInt64(), error = new { code, message = error } }));
            }
        }
        else
            Console.WriteLine(JsonSerializer.Serialize(new { id = id.GetInt64(), result = new { rateLimits = new { primary = new { usedPercent = 25, windowDurationMins = 300 } } } }));
    }
    return 0;
}

var assembly = Assembly.Load("CodexTokenQuest.Core");
var parser = assembly.GetType("CodexTokenQuest.Core.UsageSnapshotParser", throwOnError: true)!;
var parse = parser.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static)!;

using var limits = JsonDocument.Parse("""
{
  "rateLimits": {
    "limitId": "codex",
    "primary": { "usedPercent": 25, "windowDurationMins": 300, "resetsAt": 1788134400 },
    "secondary": { "usedPercent": 40.5, "windowDurationMins": 10080, "resetsAt": 1788739200 }
  },
  "rateLimitsByLimitId": {
    "codex": {
      "limitId": "codex",
      "limitName": "Codex",
      "primary": { "usedPercent": 25, "windowDurationMins": 300, "resetsAt": 1788134400 },
      "secondary": { "usedPercent": 40.5, "windowDurationMins": 10080, "resetsAt": 1788739200 }
    }
  },
  "rateLimitResetCredits": { "availableCount": 1 }
}
""");

using var usage = JsonDocument.Parse("""
{
  "summary": {
    "lifetimeTokens": 1234567,
    "peakDailyTokens": 45678,
    "longestRunningTurnSec": 540,
    "currentStreakDays": 8,
    "longestStreakDays": 14
  },
  "dailyUsageBuckets": [{ "startDate": "2026-08-30", "tokens": 12345 }]
}
""");

var snapshot = parse.Invoke(null, [limits.RootElement, usage.RootElement, null, DateTimeOffset.UtcNow])!;
var type = snapshot.GetType();
var rateLimits = (System.Collections.ICollection)type.GetProperty("RateLimits")!.GetValue(snapshot)!;
var dailyUsage = (System.Collections.ICollection)type.GetProperty("DailyUsage")!.GetValue(snapshot)!;
var credits = (int?)type.GetProperty("AvailableResetCredits")!.GetValue(snapshot);

Assert(rateLimits.Count == 2, "Primary and secondary windows should be parsed.");
Assert(dailyUsage.Count == 1, "Daily token usage should be parsed.");
Assert(credits == 1, "Available reset credits should be parsed.");

var experienceProgress = assembly.GetType("CodexTokenQuest.Core.ExperienceProgress", throwOnError: true)!;
var getLevel = experienceProgress.GetMethod("GetLevel", BindingFlags.NonPublic | BindingFlags.Static)!;
var getThreshold = experienceProgress.GetMethod("GetThreshold", BindingFlags.NonPublic | BindingFlags.Static)!;
var fastLevel = (int)getLevel.Invoke(null, [1_000_000_000_000L, 1_000L])!;
var slowLevel = (int)getLevel.Invoke(null, [1_000_000_000_000L, 1_000_000_000L])!;
var levelTenThreshold = (long)getThreshold.Invoke(null, [10, 1_000_000L])!;
Assert(fastLevel > slowLevel, "A higher experience base should slow level progression.");
Assert(levelTenThreshold > 0, "Experience thresholds should remain positive above level one.");

var localReader = assembly.GetType("CodexTokenQuest.Core.LocalTokenUsageReader", throwOnError: true)!;
var tryReadIncrement = localReader.GetMethod("TryReadIncrement", BindingFlags.NonPublic | BindingFlags.Static)!;
var localToday = DateOnly.FromDateTime(DateTime.Today);
var localTimestamp = DateTimeOffset.Now.ToString("O");
var tokenEvent = JsonSerializer.Serialize(new
{
    timestamp = localTimestamp,
    type = "event_msg",
    payload = new
    {
        type = "token_count",
        info = new { last_token_usage = new { total_tokens = 24680L } }
    }
});
var arguments = new object?[] { tokenEvent, localToday, 0L };
var parsedLocalTokens = (bool)tryReadIncrement.Invoke(null, arguments)!;
Assert(parsedLocalTokens && (long)arguments[2]! == 24680L, "Today's local session token increment should be parsed.");

CrossPlatformTests.Run(args.Contains("--render"));
Console.WriteLine("All Codex Token Quest tests passed.");
return 0;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
