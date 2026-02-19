using System.Text.Json.Serialization;

namespace MyClaw.Cron;

/// <summary>
/// Cron 任务调度配置
/// </summary>
public class Schedule
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "cron"; // "cron" | "every" | "at"

    [JsonPropertyName("expr")]
    public string Expr { get; set; } = ""; // cron expression

    [JsonPropertyName("everyMs")]
    public long EveryMs { get; set; } // interval in milliseconds

    [JsonPropertyName("atMs")]
    public long AtMs { get; set; } // one-shot timestamp ms
}

/// <summary>
/// Cron 任务负载
/// </summary>
public class Payload
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("deliver")]
    public bool Deliver { get; set; }

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "";

    [JsonPropertyName("to")]
    public string To { get; set; } = "";
}

/// <summary>
/// Cron 任务状态
/// </summary>
public class JobState
{
    [JsonPropertyName("nextRunAtMs")]
    public long NextRunAtMs { get; set; }

    [JsonPropertyName("lastRunAtMs")]
    public long LastRunAtMs { get; set; }

    [JsonPropertyName("lastStatus")]
    public string LastStatus { get; set; } = ""; // "ok" | "error"

    [JsonPropertyName("lastError")]
    public string LastError { get; set; } = "";
}

/// <summary>
/// Cron 任务
/// </summary>
public class CronJob
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("schedule")]
    public Schedule Schedule { get; set; } = new();

    [JsonPropertyName("payload")]
    public Payload Payload { get; set; } = new();

    [JsonPropertyName("state")]
    public JobState State { get; set; } = new();

    [JsonPropertyName("deleteAfterRun")]
    public bool DeleteAfterRun { get; set; }

    /// <summary>
    /// 创建新的 CronJob
    /// </summary>
    public static CronJob Create(string name, Schedule schedule, Payload payload)
    {
        var id = GenerateId();
        return new CronJob
        {
            Id = id,
            Name = name,
            Schedule = schedule,
            Payload = payload,
            Enabled = true
        };
    }

    private static string GenerateId()
    {
        // 生成 8 字符的随机 ID
        var bytes = new byte[4];
        Random.Shared.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
