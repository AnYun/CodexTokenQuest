using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace CodexTokenQuest.Core;

internal sealed class CodexAppServerClient : IAsyncDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _input;
    private readonly StreamReader _output;
    private readonly ConcurrentQueue<string> _errors = new();
    private readonly Task _errorPump;
    private long _nextId;
    private string? _unsupportedUsageWarning;

    private CodexAppServerClient(Process process)
    {
        _process = process;
        _input = process.StandardInput;
        _output = process.StandardOutput;
        _errorPump = PumpErrorsAsync(process.StandardError);
    }

    public static async Task<CodexAppServerClient> StartAsync(CancellationToken cancellationToken, ProcessStartInfo? startInfo = null)
    {
        Process process;
        try
        {
            process = Process.Start(startInfo ?? CodexExecutableResolver.CreateStartInfo())
                ?? throw new CodexAppServerException("Unable to start codex app-server.");
        }
        catch (Exception exception) when (exception is not CodexAppServerException)
        {
            throw new CodexAppServerException("Codex CLI was not found. Install Codex and sign in before trying again.", exception);
        }

        var client = new CodexAppServerClient(process);
        try
        {
            await client.SendRequestAsync(
                "initialize",
                new
                {
                    clientInfo = new
                    {
                        name = "codex_usage_companion",
                        title = "Codex Token Quest",
                        version = "0.1.0"
                    }
                },
                cancellationToken);

            await client.SendNotificationAsync("initialized", new { }, cancellationToken);
            return client;
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }

    public async Task<UsageSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        var rateLimits = await SendRequestAsync("account/rateLimits/read", new { }, cancellationToken);

        JsonElement? usage = null;
        string? usageWarning = _unsupportedUsageWarning;
        if (_unsupportedUsageWarning is null)
        {
            try
            {
                usage = await SendRequestAsync("account/usage/read", new { }, cancellationToken);
            }
            catch (CodexAppServerException exception)
            {
                usageWarning = exception.Message;
                // Capabilities remain fixed for this app-server process. Transient
                // failures still retry; unsupported methods are probed only once.
                if (exception.IsUnsupportedMethod) _unsupportedUsageWarning = usageWarning;
            }
        }

        return UsageSnapshotParser.Parse(rateLimits, usage, usageWarning, DateTimeOffset.UtcNow)
            with { UsageUnsupported = _unsupportedUsageWarning is not null };
    }

    private async Task<JsonElement> SendRequestAsync(
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextId);
        await WriteAsync(new { method, id, @params = parameters }, cancellationToken);

        while (true)
        {
            var line = await _output.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                throw new CodexAppServerException(BuildExitMessage());
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                if (!root.TryGetProperty("id", out var responseId) || responseId.ValueKind != JsonValueKind.Number || responseId.GetInt64() != id)
                {
                    continue;
                }

                if (root.TryGetProperty("error", out var error))
                {
                    var message = error.TryGetProperty("message", out var messageNode)
                        ? messageNode.GetString()
                        : error.GetRawText();
                    if (message?.Contains("authentication required", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        throw new CodexAppServerException(
                            "Codex CLI is not signed in to ChatGPT. Run `codex login` in a terminal, complete sign-in, and try again.");
                    }

                    var code = error.TryGetProperty("code", out var codeNode) && codeNode.TryGetInt32(out var value) ? value : 0;
                    // Some Codex versions deserialize the method enum before RPC
                    // dispatch, returning Invalid Request rather than Method Not Found.
                    var unsupported = code == -32601 || (code == -32600 &&
                        message?.Contains($"unknown variant `{method}`", StringComparison.Ordinal) == true);
                    throw new CodexAppServerException($"{method} failed: {message}", unsupported);
                }

                if (!root.TryGetProperty("result", out var result))
                {
                    throw new CodexAppServerException($"{method} returned a response without a result.");
                }

                return result.Clone();
            }
        }
    }

    private Task SendNotificationAsync(string method, object parameters, CancellationToken cancellationToken) =>
        WriteAsync(new { method, @params = parameters }, cancellationToken);

    private async Task WriteAsync(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, JsonDefaults.Compact);
        await _input.WriteLineAsync(json.AsMemory(), cancellationToken);
        await _input.FlushAsync(cancellationToken);
    }

    private async Task PumpErrorsAsync(StreamReader error)
    {
        while (await error.ReadLineAsync() is { } line)
        {
            _errors.Enqueue(line);
            while (_errors.Count > 8)
            {
                _errors.TryDequeue(out _);
            }
        }
    }

    private string BuildExitMessage()
    {
        var detail = string.Join(Environment.NewLine, _errors);
        return string.IsNullOrWhiteSpace(detail)
            ? "codex app-server stopped. Verify that Codex CLI is signed in to a ChatGPT account."
            : $"codex app-server stopped: {detail}";
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _input.Close();
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            await _process.WaitForExitAsync();
            await _errorPump;
        }
        catch
        {
            // Best-effort cleanup during process shutdown.
        }
        finally
        {
            _process.Dispose();
        }
    }
}

internal sealed class CodexAppServerException : Exception
{
    public bool IsUnsupportedMethod { get; }
    public CodexAppServerException(string message, bool isUnsupportedMethod = false) : base(message)
    { IsUnsupportedMethod = isUnsupportedMethod; }
    public CodexAppServerException(string message, Exception innerException) : base(message, innerException) { }
}

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Compact = new(JsonSerializerDefaults.Web);
    public static readonly JsonSerializerOptions Pretty = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
}
