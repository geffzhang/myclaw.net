// MessageBus 已移动到 MyClaw.Core.Messaging
// 为了向后兼容，这里导出同名类型
namespace MyClaw.Gateway;

/// <summary>
/// 消息总线 - 已移动到 MyClaw.Core.Messaging
/// </summary>
[Obsolete("Use MyClaw.Core.Messaging.MessageBus instead")]
public class MessageBus : Core.Messaging.MessageBus
{
    public MessageBus(int bufferSize = 100) : base(bufferSize) { }
}
