# Trion Gizmo UI Refresh Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在不改变 `Trion / CombatBody / Trigger` 真值边界的前提下，把 `Gizmo_TrionStatus` 重绘成新的短卡片样式，并落地 4 槽位状态图标和断开式分隔线。

**Architecture:** 继续复用现有 `ITrionReader`、`Gizmo_TrionStatus` 和 Trion 扩展注册表，不新增新的 owner。主改动集中在 `Gizmo_TrionStatus` 的布局和绘制逻辑；外部系统状态仍通过扩展徽标提供，但 UI 层将把底部右侧状态区限制为最多 4 个图标，并统一用图标亮/暗表现状态。

**Tech Stack:** C# 7.3, RimWorld/Verse `Gizmo` 与 `Widgets`, PowerShell smoke tests

---

### Task 1: 锁定新版 Trion gizmo 视觉契约测试

**Files:**
- Modify: `Source/BDP.Tests/TrionGuiSmokeTests.ps1`
- Modify: `Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1`

**Step 1: Write the failing test**
- 增加断言：`Gizmo_TrionStatus` 标题必须为 `Trion能量`
- 增加断言：顶部不再显示旧的“可用 当前/总量 + 恢复/消耗”头部结构
- 增加断言：底部必须存在说明文本构建逻辑，数值格式使用 `F1`
- 增加断言：状态图标区最多 4 个槽位
- 增加断言：分隔线不再直接画满整条高度，而是存在断开式绘制逻辑

**Step 2: Run test to verify it fails**

Run:
```powershell
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- FAIL
- 失败点集中在旧头部结构、旧分隔线和未限制 4 槽位

**Step 3: Write minimal implementation notes**
- 先只改测试，不提前动生产代码

**Step 4: Run test again**
- 预期仍 FAIL，作为后续实现起点

**Step 5: Commit**
```powershell
git add Source/BDP.Tests/TrionGuiSmokeTests.ps1 Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1
git commit -m "test: lock refreshed trion gizmo ui contract"
```

### Task 2: 重构 Gizmo_TrionStatus 的整体布局

**Files:**
- Modify: `Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`

**Step 1: Write the failing test**
- 若 Task 1 还未覆盖，补断言：卡片高度与结构不再依赖“顶部信息行 + 下方扩展行”旧布局

**Step 2: Run test to verify it fails**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- FAIL because current `Gizmo_TrionStatus` still uses the old header and extension row layout

**Step 3: Write minimal implementation**
- 把卡片改成三段式：
  - 标题区
  - 更粗的主条区
  - 底部说明/图标区
- 保持 `GetWidth()` 受控，不让卡片过宽
- 调整主条 Y 位置到整体中下部
- 去掉条内居中大号文本

**Step 4: Run test to verify it passes**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- 相关布局断言 PASS

**Step 5: Commit**
```powershell
git add Source/BDP/Core/Trion/Gizmo_TrionStatus.cs Source/BDP.Tests/TrionGuiSmokeTests.ps1
git commit -m "feat: restructure trion gizmo card layout"
```

### Task 3: 改底部说明文本与数值格式

**Files:**
- Modify: `Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`
- Test: `Source/BDP.Tests/TrionGuiSmokeTests.ps1`

**Step 1: Write the failing test**
- 锁定底部左侧说明文本内容
- 锁定标题只保留 `Trion能量`
- 锁定显示数值统一 `F1`

**Step 2: Run test to verify it fails**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- FAIL because current code still formats parts of the UI with `F0` / `F2` and uses top-row text composition

**Step 3: Write minimal implementation**
- 把 `BuildRateText` / 顶栏文案拆成底部说明文本构建函数
- 标题固定写 `Trion能量`
- 可用量、恢复/消耗文案统一使用 `F1`
- 保留 tooltip 的正式资源明细，但可继续更详细

**Step 4: Run test to verify it passes**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- PASS on text-format assertions

**Step 5: Commit**
```powershell
git add Source/BDP/Core/Trion/Gizmo_TrionStatus.cs Source/BDP.Tests/TrionGuiSmokeTests.ps1
git commit -m "feat: move trion details to bottom info row"
```

### Task 4: 落地 4 槽位状态图标区

**Files:**
- Modify: `Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`
- Modify: `Source/BDP/Core/CombatBody/External/CombatBodyTrionGizmoExtensionProvider.cs`
- Modify: `Source/BDP/Core/Trion/External/TrionGizmoExtensionBadge.cs`
- Test: `Source/BDP.Tests/TrionGuiSmokeTests.ps1`

**Step 1: Write the failing test**
- 锁定 gizmo 底部右侧状态区只显示最多 4 个图标
- 锁定图标采用固定图形 + tint 亮/暗表达，而不是靠有无图标文本切换结构

**Step 2: Run test to verify it fails**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- FAIL because current code still按需追加徽标且未限制最大数量

**Step 3: Write minimal implementation**
- `CollectBadges()` 结果裁成最多 4 个
- 调整底部右侧图标区域宽度按 4 槽位预留
- 优先让 provider 返回正式图标或稳定的示意图形
- `Frozen` 等状态尽量也走图标式表达

**Step 4: Run test to verify it passes**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- PASS on max-slot and icon-state assertions

**Step 5: Commit**
```powershell
git add Source/BDP/Core/Trion/Gizmo_TrionStatus.cs Source/BDP/Core/CombatBody/External/CombatBodyTrionGizmoExtensionProvider.cs Source/BDP/Core/Trion/External/TrionGizmoExtensionBadge.cs Source/BDP.Tests/TrionGuiSmokeTests.ps1
git commit -m "feat: cap trion gizmo status badges to four slots"
```

### Task 5: 改断开式分隔线绘制

**Files:**
- Modify: `Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`
- Test: `Source/BDP.Tests/TrionGuiSmokeTests.ps1`

**Step 1: Write the failing test**
- 锁定分隔线绘制不再直接调用一条贯通的整高竖线
- 锁定存在“上短线 / 下短线”式绘制 helper

**Step 2: Run test to verify it fails**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- FAIL because current divider is still one full-height vertical line

**Step 3: Write minimal implementation**
- 用两个短矩形或两段竖线替代现有整高 divider
- 保持正式锁定边界和预测锁定边界的语义不变

**Step 4: Run test to verify it passes**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
```

Expected:
- PASS

**Step 5: Commit**
```powershell
git add Source/BDP/Core/Trion/Gizmo_TrionStatus.cs Source/BDP.Tests/TrionGuiSmokeTests.ps1
git commit -m "feat: draw split divider markers in trion bar"
```

### Task 6: 做针对性回归验证

**Files:**
- Test: `Source/BDP.Tests/TrionGuiSmokeTests.ps1`
- Test: `Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1`
- Test: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`

**Step 1: Run tests**

Run:
```powershell
& '.\Source\BDP.Tests\TrionGuiSmokeTests.ps1'
& '.\Source\BDP.Tests\TrionGeneGuiContractsSmokeTests.ps1'
& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'
```

Expected:
- PASS
- 不出现 `CompTrion` 越权依赖 CombatBody/Trigger 内部实现的回归

**Step 2: Review visuals**
- 手工检查文案、条粗细、图标拥挤度、4 槽位上限
- 若需要，仅做 UI 微调，不扩散到正式语义层

**Step 3: Commit**
```powershell
git add Source/BDP/Core/Trion/Gizmo_TrionStatus.cs Source/BDP/Core/CombatBody/External/CombatBodyTrionGizmoExtensionProvider.cs Source/BDP/Core/Trion/External/TrionGizmoExtensionBadge.cs Source/BDP.Tests/TrionGuiSmokeTests.ps1 Source/BDP.Tests/TrionGeneGuiContractsSmokeTests.ps1
git commit -m "feat: refresh trion gizmo card ui"
```
