---
标题：Pawn外观动态变化机制
版本号: v1.0
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld中Pawn外观动态变化的完整机制分析——基于SetAllGraphicsDirty统一刷新入口，涵盖：12大类触发场景（基因/Hediff/衣物/变异体/年龄/风格/特性/手术/复活/隐身/吞噬者/活动度）、40+调用点分布、渲染树惰性重建流程（SetDirty→EnsureInitialized→TrySetupGraphIfNeeded→SetupDynamicNodes）、5种动态节点注入器的重建行为、PawnRenderNode_Body/Head的GraphicFor优先级链（干尸→变异体→CreepJoiner→正常）、原版动态外观变化先例（变异体全身替换/基因毛皮覆盖/Hediff视觉附件/隐身透明/吞噬者膨胀）、模组扩展点（CompRenderNodes/自定义Node/Harmony补丁），含源码引用表
---

# Pawn外观动态变化机制

**总览**：RimWorld中Pawn外观的所有动态变化都通过一个统一入口——`PawnRenderer.SetAllGraphicsDirty()`——触发渲染树完整重建。代码中有**40+个调用点**分布在12大类场景中。渲染树采用**惰性重建**策略：`SetDirty()`仅标记脏位，下次`EnsureInitialized()`时才实际重建，避免同一帧内多次变化导致重复重建。

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    外观动态变化完整流程                                    │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  触发源（12大类）                                                         │
│  ├── 基因变化（添加/移除/激活/停用）                                       │
│  ├── Hediff变化（添加/移除/可见性变化）                                    │
│  ├── 衣物变化（穿戴/脱下）                                                │
│  ├── 变异体转化/恢复                                                      │
│  ├── 年龄阶段变化（婴儿→儿童→成人）                                       │
│  ├── 风格变化（发型/胡须/纹身）                                           │
│  ├── 特性变化（添加/移除）                                                │
│  ├── 手术（截肢/安装植入体）                                              │
│  ├── 复活                                                                │
│  ├── 隐身（HediffComp_Invisibility）                                     │
│  ├── 吞噬者（CompDevourer吞噬/消化/吐出）                                │
│  └── 活动度变化（CompActivity）                                           │
│           │                                                              │
│           ▼                                                              │
│  PawnRenderer.SetAllGraphicsDirty()                                      │
│  ├── renderTree.SetDirty()          ← 标记渲染树需要重建                  │
│  ├── SilhouetteUtility.NotifyGraphicDirty()  ← 清除轮廓缓存              │
│  ├── WoundOverlays.ClearCache()     ← 清除伤口覆盖缓存                   │
│  ├── PortraitsCache.SetDirty()      ← 清除肖像缓存                       │
│  └── GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty()  ← 清除纹理集  │
│           │                                                              │
│           ▼ （下次渲染时）                                                │
│  EnsureInitialized() → TrySetupGraphIfNeeded()                           │
│  ├── 创建rootNode（从PawnRenderTreeDef.root）                             │
│  ├── SetupDynamicNodes()  ← 重新注入5种动态节点 + CompRenderNodes         │
│  └── InitializeAncestors()                                               │
│           │                                                              │
│           ▼                                                              │
│  每个PawnRenderNode重新解析：                                             │
│  ├── GraphicFor(pawn)  ← 根据当前状态选择贴图                             │
│  ├── ColorFor(pawn)    ← 根据当前状态选择颜色                             │
│  └── ShaderFor(pawn)   ← 根据当前状态选择着色器                           │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

## 1. SetAllGraphicsDirty触发场景分类（12大类）

| # | 触发类别 | 调用位置 | 调用点数 | 典型场景 |
|---|---------|---------|---------|---------|
| 1 | **基因变化** | `Gene.PostAdd()`/`PostRemove()`, `Pawn_GeneTracker` | 3 | 基因添加/移除/Xenogerm植入 |
| 2 | **Hediff变化** | `Pawn_HealthTracker.AddHediff()`/`RemoveHediff()`, `HediffSet.DirtyCache()` | 4 | 添加/移除有视觉效果的Hediff |
| 3 | **衣物变化** | `Pawn_ApparelTracker` | 1 | 穿戴/脱下衣物 |
| 4 | **变异体** | `Pawn_MutantTracker.Turn()`/`Revert()` | 2 | 蹒跚者转化/恢复 |
| 5 | **年龄阶段** | `Pawn_AgeTracker`, `LifeStageWorker_HumanlikeChild/Adult` | 3 | 婴儿→儿童→成人 |
| 6 | **风格变化** | `Pawn_StyleTracker`, `Dialog_StylingStation` | 6 | 发型/胡须/纹身/肤色修改 |
| 7 | **特性变化** | `TraitSet.GainTrait()`/`RemoveTrait()` | 2 | 有视觉效果的特性添加/移除 |
| 8 | **手术** | `Recipe_RemoveBodyPart` | 1 | 截肢（影响头部渲染） |
| 9 | **复活** | `ResurrectionUtility` | 1 | 复活后恢复正常外观 |
| 10 | **隐身** | `HediffComp_Invisibility` | 1 | 隐身开始/结束切换着色器 |
| 11 | **吞噬者** | `CompDevourer` | 3 | 吞噬/消化/吐出改变体型 |
| 12 | **其他** | `CompActivity`, `Pawn_CreepJoinerTracker`, `Corpse`, `PawnGenerator` | 5+ | 活动度变化、CreepJoiner揭示、尸体腐烂、Pawn生成 |

**条件触发优化**：并非所有变化都无条件触发重建。Hediff和基因系统有条件检查：

```csharp
// Pawn_HealthTracker.AddHediff() L252-256
if (hediff.def.HasDefinedGraphicProperties)        // 仅有视觉效果的Hediff
    pawn.Drawer.renderer.SetAllGraphicsDirty();
// ...
if (hediff.def.HasDefinedGraphicProperties || hediff.def.forceRenderTreeRecache)
    pawn.Drawer.renderer.SetAllGraphicsDirty();     // 或强制刷新标记

// Gene.PostAdd() L69-71
if (def.HasDefinedGraphicProperties)                // 仅有视觉效果的基因
    pawn.Drawer.renderer.SetAllGraphicsDirty();
```

> **关键发现**：`HasDefinedGraphicProperties`是性能守门员——只有`renderNodeProperties`非空的Def才触发渲染树重建，大多数纯数值效果的基因/Hediff不会触发。

## 2. 渲染树重建流程

`SetAllGraphicsDirty()`通过`LongEventHandler.ExecuteWhenFinished()`延迟执行，确保在当前帧所有逻辑完成后才标记脏位：

```csharp
// PawnRenderer.SetAllGraphicsDirty() L671
public void SetAllGraphicsDirty()
{
    LongEventHandler.ExecuteWhenFinished(delegate
    {
        if (renderTree.Resolved)
        {
            renderTree.SetDirty();                                    // 1. 标记渲染树脏
            SilhouetteUtility.NotifyGraphicDirty(pawn);              // 2. 清除轮廓缓存
            WoundOverlays.ClearCache();                              // 3. 清除伤口覆盖
            PortraitsCache.SetDirty(pawn);                           // 4. 清除肖像缓存
            GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);// 5. 清除纹理图集
        }
    });
}
```

**重建时机**：`PawnRenderTree.EnsureInitialized()`在每次渲染前调用，检查`Resolved`标记。如果为false，调用`TrySetupGraphIfNeeded()`完整重建：

1. 从`PawnRenderTreeDef.root`创建新的`rootNode`
2. 调用`SetupDynamicNodes()`重新注入所有动态节点
3. 处理`tmpChildTagNodes`中的延迟子节点
4. 调用`InitializeAncestors()`建立祖先关系

> **关键发现**：渲染树重建是**全量重建**而非增量更新——每次SetDirty后，整棵树从头构建，所有动态节点重新注入。这意味着外观变化的"成本"是固定的，不随变化类型而异。

## 3. 动态节点重建行为

渲染树重建时，5种`DynamicPawnRenderNodeSetup`子类重新扫描Pawn当前状态，决定注入哪些节点：

| # | 注入器 | 扫描数据 | 注入条件 | 变化场景 |
|---|--------|---------|---------|---------|
| 1 | `DynamicSetup_Apparel` | `pawn.apparel.WornApparel` | 每件穿戴衣物（非武器） | 穿脱衣物 |
| 2 | `DynamicSetup_Genes` | `pawn.genes.GenesListForReading` | `Active && HasDefinedGraphicProperties` | 基因添加/移除/激活/停用 |
| 3 | `DynamicSetup_Hediffs` | `pawn.health.hediffSet.hediffs` | `Visible && HasDefinedGraphicProperties` | Hediff添加/移除 |
| 4 | `DynamicSetup_Traits` | `pawn.story.traits` | `!Suppressed && HasDefinedGraphicProperties` | 特性添加/移除/压制 |
| 5 | `DynamicSetup_Mutants` | `pawn.mutant.Def` | `IsMutant && HasDefinedGraphicProperties` | 变异体转化/恢复 |

**注入流程**（以基因为例）：
```csharp
// DynamicPawnRenderNodeSetup_Genes.GetDynamicNodes()
foreach (Gene g in pawn.genes.GenesListForReading)
{
    if (!g.Active || !g.def.HasDefinedGraphicProperties) continue;
    foreach (PawnRenderNodeProperties props in g.def.RenderNodeProperties)
    {
        if (tree.ShouldAddNodeToTree(props))
        {
            PawnRenderNode node = Activator.CreateInstance(props.nodeClass, pawn, props, tree);
            node.gene = g;                    // 关联基因实例
            yield return (node, parent: null); // 注入到渲染树
        }
    }
}
```

> **关键发现**：动态节点通过`Activator.CreateInstance`反射创建——`nodeClass`字段决定节点类型，模组可在XML中指定自定义`PawnRenderNode`子类。

## 4. PawnRenderNode的GraphicFor优先级链

渲染树重建后，每个节点通过`GraphicFor(pawn)`解析当前应显示的贴图。两个核心节点有多级优先级：

**PawnRenderNode_Body.GraphicFor()优先级**（从高到低）：

| 优先级 | 条件 | 贴图来源 | 场景 |
|--------|------|---------|------|
| 1 | `CurRotDrawMode == Dessicated` | `bodyType.bodyDessicatedGraphicPath` | 干尸 |
| 2 | `IsMutant && bodyTypeGraphicPaths非空` | `mutant.Def.GetBodyGraphicPath(pawn)` | 变异体（蹒跚者等） |
| 3 | `IsCreepJoiner && bodyTypeGraphicPaths非空` | `creepjoiner.form.GetBodyGraphicPath(pawn)` | CreepJoiner揭示 |
| 4 | 默认 | `story.bodyType.bodyNakedGraphicPath` | 正常状态 |

**PawnRenderNode_Head.GraphicFor()优先级**（从高到低）：

| 优先级 | 条件 | 贴图来源 | 场景 |
|--------|------|---------|------|
| 1 | `!hediffSet.HasHead` | `null`（不渲染） | 头部缺失 |
| 2 | `CurRotDrawMode == Dessicated` | `HeadTypeDefOf.Skull` | 干尸 |
| 3 | 默认 | `story.headType.GetGraphic()` | 正常状态 |

## 5. 原版动态外观变化先例

RimWorld原版有8种动态外观变化机制，按变化深度从深到浅：

| # | 先例 | DLC | 变化内容 | 技术实现 | 可逆性 |
|---|------|-----|---------|---------|--------|
| 1 | **变异体转化** | Anomaly | 全身贴图替换+覆盖层 | MutantDef.bodyTypeGraphicPaths + DynamicSetup_Mutants注入覆盖节点 | 部分可逆 |
| 2 | **基因毛皮** | Biotech | 身体覆盖毛皮层 | GeneDef设置`furDef` → PawnRenderNode_Fur渲染 | 移除基因即恢复 |
| 3 | **基因视觉附件** | Biotech | 头部/身体附加视觉元素 | GeneDef.renderNodeProperties → DynamicSetup_Genes注入 | 移除基因即恢复 |
| 4 | **Hediff视觉** | Core+ | 头部/身体附加视觉元素 | HediffDef.renderNodeProperties → DynamicSetup_Hediffs注入 | 移除Hediff即恢复 |
| 5 | **隐身** | Anomaly | 整体透明化 | HediffComp_Invisibility切换着色器为透明 | 完全可逆 |
| 6 | **吞噬者膨胀** | Anomaly | 体型变化 | CompDevourer修改drawSize → SetAllGraphicsDirty | 完全可逆 |
| 7 | **年龄阶段** | Biotech | 体型/头型/缩放变化 | LifeStageWorker修改bodyType → SetAllGraphicsDirty | 不可逆（成长） |
| 8 | **干尸化** | Core | 全身贴图替换为骨架 | PawnRenderNode_Body/Head的GraphicFor优先级1 | 不可逆（死亡） |

**变异体转化的外观变化链**（最复杂的先例）：
```
Pawn_MutantTracker.Turn()
  → 设置mutant.Def（MutantDef）
  → SetAllGraphicsDirty()
  → 渲染树重建
    → PawnRenderNode_Body.GraphicFor(): 检测IsMutant → 使用MutantDef.bodyTypeGraphicPaths
    → DynamicSetup_Mutants: 注入MutantDef.renderNodeProperties定义的覆盖层节点
  → 结果：身体贴图替换 + 额外覆盖层（如蹒跚者的腐烂效果）
```

**隐身的外观变化**（最轻量的先例）：
```
HediffComp_Invisibility
  → 添加/移除时调用SetAllGraphicsDirty()
  → 渲染树重建
  → PawnRenderNode.ShaderFor(): 检测隐身状态 → 切换为透明着色器
  → 结果：Pawn整体变为半透明
```

## 6. 模组扩展外观动态变化的方式

| # | 扩展方式 | 难度 | 适用场景 | 说明 |
|---|---------|------|---------|------|
| 1 | **GeneDef/HediffDef的renderNodeProperties** | 低(XML) | 基因/Hediff附加视觉元素 | 定义`renderNodeProperties`列表，自动由DynamicSetup注入 |
| 2 | **ThingComp.CompRenderNodes()** | 中(C#) | 装备/物品附加视觉元素 | 重写返回自定义PawnRenderNode列表，无需Harmony |
| 3 | **自定义PawnRenderNode子类** | 中(C#) | 自定义贴图解析逻辑 | 重写`GraphicFor()`/`ColorFor()`/`ShaderFor()` |
| 4 | **自定义PawnRenderNodeWorker子类** | 中(C#) | 自定义绘制行为 | 重写`CanDrawNow()`/`OffsetFor()`/`ScaleFor()`/`PreDraw()`/`PostDraw()` |
| 5 | **修改Pawn_StoryTracker字段** | 低(C#) | 体型/头型/发型/肤色变化 | 直接修改story字段 + 调用SetAllGraphicsDirty() |
| 6 | **Harmony补丁GraphicFor** | 高(C#) | 修改原版贴图解析逻辑 | Patch具体Node的GraphicFor方法（侵入性强） |

**模组实现"状态切换外观变化"的推荐模式**：

```
方案A（轻量，推荐）：Hediff标记 + renderNodeProperties
  → 添加Hediff时自动注入视觉节点 + SetAllGraphicsDirty
  → 移除Hediff时自动移除视觉节点 + SetAllGraphicsDirty
  → 优点：纯XML定义视觉效果，完全可逆，无C#

方案B（中量）：自定义Gene子类 + renderNodeProperties
  → 基因激活/停用时自动切换视觉
  → 优点：与基因系统深度集成

方案C（重量）：自定义PawnRenderNode + CompRenderNodes
  → ThingComp控制节点注入逻辑
  → 优点：最灵活，可实现任意条件的视觉切换
```

## 7. 关键源码引用表

| 类 | 方法/字段 | 命名空间 | 行号 | 说明 |
|----|---------|---------|------|------|
| `PawnRenderer` | `SetAllGraphicsDirty()` | Verse | L671 | 统一刷新入口（5步清除缓存） |
| `PawnRenderTree` | `SetDirty()` | Verse | — | 标记渲染树需要重建 |
| `PawnRenderTree` | `TrySetupGraphIfNeeded()` | Verse | L327 | 渲染树完整重建（创建root+动态节点+祖先） |
| `PawnRenderTree` | `SetupDynamicNodes()` | Verse | L375 | 遍历5种DynamicSetup + CompRenderNodes |
| `PawnRenderNode_Body` | `GraphicFor()` | Verse | L12 | 身体贴图优先级：干尸→变异体→CreepJoiner→正常 |
| `PawnRenderNode_Head` | `GraphicFor()` | Verse | L22 | 头部贴图优先级：无头→干尸→正常 |
| `DynamicSetup_Genes` | `GetDynamicNodes()` | Verse | L10 | 基因视觉节点注入（Active+HasGraphicProps） |
| `DynamicSetup_Hediffs` | `GetDynamicNodes()` | Verse | L10 | Hediff视觉节点注入（Visible+HasGraphicProps） |
| `DynamicSetup_Mutants` | `GetDynamicNodes()` | Verse | L10 | 变异体覆盖层注入 |
| `DynamicSetup_Traits` | `GetDynamicNodes()` | Verse | L10 | 特性视觉节点注入（!Suppressed） |
| `Gene` | `PostAdd()`/`PostRemove()` | Verse | L69/L77 | 基因变化触发SetAllGraphicsDirty |
| `Pawn_HealthTracker` | `AddHediff()`/`RemoveHediff()` | Verse | L252/L275 | Hediff变化条件触发 |
| `Pawn_MutantTracker` | `Turn()`/`Revert()` | RimWorld | L299/L321 | 变异体转化/恢复触发 |
| `HediffComp_Invisibility` | — | Verse | L208 | 隐身状态切换触发 |
| `CompDevourer` | — | RimWorld | L149/L169/L197 | 吞噬/消化/吐出触发 |
| `TraitSet` | `GainTrait()`/`RemoveTrait()` | RimWorld | L293/L371 | 特性变化条件触发 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-15 | 创建文档：Pawn外观动态变化机制完整源码分析（SetAllGraphicsDirty统一入口5步清除+12大类触发场景40+调用点分布表+条件触发优化HasDefinedGraphicProperties守门员+渲染树惰性重建流程LongEventHandler延迟+TrySetupGraphIfNeeded全量重建+5种DynamicSetup重建行为注入条件表+PawnRenderNode_Body/Head的GraphicFor优先级链4级/3级+8种原版动态外观变化先例变异体全身替换/基因毛皮/基因视觉附件/Hediff视觉/隐身透明/吞噬者膨胀/年龄阶段/干尸化+变异体转化外观变化链+隐身外观变化链+6种模组扩展方式+3种推荐模式ABC+源码引用表16项） | Claude Opus 4.6 |
