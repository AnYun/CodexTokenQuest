using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodexTokenQuest.Core;

internal sealed record UsageSnapshot(
    DateTimeOffset FetchedAt,
    IReadOnlyList<RateLimitBucket> RateLimits,
    TokenSummary? Tokens,
    IReadOnlyList<DailyTokenUsage> DailyUsage,
    int? AvailableResetCredits,
    string? Warning);

internal sealed record RateLimitBucket(
    string Id,
    string? Name,
    string Window,
    decimal UsedPercent,
    int? WindowDurationMinutes,
    DateTimeOffset? ResetsAt,
    string? PlanType,
    string? ReachedType)
{
    [JsonIgnore]
    public decimal RemainingPercent => Math.Clamp(100m - UsedPercent, 0m, 100m);
}

internal sealed record TokenSummary(
    long? LifetimeTokens,
    long? PeakDailyTokens,
    long? LongestRunningTurnSeconds,
    int? CurrentStreakDays,
    int? LongestStreakDays);

internal sealed record DailyTokenUsage(DateOnly Date, long Tokens);

internal static class UsageSnapshotParser
{
    public static UsageSnapshot Parse(
        JsonElement rateLimitResult,
        JsonElement? usageResult,
        string? warning,
        DateTimeOffset fetchedAt)
    {
        var buckets = new List<RateLimitBucket>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        if (rateLimitResult.TryGetProperty("rateLimitsByLimitId", out var byId) && byId.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in byId.EnumerateObject())
            {
                AddWindows(property.Name, property.Value, buckets, seen);
            }
        }

        if (buckets.Count == 0 && rateLimitResult.TryGetProperty("rateLimits", out var legacy) && legacy.ValueKind == JsonValueKind.Object)
        {
            var id = ReadString(legacy, "limitId") ?? "codex";
            AddWindows(id, legacy, buckets, seen);
        }

        var resetCredits = rateLimitResult.TryGetProperty("rateLimitResetCredits", out var credits) &&
                           credits.ValueKind == JsonValueKind.Object
            ? ReadInt32(credits, "availableCount")
            : null;

        TokenSummary? tokenSummary = null;
        var dailyUsage = new List<DailyTokenUsage>();
        if (usageResult is { } usage)
        {
            if (usage.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.Object)
            {
                tokenSummary = new TokenSummary(
                    ReadInt64(summary, "lifetimeTokens"),
                    ReadInt64(summary, "peakDailyTokens"),
                    ReadInt64(summary, "longestRunningTurnSec"),
                    ReadInt32(summary, "currentStreakDays"),
                    ReadInt32(summary, "longestStreakDays"));
            }

            if (usage.TryGetProperty("dailyUsageBuckets", out var daily) && daily.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in daily.EnumerateArray())
                {
                    var dateText = ReadString(item, "startDate");
                    var tokens = ReadInt64(item, "tokens");
                    if (DateOnly.TryParse(dateText, out var date) && tokens is not null)
                    {
                        dailyUsage.Add(new DailyTokenUsage(date, tokens.Value));
                    }
                }
            }
        }

        return new UsageSnapshot(fetchedAt, buckets, tokenSummary, dailyUsage, resetCredits, warning);
    }

    private static void AddWindows(
        string fallbackId,
        JsonElement limit,
        ICollection<RateLimitBucket> target,
        ISet<string> seen)
    {
        var id = ReadString(limit, "limitId") ?? fallbackId;
        var name = ReadString(limit, "limitName");
        var planType = ReadString(limit, "planType");
        var reachedType = ReadString(limit, "rateLimitReachedType");

        AddWindow("primary", "主要", id, name, planType, reachedType, limit, target, seen);
        AddWindow("secondary", "次要", id, name, planType, reachedType, limit, target, seen);
    }

    private static void AddWindow(
        string propertyName,
        string label,
        string id,
        string? name,
        string? planType,
        string? reachedType,
        JsonElement limit,
        ICollection<RateLimitBucket> target,
        ISet<string> seen)
    {
        if (!limit.TryGetProperty(propertyName, out var window) || window.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var usedPercent = ReadDecimal(window, "usedPercent");
        if (usedPercent is null || !seen.Add($"{id}:{propertyName}"))
        {
            return;
        }

        var resetsAtUnix = ReadInt64(window, "resetsAt");
        DateTimeOffset? resetsAt = null;
        if (resetsAtUnix is >= 0)
        {
            try
            {
                resetsAt = DateTimeOffset.FromUnixTimeSeconds(resetsAtUnix.Value);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Keep malformed timestamps unavailable without discarding the bucket.
            }
        }

        target.Add(new RateLimitBucket(
            id,
            name,
            label,
            usedPercent.Value,
            ReadInt32(window, "windowDurationMins"),
            resetsAt,
            planType,
            reachedType));
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.String
            ? node.GetString()
            : null;

    private static long? ReadInt64(JsonElement element, string name) =>
        element.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.Number && node.TryGetInt64(out var value)
            ? value
            : null;

    private static int? ReadInt32(JsonElement element, string name) =>
        element.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.Number && node.TryGetInt32(out var value)
            ? value
            : null;

    private static decimal? ReadDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.Number && node.TryGetDecimal(out var value)
            ? value
            : null;
}
