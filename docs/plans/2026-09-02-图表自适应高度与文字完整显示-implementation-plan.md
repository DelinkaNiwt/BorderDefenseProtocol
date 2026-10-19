# 图表自适应高度与文字完整显示 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让装配台雷达/精度图卡片按实际图表内容收紧高度，并让雷达长标签与精度纵轴刻度完整显示。

**Architecture:** 保留现有图表视口和几何绘制器，只把卡片外框高度从固定常量改为绘制/测量共用的内容高度函数。文本继续使用 RimWorld IMGUI 的局部裁切，但矩形宽度由 `TextWidth` 或明确的纵轴刻度边距提供，并限制在卡片内部。

**Tech Stack:** C#（C#语言）、RimWorld 1.6 Verse/Unity IMGUI（图形界面）、PowerShell 冒烟测试、`dotnet msbuild`。

---

### Task 1: 更新失败契约

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ChartGeometryRendererSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblerDetailRendererContractSmokeTests.ps1`

**Step 1: Write the failing test**

- 要求 `MeasureRadarCardHeight`、`MeasureAccuracyCardHeight` 存在。
- 禁止 `RadarCardHeight`、`AccuracyCardHeight` 作为固定卡片高度常量。
- 要求雷达标签使用 `TextWidth`，精度图存在独立纵轴刻度宽度和绘图区左边距。

**Step 2: Run test to verify it fails**

```powershell
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File '模组工程/BorderDefenseProtocol/Source/BDP.Tests/ChartGeometryRendererSmokeTests.ps1'
```

预期：FAIL，因为现有代码仍直接使用 `RadarCardHeight`、`AccuracyCardHeight`，雷达标签宽度固定且精度纵轴区域只有 20f。

### Task 2: 将通道布局改为共用内容高度

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs:32-45,718-915`

**Step 1: Write minimal implementation**

- 新增雷达/精度卡片标题、底部内边距常量和 `MeasureRadarCardHeight`、`MeasureAccuracyCardHeight`。
- `DrawRangedChannel`、`MeasureRangedChannel`、`DrawMeleeToolPair`、`MeasureMeleeToolPair` 统一调用测量方法。
- 用测量结果创建卡片矩形并推进游标，避免绘制高度与滚动内容高度分叉。
- 不改变雷达/精度图视口高度。

### Task 3: 修复雷达长标签的实际宽度

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs:1300-1340`

**Step 1: Write minimal implementation**

- 使用 `TextWidth` 测量轴名称和当前值的最大宽度，设置最小视觉宽度。
- 将标签矩形水平位置限制在 `plotRect` 范围内，避免完整文字穿透卡片边界或被局部组裁切。

### Task 4: 修复精度图纵轴刻度

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs:1350-1490`

**Step 1: Write minimal implementation**

- 增加纵轴刻度宽度、绘图区左边距和右边距常量。
- 将 `origin.x`、`topRight.x`、纵轴刻度矩形宽度和 `AccuracyX` 统一使用这些边距。
- 保证 `0%`、`50%`、`100%` 在独立的足够宽度内右对齐显示。

### Task 5: 验证、记录和提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志104.md`（若条目不超过 20 条）或 Create: `日志/Agent工作日志/Agent日志105.md`

**Step 1: Run tests and build**

```powershell
$testRoot='模组工程/BorderDefenseProtocol/Source/BDP.Tests'
$tests=@('ChartGeometryRendererSmokeTests.ps1','TriggerAssemblerDetailRendererContractSmokeTests.ps1','TriggerAssemblyWindowSmokeTests.ps1','UnifiedModAssemblyLayoutSmokeTests.ps1','TriggerAssemblyContentBoundarySmokeTests.ps1')
foreach($test in $tests){ & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $testRoot $test); if($LASTEXITCODE -ne 0){ exit $LASTEXITCODE } }
dotnet msbuild '模组工程/BorderDefenseProtocol/Source/BDP.Content/BDP.Content.csproj' /t:Build /p:Configuration=Release /p:Platform=AnyCPU
if($LASTEXITCODE -ne 0){ exit $LASTEXITCODE }
git diff --check
```

预期：5 个测试 PASS、Release 构建成功、差异检查无错误；最终留白和文字完整性需游戏内截图确认。

**Step 2: Commit**

```powershell
git add -- <源码> <测试> <工作日志>
git commit -m "feat: 收紧图表布局并修复刻度显示"
```

只提交源码、测试和工作日志；构建产生的 DLL/PDB 保留为工作树产物，不纳入提交。
