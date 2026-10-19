# BDP 芯片详情面板视觉设计优化

## 目标

触发器装配台和芯片制造台共用同一套芯片详情渲染器（`ChipInformationDetailRenderer.cs`）。信息层次（Feature→Groups/Subgroups→Children，形态/姿态上下文树）已经和功能语义模型完全对应，问题在纯视觉设计层面：全程只有一套"深青底+青绿标题条"反复复制粘贴，玩家看面板时没有主次停靠点。本次只改视觉表现，不改信息模型、不改语义规则、不改业务逻辑。

## 已确认事实

- 身份区（`DrawGeneralIdentity`）与窗口固定头部（`Panel_ChipDetail.DrawPanelFrame`）重复显示同一句"芯片详情"标题（`BDP_Window_TriggerAssembly_DetailPanelTitle`），真正该突出的芯片名反而只是普通小字。
- 5 个语义功能（`ChipFeatureDef`：远程攻击/近战攻击/护盾/机动/通用能力）没有色彩身份，翻卡片只能读标题文字辨认类型。
- 带标题方框有三套皮肤并存：`DrawSectionFrame`（整块信息区）、`DrawChartFrame`（距离环）、`DrawLegacyChartFrame`（雷达图/精度图），且多处"框中再套框"。
- 精度折线图（175f，绘图区仅 138f）与雷达图（244f）叠加，一个远程攻击功能的图表部分能占满窗口一半高度。
- 属性行标签与数值同色同权重，扫视找不到重点数字。
- 形态/姿态递归用"逐层套框+缩进"表达，深两三层后视觉上叠成方框套娃。

## 设计体系：三层视觉权重

- **第一层·唯一强区（身份）**：芯片图标+芯片名，无框，全篇视觉权重最高，只出现一次。
- **第二层·功能卡片**：每个语义功能保留独立描边卡片，左侧加语义色标；图表子容器统一同一套边框/标题皮肤。
- **第三层·轻量列表/嵌套线**：概览、组合关系、功能区/上下文区外层标题从整块加框降级为无框小标题；形态/姿态递归改用左侧竖线+缩进表达深度，不再逐层套框。

## 具体改动与依据

1. **身份区重做**：删除内容区重复标题；芯片名提升到 `GameFont.Medium`；旁边加图标——装配台用真实 `Thing`（`Widgets.ThingIcon`），制造台草稿用常量成品定义回退（`ChipProductCreationService.ManufacturedChipDefName` + `Widgets.DefIcon`），因为所有动态成品共用同一个 `ThingDef`，这不是伪造数据。
2. **语义功能色标**：新增 `DefinitionKey → Color` 映射（远程青/近战暖橙/护盾蓝紫/机动绿/能力紫），卡片左侧加 3px 色带，标题文字同色。手法沿用 `Panel_ChipInventory.DrawChipCard` 里已有的左侧色带绘制方式，不是新发明。
3. **图表边框统一**：`DrawChartFrame`/`DrawLegacyChartFrame` 合并为一个方法（底色/边框沿用图表专用的 `ChartBackgroundColor`/`ChartBorderColor`）。护盾扇形图不新增标题文案，维持"角度值画在图形中心"的既有克制设计。
4. **精度图压缩**：`AccuracyChartHeight`（175f→131f）/`AccuracyPlotHeight`（138f→100f）/`AccuracyTitleHeight`（29f→23f）。雷达图保持独占整行不变——它的轴标签围绕圆心 360 度分布，横向压缩会挤坏标签，且信息密度最高，不做并排。
5. **属性数值着色**：`GetFeatureLineText`/`GetFieldText` 只给数值部分套用 `ColoredText.Colorize(AccentColor)`，标签保持原色。已用 DecompilerServer 核实 `Verse.Text` 的 `GUIStyle` 继承自 `GUI.skin.label`，未显式关闭 `richText`（走 Unity 默认值 true）；原版程序集 154 处 `Colorize` 调用点里包含 `Thing.SpecialDisplayStats` 等同类场景，证明这个用法在原版体系里成立。
6. **嵌套层次改竖线**：形态/姿态递归（`DrawContextTree`/`MeasureContextTree`）从整块套框改为左侧引导线（`DrawNestingGuide`，用 `Widgets.DrawBoxSolid` 画 1px 矩形而非 `Widgets.DrawLineVertical`，因为后者不支持传色）+ 缩进，线条随深度加深。引导线做上下内缩、柔化透明度处理，呼应项目里 `Gizmo_TrionStatus.DrawSoftDivider`"禁止满高度亮线"的既有克制审美。

### 范围收缩说明

原计划里"信息组/子组同样改竖线"这一条在实现前重新核对代码后发现前提不成立：`DrawFeatureGroup`/`DrawFeatureGroup` 当前本来就没有画方框，只用缩进+变色（子组用浅灰绿、顶层组用深青）表达层级。它们最多一层嵌套（Core 校验强制禁止子组再嵌套子组），不是"套娃"问题的来源；真正的套娃来自形态/姿态递归（深度不限），已经改完。为这里额外加竖线只会引入新的宽度计算和 Measure/Draw 不同步风险，收益边际，因此维持现状，不做改动。

### 连带清理

`DrawSectionFrame` 的四处剩余调用点（概览区/组合关系区/功能区/上下文区的外层标题）全部替换为新增的无框轻量方法 `DrawCaption`，`DrawSectionFrame` 本体删除。对应的四个 Measure 方法（`MeasureFeatureSection`/`MeasureGeneralBlocks`/`MeasureContexts`/`MeasureRelationSection`）同步把打底高度从 `HeaderHeight + 5f`（33f）改成 `23f`，避免测量高度和实际绘制高度不一致。`HeaderHeight` 常量因此在该文件内无剩余引用，一并删除。

修复过程中发现 `DrawText` 现有签名只支持 4 参数（无 `TextAnchor`），身份区需要垂直居中文字，给其加了一个带默认值 `TextAnchor.UpperLeft` 的可选参数，不破坏原有调用点。

## 验证

- 构建 `BDP.Content.csproj`（Debug）：0 警告、0 错误。
- 六项相关 smoke test 全部 PASS：`ChipDetailsSharedRendererSmokeTests`、`ChipManufacturingPreviewSmokeTests`、`ChipInformationComparisonBaselineSmokeTests`、`TriggerAssemblerChipDetailPanelSmokeTests`、`TriggerAssemblerConsumptionAndRadarLabelsSmokeTests`、`NeutralChartToolsContractSmokeTests`。
- 游戏内视觉效果（功能色标辨识度、引导线清晰度、数值着色换行、精度图压缩后可读性）需要实际进游戏确认，构建通过不代表视觉正确。
