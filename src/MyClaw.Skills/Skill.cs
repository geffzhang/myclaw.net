namespace MyClaw.Skills;

/// <summary>
/// Skill 定义
/// </summary>
public class Skill
{
    /// <summary>
    /// Skill 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Skill 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 关键词列表
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Skill 内容（Markdown 主体）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 来源文件路径
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// 所在目录
    /// </summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// 获取系统提示词
    /// </summary>
    public string GetSystemPrompt()
    {
        return Content;
    }
}

/// <summary>
/// Skill 元数据（YAML Frontmatter）
/// </summary>
public class SkillMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
}
