---
标题：Claude Code Skills 使用指南
版本号: v1.0
更新日期: 2026-02-01
最后修改者: Claude Opus 4.5
标签: [文档][工具][Skills][使用指南]
摘要: RimworldModStudio项目中自定义Skills的使用说明和参考文档
---

# Claude Code Skills 使用指南

本文档介绍RimworldModStudio项目中可用的自定义Skills及其使用方法。

---

## 什么是Skills？

Skills是Claude Code中的可重用工作流程模板。通过定义标准化的执行步骤，Skills可以将复杂的多步骤任务自动化，提高工作效率和一致性。

## 如何使用Skills？

在对话中使用斜杠命令调用skill：

```
/skill-name param1="value1" param2="value2"
```

---

## 可用Skills列表

### 1. design-doc-outline - 文档大纲设计

**用途**：基于全局规划文件和文档定位，自动设计结构化的文档大纲。

**适用场景**：
- 为RimWT项目创建新的概念笔记文档
- 需要基于全局规划文件设计文档结构
- 需要确保文档格式符合项目规范

**参数**：

| 参数名 | 类型 | 必需 | 默认值 | 说明 |
|--------|------|------|--------|------|
| `doc_id` | string | 是 | - | 文档序号（如"1.4"、"2.1"） |
| `concept_name` | string | 是 | - | 核心概念名称（如"Trion战斗体"） |
| `planning_file` | string | 否 | `C:\NiwtDatas\Projects\RimworldModStudio\临时\RimWT\RimWT模组开发完整流程与文档体系.md` | 全局规划文件路径 |
| `output_dir` | string | 否 | `C:\NiwtDatas\Projects\RimworldModStudio\临时\RimWT\第一阶段：原材料理解` | 输出目录路径 |

**调用示例**：

基本调用（使用默认值）：
```
/design-doc-outline doc_id="1.5" concept_name="Trion兵系统"
```

完整调用（指定所有参数）：
```
/design-doc-outline doc_id="2.1" concept_name="原作术语词典" planning_file="C:\NiwtDatas\Projects\RimworldModStudio\临时\RimWT\RimWT模组开发完整流程与文档体系.md" output_dir="C:\NiwtDatas\Projects\RimworldModStudio\临时\RimWT\第二阶段：素材提炼"
```

**工作流程**：

1. **理解文档定位**
   - 读取规划文件，找到指定doc_id的条目
   - 分析文档定位和用户期望

2. **信息收集**
   - 进行网络搜索收集相关概念信息
   - 参考已有笔记的结构风格
   - 提取相关描述

3. **设计大纲结构**
   - 基于概念特性设计6-8个主章节
   - 每章3-4个小节
   - 遵循"广度优先"原则

4. **用户确认**
   - 询问大纲深度是否合适
   - 确认特殊情况章节内容
   - 确认相关概念简述是否需要

5. **生成交付物**
   - 更新规划文件中的条目定义
   - 创建标准化大纲文件

**输出文件格式**：

生成的文档包含：
- 元信息（标题、版本、标签、摘要）
- 文档说明
- 6-8个主章节，每章3-4个小节
- 术语对照表
- 历史修改记录

**预期效果**：

- **效率提升**：从手动30-60分钟缩短到5-10分钟
- **质量保证**：统一的结构和格式
- **一致性**：所有文档遵循相同标准
- **可维护性**：易于更新和扩展

**详细文档**：`.claude/skills/design-doc-outline.md`

---

## Skills开发指南

### 创建新的Skill

1. 在`.claude/skills/`目录下创建新的`.md`文件
2. 添加YAML前置元数据：
   ```yaml
   ---
   name: skill-name
   description: Skill描述
   version: 1.0.0
   author: 作者名
   tags: [标签1, 标签2]
   ---
   ```
3. 编写详细的使用说明和执行流程
4. 更新本文档，添加新skill的说明

### Skill设计原则

1. **参数化设计**：使用`{{param_name}}`占位符
2. **清晰的步骤**：将工作流程分解为明确的步骤
3. **错误处理**：考虑各种异常情况
4. **用户交互**：在关键决策点使用AskUserQuestion
5. **遵循规范**：符合项目CLAUDE.md中的规则

### Skill文件结构

```markdown
---
name: skill-name
description: 简短描述
version: 1.0.0
author: 作者
tags: [标签]
---

# Skill标题

## 概述
[Skill的用途和价值]

## 使用场景
[适用的场景列表]

## 参数说明
[参数表格]

## 调用示例
[示例代码]

## 工作流程
[详细步骤]

---

# Skill Prompt

[详细的执行指令，使用{{param}}占位符]

## 执行步骤

### 第1步：...
[具体指令]

### 第2步：...
[具体指令]

---

## 注意事项
[重要提醒和错误处理]

## 版本历史
[版本记录表格]

## 相关资源
[相关文件链接]
```

---

## 相关资源

- Skills目录：`.claude/skills/`
- 项目规则：`.claude/CLAUDE.md`
- 元信息模板：`系统文件/元信息及历史记录模板.md`
- 标签说明：`系统文件/标签类别及说明.md`

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-01 | 创建Skills使用指南，添加design-doc-outline说明 | Claude Opus 4.5 |

---

**文档状态**：已完成
**下一步**：根据需要添加更多Skills

---

**文档创建完成** | Claude Opus 4.5