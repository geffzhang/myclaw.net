using MyClaw.Memory;

namespace MyClaw.Core.Tests.Memory;

public class MemoryStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MemoryStore _store;

    public MemoryStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"myclaw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _store = new MemoryStore(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void WriteLongTerm_NewContent_ShouldCreateFile()
    {
        _store.WriteLongTerm("Test content");

        var memoryDir = Path.Combine(_tempDir, "memory");
        var filePath = Path.Combine(memoryDir, "MEMORY.md");
        Assert.True(File.Exists(filePath));
        Assert.Equal("Test content", File.ReadAllText(filePath));
    }

    [Fact]
    public void ReadLongTerm_ExistingFile_ShouldReturnContent()
    {
        _store.WriteLongTerm("Test content");

        var content = _store.ReadLongTerm();

        Assert.Equal("Test content", content);
    }

    [Fact]
    public void ReadLongTerm_NoFile_ShouldReturnEmpty()
    {
        var content = _store.ReadLongTerm();

        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public void AppendToday_NewContent_ShouldCreateDailyFile()
    {
        _store.AppendToday("Entry 1");

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_tempDir, "memory", $"{today}.md");
        Assert.True(File.Exists(filePath));
        Assert.Contains("Entry 1", File.ReadAllText(filePath));
    }

    [Fact]
    public void AppendToday_MultipleCalls_ShouldAppendContent()
    {
        _store.AppendToday("Line 1");
        _store.AppendToday("Line 2");

        var content = _store.ReadToday();

        Assert.Contains("Line 1", content);
        Assert.Contains("Line 2", content);
    }

    [Fact]
    public void ReadToday_NoFile_ShouldReturnEmpty()
    {
        var content = _store.ReadToday();

        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public void GetRecentMemories_WithMultipleDays_ShouldReturnRecent()
    {
        // 创建几天前的文件
        var memoryDir = Path.Combine(_tempDir, "memory");
        Directory.CreateDirectory(memoryDir);
        
        File.WriteAllText(Path.Combine(memoryDir, "2024-01-01.md"), "Day 1 content");
        File.WriteAllText(Path.Combine(memoryDir, "2024-01-02.md"), "Day 2 content");
        File.WriteAllText(Path.Combine(memoryDir, "2024-01-03.md"), "Day 3 content");

        var recent = _store.GetRecentMemories(2);

        Assert.Contains("Day 2 content", recent);
        Assert.Contains("Day 3 content", recent);
    }

    [Fact]
    public void GetRecentMemories_EmptyDirectory_ShouldReturnEmpty()
    {
        var recent = _store.GetRecentMemories(7);

        Assert.Equal(string.Empty, recent);
    }

    [Fact]
    public void GetMemoryContext_WithData_ShouldReturnCombinedContext()
    {
        _store.WriteLongTerm("Long term memory");
        _store.AppendToday("Today's entry");

        var context = _store.GetMemoryContext();

        Assert.Contains("长期记忆", context);
        Assert.Contains("Long term memory", context);
        Assert.Contains("最近日志", context);
        Assert.Contains("Today's entry", context);
    }

    [Fact]
    public void GetMemoryContext_NoData_ShouldReturnEmpty()
    {
        var context = _store.GetMemoryContext();

        Assert.Equal(string.Empty, context);
    }
}
