using MyClaw.Core.Messaging;

namespace MyClaw.Channels;

/// <summary>
/// 渠道接口 - 定义消息渠道的基本操作
/// </summary>
public interface IChannel
{
    /// <summary>
    /// 渠道名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 是否已启用
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 启动渠道
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// 停止渠道
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 发送消息
    /// </summary>
    Task SendAsync(OutboundMessage message, CancellationToken ct = default);
}

/// <summary>
/// 渠道基类
/// </summary>
public abstract class ChannelBase : IChannel
{
    protected readonly HashSet<string> _allowFrom;

    public abstract string Name { get; }
    public abstract bool IsEnabled { get; }

    protected ChannelBase(IEnumerable<string>? allowFrom = null)
    {
        _allowFrom = allowFrom?.ToHashSet() ?? new HashSet<string>();
    }

    /// <summary>
    /// 检查发送者是否被允许
    /// </summary>
    protected bool IsAllowed(string senderId)
    {
        if (_allowFrom.Count == 0)
            return true;
        return _allowFrom.Contains(senderId);
    }

    public abstract Task StartAsync(CancellationToken ct = default);
    public abstract Task StopAsync();
    public abstract Task SendAsync(OutboundMessage message, CancellationToken ct = default);
}
