using N2N_Saenai.Initialization;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using N2N_Saenai.Serialization;

namespace N2N_Saenai.Core;

public sealed class EdgeClientSession : IAsyncDisposable
{
    private readonly string _pipeName = $"SaenaiN2N-{Guid.NewGuid():N}";
    private NamedPipeServerStream? _pipe;
    private Process? _process;
    private CancellationTokenSource? _cancellation;

    // 一次会话内的连接状态跟踪（n2n stdout 关键行）
    private string? _tapIp;
    private bool _connectedRaised;

    public event EventHandler<ConnectionLogEvent>? LogReceived;
    /// <summary>n2n 完成注册且虚拟网卡就绪后触发，参数为分配到的虚拟 IP（可能为空字符串）。</summary>
    public event EventHandler<string>? Connected;
    /// <summary>n2n 进程退出时触发（无论正常断开还是异常退出）。</summary>
    public event EventHandler? Disconnected;

    public bool IsRunning => _process is { HasExited: false };

    public async Task StartAsync(N2NUserData data)
    {
        if (IsRunning) return;
        var executable = Path.Combine(AppContext.BaseDirectory, "n2n", "edge.exe");
        if (!File.Exists(executable)) throw new FileNotFoundException("未找到 n2n edge.exe，请重新生成或发布应用。", executable);

        _tapIp = null;
        _connectedRaised = false;
        _cancellation = new CancellationTokenSource();
        _pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var connected = _pipe.WaitForConnectionAsync(_cancellation.Token);
        _process = new Process { StartInfo = new ProcessStartInfo
        {
            FileName = executable, Arguments = $"--saenai-pipe {_pipeName}", UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        }};
        _process.OutputDataReceived += OnStdoutLine;
        _process.ErrorDataReceived += (_, e) => WriteLog("n2n stderr", e.Data);
        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) =>
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
            try { WriteLog("系统", $"n2n 已退出，退出代码：{_process?.ExitCode}。"); }
            catch (InvalidOperationException) { WriteLog("系统", "n2n 已退出。"); }
        };
        if (!_process.Start()) throw new InvalidOperationException("无法启动 n2n edge。");
        WriteLog("系统", "n2n edge 进程已启动。");
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        try
        {
            await connected.WaitAsync(TimeSpan.FromSeconds(10), _cancellation.Token);
            await using var writer = new StreamWriter(_pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync(JsonSerializer.Serialize(new PipeMessage { Type = "configure", Data = data }, AppJsonContext.Default.PipeMessage));
            _ = ReadPipeAsync(_pipe, _cancellation.Token);
            WriteLog("系统", "已将会话配置安全传给 n2n，正在建立连接。");
        }
        catch
        {
            await StopAsync();
            throw new TimeoutException("n2n 未在 10 秒内连接到应用控制通道。");
        }
    }

    public async Task StopAsync()
    {
        _cancellation?.Cancel();
        if (_process is { HasExited: false }) _process.Kill(entireProcessTree: true);
        if (_process is not null) { await _process.WaitForExitAsync(); _process.Dispose(); }
        _process = null;
        _pipe?.Dispose(); _pipe = null;
        _cancellation?.Dispose(); _cancellation = null;
    }

    // n2n 在终端输出的所有内容都原样打印进日志面板；
    // 同时从关键行中提取连接状态（虚拟 IP、连接成功）。
    private void OnStdoutLine(object? sender, DataReceivedEventArgs e)
    {
        var line = e.Data;
        if (string.IsNullOrWhiteSpace(line)) return;

        if (line.Contains("created local tap device IP:"))
        {
            _tapIp = ExtractIp(line);
        }

        if (line.Contains("received REGISTER_SUPER_ACK from supernode") ||
            line.Contains("[OK] edge <<<"))
        {
            // 重新注册的应答会周期性出现，连接成功只触发一次。
            TryRaiseConnected();
        }

        WriteLog("n2n", line);
    }

    // "连接成功" = 虚拟网卡就绪 + 超级节点应答，两者都到齐才算。
    private void TryRaiseConnected()
    {
        if (_connectedRaised || _tapIp is null) return;
        _connectedRaised = true;
        Connected?.Invoke(this, _tapIp);
    }

    private static string ExtractIp(string line)
    {
        // 形如：created local tap device IP: 10.10.114.213, Mask: ...
        var start = line.IndexOf("IP:", StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        start += 3;
        while (start < line.Length && !char.IsDigit(line[start])) start++;
        var end = start;
        while (end < line.Length && (char.IsDigit(line[end]) || line[end] == '.')) end++;
        return line[start..end];
    }

    private async Task ReadPipeAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        while (!cancellationToken.IsCancellationRequested && await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            try
            {
                var message = JsonSerializer.Deserialize(line, AppJsonContext.Default.PipeMessage);
                if (message is null)
                {
                    WriteLog("n2n pipe", line);
                    continue;
                }

                // Show every wrapper message. Unknown future message kinds are retained as raw JSON.
                WriteLog($"n2n pipe/{message.Type ?? "unknown"}", message.Message ?? line);
            }
            catch (JsonException) { WriteLog("n2n pipe", line); }
        }
    }

    private void WriteLog(string source, string? message)
    {
        if (!string.IsNullOrWhiteSpace(message)) LogReceived?.Invoke(this, new ConnectionLogEvent(source, message));
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}

public sealed record ConnectionLogEvent(string Source, string Message);

public sealed class PipeMessage
{
    [System.Text.Json.Serialization.JsonPropertyName("type")] public string? Type { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("level")] public string? Level { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("message")] public string? Message { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("data")] public N2NUserData? Data { get; set; }
}
