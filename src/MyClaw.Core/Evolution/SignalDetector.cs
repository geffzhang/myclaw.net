using System.Text.RegularExpressions;

namespace MyClaw.Core.Evolution;

/// <summary>
/// 进化信号类型
/// </summary>
public enum EvolutionSignal
{
    UserPreference,     // 用户偏好
    PersonalityCorrection, // 性格修正
    EnvironmentConfig,  // 环境配置
    ToolExperience,     // 工具经验
    IdentityChange,     // 身份改变
    WorkflowLearned,    // 工作流学习
    ImportantFact,      // 重要事实
    DailyLogEntry       // 日常记录
}

/// <summary>
/// 检测到的信号
/// </summary>
public class DetectedSignal
{
    /// <summary>
    /// 信号类型
    /// </summary>
    public EvolutionSignal SignalType { get; set; }

    /// <summary>
    /// 目标文件
    /// </summary>
    public string TargetFile { get; set; } = string.Empty;

    /// <summary>
    /// 建议工具
    /// </summary>
    public string SuggestedTool { get; set; } = string.Empty;

    /// <summary>
    /// 匹配内容
    /// </summary>
    public string MatchedContent { get; set; } = string.Empty;

    /// <summary>
    /// 置信度 (0-1)
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// 进化信号检测器 - 实现"信号 → 文件 → 工具"的自动化进化链
/// </summary>
public class SignalDetector
{
    // 信号检测模式
    private readonly List<(EvolutionSignal Signal, string[] Patterns, string TargetFile, string Tool)> _signalPatterns = new()
    {
        // 用户偏好 -> USER.md
        (EvolutionSignal.UserPreference,
            new[] { "我喜欢", "I like", "不要", "don't", "以后请", "please.*next time", "记住我喜欢", "remember I like" },
            "USER.md", "miniclaw_update"),

        // 性格修正 -> SOUL.md
        (EvolutionSignal.PersonalityCorrection,
            new[] { "别那么严肃", "less serious", "活泼一点", "more lively", "你是一个", "you are a", "改变性格", "change personality" },
            "SOUL.md", "miniclaw_update"),

        // 环境配置 -> TOOLS.md
        (EvolutionSignal.EnvironmentConfig,
            new[] { "项目用的是", "project uses", "服务器IP", "server IP", "路径是", "path is", "API key", "密钥" },
            "TOOLS.md", "miniclaw_update"),

        // 工具经验 -> TOOLS.md
        (EvolutionSignal.ToolExperience,
            new[] { "这个工具的参数", "tool parameter", "踩坑记录", "pitfall", "解决方案", "solution.*tool" },
            "TOOLS.md", "miniclaw_update"),

        // 身份改变 -> IDENTITY.md
        (EvolutionSignal.IdentityChange,
            new[] { "叫你自己", "call yourself", "记住你的名字是", "your name is", "改名", "rename" },
            "IDENTITY.md", "miniclaw_update"),

        // 工作流学习 -> AGENTS.md
        (EvolutionSignal.WorkflowLearned,
            new[] { "最好的实践是", "best practice", "以后都按这个流程", "follow this workflow", "标准化", "standardize" },
            "AGENTS.md", "miniclaw_update"),

        // 重要事实 -> MEMORY.md
        (EvolutionSignal.ImportantFact,
            new[] { "重要", "important", "记住这个", "remember this", "别忘了", "don't forget", "mark this" },
            "MEMORY.md", "miniclaw_update"),
    };

    // 日常记录触发词 -> 每日日志
    private readonly string[] _dailyLogTriggers = {
        "记住这个", "mark", "note", "别忘了", "don't forget",
        "完成了", "finished", "下一步", "next step"
    };

    /// <summary>
    /// 检测用户输入中的进化信号
    /// </summary>
    public List<DetectedSignal> DetectSignals(string userInput)
    {
        var signals = new List<DetectedSignal>();
        if (string.IsNullOrWhiteSpace(userInput)) return signals;

        var lowerInput = userInput.ToLower();

        foreach (var (signal, patterns, targetFile, tool) in _signalPatterns)
        {
            foreach (var pattern in patterns)
            {
                // 简单包含匹配
                if (lowerInput.Contains(pattern.ToLower()))
                {
                    signals.Add(new DetectedSignal
                    {
                        SignalType = signal,
                        TargetFile = targetFile,
                        SuggestedTool = tool,
                        MatchedContent = pattern,
                        Confidence = 0.8
                    });
                    break; // 该信号类型已匹配，不再检查其他模式
                }

                // 正则匹配（如果模式包含正则元字符）
                try
                {
                    if (Regex.IsMatch(userInput, pattern, RegexOptions.IgnoreCase))
                    {
                        signals.Add(new DetectedSignal
                        {
                            SignalType = signal,
                            TargetFile = targetFile,
                            SuggestedTool = tool,
                            MatchedContent = pattern,
                            Confidence = 0.9
                        });
                        break;
                    }
                }
                catch { /* 忽略无效正则 */ }
            }
        }

        // 检测日常记录触发词
        foreach (var trigger in _dailyLogTriggers)
        {
            if (lowerInput.Contains(trigger.ToLower()))
            {
                // 检查是否已经被其他信号覆盖
                if (!signals.Any(s => s.SignalType == EvolutionSignal.ImportantFact))
                {
                    signals.Add(new DetectedSignal
                    {
                        SignalType = EvolutionSignal.DailyLogEntry,
                        TargetFile = $"memory/{DateTime.Now:yyyy-MM-dd}.md",
                        SuggestedTool = "miniclaw_note",
                        MatchedContent = trigger,
                        Confidence = 0.7
                    });
                }
                break;
            }
        }

        return signals.DistinctBy(s => s.SignalType).ToList();
    }

    /// <summary>
    /// 生成进化建议
    /// </summary>
    public string GenerateEvolutionAdvice(List<DetectedSignal> signals)
    {
        if (signals.Count == 0) return string.Empty;

        var lines = new List<string>();
        lines.Add("🧬 检测到进化信号:");

        foreach (var signal in signals)
        {
            lines.Add($"  • {signal.SignalType} → {signal.TargetFile} (使用 {signal.SuggestedTool})");
        }

        lines.Add("\n建议执行相应的工具调用以更新记忆。");

        return string.Join("\n", lines);
    }
}
