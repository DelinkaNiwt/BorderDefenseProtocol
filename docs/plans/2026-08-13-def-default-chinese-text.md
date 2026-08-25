# 正式 Def 默认中文文本 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将主模组正式 Def 中可直接书写的纯英文玩家文本改为中文原文，同时继续保留现有语言包覆盖。

**Architecture:** 只修改 `1.6/Content/Defs` 内可翻译的 `label`、`description`、`DisplayLabel` 与 Verb（动作）标签。稳定键、DefName、类名、资源路径、枚举和引用字段保持英文；增加静态边界检查，防止正式 Def 的可翻译字段重新出现纯英文默认值。

**Tech Stack:** RimWorld 1.6 XML、PowerShell 回归检查、Git。

---

### Task 1: 建立失败的默认中文边界检查

**Files:**
- Create: `Source/BDP.Tests/FormalDefDefaultChineseTextSmokeTests.ps1`

**Steps:**
1. 扫描 `1.6/Content/Defs` 的 `label`、`description`、`DisplayLabel`、`jobString`、`gerund`、`reportString` 及常见玩家文本字段。
2. 忽略已经包含中文的中英混合文本，只拒绝纯英文默认文本。
3. 运行脚本，确认报告当前 28 处英文。

### Task 2: 将28处默认文本改为中文

**Files:**
- Modify: `1.6/Content/Defs/AbilityDef/CombatBodyShortJump.xml`
- Modify: `1.6/Content/Defs/AbilityDef/Grasshopper.xml`
- Modify: `1.6/Content/Defs/AbilityDef/LightSoulPropulsion.xml`
- Modify: `1.6/Content/Defs/ChipActionPresetDef/Presets.xml`
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`
- Modify: `1.6/Content/Defs/HediffDef/LightSoul.xml`
- Modify: `1.6/Content/Defs/ThingDef/Items/Chips.xml`
- Modify: `1.6/Content/Defs/ThingDef/Items/InvalidChipRemnant.xml`
- Modify: `1.6/Content/Defs/ThingDef/PawnFlyer_BounceSlash.xml`
- Modify: `1.6/Content/Defs/ThingDef/PawnFlyer_CombatBodyShortJump.xml`
- Modify: `1.6/Content/Defs/ThingDef/PawnFlyer_Grasshopper.xml`

**Steps:**
1. 优先复用简体中文 DefInjected（定义注入语言包）或 Keyed（键值语言包）中的既有中文。
2. 将内部飞行器和 Verb 标签改为简短、明确的中文。
3. 更新误导性的“默认英文由语言包覆盖”注释。
4. 运行边界检查，确认没有纯英文玩家文本。

### Task 3: 关联验证与提交

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志46.md`

**Steps:**
1. 运行光魂、能力、飞行器和制造本地化关联测试。
2. XML 全量解析并执行 `git diff --check`。
3. 记录工作日志。
4. 提交默认中文文本改动。
