# BDP 芯片详情旧版图表表现复刻 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在不修改现有信息模型的前提下，用现有 Core 通用图表设施复刻旧版芯片详情雷达图、精度折线图和护盾角度扇形图的视觉与图中文字。

**Architecture:** Content 继续从当前 `ChipInformationNode` / comparison 数据组装图表数据；Core 图表工具补足通用的多边形边界裁切与独立 X 轴刻度格式能力。旧版的轴顺序、量程、颜色、填充、刻度、锚点、扇形几何与标注回到面板调用层配置，不恢复旧信息模型或旧 Content 专属绘制器。

**Tech Stack:** C#、RimWorld Verse/Unity GUI、BDP Core 中性图表设施、PowerShell 源码契约检查、MSBuild。

---

### Task 1: 固化旧版表现契约

**Files:**
- Modify: `Source/BDP.Tests/NeutralChartToolsContractSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ChipDetailsSharedRendererSmokeTests.ps1`

**Step 1:** 增加源码契约，覆盖旧版雷达指标顺序与裁边、精度距离锚点与 0/3/12/25/40/50 刻度、护盾扇形填充/参考圆/角度标注；确保信息模型定义文件不进入改动范围。

**Step 2:** 运行两项 PowerShell 检查，确认新契约在实现前失败。

**Step 3:** 记录失败断言所指向的缺失行为，不通过放宽断言来消除失败。

### Task 2: 扩展中性通用图表绘制能力

**Files:**
- Modify: `Source/BDP/Core/UI/Charts/ChartDrawingContext.cs`
- Modify: `Source/BDP/Core/UI/Charts/ChartGeometryRenderer.cs`
- Modify: `Source/BDP/Core/UI/Charts/RadarChartRenderer.cs`
- Modify: `Source/BDP/Core/UI/Charts/ChartStyle.cs`（仅在确认折线 X 轴需要独立刻度格式时）
- Modify: `Source/BDP/Core/UI/Charts/LineChartRenderer.cs`（仅在确认折线 X 轴需要独立刻度格式时）

**Step 1:** 给通用绘图上下文增加可选凸多边形裁切入口，让多边形填充和折线可以限制在雷达边界内；保持默认矩形裁切行为。

**Step 2:** 让雷达序列轮廓和填充都按雷达多边形边界裁切，轴与网格仍使用现有几何路径。

**Step 3:** 若现有 `ChartStyle` 无法分别格式化 X/Y 刻度，则仅增加中性的 X 刻度格式配置，并保持当前默认值与既有调用兼容。

**Step 4:** 运行两项源码契约检查和 Core 编译。

### Task 3: 按旧版规则配置当前信息模型数据

**Files:**
- Modify: `Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`

**Step 1:** 用旧版顺序和职业量程从现有信息节点构造雷达数据；保留超量程实际数值，交由 Core 雷达边界裁切，并恢复旧版青色双层填充、基准虚线和文字位置/颜色。

**Step 2:** 将当前精度锚点映射到距离 3/12/25/40，生成 0–50 的插值曲线；仅在四个原始锚点画点和百分比标签，并恢复旧版坐标刻度。

**Step 3:** 将护盾图改为 112×112 旧版几何：36 段参考圆、按角度填充扇区、两条边界射线、居中角度及本地化标签；保持当前比较/提示数据来源，不叠画旧式基准扇形。

**Step 4:** 移除图内多余的当前/基准图例、基准数值系列标注及其它不属于旧版的图中文字；通过现有本地化键或模型标签解析显示业务标签，不硬编码玩家文案。

**Step 5:** 运行源码契约检查并编译 Content/Core，检查信息模型文件确实无改动。

### Task 4: 回看差异并提交

**Files:**
- Review only: 本计划涉及的 Core、Content、测试文件
- Create/Modify: `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志\Agent工作日志-20260929-BDP图表旧版表现复刻.md`

**Step 1:** 检查最终 diff，确认只涉及通用图表工具、面板绘图调用、契约检查和本次计划/日志。

**Step 2:** 若本地游戏环境可用，使用同一芯片对照旧版截图验证布局、文字、曲线和颜色；不可用时明确记录未完成的游戏内视觉核对。

**Step 3:** 将本次变更涉及文件逐个显式暂存，提交一个便于回退的中文说明提交；保留工作区其他既有改动不动。
