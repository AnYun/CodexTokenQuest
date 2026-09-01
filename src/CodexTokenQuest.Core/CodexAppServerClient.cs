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

    private CodexAppServerClient(Process process)
    {
        _process = process;
        _input = process.StandardInput;
        _output = process.StandardOutput;
        _errorPump = PumpErrorsAsync(process.StandardError);
    }

    public static async Task<CodexAppServerClient> StartAsync(CancellationToken cancellationToken)
    {
        Process process;
        try
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = "codex",
                Arguments = "app-server --listen stdio://",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }) ?? throw new CodexAppServerException("無法啟動 codex app-server。");
        }
        catch (Exception exception) when (exception is not CodexAppServerException)
        {
            throw new CodexAppServerException("找不到 Codex CLI。請先安裝 Codex 並完成登入。", exception);
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
        string? usageWarning = null;
        try
        {
            usage = await SendRequestAsync("account/usage/read", new { }, cancellationToken);
        }
        catch (CodexAppServerException exception)
        {
            usageWarning = exception.Message;
        }

        return UsageSnapshotParser.Parse(rateLimits, usage, usageWarning, DateTimeOffset.UtcNow);
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
                            "Codex CLI 尚未登入 ChatGPT。請先在終端執行 `codex login`，完成登入後再試一次。" );
                    }

                    throw new CodexAppServerException($"{method} 失敗：{message}");
                }

                if (!root.TryGetProperty("result", out var result))
                {
                    throw new CodexAppServerException($"{method} 回傳內容缺少 result。" );
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
            ? "codex app-server 已停止。請確認 Codex CLI 已登入 ChatGPT 帳號。"
            : $"codex app-server 已停止：{detail}";
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
    public CodexAppServerException(string message) : base(message) { }
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
