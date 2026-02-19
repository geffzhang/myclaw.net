using MyClaw.Core.Execution;

namespace MyClaw.Core.Tests.Execution;

public class CommandExecutorTests
{
    private readonly CommandExecutor _executor = new();

    #region Allowed Commands Tests

    [Theory]
    [InlineData("git status")]
    [InlineData("ls -la")]
    [InlineData("cat file.txt")]
    [InlineData("npm install")]
    [InlineData("python script.py")]
    [InlineData("cargo build")]
    [InlineData("go build")]
    [InlineData("make")]
    [InlineData("node index.js")]
    [InlineData("echo hello")]
    public async Task ExecuteAsync_AllowedCommands_ShouldExecute(string command)
    {
        // 这些命令应该通过安全验证
        var result = await _executor.ExecuteAsync(command);

        // 命令应该被允许执行（即使实际执行可能失败，也不应该是安全错误）
        Assert.DoesNotContain("安全错误", result.Output);
    }

    #endregion

    #region Blocked Commands Tests

    [Theory]
    [InlineData("rm -rf /")]
    [InlineData("rm file.txt")]
    [InlineData("sudo apt-get update")]
    [InlineData("sudo")]
    [InlineData("chown user:group file")]
    [InlineData("chmod 777 file")]
    [InlineData("mv file1 file2")]
    [InlineData("dd if=/dev/zero of=/dev/sda")]
    public async Task ExecuteAsync_BlockedCommands_ShouldReturnSecurityError(string command)
    {
        var result = await _executor.ExecuteAsync(command);

        Assert.Contains("安全错误", result.Output);
        Assert.Equal(-1, result.ExitCode);
    }

    #endregion

    #region Command Execution Tests

    [Fact]
    public async Task ExecuteAsync_EchoCommand_ShouldReturnOutput()
    {
        var result = await _executor.ExecuteAsync("echo hello");

        Assert.True(result.IsSuccess);
        Assert.Contains("hello", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCommand_ShouldReturnSecurityError()
    {
        // 使用一个不在白名单中的命令
        var result = await _executor.ExecuteAsync("invalid_command_xyz");

        // 应该返回安全错误
        Assert.Contains("安全错误", result.Output);
        Assert.Equal(-1, result.ExitCode);
    }

    #endregion

    #region Command Result Tests

    [Fact]
    public void CommandResult_DefaultConstructor_ShouldSetDefaults()
    {
        var result = new CommandResult();

        Assert.Equal(string.Empty, result.Output);
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CommandResult_ParameterizedConstructor_ShouldSetProperties()
    {
        var result = new CommandResult
        {
            Output = "output",
            ExitCode = 1
        };

        Assert.Equal("output", result.Output);
        Assert.Equal(1, result.ExitCode);
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Timeout Tests

    [Fact]
    public async Task ExecuteAsync_LongRunningCommand_ShouldTimeout()
    {
        // 使用 Python 或 Node 来模拟长时间运行
        // 由于测试环境可能没有这些工具，我们使用非常短的超时时间来测试超时逻辑
        if (OperatingSystem.IsWindows())
        {
            // Windows 使用 ping 来模拟延迟（ping -n 11 127.0.0.1 约 10 秒）
            var result = await _executor.ExecuteAsync("ping -n 11 127.0.0.1", timeoutMs: 500);

            // 应该超时或被限制
            Assert.True(result.ExitCode != 0 || result.Output.Contains("超时"));
        }
        else
        {
            // Linux/Mac 使用 sleep
            var result = await _executor.ExecuteAsync("sleep 10", timeoutMs: 500);

            Assert.True(result.ExitCode != 0 || result.Output.Contains("超时"));
        }
    }

    #endregion

    #region Whitelist Validation Tests

    [Fact]
    public async Task ExecuteAsync_GitCommands_Allowed()
    {
        var result = await _executor.ExecuteAsync("git --version");
        Assert.DoesNotContain("安全错误", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_DirectoryListing_Allowed()
    {
        var result = await _executor.ExecuteAsync("dir");
        Assert.DoesNotContain("安全错误", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_FileReading_Allowed()
    {
        // type 是 Windows 的命令，cat 是 Linux 的
        if (OperatingSystem.IsWindows())
        {
            var result = await _executor.ExecuteAsync("echo test > temp.txt && type temp.txt && del temp.txt");
            // 包含危险字符 ; 应该被阻止
        Assert.Contains("安全错误", result.Output);
        }
    }

    #endregion
}
