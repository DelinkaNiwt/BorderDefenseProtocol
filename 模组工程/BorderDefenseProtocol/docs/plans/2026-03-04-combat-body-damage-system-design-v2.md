---
标题：战斗体伤害系统设计文档
版本号: v2.1
更新日期: 2026-03-04
最后修改者: Claude Sonnet 4.6
标签：[文档][部分实现][未锁定]
摘要: BDP战斗体伤害隔离系统设计文档v2.1。反映实际简化实现：静态Handler链（TrionCostHandler→ShadowHPHandler→CollapseHandler），影子HP系统（defName key），拦截点FinalizeAndAddInjury。v2.0的管道模式、XML配置、伤口系统标记为未来版本。
---

# 战斗体伤害系统设计文档 v2.0

## 目录

1. [概述](#一概述)
2. [架构设计](#二架构设计)
3. [核心接口与数据结构](#三核心接口与数据结构)
4. [管道执行器](#四管道执行器)
5. [伤害处理器](#五伤害处理器)
6. [状态管理](#六状态管理)
7. [伤口系统](#七伤口系统)
8. [配置系统](#八配置系统)
9. [死亡拦截](#九死亡拦截)
10. [数据流与时序](#十数据流与时序)
11. [实现指南](#十一实现指南)
12. [测试建议](#十二测试建议)

---

## 一、概述

### 1.1 设计目标

实现战斗体的核心价值——**完全伤害隔离**（需求NR-001）：
- 战斗体受伤不影响真身HP
- 战斗体解除后真身100%完好
- 战斗体有独立的耐久系统（影子HP）
- 战斗体破裂时触发紧急脱离

### 1.2 设计范围

本文档涵盖战斗体伤害隔离子系统的完整设计：

✅ **包含内容**：
- 管道式伤害处理架构
- 影子HP追踪系统
- 战斗体伤口Hediff
- 部位毁坏检测
- 破裂触发机制
- 死亡/倒地拦截
- 配置化Handler管道

❌ **不包含内容**：
- 引导时间机制（后续版本）
- 完整紧急脱离系统（仅路由）
- 枯竭debuff（后续版本）

### 1.3 设计原则

1. **事件驱动** - 破裂检测用事件回调，不轮询
2. **配置驱动** - 所有数值和Handler可在XML中配置
3. **最小化Harmony** - 只有1个patch（死亡拦截）
4. **原版兼容** - 参考原版流血和伤口机制
5. **职责分离** - 使用管道模式，每个Handler职责单一
6. **易于扩展** - 新增伤害类型无需修改现有代码

### 1.4 依赖文档

- 需求来源：`战斗体系统需求设计文档.md` (v4.1, 43条需求)
- 架构参考：`战斗体系统架构设计文档.md` (v1.3)
- 技术参考：`战斗体系统RimWorld原生技术参考分析.md` (v1.0)
- 架构评估：`2026-03-04-combat-body-damage-system-architecture-review.md` (v1.0)

---

## 二、架构设计

### 2.1 组件结构（当前实现）

```
Combat/
├── Damage/
│   ├── CombatBodyDamageHandler.cs          # 静态Handler链入口
│   ├── ShadowHPTracker.cs                  # 影子HP追踪器
│   ├── Handlers/
│   │   ├── TrionCostHandler.cs             # Trion消耗处理器
│   │   ├── ShadowHPHandler.cs              # 影子HP处理器
│   │   ├── CollapseHandler.cs              # 破裂检测处理器
│   │   └── PartDestructionHandler.cs       # 部位破坏处理器
│   └── Patch_ThingWithComps_PreApplyDamage.cs  # 伤害拦截Harmony patch
```

### 2.2 架构模式：静态Handler链（当前实现）

**核心思想**：使用静态方法链式调用处理伤害，直接操作DamageInfo和Pawn状态。

```
Hediff_Injury 输入（FinalizeAndAddInjury拦截）
    ↓
┌─────────────────────────────────┐
│  CombatBodyDamageHandler        │
│  (静态Handler链入口)             │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  [1] TrionCostHandler.Handle()  │
│      计算并消耗Trion            │
│      返回: bool (是否充足)      │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  [2] ShadowHPHandler.Handle()   │
│      更新影子HP                 │
│      检测部位破坏               │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  [3] CollapseHandler.Handle()   │
│      检查破裂条件               │
│      触发解除流程               │
└─────────────────────────────────┘
    ↓
处理完成（阻止原版伤害应用）
```

**实际代码示例**（从CombatBodyDamageHandler.cs）：

```csharp
// Handler 1: Trion消耗
bool trionSufficient = TrionCostHandler.Handle(pawn, damage);

// Handler 2: 影子HP
bool shadowHPSuccess = true;
if (hitPart != null)
{
    shadowHPSuccess = ShadowHPHandler.Handle(pawn, hitPart, damage);
}

// Handler 3: 破裂检测
bool criticalPartDestroyed = IsCriticalPartDestroyed(pawn, hitPart);
CollapseHandler.Handle(pawn, !trionSufficient, criticalPartDestroyed, collapseReason);
```

**优势**：
- ✅ 简单直接：无需反射、无需配置解析
- ✅ 性能优越：静态方法调用，零开销
- ✅ 易于调试：调用栈清晰
- ✅ 快速迭代：修改Handler无需重启游戏

**局限性**：
- ❌ 不可配置：Handler顺序和参数硬编码
- ❌ 扩展性弱：新增Handler需修改CombatBodyDamageHandler
- ❌ 无上下文传递：Handler间通过返回值通信

### 2.3 架构模式：管道模式（未来版本）

> **注意**：以下设计为v2.0原始设计，当前未实现。保留作为未来重构参考。

**核心思想**：将伤害处理拆分为多个独立的Handler，通过管道串联执行。

```
DamageInfo 输入
    ↓
┌─────────────────────────────────┐
│  HediffComp_CombatBodyActive    │
│  (管道执行器)                    │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  BuildContext()                 │
│  创建DamageContext              │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  ExecutePipeline()              │
│  遍历Handler列表                │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  [1] TrionCostHandler           │
│      计算并消耗Trion            │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  [2] ShadowHPHandler            │
│      更新影子HP                 │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  [3] PartDestroyHandler         │
│      检查部位毁坏               │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  [4] WoundHandler               │
│      创建/更新伤口              │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  [5] CollapseHandler            │
│      检查破裂条件               │
└─────────────────────────────────┘
    ↓
处理完成（absorbed = true）
```

**优势**（未来版本）：
- ✅ 职责分离：每个Handler < 100行代码
- ✅ 易于测试：可以单独测试每个Handler
- ✅ 易于扩展：新增Handler无需修改现有代码
- ✅ 可配置：通过XML定义Handler顺序和参数

### 2.4 与现有系统的集成

```
Gene_TrionGland (外层FSM)
    ↓ 激活战斗体
CombatBodyOrchestrator
    ↓ 添加Hediff
HediffComp_CombatBodyActive (本系统)
    ↓ 拦截伤害
PostPreApplyDamage钩子
    ↓ 执行管道
Handler Pipeline
    ↓ 检测破裂
TriggerPassiveCollapse()
    ↓ 通知外层
Gene_TrionGland.OnCombatBodyEnded()
```

---

## 三、核心接口与数据结构（未来版本）

> **注意**：以下接口和数据结构为v2.0原始设计，当前未实现。保留作为未来重构参考。

### 3.1 IBDPDamageHandler（Handler接口）

**文件**：`Combat/DamageHandlers/IBDPDamageHandler.cs`

```csharp
namespace BDP.Combat.DamageHandlers
{
    /// <summary>
    /// 伤害处理器接口。
    /// 所有伤害处理逻辑通过实现此接口来扩展。
    /// </summary>
    public interface IBDPDamageHandler
    {
        /// <summary>
        /// 处理伤害。
        /// </summary>
        /// <param name="context">伤害上下文，包含所有处理所需的数据</param>
        void Handle(DamageContext context);
    }
}
```

### 3.2 IConfigurableHandler（可配置Handler接口）

**文件**：`Combat/DamageHandlers/IConfigurableHandler.cs`

```csharp
namespace BDP.Combat.DamageHandlers
{
    /// <summary>
    /// 可配置Handler接口。
    /// 需要从XML读取配置的Handler实现此接口。
    /// </summary>
    public interface IConfigurableHandler
    {
        /// <summary>
        /// 配置Handler。
        /// </summary>
        /// <param name="def">Handler配置定义</param>
        void Configure(DamageHandlerDef def);
    }
}
```

### 3.3 DamageContext（伤害上下文）

**文件**：`Combat/DamageHandlers/DamageContext.cs`

```csharp
namespace BDP.Combat.DamageHandlers
{
    /// <summary>
    /// 伤害处理上下文。
    /// 在管道中的Handler之间传递数据，避免全局状态。
    /// </summary>
    public class DamageContext
    {
        // ── 输入数据 ──

        /// <summary>受伤的Pawn</summary>
        public Pawn Pawn { get; set; }

        /// <summary>伤害信息</summary>
        public DamageInfo DamageInfo { get; set; }

        // ── 共享状态 ──

        /// <summary>影子HP追踪器</summary>
        public ShadowHPTracker ShadowHP { get; set; }

        /// <summary>伤口追踪器</summary>
        public WoundTracker Wounds { get; set; }

        // ── 处理结果 ──

        /// <summary>是否应该触发破裂</summary>
        public bool ShouldCollapse { get; set; }

        /// <summary>破裂原因</summary>
        public string CollapseReason { get; set; }

        /// <summary>部位是否被毁</summary>
        public bool PartDestroyed { get; set; }

        /// <summary>被毁的部位</summary>
        public BodyPartRecord DestroyedPart { get; set; }

        // ── 中间数据（Handler之间传递）──

        /// <summary>本次伤害消耗的Trion</summary>
        public float TrionCost { get; set; }

        /// <summary>受伤部位剩余HP</summary>
        public float PartHPRemaining { get; set; }
    }
}
```

---

## 四、管道执行器（未来版本）

> **注意**：以下管道执行器为v2.0原始设计，当前未实现。保留作为未来重构参考。

### 4.1 当前实现：CombatBodyDamageHandler（静态Handler链）

**文件**：`Combat/Damage/CombatBodyDamageHandler.cs`

当前实现使用静态方法链式调用，直接在拦截点处理伤害：

```csharp
public static class CombatBodyDamageHandler
{
    public static void HandleDamage(Pawn pawn, Hediff_Injury injury)
    {
        float damage = injury.Severity;          // 护甲后伤害（原版已算好）
        BodyPartRecord hitPart = injury.Part;    // 命中部位（原版已选好）

        // Handler 1: Trion消耗
        bool trionSufficient = TrionCostHandler.Handle(pawn, damage);

        // Handler 2: 影子HP
        bool shadowHPSuccess = true;
        if (hitPart != null)
        {
            shadowHPSuccess = ShadowHPHandler.Handle(pawn, hitPart, damage);
        }

        // Handler 3: 破裂检测
        bool criticalPartDestroyed = IsCriticalPartDestroyed(pawn, hitPart);
        CollapseHandler.Handle(pawn, !trionSufficient, criticalPartDestroyed, collapseReason);
    }
}
```

### 4.2 未来版本：HediffComp_CombatBodyActive（管道执行器）

**文件**：`Combat/Hediffs/HediffComp_CombatBodyActive.cs`

```csharp
using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using BDP.Combat.DamageHandlers;
using BDP.Combat.State;
using BDP.Core.Genes;

namespace BDP.Combat.Hediffs
{
    /// <summary>
    /// 战斗体激活状态的Hediff组件。
    /// 职责：
    /// 1. 构建Handler管道（从XML配置）
    /// 2. 拦截伤害并执行管道
    /// 3. 响应破裂事件（Trion耗尽、倒地）
    /// </summary>
    public class HediffComp_CombatBodyActive : HediffComp
    {
        // ── 配置 ──

        private HediffCompProperties_CombatBody Props =>
            (HediffCompProperties_CombatBody)props;

        // ── Handler管道 ──

        private List<IBDPDamageHandler> pipeline;

        // ── 状态管理（独立组件）──

        private ShadowHPTracker shadowHP;
        private WoundTracker wounds;

        // ── 状态查询 ──

        /// <summary>战斗体是否激活</summary>
        private bool IsActive => parent != null && !parent.ShouldRemove;

        // ═══════════════════════════════════════════════════════════
        // 生命周期
        // ═══════════════════════════════════════════════════════════

        public override void CompPostMake()
        {
            base.CompPostMake();

            // 初始化影子HP追踪器
            shadowHP = new ShadowHPTracker();
            shadowHP.Initialize(Pawn);

            // 初始化伤口追踪器
            wounds = new WoundTracker(Pawn);

            // 构建Handler管道（从XML配置）
            pipeline = BuildPipeline();

            // 订阅Trion耗尽事件
            var compTrion = Pawn.GetComp<CompTrion>();
            if (compTrion != null)
            {
                compTrion.OnTrionDepleted += HandleTrionDepleted;
            }

            Log.Message($"[BDP] 战斗体激活: {Pawn.Name}, 管道Handler数量: {pipeline.Count}");
        }

        public override void CompExposeData()
        {
            base.CompExposeData();

            // 序列化状态管理器
            Scribe_Deep.Look(ref shadowHP, "shadowHP");
            Scribe_Deep.Look(ref wounds, "wounds");

            // 读档后重建
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (shadowHP == null)
                {
                    shadowHP = new ShadowHPTracker();
                    shadowHP.Initialize(Pawn);
                }

                if (wounds == null)
                {
                    wounds = new WoundTracker(Pawn);
                }

                // 重建管道
                pipeline = BuildPipeline();

                // 重新订阅事件
                var compTrion = Pawn.GetComp<CompTrion>();
                if (compTrion != null)
                {
                    compTrion.OnTrionDepleted += HandleTrionDepleted;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 管道构建
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 从XML配置构建Handler管道。
        /// </summary>
        private List<IBDPDamageHandler> BuildPipeline()
        {
            var handlers = new List<IBDPDamageHandler>();

            // 从Props读取Handler配置
            if (Props.damageHandlers != null && Props.damageHandlers.Count > 0)
            {
                foreach (var handlerDef in Props.damageHandlers)
                {
                    try
                    {
                        // 通过反射实例化Handler
                        var handler = (IBDPDamageHandler)Activator.CreateInstance(handlerDef.handlerClass);

                        // 如果Handler需要配置，传入配置对象
                        if (handler is IConfigurableHandler configurable)
                        {
                            configurable.Configure(handlerDef);
                        }

                        handlers.Add(handler);
                        Log.Message($"[BDP] 加载Handler: {handlerDef.handlerClass.Name}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[BDP] 无法实例化Handler: {handlerDef.handlerClass?.Name}, 错误: {ex.Message}");
                    }
                }
            }
            else
            {
                // 默认管道（如果XML未配置）
                Log.Warning("[BDP] 未找到Handler配置，使用默认管道");
                handlers.Add(new TrionCostHandler());
                handlers.Add(new ShadowHPHandler());
                handlers.Add(new PartDestroyHandler());
                handlers.Add(new WoundHandler());
                handlers.Add(new CollapseHandler());
            }

            return handlers;
        }

        // ═══════════════════════════════════════════════════════════
        // 伤害拦截（核心入口）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 伤害拦截入口。
        /// 在原版伤害应用前调用，可以修改伤害或完全吸收。
        /// </summary>
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!IsActive) return;

            // 构建上下文
            var context = new DamageContext
            {
                Pawn = Pawn,
                DamageInfo = dinfo,
                ShadowHP = shadowHP,
                Wounds = wounds
            };

            // 执行管道
            ExecutePipeline(context);

            // 处理破裂
            if (context.ShouldCollapse)
            {
                TriggerPassiveCollapse(context.CollapseReason);
            }

            // 完全吸收伤害，阻止原版HP系统
            absorbed = true;
        }

        /// <summary>
        /// 执行Handler管道。
        /// </summary>
        private void ExecutePipeline(DamageContext context)
        {
            foreach (var handler in pipeline)
            {
                try
                {
                    handler.Handle(context);

                    // 如果已经触发破裂，提前终止管道
                    if (context.ShouldCollapse)
                    {
                        Log.Message($"[BDP] 管道提前终止: {context.CollapseReason}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[BDP] Handler执行失败: {handler.GetType().Name}, 错误: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 事件回调
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Trion耗尽事件回调。
        /// </summary>
        private void HandleTrionDepleted()
        {
            TriggerPassiveCollapse("Trion耗尽");
        }

        /// <summary>
        /// 倒地事件回调。
        /// </summary>
        public override void Notify_Downed()
        {
            base.Notify_Downed();
            if (IsActive)
            {
                TriggerPassiveCollapse("倒地");
            }
        }

        /// <summary>
        /// 触发被动破裂。
        /// </summary>
        private void TriggerPassiveCollapse(string reason)
        {
            Log.Message($"[BDP] 战斗体破裂: {Pawn.Name} - {reason}");

            // 通知Gene层
            var gene = Pawn.genes?.GetFirstGeneOfType<Gene_TrionGland>();
            if (gene != null)
            {
                gene.OnCombatBodyEnded(CombatBodyEndReason.Passive);
            }
        }
    }
}
```

---

## 五、伤害处理器（当前实现）

### 5.1 TrionCostHandler（Trion消耗处理器）

**文件**：`Combat/Damage/Handlers/TrionCostHandler.cs`

**职责**：
- 计算Trion消耗：cost = damage * trionCostPerDamage
- 从CompTrion中扣除Trion
- Trion不足时返回false触发破裂

**实际代码**（从源码摘录）：

```csharp
using BDP.Core;
using Verse;

namespace BDP.Combat
{
    public static class TrionCostHandler
    {
        // TODO: 从XML配置读取，临时硬编码
        private const float TRION_COST_PER_DAMAGE = 0.5f;

        /// <summary>
        /// 处理Trion消耗。
        /// </summary>
        /// <param name="pawn">受伤的Pawn</param>
        /// <param name="damage">伤害量</param>
        /// <returns>true=消耗成功，false=Trion不足（触发破裂）</returns>
        public static bool Handle(Pawn pawn, float damage)
        {
            var compTrion = pawn.GetComp<CompTrion>();
            if (compTrion == null)
            {
                Log.Error($"[BDP] TrionCostHandler: {pawn.LabelShort} 缺少CompTrion");
                return false;
            }

            float cost = damage * TRION_COST_PER_DAMAGE;
            bool success = compTrion.Consume(cost);

            if (!success)
            {
                Log.Message($"[BDP] TrionCostHandler: {pawn.LabelShort} Trion不足，触发破裂");
            }

            return success;
        }
    }
}
```

### 5.2 ShadowHPHandler（影子HP处理器）

**文件**：`Combat/Damage/Handlers/ShadowHPHandler.cs`

**职责**：
- 应用伤害到影子HP：ShadowHPTracker.TakeDamage(part, damage)
- 检查部位是否破坏
- 部位破坏时调用PartDestructionHandler（移除真身部位）
- 关键部位破坏时不标记部位（因为会立即触发战斗体破裂）

**实际代码**（从源码摘录）：

```csharp
using BDP.Core;
using Verse;

namespace BDP.Combat
{
    public static class ShadowHPHandler
    {
        // 关键部位定义
        private static readonly System.Collections.Generic.HashSet<string> CriticalParts =
            new System.Collections.Generic.HashSet<string>
        {
            "Head",   // 头部
            "Brain",  // 大脑
            "Heart",  // 心脏
            "Neck",   // 脖子
            "Torso"   // 躯干
        };

        public static bool Handle(Pawn pawn, BodyPartRecord part, float damage)
        {
            var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();
            if (gene?.ShadowHP == null)
            {
                Log.Error($"[BDP] ShadowHPHandler: {pawn.LabelShort} 缺少ShadowHPTracker");
                return false;
            }

            float hpBefore = gene.ShadowHP.GetHP(part);

            // 已破坏的部位不再受伤
            if (hpBefore <= 0f)
            {
                Log.Message($"[BDP]   ShadowHP [{part.def.defName}]: 已破坏，跳过伤害");
                return true;
            }

            gene.ShadowHP.TakeDamage(part, damage);
            float hpAfter = gene.ShadowHP.GetHP(part);

            if (hpAfter <= 0f)
            {
                Log.Message($"[BDP]   ShadowHP [{part.def.defName}]: {hpBefore:F1} → {hpAfter:F1} ★破坏★");

                // 检查是否为关键部位
                bool isCritical = CriticalParts.Contains(part.def.defName);

                if (isCritical)
                {
                    // 关键部位破坏：不标记部位，直接触发破裂
                    Log.Message($"[BDP]   关键部位破坏，将触发战斗体破裂（不标记部位）");
                }
                else
                {
                    // 非关键部位破坏：标记部位缺失
                    Log.Message($"[BDP]   非关键部位破坏，标记部位缺失");
                    gene.PartDestruction?.Handle(pawn, part);
                }
            }
            else
            {
                Log.Message($"[BDP]   ShadowHP [{part.def.defName}]: {hpBefore:F1} → {hpAfter:F1}");
            }

            return true;
        }
    }
}
```

### 5.3 CollapseHandler（破裂检测处理器）

**文件**：`Combat/Damage/Handlers/CollapseHandler.cs`

**职责**：
- 检测破裂条件（Trion耗尽、关键部位破坏）
- 触发解除流程：调用Gene_TrionGland.DeactivateCombatBody(isEmergency: true)

**实际代码**（从源码摘录）：

```csharp
using BDP.Core;
using Verse;

namespace BDP.Combat
{
    public static class CollapseHandler
    {
        public static void Handle(Pawn pawn, bool trionDepleted, bool criticalPartDestroyed, string reason = null)
        {
            if (!trionDepleted && !criticalPartDestroyed)
                return;

            var gene = pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();
            if (gene == null || !gene.IsCombatBodyActive)
                return;

            // 使用传入的详细原因，或生成默认原因
            if (string.IsNullOrEmpty(reason))
            {
                reason = trionDepleted ? "Trion耗尽" : "关键部位破坏";
            }

            Log.Warning($"[BDP] ═══════════════════════════════════════════════════");
            Log.Warning($"[BDP] ⚠ 战斗体破裂！");
            Log.Warning($"[BDP] ⚠ 目标: {pawn.LabelShort}");
            Log.Warning($"[BDP] ⚠ 原因: {reason}");
            Log.Warning($"[BDP] ═══════════════════════════════════════════════════");

            // 触发战斗体破裂（紧急解除）
            gene.DeactivateCombatBody(isEmergency: true);
        }
    }
}
```

### 5.4 未来版本：其他Handler

> **注意**：以下Handler为v2.0原始设计，当前未实现。保留作为未来扩展参考。

#### PartDestroyHandler（部位毁坏处理器）

**文件**：`Combat/DamageHandlers/PartDestroyHandler.cs`
            if (damageMultipliers != null && damageMultipliers.TryGetValue(damageDef, out float multiplier))
                return multiplier;
            return defaultMultiplier;
        }
    }
}
```

### 5.2 ShadowHPHandler（影子HP处理器）

**文件**：`Combat/DamageHandlers/ShadowHPHandler.cs`

```csharp
using Verse;

namespace BDP.Combat.DamageHandlers
{
    public class ShadowHPHandler : IBDPDamageHandler
    {
        public void Handle(DamageContext context)
        {
            var hitPart = context.DamageInfo.HitPart;
            if (hitPart == null) return;

            context.ShadowHP.TakeDamage(hitPart, context.DamageInfo.Amount);
            context.PartHPRemaining = context.ShadowHP.GetPartHP(hitPart);
        }
    }
}
```

### 5.3 PartDestroyHandler（部位毁坏处理器）

**文件**：`Combat/DamageHandlers/PartDestroyHandler.cs`

```csharp
using System.Collections.Generic;
using Verse;
using BDP.Core.Comps;

namespace BDP.Combat.DamageHandlers
{
    public class PartDestroyHandler : IBDPDamageHandler, IConfigurableHandler
    {
        private HashSet<string> weakPoints;
        private float organDestroyedDrainPerDay = 10f;

        public void Configure(DamageHandlerDef def)
        {
            weakPoints = def.weakPoints != null
                ? new HashSet<string>(def.weakPoints)
                : new HashSet<string>();
            organDestroyedDrainPerDay = def.organDestroyedDrainPerDay;
        }

        public void Handle(DamageContext context)
        {
            var hitPart = context.DamageInfo.HitPart;
            if (hitPart == null) return;

            if (context.ShadowHP.IsPartDestroyed(hitPart))
            {
                context.PartDestroyed = true;
                context.DestroyedPart = hitPart;
                context.Wounds.RemoveWound(hitPart);

                if (IsWeakPoint(hitPart))
                {
                    context.ShouldCollapse = true;
                    context.CollapseReason = $"{hitPart.Label}被毁";
                }
                else
                {
                    RegisterHighDrain(context.Pawn, hitPart);
                }
            }
        }

        private bool IsWeakPoint(BodyPartRecord part)
        {
            return weakPoints != null && weakPoints.Contains(part.def.defName);
        }

        private void RegisterHighDrain(Pawn pawn, BodyPartRecord part)
        {
            var compTrion = pawn.GetComp<CompTrion>();
            if (compTrion != null)
            {
                string key = $"organ_destroyed_{part.def.defName}";
                compTrion.RegisterDrain(key, organDestroyedDrainPerDay);
            }
        }
    }
}
```

### 5.4 WoundHandler（伤口处理器）

**文件**：`Combat/DamageHandlers/WoundHandler.cs`

```csharp
using Verse;
using BDP.Core.Defs;

namespace BDP.Combat.DamageHandlers
{
    public class WoundHandler : IBDPDamageHandler, IConfigurableHandler
    {
        private HediffDef woundHediff;
        private float mergeThreshold = 5.0f;

        public void Configure(DamageHandlerDef def)
        {
            woundHediff = def.woundHediff ?? BDP_DefOf.BDP_CombatWound;
            mergeThreshold = def.mergeThreshold;
        }

        public void Handle(DamageContext context)
        {
            if (context.PartDestroyed) return;

            var hitPart = context.DamageInfo.HitPart;
            if (hitPart == null) return;

            context.Wounds.CreateOrMergeWound(hitPart, context.DamageInfo.Amount, mergeThreshold);
        }
    }
}
```

### 5.5 CollapseHandler（破裂检测处理器）

**文件**：`Combat/DamageHandlers/CollapseHandler.cs`

```csharp
namespace BDP.Combat.DamageHandlers
{
    public class CollapseHandler : IBDPDamageHandler, IConfigurableHandler
    {
        private float totalDamageThreshold = float.MaxValue;

        public void Configure(DamageHandlerDef def)
        {
            totalDamageThreshold = def.totalDamageThreshold;
        }

        public void Handle(DamageContext context)
        {
            // 预留：未来可以添加更多破裂条件
        }
    }
}
```




---

## 六、状态管理与配置系统

### 6.1 ShadowHPTracker（影子HP追踪器）

**关键优化**：使用defName路径作为key，序列化简单，兼容性好。

### 6.2 完整XML配置示例

```xml
<HediffDef>
  <defName>BDP_CombatBodyActive</defName>
  <label>战斗体激活</label>
  <comps>
    <li Class="BDP.Combat.Hediffs.HediffCompProperties_CombatBody">
      <damageHandlers>
        <li>
          <handlerClass>BDP.Combat.DamageHandlers.TrionCostHandler</handlerClass>
          <defaultMultiplier>1.0</defaultMultiplier>
        </li>
        <li>
          <handlerClass>BDP.Combat.DamageHandlers.ShadowHPHandler</handlerClass>
        </li>
        <li>
          <handlerClass>BDP.Combat.DamageHandlers.PartDestroyHandler</handlerClass>
          <weakPoints>
            <li>Brain</li>
            <li>Heart</li>
          </weakPoints>
        </li>
        <li>
          <handlerClass>BDP.Combat.DamageHandlers.WoundHandler</handlerClass>
        </li>
        <li>
          <handlerClass>BDP.Combat.DamageHandlers.CollapseHandler</handlerClass>
        </li>
      </damageHandlers>
    </li>
  </comps>
</HediffDef>
```

---

## 七、实现指南

### 7.1 实现顺序

1. **核心接口**（1-2小时）：IBDPDamageHandler、DamageContext
2. **状态管理**（2-3小时）：ShadowHPTracker、WoundTracker
3. **Handler实现**（3-4小时）：5个Handler
4. **管道执行器**（2-3小时）：HediffComp_CombatBodyActive
5. **配置与集成**（2-3小时）：XML配置、Harmony patch
6. **测试与调试**（4-6小时）

**总计**：15-23小时

### 7.2 关键注意事项

1. **序列化测试**：重点测试ShadowHPTracker的存档/读档
2. **Handler顺序**：管道中Handler的顺序很重要
3. **错误处理**：每个Handler都要有try-catch
4. **日志输出**：关键节点添加日志

---

## 八、测试建议

### 8.1 集成测试场景

**场景1：基础伤害处理**
- 激活战斗体 → 受到子弹伤害
- 验证：Trion减少、影子HP减少、创建伤口Hediff

**场景2：弱点部位破裂**
- 激活战斗体 → 头部受到致命伤害
- 验证：触发破裂、通知Gene层

**场景3：Trion耗尽破裂**
- 激活战斗体（低Trion） → 受到大量伤害
- 验证：Trion耗尽、触发破裂

**场景4：存档加载**
- 激活战斗体并受伤 → 保存 → 加载
- 验证：影子HP、伤口Hediff正确恢复

---

## 九、拦截点迁移（重要变更）

### 9.1 迁移原因

**原设计**（v2.0）：使用`PostPreApplyDamage`钩子拦截伤害
- 拦截时机：原版伤害计算前
- 需要手动猜测命中部位
- 需要手动模拟护甲计算
- 与原版系统耦合度高

**当前实现**：使用`FinalizeAndAddInjury`拦截点
- 拦截时机：原版已构造Hediff_Injury对象后
- 直接使用原版计算结果：`injury.Severity`（护甲后伤害）、`injury.Part`（命中部位）
- 避免手动猜测和模拟
- 更好的原版隔离

### 9.2 实现方式

**Harmony Patch**：

```csharp
[HarmonyPatch(typeof(Pawn_HealthTracker), "FinalizeAndAddInjury")]
public static class Patch_ThingWithComps_PreApplyDamage
{
    public static bool Prefix(Pawn ___pawn, Hediff_Injury injury)
    {
        // 检查是否为战斗体状态
        var gene = ___pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        if (gene == null || !gene.IsCombatBodyActive)
            return true; // 非战斗体，执行原版逻辑

        // 战斗体伤害处理
        CombatBodyDamageHandler.HandleDamage(___pawn, injury);

        // 阻止原版添加伤口
        return false;
    }
}
```

### 9.3 优势对比

| 方面 | PreApplyDamage（v2.0设计） | FinalizeAndAddInjury（当前实现） |
|------|---------------------------|--------------------------------|
| **命中部位** | 需要手动猜测 | 原版已选好（injury.Part） |
| **护甲计算** | 需要手动模拟 | 原版已算好（injury.Severity） |
| **代码复杂度** | 高（需要模拟原版逻辑） | 低（直接使用原版结果） |
| **原版兼容性** | 低（深度耦合） | 高（仅拦截最终结果） |
| **维护成本** | 高（原版更新需同步） | 低（原版更新自动适配） |

### 9.4 日志输出示例

```
[BDP] ── 伤害拦截 ──────────────────────────────────────────
[BDP]   目标: 殖民者A  伤害类型: Bullet  伤害量: 15.0 (护甲后)
[BDP]   受伤部位: LeftArm
[BDP]   处理前 → Trion: 100.0  部位影子HP: 20.0
[BDP]   ShadowHP [LeftArm]: 20.0 → 5.0
[BDP]   处理后 → Trion: 92.5 (消耗: 7.5)  部位影子HP: 5.0
[BDP] ────────────────────────────────────────────────────
```

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v2.1 | 2026-03-04 | 反映实际简化实现。架构：静态Handler链替代管道模式。拦截点：FinalizeAndAddInjury替代PreApplyDamage。已实现：影子HP追踪、Trion消耗、破裂触发。未实现：管道模式、XML配置、伤口系统（标记为未来版本）。新增第九章"拦截点迁移"说明架构变更原因。 | Claude Sonnet 4.6 |
| v2.0 | 2026-03-04 | 完整设计版本。基于架构评估报告优化：引入管道模式实现职责分离，优化影子HP系统使用defName作为key，支持配置化伤害处理策略。本版本包含所有组件的完整实现设计、数据流说明、实现指南和测试建议，可直接用于开发实施。 | Claude Sonnet 4.6 |
| v1.0 | 2026-03-04 | 初版完成 | Claude Sonnet 4.6 |
