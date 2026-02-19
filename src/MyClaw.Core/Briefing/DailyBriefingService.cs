using MyClaw.Core.Analytics;
using MyClaw.Core.Entities;
using MyClaw.Memory;

namespace MyClaw.Core.Briefing;

/// <summary>
/// 每日简报服务 - 生成昨日回顾和今日概览
/// </summary>
public class DailyBriefingService
{
    private readonly MemoryStore _memoryStore;
    private readonly AnalyticsService _analyticsService;
    private readonly EntityStore? _entityStore;

    public DailyBriefingService(
        MemoryStore memoryStore,
        AnalyticsService analyticsService,
        EntityStore? entityStore = null)
    {
        _memoryStore = memoryStore;
        _analyticsService = analyticsService;
        _entityStore = entityStore;
    }

    /// <summary>
    /// 生成每日简报
    /// </summary>
    public async Task<string> GenerateBriefingAsync()
    {
        var now = DateTime.Now;
        var today = now.ToString("yyyy-MM-dd");
        var yesterday = now.AddDays(-1).ToString("yyyy-MM-dd");

        var lines = new List<string>
        {
            $"## 🌅 Daily Briefing — {today}",
            ""
        };

        // 昨日活动
        var yesterdaySection = await GenerateYesterdaySectionAsync(yesterday);
        if (!string.IsNullOrEmpty(yesterdaySection))
        {
            lines.Add(yesterdaySection);
        }

        // 未解决问题
        var openQuestionsSection = await GenerateOpenQuestionsSectionAsync(yesterday);
        if (!string.IsNullOrEmpty(openQuestionsSection))
        {
            lines.Add(openQuestionsSection);
        }

        // 使用统计
        var statsSection = GenerateStatsSection();
        lines.Add(statsSection);

        // 实体摘要
        if (_entityStore != null)
        {
            var entitySection = await GenerateEntitySectionAsync();
            if (!string.IsNullOrEmpty(entitySection))
            {
                lines.Add(entitySection);
            }
        }

        // 健康检查
        var healthSection = GenerateHealthSection();
        if (!string.IsNullOrEmpty(healthSection))
        {
            lines.Add(healthSection);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成昨日回顾部分
    /// </summary>
    private async Task<string> GenerateYesterdaySectionAsync(string yesterday)
    {
        var yesterdayLog = _memoryStore.GetRecentMemories(2);
        
        if (string.IsNullOrWhiteSpace(yesterdayLog))
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            "### 📋 Yesterday's Activity",
            ""
        };

        // 解析条目数
        var entries = yesterdayLog.Split('\n')
            .Where(l => l.TrimStart().StartsWith("- ["))
            .ToList();

        lines.Add($"Total entries: {entries.Count}");
        lines.Add("");

        // 显示最近 5 条
        var recent = entries.TakeLast(5).ToList();
        if (recent.Count > 0)
        {
            lines.Add("Recent entries:");
            foreach (var entry in recent)
            {
                lines.Add(entry);
            }
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成未解决问题部分
    /// </summary>
    private async Task<string> GenerateOpenQuestionsSectionAsync(string yesterday)
    {
        var yesterdayLog = _memoryStore.GetRecentMemories(2);
        
        if (string.IsNullOrWhiteSpace(yesterdayLog))
        {
            return string.Empty;
        }

        // 查找包含问题标记的行
        var questionPatterns = new[] { "?", "TODO", "todo", "待", "问题", "question", "需要" };
        var questions = yesterdayLog.Split('\n')
            .Where(l => questionPatterns.Any(p => l.Contains(p)))
            .Take(5)
            .ToList();

        if (questions.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            "### ❌ Unresolved Questions",
            ""
        };

        foreach (var q in questions)
        {
            lines.Add(q.Trim());
        }
        lines.Add("");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成统计部分
    /// </summary>
    private string GenerateStatsSection()
    {
        var analytics = _analyticsService.GetAnalytics();
        var lines = new List<string>
        {
            "### 📊 Usage Stats",
            ""
        };

        lines.Add($"- Boot count: {analytics.BootCount}");
        lines.Add($"- Average boot time: {analytics.AverageBootMs}ms");
        lines.Add($"- Total tool calls: {analytics.TotalToolCalls}");

        // 最常用工具
        var topTools = analytics.GetTopTools(3);
        if (topTools.Count > 0)
        {
            lines.Add($"- Top tools: {string.Join(", ", topTools.Select(t => $"{t.Key}({t.Value})"))}");
        }

        lines.Add("");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成实体摘要部分
    /// </summary>
    private async Task<string> GenerateEntitySectionAsync()
    {
        if (_entityStore == null) return string.Empty;

        var entities = await _entityStore.ListAsync();
        if (entities.Count == 0) return string.Empty;

        var lines = new List<string>
        {
            "### 🕸️ Top Entities",
            ""
        };

        var recentEntities = entities
            .OrderByDescending(e => e.LastMentioned)
            .Take(5)
            .ToList();

        foreach (var e in recentEntities)
        {
            lines.Add($"- **{e.Name}** ({e.Type}, {e.MentionCount}x) — last: {e.LastMentioned}");
        }
        lines.Add("");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成健康检查部分
    /// </summary>
    private string GenerateHealthSection()
    {
        // 检查是否需要蒸馏
        var recent = _memoryStore.GetRecentMemories(1);
        var entryCount = recent.Split('\n').Count(l => l.TrimStart().StartsWith("- ["));

        if (entryCount < 10)
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            "### 🏥 Health",
            ""
        };

        if (entryCount > 20)
        {
            lines.Add($"⚠️ Memory has {entryCount} entries. Consider distilling.");
        }
        else
        {
            lines.Add($"ℹ️ Memory has {entryCount} entries.");
        }
        lines.Add("");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成简单的单行摘要
    /// </summary>
    public string GenerateOneLineSummary()
    {
        var analytics = _analyticsService.GetAnalytics();
        var parts = new List<string>();

        if (analytics.BootCount > 0)
        {
            parts.Add($"🔄 {analytics.BootCount} boots");
        }

        if (analytics.TotalToolCalls > 0)
        {
            parts.Add($"🔧 {analytics.TotalToolCalls} tool calls");
        }

        var topTool = analytics.GetTopTools(1).FirstOrDefault();
        if (!string.IsNullOrEmpty(topTool.Key))
        {
            parts.Add($"⭐ Top: {topTool.Key}");
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : "No activity yet";
    }
}
