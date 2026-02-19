using System.Net;
using System.Text;
using System.Text.Json;
using MyClaw.Core.Configuration;
using MyClaw.Core.Entities;
using MyClaw.Core.Evolution;
using MyClaw.Core.Execution;
using MyClaw.Core.Memory;
using MyClaw.Skills;

namespace MyClaw.MCP;

/// <summary>
/// MCP Server - Model Context Protocol over HTTP
/// </summary>
public class McpServer
{
    private readonly int _port;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    private MemoryStore _memoryStore = null!;
    private EntityStore _entityStore = null!;
    private SkillManager _skillManager = null!;
    private CommandExecutor _commandExecutor = null!;
    private SignalDetector _signalDetector = null!;

    public McpServer(int port)
    {
        _port = port;
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();

        // Initialize components
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var workspace = Path.Combine(home, ".myclaw", "workspace");
        Directory.CreateDirectory(workspace);

        _memoryStore = new MemoryStore(workspace);
        _entityStore = new EntityStore(workspace);
        _skillManager = new SkillManager(workspace);
        _skillManager.LoadSkills();
        _commandExecutor = new CommandExecutor();
        _signalDetector = new SignalDetector();

        await _entityStore.LoadAsync();

        // Start HTTP server
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();

        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        await Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP] Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // CORS
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 200;
            response.Close();
            return;
        }

        var path = request.Url?.AbsolutePath ?? "/";

        try
        {
            switch (path)
            {
                case "/mcp/v1/initialize":
                    await HandleInitializeAsync(response);
                    break;
                case "/mcp/v1/tools/list":
                    await HandleListToolsAsync(response);
                    break;
                case "/mcp/v1/tools/call":
                    await HandleCallToolAsync(request, response);
                    break;
                case "/mcp/v1/resources/list":
                    await HandleListResourcesAsync(response);
                    break;
                case "/mcp/v1/resources/read":
                    await HandleReadResourceAsync(request, response);
                    break;
                case "/mcp/v1/prompts/list":
                    await HandleListPromptsAsync(response);
                    break;
                case "/mcp/v1/prompts/get":
                    await HandleGetPromptAsync(request, response);
                    break;
                case "/sse":
                    await HandleSseAsync(response);
                    break;
                default:
                    response.StatusCode = 404;
                    await WriteJsonAsync(response, new { error = "Not found" });
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MCP] Error: {ex.Message}");
            response.StatusCode = 500;
            await WriteJsonAsync(response, new { error = ex.Message });
        }
    }

    private async Task HandleInitializeAsync(HttpListenerResponse response)
    {
        var result = new
        {
            protocolVersion = "2024-11-05",
            serverInfo = new { name = "myclaw", version = "1.0.0" },
            capabilities = new
            {
                tools = new { },
                resources = new { },
                prompts = new { }
            }
        };
        await WriteJsonAsync(response, result);
    }

    private async Task HandleListToolsAsync(HttpListenerResponse response)
    {
        var tools = new List<object>
        {
            new
            {
                name = "miniclaw_update",
                description = "【本能：神经重塑】修改核心认知文件",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        filename = new { type = "string", @enum = new[] { "AGENTS.md", "SOUL.md", "USER.md", "TOOLS.md", "IDENTITY.md", "MEMORY.md", "HEARTBEAT.md" } },
                        content = new { type = "string" }
                    },
                    required = new[] { "filename", "content" }
                }
            },
            new
            {
                name = "miniclaw_note",
                description = "【本能：海马体写入】追加今日日志",
                inputSchema = new
                {
                    type = "object",
                    properties = new { text = new { type = "string" } },
                    required = new[] { "text" }
                }
            },
            new
            {
                name = "miniclaw_read",
                description = "【本能：全脑唤醒】读取上下文和记忆",
                inputSchema = new
                {
                    type = "object",
                    properties = new { mode = new { type = "string", @enum = new[] { "full", "minimal" } } }
                }
            },
            new
            {
                name = "miniclaw_archive",
                description = "【日志归档】归档今日日志",
                inputSchema = new { type = "object" }
            },
            new
            {
                name = "miniclaw_entity",
                description = "【本能：概念连接】管理实体知识图谱",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        action = new { type = "string", @enum = new[] { "add", "remove", "link", "query", "list" } },
                        name = new { type = "string" },
                        type = new { type = "string", @enum = new[] { "person", "project", "tool", "concept", "place", "other" } },
                        attributes = new { type = "object" },
                        relation = new { type = "string" }
                    },
                    required = new[] { "action" }
                }
            },
            new
            {
                name = "miniclaw_exec",
                description = "【本能：感官与手】安全执行终端命令",
                inputSchema = new
                {
                    type = "object",
                    properties = new { command = new { type = "string" } },
                    required = new[] { "command" }
                }
            },
            new
            {
                name = "miniclaw_status",
                description = "【系统诊断】返回完整状态",
                inputSchema = new { type = "object" }
            }
        };

        // Add skill tools
        foreach (var skill in _skillManager.LoadedSkills)
        {
            tools.Add(new
            {
                name = $"skill_{skill.Name}",
                description = $"【Skill: {skill.Name}】{skill.Description}",
                inputSchema = new { type = "object" }
            });
        }

        await WriteJsonAsync(response, new { tools });
    }

    private async Task HandleCallToolAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        var body = await ReadBodyAsync(request);
        var call = JsonSerializer.Deserialize<ToolCallRequest>(body);

        if (call == null)
        {
            response.StatusCode = 400;
            await WriteJsonAsync(response, new { error = "Invalid request" });
            return;
        }

        var result = await ExecuteToolAsync(call.name, call.arguments);
        await WriteJsonAsync(response, new { content = new[] { new { type = "text", text = result } } });
    }

    private async Task<string> ExecuteToolAsync(string name, Dictionary<string, object>? args)
    {
        args ??= new Dictionary<string, object>();

        try
        {
            return name switch
            {
                "miniclaw_update" => await ToolUpdateAsync(args),
                "miniclaw_note" => ToolNote(args),
                "miniclaw_read" => ToolRead(args),
                "miniclaw_archive" => ToolArchive(),
                "miniclaw_entity" => await ToolEntityAsync(args),
                "miniclaw_exec" => await ToolExecAsync(args),
                "miniclaw_status" => ToolStatus(),
                _ => name.StartsWith("skill_") ? await ToolSkillAsync(name, args) : $"Unknown tool: {name}"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> ToolUpdateAsync(Dictionary<string, object> args)
    {
        var filename = args["filename"].ToString()!;
        var content = args["content"].ToString()!;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(home, ".myclaw", "workspace", filename);

        // Backup
        if (File.Exists(path))
        {
            File.Copy(path, path + ".bak", overwrite: true);
        }

        await File.WriteAllTextAsync(path, content);
        return $"Updated {filename}.";
    }

    private string ToolNote(Dictionary<string, object> args)
    {
        var text = args["text"].ToString()!;
        _memoryStore.AppendToday(text);
        return "Logged to today's memory.";
    }

    private string ToolRead(Dictionary<string, object> args)
    {
        var mode = args.TryGetValue("mode", out var m) ? m.ToString() : "full";

        var parts = new List<string>();

        // Core files
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var workspace = Path.Combine(home, ".myclaw", "workspace");

        foreach (var file in new[] { "AGENTS.md", "SOUL.md", "IDENTITY.md", "USER.md", "TOOLS.md" })
        {
            var path = Path.Combine(workspace, file);
            if (File.Exists(path))
            {
                parts.Add($"## {file}\n{File.ReadAllText(path)}");
            }
        }

        // Memory
        var memory = _memoryStore.ReadLongTerm();
        if (!string.IsNullOrEmpty(memory))
        {
            parts.Add($"## MEMORY.md\n{memory}");
        }

        // Today
        var today = _memoryStore.ReadToday();
        if (!string.IsNullOrEmpty(today))
        {
            parts.Add($"## Today\n{today}");
        }

        return string.Join("\n\n", parts);
    }

    private string ToolArchive()
    {
        return _memoryStore.ArchiveToday() ? "Archived today's log." : "No log to archive.";
    }

    private async Task<string> ToolEntityAsync(Dictionary<string, object> args)
    {
        var action = args["action"].ToString()!;

        return action switch
        {
            "add" => await EntityAddAsync(args),
            "remove" => await EntityRemoveAsync(args),
            "link" => await EntityLinkAsync(args),
            "query" => await EntityQueryAsync(args),
            "list" => await EntityListAsync(args),
            _ => "Unknown action"
        };
    }

    private async Task<string> EntityAddAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var type = Enum.Parse<EntityType>(args["type"].ToString()!, true);
        var attributes = args.TryGetValue("attributes", out var a) && a is Dictionary<string, object> dict
            ? dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "")
            : new Dictionary<string, string>();
        var relations = args.TryGetValue("relation", out var r) && r != null
            ? new List<string> { r.ToString()! }
            : new List<string>();

        var entity = new Entity
        {
            Name = name,
            Type = type,
            Attributes = attributes,
            Relations = relations
        };

        var result = await _entityStore.AddAsync(entity);
        return $"Entity '{result.Name}' ({result.Type}) - {result.MentionCount} mentions.";
    }

    private async Task<string> EntityRemoveAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var removed = await _entityStore.RemoveAsync(name);
        return removed ? $"Removed '{name}'." : $"Entity '{name}' not found.";
    }

    private async Task<string> EntityLinkAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var relation = args["relation"].ToString()!;
        var linked = await _entityStore.LinkAsync(name, relation);
        return linked ? $"Linked '{name}' → '{relation}'." : $"Entity '{name}' not found.";
    }

    private async Task<string> EntityQueryAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var entity = await _entityStore.QueryAsync(name);
        if (entity == null) return $"Entity '{name}' not found.";

        var attrs = string.Join(", ", entity.Attributes.Select(a => $"{a.Key}: {a.Value}"));
        return $"**{entity.Name}** ({entity.Type})\nMentions: {entity.MentionCount}\nAttributes: {attrs}\nRelations: {string.Join("; ", entity.Relations)}";
    }

    private async Task<string> EntityListAsync(Dictionary<string, object> args)
    {
        EntityType? filter = null;
        if (args.TryGetValue("filterType", out var ft) && ft != null)
        {
            filter = Enum.Parse<EntityType>(ft.ToString()!, true);
        }

        var entities = await _entityStore.ListAsync(filter);
        if (entities.Count == 0) return "No entities found.";

        var lines = entities.Select(e => $"- **{e.Name}** ({e.Type}, {e.MentionCount}x) - last: {e.LastMentioned}");
        return $"## Entities ({entities.Count})\n{string.Join("\n", lines)}";
    }

    private async Task<string> ToolExecAsync(Dictionary<string, object> args)
    {
        var command = args["command"].ToString()!;
        var result = await _commandExecutor.ExecuteAsync(command);
        return result.IsSuccess ? result.Output : $"Error (exit {result.ExitCode}): {result.Output}";
    }

    private string ToolStatus()
    {
        var evaluation = _memoryStore.EvaluateDistillation();
        var entityCount = _entityStore.GetCountAsync().Result;
        var archivedCount = _memoryStore.GetArchivedCount();

        return $"""
            === MyClaw Status ===

            Distillation: {(evaluation.ShouldDistill ? $"⚠️ {evaluation.Urgency}: {evaluation.Reason}" : "✅ OK")}
            Entities: {entityCount}
            Archived: {archivedCount}
            Skills: {_skillManager.LoadedSkills.Count}
            """;
    }

    private async Task<string> ToolSkillAsync(string name, Dictionary<string, object> args)
    {
        var skillName = name.Replace("skill_", "");
        var skill = _skillManager.GetSkill(skillName);
        if (skill == null) return $"Skill '{skillName}' not found.";

        var content = skill.GetSystemPrompt();
        return $"## Skill: {skill.Name}\n\n{content}\n\nInput: {JsonSerializer.Serialize(args)}";
    }

    private async Task HandleListResourcesAsync(HttpListenerResponse response)
    {
        var resources = new List<object>
        {
            new { uri = "myclaw://context", name = "MyClaw Context", mimeType = "text/markdown" },
            new { uri = "myclaw://skills", name = "Skills Index", mimeType = "text/markdown" }
        };

        await WriteJsonAsync(response, new { resources });
    }

    private async Task HandleReadResourceAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        var uri = request.QueryString["uri"] ?? "";
        var content = uri switch
        {
            "myclaw://context" => ToolRead(new Dictionary<string, object>()),
            "myclaw://skills" => string.Join("\n", _skillManager.LoadedSkills.Select(s => $"- {s.Name}: {s.Description}")),
            _ => "Unknown resource"
        };

        await WriteJsonAsync(response, new { contents = new[] { new { uri, mimeType = "text/markdown", text = content } } });
    }

    private async Task HandleListPromptsAsync(HttpListenerResponse response)
    {
        var prompts = new List<object>
        {
            new { name = "miniclaw_wakeup", description = "Wake up and load context" },
            new { name = "miniclaw_growup", description = "Memory distillation" },
            new { name = "miniclaw_briefing", description = "Daily briefing" }
        };

        await WriteJsonAsync(response, new { prompts });
    }

    private async Task HandleGetPromptAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        var name = request.QueryString["name"] ?? "";
        var messages = name switch
        {
            "miniclaw_wakeup" => new[] { new { role = "user", content = new { type = "text", text = "SYSTEM: Waking up... Call tool `miniclaw_read` to load context." } } },
            "miniclaw_growup" => new[] { new { role = "user", content = new { type = "text", text = "SYSTEM: Initiating memory distillation. Review today's log and update MEMORY.md." } } },
            "miniclaw_briefing" => new[] { new { role = "user", content = new { type = "text", text = $"Daily briefing:\n{ToolStatus()}" } } },
            _ => Array.Empty<object>()
        };

        await WriteJsonAsync(response, new { messages });
    }

    private async Task HandleSseAsync(HttpListenerResponse response)
    {
        response.ContentType = "text/event-stream";
        response.Headers.Add("Cache-Control", "no-cache");

        var encoder = Encoding.UTF8;
        var data = encoder.GetBytes("data: {\"type\":\"connected\"}\n\n");
        await response.OutputStream.WriteAsync(data);

        // Keep connection open for 30 seconds
        await Task.Delay(30000);
    }

    private async Task WriteJsonAsync(HttpListenerResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private async Task<string> ReadBodyAsync(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream);
        return await reader.ReadToEndAsync();
    }

    private class ToolCallRequest
    {
        public string name { get; set; } = string.Empty;
        public Dictionary<string, object>? arguments { get; set; }
    }
}
