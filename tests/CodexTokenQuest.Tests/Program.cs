using System.Reflection;
using System.Text.Json;

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

Assert(rateLimits.Count == 2, "應解析 primary 與 secondary 視窗");
Assert(dailyUsage.Count == 1, "應解析每日 Token 用量");
Assert(credits == 1, "應解析可用重置次數");

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
Assert(parsedLocalTokens && (long)arguments[2]! == 24680L, "應解析本機今日工作階段的增量 Token");

Console.WriteLine("All usage parser tests passed.");
return 0;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
