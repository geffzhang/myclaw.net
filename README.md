# MyClaw.NET

基于 C# 和 AgentScope.NET 的个人 AI 助手 - myclaw 项目的 1:1 复刻版本。

Personal AI assistant built on [AgentScope.NET](https://github.com/linkerlin/agentscope.net) - A 1:1 replication of [myclaw](https://github.com/stellarlinkco/myclaw).

## 项目状态 Project Status

🚧 **开发中 In Development**

- ✅ 项目结构创建完成 / Project structure created
- ✅ 设计方案完成 / Design plan completed
- ✅ 实施计划完成 / Implementation plan completed
- ⏳ Phase 1: 基础设施开发中 / Phase 1: Infrastructure in progress

## 特性 Features

### 计划实现 Planned Features

- **CLI Agent** - 单次消息或交互式 REPL 模式 / Single message or interactive REPL mode
- **Gateway** - 完整编排：渠道 + 定时任务 + 心跳 / Full orchestration: channels + cron + heartbeat
- **多渠道支持** / Multi-Channel Support:
  - Telegram Bot
  - Feishu (飞书/Lark)
  - WeCom (企业微信)
  - WhatsApp
  - Web UI (浏览器界面)
- **多模态** - 图像识别和文档处理 / Image recognition and document processing
- **定时任务** - JSON 持久化的 Cron 作业 / Cron jobs with JSON persistence
- **心跳任务** - 周期性任务 / Periodic heartbeat tasks
- **记忆系统** - 长期记忆 (MEMORY.md) + 每日记忆 / Long-term (MEMORY.md) + daily memories
- **技能系统** - 从工作区加载自定义技能 / Custom skill loading from workspace
- **多 Provider** - 支持 Anthropic 和 OpenAI 模型 / Support for Anthropic and OpenAI models

## 快速开始 Quick Start

### 前置要求 Prerequisites

- .NET 9.0 SDK
- SQLite

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
# Copy the example configuration
cp .env.example .env

# Edit .env and add your API keys
# vim .env
```

### 运行 Run

```bash
# Run agent mode (single message)
dotnet run --project src/MyClaw.CLI -- agent -m "Hello"

# Run agent mode (REPL)
dotnet run --project src/MyClaw.CLI -- agent

# Run gateway mode
dotnet run --project src/MyClaw.CLI -- gateway
```

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
│   └── MyClaw.Heartbeat/     # 心跳服务 / Heartbeat service
├── tests/
│   ├── MyClaw.Core.Tests/
│   └── MyClaw.Integration.Tests/
├── docs/
│   ├── 设计方案.md           # 设计文档 / Design document
│   └── 实施计划.md           # 实施计划 / Implementation plan
├── workspace/                # 默认工作区 / Default workspace
│   └── skills/               # 自定义技能 / Custom skills
└── MyClaw.sln
```

## 文档 Documentation

- [设计方案.md](./设计方案.md) - 详细的系统设计和架构 / Detailed system design and architecture
- [实施计划.md](./实施计划.md) - 16周实施计划 / 16-week implementation plan

## 技术栈 Tech Stack

- **.NET 9.0** - 核心运行时 / Core runtime
- **AgentScope.NET** - Agent 框架 / Agent framework
- **Entity Framework Core** - ORM
- **SQLite** - 数据库 / Database
- **System.CommandLine** - CLI 框架 / CLI framework
- **Quartz.NET** - 任务调度 / Job scheduling

## 开发路线图 Roadmap

### Phase 1: 基础设施 (Week 1-2)
- [x] 项目结构搭建
- [ ] 配置系统实现
- [ ] 日志系统集成

### Phase 2: Core Agent (Week 3-4)
- [ ] MyClawAgent 实现
- [ ] Memory 系统集成
- [ ] Agent 模式（单次 + REPL）

### Phase 3: Gateway 基础 (Week 5-6)
- [ ] MessageBus 实现
- [ ] ChannelManager 实现
- [ ] Gateway 服务协调

### Phase 4: Channels (Week 7-10)
- [ ] WebUI Channel
- [ ] Telegram Channel
- [ ] Feishu Channel
- [ ] WeCom Channel

### Phase 5: Skills & Tools (Week 11-12)
- [ ] Skill 加载系统
- [ ] 示例 Skills

### Phase 6: Scheduling (Week 13-14)
- [ ] Cron 系统
- [ ] Heartbeat 服务

### Phase 7: Testing & Polish (Week 15-16)
- [ ] 完整测试
- [ ] 文档完善
- [ ] 发布准备

## 贡献 Contributing

欢迎贡献！请查看 [实施计划.md](./实施计划.md) 了解当前进展和未完成的任务。

Contributions are welcome! Please see [实施计划.md](./实施计划.md) for current progress and pending tasks.

## 许可证 License

MIT License - 详见 [LICENSE](./LICENSE) 文件

## 致谢 Acknowledgments

- [myclaw](https://github.com/stellarlinkco/myclaw) - 原始项目 / Original project
- [AgentScope.NET](https://github.com/linkerlin/agentscope.net) - 底层框架 / Underlying framework
- [agentsdk-go](https://github.com/cexll/agentsdk-go) - myclaw 的底层框架

---

**Status**: 🚧 In Development  
**Version**: 0.1.0-alpha  
**Last Updated**: 2026-02-19
