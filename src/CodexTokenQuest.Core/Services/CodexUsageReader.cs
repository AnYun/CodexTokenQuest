namespace CodexTokenQuest.Core;

internal sealed record CodexServerSource(string Name, Func<CancellationToken, Task<CodexAppServerClient>> Start);

// Both desktops use the same capability fallback. Try PATH first, then known
// installations when that server cannot supply the optional account usage API.
internal sealed class CodexUsageReader : IAsyncDisposable
{
    private readonly IReadOnlyList<CodexServerSource> _sources;
    private readonly Action<string> _log;
    private CodexAppServerClient? _active;
    private bool _checkedAlternatives;

    internal CodexUsageReader(IReadOnlyList<CodexServerSource>? sources = null, Action<string>? log = null)
    {
        _sources = sources ?? CodexExecutableResolver.ResolveCandidates()
            .Select(path => new CodexServerSource(path, token =>
                CodexAppServerClient.StartAsync(token, CodexExecutableResolver.CreateStartInfo(path)))).ToArray();
        if (_sources.Count == 0)
            _sources = [new("PATH", token => CodexAppServerClient.StartAsync(token))];
        _log = log ?? AppPaths.Log;
    }

    internal async Task<UsageSnapshot> ReadSnapshotAsync(CancellationToken token)
    {
        if (_active is null)
        {
            _log($"Reading usage with Codex CLI: {_sources[0].Name}");
            _active = await _sources[0].Start(token);
        }
        var current = await _active.ReadSnapshotAsync(token);
        if (!current.UsageUnsupported || _checkedAlternatives) return current;

        var retryAlternatives = false;
        for (var i = 1; i < _sources.Count; i++)
        {
            CodexAppServerClient? candidate = null;
            try
            {
                _log($"Checking account usage support: {_sources[i].Name}");
                candidate = await _sources[i].Start(token);
                var snapshot = await candidate.ReadSnapshotAsync(token);
                if (snapshot.UsageUnsupported) continue;
                if (snapshot.Warning is not null) { retryAlternatives = true; continue; }
                // Switch the entire source so quota and totals remain from the same
                // server/account. All processes inherit the same CODEX_HOME.
                await _active.DisposeAsync();
                _active = candidate; candidate = null;
                _checkedAlternatives = true;
                _log($"Selected Codex CLI with account usage support: {_sources[i].Name}");
                return snapshot;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { retryAlternatives = true; _log($"Alternative Codex CLI unavailable: {e.Message}"); }
            finally { if (candidate is not null) await candidate.DisposeAsync(); }
        }
        _checkedAlternatives = !retryAlternatives;
        return current;
    }

    public async ValueTask DisposeAsync()
    {
        if (_active is not null) await _active.DisposeAsync();
    }
}
