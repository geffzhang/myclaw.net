using System;
using System.CommandLine;
using System.Threading.Tasks;
using MyClaw.Core.Configuration;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Gateway 命令 - 启动完整网关服务
/// </summary>
public class GatewayCommand : Command
{
    public GatewayCommand() : base("gateway", "Start the full gateway (channels + cron + heartbeat)")
    {
        this.SetHandler(async () =>
        {
            var cfg = ConfigurationLoader.Load();

            if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]API key not set. Run 'myclaw onboard' or set MYCLAW_API_KEY / ANTHROPIC_API_KEY[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[blue]Starting gateway on {cfg.Gateway.Host}:{cfg.Gateway.Port}...[/]");
            
            // TODO: Start Gateway service
            await Task.Delay(100);
            
            AnsiConsole.MarkupLine("[green]Gateway started. Press Ctrl+C to stop.[/]");
            
            // Wait for shutdown signal
            await Task.Delay(-1);
        });
    }
}
