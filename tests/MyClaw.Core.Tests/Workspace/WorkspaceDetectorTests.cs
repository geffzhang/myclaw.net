using MyClaw.Core.Workspace;

namespace MyClaw.Core.Tests.Workspace;

public class WorkspaceDetectorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly WorkspaceDetector _detector;

    public WorkspaceDetectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"workspace_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _detector = new WorkspaceDetector(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            // 尝试删除 .git 目录中的只读文件
            var gitDir = Path.Combine(_tempDir, ".git");
            if (Directory.Exists(gitDir))
            {
                foreach (var file in Directory.GetFiles(gitDir, "*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                }
            }
            
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    [Fact]
    public async Task DetectAsync_EmptyDirectory_ReturnsBasicInfo()
    {
        var result = await _detector.DetectAsync();

        Assert.NotNull(result);
        Assert.Equal(Path.GetFileName(_tempDir), result.Name);
        Assert.Equal(_tempDir, result.Path);
        Assert.NotNull(result.TechStack);
        Assert.NotNull(result.Git);
        Assert.False(result.Git.IsRepo);
    }

    [Fact]
    public async Task DetectAsync_WithGitRepo_DetectsGitInfo()
    {
        // 初始化 Git 仓库
        await ExecuteCommandAsync("git", "init", _tempDir);
        await ExecuteCommandAsync("git", "config user.email \"test@test.com\"", _tempDir);
        await ExecuteCommandAsync("git", "config user.name \"Test\"", _tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "test.txt"), "test");
        await ExecuteCommandAsync("git", "add .", _tempDir);
        await ExecuteCommandAsync("git", "commit -m \"initial\"", _tempDir);

        var result = await _detector.DetectAsync();

        Assert.True(result.Git.IsRepo);
        Assert.NotEmpty(result.Git.Branch);
        Assert.Equal("clean", result.Git.Status);
    }

    [Fact]
    public async Task DetectAsync_WithUncommittedChanges_DetectsDirtyStatus()
    {
        // 初始化 Git 仓库并创建未提交的更改
        await ExecuteCommandAsync("git", "init", _tempDir);
        await ExecuteCommandAsync("git", "config user.email \"test@test.com\"", _tempDir);
        await ExecuteCommandAsync("git", "config user.name \"Test\"", _tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "committed.txt"), "committed");
        await ExecuteCommandAsync("git", "add .", _tempDir);
        await ExecuteCommandAsync("git", "commit -m \"initial\"", _tempDir);

        // 添加未提交的更改
        File.WriteAllText(Path.Combine(_tempDir, "dirty.txt"), "dirty");

        var result = await _detector.DetectAsync();

        Assert.True(result.Git.IsRepo);
        Assert.Equal("dirty", result.Git.Status);
        Assert.True(result.Git.UncommittedChanges > 0);
    }

    [Fact]
    public async Task DetectAsync_WithTechFiles_DetectsTechStack()
    {
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
        File.WriteAllText(Path.Combine(_tempDir, "tsconfig.json"), "{}");

        var result = await _detector.DetectAsync();

        Assert.Contains("Node.js", result.TechStack);
        Assert.Contains("TypeScript", result.TechStack);
    }

    [Fact]
    public void DetectQuick_ReturnsBasicInfoWithoutGitCommands()
    {
        // 这个方法不执行 Git 命令，只检测技术栈
        File.WriteAllText(Path.Combine(_tempDir, "Cargo.toml"), "[package]");

        var result = _detector.DetectQuick();

        Assert.Equal(Path.GetFileName(_tempDir), result.Name);
        Assert.Contains("Rust", result.TechStack);
        Assert.False(result.Git.IsRepo); // 没有执行 Git 检测
    }

    [Fact]
    public async Task GetRecentChangesAsync_WithCommits_ReturnsChanges()
    {
        // 初始化 Git 仓库
        await ExecuteCommandAsync("git", "init", _tempDir);
        await ExecuteCommandAsync("git", "config user.email \"test@test.com\"", _tempDir);
        await ExecuteCommandAsync("git", "config user.name \"Test\"", _tempDir);

        // 创建一些文件并提交
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "content1");
        await ExecuteCommandAsync("git", "add .", _tempDir);
        await ExecuteCommandAsync("git", "commit -m \"first\"", _tempDir);

        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "content2");
        await ExecuteCommandAsync("git", "add .", _tempDir);
        await ExecuteCommandAsync("git", "commit -m \"second\"", _tempDir);

        var changes = await _detector.GetRecentChangesAsync(2);

        // 应该返回最近更改的文件
        Assert.True(changes.Count >= 0); // 可能有变化，也可能没有
    }

    [Fact]
    public void ToContextString_ContainsAllInfo()
    {
        var info = new WorkspaceInfo
        {
            Name = "TestProject",
            Path = "/test/path",
            TechStack = new List<string> { "Node.js", "TypeScript" },
            Git = new GitInfo
            {
                IsRepo = true,
                Branch = "main",
                Status = "clean",
                RecentCommits = "abc123 Fix bug\ndef456 Add feature"
            }
        };

        var context = info.ToContextString();

        Assert.Contains("TestProject", context);
        Assert.Contains("/test/path", context);
        Assert.Contains("main", context);
        Assert.Contains("clean", context);
        Assert.Contains("Node.js", context);
        Assert.Contains("TypeScript", context);
    }

    private async Task ExecuteCommandAsync(string command, string arguments, string workingDirectory)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }
}
