namespace MyClaw.Core.Workspace;

/// <summary>
/// 工作区信息 - 包含项目、Git和技术栈信息
/// </summary>
public class WorkspaceInfo
{
    /// <summary>
    /// 项目名称 (当前目录名)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作区完整路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Git 仓库信息
    /// </summary>
    public GitInfo Git { get; set; } = new();

    /// <summary>
    /// 检测到的技术栈列表
    /// </summary>
    public List<string> TechStack { get; set; } = new();

    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// 将工作区信息格式化为上下文字符串
    /// </summary>
    public string ToContextString()
    {
        var lines = new List<string>
        {
            "## 👁️ Workspace Awareness",
            $"**Project**: {Name}",
            $"**Path**: `{Path}`"
        };

        if (Git.IsRepo)
        {
            lines.Add($"**Git**: {Git.Branch} | {Git.Status}");
            if (!string.IsNullOrEmpty(Git.RecentCommits))
            {
                lines.Add($"**Recent Commits**:");
                foreach (var line in Git.RecentCommits.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    lines.Add($"  {line}");
                }
            }
            if (Git.UncommittedChanges > 0)
            {
                lines.Add($"⚠️ **{Git.UncommittedChanges} uncommitted changes**");
            }
        }

        if (TechStack.Count > 0)
        {
            lines.Add($"**Stack**: {string.Join(", ", TechStack)}");
        }

        lines.Add("");
        return string.Join("\n", lines);
    }
}
