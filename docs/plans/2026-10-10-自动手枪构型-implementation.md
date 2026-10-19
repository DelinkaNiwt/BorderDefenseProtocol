# 自动手枪武装构型实施计划

> **For Codex:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 新增自动手枪武装构型，并让小兵弹丸固定装载按“功能 + 构型”组合解析。

**Architecture:** 自动手枪参数和视觉留在 Content Def（内容定义）中。固定装载创建单功能芯片时，把该功能配置的固定装载构型写入现有组合记录，再复用 `ChipCombinationResolver`；不修改 Core（核心）固定装载接口或其它射手功能的默认构型。

**Tech Stack:** C#（C#语言）、RimWorld XML（游戏定义配置）、简体中文语言包。

---

### 任务一：增加自动手枪构型和视觉预设

**文件：**
- 修改：`1.6/Content/Defs/ChipArmamentFormDef/Presets.xml`
- 修改：`1.6/Content/Defs/ExpressionDef/Visual.xml`
- 修改：`Languages/ChineseSimplified (简体中文)/Keyed/ChipManufacturing.xml`
- 修改：`Languages/ChineseSimplified (简体中文)/DefInjected/BDP.Core.Expressions.ExpressionVisualPresetDef/Pistol.xml`

**步骤：**
1. 新增自动手枪构型，兼容枪手和射手；复制左轮手枪构型的研究、材料与工作量。
2. 覆盖原版自动手枪对应的射程、精度、射击节奏、冷却、音效与枪口闪光；不添加投射物倍率覆盖。
3. 新增单持和双持视觉预设，沿用小型枪械基准和左轮手枪握持位置，只替换为原版自动手枪贴图路径。
4. 添加构型名称、说明和视觉预设语言键。

### 任务二：让小兵弹丸显式组合自动手枪构型

**文件：**
- 修改：`Source/BDP.Content/Assembly/ChipManufacturing/Defs/ChipManufacturingPresetDef.cs`
- 修改：`Source/BDP.Content/Assembly/ChipManufacturing/Production/ChipProductCreationService.cs`
- 修改：`1.6/Content/Defs/ChipManufacturingPresetDef/NearRealm.xml`

**步骤：**
1. 给芯片功能定义增加可选的固定装载构型引用，并补充中文成员注释。
2. 固定装载创建芯片时，将该构型 DefName（定义标识）写入 `ChipCombinationRecord`，继续走现有组合解析器和构型应用逻辑。
3. 只给小兵弹丸配置自动手枪构型；保持每发 2 Trion 和其他芯片参数不变。

### 任务三：检查配置、构建与提交

**文件：**
- 修改：`docs/plans/2026-10-10-自动手枪构型-implementation.md`
- 修改：`../../日志/Agent工作日志` 下本次工作日志

**步骤：**
1. 对本次修改的 XML 执行结构解析，并检查差异只包含本功能及工作日志。
2. 构建 `Source/BDP.Content/BDP.Content.csproj`，确认 C#（C#语言）修改可编译。
3. 记录工作日志并提交本次功能；不新增或运行测试，不纳入工作区中已有的无关改动。
