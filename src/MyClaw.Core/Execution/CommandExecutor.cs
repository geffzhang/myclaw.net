using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MyClaw.Core.Execution;

public class CommandResult
{
    /// <summary>
    /// 输出内容
    /// </summary>
    public string Output { get; set; } = string.Empty;
    /// <summary>
    /// 退出码
    /// </summary>
    public int ExitCode { get; set; }
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
}

public class CommandExecutor
{
    private readonly ILogger<CommandExecutor>? _logger;

    public CommandExecutor(ILogger<CommandExecutor>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 允许的命令白名单
    /// </summary>
    private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        // 文件操作
        "ls", "cat", "find", "grep", "head", "tail", "wc", "dir", "type",
        // Git 操作
        "git",
        // 环境检查
        "echo", "date", "uname", "which", "pwd", "ps", "whoami", "hostname",
        // 包管理
        "npm", "node", "pnpm", "yarn", "npx",
        "python", "python3", "pip", "pip3",
        "cargo", "rustc",
        "go", "golang",
        "dotnet", "nuget","dnx",
        // 构建工具
        "make", "cmake", "msbuild",
        // 其他
        "tree", "du", "df", "curl", "wget"
    };

    /// <summary>
    /// 命令黑名单
    /// </summary>
    private static readonly HashSet<string> BlockedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm", "del", "rd", "rmdir",
        "sudo", "su",
        "chown", "chmod", "chgrp",
        "mv", "move",
        "dd", "mkfs", "fdisk", "format",
        "shutdown", "reboot", "halt",
        "kill", "pkill", "killall",
        ">:", ">>", "|", "&", ";"
    };

    /// <summary>
    /// 执行命令（带安全检查）
    /// </summary>
    /// <param name="command"></param>
    /// <param name="timeoutMs"></param>
    /// <returns></returns>
    public async Task<CommandResult> ExecuteAsync(string command, int timeoutMs = 10000)
    {
        var validation = ValidateCommand(command);
        if (!validation.IsValid)
        {
            // 日志记录被拒绝的命令
            _logger?.LogWarning("命令被拒绝: {Command}，原因: {Reason}", command, validation.ErrorMessage);

            return new CommandResult
            {
                Output = $"安全错误: {validation.ErrorMessage}",
                ExitCode = -1
            };
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = GetShell(),
                Arguments = GetShellArguments(command),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = await Task.Run(() =>
                process.WaitForExit(timeoutMs));

            if (!completed)
            {
                try { process.Kill(); } catch { }
                return new CommandResult
                {
                    Output = "错误: 命令超时 (10秒限制)",
                    ExitCode = -1
                };
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            // 限制输出大小 (1MB)
            const int maxOutput = 1024 * 1024;
            if (output.Length > maxOutput)
            {
                output = output.Substring(0, maxOutput) +
                    "\n... [输出已截断，超过 1MB 限制]";
            }

            return new CommandResult
            {
                Output = output + (string.IsNullOrEmpty(error) ? "" : $"\n[stderr]: {error}"),
                ExitCode = process.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Output = $"执行错误: {ex.Message}",
                ExitCode = -1
            };
        }
    }

    /// <summary>
    /// 验证命令安全性
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    private (bool IsValid, string ErrorMessage) ValidateCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            _logger?.LogWarning("命令被拒绝: 命令为空");
            return (false, "命令为空");
        }

        // 提取第一个token（主命令）
        var firstToken = command.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;

        // 检查是否包含路径
        if (firstToken.Contains('/') || firstToken.Contains('\\'))
        {
            // 提取命令名
            var cmdName = Path.GetFileName(firstToken);
            if (!AllowedCommands.Contains(cmdName))
            {
                _logger?.LogWarning("命令被拒绝: {Command}，原因: 命令 '{CmdName}' 不在允许的白名单中", command, cmdName);
                return (false, $"命令 '{cmdName}' 不在允许的白名单中");
            }
        }
        else
        {
            if (!AllowedCommands.Contains(firstToken))
            {
                _logger?.LogWarning("命令被拒绝: {Command}，原因: 命令 '{FirstToken}' 不在允许的白名单中", command, firstToken);
                return (false, $"命令 '{firstToken}' 不在允许的白名单中");
            }
        }

        // 检查危险字符
        if (BlockedCommands.Any(bc => command.Contains(bc)))
        {
            _logger?.LogWarning("命令被拒绝: {Command}，原因: 命令包含潜在危险字符/模式", command);
            return (false, "命令包含潜在危险字符/模式");
        }

        return (true, string.Empty);
    }

    private string GetShell()
    {
        if (OperatingSystem.IsWindows())
        {
            return "cmd.exe";
        }
        return "/bin/bash";
    }

    private string GetShellArguments(string command)
    {
        if (OperatingSystem.IsWindows())
        {
            return $"/c {command}";
        }
        return $"-c \"{command}\"";
    }
}
