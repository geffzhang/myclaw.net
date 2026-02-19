using System.Net;
using System.Text;
using System.Text.Json;
using MyClaw.Core.Configuration;
using MyClaw.Core.Messaging;

namespace MyClaw.Channels.WebUI;

/// <summary>
/// WebUI 渠道 - 提供 HTTP API 和 WebSocket 支持
/// </summary>
public class WebUIChannel : ChannelBase
{
    private readonly WebUIConfig _config;
    private readonly MessageBus _messageBus;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly List<WebSocketContext> _clients = new();
    private readonly object _lock = new();

    public override string Name => "webui";
    public override bool IsEnabled => _config.Enabled;

    public WebUIChannel(WebUIConfig config, MessageBus messageBus) : base(config.AllowFrom)
    {
        _config = config;
        _messageBus = messageBus;
    }

    public override async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://{_config.Host}:{_config.Port}/");
        _listener.Start();

        Console.WriteLine($"[webui] listening on http://{_config.Host}:{_config.Port}");

        _ = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);
        
        await Task.CompletedTask;
    }

    public override async Task StopAsync()
    {
        _cts?.Cancel();
        
        lock (_lock)
        {
            foreach (var client in _clients)
            {
                try { client.Socket.Abort(); } catch { }
            }
            _clients.Clear();
        }

        _listener?.Stop();
        _listener?.Close();
        
        await Task.CompletedTask;
    }

    public override async Task SendAsync(OutboundMessage message, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type = "message",
            channel = message.Channel,
            chatId = message.ChatID,
            content = message.Content,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        // 广播给所有 WebSocket 客户端
        List<WebSocketContext> clients;
        lock (_lock)
        {
            clients = _clients.Where(c => c.ChatID == message.ChatID || message.ChatID == "broadcast").ToList();
        }

        var bytes = Encoding.UTF8.GetBytes(payload);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var client in clients)
        {
            try
            {
                if (client.Socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    await client.Socket.SendAsync(segment, System.Net.WebSockets.WebSocketMessageType.Text, true, ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[webui] send to client failed: {ex.Message}");
            }
        }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context, ct), ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[webui] accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            // CORS
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            var path = request.Url?.LocalPath ?? "/";

            switch (path)
            {
                case "/ws":
                    await HandleWebSocketAsync(context, ct);
                    break;
                case "/api/chat":
                    await HandleChatApiAsync(context, ct);
                    break;
                case "/api/status":
                    await HandleStatusApiAsync(context, ct);
                    break;
                case "/":
                    await HandleRootAsync(context, ct);
                    break;
                default:
                    response.StatusCode = 404;
                    response.Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[webui] request error: {ex.Message}");
            try
            {
                response.StatusCode = 500;
                response.Close();
            }
            catch { }
        }
    }

    private async Task HandleWebSocketAsync(HttpListenerContext context, CancellationToken ct)
    {
        if (!context.Request.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var wsContext = await context.AcceptWebSocketAsync(null);
        var socket = wsContext.WebSocket;
        
        var client = new WebSocketContext { Socket = socket, ChatID = "default" };
        
        lock (_lock)
        {
            _clients.Add(client);
        }

        Console.WriteLine($"[webui] websocket client connected, total: {_clients.Count}");

        try
        {
            var buffer = new byte[4096];
            while (socket.State == System.Net.WebSockets.WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                
                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleWebSocketMessageAsync(client, message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[webui] websocket error: {ex.Message}");
        }
        finally
        {
            lock (_lock)
            {
                _clients.Remove(client);
            }
            try { socket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait(); } catch { }
            Console.WriteLine($"[webui] websocket client disconnected, total: {_clients.Count}");
        }
    }

    private async Task HandleWebSocketMessageAsync(WebSocketContext client, string message)
    {
        try
        {
            var data = JsonSerializer.Deserialize<WebSocketMessage>(message);
            if (data?.Type == "chat" && !string.IsNullOrEmpty(data.Content))
            {
                // 发布到消息总线
                await _messageBus.PublishInboundAsync(new InboundMessage
                {
                    Channel = "webui",
                    ChatID = data.ChatID ?? client.ChatID,
                    SenderID = client.GetHashCode().ToString(),
                    Content = data.Content,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[webui] parse message error: {ex.Message}");
        }
    }

    private async Task HandleChatApiAsync(HttpListenerContext context, CancellationToken ct)
    {
        if (context.Request.HttpMethod != "POST")
        {
            context.Response.StatusCode = 405;
            context.Response.Close();
            return;
        }

        using var reader = new StreamReader(context.Request.InputStream);
        var body = await reader.ReadToEndAsync(ct);
        
        var request = JsonSerializer.Deserialize<ChatRequest>(body);
        if (request?.Message == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        // 检查发送者
        if (!IsAllowed(request.From ?? "anonymous"))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return;
        }

        // 发布到消息总线
        await _messageBus.PublishInboundAsync(new InboundMessage
        {
            Channel = "webui",
            ChatID = request.ChatID ?? "default",
            SenderID = request.From ?? "anonymous",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });

        var response = JsonSerializer.Serialize(new { success = true, message = "Message received" });
        var bytes = Encoding.UTF8.GetBytes(response);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    private async Task HandleStatusApiAsync(HttpListenerContext context, CancellationToken ct)
    {
        var status = new
        {
            channel = "webui",
            enabled = IsEnabled,
            clients = _clients.Count,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var response = JsonSerializer.Serialize(status);
        var bytes = Encoding.UTF8.GetBytes(response);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    private async Task HandleRootAsync(HttpListenerContext context, CancellationToken ct)
    {
        var html = @"<!DOCTYPE html>
<html>
<head>
    <title>MyClaw WebUI</title>
    <meta charset='utf-8'>
    <style>
        body { font-family: system-ui, sans-serif; max-width: 800px; margin: 50px auto; padding: 20px; }
        h1 { color: #333; }
        #messages { border: 1px solid #ddd; height: 400px; overflow-y: auto; padding: 10px; margin: 20px 0; }
        .message { margin: 10px 0; padding: 10px; border-radius: 8px; }
        .user { background: #e3f2fd; text-align: right; }
        .assistant { background: #f5f5f5; }
        #input { width: 100%; padding: 10px; box-sizing: border-box; }
        button { padding: 10px 20px; margin-top: 10px; }
        #status { color: #666; font-size: 14px; }
    </style>
</head>
<body>
    <h1>🤖 MyClaw WebUI</h1>
    <div id='status'>Connecting...</div>
    <div id='messages'></div>
    <input type='text' id='input' placeholder='Type a message...' />
    <button onclick='send()'>Send</button>
    <script>
        const ws = new WebSocket('ws://' + location.host + '/ws');
        const messages = document.getElementById('messages');
        const input = document.getElementById('input');
        const status = document.getElementById('status');
        
        ws.onopen = () => status.textContent = 'Connected';
        ws.onclose = () => status.textContent = 'Disconnected';
        ws.onmessage = (e) => {
            const data = JSON.parse(e.data);
            const div = document.createElement('div');
            div.className = 'message assistant';
            div.textContent = 'Assistant: ' + data.content;
            messages.appendChild(div);
            messages.scrollTop = messages.scrollHeight;
        };
        
        function send() {
            const text = input.value.trim();
            if (!text) return;
            ws.send(JSON.stringify({type:'chat',content:text,chatId:'web'}));
            const div = document.createElement('div');
            div.className = 'message user';
            div.textContent = 'You: ' + text;
            messages.appendChild(div);
            input.value = '';
            messages.scrollTop = messages.scrollHeight;
        }
        
        input.onkeypress = (e) => e.key === 'Enter' && send();
    </script>
</body>
</html>";

        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html";
        context.Response.StatusCode = 200;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    private class WebSocketContext
    {
        public System.Net.WebSockets.WebSocket Socket { get; set; } = null!;
        public string ChatID { get; set; } = "default";
    }

    private class WebSocketMessage
    {
        public string Type { get; set; } = "";
        public string? Content { get; set; }
        public string? ChatID { get; set; }
    }

    private class ChatRequest
    {
        public string? Message { get; set; }
        public string? ChatID { get; set; }
        public string? From { get; set; }
    }
}
