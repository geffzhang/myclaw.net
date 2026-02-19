namespace MyClaw.Memory;

/// <summary>
/// 为 Agent 提供记忆上下文
/// </summary>
public static class MemoryContextProvider
{
    /// <summary>
    /// 获取记忆上下文字符串
    /// </summary>
    public static string GetMemoryContext(this MemoryStore store)
    {
        var parts = new List<string>();

        // 添加长期记忆摘要
        var longTerm = store.ReadLongTerm();
        if (!string.IsNullOrWhiteSpace(longTerm))
        {
            parts.Add("## 长期记忆\n" + longTerm);
        }

        // 添加近期记忆（最近3天）
        var recent = store.GetRecentMemories(3);
        if (!string.IsNullOrWhiteSpace(recent))
        {
            parts.Add("## 近期记忆（最近3天）\n" + recent);
        }

        return string.Join("\n\n", parts);
    }
}
