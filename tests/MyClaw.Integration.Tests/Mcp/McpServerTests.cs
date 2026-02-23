using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MyClaw.Integration.Tests.Mcp;

public class McpServerTests : IClassFixture<McpTestFixture>, IAsyncLifetime
{
    private readonly McpTestFixture _fixture;
    private readonly HttpClient _client;

    public McpServerTests(McpTestFixture fixture)
    {
        _fixture = fixture;
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    #region Protocol Tests

    [Fact]
    public async Task Initialize_ShouldReturnProtocolVersion()
    {
        var response = await SendJsonRpcAsync("initialize", new { });
        
        Assert.NotNull(response.Result);
        var result = response.Result.Value;
        Assert.Equal("2024-11-05", result.GetProperty("protocolVersion").GetString());
        Assert.Equal("myclaw", result.GetProperty("serverInfo").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Ping_ShouldReturnEmptyResult()
    {
        var response = await SendJsonRpcAsync("ping", new { });
        
        Assert.NotNull(response.Result);
    }

    [Fact]
    public async Task InvalidJsonRpcVersion_ShouldReturnError()
    {
        var request = new
        {
            jsonrpc = "1.0",
            id = 1,
            method = "initialize",
            @params = new { }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var httpResponse = await _client.PostAsync($"http://localhost:{_fixture.Port}/mcp", content);
        var body = await httpResponse.Content.ReadAsStringAsync();
        var response = JsonDocument.Parse(body);

        Assert.True(response.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task UnknownMethod_ShouldReturnError()
    {
        var response = await SendJsonRpcAsync("unknown_method", new { });
        
        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error.Value.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        var response = await _client.GetAsync($"http://localhost:{_fixture.Port}/health");
        var body = await response.Content.ReadAsStringAsync();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ok", body);
    }

    [Fact]
    public async Task OptionsRequest_ShouldReturn200()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, $"http://localhost:{_fixture.Port}/mcp");
        var response = await _client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Tools List Tests

    [Fact]
    public async Task ToolsList_ShouldReturnAllCoreTools()
    {
        var response = await SendJsonRpcAsync("tools/list", new { });
        
        Assert.NotNull(response.Result);
        var tools = response.Result.Value.GetProperty("tools").EnumerateArray().ToList();
        
        var toolNames = tools.Select(t => t.GetProperty("name").GetString()).ToList();
        
        Assert.Contains("myclaw_update", toolNames);
        Assert.Contains("myclaw_note", toolNames);
        Assert.Contains("myclaw_read", toolNames);
        Assert.Contains("myclaw_archive", toolNames);
        Assert.Contains("myclaw_entity", toolNames);
        Assert.Contains("myclaw_exec", toolNames);
        Assert.Contains("myclaw_status", toolNames);
    }

    [Fact]
    public async Task ToolsList_ShouldHaveValidSchemas()
    {
        var response = await SendJsonRpcAsync("tools/list", new { });
        
        var tools = response.Result!.Value.GetProperty("tools").EnumerateArray();
        
        foreach (var tool in tools)
        {
            Assert.True(tool.TryGetProperty("name", out _));
            Assert.True(tool.TryGetProperty("description", out _));
            Assert.True(tool.TryGetProperty("inputSchema", out _));
            
            var schema = tool.GetProperty("inputSchema");
            Assert.Equal("object", schema.GetProperty("type").GetString());
        }
    }

    #endregion

    #region myclaw_update Tests

    [Fact]
    public async Task Update_ShouldCreateFile()
    {
        var response = await CallToolAsync("myclaw_update", new
        {
            filename = "SOUL.md",
            content = "# Test Soul\n\nThis is a test soul content."
        });

        var text = GetToolResultText(response);
        Assert.Contains("已更新", text);
        
        var filePath = Path.Combine(_fixture.WorkspacePath, "SOUL.md");
        Assert.True(File.Exists(filePath));
        
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("# Test Soul", content);
    }

    [Fact]
    public async Task Update_ShouldCreateBackup()
    {
        var filePath = Path.Combine(_fixture.WorkspacePath, "AGENTS.md");
        await File.WriteAllTextAsync(filePath, "Original content");
        
        await CallToolAsync("myclaw_update", new
        {
            filename = "AGENTS.md",
            content = "New content"
        });

        Assert.True(File.Exists(filePath + ".bak"));
        var backup = await File.ReadAllTextAsync(filePath + ".bak");
        Assert.Equal("Original content", backup);
    }

    [Fact]
    public async Task Update_ShouldSupportAllDnaFiles()
    {
        var dnaFiles = new[] { "AGENTS.md", "SOUL.md", "USER.md", "TOOLS.md", "IDENTITY.md", "MEMORY.md", "HEARTBEAT.md" };
        
        foreach (var file in dnaFiles)
        {
            var response = await CallToolAsync("myclaw_update", new
            {
                filename = file,
                content = $"# {file} content"
            });
            
            var text = GetToolResultText(response);
            Assert.Contains("已更新", text);
        }
    }

    #endregion

    #region myclaw_note Tests

    [Fact]
    public async Task Note_ShouldAppendToTodayLog()
    {
        var response = await CallToolAsync("myclaw_note", new
        {
            text = "Test note entry at " + DateTime.UtcNow
        });

        var text = GetToolResultText(response);
        Assert.Contains("已记录", text);
    }

    [Fact]
    public async Task Note_MultipleEntries_ShouldAppendSequentially()
    {
        await CallToolAsync("myclaw_note", new { text = "First note" });
        await CallToolAsync("myclaw_note", new { text = "Second note" });

        var response = await CallToolAsync("myclaw_read", new { mode = "full" });
        var text = GetToolResultText(response);
        
        Assert.Contains("First note", text);
        Assert.Contains("Second note", text);
    }

    #endregion

    #region myclaw_read Tests

    [Fact]
    public async Task Read_EmptyWorkspace_ShouldReturnEmpty()
    {
        var response = await CallToolAsync("myclaw_read", new { mode = "full" });
        var text = GetToolResultText(response);
        
        Assert.NotNull(text);
    }

    [Fact]
    public async Task Read_ShouldIncludeAllDnaFiles()
    {
        await CallToolAsync("myclaw_update", new { filename = "SOUL.md", content = "Soul content" });
        await CallToolAsync("myclaw_update", new { filename = "USER.md", content = "User content" });
        
        var response = await CallToolAsync("myclaw_read", new { mode = "full" });
        var text = GetToolResultText(response);
        
        Assert.Contains("SOUL.md", text);
        Assert.Contains("Soul content", text);
        Assert.Contains("USER.md", text);
        Assert.Contains("User content", text);
    }

    [Fact]
    public async Task Read_ShouldIncludeMemory()
    {
        var memoryPath = Path.Combine(_fixture.WorkspacePath, "MEMORY.md");
        await File.WriteAllTextAsync(memoryPath, "Long term memory content");
        
        var response = await CallToolAsync("myclaw_read", new { mode = "full" });
        var text = GetToolResultText(response);
        
        Assert.Contains("MEMORY.md", text);
        Assert.Contains("Long term memory content", text);
    }

    [Fact]
    public async Task Read_ShouldIncludeTodayLog()
    {
        await CallToolAsync("myclaw_note", new { text = "Today's activity" });
        
        var response = await CallToolAsync("myclaw_read", new { mode = "full" });
        var text = GetToolResultText(response);
        
        Assert.Contains("Today's activity", text);
    }

    #endregion

    #region myclaw_archive Tests

    [Fact]
    public async Task Archive_NoLog_ShouldReturnNoLogMessage()
    {
        var response = await CallToolAsync("myclaw_archive", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("没有可归档", text);
    }

    [Fact]
    public async Task Archive_WithLog_ShouldArchiveSuccessfully()
    {
        await CallToolAsync("myclaw_note", new { text = "Note to archive" });
        
        var response = await CallToolAsync("myclaw_archive", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("已归档", text);
    }

    #endregion

    #region myclaw_entity Tests

    [Fact]
    public async Task Entity_Add_ShouldCreateEntity()
    {
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "add",
            name = "TestProject",
            type = "project",
            attributes = new { language = "C#", framework = ".NET 9" }
        });

        var text = GetToolResultText(response);
        Assert.Contains("TestProject", text);
        Assert.Contains("Project", text);
    }

    [Fact]
    public async Task Entity_AddAllTypes_ShouldSucceed()
    {
        var types = new[] { "person", "project", "tool", "concept", "place", "other" };
        
        foreach (var type in types)
        {
            var entityName = $"Test_{type}_Entity";
            var response = await CallToolAsync("myclaw_entity", new
            {
                action = "add",
                name = entityName,
                type = type
            });
            
            var text = GetToolResultText(response);
            Assert.Contains(entityName, text);
        }
    }

    [Fact]
    public async Task Entity_Query_ShouldReturnEntity()
    {
        await CallToolAsync("myclaw_entity", new
        {
            action = "add",
            name = "QueryTest",
            type = "concept",
            attributes = new { importance = "high" }
        });
        
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "query",
            name = "QueryTest"
        });
        
        var text = GetToolResultText(response);
        Assert.Contains("QueryTest", text);
        Assert.Contains("Concept", text);
    }

    [Fact]
    public async Task Entity_QueryNonExistent_ShouldReturnNotFound()
    {
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "query",
            name = "NonExistent"
        });
        
        var text = GetToolResultText(response);
        Assert.Contains("不存在", text);
    }

    [Fact]
    public async Task Entity_List_ShouldReturnAllEntities()
    {
        await CallToolAsync("myclaw_entity", new { action = "add", name = "ListTest1", type = "project" });
        await CallToolAsync("myclaw_entity", new { action = "add", name = "ListTest2", type = "person" });
        
        var response = await CallToolAsync("myclaw_entity", new { action = "list" });
        var text = GetToolResultText(response);
        
        Assert.Contains("Entities", text);
    }

    [Fact]
    public async Task Entity_Link_ShouldAddRelation()
    {
        await CallToolAsync("myclaw_entity", new { action = "add", name = "LinkTest1", type = "person" });
        
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "link",
            name = "LinkTest1",
            relation = "works_on:myclaw"
        });
        
        var text = GetToolResultText(response);
        Assert.Contains("已关联", text);
    }

    [Fact]
    public async Task Entity_Remove_ShouldDeleteEntity()
    {
        await CallToolAsync("myclaw_entity", new { action = "add", name = "ToRemove", type = "tool" });
        
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "remove",
            name = "ToRemove"
        });
        
        var text = GetToolResultText(response);
        Assert.Contains("已删除", text);
    }

    #endregion

    #region myclaw_exec Tests

    [Fact]
    public async Task Exec_EchoCommand_ShouldReturnOutput()
    {
        var response = await CallToolAsync("myclaw_exec", new
        {
            command = "echo Hello World"
        });

        var text = GetToolResultText(response);
        Assert.Contains("Hello World", text);
    }

    [Fact]
    public async Task Exec_ValidCommand_ShouldSucceed()
    {
        if (OperatingSystem.IsWindows())
        {
            var response = await CallToolAsync("myclaw_exec", new { command = "dir" });
            var text = GetToolResultText(response);
            Assert.DoesNotContain("错误", text);
        }
        else
        {
            var response = await CallToolAsync("myclaw_exec", new { command = "ls" });
            var text = GetToolResultText(response);
            Assert.DoesNotContain("错误", text);
        }
    }

    [Fact]
    public async Task Exec_InvalidCommand_ShouldReturnError()
    {
        var response = await CallToolAsync("myclaw_exec", new
        {
            command = "nonexistent_command_12345"
        });

        var text = GetToolResultText(response);
        Assert.Contains("错误", text);
    }

    #endregion

    #region myclaw_status Tests

    [Fact]
    public async Task Status_ShouldReturnStatusInfo()
    {
        var response = await CallToolAsync("myclaw_status", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("MyClaw Status", text);
    }

    [Fact]
    public async Task Status_AfterAddingEntity_ShouldReflectCount()
    {
        await CallToolAsync("myclaw_entity", new { action = "add", name = "StatusTest", type = "project" });
        
        var response = await CallToolAsync("myclaw_status", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("MyClaw Status", text);
    }

    #endregion

    #region Resources Tests

    [Fact]
    public async Task ResourcesList_ShouldReturnAllResources()
    {
        var response = await SendJsonRpcAsync("resources/list", new { });
        
        Assert.NotNull(response.Result);
        var resources = response.Result.Value.GetProperty("resources").EnumerateArray().ToList();
        
        var uris = resources.Select(r => r.GetProperty("uri").GetString()).ToList();
        
        Assert.Contains("myclaw://context", uris);
        Assert.Contains("myclaw://skills", uris);
        Assert.Contains("myclaw://status", uris);
    }

    [Fact]
    public async Task ResourcesRead_Context_ShouldReturnContent()
    {
        await CallToolAsync("myclaw_update", new { filename = "SOUL.md", content = "Context test" });
        
        var response = await SendJsonRpcAsync("resources/read", new { uri = "myclaw://context" });
        
        Assert.NotNull(response.Result);
        var contents = response.Result.Value.GetProperty("contents").EnumerateArray().First();
        var text = contents.GetProperty("text").GetString();
        
        Assert.Contains("Context test", text);
    }

    [Fact]
    public async Task ResourcesRead_Skills_ShouldReturnSkillList()
    {
        var response = await SendJsonRpcAsync("resources/read", new { uri = "myclaw://skills" });
        
        Assert.NotNull(response.Result);
        var contents = response.Result.Value.GetProperty("contents").EnumerateArray().First();
        Assert.Equal("myclaw://skills", contents.GetProperty("uri").GetString());
    }

    [Fact]
    public async Task ResourcesRead_Status_ShouldReturnStatus()
    {
        var response = await SendJsonRpcAsync("resources/read", new { uri = "myclaw://status" });
        
        Assert.NotNull(response.Result);
        var contents = response.Result.Value.GetProperty("contents").EnumerateArray().First();
        var text = contents.GetProperty("text").GetString();
        
        Assert.NotNull(text);
    }

    [Fact]
    public async Task ResourcesRead_UnknownUri_ShouldReturnUnknownResource()
    {
        var response = await SendJsonRpcAsync("resources/read", new { uri = "myclaw://unknown" });
        
        var contents = response.Result!.Value.GetProperty("contents").EnumerateArray().First();
        var text = contents.GetProperty("text").GetString();
        
        Assert.Contains("未知资源", text);
    }

    [Fact]
    public async Task ResourceTemplatesList_ShouldReturnEmpty()
    {
        var response = await SendJsonRpcAsync("resources/templates/list", new { });
        
        Assert.NotNull(response.Result);
    }

    #endregion

    #region Prompts Tests

    [Fact]
    public async Task PromptsList_ShouldReturnAllPrompts()
    {
        var response = await SendJsonRpcAsync("prompts/list", new { });
        
        Assert.NotNull(response.Result);
        var prompts = response.Result.Value.GetProperty("prompts").EnumerateArray().ToList();
        
        var names = prompts.Select(p => p.GetProperty("name").GetString()).ToList();
        
        Assert.Contains("myclaw_wakeup", names);
        Assert.Contains("myclaw_growup", names);
        Assert.Contains("myclaw_briefing", names);
    }

    [Fact]
    public async Task PromptsGet_Wakeup_ShouldReturnWakeupMessage()
    {
        var response = await SendJsonRpcAsync("prompts/get", new { name = "myclaw_wakeup" });
        
        Assert.NotNull(response.Result);
        var messages = response.Result.Value.GetProperty("messages").EnumerateArray().ToList();
        
        Assert.NotEmpty(messages);
        var content = messages[0].GetProperty("content").GetProperty("text").GetString();
        Assert.Contains("唤醒", content);
    }

    [Fact]
    public async Task PromptsGet_Growup_ShouldReturnGrowupMessage()
    {
        var response = await SendJsonRpcAsync("prompts/get", new { name = "myclaw_growup" });
        
        Assert.NotNull(response.Result);
        var messages = response.Result.Value.GetProperty("messages").EnumerateArray().ToList();
        Assert.NotEmpty(messages);
        
        var content = messages[0].GetProperty("content").GetProperty("text").GetString();
        Assert.Contains("记忆蒸馏", content);
    }

    [Fact]
    public async Task PromptsGet_Briefing_ShouldReturnBriefingMessage()
    {
        var response = await SendJsonRpcAsync("prompts/get", new { name = "myclaw_briefing" });
        
        Assert.NotNull(response.Result);
        var messages = response.Result.Value.GetProperty("messages").EnumerateArray().ToList();
        Assert.NotEmpty(messages);
        
        var content = messages[0].GetProperty("content").GetProperty("text").GetString();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task PromptsGet_UnknownName_ShouldReturnEmptyMessages()
    {
        var response = await SendJsonRpcAsync("prompts/get", new { name = "unknown_prompt" });
        
        var messages = response.Result!.Value.GetProperty("messages").EnumerateArray().ToList();
        Assert.Empty(messages);
    }

    #endregion

    #region Unknown Tool Tests

    [Fact]
    public async Task CallUnknownTool_ShouldReturnError()
    {
        var response = await CallToolAsync("unknown_tool", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("未知工具", text);
    }

    #endregion

    #region Helper Methods

    private async Task<JsonRpcResponse> SendJsonRpcAsync(string method, object? Params)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method,
            @params = Params
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var httpResponse = await _client.PostAsync($"http://localhost:{_fixture.Port}/mcp", content);
        var body = await httpResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        return new JsonRpcResponse
        {
            JsonRpc = doc.RootElement.GetProperty("jsonrpc").GetString()!,
            Id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null,
            Result = doc.RootElement.TryGetProperty("result", out var resultEl) ? resultEl : null,
            Error = doc.RootElement.TryGetProperty("error", out var errorEl) ? errorEl : null
        };
    }

    private async Task<JsonRpcResponse> CallToolAsync(string toolName, object arguments)
    {
        return await SendJsonRpcAsync("tools/call", new
        {
            name = toolName,
            arguments
        });
    }

    private static string GetToolResultText(JsonRpcResponse response)
    {
        if (response.Result == null) return string.Empty;
        
        var content = response.Result.Value.GetProperty("content").EnumerateArray().First();
        return content.GetProperty("text").GetString() ?? string.Empty;
    }

    private class JsonRpcResponse
    {
        public string JsonRpc { get; set; } = string.Empty;
        public string? Id { get; set; }
        public JsonElement? Result { get; set; }
        public JsonElement? Error { get; set; }
    }

    #endregion
}
