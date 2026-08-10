# DevHarness Trigger可视化改造计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在不污染主模组的前提下，为 `Trigger` 第一测试环节补齐“缩略摘要 + 详细诊断”两层可视化，提高游戏内判读效率。

**Architecture:** 只修改 `BorderDefenseProtocol.DevHarness`。缩略 `Gizmo` 只读取正式接口并负责一眼判断；详细窗口负责状态追因与名词辨析。主模组接口不新增测试专用能力，不把测试显示逻辑回流进正式 owner。

**Tech Stack:** C# 7.3、RimWorld Window/Gizmo API、主模组正式读取口与诊断口

---

### Task 1: 写入测试层可视化结构

**Files:**
- Create: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\Gizmo_TriggerLoadoutSummary.cs`
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\DevHarnessTriggerGizmoProvider.cs`

**Step 1: 先定义最小显示责任**

- 缩略 `Gizmo` 只显示三侧摘要
- 不承载正式业务裁定
- 点击后只打开详细诊断窗口

**Step 2: 实现最小代码**

- 使用 `ITriggerLoadoutReader` 和 `ITriggerIntegrityDiagnostics`
- 显示每侧：
  - 槽位数
  - 已装数
  - 正式激活槽位
  - 切换中标记
  - 禁用标记

**Step 3: 人工验证**

- 进入游戏后能在开发模式下看到新的缩略摘要面
- 点击摘要面能打开详细窗口

### Task 2: 重构详细诊断窗口信息分层

**Files:**
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\Window_TriggerLoadoutDiagnostics.cs`

**Step 1: 先定义失败标准**

- 如果窗口仍需要逐行读原始字段才能看懂当前状态，则改造不算完成
- 如果 `Special=0` 仍然表现为空白，容易误解为漏显示，则改造不算完成

**Step 2: 实现最小代码**

- 顶部增加名词辨析与总览摘要
- 左侧按 `Main / Sub / Special` 分块
- 每块先显示摘要，再显示槽位明细与切换态
- 把 `ThingOwner\`1` 等底层类型名翻译为测试者可读说明
- 右侧容器列表补“被哪侧哪槽引用”

**Step 3: 人工验证**

- 仅看窗口总览即可判断当前主副侧是否装载、是否激活、是否切换中
- 能区分容器索引与槽位索引

### Task 3: 编译与交付验证

**Files:**
- Modify: `C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\docs\99-会话交接\2026-03-24-工作推进-卷02.md`

**Step 1: 编译验证**

Run: `dotnet msbuild BDP.DevHarness.csproj -p:Configuration=Debug -t:Build -v:minimal`

Expected: `Build succeeded`

**Step 2: 架构复审**

- 确认改动只停留在 `DevHarness`
- 确认没有为 UI 再向主模组索要测试专用接口

**Step 3: 记录日志**

- 把本轮改造、验证结果、建议测试方法追加到工作日志
