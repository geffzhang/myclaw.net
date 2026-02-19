using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using MyClaw.Core.Configuration;
using MyClaw.Gateway;
using MyClaw.MCP;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Gateway 命令 - 启动完整网关服务（包含MCP服务）
/// </summary>
public class GatewayCommand : Command
{
    public GatewayCommand() : base("gateway", "Start the full gateway (channels + cron + heartbeat + mcp)")
    {
        // MCP 端口参数，默认 2334
        var mcpPortOption = new Option<int>(
            aliases: new[] { "--mcpport" },
            description: "MCP service port (default: 2334)",
            getDefaultValue: () => 2334);

        AddOption(mcpPortOption);

        this.SetHandler(async (int mcpPort) =>
        {
            var cfg = ConfigurationLoader.Load();

            if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]API key not set. Run 'myclaw onboard' or set MYCLAW_API_KEY / ANTHROPIC_API_KEY[/]");
                return;
            }

            var cts = new CancellationTokenSource();
            
            // Handle Ctrl+C
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Start MCP Service
            AnsiConsole.MarkupLine($"[blue]Starting MCP service on port {mcpPort}...[/]");
            var mcpServer = new McpServer(mcpPort);
            await mcpServer.StartAsync();
            AnsiConsole.MarkupLine($"[green]✓ MCP service started on http://localhost:{mcpPort}[/]");

            // Start Gateway
            AnsiConsole.MarkupLine($"[blue]Starting gateway on {cfg.Gateway.Host}:{cfg.Gateway.Port}...[/]");
            
            try
            {
                var gateway = new GatewayService(cfg);
                _ = Task.Run(() => gateway.StartAsync(cts.Token), cts.Token);
                
                AnsiConsole.MarkupLine($"[green]✓ Gateway started on {cfg.Gateway.Host}:{cfg.Gateway.Port}[/]");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[blue]Services running:[/]");
                AnsiConsole.MarkupLine($"  • MCP Server:   http://localhost:{mcpPort}");
                AnsiConsole.MarkupLine($"  • Gateway:      {cfg.Gateway.Host}:{cfg.Gateway.Port}");
                AnsiConsole.MarkupLine($"  • WebUI:        http://localhost:{cfg.Channels?.WebUI?.Port ?? 8080} (if enabled)");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[yellow]Press Ctrl+C to stop all services.[/]");
                
                // Wait for shutdown signal
                await Task.Delay(-1, cts.Token);
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("\n[yellow]Shutting down services...[/]");
                await mcpServer.StopAsync();
                AnsiConsole.MarkupLine("[green]✓ Services stopped.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Gateway error: {ex.Message}[/]");
                await mcpServer.StopAsync();
            }
        }, mcpPortOption);
    }
}
