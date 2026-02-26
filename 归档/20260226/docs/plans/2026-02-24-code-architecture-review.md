---
标题：BDP模组代码与架构审查报告
版本号: v1.0
更新日期: 2026-02-24
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 对BorderDefenseProtocol模组当前实现（微内核+触发器模块，49个C#源文件）的全面代码审查。涵盖架构总览（深入到子系统级别）、分级问题清单（5 Critical / 6 Major / 6 Minor / 4 Cosmetic）、重构路线图（5个Phase，14项改进）、引擎兼容性风险清单（13个脆弱点）。
---

# BDP模组代码与架构审查报告

## 审查范围与条件

- **审查对象**：BorderDefenseProtocol 模组全部源码（49个C#文件 + 10个XML定义文件）
- **审查维度**：架构设计、代码质量、性能、可维护性、RimWorld引擎兼容性
- **重构约束**：开发期，无存档兼容约束，仅支持RimWorld 1.6
- **架构参考**：`5.1_BDP模组总体架构设计——微内核+多模块方案.md` v1.3

---

## 1. 架构总览

当前实现只涉及架构文档中的两个模块：**微内核(BDP.Core)** 和 **触发器模块(BDP.Trigger)**。

```
BDP 当前实现架构（2个模块，5个子系统，49个源文件）

┌─────────────────────────────────────────────────────────────────────┐
│  微内核 BDP.Core（7个文件）                                          │
│                                                                     │
│  ┌─────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │ CompTrion(432行) │  │ Gene_TrionGland  │  │ Gizmo_TrionBar   │   │
│  │ 能量容器+恢复驱动│  │ Stat聚合适配     │  │ 资源条GUI        │   │
│  │ +RegisterDrain  │  │                  │  │                  │   │
│  │ +事件OnDepleted │  │                  │  │                  │   │
│  └────────┬────────┘  └──────────────────┘  └──────────────────┘   │
│           │                                                         │
│  ┌────────┴────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │ CompProps_Trion  │  │ BDPDefExtension  │  │ BDP_DefOf        │   │
│  └─────────────────┘  └──────────────────┘  └──────────────────┘   │
└──────────────────────────────┬──────────────────────────────────────┘
                               │ C#依赖（Consume/Allocate/RegisterDrain）
                               ↓
┌─────────────────────────────────────────────────────────────────────┐
│  触发器模块 BDP.Trigger（42个文件）                                   │
│                                                                     │
│  ┌─── 子系统1：槽位状态机 ──────────────────────────────────────┐   │
│  │  CompTriggerBody(1337行,partial) ← GOD CLASS                │   │
│  │  ├─ 槽位管理（Left/Right/Special）                           │   │
│  │  ├─ 切换状态机（WindingDown→WarmingUp→Idle，懒求值）         │   │
│  │  ├─ 芯片装载/卸载                                           │   │
│  │  ├─ 战斗体生命周期（Generate/Dismiss/OnTrionDepleted）       │   │
│  │  ├─ Verb创建+缓存（反射Activator+fi_verbTracker）           │   │
│  │  ├─ Gizmo生成（Command_BDPChipAttack）                      │   │
│  │  ├─ 组合能力系统（ComboAbilityDef匹配）                     │   │
│  │  └─ 序列化（PostExposeData+读档恢复）                        │   │
│  │                                                              │   │
│  │  ChipSlot / SwitchContext / CompProps_TriggerBody            │   │
│  │  TriggerChipComp / CompProps_TriggerChip                    │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─── 子系统2：芯片效果 ───────────────────────────────────────┐   │
│  │  IChipEffect（接口：Activate/Deactivate/Tick/CanActivate）  │   │
│  │  ├─ WeaponChipEffect → SetSideVerbs → RebuildVerbs          │   │
│  │  ├─ ShieldChipEffect → AddHediff/RemoveHediff               │   │
│  │  └─ UtilityChipEffect → GainAbility/RemoveAbility           │   │
│  │                                                              │   │
│  │  WeaponChipConfig / ShieldChipConfig / UtilityChipConfig    │   │
│  │  CompAbilityEffect_TrionCost                                │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─── 子系统3：双武器Verb系统 ─────────────────────────────────┐   │
│  │  DualVerbCompositor（纯静态，合成规则引擎）                  │   │
│  │  ├─ ComposeVerbs: 左+右 → 独立Verb + 合成Verb              │   │
│  │  └─ 4种组合：单侧/近近/远远/近远混合                        │   │
│  │                                                              │   │
│  │  Verb继承树：                                                │   │
│  │  Verb_Shoot → Verb_BDPRangedBase(130行引擎代码复制)         │   │
│  │    ├─ Verb_BDPShoot / Verb_BDPVolley                        │   │
│  │    ├─ Verb_BDPDualRanged / Verb_BDPDualVolley               │   │
│  │    ├─ Verb_BDPGuided / Verb_BDPGuidedVolley                 │   │
│  │  Verb_MeleeAttackDamage → Verb_BDPMelee(60行引擎代码复制)  │   │
│  │                                                              │   │
│  │  JobDriver_BDPChipRangedAttack / JobDriver_BDPChipMeleeAttack│  │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─── 子系统4：弹道系统 ───────────────────────────────────────┐   │
│  │  Bullet_BDP ←──┐                                            │   │
│  │  Projectile_ExplosiveBDP ←─┤ 95%代码重复（拖尾+引导飞行）  │   │
│  │                            │                                │   │
│  │  GuidedFlightController（组合模式，管理折线路径点）          │   │
│  │  GuidedTargetingHelper（Shift+点击多步锚点瞄准）            │   │
│  │  GuidedVerbState（引导弹共享状态，单侧/双侧模式）          │   │
│  │  BDPEffectMapComponent（拖尾线段渲染管理）                  │   │
│  │  BDPTrailSegment / BeamTrailConfig                          │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─── 子系统5：UI ─────────────────────────────────────────────┐   │
│  │  Gizmo_TriggerBodyStatus（内联状态条，四态显示）             │   │
│  │  Window_TriggerBodySlots（浮动窗口，芯片管理）              │   │
│  │  Command_BDPChipAttack（攻击Gizmo，齐射+引导弹拦截）       │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘

关键数据流：
  芯片激活 → WeaponChipEffect.Activate()
    → CompTriggerBody.SetSideVerbs() → RebuildVerbs()
      → DualVerbCompositor.ComposeVerbs() → Activator.CreateInstance(verbClass)
        → 反射设置verb.verbTracker → 缓存到leftHandAttackVerb等字段
          → CompGetEquippedGizmosExtra() → Command_BDPChipAttack

  攻击执行 → Command_BDPChipAttack.ProcessInput()
    → verb.OrderForceTarget() → BDP_ChipRangedAttack Job
      → JobDriver手动调用verb.VerbTick() → TryCastShot()
        → TryCastShotCore(chipEquipment) → Projectile.Launch()
```

### 1.1 子系统1：槽位状态机（核心控制层）

```
CompTriggerBody (1337行, partial class) — 当前承担6个职责

  职责A：槽位数据管理
    leftHandSlots / rightHandSlots / specialSlots: List<ChipSlot>
    dualHandLockSlot: ChipSlot（双手锁定引用）
    ChipSlot {index, side, loadedChip, isActive} — IExposable
    API: LoadChip / UnloadChip / GetSlot / AllSlots / AllActiveSlots

  职责B：切换状态机（按侧独立，懒求值驱动）
    leftSwitchCtx / rightSwitchCtx: SwitchContext（null=Idle）
    SwitchContext {phase, phaseEndTick, targetSlotIndex, windingDownSlotIndex, durations}
    状态转换（由IsSwitching属性getter触发TryResolveSideSwitch）：
      Idle ──ActivateChip()──→ WarmingUp ──到期──→ Idle
      Idle ──ActivateChip(有旧)──→ WindingDown ──到期──→ WarmingUp ──到期──→ Idle
    ⚠ 副作用在getter中：TryResolveSideSwitch()调用DeactivateSlot/DoActivate

  职责C：芯片激活/关闭（业务逻辑核心）
    CanActivateChip() — 6项前置检查
    DoActivate(slot): Consume → RegisterDrain → 设置临时上下文 → effect.Activate → isActive=true
    DeactivateSlot(slot): UnregisterDrain → effect.Deactivate → isActive=false
    ⚠ 临时上下文(ActivatingSide/ActivatingSlot)无try/finally保护

  职责D：Verb创建+缓存（反射密集区）
    6个缓存字段：left/right/dual × attack/volley
    RebuildVerbs → CreateAndCacheChipVerbs → DualVerbCompositor.ComposeVerbs
      → Activator.CreateInstance → 反射fi_verbTracker.SetValue
    ⚠ 静态反射缓存：fi_verbTracker (BindingFlags.NonPublic)

  职责E：Gizmo生成
    CompGetEquippedGizmosExtra → 最多3个Command_BDPChipAttack + 状态条

  职责F：战斗体+组合能力+序列化
    IsCombatBodyActive / DismissCombatBody / OnTrionDepleted
    TryGrantComboAbility / TryRevokeComboAbilities
    PostExposeData（含读档恢复：重新Activate所有激活芯片）
```

### 1.2 子系统2：芯片效果（策略模式层）

```
IChipEffect 接口 → Activate / Deactivate / Tick / CanActivate

  WeaponChipEffect（最复杂）
    Activate: 确定侧别 → GetConfig(WeaponChipConfig) → SetSideVerbs → RebuildVerbs
    WeaponChipConfig: verbProperties / tools / trionCostPerShot
      supportsVolley / supportsGuided / maxAnchors / meleeBurstCount 等

  ShieldChipEffect（简单）
    Activate: AddHediff(cfg.shieldHediffDef)
    ShieldChipConfig: shieldHediffDef, trionCostPerDamageFactor

  UtilityChipEffect（简单）
    Activate: GainAbility(cfg.abilityDef)
    UtilityChipConfig: abilityDef

  配置读取统一模式：triggerComp.GetChipExtension<T>()
    → 优先ActivatingSlot → 回退遍历AllActiveSlots
```

### 1.3 子系统3：双武器Verb系统

```
DualVerbCompositor（纯静态，无状态）
  组合规则矩阵：null/近战/远程 × null/近战/远程 → 9种组合
  侧别编码：VerbProperties.label = "BDP_LeftHand" 等
  CopyVerbProps: MemberwiseClone（反射缓存的Func委托）

Verb继承树：
  Verb_Shoot → Verb_BDPRangedBase(316行，TryCastShotCore复制130行引擎代码)
    ├─ Verb_BDPShoot（单发）/ Verb_BDPVolley（齐射）
    ├─ Verb_BDPDualRanged（双侧交替burst）/ Verb_BDPDualVolley（双侧齐射）
    ├─ Verb_BDPGuided（引导弹单发）/ Verb_BDPGuidedVolley（引导弹齐射）
  Verb_MeleeAttackDamage → Verb_BDPMelee(374行，ApplyMeleeDamageToTarget复制60行)
    hitIndex状态机 + pendingInterval + 引擎burst驱动
```

### 1.4 子系统4：弹道系统

```
Bullet_BDP / Projectile_ExplosiveBDP — 95%代码重复
  共享逻辑：Config懒加载 / SpawnSetup预缓存Material / TickInterval拖尾
            InitGuidedFlight / ImpactSomething锚点推进 / ExposeData

GuidedFlightController (IExposable) — 组合模式，管理折线路径点
GuidedTargetingHelper (静态) — Shift+点击多步锚点瞄准
GuidedVerbState — 引导弹共享状态（单侧/双侧模式）
BDPEffectMapComponent (MapComponent) — 拖尾线段渲染管理
BDPTrailSegment / BeamTrailConfig (DefModExtension)
```

### 1.5 子系统5：UI

```
Gizmo_TriggerBodyStatus — 常驻状态条，四态视觉
Window_TriggerBodySlots — 浮动窗口，芯片管理
Command_BDPChipAttack — 攻击Gizmo，齐射+引导弹拦截
```

---

## 2. 问题清单

问题分级标准：
- **Critical**：可能导致运行时崩溃、存档损坏、或引擎更新后静默失效
- **Major**：显著影响可维护性或扩展性，但当前不会崩溃
- **Minor**：代码质量问题，不影响功能
- **Cosmetic**：风格和规范问题

### 2.1 Critical

**C1. 引擎源码复制——Verb_BDPRangedBase.TryCastShotCore** `[已缓解：版本校验哈希]`
- 位置：`Verb_BDPRangedBase.cs:68-199`
- 问题：复制了 `Verb_LaunchProjectile.TryCastShot()` 约130行源码，将 `equipmentSource` 替换为芯片Thing。RimWorld任何版本更新修改此方法，BDP的复制版本都不会自动跟进，导致行为偏差或崩溃。
- 风险：1.6的小版本更新（如4633→4700）就可能触发。
- 建议：用Harmony Transpiler替代，或保留复制但加版本校验哈希。
- 处置：`BDPMod.VerifyEngineMethodIntegrity()` 在mod加载时校验原版IL哈希（SHA256），不匹配时Log.Warning。基于RimWorld 1.6.4633。

**C2. 引擎源码复制——Verb_BDPMelee.ApplyMeleeDamageToTarget** `[已缓解：版本校验哈希]`
- 位置：`Verb_BDPMelee.cs:287-348`
- 问题：同C1，复制了约60行 `Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget()` 源码。
- 建议：Harmony Transpiler替换 `DamageInfo` 构造中的 `weaponDef` 参数。
- 处置：`BDPMod.VerifyEngineMethodIntegrity()` 双重校验——ApplyMeleeDamageToTarget（哨兵）+ DamageInfosToApply状态机MoveNext（精确检测）。基于RimWorld 1.6.4633。

**C3. ActivatingSide/ActivatingSlot 临时上下文无异常保护** `[已修复：try/finally]`
- 位置：`CompTriggerBody.cs:954-976`（DoActivate）和 `989-1014`（DeactivateSlot）
- 问题：如果 `IChipEffect.Activate()` 抛出异常，临时上下文永远不会被清除，后续所有 `GetChipExtension<T>()` 调用都会读到错误的槽位。
- 建议：加 try/finally 保护。
- 处置：3处（DoActivate、DeactivateSlot、PostExposeData读档恢复）均已加try/finally。

**C4. 反射访问 Verb.verbTracker 字段** `[已修复：移除反射，直接赋值]`
- 位置：`CompTriggerBody.cs:407-408`
- 问题：~~依赖RimWorld内部字段名~~（实际为public字段，反射多余）。已改为直接赋值 `verb.verbTracker = VerbTracker;`。
- 处置：移除 `fi_verbTracker` 反射字段、静态构造函数断言、`using System.Reflection`，两处 `SetValue` 改为直接赋值。

**C5. IsSwitching 属性getter中的状态转换副作用** `[降级为Cosmetic：经验证幂等且无实际风险]`
- 位置：`CompTriggerBody.cs:167-175`
- 问题：getter调用 `TryResolveSideSwitch()`，会调用 `DeactivateSlot()` 和 `DoActivate()`。
- 验证结论：
  - 前提成立：`EquipmentTrackerTick`确实不调用CompTick，懒求值是合理适配。
  - 幂等性成立：同一tick内多次调用，首次结算后后续全部no-op。
  - 报告三个风险场景均不成立：调试Watch仅影响体验；UI多次访问因幂等无害；PostExposeData不访问IsSwitching。
  - 无递归风险：DoActivate/DeactivateSlot/CanActivateChip均不回调IsSwitching。
- 处置：保持现状。Harmony方案引入新依赖解决不存在的问题，属过度工程。

### 2.2 Major

**M1. CompTriggerBody 是 God Class（1350行，6个职责）** `[保留现状：高耦合下拆分收益低]`
- 位置：`CompTriggerBody.cs` 全文
- 验证结论：职责间高度耦合（DoActivate/DeactivateSlot/TryResolveSideSwitch跨区域访问字段和方法），partial拆分仅物理分离不改变逻辑耦合。RimWorld Comp模式天然倾向大类。Debug方法已拆出（CompTriggerBody.Debug.cs），是最有价值的拆分。
- 处置：保留现状，不做进一步拆分。

**M2. Bullet_BDP 和 Projectile_ExplosiveBDP 95%代码重复**
- 位置：两个弹道类全文
- 建议：提取 `BDPProjectileHelper` 组合类，封装拖尾+引导飞行逻辑。

**M3. GetCurrentChipThing 和 GetChipConfig 重复的三级回退逻辑**
- 位置：`Verb_BDPRangedBase.cs:254-313`
- 建议：提取通用 `FindChipSlot()` 方法返回 `ChipSlot`。

**M4. Verb类数量膨胀（8个Verb子类）**
- 位置：DualWeapon/ 和 Projectiles/ 目录
- 问题：Guided变体和非Guided变体的区别仅在3个方法重写。
- 建议：用组合替代继承，Guided行为通过 `GuidedVerbState` 注入。

**M5. DualVerbCompositor 合成Verb从零构造而非克隆**
- 位置：`DualVerbCompositor.cs:135-270`
- 问题：手动 `new VerbProperties { ... }` 列出十几个字段，RimWorld新增字段会遗漏。
- 建议：从某一侧VP克隆后修改。

**M6. ComboAbilityDef 匹配在每次芯片激活时遍历 DefDatabase**
- 位置：`CompTriggerBody.cs:1296`
- 建议：游戏启动时构建 `Dictionary<(ThingDef, ThingDef), ComboAbilityDef>` 查找表。

### 2.3 Minor

**m1. 热路径LINQ分配**
- 位置：`CompTriggerBody.cs:347,355`、`DualVerbCompositor.cs:91,96`
- 建议：替换为手动for循环。

**m2. 硬编码中文字符串**
- 位置：`CompTriggerBody.cs:1260`（"双手触发"）、`CompAbilityEffect_TrionCost.cs:38,48`
- 建议：使用 `"BDP_KeyName".Translate()` + Languages/目录。

**m3. ShieldChipConfig 和 UtilityChipConfig 定义在Effect文件中**
- 建议：统一提取到 `Data/` 目录下的独立文件。

**m4. CompTriggerBody缓存模式在Melee和RangedBase中重复**
- 位置：`Verb_BDPMelee.cs:61-74` 和 `Verb_BDPRangedBase.cs:28-45`
- 建议：提取为静态工具方法。

**m5. parent.def.Verbs 可能为null**
- 位置：`CompTriggerBody.cs:142`
- 建议：`=> parent.def.Verbs ?? new List<VerbProperties>();`

**m6. DeactivateAll 异常处理过于宽泛**
- 位置：`CompTriggerBody.cs:904-913`
- 建议：catch中也尝试 `UnregisterDrain` 清理。

### 2.4 Cosmetic

**c1. 版本变更注释堆积**（v2.0/v3.0/.../v7.0）
- 建议：保留设计说明，删除历史变更记录（属于git历史）。

**c2. 内部Bug ID注释**（Bug1-Bug11, B2-B5, Fix-1-Fix-14, T23-T36）
- 建议：移到git commit message，代码只保留"为什么"的解释。

**c3. 不变量列表过长**（14条，无运行时验证）
- 建议：关键不变量转化为 `Debug.Assert` 或 `#if DEBUG` 检查。

**c4. Debug方法无条件编译守卫**
- 建议：用 `[Conditional("DEBUG")]` 标记或Release配置排除文件。

---

## 3. 重构路线图

### Phase 1：防御性修复（P0）

| # | 改进项 | 对应问题 | 依赖 | 工作量 |
|---|--------|---------|------|--------|
| R1 | ActivatingSide/ActivatingSlot 加 try/finally | C3 | 无 | 极小 |
| R2 | 反射失败时的防御性降级 | C4 | 无 | 小 |

### Phase 2：引擎兼容性（P1）

| # | 改进项 | 对应问题 | 依赖 | 工作量 |
|---|--------|---------|------|--------|
| R3 | Verb_BDPRangedBase 改用 Harmony 替代源码复制 | C1 | 无 | 中等 |
| R4 | Verb_BDPMelee 同上 | C2 | R3方案选择 | 中等 |
| R5 | IsSwitching 副作用移出属性getter | C5 | 无 | 中等 |

R3方案选择：
- A) Transpiler 替换 EquipmentSource IL指令（精确但IL脆弱）
- B) Prefix 拦截 + 临时替换引用（侵入性大）
- C) 保留复制但加版本校验哈希（妥协方案，推荐作为过渡）

### Phase 3：架构清理（P2）

| # | 改进项 | 对应问题 | 依赖 | 工作量 |
|---|--------|---------|------|--------|
| R6 | CompTriggerBody partial class 拆分 | M1 | R1,R5 | 中等 |
| R7 | 弹道类代码重复消除 | M2 | 无 | 小 |
| R9 | GetCurrentChipThing/GetChipConfig 合并 | M3 | 无 | 小 |
| R10 | DualVerbCompositor 合成Verb改用克隆 | M5 | 无 | 小 |

### Phase 4：Verb层次简化（P2）

| # | 改进项 | 对应问题 | 依赖 | 工作量 |
|---|--------|---------|------|--------|
| R8 | Verb类层次简化（组合替代继承） | M4 | R3 | 大 |

### Phase 5：代码质量打磨（P3）

| # | 改进项 | 对应问题 | 依赖 | 工作量 |
|---|--------|---------|------|--------|
| R11 | 热路径LINQ替换 | m1 | R6 | 小 |
| R12 | 硬编码中文字符串本地化 | m2 | 无 | 小 |
| R13 | 历史注释清理 | c1,c2 | 全部重构完成后 | 中等 |
| R14 | 不变量转化为运行时断言 | c3 | R6 | 小 |

---

## 4. 引擎兼容性风险清单

| # | 脆弱点 | 位置 | 风险 | 缓解措施 |
|---|--------|------|------|---------|
| E1 | Verb_LaunchProjectile.TryCastShot() 130行源码复制 | Verb_BDPRangedBase:68-199 | 高 | 无 → R3 |
| E2 | Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget() 60行源码复制 | Verb_BDPMelee:287-348 | 高 | 无 → R4 |
| E3 | Verb.verbTracker 内部字段反射访问 | CompTriggerBody:407-408 | 高 | 静态断言（仅报错）→ R2 |
| E4 | Verb.burstShotsLeft 字段直接读写 | Verb_BDPMelee:176 | 中 | 无 |
| E5 | Verb.ticksToNextBurstShot 字段直接写入 | Verb_BDPMelee:193 | 中 | 无 |
| E6 | Projectile.ticksToImpact 字段直接写入 | Bullet_BDP:88,103 | 中 | 无 |
| E7 | Projectile.origin / destination 字段直接写入 | Bullet_BDP:99-100 | 中 | 无 |
| E8 | Projectile.StartingTicksToImpact 属性读取 | Bullet_BDP:88 | 低 | 无 |
| E9 | VerbTracker.InitVerbsFromZero() 公开方法调用 | CompTriggerBody:440 | 低 | 无 |
| E10 | Pawn.stances.SetStance() 清除Stance_Cooldown | Verb_BDPMelee:142 | 低 | 无 |
| E11 | Find.Targeter.BeginTargeting() 7参数重载签名 | GuidedTargetingHelper:119 | 低 | 无 |
| E12 | MemberwiseClone via反射用于VerbProperties深拷贝 | DualVerbCompositor | 低 | 反射缓存（已有） |
| E13 | EquipmentTrackerTick不调用装备CompTick()的行为假设 | CompTriggerBody类头注释 | 无 | 懒求值设计（已适配） |

### 版本升级检查清单

RimWorld更新时应验证：
- [ ] Verb_LaunchProjectile.TryCastShot() 签名和逻辑是否变化
- [ ] Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget() 是否变化
- [ ] Verb 类的 verbTracker 字段名是否变化
- [ ] Verb 类的 burstShotsLeft / ticksToNextBurstShot 字段可见性
- [ ] Projectile 类的 origin / destination / ticksToImpact 字段可见性
- [ ] Find.Targeter.BeginTargeting() 重载签名
- [ ] VerbTracker.InitVerbsFromZero() 是否仍为public

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-24 | 创建文档。全面代码审查：架构总览（5个子系统深入展开）、问题清单（5C/6M/6m/4c共21项）、重构路线图（5 Phase 14项）、引擎兼容性风险清单（13个脆弱点）。 | Claude Opus 4.6 |
