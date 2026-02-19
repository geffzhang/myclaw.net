namespace MyClaw.Core.Ace;

/// <summary>
/// 会话延续检测结果
/// </summary>
public class ContinuationResult
{
    /// <summary>
    /// 是否是返回场景
    /// </summary>
    public bool IsReturn { get; set; }

    /// <summary>
    /// 距离上次活动的小时数
    /// </summary>
    public double HoursSinceLastActivity { get; set; }

    /// <summary>
    /// 最后话题
    /// </summary>
    public string LastTopic { get; set; } = string.Empty;

    /// <summary>
    /// 近期决策
    /// </summary>
    public List<string> RecentDecisions { get; set; } = new();

    /// <summary>
    /// 开放问题
    /// </summary>
    public List<string> OpenQuestions { get; set; } = new();
}

/// <summary>
/// 会话延续检测器
/// </summary>
public class ContinuationDetector
{
    /// <summary>
    /// 检测会话延续状态
    /// </summary>
    public ContinuationResult Detect(string dailyLog, DateTime? lastActivity)
    {
        var result = new ContinuationResult
        {
            IsReturn = false,
            HoursSinceLastActivity = 0,
            LastTopic = string.Empty,
            RecentDecisions = new List<string>(),
            OpenQuestions = new List<string>()
        };

        if (!lastActivity.HasValue) return result;

        var hoursSince = (DateTime.Now - lastActivity.Value).TotalHours;
        if (hoursSince < 1) return result; // 不到1小时不算"返回"

        result.IsReturn = true;
        result.HoursSinceLastActivity = Math.Round(hoursSince * 10) / 10;

        if (string.IsNullOrEmpty(dailyLog)) return result;

        // 提取条目
        var entries = dailyLog.Split('\n')
            .Where(l => l.TrimStart().StartsWith("- ["))
            .ToList();

        if (entries.Count == 0) return result;

        // 提取最后话题
        var lastEntry = entries.Last();
        var topicMatch = System.Text.RegularExpressions.Regex.Match(
            lastEntry, @"^- \[\d{1,2}:\d{2}(?::\d{2})?\]\s*(.+)$");
        if (topicMatch.Success)
        {
            result.LastTopic = topicMatch.Groups[1].Value.Substring(0,
                Math.Min(120, topicMatch.Groups[1].Value.Length));
        }

        // 提取决策 (最近10条)
        var decisionPatterns = new[] { "decided", "选择", "确认", "agreed", "决定", "chosen", "confirmed" };
        result.RecentDecisions = entries.TakeLast(10)
            .Where(e => decisionPatterns.Any(p =>
                e.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .Select(e => System.Text.RegularExpressions.Regex.Replace(e,
                @"^- \[\d{1,2}:\d{2}(?::\d{2})?\]\s*", "").Substring(0,
                Math.Min(80, System.Text.RegularExpressions.Regex.Replace(e,
                @"^- \[\d{1,2}:\d{2}(?::\d{2})?\]\s*", "").Length)))
            .ToList();

        // 提取开放问题
        var questionPatterns = new[] { "?", "TODO", "todo", "待", "问题", "question", "需要" };
        result.OpenQuestions = entries.TakeLast(10)
            .Where(e => questionPatterns.Any(p => e.Contains(p)))
            .Select(e => System.Text.RegularExpressions.Regex.Replace(e,
                @"^- \[\d{1,2}:\d{2}(?::\d{2})?\]\s*", "").Substring(0,
                Math.Min(80, System.Text.RegularExpressions.Regex.Replace(e,
                @"^- \[\d{1,2}:\d{2}(?::\d{2})?\]\s*", "").Length)))
            .ToList();

        return result;
    }
}
