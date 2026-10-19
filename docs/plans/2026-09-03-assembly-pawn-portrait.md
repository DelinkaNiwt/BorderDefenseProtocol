# 装配台操作者实际 Pawn 肖像 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在装配台左栏使用 RimWorld 原版渲染管线绘制操作者当前外观的正面运行时肖像，不生成 PNG，也不显示伤口等健康状态贴图。

**Architecture:** 保留战斗变身使用的 128×128 Pawn 快照逻辑不变，新增装配台专用的正面肖像捕获入口。窗口拥有并缓存肖像快照，在打开后捕获一次、关闭时释放；左栏只负责把透明 RenderTexture 绘制到中央区域。

**Tech Stack:** C# 7.3、RimWorld PawnCacheRenderer、Unity RenderTexture、Verse Window/GUI。

---

### Task 1: 写正面肖像契约测试

**Files:**
- Create: `Source/BDP.Tests/AssemblyPawnPortraitSmokeTests.ps1`

**Step 1: Write the failing test**

验证捕获入口使用 `Rot4.North` 和 `angle: 0f`，关闭 head wound/健康覆盖类渲染标志，窗口持有并释放快照，且不再调用矢量人物绘制器。

**Step 2: Run test to verify it fails**

Run: `pwsh -NoProfile -File Source/BDP.Tests/AssemblyPawnPortraitSmokeTests.ps1`

Expected: FAIL，因为装配台仍调用 `OperatorFigureRenderer.Draw`，且不存在装配台正面肖像入口。

### Task 2: 实现装配台专用 Pawn 肖像捕获

**Files:**
- Create: `Source/BDP.Content/Assembly/Window/AssemblyPawnPortraitCapture.cs`
- Modify: `Source/BDP.Content/CombatBody/Transform/CombatBodyPawnVisualSnapshot.cs`

**Step 1: Implement minimal code**

使用原版 `Find.PawnCacheRenderer.RenderPawn` 捕获透明运行时纹理；固定正面朝向和直立角度；使用肖像渲染路径并关闭伤口相关渲染标志；资源尺寸独立为 256×256。

**Step 2: Run test to verify it passes**

Run: `pwsh -NoProfile -File Source/BDP.Tests/AssemblyPawnPortraitSmokeTests.ps1`

Expected: PASS。

### Task 3: 接入装配台窗口和左栏

**Files:**
- Modify: `Source/BDP.Content/Assembly/Window/Window_TriggerAssembly.cs`
- Modify: `Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs`
- Delete or retire: `Source/BDP.Content/Assembly/Window/OperatorFigureRenderer.cs`

**Step 1: Implement minimal code**

窗口首次绘制时懒捕获肖像并缓存，`PostClose` 释放；将快照传入左栏，在原中央人物区域以 `GUI.DrawTexture` 绘制，并保持透明背景和比例适配。

**Step 2: Run source smoke test and build**

Run: `pwsh -NoProfile -File Source/BDP.Tests/AssemblyPawnPortraitSmokeTests.ps1`

Run: `dotnet build Source/BDP.Content/BDP.Content.csproj --no-restore`

Expected: smoke test PASS，BDP.Content 编译退出码为 0。

### Task 4: 回归验证、日志和提交

**Files:**
- Modify: `日志/Agent工作日志/2026-09-03.md`（若当日文件达到 20 条则新建日志文件）

**Step 1: Run focused existing tests**

运行装配台、战斗人物视觉和构建边界相关 smoke tests，确认原有战斗变身捕获仍使用旧逻辑。

**Step 2: Review diff**

确认没有写入 PNG、没有修改 Pawn 实际朝向/健康状态、没有把 UI 业务放入 Core.dll。

**Step 3: Commit**

```bash
git add Source/BDP.Content Source/BDP.Tests docs/plans 日志/Agent工作日志
git commit -m "feat: render assembly operator pawn portrait"
```
