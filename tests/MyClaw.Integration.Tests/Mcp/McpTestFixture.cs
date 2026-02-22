using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MyClaw.MCP;

namespace MyClaw.Integration.Tests.Mcp;

public class McpTestFixture : IAsyncLifetime
{
    public int Port { get; }
    public string WorkspacePath { get; }
    private McpServer? _server;

    public McpTestFixture()
    {
        Port = 29500 + Random.Shared.Next(1000);
        WorkspacePath = Path.Combine(Path.GetTempPath(), $"myclaw_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(WorkspacePath);
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "memory"));
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "skills"));
    }

    public async Task InitializeAsync()
    {
        _server = new McpServer(Port, WorkspacePath);
        await _server.StartAsync();
        
        await Task.Delay(100);
    }

    public async Task DisposeAsync()
    {
        if (_server != null)
        {
            await _server.StopAsync();
        }
        
        try
        {
            if (Directory.Exists(WorkspacePath))
            {
                Directory.Delete(WorkspacePath, true);
            }
        }
        catch
        {
        }
    }
}
