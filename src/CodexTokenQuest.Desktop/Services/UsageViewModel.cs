using System.ComponentModel;

namespace CodexTokenQuest.Desktop;

internal sealed class UsageViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    private CodexUsageReader? _client;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Func<CancellationToken, Task<UsageSnapshot>> _read;
    private readonly Func<DateOnly, long?> _readLocal;
    private Task? _pending;
    internal UsageSnapshot? Snapshot { get; private set; }
    internal bool Refreshing { get; private set; }
    internal string? Error { get; private set; }
    internal string? Notice => Error is not null
        ? UiText.Pick("Read failed. Retry; last data retained.", "讀取失敗，請重試。保留上次資料。")
        : Snapshot?.UsageUnsupported == true
            ? UiText.Pick("This Codex version does not provide total usage.", "此 Codex 版本未提供累計用量。")
            : Snapshot?.Warning is not null
                ? UiText.Pick("Some usage is unavailable. Retry later.", "部分用量暫時無法讀取，請稍後重試。") : null;
    internal DateTimeOffset Now { get; private set; } = DateTimeOffset.Now;
    internal DateTimeOffset? LastFetched => Snapshot?.FetchedAt;
    internal long? TodayTokens { get; private set; }
    internal long? LifetimeTokens => Snapshot?.Tokens?.LifetimeTokens;
    internal IReadOnlyList<DailyTokenUsage> History { get; private set; } = [];
    internal RateLimitBucket? Weekly => Snapshot?.RateLimits
        .OrderBy(b => b.WindowDurationMinutes >= 10080 ? 0 : 1)
        .ThenBy(b => b.Id.Equals("codex", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenByDescending(b => b.WindowDurationMinutes ?? 0).FirstOrDefault();
    internal decimal? Stamina => Weekly?.RemainingPercent;
    internal int Level(long experienceBase) => ExperienceProgress.GetLevel(LifetimeTokens ?? 0, experienceBase);
    internal decimal Progress(long experienceBase) => ExperienceProgress.GetLevelProgress(LifetimeTokens ?? 0, Level(experienceBase), experienceBase);
    public event PropertyChangedEventHandler? PropertyChanged;
    internal event Action? Changed;

    internal UsageViewModel(Func<CancellationToken, Task<UsageSnapshot>>? read = null, Func<DateOnly, long?>? readLocal = null)
    {
        _read = read ?? ReadAsync;
        _readLocal = readLocal ?? (date => LocalTokenUsageReader.ReadForDate(date));
    }
    private async Task<UsageSnapshot> ReadAsync(CancellationToken token)
    {
        _client ??= new CodexUsageReader();
        return await _client.ReadSnapshotAsync(token);
    }
    internal Task RefreshAsync()
    {
        if (Refreshing || _shutdown.IsCancellationRequested) return _pending ?? Task.CompletedTask;
        return _pending = RefreshCoreAsync();
    }
    private async Task RefreshCoreAsync()
    {
        Refreshing = true; Notify();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var snapshot = await _read(timeout.Token);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var local = await Task.Run(() => _readLocal(today), timeout.Token);
            var api = snapshot.DailyUsage.FirstOrDefault(item => item.Date == today)?.Tokens;
            TodayTokens = api is not null && local is not null ? Math.Max(api.Value, local.Value) : api ?? local;
            Snapshot = snapshot;
            History = Enumerable.Range(0, 7).Select(i => today.AddDays(i - 6))
                .Select(date => new DailyTokenUsage(date, date == today ? TodayTokens ?? 0
                    : snapshot.DailyUsage.FirstOrDefault(item => item.Date == date)?.Tokens ?? 0)).ToArray();
            Error = null;
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Error = exception is OperationCanceledException
                ? UiText.Pick("Usage request timed out. Retry.", "用量請求逾時，請重試。") : exception.Message;
            if (_client is not null) { await _client.DisposeAsync(); _client = null; }
        }
        finally { Refreshing = false; Notify(); }
    }
    internal void Tick(DateTimeOffset now) { Now = now; Notify(); }
    private void Notify() { PropertyChanged?.Invoke(this, new(null)); Changed?.Invoke(); }
    internal string ResetText(DateTimeOffset? reset, bool next = true)
    {
        var label = next ? $"◆ {UiText.Next} {HudCopy.Reset}" : HudCopy.Reset;
        if (reset is null) return $"{label} // {UiText.Unknown}";
        var remaining = reset.Value - Now;
        var duration = remaining <= TimeSpan.Zero ? UiText.Syncing : FormatDuration(remaining);
        return $"{label} // {reset.Value.ToLocalTime():MM/dd HH:mm} // {duration}";
    }
    private static string FormatDuration(TimeSpan duration) => UiText.IsTraditionalChinese
        ? duration.TotalDays >= 1 ? $"T-{(int)duration.TotalDays}日 {duration.Hours:00}時"
            : duration.TotalHours >= 1 ? $"T-{(int)duration.TotalHours:00}時 {duration.Minutes:00}分"
            : $"T-{duration.Minutes:00}分 {duration.Seconds:00}秒"
        : duration.TotalDays >= 1 ? $"T-{(int)duration.TotalDays}D {duration.Hours:00}H"
            : duration.TotalHours >= 1 ? $"T-{(int)duration.TotalHours:00}H {duration.Minutes:00}M"
            : $"T-{duration.Minutes:00}M {duration.Seconds:00}S";
    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        if (_pending is not null) await _pending;
        if (_client is not null) await _client.DisposeAsync();
        _shutdown.Dispose();
    }
}
