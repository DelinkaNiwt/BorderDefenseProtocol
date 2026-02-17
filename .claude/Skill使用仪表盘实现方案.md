# Skill 使用仪表盘实现方案

## 方案概述

Skill 使用仪表盘的目标是让 AI 和用户都能实时看到：
- 当前会话中有哪些可用 skills
- 哪些已被使用
- 哪些应该用但没用

## 实现方式

### 方式1：System Reminder 注入（推荐，无需修改代码）

在每次对话开始或关键节点，通过 system reminder 显示仪表盘：

```xml
<system-reminder>
═══════════════════════════════════════════════════════
                    Skill 使用仪表盘
═══════════════════════════════════════════════════════

本次会话可用 Skills: 15
已使用: 1
  ✓ using-superpowers (对话开始时)

未使用但可能相关:
  ⚠ brainstorming - 当前任务涉及架构设计
  ⚠ planning-with-files - 当前任务涉及文档生成

检查点提醒:
  - 看到"如何设计"、"怎么抽象" → 调用 brainstorming
  - 准备创建文档 → 调用 planning-with-files

═══════════════════════════════════════════════════════
</system-reminder>
```

**实现步骤**：

1. 在 `.claude/CLAUDE.md` 中添加触发规则：

```markdown
## Skill 仪表盘显示规则

在以下时机自动显示 Skill 仪表盘：
- 对话开始后的第一次回复
- 用户提出新任务时
- 完成一个任务后

仪表盘格式：
- 列出所有可用 skills（从 system-reminder 中提取）
- 标记已使用的 skills（✓）
- 标记应该用但未用的 skills（⚠）
- 提供触发词提醒
```

2. AI 在回复开头主动显示：

```
[Skill 仪表盘]
可用: 15 | 已用: 1 | 未用但相关: 2

✓ using-superpowers
⚠ brainstorming (架构设计任务)
⚠ planning-with-files (文档生成任务)

---

[你的实际回复内容]
```

### 方式2：自定义 Skill 追踪器（需要代码支持）

创建一个专门的 skill 来管理仪表盘：

```markdown
# skill: skill-dashboard

## 功能
追踪和显示当前会话的 skill 使用情况

## 调用时机
- 对话开始时自动调用
- 用户输入 `/skills` 命令时
- 完成任务后自动调用

## 输出格式
┌─────────────────────────────────────────┐
│         Skill 使用仪表盘                 │
├─────────────────────────────────────────┤
│ 会话时长: 15分钟                         │
│ 可用 Skills: 15                          │
│ 已使用: 2 (13%)                          │
├─────────────────────────────────────────┤
│ ✓ 已使用:                                │
│   - using-superpowers (00:00)           │
│   - brainstorming (05:23)               │
├─────────────────────────────────────────┤
│ ⚠ 应该用但未用:                          │
│   - planning-with-files                 │
│     原因: 生成了技术报告但未先规划       │
├─────────────────────────────────────────┤
│ ○ 可用但未触发:                          │
│   - test-driven-development             │
│   - systematic-debugging                │
│   - ... (10 more)                       │
└─────────────────────────────────────────┘
```

### 方式3：会话元数据追踪（最完整，需要平台支持）

在 Claude Code 的会话元数据中追踪 skill 使用：

```json
{
  "session_id": "abc123",
  "skills_available": [
    "brainstorming",
    "planning-with-files",
    "test-driven-development",
    ...
  ],
  "skills_used": [
    {
      "name": "using-superpowers",
      "timestamp": "2026-02-15T10:00:00Z",
      "trigger": "auto"
    },
    {
      "name": "brainstorming",
      "timestamp": "2026-02-15T10:05:23Z",
      "trigger": "manual"
    }
  ],
  "skills_missed": [
    {
      "name": "planning-with-files",
      "should_have_triggered_at": "2026-02-15T10:10:00Z",
      "reason": "User requested document generation"
    }
  ]
}
```

这需要 Claude Code 平台层面的支持，可以在 UI 中显示实时仪表盘。

## 推荐实施路径

### 阶段1：手动仪表盘（立即可用）

在 CLAUDE.md 中添加规则，要求 AI 在关键节点主动显示仪表盘：

```markdown
## Skill 仪表盘显示要求

在以下时机，必须在回复开头显示 Skill 仪表盘：
1. 对话开始后的第一次回复
2. 用户提出新任务时（检测到触发词）
3. 完成任务后

格式：
[Skill 仪表盘]
可用: X | 已用: Y | 相关未用: Z
✓ 已使用的 skills
⚠ 应该用但未用的 skills
---
```

### 阶段2：System Reminder 自动化（需要配置）

如果 Claude Code 支持自定义 system reminder，可以配置定期注入仪表盘提醒。

### 阶段3：平台集成（长期目标）

向 Claude Code 团队提交 feature request，请求内置 skill 使用追踪和仪表盘功能。

## 示例：手动仪表盘的实际使用

**对话开始时**：

```
[Skill 仪表盘]
可用: 15 | 已用: 1 | 相关未用: 0
✓ using-superpowers (已加载)
---

你好！有什么需要帮忙的？
```

**用户问"如何设计 Trion 系统"时**：

```
[Skill 仪表盘]
可用: 15 | 已用: 1 | 相关未用: 1
✓ using-superpowers
⚠ brainstorming (检测到"如何设计"触发词)
---

检测到架构设计任务，正在调用 brainstorming skill...
```

**任务完成后**：

```
[Skill 仪表盘 - 会话总结]
可用: 15 | 已用: 2 | 相关未用: 1
✓ using-superpowers
✓ brainstorming
⚠ planning-with-files (生成了报告但未先规划)

反思：下次生成大型文档前应先调用 planning-with-files 规划结构。
---

报告已生成完成。
```

## 实施建议

**立即可做**：
1. 在 CLAUDE.md 中添加"仪表盘显示要求"
2. 要求 AI 在关键节点主动显示
3. 在事后反思中总结 skill 使用情况

**需要平台支持**：
1. 自动注入 system reminder
2. UI 层面的实时仪表盘
3. 跨会话的 skill 使用统计

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-15 | 初始版本：三种实现方式（System Reminder/自定义Skill/平台集成）、推荐实施路径、示例演示 | Claude Haiku 4.5 |
