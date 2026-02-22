# MyClaw.NET

基于 C# 和 AgentScope.NET 的个人 AI 助手 - myclaw 项目的 1:1 复刻版本。

Personal AI assistant built on [AgentScope.NET](https://github.com/linkerlin/agentscope.net) - A 1:1 replication of [myclaw](https://github.com/stellarlinkco/myclaw).

更多AI内容，请访问 [智柴网](https://zhichai.net/) 。
## 项目状态 Project Status

🚧 **开发中 In Development**

- ✅ 项目结构创建完成 / Project structure created
- ✅ 设计方案完成 / Design plan completed
- ✅ 实施计划完成 / Implementation plan completed
- ✅ Phase 1: 基础设施完成 / Phase 1: Infrastructure completed
- ✅ Phase 3: Gateway 基础完成 / Phase 3: Gateway completed
- ✅ Phase 5: Skills 系统完成 / Phase 5: Skills completed
- ✅ Phase 6: Scheduling 完成 / Phase 6: Scheduling completed
- ⏳ Phase 2: Core Agent 开发中 / Phase 2: Core Agent in progress

## 特性 Features

### 已实现 Implemented

- **CLI** - 完整的命令行接口 (agent, gateway, onboard, status, skills)
- **配置系统** - JSON 配置 + 环境变量覆盖
- **Memory 系统** - 长期记忆 (MEMORY.md) + 每日日记
- **MCP 服务** - 基于 streamable-http 的 MCP 协议实现
- **Gateway 基础** - MessageBus, ChannelManager, GatewayService
- **Skills 系统** - SKILL.md 加载器 + 3 个示例 Skills
- **Scheduling** - Cron 任务 (Quartz.NET) + Heartbeat 服务

### 计划实现 Planned

- **Agent 运行时** - 等待 AgentScope.NET 集成
- **多渠道支持** - Telegram, Feishu, WeCom, WebUI
- **多模态** - 图像识别和文档处理

## 快速开始 Quick Start

### 前置要求 Prerequisites

- .NET 9.0 SDK

### 构建 Build

```bash
# Clone the repository
git clone https://github.com/linkerlin/myclaw.net.git
cd myclaw.net

# Build the solution
dotnet build

# Run tests
dotnet test
```

### 配置 Configuration

```bash
# Initialize config and workspace
dotnet run --project src/MyClaw.CLI -- onboard

# Edit config
# ~/.myclaw/config.json
```

### 运行 Run

```bash
# Show status
dotnet run --project src/MyClaw.CLI -- status

# Skills management
dotnet run --project src/MyClaw.CLI -- skills list
dotnet run --project src/MyClaw.CLI -- skills info writer

# Run agent mode (single message)
dotnet run --project src/MyClaw.CLI -- agent -m "Hello"

# Run agent mode (REPL)
dotnet run --project src/MyClaw.CLI -- agent

# Run gateway mode
dotnet run --project src/MyClaw.CLI -- gateway
```

## MCP 服务 MCP Service

MyClaw 提供 MCP (Model Context Protocol) 服务，支持通过 streamable-http 协议连接。

### 端点 Endpoint

```
http://localhost:2334/mcp
```

### MCP 工具 MCP Tools

| 工具 | 描述 |
|------|------|
| `myclaw_update` | 神经重塑 - 修改核心认知文件 |
| `myclaw_note` | 海马体写入 - 追加今日日志 |
| `myclaw_read` | 全脑唤醒 - 读取上下文和记忆 |
| `myclaw_archive` | 日志归档 |
| `myclaw_entity` | 概念连接 - 管理实体知识图谱 |
| `myclaw_exec` | 感官与手 - 安全执行终端命令 |
| `myclaw_status` | 系统诊断 |

### MCP 提示词 MCP Prompts

| 提示词 | 描述 |
|--------|------|
| `myclaw_wakeup` | 唤醒并加载上下文 |
| `myclaw_growup` | 记忆蒸馏 |
| `myclaw_briefing` | 每日简报 |

### Kimi CLI 配置 Kimi CLI Configuration

在 Kimi CLI 配置文件中添加：

```json
{
  "mcpServers": {
    "myclaw": {
      "type": "streamable-http",
      "url": "http://localhost:2334/mcp"
    }
  }
}
```

### Claude Desktop 配置 Claude Desktop Configuration

配置文件位置：
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "myclaw": {
      "type": "streamable-http",
      "url": "http://localhost:2334/mcp"
    }
  }
}

## 项目结构 Project Structure

```
myclaw.net/
├── src/
│   ├── MyClaw.Core/          # 核心库 / Core library
│   ├── MyClaw.CLI/           # 命令行接口 / CLI
│   ├── MyClaw.Agent/         # Agent 实现 / Agent implementation
│   ├── MyClaw.Gateway/       # Gateway 服务 / Gateway service
│   ├── MyClaw.Channels/      # 渠道实现 / Channel implementations
│   ├── MyClaw.Memory/        # 内存系统 / Memory system
│   ├── MyClaw.Skills/        # 技能系统 / Skills system
│   ├── MyClaw.Cron/          # 定时任务 / Cron scheduler
│   ├── MyClaw.Heartbeat/     # 心跳服务 / Heartbeat service
│   └── MyClaw.MCP/           # MCP 服务 / MCP service
├── tests/
│   ├── MyClaw.Core.Tests/
│   └── MyClaw.Integration.Tests/
├── docs/
│   ├── 设计方案.md           # 设计文档 / Design document
│   └── 实施计划.md           # 实施计划 / Implementation plan
├── workspace/                # 示例工作区 / Example workspace
│   └── skills/               # 示例技能 / Example skills
└── MyClaw.slnx
```

## 示例 Skills

项目包含 3 个示例 Skills：

| Skill | 描述 | 关键词 |
|-------|------|--------|
| writer | 写作助手 | write, draft, content, article |
| web-search | 网络搜索 | search, web, google, find |
| calculator | 计算器 | calculate, math, convert |

## 文档 Documentation

- [设计方案.md](./设计方案.md) - 详细的系统设计和架构 / Detailed system design
- [实施计划.md](./实施计划.md) - 16周实施计划 / 16-week implementation plan
- [实施进度报告.md](./实施进度报告.md) - 当前进度报告 / Current progress report

## 技术栈 Tech Stack

- **.NET 9.0** - 核心运行时 / Core runtime
- **AgentScope.NET** - Agent 框架 / Agent framework (待集成)
- **System.CommandLine** - CLI 框架 / CLI framework
- **Quartz.NET** - 任务调度 / Job scheduling
- **Serilog** - 日志 / Logging

## 开发路线图 Roadmap

### Phase 1: 基础设施 (Week 1-2) ✅
- [x] 项目结构搭建
- [x] 配置系统实现 (JSON + 环境变量)
- [x] 日志系统集成 (Serilog)
- [x] CLI 框架 (System.CommandLine)

### Phase 2: Core Agent (Week 3-4) ⏳
- [x] Memory 系统集成 (长期记忆 + 每日记忆)
- [ ] MyClawAgent 实现 (等待 AgentScope.NET)

### Phase 3: Gateway 基础 (Week 5-6) ✅
- [x] MessageBus 实现 (Channel<T>)
- [x] ChannelManager 实现
- [x] GatewayService 实现
- [x] 消息模型 (Inbound/Outbound)

### Phase 4: Channels (Week 7-10) ⏳
- [ ] WebUI Channel
- [ ] Telegram Channel
- [ ] Feishu Channel
- [ ] WeCom Channel

### Phase 5: Skills & Tools (Week 11-12) ✅
- [x] Skill 加载系统 (YAML Frontmatter 解析)
- [x] SkillManager (技能管理和查询)
- [x] 3 个示例 Skills (writer, web-search, calculator)
- [x] Skills CLI 完善

### Phase 6: Scheduling (Week 13-14) ✅
- [x] Cron 系统 (Quartz.NET)
- [x] Heartbeat 服务

### Phase 7: MCP Service ✅
- [x] MCP 服务 (streamable-http)
- [x] JSON-RPC 2.0 协议支持
- [x] 7 个核心工具 (myclaw_*)
- [x] 3 个提示词模板

### Phase 8: Testing & Polish (Week 15-16)
- [ ] 完整测试
- [ ] 文档完善
- [ ] 发布准备

## 贡献 Contributing

欢迎贡献！请查看 [实施计划.md](./实施计划.md) 了解当前进展和未完成的任务。

Contributions are welcome! Please see [实施计划.md](./实施计划.md) for current progress.

## 许可证 License

MIT License - 详见 [LICENSE](./LICENSE) 文件

## 致谢 Acknowledgments

- [myclaw](https://github.com/stellarlinkco/myclaw) - 原始项目 / Original project
- [AgentScope.NET](https://github.com/linkerlin/agentscope.net) - 底层框架 / Underlying framework
- [agentsdk-go](https://github.com/cexll/agentsdk-go) - myclaw 的底层框架

---

**Status**: 🚧 In Development  
**Version**: 0.3.0-alpha  
**Last Updated**: 2026-02-23
