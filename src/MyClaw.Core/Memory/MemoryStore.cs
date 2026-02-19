using MyClaw.Core.Entities;

namespace MyClaw.Core.Memory;

/// <summary>
/// 文件系统记忆存储 - 支持长期记忆和每日日志
/// </summary>
public class MemoryStore
{
    private readonly string _workspace;
    private readonly string _memoryDir;
    private readonly DistillationEvaluator _evaluator;

    public MemoryStore(string workspace, int tokenBudget = 8000)
    {
        _workspace = workspace;
        _memoryDir = Path.Combine(workspace, "memory");
        _evaluator = new DistillationEvaluator(tokenBudget);

        // 确保目录存在
        Directory.CreateDirectory(_memoryDir);
        Directory.CreateDirectory(Path.Combine(_memoryDir, "archived"));
    }

    /// <summary>
    /// 读取长期记忆 (MEMORY.md)
    /// </summary>
    public string ReadLongTerm()
    {
        var path = Path.Combine(_workspace, "MEMORY.md");
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    /// <summary>
    /// 写入长期记忆 (MEMORY.md)
    /// </summary>
    public void WriteLongTerm(string content)
    {
        var path = Path.Combine(_workspace, "MEMORY.md");
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// 读取今日日志
    /// </summary>
    public string ReadToday()
    {
        var path = GetTodayPath();
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    /// <summary>
    /// 追加到今日日志
    /// </summary>
    public void AppendToday(string content)
    {
        var path = GetTodayPath();
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var entry = $"- [{timestamp}] {content}\n";
        File.AppendAllText(path, entry);
    }

    /// <summary>
    /// 获取记忆状态 (用于蒸馏评估)
    /// </summary>
    public MemoryStatus GetMemoryStatus()
    {
        var content = ReadToday();
        var info = new FileInfo(GetTodayPath());

        // 计算条目数量
        var entryCount = content.Split('\n')
            .Count(l => l.TrimStart().StartsWith("- ["));

        // 计算最旧条目年龄
        double oldestAge = 0;
        var timeMatch = System.Text.RegularExpressions.Regex.Match(content, @"^- \[(\d{1,2}:\d{2}:\d{2})\]", System.Text.RegularExpressions.RegexOptions.Multiline);
        if (timeMatch.Success)
        {
            try
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var entryTime = DateTime.Parse($"{today}T{timeMatch.Groups[1].Value}");
                oldestAge = (DateTime.Now - entryTime).TotalHours;
            }
            catch { /* ignore */ }
        }

        return new MemoryStatus
        {
            EntryCount = entryCount,
            LogBytes = (int)info.Length,
            OldestEntryAgeHours = oldestAge
        };
    }

    /// <summary>
    /// 评估是否需要蒸馏
    /// </summary>
    public DistillationEvaluation EvaluateDistillation()
    {
        var status = GetMemoryStatus();
        return _evaluator.Evaluate(status);
    }

    /// <summary>
    /// 归档今日日志
    /// </summary>
    public bool ArchiveToday()
    {
        var todayPath = GetTodayPath();
        if (!File.Exists(todayPath)) return false;

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var archivePath = Path.Combine(_memoryDir, "archived", $"{today}.md");

        File.Move(todayPath, archivePath, overwrite: true);
        return true;
    }

    /// <summary>
    /// 获取归档日志数量
    /// </summary>
    public int GetArchivedCount()
    {
        var archiveDir = Path.Combine(_memoryDir, "archived");
        if (!Directory.Exists(archiveDir)) return 0;

        return Directory.GetFiles(archiveDir, "*.md").Length;
    }

    /// <summary>
    /// 获取最近 N 天的记忆
    /// </summary>
    public string GetRecentMemories(int days)
    {
        var parts = new List<string>();
        var today = DateTime.Now;

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            var dateStr = date.ToString("yyyy-MM-dd");
            var path = Path.Combine(_memoryDir, $"{dateStr}.md");

            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                parts.Add($"## {dateStr}\n{content}");
            }
        }

        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// 从今日日志中提取相关实体
    /// </summary>
    public async Task<List<Entity>> SurfaceRelevantEntitiesAsync(EntityStore entityStore)
    {
        var content = ReadToday();
        if (string.IsNullOrEmpty(content)) return new List<Entity>();

        return await entityStore.SurfaceRelevantAsync(content);
    }

    private string GetTodayPath()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        return Path.Combine(_memoryDir, $"{today}.md");
    }
}
