using MyClaw.Core.Workspace;

namespace MyClaw.Core.Tests.Workspace;

public class WorkspaceContextServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly WorkspaceContextService _service;

    public WorkspaceContextServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"workspace_ctx_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _service = new WorkspaceContextService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task GetContextSectionAsync_ReturnsWorkspaceSection()
    {
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");

        var section = await _service.GetContextSectionAsync();

        Assert.Equal("workspace", section.Name);
        Assert.Equal(6, section.Priority);
        Assert.Contains(Path.GetFileName(_tempDir), section.Content);
        Assert.Contains("Node.js", section.Content);
    }

    [Fact]
    public void GetQuickContextSection_ReturnsSectionWithoutGit()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Cargo.toml"), "[package]");

        var section = _service.GetQuickContextSection();

        Assert.Equal("workspace", section.Name);
        Assert.Contains("Rust", section.Content);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsSummary()
    {
        File.WriteAllText(Path.Combine(_tempDir, "go.mod"), "module test");

        var summary = await _service.GetSummaryAsync();

        Assert.NotNull(summary);
        Assert.Contains(Path.GetFileName(_tempDir), summary);
        Assert.Contains("Go", summary);
    }

    [Fact]
    public async Task GetPrimaryTechStackAsync_ReturnsMainTechs()
    {
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
        File.WriteAllText(Path.Combine(_tempDir, "Dockerfile"), "FROM node");

        var techs = await _service.GetPrimaryTechStackAsync();

        Assert.Contains("Node.js", techs);
        // Docker 是通用技术，不应作为主要技术栈
        Assert.DoesNotContain("Docker", techs);
    }

    [Fact]
    public void InvalidateCache_ClearsCache()
    {
        _service.InvalidateCache();
        // 这个方法只是确保不抛出异常
        Assert.True(true);
    }

    [Fact]
    public async Task GetWorkspaceInfoAsync_CachesResult()
    {
        // 第一次调用
        var info1 = await _service.GetWorkspaceInfoAsync();
        var detectTime1 = info1.DetectedAt;

        // 立即第二次调用，应该返回缓存
        var info2 = await _service.GetWorkspaceInfoAsync();
        var detectTime2 = info2.DetectedAt;

        Assert.Equal(detectTime1, detectTime2);
    }
}
