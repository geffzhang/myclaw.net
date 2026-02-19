namespace MyClaw.Heartbeat;

/// <summary>
/// 心跳服务 - 周期性执行任务
/// </summary>
public class HeartbeatService
{
    private readonly string _workspace;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// 心跳回调
    /// </summary>
    public Func<string, Task<string>>? OnHeartbeat { get; set; }

    public HeartbeatService(string workspace, TimeSpan? interval = null)
    {
        _workspace = workspace;
        _interval = interval ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// 启动心跳服务
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _cts.Token;

        Console.WriteLine($"[heartbeat] started, interval={_interval}");

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                await TickAsync();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[heartbeat] stopped");
        }
    }

    /// <summary>
    /// 停止心跳服务
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 执行一次心跳
    /// </summary>
    private async Task TickAsync()
    {
        var hbPath = Path.Combine(_workspace, "HEARTBEAT.md");
        
        if (!File.Exists(hbPath))
        {
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(hbPath);
            content = content.Trim();

            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            Console.WriteLine($"[heartbeat] triggering with prompt ({content.Length} chars)");

            if (OnHeartbeat == null)
            {
                Console.WriteLine("[heartbeat] no handler set");
                return;
            }

            var result = await OnHeartbeat(content);

            if (result.Contains("HEARTBEAT_OK"))
            {
                Console.WriteLine("[heartbeat] nothing to do");
            }
            else
            {
                Console.WriteLine($"[heartbeat] result: {Truncate(result, 200)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[heartbeat] error: {ex.Message}");
        }
    }

    private static string Truncate(string s, int n)
    {
        if (s.Length <= n)
            return s;
        return s[..n] + "...";
    }
}
