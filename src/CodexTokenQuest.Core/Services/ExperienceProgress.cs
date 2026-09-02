namespace CodexTokenQuest.Core;

internal static class ExperienceProgress
{
    internal const int MaximumLevel = 99;

    internal static int GetLevel(long tokens, long experienceBase)
    {
        if (tokens <= 0)
        {
            return 1;
        }

        var normalizedBase = Math.Max(1, experienceBase);
        return Math.Clamp(1 + (int)Math.Floor(10d * Math.Log10(tokens / (double)normalizedBase + 1d)), 1, MaximumLevel);
    }

    internal static long GetThreshold(int level, long experienceBase)
    {
        if (level <= 1)
        {
            return 0;
        }

        var normalizedBase = Math.Max(1, experienceBase);
        var threshold = normalizedBase * (Math.Pow(10d, (level - 1) / 10d) - 1d);
        return threshold >= long.MaxValue ? long.MaxValue : (long)Math.Round(threshold);
    }

    internal static decimal GetLevelProgress(long tokens, int level, long experienceBase)
    {
        if (level >= MaximumLevel)
        {
            return 100m;
        }

        var current = GetThreshold(level, experienceBase);
        var next = GetThreshold(level + 1, experienceBase);
        return next <= current
            ? 100m
            : Math.Clamp((decimal)(tokens - current) / (next - current) * 100m, 0m, 100m);
    }
}
