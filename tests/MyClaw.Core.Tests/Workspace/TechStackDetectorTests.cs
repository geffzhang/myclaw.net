using MyClaw.Core.Workspace;

namespace MyClaw.Core.Tests.Workspace;

public class TechStackDetectorTests : IDisposable
{
    private readonly string _tempDir;

    public TechStackDetectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"techstack_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Detect_EmptyDirectory_ReturnsEmpty()
    {
        var result = TechStackDetector.Detect(_tempDir);

        Assert.Empty(result);
    }

    [Fact]
    public void Detect_NodeJsProject_ReturnsNodeJs()
    {
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains("Node.js", result);
    }

    [Fact]
    public void Detect_TypeScriptProject_ReturnsTypeScript()
    {
        File.WriteAllText(Path.Combine(_tempDir, "tsconfig.json"), "{}");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains("TypeScript", result);
    }

    [Fact]
    public void Detect_DotNetProject_ReturnsDotNet()
    {
        File.WriteAllText(Path.Combine(_tempDir, "MyProject.csproj"), "<Project></Project>");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains(".NET", result);
        Assert.Contains("C#", result);
    }

    [Fact]
    public void Detect_PythonProject_ReturnsPython()
    {
        File.WriteAllText(Path.Combine(_tempDir, "requirements.txt"), "requests\nnumpy");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains("Python", result);
    }

    [Fact]
    public void Detect_RustProject_ReturnsRust()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Cargo.toml"), "[package]");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains("Rust", result);
    }

    [Fact]
    public void Detect_GoProject_ReturnsGo()
    {
        File.WriteAllText(Path.Combine(_tempDir, "go.mod"), "module example.com/test");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains("Go", result);
    }

    [Fact]
    public void Detect_JavaProject_ReturnsJava()
    {
        File.WriteAllText(Path.Combine(_tempDir, "pom.xml"), "<project></project>");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains("Java", result);
        Assert.Contains("Maven", result);
    }

    [Fact]
    public void Detect_DockerProject_ReturnsDocker()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Dockerfile"), "FROM node:18");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains("Docker", result);
    }

    [Fact]
    public void Detect_MultiTechProject_ReturnsAll()
    {
        // 创建一个既有 Node.js 又有 TypeScript 还有 Docker 的项目
        File.WriteAllText(Path.Combine(_tempDir, "package.json"), "{}");
        File.WriteAllText(Path.Combine(_tempDir, "tsconfig.json"), "{}");
        File.WriteAllText(Path.Combine(_tempDir, "Dockerfile"), "FROM node:18");
        File.WriteAllText(Path.Combine(_tempDir, "docker-compose.yml"), "version: '3'");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains("Node.js", result);
        Assert.Contains("TypeScript", result);
        Assert.Contains("Docker", result);
    }

    [Fact]
    public void Detect_NonExistentDirectory_ReturnsEmpty()
    {
        var result = TechStackDetector.Detect("/nonexistent/path/xyz");

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("jest.config.js", "Jest")]
    [InlineData("vite.config.ts", "Vite")]
    [InlineData("Makefile", "Make")]
    [InlineData("CMakeLists.txt", "CMake")]
    [InlineData("Brewfile", "Homebrew")]
    public void Detect_SpecificFiles_ReturnsCorrectTech(string fileName, string expectedTech)
    {
        File.WriteAllText(Path.Combine(_tempDir, fileName), "");

        var result = TechStackDetector.Detect(_tempDir);

        Assert.Contains(expectedTech, result);
    }
}
