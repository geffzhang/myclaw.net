namespace MyClaw.Uno;

/// <summary>
/// Service interface for WebUI to communicate with MyClaw Agent
/// </summary>
public interface IWebUIService
{
    /// <summary>
    /// Event triggered when a message is received from the agent
    /// </summary>
    event EventHandler<string>? OnMessageReceived;

    /// <summary>
    /// Send a message to the agent
    /// </summary>
    Task SendMessageAsync(string message);

    /// <summary>
    /// Check if the service is connected
    /// </summary>
    bool IsConnected { get; }
}

/// <summary>
/// WebUI Service implementation that connects to Gateway
/// </summary>
public class WebUIService : IWebUIService
{
    private readonly Func<string, Task<string>> _onSendMessage;

    public event EventHandler<string>? OnMessageReceived;

    public bool IsConnected { get; private set; } = true;

    public WebUIService(Func<string, Task<string>> onSendMessage)
    {
        _onSendMessage = onSendMessage;
    }

    public async Task SendMessageAsync(string message)
    {
        try
        {
            var response = await _onSendMessage(message);
            OnMessageReceived?.Invoke(this, response);
        }
        catch (Exception ex)
        {
            OnMessageReceived?.Invoke(this, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Simulate receiving a message (for testing)
    /// </summary>
    public void ReceiveMessage(string message)
    {
        OnMessageReceived?.Invoke(this, message);
    }
}
