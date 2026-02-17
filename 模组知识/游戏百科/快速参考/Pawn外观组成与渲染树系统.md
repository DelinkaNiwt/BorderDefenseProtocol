---
标题：Pawn外观组成与渲染树系统
版本号: v1.0
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld中Pawn外观的完整组成分析——基于PawnRenderTree树形渲染架构，涵盖：Humanlike渲染树静态结构（XML定义的14个节点4层嵌套）、5种动态节点注入器（Apparel/Genes/Hediffs/Traits/Mutants）、外观数据源（Pawn_StoryTracker的7个核心字段：bodyType/headType/hairDef/hairColor/skinColorBase/furDef/melanin）、15个PawnRenderNode子类职责、baseLayer层级排序（Body=0→StatusOverlay=100）、颜色系统（Skin/Hair/Custom三种colorType）、SetAllGraphicsDirty刷新机制、模组扩展点（CompRenderNodes+自定义Node子类），含源码引用表
---

# Pawn外观组成与渲染树系统

**总览**：Pawn的外观由**PawnRenderTree**（渲染树）管理，采用**树形节点架构**。每个视觉元素（身体、头部、头发、胡须、衣服、纹身等）是树上的一个`PawnRenderNode`节点。渲染树分两部分构建：**静态节点**由XML中的`PawnRenderTreeDef`定义，**动态节点**由5种`DynamicPawnRenderNodeSetup`子类在运行时注入。外观数据（体型、头型、发型、肤色等）存储在`Pawn_StoryTracker`中。

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    Humanlike Pawn 渲染树完整结构                          │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Root (PawnRenderNode_Parent)                                            │
│  ├── Body (PawnRenderNode_Body) ─── baseLayer=0                          │
│  │   ├── Body Tattoo (Ideology) ─── baseLayer=2                          │
│  │   ├── Wounds pre-apparel ─── baseLayer=8                              │
│  │   ├── Baby Swaddle (Biotech) ─── baseLayer=10                         │
│  │   ├── ★ ApparelBody (动态挂载点) ─── baseLayer=20                     │
│  │   │   └── [DynamicSetup_Apparel 注入身体衣物节点]                      │
│  │   ├── Wounds post-apparel ─── baseLayer=30                            │
│  │   └── Firefoam ─── baseLayer=40                                       │
│  │                                                                       │
│  ├── Head Stump (PawnRenderNode_Stump) ─── baseLayer=50                  │
│  │                                                                       │
│  ├── Head (PawnRenderNode_Head) ─── baseLayer=50                         │
│  │   ├── Head Tattoo (Ideology) ─── baseLayer=52                         │
│  │   ├── Beard (PawnRenderNode_Beard) ─── baseLayer=60                   │
│  │   ├── Hair (PawnRenderNode_Hair) ─── baseLayer=62                     │
│  │   ├── Head Wounds ─── baseLayer=65                                    │
│  │   ├── ★ ApparelHead (动态挂载点) ─── baseLayer=70                     │
│  │   │   └── [DynamicSetup_Apparel 注入头部衣物节点]                      │
│  │   ├── Firefoam ─── baseLayer=85                                       │
│  │   └── Status Overlay ─── baseLayer=100                                │
│  │                                                                       │
│  └── Carried (武器/携带物)                                                │
│                                                                          │
│  ★ 动态注入（运行时添加到对应parentTag下）：                               │
│  ├── DynamicSetup_Apparel → 穿戴的每件衣物                                │
│  ├── DynamicSetup_Genes → 有视觉效果的活跃基因（含Fur）                    │
│  ├── DynamicSetup_Hediffs → 有视觉效果的Hediff                            │
│  ├── DynamicSetup_Traits → 有视觉效果的特性                               │
│  └── DynamicSetup_Mutants → 变异体覆盖层                                  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

## 1. 外观数据源：Pawn_StoryTracker

Pawn的外观数据集中存储在`Pawn_StoryTracker`（`pawn.story`）中，7个核心字段：

| # | 字段 | 类型 | 说明 | 决定什么 |
|---|------|------|------|---------|
| 1 | `bodyType` | `BodyTypeDef` | 体型 | 身体贴图路径、头部偏移、伤口锚点 |
| 2 | `headType` | `HeadTypeDef` | 头型 | 头部贴图路径、发型/胡须网格尺寸 |
| 3 | `hairDef` | `HairDef` | 发型 | 头发贴图 |
| 4 | `hairColor` | `Color` | 发色 | 头发、胡须、毛皮的着色 |
| 5 | `skinColorBase` | `Color?` | 肤色基础值 | 身体、头部的着色 |
| 6 | `furDef` | `FurDef` | 毛皮定义 | 毛皮覆盖层贴图（基因驱动） |
| 7 | `melanin` | `float` | 黑色素值 | 肤色计算的备用来源 |

**BodyTypeDef（体型）**——6种原版体型：

| 体型 | defName | 适用 | 特殊 |
|------|---------|------|------|
| 男性 | `Male` | 成年男性默认 | — |
| 女性 | `Female` | 成年女性默认 | — |
| 瘦削 | `Thin` | 随机分配 | 体型较小 |
| 壮硕 | `Hulk` | 随机分配 | 体型最大 |
| 肥胖 | `Fat` | 随机分配 | — |
| 婴儿 | `Baby` | Biotech婴儿 | 专用渲染 |
| 儿童 | `Child` | Biotech儿童 | 缩放比例不同 |

每个`BodyTypeDef`定义了：`bodyNakedGraphicPath`（裸体贴图路径）、`bodyDessicatedGraphicPath`（干尸贴图）、`headOffset`（头部偏移量）、`bodyGraphicScale`（缩放）、`woundAnchors`（伤口锚点列表）。

**HeadTypeDef（头型）**——按性别和宽窄分类，核心字段：`graphicPath`（贴图路径）、`gender`（性别限制）、`narrow`（窄脸标记）、`hairMeshSize`/`beardMeshSize`（发型/胡须网格尺寸）、`requiredGenes`（需要特定基因才可用）。

## 2. PawnRenderNode子类（15种）

每种视觉元素对应一个`PawnRenderNode`子类，负责解析自己的贴图（`GraphicFor`）、颜色（`ColorFor`）、着色器（`ShaderFor`）：

| # | 子类 | 职责 | GraphicFor核心逻辑 |
|---|------|------|--------------------|
| 1 | `PawnRenderNode_Body` | 身体 | 干尸→变异体→creepjoiner→`story.bodyType.bodyNakedGraphicPath` |
| 2 | `PawnRenderNode_Head` | 头部 | 无头→null，干尸→Skull，正常→`story.headType.GetGraphic()` |
| 3 | `PawnRenderNode_Hair` | 头发 | `story.hairDef.GraphicFor()`，婴儿/新生儿跳过 |
| 4 | `PawnRenderNode_Beard` | 胡须 | `story.beardDef`（类似Hair） |
| 5 | `PawnRenderNode_Fur` | 毛皮覆盖 | `story.furDef.GetFurBodyGraphicPath()`，颜色用HairColor |
| 6 | `PawnRenderNode_Apparel` | 衣物 | `ApparelGraphicRecordGetter.TryGetGraphicApparel()` |
| 7 | `PawnRenderNode_Tattoo` | 纹身 | Body/Head两个子类，Ideology DLC |
| 8 | `PawnRenderNode_Stump` | 断头桩 | 头部缺失时显示的残桩贴图 |
| 9 | `PawnRenderNode_Swaddle` | 襁褓 | Biotech婴儿包裹贴图 |
| 10 | `PawnRenderNode_AnimalPart` | 动物部件 | 动物渲染树的基础节点 |
| 11 | `PawnRenderNode_AttachmentHead` | 头部附件 | 头部附着物（如Anomaly寄生体） |
| 12 | `PawnRenderNode_Spastic` | 痉挛效果 | Anomaly痉挛视觉效果 |
| 13 | `PawnRenderNode_NociosphereSegment` | 痛觉球段 | Anomaly痛觉球实体的分段渲染 |
| 14 | `PawnRenderNode_TurretGun` | 炮塔武器 | 机械体背部炮塔 |
| 15 | `PawnRenderNode_Parent` | 容器节点 | 不渲染自身，仅作为子节点的挂载点（如Root、ApparelBody、ApparelHead） |

> **关键发现**：`PawnRenderNode_Parent`是纯容器节点——它不渲染任何贴图，仅通过`tagDef`提供命名挂载点（如`ApparelBody`、`ApparelHead`），供动态节点注入器定位父节点。

## 3. 动态节点注入系统

`PawnRenderTree.SetupDynamicNodes()`在渲染树构建时调用，遍历5种`DynamicPawnRenderNodeSetup`子类 + 所有`ThingComp.CompRenderNodes()`：

```csharp
// Verse.PawnRenderTree.SetupDynamicNodes() L375
private void SetupDynamicNodes()
{
    // 1. 遍历5种DynamicPawnRenderNodeSetup
    foreach (var setup in dynamicNodeTypeInstances)
    {
        if (setup.HumanlikeOnly && !pawn.RaceProps.Humanlike) continue;
        foreach (var (child, parent) in setup.GetDynamicNodes(pawn, this))
            AddChild(child, parent);
    }
    // 2. 遍历所有ThingComp（模组扩展点）
    foreach (ThingComp comp in pawn.AllComps)
    {
        List<PawnRenderNode> list = comp.CompRenderNodes();
        if (list != null)
            foreach (var item in list)
                if (ShouldAddNodeToTree(item?.Props))
                    AddChild(item, null);
    }
}
```

**5种动态注入器**：

| # | 注入器 | 数据源 | 注入条件 | 目标父节点 |
|---|--------|--------|---------|-----------|
| 1 | `DynamicSetup_Apparel` | `pawn.apparel.WornApparel` | 每件穿戴衣物 | ApparelHead 或 ApparelBody（按layer判定） |
| 2 | `DynamicSetup_Genes` | `pawn.genes.GenesListForReading` | `HasDefinedGraphicProperties` | 按props.parentTagDef |
| 3 | `DynamicSetup_Hediffs` | `pawn.health.hediffSet` | `HasDefinedGraphicProperties` | 按props.parentTagDef |
| 4 | `DynamicSetup_Traits` | `pawn.story.traits` | `HasDefinedGraphicProperties` | 按props.parentTagDef |
| 5 | `DynamicSetup_Mutants` | `pawn.mutant.Def` | 变异体激活时 | 按props.parentTagDef |

> **关键发现**：`CompRenderNodes()`是模组最灵活的扩展点——任何自定义`ThingComp`都可以通过重写此方法向渲染树注入任意节点，无需修改原版代码。

## 4. 层级排序（baseLayer）

`PawnRenderNodeProperties.baseLayer`决定节点的绘制顺序（值越大越靠前/越上层）。Humanlike渲染树的完整层级：

| baseLayer | 节点 | 说明 |
|-----------|------|------|
| 0 | Body | 身体（最底层） |
| 2 | Body Tattoo | 身体纹身（Ideology） |
| 8 | Body Wounds (pre) | 衣物下方的伤口 |
| 10 | Baby Swaddle | 婴儿襁褓（Biotech） |
| 20 | ApparelBody | 身体衣物挂载点 |
| 30 | Body Wounds (post) | 衣物上方的伤口 |
| 40 | Body Firefoam | 身体灭火泡沫 |
| 50 | Head Stump / Head | 断头桩 / 头部 |
| 52 | Head Tattoo | 头部纹身（Ideology） |
| 60 | Beard | 胡须 |
| 62 | Hair | 头发 |
| 65 | Head Wounds | 头部伤口 |
| 70 | ApparelHead | 头部衣物挂载点 |
| 85 | Head Firefoam | 头部灭火泡沫 |
| 100 | Status Overlay | 状态覆盖层（最顶层） |

动态注入的节点在其父节点的baseLayer基础上按注入顺序递增偏移，确保同层级内的多件衣物有确定的绘制顺序。

## 5. 颜色系统

`PawnRenderNodeProperties.colorType`（类型`AttachmentColorType`）决定节点的着色来源：

| colorType | 颜色来源 | 使用节点 |
|-----------|---------|---------|
| `Skin` | `pawn.story.SkinColor`（melanin计算或基因覆盖） | Body, Head, Stump, Tattoo |
| `Hair` | `pawn.story.HairColor`（基因或随机） | Hair, Beard, Fur |
| `Custom` | `props.color`字段直接指定 | 特殊效果节点 |

**肤色解析优先级**（`Pawn_StoryTracker.SkinColor`属性）：
1. `skinColorOverride`（基因强制覆盖，如Deathrested灰色）
2. `skinColorBase`（基因设定的基础肤色）
3. `melanin`值计算（传统随机肤色）

**发色**：`pawn.story.HairColor`，由基因或角色生成时随机决定。毛皮（Fur）也使用HairColor着色——原因是毛皮通常与发色协调。

## 6. 渲染刷新机制

当Pawn外观发生变化时（穿脱衣物、基因变化、Hediff添加/移除等），需要调用`SetAllGraphicsDirty()`触发完整重建：

```csharp
// Verse.PawnRenderer
public void SetAllGraphicsDirty()
{
    renderTree.SetDirty();           // 标记渲染树需要重建
    PortraitsCache.SetDirty(pawn);   // 清除肖像缓存
    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);  // 清除纹理图集
}
```

`PawnRenderTree.SetDirty()`将`Resolved`标记为false → 下次`EnsureInitialized()`时调用`TrySetupGraphIfNeeded()` → 重新从`PawnRenderTreeDef`构建rootNode → 重新调用`SetupDynamicNodes()`注入动态节点。

**触发SetAllGraphicsDirty的常见场景**：
- 穿脱衣物（`Pawn_ApparelTracker`）
- 基因添加/移除（`Pawn_GeneTracker`）
- Hediff添加/移除（`Pawn_HealthTracker`）
- 变异体转化/恢复（`Pawn_MutantTracker`）
- 体型/头型/发型变更
- 年龄阶段变化（婴儿→儿童→成人）

## 7. 渲染管线概览

```
PawnRenderer.RenderPawnInternal()
  └── PawnRenderTree.Draw(parms)
        ├── EnsureInitialized()          // 确保渲染树已构建
        │   └── TrySetupGraphIfNeeded()  // 如果dirty则重建
        │       ├── 创建rootNode（从PawnRenderTreeDef.root）
        │       ├── SetupDynamicNodes()  // 注入动态节点
        │       └── InitializeAncestors()
        ├── ParallelPreDraw(parms)       // 并行预处理（计算矩阵等）
        │   └── 遍历所有节点 → AppendRequests() → 生成drawRequests列表
        └── 遍历drawRequests
            └── Worker.PreDraw() → GenDraw.DrawMeshNowOrLater() → Worker.PostDraw()
```

每个`PawnRenderNode`通过`AppendRequests()`生成`PawnGraphicDrawRequest`（包含Graphic、Mesh、Material、Matrix），最终由`PawnRenderNodeWorker`控制实际绘制行为。

## 8. 模组扩展点总结

| 扩展方式 | 难度 | 说明 |
|---------|------|------|
| **XML定义新PawnRenderTreeDef** | 低 | 为自定义种族定义完整渲染树 |
| **自定义PawnRenderNode子类** | 中 | 重写`GraphicFor`/`ColorFor`/`ShaderFor`实现自定义贴图逻辑 |
| **自定义PawnRenderNodeWorker子类** | 中 | 重写`PreDraw`/`PostDraw`控制绘制行为（如翻转、隐藏、特效） |
| **ThingComp.CompRenderNodes()** | 中 | 通过Comp向渲染树注入任意节点（最灵活，无需Harmony） |
| **基因/Hediff的graphicData** | 低 | XML中为Gene/Hediff配置`graphicData`，自动由DynamicSetup注入 |
| **Harmony Patch** | 高 | Patch具体Node的`GraphicFor`等方法（社区常用但侵入性强） |

**社区模组实践**：
- **HAR (Humanoid Alien Races)**：自定义`AlienPawnRenderNode_BodyAddon`，为外星种族添加额外身体部件
- **VEF (Vanilla Expanded Framework)**：`PawnRenderNode_Omni`通用节点，支持多种附加视觉效果
- **Milira**：自定义Head/Hair/Apparel节点，实现独特的种族外观

## 9. 关键源码引用表

| 类 | 方法/字段 | 命名空间 | 行号 | 说明 |
|----|---------|---------|------|------|
| `PawnRenderTree` | `TrySetupGraphIfNeeded()` | Verse | L327 | 渲染树构建入口 |
| `PawnRenderTree` | `SetupDynamicNodes()` | Verse | L375 | 动态节点注入 |
| `PawnRenderTree` | `Draw()` | Verse | — | 渲染树绘制入口 |
| `PawnRenderTree` | `SetDirty()` | Verse | — | 标记需要重建 |
| `PawnRenderNode` | `GraphicFor()` | Verse | — | 贴图解析（虚方法） |
| `PawnRenderNode` | `ColorFor()` | Verse | — | 颜色解析（虚方法） |
| `PawnRenderNode` | `AppendRequests()` | Verse | — | 生成绘制请求 |
| `PawnRenderNode_Body` | `GraphicFor()` | Verse | L12 | 身体贴图：干尸→变异体→creepjoiner→bodyType |
| `PawnRenderNode_Head` | `GraphicFor()` | Verse | L22 | 头部贴图：无头→干尸→headType |
| `PawnRenderNode_Hair` | `GraphicFor()` | Verse | — | 头发贴图：hairDef |
| `PawnRenderNode_Fur` | `GraphicFor()` | Verse | — | 毛皮贴图：furDef |
| `PawnRenderNode_Apparel` | `GraphicFor()` | Verse | — | 衣物贴图：ApparelGraphicRecordGetter |
| `PawnRenderNodeProperties` | 类定义 | Verse | — | 30+配置字段（nodeClass/workerClass/tagDef/baseLayer/colorType等） |
| `DynamicPawnRenderNodeSetup_Apparel` | `GetDynamicNodes()` | Verse | L13 | 衣物动态注入 |
| `DynamicPawnRenderNodeSetup_Genes` | `GetDynamicNodes()` | Verse | — | 基因动态注入 |
| `DynamicPawnRenderNodeSetup_Hediffs` | `GetDynamicNodes()` | Verse | — | Hediff动态注入 |
| `Pawn_StoryTracker` | 类定义 | RimWorld | — | 外观数据存储（bodyType/headType/hairDef/hairColor/skinColorBase/furDef） |
| `PawnRenderer` | `SetAllGraphicsDirty()` | Verse | — | 触发完整渲染刷新 |
| `PawnRenderTreeDef` | Humanlike | Core XML | — | 静态渲染树定义（14个节点） |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-15 | 创建文档：Pawn外观组成与渲染树系统完整源码分析（PawnRenderTree树形架构+Humanlike渲染树14节点4层嵌套静态结构+5种DynamicPawnRenderNodeSetup动态注入器+Pawn_StoryTracker 7核心外观字段+BodyTypeDef 7体型+HeadTypeDef结构+15种PawnRenderNode子类职责表+baseLayer层级排序Body=0→StatusOverlay=100+颜色系统Skin/Hair/Custom三种colorType+肤色解析优先级+SetAllGraphicsDirty刷新机制+渲染管线流程+6种模组扩展点+社区模组实践HAR/VEF/Milira+源码引用表18项） | Claude Opus 4.6 |
