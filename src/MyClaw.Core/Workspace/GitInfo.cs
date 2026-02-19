namespace MyClaw.Core.Workspace;

/// <summary>
/// Git 仓库信息
/// </summary>
public class GitInfo
{
    /// <summary>
    /// 是否是 Git 仓库
    /// </summary>
    public bool IsRepo { get; set; }

    /// <summary>
    /// 当前分支
    /// </summary>
    public string Branch { get; set; } = string.Empty;

    /// <summary>
    /// 仓库状态 (clean/dirty)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 最近的提交记录
    /// </summary>
    public string RecentCommits { get; set; } = string.Empty;

    /// <summary>
    /// 未提交的更改数
    /// </summary>
    public int UncommittedChanges { get; set; }

    /// <summary>
    /// 远程仓库 URL
    /// </summary>
    public string? RemoteUrl { get; set; }
}
