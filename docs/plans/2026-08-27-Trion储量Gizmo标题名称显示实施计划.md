# Trion储量 Gizmo 标题名称显示 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在 Trion 资源 Gizmo 标题中显示宿主小人的短名称，最终文案为 `Trion储量（小人短名称）`。

**Architecture:** 保持现有 `Gizmo_TrionStatus` 的通用 `Thing owner` 边界，只在标题绘制层读取 `owner.LabelShortCap`。标题文案继续从简体中文语言包读取，通过现有 `BDP_Trion_Status_Title` 语言键传入名称参数；不改变 Trion 真值、读写口、资源条或扩展面板。

**Tech Stack:** C# 7.3、RimWorld/Verse `Gizmo` 与 `Widgets`、简体中文 keyed language XML、PowerShell smoke tests、现有 BDP 构建脚本。

---

### Task 1: 增加标题名称的失败烟测

**Files:**
- Create: `Source/BDP.Tests/TrionGizmoTitleNameSmokeTests.ps1`
- Reference: `Source/BDP/Core/Trion/Gizmo_TrionStatus.cs`
- Reference: `Languages/ChineseSimplified (简体中文)/Keyed/StatsReport.xml`

**Step 1: Write the failing test**

增加静态断言：

- `Gizmo_TrionStatus` 标题区域读取 `owner.LabelShortCap`。
- 标题调用 `BDP_Trion_Status_Title` 时传入短名称。
- 语言键内容必须为 `Trion储量（{0}）`。

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File ".\Source\BDP.Tests\TrionGizmoTitleNameSmokeTests.ps1"
```

Expected: FAIL，因为当前标题没有读取 `owner.LabelShortCap`，语言键也没有 `{0}` 参数。

**Step 3: Commit**

```powershell
git add Source/BDP.Tests/TrionGizmoTitleNameSmokeTests.ps1
git commit -m "test: lock trion gizmo title pawn name"
```

### Task 2: 参数化 Trion储量标题并复用短名称

**Files:**
- Modify: `Languages/ChineseSimplified (简体中文)/Keyed/StatsReport.xml:16`
- Modify: `Source/BDP/Core/Trion/Gizmo_TrionStatus.cs:301-326`

**Step 1: Update the language key**

把 `BDP_Trion_Status_Title` 从 `Trion储量` 改为 `Trion储量（{0}）`。

**Step 2: Update the title drawing**

在 `DrawTitleRow` 中使用：

```csharp
string ownerLabel = owner != null ? owner.LabelShortCap : string.Empty;
Widgets.Label(titleLabelRect, "BDP_Trion_Status_Title".Translate(ownerLabel));
```

保持标题矩形、徽标位置、字体、锚点和其余绘制流程不变。

**Step 3: Run the focused test**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File ".\Source\BDP.Tests\TrionGizmoTitleNameSmokeTests.ps1"
```

Expected: PASS。

**Step 4: Commit**

```powershell
git add Source/BDP/Core/Trion/Gizmo_TrionStatus.cs Languages/ChineseSimplified (简体中文)/Keyed/StatsReport.xml
git commit -m "feat: show pawn name in trion gizmo title"
```

### Task 3: 做针对性回归与构建验证

**Files:**
- Test: `Source/BDP.Tests/TrionGizmoTitleNameSmokeTests.ps1`
- Test: `Source/BDP.Tests/TrionGizmoPanelLayoutSmokeTests.ps1`
- Build: `Source/BDP/BDP.csproj`
- Build: `Source/BDP.Content/BDP.Content.csproj`

**Step 1: Run focused and layout smoke tests**

```powershell
powershell -ExecutionPolicy Bypass -File ".\Source\BDP.Tests\TrionGizmoTitleNameSmokeTests.ps1"
powershell -ExecutionPolicy Bypass -File ".\Source\BDP.Tests\TrionGizmoPanelLayoutSmokeTests.ps1"
```

Expected: 两项均 PASS。

**Step 2: Compile the current BDP assemblies**

运行以下工程现有构建命令，编译 `BDP.Core.dll` 与 `BDP.Content.dll`：

```powershell
dotnet build ".\\Source\\BDP\\BDP.csproj" --configuration Release --no-restore
dotnet build ".\\Source\\BDP.Content\\BDP.Content.csproj" --configuration Release --no-restore
```

确认 `Thing.LabelShortCap`、语言键参数和 XML 均可正常处理。

Expected: 构建退出码为 0，且没有新增编译错误。

**Step 3: Review the final diff**

确认只包含标题语言包、Gizmo 标题逻辑、聚焦烟测、实施文档和工作日志；不包含 Core/Content 依赖方向变化或资源真值改动。

**Step 4: Record the work log**

在 `日志/Agent工作日志` 中记录本次改动、测试和构建结果；不把已有未跟踪日志混入功能提交。
