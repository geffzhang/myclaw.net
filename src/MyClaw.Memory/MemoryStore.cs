using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MyClaw.Memory;

/// <summary>
/// 记忆存储 - 管理长期记忆和每日日记
/// </summary>
public class MemoryStore
{
    private readonly string _workspace;

    public MemoryStore(string workspace)
    {
        _workspace = workspace;
    }

    private string MemoryDir => Path.Combine(_workspace, "memory");

    private void EnsureDir()
    {
        if (!Directory.Exists(MemoryDir))
        {
            Directory.CreateDirectory(MemoryDir);
        }
    }

    #region Long-term Memory

    /// <summary>
    /// 读取长期记忆 (MEMORY.md)
    /// </summary>
    public string ReadLongTerm()
    {
        var path = Path.Combine(MemoryDir, "MEMORY.md");
        if (!File.Exists(path))
        {
            return string.Empty;
        }
        return File.ReadAllText(path);
    }

    /// <summary>
    /// 写入长期记忆 (MEMORY.md)
    /// </summary>
    public void WriteLongTerm(string content)
    {
        EnsureDir();
        var path = Path.Combine(MemoryDir, "MEMORY.md");
        File.WriteAllText(path, content);
    }

    #endregion

    #region Daily Journal

    /// <summary>
    /// 获取今天的日记文件路径
    /// </summary>
    private string GetTodayFilePath()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        return Path.Combine(MemoryDir, $"{today}.md");
    }

    /// <summary>
    /// 读取今天的日记
    /// </summary>
    public string ReadToday()
    {
        var path = GetTodayFilePath();
        if (!File.Exists(path))
        {
            return string.Empty;
        }
        return File.ReadAllText(path);
    }

    /// <summary>
    /// 追加内容到今天日记
    /// </summary>
    public void AppendToday(string content)
    {
        EnsureDir();
        var path = GetTodayFilePath();
        File.AppendAllText(path, content + Environment.NewLine);
    }

    /// <summary>
    /// 获取最近几天的记忆
    /// </summary>
    public string GetRecentMemories(int days)
    {
        if (!Directory.Exists(MemoryDir))
        {
            return string.Empty;
        }

        var entries = Directory.GetFiles(MemoryDir, "*.md")
            .Select(f => new FileInfo(f))
            .Where(f => f.Name != "MEMORY.md")
            .OrderByDescending(f => f.Name)
            .ToList();

        if (days > 0 && entries.Count > days)
        {
            entries = entries.Take(days).ToList();
        }

        var sb = new StringBuilder();
        foreach (var entry in entries)
        {
            var content = File.ReadAllText(entry.FullName).Trim();
            if (string.IsNullOrEmpty(content))
            {
                continue;
            }

            var date = Path.GetFileNameWithoutExtension(entry.Name);
            sb.AppendLine($"## {date}");
            sb.AppendLine(content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    #endregion

    #region Context Assembly

    /// <summary>
    /// 获取用于 LLM 系统提示的记忆上下文
    /// </summary>
    public string GetMemoryContext()
    {
        var sb = new StringBuilder();

        var longTerm = ReadLongTerm();
        if (!string.IsNullOrWhiteSpace(longTerm))
        {
            sb.AppendLine("# Long-term Memory");
            sb.AppendLine(longTerm);
            sb.AppendLine();
        }

        var recent = GetRecentMemories(7);
        if (!string.IsNullOrWhiteSpace(recent))
        {
            sb.AppendLine("# Recent Journal");
            sb.AppendLine(recent);
        }

        return sb.ToString();
    }

    #endregion
}
