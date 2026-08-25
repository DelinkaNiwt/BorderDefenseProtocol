# 光魂本地化闭环 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 消除光魂制造说明、表达名称、攻击按钮与新伤口来源中的英文泄漏。

**Architecture:** 为既有动作预设和表达配置增加可选语言键，在定义进入正式运行时之前统一解析。具体中文仅存于 Keyed（键值语言包），英文定义保留为缺少翻译时的回退。

**Tech Stack:** RimWorld 1.6、C#、XML、PowerShell 回归测试。

---

### Task 1: 建立失败回归测试

**Files:**
- Modify: `Source/BDP.Tests/LightSoulChipSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ChipManufacturingLocalizationSmokeTests.ps1`

**Steps:**
1. 断言动作预设拥有说明键及解析属性，信息窗读取解析说明。
2. 断言表达配置拥有工具名称键，解释器在正式工具复制前解析它。
3. 断言光魂四个表达名称和两个工具名称均声明 Keyed 键。
4. 运行两个脚本，确认因设施与配置缺失而失败。

### Task 2: 实现通用语言键设施

**Files:**
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/Defs/ChipActionPresetDef.cs`
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/Window_ChipPresetInfo.cs`
- Modify: `Source/BDP/Core/Expressions/Config/ChipExpressionEntryConfig.cs`
- Modify: `Source/BDP/Core/Expressions/Contract/ChipExpressionContractInterpreter.cs`
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/Resolution/ChipGunShellExpressionService.cs`

**Steps:**
1. 增加 `descriptionKey` 与 `ResolvedDescription`。
2. 信息窗只对动作预设使用解析说明，其他定义保持原版行为。
3. 增加 `ToolLabelKeys`，复制 `Tool` 后按索引解析语言键。
4. 制造克隆链保留工具名称键。
5. 运行回归脚本，确认设施相关断言通过。

### Task 3: 补齐光魂语言配置

**Files:**
- Modify: `1.6/Content/Defs/ChipActionPresetDef/Presets.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/Keyed/Gameplay.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/DefInjected/ChipActionPresetDef/Presets.xml`

**Steps:**
1. 给光魂说明和四个表达条目声明稳定语言键。
2. 给重刃钝击、切割声明与 `tools` 顺序一致的工具名称键。
3. 从失效的光魂自定义 Def 注入文件移除重复嵌套翻译，避免继续产生误导。
4. 运行两个回归脚本并确认通过。

### Task 4: 编译、全量验证与记录

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志*.md`

**Steps:**
1. 编译 Core、Content 和 Development 正式配置。
2. 运行 BDP 全部 PowerShell 测试脚本。
3. 检查差异只包含计划内文件。
4. 写倒序工作日志并提交功能修复。
