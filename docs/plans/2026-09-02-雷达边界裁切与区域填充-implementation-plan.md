# 雷达边界裁切与区域填充 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将雷达图的超限几何裁切到最外层多边形，并增加克制的半透明闭合区域填充。

**Architecture:** 在现有 `ChartGeometryRenderer` 中复用矩形裁切流程，新增凸多边形裁切阶段；雷达调用方传入最外层雷达环作为边界。保留当前/声明值数据语义，只改变几何输出边界和填充颜色。

**Tech Stack:** C# 7.3、RimWorld 1.6、Unity GL 三角形、PowerShell 冒烟契约、MSBuild。

---

### Task 1: 增加失败契约

**Files:**
- Modify: `Source/BDP.Tests/ChartGeometryRendererSmokeTests.ps1`

**Step 1: Write the failing test**

增加对凸多边形裁切入口、雷达传入外层边界、以及两种半透明填充颜色的源码契约。

**Step 2: Run test to verify it fails**

Run: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source/BDP.Tests/ChartGeometryRendererSmokeTests.ps1`

Expected: FAIL，因为当前绘制器只有矩形裁切，雷达调用没有传入外层边界。

### Task 2: 实现凸多边形裁切

**Files:**
- Modify: `Source/BDP.Content/Assembly/Window/ChartGeometryRenderer.cs`

**Step 1: Write minimal implementation**

让填充、折线、圆点和十字标记的几何裁切流程接受可选凸多边形边界；使用边界有向面积确定内侧方向，逐边裁切，避免假设雷达点顺逆时针方向。

**Step 2: Run test to verify it passes**

重新运行图表契约，确认入口和裁切逻辑存在。

### Task 3: 迁移雷达调用并设置底色

**Files:**
- Modify: `Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`

**Step 1: Write minimal implementation**

生成一次最外层雷达环，作为当前值、声明值、轮廓、差异线和超限十字的裁切边界；将当前值填充调整为可见但低饱和的青绿色半透明层，声明值填充保持更暗、更低透明度。

**Step 2: Run test to verify it passes**

运行图表和详情渲染契约，确认雷达主体继续保留原数据调用。

### Task 4: 构建和收尾验证

**Files:**
- Modify: `日志/Agent工作日志/Agent日志103.md` 或下一个日志文件（每个文件最多 20 条）

**Step 1: Run verification**

依次运行图表、装配台详情、装配台窗口、程序集布局和 Content 边界测试；再运行 `dotnet msbuild Source/BDP.Content/BDP.Content.csproj /t:Build /p:Configuration=Release /p:Platform=AnyCPU` 和 `git diff --check`。

**Step 2: Commit**

提交源码、契约、设计/计划和工作日志；保留构建生成的 `1.6/Assemblies/BDP.Content.dll/.pdb` 为可测试产物，不重置工作区原有二进制改动。
