# 浮空根因修复 Implementation Plan

> **For Codex:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让浮空功能真正使用已设计的非屋顶飞行路径规则，并按 RimWorld 1.6 原版飞行绘制链同步角色、影子和名字标签。

**Architecture:** 保留 BDP 的 Hediff、GameComponent 和现有三态模式；在 RimWorld 1.6 的 `PathFinderMapData.ParameterizeGridJob` 入口把 BDP 浮空 PathGrid 送入实际路径计算，同时用带身份的轻量 `IPathGridCustomizer` 隔离原版网格缓存。始终浮空路径固定排除屋顶，移动浮空继续使用 Hybrid；视觉层改为仿照原版 `Pawn_DrawTracker.DrawPos`、`PawnRenderer.DrawShadowInternal` 的分层绘制，不修改 Pawn 的真实 `Position`。

**Tech Stack:** RimWorld 1.6.9676.17735、Harmony 2.3.6、C# 7.3、PowerShell 静态/程序集冒烟测试、MSBuild。

---

### Task 1: 建立回归测试并确认当前实现失败

**Files:**
- Modify: `Source/BDP.Tests/FloatingFlightGizmoSmokeTests.ps1`
- Modify: `Source/BDP.Tests/FloatingFlightPathFollowerSmokeTests.ps1`
- Modify: `Source/BDP.Tests/FloatingFlightPathGridSmokeTests.ps1`
- Modify: `Source/BDP.Tests/FloatingFlightReachabilitySmokeTests.ps1`
- Modify: `Source/BDP.Tests/FloatingFlightPresentationSmokeTests.ps1`
- Create: `Source/BDP.Tests/FloatingFlightNativePathGridSmokeTests.ps1`

**Step 1:** 增加语言描述必须传入当前模式和下一模式的断言。

**Step 2:** 增加始终浮空遇到屋顶节点时必须 `ResetToCurrentPosition` 并重新请求路径的断言。

**Step 3:** 增加 PathFinder 实际网格补丁、按路径种类隔离缓存、原版 `Pawn_DrawTracker.DrawPos` 和飞行影子材质的断言。

**Step 4:** 运行新增和修改后的测试，确认它们因当前源码缺少这些行为而失败。

### Task 2: 修复浮空按钮本地化参数

**Files:**
- Modify: `Source/BDP.Content/FloatingFlight/UI/FloatingFlightGizmoProvider.cs`

**Step 1:** 让 `BuildDescription` 接收当前模式和下一模式。

**Step 2:** 将两个本地化参数同时传给 `BDP_Command_FloatingFlight_Description`。

**Step 3:** 运行 Gizmo 测试。

### Task 3: 把 BDP 浮空网格接入 RimWorld 1.6 实际 PathFinder 作业

**Files:**
- Create: `Source/BDP.Content/FloatingFlight/Pathing/FloatingFlightPathGridCustomizer.cs`
- Create: `Source/BDP.Content/FloatingFlight/Patches/Patch_PathFinderMapData_ParameterizeGridJob_FloatingFlight.cs`
- Modify: `Source/BDP.Content/FloatingFlight/Patches/Patch_PathFinder_MakeDestination_FloatingFlight.cs`

**Step 1:** 实现无资源、可复用的路径网格身份包装器；保留已有第三方 `IPathGridCustomizer`，只增加 BDP 路径种类身份，避免原版 `MapGridRequest` 把普通、Hybrid、Always 网格混用。

**Step 2:** 在 `PathFinder.CreateRequest` 返回后，为 Hybrid/Always 请求注入对应包装器。

**Step 3:** 在 `PathFinderMapData.ParameterizeGridJob` 的 Postfix 中，把当前浮空上下文的 `PathGrid.Grid_Unsafe` 赋给实际 `PathGridJob.pathGridDirect`。

**Step 4:** 保持终点策略独立：中间路径使用浮空网格；最终停留格继续按原版合法停留规则和屋顶规则过滤。

**Step 5:** 运行 PathGrid、Reachability、PathFinder 网格测试和编译。

### Task 4: 修正始终浮空屋顶边界交接

**Files:**
- Modify: `Source/BDP.Content/FloatingFlight/Patches/Patch_PawnPathFollower_Steps_FloatingFlight.cs`

**Step 1:** 在 `Always` 模式实际飞行时，如果旧路径仍给出屋顶下一个格，不执行降落进入屋顶。

**Step 2:** 调用原版 `ResetToCurrentPosition`，保留目标并重新生成路径。

**Step 3:** 仅 `MoveOnly` 模式继续走现有屋顶降落交接逻辑。

**Step 4:** 运行路径跟随和兼容性测试。

### Task 5: 按原版飞行绘制链修复高度、影子和名字标签

**Files:**
- Modify: `Source/BDP.Content/FloatingFlight/Presentation/FloatingFlightPresentationUtility.cs`
- Modify: `Source/BDP.Content/FloatingFlight/Presentation/Patch_FloatingFlightPresentation.cs`

**Step 1:** 将 BDP 视觉偏移接入 `Pawn_DrawTracker.DrawPos`，让所有原版视觉消费者（包括名字标签）看到同一份偏移；不修改 `Pawn.Position`。

**Step 2:** 在 `PawnRenderer.DrawShadowInternal` 中仿照原版 `Pawn.Flying` 分支使用 `FlightShadowMaterial`、地面高度和 BDP 表现因子；不再简单抵消全部偏移。

**Step 3:** 保留倒地、死亡、离图时零偏移规则。

**Step 4:** 运行视觉测试并编译。

### Task 6: 完整验证、打包和日志

**Files:**
- Test: `Source/BDP.Tests/FloatingFlight*SmokeTests.ps1`
- Build: `Source/BDP.sln`
- Log: `日志/Agent工作日志/Agent日志*.md`

**Step 1:** 运行全部浮空相关 PowerShell 测试。

**Step 2:** 构建 Core、Content、Development Release，确认无新增编译错误。

**Step 3:** 检查程序集时间戳和版本基线，确认输出仍为 RimWorld 1.6 目录。

**Step 4:** 写入本次修复日志并提交精确改动文件。
