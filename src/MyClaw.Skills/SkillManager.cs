namespace MyClaw.Skills;

/// <summary>
/// Skill 管理器 - 管理已加载的 Skills
/// </summary>
public class SkillManager
{
    private readonly string _skillsDirectory;
    private readonly Dictionary<string, Skill> _skills = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public SkillManager(string skillsDirectory)
    {
        _skillsDirectory = skillsDirectory;
    }

    /// <summary>
    /// 已加载的 Skills
    /// </summary>
    public IReadOnlyList<Skill> LoadedSkills
    {
        get
        {
            lock (_lock)
            {
                return _skills.Values.ToList();
            }
        }
    }

    /// <summary>
    /// 加载所有 Skills
    /// </summary>
    public void LoadSkills()
    {
        var skills = SkillLoader.LoadSkills(_skillsDirectory);

        lock (_lock)
        {
            _skills.Clear();
            foreach (var skill in skills)
            {
                _skills[skill.Name] = skill;
            }
        }

        Console.WriteLine($"[skills] 从 {_skillsDirectory} 加载了 {_skills.Count} 个技能");
    }

    /// <summary>
    /// 获取指定名称的 Skill
    /// </summary>
    public Skill? GetSkill(string name)
    {
        lock (_lock)
        {
            return _skills.TryGetValue(name, out var skill) ? skill : null;
        }
    }

    /// <summary>
    /// 根据关键词查找匹配的 Skills
    /// </summary>
    public List<Skill> FindByKeyword(string keyword)
    {
        var normalized = keyword.ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return new List<Skill>();
        }

        lock (_lock)
        {
            return _skills.Values
                .Where(s => s.Keywords.Any(k => k.Contains(normalized) || normalized.Contains(k)))
                .ToList();
        }
    }

    /// <summary>
    /// 检查 Skill 是否存在
    /// </summary>
    public bool HasSkill(string name)
    {
        lock (_lock)
        {
            return _skills.ContainsKey(name);
        }
    }

    /// <summary>
    /// 获取所有 Skill 名称
    /// </summary>
    public List<string> GetSkillNames()
    {
        lock (_lock)
        {
            return _skills.Keys.OrderBy(n => n).ToList();
        }
    }
}
