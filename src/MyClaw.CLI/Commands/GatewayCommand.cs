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
    public GatewayCommand() : base("gateway", "启动完整网关（渠道 + 定时任务 + 心跳 + MCP）")
    {
        // MCP 端口参数，默认 2334
        var mcpPortOption = new Option<int>(
            aliases: new[] { "--mcpport" },
            description: "MCP 服务端口（默认: 2334）",
            getDefaultValue: () => 2334);

        AddOption(mcpPortOption);

        this.SetHandler(async (int mcpPort) =>
        {
            var cfg = ConfigurationLoader.Load();

            if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]API 密钥未设置。请运行 'myclaw onboard' 或设置 MYCLAW_API_KEY / ANTHROPIC_API_KEY[/]");
                return;
            }

            var cts = new CancellationTokenSource();
            
            // 处理 Ctrl+C
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // 启动 MCP 服务
            AnsiConsole.MarkupLine($"[blue]正在端口 {mcpPort} 启动 MCP 服务...[/]");
            var mcpServer = new McpServer(mcpPort);
            await mcpServer.StartAsync();
            AnsiConsole.MarkupLine($"[green]✓ MCP 服务已启动 http://localhost:{mcpPort}[/]");

            // 启动网关
            AnsiConsole.MarkupLine($"[blue]正在 {cfg.Gateway.Host}:{cfg.Gateway.Port} 启动网关...[/]");
            
            try
            {
                var gateway = new GatewayService(cfg);
                _ = Task.Run(() => gateway.StartAsync(cts.Token), cts.Token);
                
                AnsiConsole.MarkupLine($"[green]✓ 网关已启动 {cfg.Gateway.Host}:{cfg.Gateway.Port}[/]");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[blue]运行中的服务:[/]");
                AnsiConsole.MarkupLine($"  • MCP 服务器:   http://localhost:{mcpPort}");
                AnsiConsole.MarkupLine($"  • 网关:         {cfg.Gateway.Host}:{cfg.Gateway.Port}");
                AnsiConsole.MarkupLine($"  • WebUI:        http://localhost:{cfg.Channels?.WebUI?.Port ?? 8080} (如已启用)");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[yellow]按 Ctrl+C 停止所有服务[/]");
                
                // 等待关闭信号
                await Task.Delay(-1, cts.Token);
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("\n[yellow]正在关闭服务...[/]");
                await mcpServer.StopAsync();
                AnsiConsole.MarkupLine("[green]✓ 服务已停止[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]网关错误: {ex.Message}[/]");
                await mcpServer.StopAsync();
            }
        }, mcpPortOption);
    }
}
