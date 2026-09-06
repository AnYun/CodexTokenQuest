using System.Text.Json.Serialization;

namespace CodexTokenQuest.Core;

internal sealed record UsageSnapshot(
    DateTimeOffset FetchedAt,
    IReadOnlyList<RateLimitBucket> RateLimits,
    TokenSummary? Tokens,
    IReadOnlyList<DailyTokenUsage> DailyUsage,
    int? AvailableResetCredits,
    string? Warning)
{
    public IReadOnlyList<ResetCredit>? ResetCredits { get; init; }
    public bool UsageUnsupported { get; init; }
}

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

internal sealed record ResetCredit(string Id, DateTimeOffset? GrantedAt, DateTimeOffset? ExpiresAt, bool NeverExpires, string Status);
