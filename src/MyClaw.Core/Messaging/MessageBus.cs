using System.Threading.Channels;

namespace MyClaw.Core.Messaging;

/// <summary>
/// 消息总线 - 管理入站和出站消息
/// </summary>
public class MessageBus
{
    private readonly Channel<InboundMessage> _inbound;
    private readonly Channel<OutboundMessage> _outbound;
    private readonly Dictionary<string, List<Func<OutboundMessage, Task>>> _subscribers = new();
    private readonly object _lock = new();

    /// <summary>
    /// 入站消息通道读取器
    /// </summary>
    public ChannelReader<InboundMessage> InboundReader => _inbound.Reader;

    /// <summary>
    /// 入站消息通道写入器
    /// </summary>
    public ChannelWriter<InboundMessage> InboundWriter => _inbound.Writer;

    /// <summary>
    /// 出站消息通道读取器
    /// </summary>
    public ChannelReader<OutboundMessage> OutboundReader => _outbound.Reader;

    /// <summary>
    /// 出站消息通道写入器
    /// </summary>
    public ChannelWriter<OutboundMessage> OutboundWriter => _outbound.Writer;

    public MessageBus(int bufferSize = 100)
    {
        var options = new BoundedChannelOptions(bufferSize)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _inbound = Channel.CreateBounded<InboundMessage>(options);
        _outbound = Channel.CreateBounded<OutboundMessage>(options);
    }

    /// <summary>
    /// 发布入站消息
    /// </summary>
    public async Task PublishInboundAsync(InboundMessage message, CancellationToken ct = default)
    {
        await _inbound.Writer.WriteAsync(message, ct);
    }

    /// <summary>
    /// 发布出站消息
    /// </summary>
    public async Task PublishOutboundAsync(OutboundMessage message, CancellationToken ct = default)
    {
        await _outbound.Writer.WriteAsync(message, ct);
    }

    /// <summary>
    /// 订阅出站消息
    /// </summary>
    public void SubscribeOutbound(string channel, Func<OutboundMessage, Task> handler)
    {
        lock (_lock)
        {
            if (!_subscribers.ContainsKey(channel))
            {
                _subscribers[channel] = new List<Func<OutboundMessage, Task>>();
            }
            _subscribers[channel].Add(handler);
        }
    }

    /// <summary>
    /// 分发出站消息到订阅者
    /// </summary>
    public async Task DispatchOutboundAsync(CancellationToken ct = default)
    {
        await foreach (var message in _outbound.Reader.ReadAllAsync(ct))
        {
            List<Func<OutboundMessage, Task>>? handlers;
            lock (_lock)
            {
                if (!_subscribers.TryGetValue(message.Channel, out handlers) || handlers == null || handlers.Count == 0)
                {
                    Console.WriteLine($"[bus] no subscriber for channel {message.Channel}, dropping message");
                    continue;
                }
            }

            // 并行发送给所有订阅者
            var tasks = handlers.Select(h => h(message));
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 关闭消息总线
    /// </summary>
    public void Close()
    {
        _inbound.Writer.Complete();
        _outbound.Writer.Complete();
    }
}
