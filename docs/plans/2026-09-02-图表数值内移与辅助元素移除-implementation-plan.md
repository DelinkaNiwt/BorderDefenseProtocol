# 图表数值内移与辅助元素移除 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将雷达和精度曲线的实际值移入图内，并移除用户红框标出的图例、范围提示和插值说明。

**Architecture:** 继续使用 `ChipInformationDetailRenderer` 的现有图表绘制路径，不改动数据模型、数值计算、雷达裁切或卡片固定高度。雷达复用已有轴标签位置显示当前值；精度曲线复用已有当前锚点，在锚点附近增加数值文本。

**Tech Stack:** C#（C#语言）、RimWorld 1.6 Verse/Unity IMGUI（图形界面）、PowerShell 冒烟测试、`dotnet msbuild`。

---

### Task 1: 更新失败契约测试

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/ChartGeometryRendererSmokeTests.ps1`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/TriggerAssemblerDetailRendererContractSmokeTests.ps1`

**Step 1: Write the failing test**

- 要求雷达主体包含图内当前值标注入口，并不再包含 `DrawRadarValuesBelow`。
- 要求精度卡片包含图内锚点值标注入口，并不再包含下方锚点键、`DrawComparisonLegend`、`AccuracyRange`、`AccuracyNote`。
- 要求详情契约不再把旧的下方数值网格和统一图例视为必需结构。

**Step 2: Run test to verify it fails**

运行：

```powershell
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File '模组工程/BorderDefenseProtocol/Source/BDP.Tests/ChartGeometryRendererSmokeTests.ps1'
```

预期：FAIL，因为现有代码仍绘制旧表格、图例和说明，且没有图内数值标注方法。

### Task 2: 移除雷达旧数值区和图例

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs:32-40,1179-1319`
- Modify: `模组工程/BorderDefenseProtocol/Languages/ChineseSimplified (简体中文)/Keyed/Windows.xml:64-78`

**Step 1: Write minimal implementation**

- 删除 `DrawRadarCard` 对 `DrawRadarValuesBelow`、`MeasureRadarValueGridHeight` 和 `DrawComparisonLegend` 的调用。
- 删除仅服务于旧雷达表格/图例的私有方法和常量。
- 从语言包删除雷达比较图例、精度范围、精度图例和精度插值说明键。

### Task 3: 增加雷达图内当前值

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs:1440-1480`

**Step 1: Write minimal implementation**

- 扩展 `DrawRadarAxisLabels`：保留轴名称，在相同轴标签位置第二行绘制 `FormatAxisValue(axis.Current, axis.IsPercentage)`。
- 轴名称使用现有 `BaseColor`，当前值使用 `AccentColor`，超限当前值使用 `WarningColor`。
- 保持现有外侧标签位置和雷达几何，不显示固定上限，避免把数值表重新搬回图边。

### Task 4: 增加精度曲线图内当前值并移除旧底部内容

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs:1488-1610`

**Step 1: Write minimal implementation**

- 删除右上角范围提示、底部四组标签/数值、比较图例和底部插值说明。
- 新增 `DrawAccuracyPointValues`，在四个当前锚点附近绘制 `FormatPercentValue(point.Current)`。
- 标签默认放在锚点上方；锚点靠近图表顶部时放到下方，并限制水平坐标在图表视口内。
- 保留当前/基础曲线、差异线、坐标轴和距离刻度。

### Task 5: 全量验证并记录

**Files:**
- Create or Modify: `日志/Agent工作日志/Agent日志105.md`

**Step 1: Run tests and build**

```powershell
$testRoot='模组工程/BorderDefenseProtocol/Source/BDP.Tests'
$tests=@('ChartGeometryRendererSmokeTests.ps1','TriggerAssemblerDetailRendererContractSmokeTests.ps1','TriggerAssemblyWindowSmokeTests.ps1','UnifiedModAssemblyLayoutSmokeTests.ps1','TriggerAssemblyContentBoundarySmokeTests.ps1')
foreach($test in $tests){ & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $testRoot $test); if($LASTEXITCODE -ne 0){ exit $LASTEXITCODE } }
dotnet msbuild '模组工程/BorderDefenseProtocol/Source/BDP.Content/BDP.Content.csproj' /t:Build /p:Configuration=Release /p:Platform=AnyCPU
if($LASTEXITCODE -ne 0){ exit $LASTEXITCODE }
git diff --check
```

预期：5 个测试 PASS，`BDP.Content` Release 构建成功，差异检查无错误。

**Step 2: Commit**

```powershell
git add -- <源码> <测试> <语言包> <工作日志>
git commit -m "feat: 将图表实际数值移入图内"
```

只提交源码、测试、语言包、计划/日志；构建生成的 DLL/PDB 按现有项目状态保留，不纳入本次提交。
