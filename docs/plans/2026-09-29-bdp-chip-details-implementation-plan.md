# BDP 芯片详情面板实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标：** 让制造台预览和触发器装配台详情使用同一套游戏原生芯片详情面板，并按已确认规则展示层次、字段和统计图。

**实现方式：** 继续以现有 `ChipInformationModel` 为信息来源，在 Content 展示层实现玩家向筛选和绘制。制造台把成品名、当前模型及共用比较基准交给详情渲染器；装配台继续交实例详情。树关系直接沿 `ChipInformationNode.Children` 遍历。Core 信息采集与解析规则不改。

**技术栈：** C# 7.3、RimWorld `Verse.Widgets`、Unity 几何绘制、PowerShell smoke tests。

---

## 顺序与完成标准

依次完成数据入口、信息层次、图表和回归验证。每个阶段先运行对应的失败测试，再做最小实现并重跑测试。最终需要 Content 编译；编译不等于游戏内视觉验收，能启动游戏时必须看真实面板，无法目视时明确交给用户用画面确认。

### 任务 1：锁定两处共用渲染器的契约

**文件：**
- 新建：`Source/BDP.Tests/ChipDetailsSharedRendererSmokeTests.ps1`
- 后续更新：`Source/BDP.Tests/TriggerAssemblerDetailRendererContractSmokeTests.ps1`
- 后续更新：`Source/BDP.Tests/ChipManufacturingPreviewSmokeTests.ps1`

**当前进度：** 新增 smoke test 已运行并按预期失败：“制造台必须使用同一详情渲染器”。这是已验证的 RED，不是脚本错误。

**步骤：**

1. 保留测试对两处入口、上下文递归、图表子项、范围环/扇形、无内部键、无空项提示和无独立构型指标的断言。
2. 运行：`pwsh -NoProfile -File Source/BDP.Tests/ChipDetailsSharedRendererSmokeTests.ps1`。
3. 后续每一项实现后重跑该脚本；更新旧契约测试，移除“可选择上下文标签”的过期预期。

### 任务 2：将制造台解析结果交给共用渲染器

**文件：**
- 修改：`Source/BDP.Content/Assembly/Window/ChipAssemblyDetailModelBuilder.cs`
- 修改：`Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipManufacturingPreviewModel.cs`
- 修改：`Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipManufacturingPreviewBuilder.cs`
- 修改：`Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipManufacturingPreviewPanel.cs`
- 必要时修改：`Source/BDP.Content/Assembly/ChipManufacturing/UI/Window_ChipManufacturing.cs`

**步骤：**

1. 在 `ChipAssemblyDetailModelBuilder` 增加一个接受已解析制造结果的内部入口，返回 `ChipInformationComparisonModel`；Current 与 Baseline 使用现有制造信息构建器和既有默认基准解析，不复制规则。
2. 将比较模型和成品显示名放进制造预览模型；无效组合继续显示已有状态文本。
3. 制造预览面板复用 `ChipInformationDetailRenderer.MeasureContentHeight` 与 `DrawContent`，让当前暗青背景、边框、标题和详情卡片直接沿用装配台风格。
4. 删除制造台专属的规格条、构型介绍/修正、模式 DTO 及其映射代码；保留共享详情调用和无效状态。
5. 更新制造台契约测试，确认只剩一条玩家详情展示路径。

### 任务 3：按已确认结构绘制信息树

**文件：**
- 修改：`Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`
- 修改：`Source/BDP.Content/Assembly/Window/Panel_ChipDetail.cs`
- 修改：`Source/BDP.Tests/TriggerAssemblerDetailRendererContractSmokeTests.ps1`
- 修改：`Source/BDP.Tests/ChipDetailsSharedRendererSmokeTests.ps1`

**步骤：**

1. 把 renderer 入口改为接收玩家向芯片名与比较模型；移除 `Root.Key` 身份文本以及选中上下文状态/点击切换。
2. 概览区只展示已确定的共性信息：构型名/构型简介不展示；激活要求按 `condition.<稳定键>.required/reference` 聚合成任意条数的行，跳过 `type` 与空值。
3. 根级功能之后，递归绘制每个 `context` 节点及其直接功能、子形态/姿态；全部默认展开，顺序沿用信息树。
4. 功能仍按“图表、功能直接属性、功能组、附属效果/组合声明结果”的层次绘制；不输出键名兜底，也不画“无属性”占位。
5. 过滤已明确排除的目标类型、视线要求、远程飞行特征、攻击计时，以及概览中不展示的槽位/占用/激活停用延迟。属性缺失不创建占位。

### 任务 4：把统计图表放回所属功能并整行绘制

**文件：**
- 修改：`Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`
- 视需要修改：`Languages/ChineseSimplified (简体中文)/Keyed/Windows.xml`
- 修改：`Source/BDP.Tests/ChipDetailsSharedRendererSmokeTests.ps1`

**步骤：**

1. 用当前/Baseline 树的对应功能字段组装图表；沿用旧图已确认的颜色、线条、填充、固定上限、裁切及超限数字颜色，不画溢出标记。
2. 远程雷达轴为伤害、护甲穿透、有效距离、弹速、停止力、RPM；职业标尺沿用既有枪手/射手与狙击手方案：普通上限依次为 `30、50%、40格、150、4、180 RPM`，狙击上限依次为 `60、50%、80格、200、4、180 RPM`。
3. RPM 使用射击次数、预热、冷却、连射间隔及齐射/连射节奏计算；子弹数量与射击节奏照常作为图外结构化字段显示。
4. 四距离精度点为近身、近距、中距、远距既有字段，按现有距离节点绘制独立整行趋势图。当前四点不全时不画残图。
5. 近战雷达轴为单次伤害、护甲穿透、APM、攻击距离；每次命中按一次攻击计。预热、冷却和命中间隔只参与计算、不显示。
6. 当前雷达必需值不齐时省略雷达；Baseline 缺项时只绘当前值图形。每一张雷达/趋势图均独占所在详情栏宽度。

### 任务 5：添加护盾扇形和非攻击距离环

**文件：**
- 修改：`Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`
- 修改：`Source/BDP.Tests/ChipDetailsSharedRendererSmokeTests.ps1`

**步骤：**

1. 有 `BlockArc` 时，在原所属护盾信息行旁绘制有实际尺寸的角度扇形，并保留数值。
2. 对远程/近战攻击以外、同时有有效距离和最小距离的功能，绘制同心范围环；仅有一个距离值时仍按普通字段显示。
3. 用测试断言仅这些语义入口调用对应的原生小图示绘制，不向其他功能推断距离。

### 任务 6：运行回归、编译并获取真实反馈

**验证文件：**
- `Source/BDP.Tests/ChipDetailsSharedRendererSmokeTests.ps1`
- `Source/BDP.Tests/TriggerAssemblerDetailRendererContractSmokeTests.ps1`
- `Source/BDP.Tests/ChipManufacturingPreviewSmokeTests.ps1`
- `Source/BDP.Tests/ChipInformationComparisonBaselineSmokeTests.ps1`

**步骤：**

1. 运行四个关联 PowerShell smoke tests，并执行 `git diff --check`。
2. 构建 `Source/BDP.Content/BDP.Content.csproj`，确认实际使用的模组程序集输出已更新。
3. 当前没有运行中的 RimWorld 进程。构建后检查本机是否能启动并进入面板；若无合适存档/场景或当前工具不能观察游戏窗口，则不声称游戏内验收完成，交付程序集并请用户提供实际画面。
4. 在 `C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志` 写一条倒序工作日志；只暂存本任务文件并提交，不包含已有脏文件。
