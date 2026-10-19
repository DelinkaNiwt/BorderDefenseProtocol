# 触发器装配台图表几何绘制 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将触发器装配台右侧详情的能力雷达图和精度曲线从粗线段模拟升级为可裁切的细致三角几何绘制，同时保持现有数据和原版分段精度语义不变。

**Architecture:** 在 `BDP.Content` 的装配台窗口目录新增 UI 专用 `ChartGeometryRenderer`。它在 `EventType.Repaint`（重绘事件）中使用 Unity `GL.TRIANGLES`（三角形绘制）提交填充三角扇和由三角形组成的细线带，并用 `GUIUtility.GUIToScreenPoint`（GUI 坐标到屏幕坐标转换）适配当前窗口和 UI 缩放。详情渲染器继续负责数值归一化、职业标尺、原版精度插值和颜色语义，只把几何提交改为新绘制器。

**Tech Stack:** C# 7.3、RimWorld 1.6 `Assembly-CSharp`、Unity `GL` / `Material` / `GUIUtility`、现有 PowerShell 冒烟测试、MSBuild。

**Working rules:** 不创建 worktree，不创建新分支；直接在当前项目执行。现有 `1.6/Assemblies/BDP.Content.dll` 和 `.pdb` 的未提交改动属于用户，编译前后只在确认属于本次构建产物时处理，不回退其它文件。

---

### Task 1: 建立图表几何绘制失败契约测试

**Files:**

- Create: `Source/BDP.Tests/ChartGeometryRendererSmokeTests.ps1`
- Inspect: `Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`

**Step 1: Write the failing test**

测试要求：

- `ChartGeometryRenderer.cs` 存在；
- 绘制器使用 `GL.TRIANGLES`、屏幕坐标转换、重绘事件判断；
- 绘制器提供填充多边形、细折线和裁切入口；
- 雷达图和精度曲线调用新绘制器；
- 雷达主体和精度曲线主体不再直接调用 `Widgets.DrawLine`；
- 详情渲染器不再使用 `DrawRadarSoftArea` 的中心放射粗线面积模拟。

**Step 2: Run the test to verify it fails**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChartGeometryRendererSmokeTests.ps1
```

Expected: FAIL because `ChartGeometryRenderer.cs` does not exist and当前雷达/曲线仍调用 `Widgets.DrawLine`。

**Step 3: Commit**

```powershell
git add -- Source\BDP.Tests\ChartGeometryRendererSmokeTests.ps1
git commit -m "test: 增加装配台图表几何绘制契约"
```

### Task 2: 实现可裁切的 UI 三角几何绘制器

**Files:**

- Create: `Source/BDP.Content/Assembly/Window/ChartGeometryRenderer.cs`

**Step 1: Implement the minimal renderer**

实现以下成员：

- `DrawFilledPolygon(Rect clipRect, IReadOnlyList<Vector2> points, Color color)`：用三角扇填充凸多边形，先对多边形执行矩形视口裁切；
- `DrawPolyline(Rect clipRect, IReadOnlyList<Vector2> points, Color color, float width, bool dashed = false)`：把每一段转换为四边形，再以两个三角形提交，宽度不再经过 `Widgets.DrawLine` 的三倍放大；
- `DrawLine`、`DrawCircle` 和 `DrawCross`：提供图表网格、端点和超界标记需要的细几何；
- `ClipPolygon` 和线段裁切辅助：保证超界真实比例不会绘出图表视口；
- `BeginGeometry` / `EndGeometry`：只在重绘事件调用，使用 `SolidColorMaterials.SimpleSolidColorMaterial`（原版纯色材质缓存）和 `GL.LoadPixelMatrix`，每次绘制恢复矩阵与材质状态。

所有 C# 成员逐成员添加中文注释；不保存窗口状态，不引用 Core 或业务对象，不创建每帧永久对象。

**Step 2: Run the contract test**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChartGeometryRendererSmokeTests.ps1
```

Expected: 仍可能因详情调用尚未迁移而 FAIL；绘制器自身契约应已被识别，记录剩余失败项。

### Task 3: 迁移雷达图绘制

**Files:**

- Modify: `Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`

**Step 1: Replace the radar geometry calls**

在 `DrawClippedRadarGeometry` 中保持中心点、半径、轴顺序、声明值/最终值、超界比例和颜色不变，但改为：

- 网格四层使用新绘制器的细多边形轮廓；
- 轴线使用细直线几何；
- 声明值和最终值分别使用三角扇半透明填充与细轮廓；
- 增减变化使用细实线/虚线；
- 超界叉标记使用细几何；
- 删除 `DrawRadarSoftArea` 的中心到顶点粗线模拟。

不改变 `RadarPoint` 的真实超界比例计算；视口裁切统一由绘制器完成。

**Step 2: Run the contract test**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChartGeometryRendererSmokeTests.ps1
```

Expected: 雷达主体相关断言 PASS；若曲线主体仍是旧实现，只保留对应失败。

**Step 3: Commit**

```powershell
git add -- Source\BDP.Content\Assembly\Window\ChartGeometryRenderer.cs Source\BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs Source\BDP.Tests\ChartGeometryRendererSmokeTests.ps1
git commit -m "feat: 使用几何绘制雷达图"
```

### Task 4: 迁移精度曲线和图表标记

**Files:**

- Modify: `Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`

**Step 1: Replace the curve geometry calls**

- 保留 `InterpolateAccuracy` 的原版四段线性插值；
- 继续用 0 到 50 格采样，改为一次连续细折线几何提交；
- 声明值使用细虚线，最终值使用细实线；
- 四个关键距离使用几何圆点或十字标记，不再用粗线段叠加；
- 图例中的线段也改用同一绘制器，避免图表主体细化后图例仍显粗。

**Step 2: Run the contract test**

Run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChartGeometryRendererSmokeTests.ps1
```

Expected: 图表几何契约测试 PASS。

**Step 3: Commit**

```powershell
git add -- Source\BDP.Content\Assembly\Window\ChipInformationDetailRenderer.cs Source\BDP.Tests\ChartGeometryRendererSmokeTests.ps1
git commit -m "feat: 使用几何绘制精度曲线"
```

### Task 5: 编译、边界验证和游戏内复测准备

**Files:**

- Modify if required by build output: `1.6/Assemblies/BDP.Content.dll`
- Modify if required by build output: `1.6/Assemblies/BDP.Content.pdb`
- Modify: `日志/Agent工作日志/` 下当前倒序工作日志文件，保持单文件不超过20条

**Step 1: Run static and boundary tests**

至少运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\ChartGeometryRendererSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\TriggerAssemblerDetailRendererContractSmokeTests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Source\BDP.Tests\TriggerAssemblyWindowSmokeTests.ps1
```

**Step 2: Build Content.dll**

```powershell
msbuild Source\BDP.Content\BDP.Content.csproj /t:Build /p:Configuration=Release /p:Platform=AnyCPU
```

Expected: exit code 0，无 C# 编译错误；输出程序集只反映本次 Content 构建。

**Step 3: Check architecture and diff**

确认：

- `ChartGeometryRenderer` 只位于 Content UI；
- Core.dll 没有新增引用；
- 图表数据和插值逻辑没有改变；
- 图表主体没有回退到 `Widgets.DrawLine`；
- `git diff --check` 通过；
- 原有用户未提交的程序集变更没有被错误回退。

**Step 4: Write the reverse chronological work log**

记录新绘制器、雷达迁移、曲线迁移、测试和编译结果；若当前日志已有20条，创建下一份日志文件。

**Step 5: Game verification handoff**

需要实际进游戏确认：三轴近战雷达、五轴远程雷达、曲线端点、超界裁切、透明填充、不同 UI 缩放，以及滚动窗口内图形是否仍在卡片边界内。未进行游戏内确认前，不声称最终视觉验收完成。

