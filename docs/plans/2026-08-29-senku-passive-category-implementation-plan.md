# 旋空被动类别修正 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将正式旋空芯片模板的画像类别修正为被动类，使现有被动芯片运行时按预期识别旋空。

**Architecture:** 只修正 Content 层正式 `ChipFunctionDef` 的 XML 数据，不新增运行时状态、分类接口或组合技逻辑。现有 `Kind=Passive`、手动激活链、同侧互斥豁免和组合技候选规则继续复用。

**Tech Stack:** RimWorld 1.6、XML Def、PowerShell 冒烟测试、`dotnet msbuild`。

---

### Task 1: 为正式旋空类别增加失败回归断言

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP.Tests\PassiveChipActivationAndComboAdmissionSmokeTests.ps1`

**Steps:**

1. 读取正式 `1.6/Content/Defs/ChipFunctionDef/Presets.xml`。
2. 定位 `BDP_ChipFunction_Senku` 节点。
3. 断言该节点的 `Profile/Category` 为 `BDP_ChipCategory_Passive`，同时保留 `Kind=Passive`、`PassiveKey` 和 `MainSub`。
4. 运行该烟测；在 XML 尚未修改时应因类别仍为 `BDP_ChipCategory_Ability` 而失败。

### Task 2: 修改正式旋空 Def 类别

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\1.6/Content/Defs/ChipFunctionDef/Presets.xml`

**Steps:**

1. 只修改 `BDP_ChipFunction_Senku` 的 `Profile/Category`。
2. 将 `BDP_ChipCategory_Ability` 改为 `BDP_ChipCategory_Passive`。
3. 不改变旋空其它字段和其它芯片模板。

### Task 3: 通过配置与构建验证

**Commands:**

```powershell
pwsh -NoProfile -File Source/BDP.Tests/PassiveChipActivationAndComboAdmissionSmokeTests.ps1
dotnet msbuild Source/BDP.Content/BDP.Content.csproj -p:Configuration=Release -p:UseSharedCompilation=false -t:Build -v:minimal
git diff --check
```

预期：烟测通过，Content Release 构建通过，差异检查无输出。

### Task 4: 更新工作日志并提交

**Files:**

- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志\Agent日志95.md`；提交前确认条目数未超过20条，超出则创建下一个日志文件。

只提交本次旋空类别修正涉及的 XML、烟测、计划文档和工作日志；保留工作区已有的构建产物修改，不提交 DLL/PDB。
