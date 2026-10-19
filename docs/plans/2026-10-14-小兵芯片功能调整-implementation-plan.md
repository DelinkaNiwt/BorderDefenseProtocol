# 小兵芯片功能调整 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 小兵弹丸改用 BDP 普通弹；小兵短剑改为两连击、单次威力减半。

**Architecture:** 仅修改公共目录中的芯片 Def（定义）和简体中文语言条目，复用现有投射物、攻击执行机制，不增加 C#（C#语言）代码。

**Tech Stack:** RimWorld Def XML（游戏定义可扩展标记语言）、简体中文 DefInjected（定义本地化）。

---

### Task 1：更新小兵芯片定义

**Files:**
- Modify: `Common/Content/Defs/ChipManufacturingPresetDef/NearRealm.xml`

**Steps:**
1. 小兵弹丸改用 `BDP_Projectile_Normal`，保留自动手枪的射程、预热、冷却和精度。
2. 小兵短剑设置 `<HitCount>2</HitCount>`，三种工具威力改为 4.5、6、6.5。
3. 同步修订相邻 XML 注释。

### Task 2：更新简体中文说明与工作日志

**Files:**
- Modify: `Languages/ChineseSimplified (简体中文)/DefInjected/BDP.Content.Assembly.ChipManufacturing.Defs.ChipManufacturingPresetDef/NearRealm.xml`
- Create: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent工作日志-20261014-BDP小兵芯片功能调整.md`

**Steps:**
1. 更新小兵弹丸说明，写明 BDP 普通弹及其伤害、穿甲、停止力、速度。
2. 更新小兵短剑说明，写明每次攻击命中两次、单次威力减半。
3. 记录 1.6、1.7 的共享内容检查结果和未进行的游戏内实测。

### Task 3：检查 XML 并提交

**Steps:**
1. 用 PowerShell XML（可扩展标记语言）解析器解析修改后的定义与语言文件。
2. 运行 `git diff --check` 检查差异格式。
3. 确认 1.6、1.7 均从 `Common` 加载该定义；本次不改程序集，因此不运行 C# 构建或自动化测试，也不做游戏内实测。
4. 只暂存本计划中的文件并提交，不纳入工作区其他未跟踪文件。
