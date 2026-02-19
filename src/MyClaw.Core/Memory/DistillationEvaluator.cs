namespace MyClaw.Core.Memory;

/// <summary>
/// 蒸馏紧急程度
/// </summary>
public enum DistillationUrgency
{
    Low,
    Medium,
    High
}

/// <summary>
/// 蒸馏评估结果
/// </summary>
public class DistillationEvaluation
{
    /// <summary>
    /// 是否需要蒸馏
    /// </summary>
    public bool ShouldDistill { get; set; }

    /// <summary>
    /// 评估原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 紧急程度
    /// </summary>
    public DistillationUrgency Urgency { get; set; } = DistillationUrgency.Low;
}

/// <summary>
/// 记忆蒸馏评估器
/// </summary>
public class DistillationEvaluator
{
    private const int EntryThreshold = 20;
    private const double BudgetThreshold = 0.4;
    private const int AgeHoursThreshold = 8;
    private const int MinEntriesForAge = 5;
    private const int SizeBytesThreshold = 8192;
    private const int CharsPerToken = 4;

    private readonly int _tokenBudget;

    public DistillationEvaluator(int tokenBudget = 8000)
    {
        _tokenBudget = tokenBudget;
    }

    /// <summary>
    /// 评估是否需要蒸馏
    /// </summary>
    public DistillationEvaluation Evaluate(MemoryStatus status)
    {
        // 条件1: 条目数量 > 20
        if (status.EntryCount > EntryThreshold)
        {
            return new DistillationEvaluation
            {
                ShouldDistill = true,
                Reason = $"{status.EntryCount} entries (> {EntryThreshold})",
                Urgency = DistillationUrgency.High
            };
        }

        // 条件2: 日志占用 Token 预算 > 40%
        var logTokens = status.LogBytes / CharsPerToken;
        var budgetPressure = (double)logTokens / _tokenBudget;
        if (budgetPressure > BudgetThreshold)
        {
            return new DistillationEvaluation
            {
                ShouldDistill = true,
                Reason = $"log consuming {budgetPressure:P0} of budget",
                Urgency = DistillationUrgency.High
            };
        }

        // 条件3: 最旧条目 > 8 小时且条目 > 5
        if (status.OldestEntryAgeHours > AgeHoursThreshold && status.EntryCount > MinEntriesForAge)
        {
            return new DistillationEvaluation
            {
                ShouldDistill = true,
                Reason = $"{status.EntryCount} entries, oldest {status.OldestEntryAgeHours:F1}h ago",
                Urgency = DistillationUrgency.Medium
            };
        }

        // 条件4: 日志大小 > 8KB
        if (status.LogBytes > SizeBytesThreshold)
        {
            return new DistillationEvaluation
            {
                ShouldDistill = true,
                Reason = $"log size {status.LogBytes}B (> {SizeBytesThreshold}B)",
                Urgency = DistillationUrgency.Low
            };
        }

        return new DistillationEvaluation
        {
            ShouldDistill = false,
            Reason = "ok",
            Urgency = DistillationUrgency.Low
        };
    }
}

/// <summary>
/// 记忆状态
/// </summary>
public class MemoryStatus
{
    /// <summary>
    /// 条目数量
    /// </summary>
    public int EntryCount { get; set; }

    /// <summary>
    /// 日志字节数
    /// </summary>
    public int LogBytes { get; set; }

    /// <summary>
    /// 最旧条目年龄(小时)
    /// </summary>
    public double OldestEntryAgeHours { get; set; }
}
