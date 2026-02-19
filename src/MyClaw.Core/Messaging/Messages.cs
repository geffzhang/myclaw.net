namespace MyClaw.Core.Messaging;

/// <summary>
/// 入站消息 - 从各个渠道接收的消息
/// </summary>
public class InboundMessage
{
    /// <summary>
    /// 渠道名称 (telegram, feishu, wecom, whatsapp, webui)
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// 发送者 ID
    /// </summary>
    public string SenderID { get; set; } = string.Empty;

    /// <summary>
    /// 聊天/会话 ID
    /// </summary>
    public string ChatID { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 媒体文件路径列表
    /// </summary>
    public List<string> Media { get; set; } = new();

    /// <summary>
    /// 附加元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 获取会话键 (Channel:ChatID)
    /// </summary>
    public string SessionKey => $"{Channel}:{ChatID}";
}

/// <summary>
/// 出站消息 - 发送到各个渠道的消息
/// </summary>
public class OutboundMessage
{
    /// <summary>
    /// 渠道名称
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// 聊天/会话 ID
    /// </summary>
    public string ChatID { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 回复的消息 ID
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// 媒体文件路径列表
    /// </summary>
    public List<string> Media { get; set; } = new();

    /// <summary>
    /// 附加元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
