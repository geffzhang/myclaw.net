using MyClaw.Core.Configuration;
using MyClaw.Core.Messaging;

namespace MyClaw.Channels.Uno;

/// <summary>
/// Uno Platform Channel - Hosts the Uno Platform UI
/// </summary>
public class UnoPlatformChannel : ChannelBase
{
    private readonly UnoUIConfig _config;
    private readonly MessageBus _messageBus;
    private CancellationTokenSource? _cts;
    private Task? _uiTask;

    public override string Name => "uno";
    public override bool IsEnabled => _config.Enabled;

    public UnoPlatformChannel(UnoUIConfig config, MessageBus messageBus) : base(config.AllowFrom)
    {
        _config = config;
        _messageBus = messageBus;
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        if (_config.Mode.Equals("desktop", StringComparison.OrdinalIgnoreCase))
        {
            // Desktop mode - start Uno app in separate thread
            _uiTask = Task.Run(() => RunDesktopApp(_cts.Token), _cts.Token);
        }
        else if (_config.Mode.Equals("wasm", StringComparison.OrdinalIgnoreCase) || 
                 _config.Mode.Equals("webassembly", StringComparison.OrdinalIgnoreCase))
        {
            // WebAssembly mode - would be hosted in browser
            Console.WriteLine("[uno] WebAssembly mode - host in browser");
        }

        return Task.CompletedTask;
    }

    public override async Task StopAsync()
    {
        _cts?.Cancel();
        
        if (_uiTask != null)
        {
            try
            {
                await _uiTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    public override async Task SendAsync(OutboundMessage message, CancellationToken ct = default)
    {
        // Send message to Uno UI
        // This would be implemented via IPC or shared state
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle message from UI to agent
    /// </summary>
    private async Task<string> HandleUIMessageAsync(string message, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string>();

        // Create a unique session for this UI interaction
        var sessionId = $"uno-{Guid.NewGuid():N}";

        // Publish to message bus
        await _messageBus.PublishInboundAsync(new InboundMessage
        {
            Channel = "uno",
            ChatID = sessionId,
            SenderID = "uno-ui",
            Content = message,
            Timestamp = DateTime.UtcNow
        }, ct);

        // For now, return a simple acknowledgment
        // In real implementation, this would wait for agent response
        return "Message sent to agent";
    }

    private void RunDesktopApp(CancellationToken ct)
    {
        Console.WriteLine("[uno] Starting desktop app...");
        
        // Note: In a real implementation, this would launch the Uno Platform app
        // For now, this is a placeholder that simulates the UI lifecycle
        
        try
        {
            // The actual Uno app would be started here
            // Microsoft.UI.Xaml.Application.Start((p) => new App());
            
            // Keep alive until cancelled
            while (!ct.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[uno] desktop app error: {ex.Message}");
        }
    }
}

/// <summary>
/// Uno Platform running mode
/// </summary>
public enum UnoMode
{
    Desktop,
    WebAssembly
}
