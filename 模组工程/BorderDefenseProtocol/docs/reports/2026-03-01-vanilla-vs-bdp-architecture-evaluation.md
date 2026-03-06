---
标题：原版特化武器系统 vs BDP触发器系统 — 架构对比评估报告
版本号: v1.0
更新日期: 2026-03-01
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 对比RimWorld原版WeaponTrait特化武器架构与BDP触发器/弹道管线架构，提炼可借鉴的设计模式，输出分优先级的优化建议
---

## 一、研究范围

| 侧 | 核心类/Def | 版本 |
|---|---|---|
| 原版 | CompUniqueWeapon, CompBladelinkWeapon, WeaponTraitDef, WeaponTraitWorker, CompEquippable, Verb_Shoot, Projectile | 1.6 (Royalty + Odyssey) |
| BDP | CompTriggerBody, Bullet_BDP, IBDPProjectileModule, GuidedModule, TrackingModule, TrailModule, Verb_BDPRangedBase 及子类, FlightPhase, 8个管线接口 | v5管线架构 |

## 二、两套系统核心架构

### 2.1 原版特化武器系统

设计哲学：**数据驱动 + 薄Worker**。

```
┌─────────────────────────────────────────────────────────┐
│                    原版武器特化架构                        │
│                                                         │
│  ┌──────────┐    ┌──────────────┐    ┌───────────────┐  │
│  │ ThingDef │───→│CompEquippable│───→│  VerbTracker  │  │
│  │ (武器XML)│    │ (IVerbOwner) │    │  ┌─────────┐  │  │
│  └────┬─────┘    └──────┬───────┘    │  │Verb_Shoot│  │  │
│       │                 │            │  └─────────┘  │  │
│       │    ┌────────────┴──────┐     └───────────────┘  │
│       │    │CompUniqueWeapon   │                        │
│       │    │ ┌──────────────┐  │     ┌───────────────┐  │
│       │    │ │WeaponTraitDef│──┼────→│WeaponTrait    │  │
│       │    │ │  (数据容器)  │  │     │  Worker       │  │
│       │    │ │ ·statOffsets │  │     │ (策略行为)    │  │
│       │    │ │ ·extraDamages│  │     │ ·Notify_*     │  │
│       │    │ │ ·damageOverr.│  │     └───────────────┘  │
│       │    │ └──────────────┘  │                        │
│       │    └───────────────────┘                        │
│       │                                                 │
│       ▼                                                 │
│  ┌──────────┐    注入点（仅4个）：                        │
│  │Projectile│    ① TryCastShot → damageDefOverride      │
│  │ (Bullet) │    ② Launch → stoppingPower               │
│  │ (无模块) │    ③ Impact → extraDamages                │
│  └──────────┘    ④ Stat系统 → statOffsets/Factors       │
└─────────────────────────────────────────────────────────┘
```

关键数据流：

```
ThingDef(武器)
  └─ comps → CompUniqueWeapon
               └─ traits: List<WeaponTraitDef>
                    ├─ damageDefOverride   ──→ Projectile.damageDefOverride（发射时写入）
                    ├─ extraDamages        ──→ Projectile.extraDamages（发射时写入）
                    ├─ additionalStopping  ──→ Projectile.stoppingPower（Launch时累加）
                    ├─ statOffsets/Factors  ──→ StatWorker.GetValue（属性计算时读取）
                    ├─ equippedHediffs     ──→ Worker.Notify_Equipped（装备时添加）
                    ├─ abilityProps        ──→ CompEquippableAbilityReloadable（Setup时注入）
                    └─ workerClass         ──→ WeaponTraitWorker子类（事件响应）
```

### 2.2 BDP触发器系统

设计哲学：**管线调度 + 模块组合**。

```
┌──────────────────────────────────────────────────────────────┐
│                     BDP触发器架构                              │
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌────────────────┐  │
│  │CompTriggerBody│───→│ 芯片槽位系统  │───→│ DualVerb       │  │
│  │ (IVerbOwner)  │    │ Left/Right   │    │ Compositor     │  │
│  │ ·RebuildVerbs │    │ ·WeaponChip  │    │ ·合成Verb      │  │
│  └───────┬───────┘    └──────┬───────┘    └────────────────┘  │
│          │                   │                                │
│          ▼                   ▼                                │
│  ┌───────────────────────────────────────┐                    │
│  │         Verb_BDPRangedBase            │                    │
│  │  ├── Verb_BDPVolley (齐射)            │                    │
│  │  ├── Verb_BDPDualRanged (双侧交替)    │                    │
│  │  └── Verb_BDPDualVolley (双侧齐射)    │                    │
│  │  内含：VerbFlightState (引导弹状态)    │                    │
│  └───────────────┬───────────────────────┘                    │
│                  │ 发射                                       │
│                  ▼                                            │
│  ┌───────────────────────────────────────────────────────┐    │
│  │              Bullet_BDP (管线宿主)                      │    │
│  │  ┌─────────────────────────────────────────────────┐  │    │
│  │  │  8阶段管线调度                                    │  │    │
│  │  │  0.PostLaunchInit  1.LifecycleCheck              │  │    │
│  │  │  2.FlightIntent    3.base.TickInterval           │  │    │
│  │  │  4.PositionModify  5.VisualObserve               │  │    │
│  │  │  6.ArrivalPolicy   7.HitResolve  8.Impact        │  │    │
│  │  └─────────────────────────────────────────────────┘  │    │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐              │    │
│  │  │ Guided   │ │ Tracking │ │  Trail   │  ← 模块      │    │
│  │  │ Module   │ │ Module   │ │ Module   │              │    │
│  │  │ P=10     │ │ P=15     │ │ P=100    │              │    │
│  │  └──────────┘ └──────────┘ └──────────┘              │    │
│  │  协作媒介：FlightPhase 状态机                          │    │
│  │  Direct→GuidedLeg→FinalApproach→Tracking→Lost→Free   │    │
│  └───────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

管线调度时序：

```
每tick TickInterval:
  ┌─ 0.PostLaunchInit ─→ 1.LifecycleCheck ─→ 2.FlightIntent ─┐
  │     (一次性)         (IBDPLifecyclePolicy) (IBDPFlightIntent│
  │                       ·超时自毁            Provider)        │
  │                       ·丢锁检测            ·追踪方向        │
  │                       ·Phase转换请求        ·引导首跳        │
  └────────────────────────────────────────────────────────────┘
                                                      │
  ┌───────────────────────────────────────────────────┘
  │
  ├─ 3.base.TickInterval ─→ 4.PositionModify ─→ 5.VisualObserve
  │    (vanilla位置计算)     (IBDPPositionModifier) (IBDPVisualObserver)
  │    (拦截检查)            ·贝塞尔偏移            ·拖尾绘制
  │    (到达判定)
  │
  └─ 到达时 ImpactSomething:
     6.ArrivalPolicy ─→ 7.HitResolve ─→ 8.Impact
     (IBDPArrivalPolicy) (IBDPHitResolver) (IBDPImpactHandler)
     ·引导段重定向        ·追踪命中保证     ·穿体穿透
     ·追踪继续飞行        ·丢锁打地面       ·自定义命中效果
```

## 三、核心设计模式对比

| 维度 | 原版 WeaponTrait | BDP 触发器 |
|---|---|---|
| 核心模式 | 数据驱动 + 策略模式 | 管线调度 + 模块组合 |
| 行为载体 | WeaponTraitDef(数据) + Worker(薄逻辑) | IBDPProjectileModule(厚逻辑) |
| 组合方式 | Comp挂载 + Trait列表 | 管线接口多继承 |
| 协作机制 | 无（Trait间完全独立） | FlightPhase状态机 |
| 注入粒度 | 4个固定注入点 | 8阶段管线 + ref struct Context |
| 数据/逻辑比 | ~90%数据 / ~10%逻辑 | ~30%数据 / ~70%逻辑 |
| 扩展方式 | 新增XML Def即可 | 新增Module类 + 实现管线接口 |
| 冲突处理 | exclusionTags生成时过滤 | Priority排序 + 首个Intent胜出（隐式） |
| Stat集成 | GetStatOffset/GetStatFactor直接接入 | 无（硬编码在Verb/Module内部） |
| 序列化 | Scribe_Defs（轻量） | Scribe_Deep + 大量状态字段 |

### 3.1 "特性"的表达成本对比

```
原版：新增一个"燃烧弹"特性
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  <WeaponTraitDef>
    <defName>Incendiary</defName>
    <damageDefOverride>Flame</damageDefOverride>
    <extraDamages><li><def>Burn</def><amount>5</amount></li></extraDamages>
  </WeaponTraitDef>
  → 0行C#，纯XML

BDP：新增一个"燃烧弹"效果
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  需要：新建 IncendiaryModule.cs
       实现 IBDPImpactHandler
       在 BDPModuleFactory 注册
       在 DefModExtension 配置
  → ~80行C#
```

### 3.2 模块间协作方式对比

```
原版：Trait之间完全独立，线性叠加
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Trait A (damageOverride)  ──→ ┐
  Trait B (statOffset)      ──→ ├──→ 各自写入不同字段，互不干扰
  Trait C (extraDamage)     ──→ ┘
  冲突处理：exclusionTags 互斥标签（生成时过滤，运行时零冲突）

BDP：模块通过Phase状态机协作
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  GuidedModule ──→ Phase=GuidedLeg ──→ Phase=FinalApproach ──┐
                                                              │
  TrackingModule ←── 读Phase判断是否激活 ←────────────────────┘
       │
       └──→ Phase=Tracking ──→ Phase=TrackingLost ──→ Phase=Free

  TrailModule ←── 读Phase决定拖尾样式

  协作媒介：FlightPhase（模块只读，宿主统一写入）
  冲突处理：Priority排序 + 首个非null Intent胜出（隐式规则）
```

## 四、BDP优于原版的地方（应保持）

| 能力 | 原版 | BDP |
|---|---|---|
| 弹道控制 | 无（直线飞行） | 折线/追踪/贝塞尔 |
| 飞行中修改 | 不可能 | 8阶段管线每tick可控 |
| 模块间协作 | 无需求 | Phase状态机精确协作 |
| 到达后决策 | 固定Impact | ArrivalPolicy可重定向 |
| 视觉效果 | 无扩展点 | VisualObserver管线 |
| 速度修正 | 无 | SpeedModifier管线 |

这些能力是原版根本不需要的，BDP的管线架构是正确的选择，不需要改动。

## 五、优化建议

### 建议 P0：引入 BDPBulletTraitDef 数据驱动层

**问题**：所有弹道效果都需要写Module类，简单效果（换伤害类型、加额外伤害、产生地面效果）的扩展成本过高。

**方案**：学习原版 WeaponTraitDef，新增一个纯数据驱动的轻量特性层。

```
分层架构：
┌─────────────────────────────────────────────────────────┐
│                    BDP 弹道系统                           │
│                                                         │
│  ┌─────────────────────────────────────────────────┐    │
│  │  第一层：BDPBulletTraitDef（数据驱动，零代码）     │    │
│  │  · damageDefOverride / extraDamages              │    │
│  │  · statOffsets / impactEffects                   │    │
│  │  → 适用：燃烧弹、穿甲弹、毒素弹等简单效果        │    │
│  └──────────────────────┬──────────────────────────┘    │
│                         │ 不够用时                       │
│  ┌──────────────────────▼──────────────────────────┐    │
│  │  第二层：IBDPProjectileModule（管线模块，写C#）    │    │
│  │  · GuidedModule / TrackingModule / TrailModule   │    │
│  │  → 适用：追踪弹、引导弹、折线弹道等复杂行为       │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

**实现要点**：
- 新建 `BDPBulletTraitDef : Def`，字段参考 WeaponTraitDef（damageDefOverride, extraDamages, statOffsets 等）
- 在 Bullet_BDP 的 Impact 阶段读取 TraitDef 列表，应用数据效果
- TraitDef 通过 DefModExtension 挂载到投射物 ThingDef 上
- 框架层一次性实现，之后新增简单效果只需写XML

**理由**：让简单的事情保持简单。管线架构处理复杂弹道行为已经很优秀，但对"命中时换个伤害类型"这种需求是杀鸡用牛刀。

**投入**：中 | **收益**：高 | **风险**：低

### 建议 P1：芯片效果接入 Stat 系统

**问题**：芯片的射速/伤害修改硬编码在 Verb/Module 内部，武器信息面板看不到芯片带来的属性变化，玩家无法直观对比不同芯片。

**方案**：在 CompTriggerBody 或新建的 CompChipStats 中重写 `GetStatOffset/GetStatFactor`。

```
原版路径（已验证可行）：
  StatWorker.GetValueUnfinalized()
    → thing.GetStatOffsets(stat)
      → CompUniqueWeapon.GetStatOffset(stat)
        → 遍历 traits → statOffsets.GetStatOffsetFromList(stat)
    → 自动显示在武器信息面板

BDP 接入方案：
  CompTriggerBody.GetStatOffset(stat)
    → 遍历激活芯片的 WeaponChipConfig
      → 读取芯片定义的 statOffsets
    → 武器面板自动显示芯片属性加成
```

**理由**：接入 Stat 系统不仅改善玩家体验（UI可见），还能让其他Mod（如 Combat Extended）通过标准 Stat 接口自然兼容 BDP 芯片效果。

**投入**：低 | **收益**：中 | **风险**：低

### 建议 P2：模块互斥标签机制

**问题**：BDP 模块间的兼容性目前靠隐式规则（Priority排序 + 首个Intent胜出）。随着模块种类增加，可能出现未预期的冲突。

**方案**：学习原版 `exclusionTags`，在模块配置中声明互斥关系。

```xml
<!-- 示例：两个FlightIntentProvider不能共存 -->
<BDPTrackingConfig>
  <exclusionTags><li>FlightControl</li></exclusionTags>
</BDPTrackingConfig>

<BDPSomeNewFlightConfig>
  <exclusionTags><li>FlightControl</li></exclusionTags>
</BDPSomeNewFlightConfig>

<!-- 但 Guided + Tracking 可以共存（有Phase协作协议） -->
<BDPGuidedConfig>
  <exclusionTags><li>FlightControl</li></exclusionTags>
  <compatibleWith><li>BDPTrackingConfig</li></compatibleWith>
</BDPGuidedConfig>
```

**实现要点**：
- 在 `BDPModuleFactory.CreateModules()` 中检查互斥标签
- 冲突时输出警告日志并跳过后注册的模块
- `compatibleWith` 白名单覆盖 `exclusionTags` 黑名单

**理由**：显式声明优于隐式约定。当前模块数量少（3个）问题不大，但随着弹道类型增加（爆炸弹、分裂弹、反弹弹等），隐式规则会变得脆弱。

**投入**：低 | **收益**：中 | **风险**：低

### 建议 P3：抽取 IChipEffectWorker 为 CompTriggerBody 减负

**问题**：CompTriggerBody 当前承担了槽位管理、Verb重建、Gizmo生成、芯片效果响应等多重职责（700+行），违反单一职责原则。

**方案**：学习原版 `WeaponTraitWorker` 的事件模式，将芯片效果响应抽取为独立接口。

```
原版 Worker 事件模型：
  WeaponTraitWorker
    ├── Notify_Equipped(pawn)
    ├── Notify_EquipmentLost(pawn)
    ├── Notify_KilledPawn(pawn)
    ├── Notify_Bonded(pawn)
    └── Notify_Unbonded(pawn)

建议的 BDP 芯片效果接口：
  interface IChipEffectWorker
  {
      void OnChipLoaded(Pawn pawn, SlotSide side);
      void OnChipRemoved(Pawn pawn, SlotSide side);
      void OnChipActivated(Pawn pawn);
      void OnKilledPawn(Pawn pawn, Pawn victim);
  }

  CompTriggerBody 职责缩减为：
    · 槽位状态机管理
    · IVerbOwner 实现
    · 芯片装载/卸载时分发 Worker 事件
  芯片效果逻辑委托给各 Worker 实现
```

**理由**：降低 CompTriggerBody 的认知负担，让芯片效果可以独立开发和测试。但此项改动涉及现有代码重构，需要谨慎评估影响范围。

**投入**：中 | **收益**：中 | **风险**：中

## 六、建议优先级总览

| 优先级 | 建议 | 投入 | 收益 | 风险 | 核心理由 |
|---|---|---|---|---|---|
| P0 | 引入 BDPBulletTraitDef 数据层 | 中 | 高 | 低 | 简单效果零代码扩展，降低扩展门槛 |
| P1 | 芯片效果接入 Stat 系统 | 低 | 中 | 低 | 改善玩家体验 + 提升Mod兼容性 |
| P2 | 模块互斥标签机制 | 低 | 中 | 低 | 显式声明防止未来模块冲突 |
| P3 | 抽取 IChipEffectWorker | 中 | 中 | 中 | CompTriggerBody 减负，提升可维护性 |

## 七、核心结论

BDP 的 v5 管线架构在处理复杂弹道行为（追踪、引导、折线、贝塞尔）方面设计精良，Phase 状态机 + Context 意图模式 + 宿主统一执行的三层分离是正确的架构选择，不需要改动。

需要学习原版的地方集中在"简单效果的表达效率"上——原版通过 WeaponTraitDef 实现了"一个XML Def搞定一个武器特性"的极致数据驱动，而 BDP 目前所有效果都需要写 C# Module。引入数据驱动层（P0）是收益最高的改进，让简单的事情保持简单，复杂的事情才用管线。

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-01 | 初版：原版WeaponTrait vs BDP触发器架构对比评估 | Claude Opus 4.6 |
