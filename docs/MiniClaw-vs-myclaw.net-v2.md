# MiniClaw vs myclaw.net 功能对比分析 (v2.0)

> 基于 myclaw.net 最新增强版本的全面对比

## 概览

| 维度 | MiniClaw | myclaw.net (Enhanced) | 优势方 |
|------|----------|----------------------|--------|
| **定位** | MCP 插件 | 独立运行时 + MCP 服务 | 各有侧重 |
| **代码量** | ~2,700 行 | ~8,000+ 行 | - |
| **部署方式** | npx 一键 | dotnet build + CLI + MCP | MiniClaw |
| **UI 界面** | ❌ 无 | ✅ Uno + WebUI | myclaw.net |
| **协议支持** | MCP | MCP + AgentScope + Gateway | myclaw.net |

---

## 1. 记忆系统对比

| 功能 | MiniClaw | myclaw.net | 状态 |
|------|----------|------------|------|
| **短期记忆** | `memory/YYYY-MM-DD.md` | `memory/YYYY-MM-DD.md` | ✅ 相同 |
| **长期记忆** | `MEMORY.md` | `MEMORY.md` | ✅ 相同 |
| **记忆蒸馏** | ✅ 自动评估 + 4条件触发 | ✅ `DistillationEvaluator` 完整实现 | ✅ 相同 |
| **记忆归档** | ✅ `miniclaw_archive` | ✅ `ArchiveToday()` | ✅ 相同 |
| **实体图谱** | ✅ 6种类型 + 关系追踪 | ✅ `EntityStore` 完整实现 | ✅ 相同 |
| **实体浮现** | ✅ 从日志自动提取 | ✅ `SurfaceRelevantAsync()` | ✅ 相同 |
| **蒸馏提醒** | ✅ 系统消息提示 | ✅ 集成到状态检查 | ✅ 相同 |

### 蒸馏触发条件对比

| 条件 | MiniClaw | myclaw.net |
|------|----------|------------|
| 条目数量 | > 20 | > 20 |
| Token 预算 | > 40% | > 40% |
| 最旧条目 | > 8h | > 8h |
| 日志大小 | > 8KB | > 8KB |

**结论**: ✅ myclaw.net 已完全实现 MiniClaw 的记忆系统

---

## 2. ACE 自适应上下文引擎对比

| 功能 | MiniClaw | myclaw.net | 状态 |
|------|----------|------------|------|
| **时间模式** | ✅ 5种模式 | ✅ `TimeModeManager` | ✅ 相同 |
| **会话延续** | ✅ 检测返回场景 | ✅ `ContinuationDetector` | ✅ 相同 |
| **Token 预算** | ✅ 8000 默认 | ✅ 可配置 | ✅ 相同 |
| **上下文裁剪** | ✅ 按优先级 | ✅ `ContextCompiler` | ✅ 相同 |
| **内容哈希** | ✅ 变更检测 | ✅ `HashString()` | ✅ 相同 |
| **工作区感知** | ✅ Git + Tech Stack | ❌ 未实现 | MiniClaw |

### 时间模式对比

| 模式 | MiniClaw | myclaw.net |
|------|----------|------------|
| Morning ☀️ | 06-09 | 06-09 |
| Work 💼 | 09-12, 14-18 | 09-12, 14-18 |
| Break 🍜 | 12-14 | 12-14 |
| Evening 🌙 | 18-22 | 18-22 |
| Night 😴 | 22-06 | 22-06 |

**结论**: ⚠️ myclaw.net 缺少工作区自动感知（Git/技术栈检测）

---

## 3. 工具系统对比

| 工具 | MiniClaw | myclaw.net | 状态 |
|------|----------|------------|------|
| `miniclaw_update` | ✅ 神经重塑 | ✅ 实现 | ✅ 相同 |
| `miniclaw_note` | ✅ 海马体写入 | ✅ 实现 | ✅ 相同 |
| `miniclaw_read` | ✅ 全脑唤醒 | ✅ 实现 | ✅ 相同 |
| `miniclaw_archive` | ✅ 日志归档 | ✅ 实现 | ✅ 相同 |
| `miniclaw_search` | ✅ 深层回忆 | ✅ `GetRecentMemories` | ✅ 相同 |
| `miniclaw_status` | ✅ 系统诊断 | ✅ 实现 | ✅ 相同 |
| `miniclaw_entity` | ✅ 概念连接 | ✅ 完整 CRUD | ✅ 相同 |
| `miniclaw_exec` | ✅ 命令执行 | ✅ `CommandExecutor` | ✅ 相同 |
| **技能工具** | ✅ 动态发现 | ✅ 集成 | ✅ 相同 |

### 命令执行安全对比

| 安全特性 | MiniClaw | myclaw.net |
|----------|----------|------------|
| 白名单机制 | ✅ 允许列表 | ✅ 允许列表 |
| 黑名单机制 | ✅ rm/sudo禁止 | ✅ rm/sudo禁止 |
| 超时限制 | 10秒 | 10秒 |
| 输出限制 | 1MB | 1MB |
| 工作目录限制 | ✅ | ✅ |

**结论**: ✅ myclaw.net 已完全实现 MiniClaw 的工具系统

---

## 4. DNA/模板系统对比

| 文件 | MiniClaw | myclaw.net | 状态 |
|------|----------|------------|------|
| `AGENTS.md` | ✅ | ✅ | ✅ 相同 |
| `SOUL.md` | ✅ | ✅ | ✅ 相同 |
| `MEMORY.md` | ✅ | ✅ | ✅ 相同 |
| `HEARTBEAT.md` | ✅ | ✅ | ✅ 相同 |
| `IDENTITY.md` | ✅ | ✅ 新增 | ✅ 已补齐 |
| `USER.md` | ✅ | ✅ 新增 | ✅ 已补齐 |
| `TOOLS.md` | ✅ | ✅ 新增 | ✅ 已补齐 |
| `BOOTSTRAP.md` | ✅ | ✅ 新增 | ✅ 已补齐 |
| `SUBAGENT.md` | ✅ | ✅ 新增 | ✅ 已补齐 |

**结论**: ✅ myclaw.net 已补齐所有 DNA 模板

---

## 5. 自动进化链对比

| 信号类型 | MiniClaw | myclaw.net | 状态 |
|----------|----------|------------|------|
| 用户偏好 → USER.md | ✅ | ✅ `SignalDetector` | ✅ 相同 |
| 性格修正 → SOUL.md | ✅ | ✅ | ✅ 相同 |
| 环境配置 → TOOLS.md | ✅ | ✅ | ✅ 相同 |
| 工具经验 → TOOLS.md | ✅ | ✅ | ✅ 相同 |
| 身份改变 → IDENTITY.md | ✅ | ✅ | ✅ 相同 |
| 工作流学习 → AGENTS.md | ✅ | ✅ | ✅ 相同 |
| 重要事实 → MEMORY.md | ✅ | ✅ | ✅ 相同 |
| 日常记录 → memory/ | ✅ | ✅ | ✅ 相同 |

### 信号检测实现对比

| 特性 | MiniClaw | myclaw.net |
|------|----------|------------|
| 关键词匹配 | ✅ | ✅ |
| 正则表达式 | ✅ | ✅ |
| 置信度评分 | ✅ | ✅ |
| 多语言支持 | ✅ 中英 | ✅ 中英 |

**结论**: ✅ myclaw.net 已实现完整的自动进化链

---

## 6. 技能系统对比

| 功能 | MiniClaw | myclaw.net | 状态 |
|------|----------|------------|------|
| **SKILL.md 格式** | ✅ YAML Frontmatter | ✅ YAML Frontmatter | ✅ 相同 |
| **技能发现** | ✅ `SkillCache` (5s TTL) | ✅ `SkillManager` (实时) | myclaw.net 更实时 |
| **技能提示词** | ✅ 动态注册 | ✅ 支持 | ✅ 相同 |
| **技能工具** | ✅ 动态注册 | ✅ 支持 | ✅ 相同 |
| **可执行技能** | ✅ `exec` 字段 | ✅ `CommandExecutor` 支持 | ✅ 相同 |
| **技能缓存** | ✅ 5秒 TTL | ❌ 实时扫描 | MiniClaw |

**结论**: ⚠️ myclaw.net 缺少技能缓存，但功能完整

---

## 7. 渠道与界面对比

| 渠道/界面 | MiniClaw | myclaw.net | 优势方 |
|-----------|----------|------------|--------|
| **MCP 协议** | ✅ 原生 | ✅ MCP服务(2334端口) | 相同 |
| **GUI 界面** | ❌ 无 | ✅ Uno Platform | **myclaw.net** |
| **Web 界面** | ❌ 无 | ✅ WebSocket | **myclaw.net** |
| **CLI 工具** | ❌ 无 | ✅ 完整 CLI | **myclaw.net** |
| **Telegram** | ❌ 无 | 📝 预留接口 | myclaw.net |
| **飞书/企微** | ❌ 无 | 📝 预留接口 | myclaw.net |

**结论**: 🏆 myclaw.net 在界面层全面领先

---

## 8. 调度与任务对比

| 功能 | MiniClaw | myclaw.net | 优势方 |
|------|----------|------------|--------|
| **心跳机制** | ✅ node-cron (30分钟) | ✅ HeartbeatService | 相同 |
| **Cron 任务** | ❌ 仅心跳 | ✅ Quartz (复杂Cron) | **myclaw.net** |
| **定时消息** | ❌ 不支持 | ✅ 可投递到渠道 | **myclaw.net** |
| **任务触发** | ❌ 不支持 | ✅ Webhook/API | **myclaw.net** |

**结论**: 🏆 myclaw.net 调度能力更强

---

## 9. 分析统计对比

| 功能 | MiniClaw | myclaw.net | 状态 |
|------|----------|------------|------|
| **工具调用统计** | ✅ | ❌ 未实现 | MiniClaw |
| **启动统计** | ✅ 时间/次数 | ❌ 未实现 | MiniClaw |
| **实体统计** | ✅ | ✅ | ✅ 相同 |
| **每日简报** | ✅ `miniclaw_briefing` | ❌ 未实现 | MiniClaw |
| **文件健康** | ✅ 检查更新频率 | ❌ 未实现 | MiniClaw |

**结论**: ⚠️ myclaw.net 缺少部分统计功能

---

## 10. 架构与部署对比

| 维度 | MiniClaw | myclaw.net | 优势方 |
|------|----------|------------|--------|
| **语言** | TypeScript | C# (.NET 9) | 各有优势 |
| **代码量** | ~2,700 行 | ~8,000+ 行 | MiniClaw 更精简 |
| **依赖** | 少 (3个npm包) | 多 (.NET生态) | MiniClaw |
| **启动速度** | ⚡ 快 | 🐢 较慢 | MiniClaw |
| **内存占用** | 🟢 低 | 🟡 中等 | MiniClaw |
| **扩展性** | 🟡 插件式 | 🟢 模块化分层 | myclaw.net |
| **企业集成** | 🟡 一般 | 🟢 强 (.NET) | myclaw.net |

---

## 功能对照表（总览）

### ✅ myclaw.net 已实现（与 MiniClaw 持平）

| 功能类别 | 具体功能 |
|----------|----------|
| 记忆系统 | 蒸馏评估、归档、实体图谱 |
| ACE 引擎 | 时间模式、会话延续、上下文编译 |
| 工具系统 | 8个核心工具 + 安全执行 |
| DNA 模板 | 9个完整模板 |
| 自动进化 | 信号检测表 |
| MCP 服务 | 完整协议实现 |

### 🏆 myclaw.net 优势功能

| 功能 | 说明 |
|------|------|
| Uno Platform GUI | 原生桌面应用 |
| WebSocket WebUI | 实时聊天界面 |
| 完整 CLI | agent/gateway/skills/status |
| Quartz 调度 | 复杂 Cron 表达式 |
| 多渠道架构 | MessageBus + Channel |
| AgentScope 集成 | ReActAgent 推理引擎 |

### ⚠️ myclaw.net 仍缺少的功能

| 功能 | MiniClaw 实现 | 优先级 |
|------|---------------|--------|
| 工作区感知 | Git状态/技术栈自动检测 | 高 |
| 技能缓存 | 5秒 TTL 避免重复扫描 | 中 |
| 使用统计 | 工具调用计数/启动时间 | 低 |
| 每日简报 | 昨日回顾/待办/实体概览 | 中 |
| 文件健康检查 | 检查 DNA 文件更新频率 | 低 |

---

## 总结

### MiniClaw 优势
1. **极简部署** - npx 一键启动，零配置
2. **运行轻快** - 代码精简，启动快速
3. **工作区感知** - 自动检测项目环境
4. **使用统计** - 完善的分析面板

### myclaw.net 优势
1. **界面完整** - Uno GUI + WebUI + CLI
2. **调度强大** - Quartz 支持复杂定时任务
3. **协议兼容** - MCP + AgentScope + Gateway
4. **企业友好** - .NET 生态，模块化架构

### 选择建议

| 场景 | 推荐 |
|------|------|
| 个人快速体验 | MiniClaw |
| 编辑器插件 | MiniClaw |
| 团队部署 | myclaw.net |
| 需要 GUI | myclaw.net |
| 定时任务 | myclaw.net |
| 企业集成 | myclaw.net |

---

*对比版本: MiniClaw v0.5.0 vs myclaw.net Enhanced (2025-02-19)*
