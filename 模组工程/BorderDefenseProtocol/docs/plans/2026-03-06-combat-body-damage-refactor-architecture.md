---
标题：战斗体伤害系统重构后架构图文注解
版本号: v1.0
更新日期: 2026-03-06
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 重构后战斗体伤害系统的完整架构图、数据流、组件关系和原版系统交互方式的图文注解。
---

# 战斗体伤害系统 — 重构后架构图文注解

## 一、总体架构对比

### 重构前（当前系统）

```
┌─ 原版伤害流程 ──────────────────────────────────────────────┐
│                                                              │
│  DamageWorker.FinalizeAndAddInjury                          │
│       │                                                      │
│       ▼                                                      │
│  ╔═══════════════════════════════════╗                       │
│  ║  Patch: Prefix → return false    ║  ← 全拦截！           │
│  ║  (伤害从不进入原版AddHediff)      ║                       │
│  ╚═══════════╤═══════════════════════╝                       │
│              │                                               │
│  ┌───────────▼──────────────── BDP平行系统 ────────────────┐ │
│  │                                                          │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │ │
│  │  │ TrionCost    │→│ ShadowHP     │→│ WoundHandler │  │ │
│  │  │ Handler      │  │ Handler      │  │ (9种自定义   │  │ │
│  │  │ (Trion消耗)  │  │ (影子HP系统) │  │  伤口Hediff) │  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  │ │
│  │          │                │                  │           │ │
│  │          ▼                ▼                  ▼           │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │ │
│  │  │ CollapseHdlr │  │ PartDestruct │  │ WoundAdapter │  │ │
│  │  │ (破裂检测)   │  │ Handler      │  │ (类型映射)   │  │ │
│  │  └──────────────┘  │ (自定义      │  └──────────────┘  │ │
│  │                     │  MissingPart) │                    │ │
│  │                     └──────────────┘                    │ │
│  │                                                          │ │
│  │  支撑: CombatBodyContext + CombatBodyDamageHandler      │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                              │
│  原版 pawn.health.AddHediff ← 被跳过，从未执行              │
│                                                              │
└──────────────────────────────────────────────────────────────┘

问题: 原版伤害链被完全截断，5个Handler + 3个支撑类 = ~1400行自定义代码
```

### 重构后（新系统）

```
┌─ 原版伤害流程（完整保留）─────────────────────────────────────┐
│                                                                │
│  DamageWorker.FinalizeAndAddInjury                            │
│       │                                                        │
│       ▼                                                        │
│  pawn.health.AddHediff(Hediff_Injury)  ← 正常执行！          │
│       │                                                        │
│       ▼                                                        │
│  CheckForStateChange                                           │
│       │                                                        │
│       ├─ ShouldBeDead?                                         │
│       │   └─ HasPreventsDeath → false  ← BDP_CombatBodyActive │
│       │                                                        │
│       ├─ ShouldBeDowned?                                       │
│       │   └─ 正常判定（可以倒地）                               │
│       │                                                        │
│       ├─ Manipulation = 0?                                     │
│       │   └─ TryDropEquipment                                  │
│       │       └─ Patch阻止触发体掉落  ← 保留的Patch           │
│       │                                                        │
│       ▼                                                        │
│  PostApplyDamage                                               │
│       │                                                        │
│       ▼                                                        │
│  ╔═════════════════════════════════════╗                       │
│  ║  Patch: Postfix (不阻止原版流程)   ║  ← 轻量介入          │
│  ║  → 通知 HediffComp_TrionDamageCost ║                       │
│  ║  → 通知 HediffComp_RuptureMonitor  ║                       │
│  ╚═════════════════════════════════════╝                       │
│                                                                │
└────────────────────────────────────────────────────────────────┘

优势: 原版伤害链完整 → 其他模组正常工作 → BDP只在末端"搭便车"
```

## 二、新系统组件关系图

```
                    ┌─────────────────────────────────────────┐
                    │         BDP_CombatBodyActive            │
                    │         (HediffDef + Hediff类)           │
                    │                                         │
                    │  XML配置层:                              │
                    │  ┌─────────────────────────────────┐    │
                    │  │ preventsDeath = true             │    │
                    │  │ painFactor = 0                   │    │
                    │  │ totalBleedFactor = 0    ← NEW    │    │
                    │  │ hungerRateFactor = 0             │    │
                    │  │ blocksMentalBreaks = true        │    │
                    │  │ statOffsets (温度/毒素)           │    │
                    │  │ makeImmuneTo (疾病)              │    │
                    │  └─────────────────────────────────┘    │
                    │                                         │
                    │  HediffComp层 (插件式效果):              │
                    │  ┌──────────────────────┐               │
                    │  │ TrionDamageCost      │──┐            │
                    │  │ (即时Trion消耗)       │  │            │
                    │  └──────────────────────┘  │            │
                    │  ┌──────────────────────┐  │            │
                    │  │ TrionWoundDrain      │  ├── 都与     │
                    │  │ (持续Trion流失)       │  │  CompTrion │
                    │  └──────────────────────┘  │  交互      │
                    │  ┌──────────────────────┐  │            │
                    │  │ RuptureMonitor       │──┘            │
                    │  │ (破裂条件监控)        │               │
                    │  └──────────────────────┘               │
                    └─────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
                    ▼               ▼               ▼
            ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
            │  CompTrion   │ │  CombatBody │ │  Hediff_     │
            │  (Trion池)   │ │  State      │ │  Collapsing  │
            │              │ │  (状态FSM)   │ │  (延时破裂)   │
            └─────────────┘ └─────────────┘ └─────────────┘
```

## 三、伤害处理数据流（重构后）

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  敌人攻击 Pawn（战斗体已激活）
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  ① Pawn.PreApplyDamage
     │
     ├─ [Patch检查] Pawn是否处于Collapsing状态？
     │   ├─ 是 → absorbed = true → 结束（无敌）
     │   └─ 否 → 继续
     │
     ├─ 原版护甲计算
     ├─ 原版装备吸收
     │
     ▼
  ② DamageWorker_AddInjury.Apply
     │
     ├─ 选择命中部位（原版随机+权重）
     ├─ 计算护甲后伤害
     ├─ 构造 Hediff_Injury
     │
     ▼
  ③ FinalizeAndAddInjury
     │
     ├─ pawn.health.AddHediff(injury)  ← 伤口正常添加！
     │   │
     │   └─ 伤口出现在健康面板 ✓
     │      原版部位HP减少 ✓
     │      如果HP耗尽 → 原版自动添加 Hediff_MissingPart ✓
     │
     ▼
  ④ CheckForStateChange
     │
     ├─ ShouldBeDead?
     │   └─ HasPreventsDeath = true (BDP_CombatBodyActive)
     │      → return false（不死）✓
     │
     ├─ ShouldBeDowned?
     │   └─ 可能为true（意识过低等）
     │      → MakeDowned（可以倒地）✓
     │
     ├─ Manipulation = 0?
     │   └─ TryDropEquipment
     │      └─ Patch检查: 是触发体? → 阻止掉落 ✓
     │
     ▼
  ⑤ PostApplyDamage
     │
     ├─ [Patch Postfix] 通知BDP系统
     │   │
     │   ├─ HediffComp_TrionDamageCost:
     │   │   cost = totalDamageDealt × 0.5
     │   │   CompTrion.Consume(cost)
     │   │   if 不足 → 标记trionDepleted
     │   │
     │   └─ HediffComp_RuptureMonitor:
     │       检查1: trionDepleted? → 触发破裂
     │       检查2: 关键部位新增MissingPart? → 触发破裂
     │
     ▼
  ⑥ 若触发破裂:
     │
     ├─ CombatBodyState → Collapsing
     ├─ 打断当前动作
     ├─ 添加 BDP_CombatBodyCollapsing Hediff
     │
     └─ (90 ticks后)
        ├─ Orchestrator.Deactivate(isEmergency: true)
        ├─ RemoveAllHediffs → 清理战斗伤口
        ├─ RestoreSnapshot → 恢复真身状态
        └─ 添加 BDP_Exhaustion debuff

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

## 四、出血抑制机制详解

```
原版出血计算路径:

  HediffSet.CalculateBleedRate()
      │
      │  float num = 1f;    // totalBleedFactor 累积器
      │  float num2 = 0f;   // bleedRate 累加器
      │
      │  for each hediff:
      │      num *= hediff.CurStage.totalBleedFactor
      │          ↑
      │          BDP_CombatBodyActive 的 stage:
      │          totalBleedFactor = 0
      │          ↓
      │          num *= 0 = 0  ← 关键！乘以0后永远为0
      │
      │      num2 += hediff.BleedRate   // 各伤口的出血率
      │
      │  return num2 * num / HealthScale
      │              ↑
      │           num = 0，所以最终结果 = 0
      │
      ▼
  BleedRateTotal = 0  → 无血迹、无失血

  HediffGiver_Bleeding.OnIntervalPassed():
      if (BleedRateTotal >= 0.1f) → false（不增加BloodLoss）
      else → 减少已有BloodLoss的severity

  结论: 只需一行XML配置 <totalBleedFactor>0</totalBleedFactor>
        即可完全抑制所有出血效果
```

## 五、死亡阻止机制详解

```
原版死亡判定路径:

  Pawn_HealthTracker.ShouldBeDead()
      │
      ├─ Dead? → skip (已经死了)
      │
      ├─ HasPreventsDeath?  ← 最早的检查点
      │   │
      │   └─ BDP_CombatBodyActive: preventsDeath = true
      │      → return false  ← 直接返回！后续检查全部跳过
      │
      │  ══ 以下检查全部被跳过 ══
      │
      ├─ (跳过) hediff.CauseDeathNow()
      ├─ (跳过) ShouldBeDeadFromRequiredCapacity()
      │         ├─ Consciousness = 0 (脑缺失)    → 不会死
      │         ├─ BloodPumping = 0 (心脏缺失)   → 不会死
      │         ├─ BloodFiltration = 0 (肾脏缺失) → 不会死
      │         └─ Metabolism = 0 (胃缺失)        → 不会死
      ├─ (跳过) CalculatePartEfficiency(corePart) ≤ 0
      └─ (跳过) ShouldBeDeadFromLethalDamageThreshold()

  结论: preventsDeath 是最强的死亡阻止机制
        位于 ShouldBeDead 的最顶部
        不需要额外代码来处理各种致死情况
```

## 六、Trion流失机制（替代出血）

```
  ┌───────────────────────────────────────────────────────────┐
  │                    Trion流失来源                           │
  │                                                           │
  │  来源1: 即时消耗 (HediffComp_TrionDamageCost)            │
  │  ┌──────────────────────────────────────────────┐        │
  │  │ 触发: 每次受伤后 (PostApplyDamage)           │        │
  │  │ 公式: cost = totalDamageDealt × costPerDamage │        │
  │  │ 示例: 受到20点伤害 → 消耗10 Trion             │        │
  │  │ 配置: costPerDamage = 0.5 (XML可调)           │        │
  │  └──────────────────────────────────────────────┘        │
  │                                                           │
  │  来源2: 持续流失 (HediffComp_TrionWoundDrain)            │
  │  ┌──────────────────────────────────────────────┐        │
  │  │ 触发: 每250 ticks 重新计算                    │        │
  │  │ 计算:                                         │        │
  │  │   遍历 pawn.health.hediffSet.hediffs          │        │
  │  │     → 筛选 Hediff_Injury                      │        │
  │  │     → 累加 severity                            │        │
  │  │   totalDrain = Σseverity × drainPerSeverityDay │        │
  │  │ 注册: CompTrion.RegisterDrain("CombatWounds") │        │
  │  │ 配置: drainPerSeverityPerDay = 5.0 (XML可调)  │        │
  │  └──────────────────────────────────────────────┘        │
  │                                                           │
  │  来源3: 维持消耗 (已有，不变)                             │
  │  ┌──────────────────────────────────────────────┐        │
  │  │ CompTrion.RegisterDrain("CombatBody", rate)   │        │
  │  └──────────────────────────────────────────────┘        │
  │                                                           │
  └───────────────────────────────────────────────────────────┘
                        │
                        ▼
                   ┌──────────┐
                   │ CompTrion │
                   │          │
                   │ cur ───→ 随时间减少
                   │          │
                   │ cur ≤ 0? │
                   │    │     │
                   │    ▼     │
                   │ 触发破裂  │
                   └──────────┘
```

## 七、关键部位破裂监控机制

```
  HediffComp_RuptureMonitor 工作流:

  ┌─────────────────────────────────────────────────────────┐
  │ 配置 (XML):                                             │
  │   criticalParts: [Head, Brain, Heart, Neck, Torso]      │
  └────────────────────────┬────────────────────────────────┘
                           │
  ╔════════════════════════▼════════════════════════════════╗
  ║ 检查时机: PostApplyDamage 回调                          ║
  ║                                                         ║
  ║ 遍历 pawn.health.hediffSet.hediffs:                     ║
  ║   for each hediff:                                      ║
  ║     if hediff is Hediff_MissingPart:                    ║
  ║       if hediff.Part.def.defName in criticalParts:      ║
  ║         if hediff 不在 "已知缺失集合" 中:               ║
  ║           → 新的关键部位缺失！触发破裂                   ║
  ║                                                         ║
  ║ 注: "已知缺失集合" = 激活时已缺失的部位                  ║
  ║     避免真身已残缺时误触发                               ║
  ╚═════════════════════════════════════════════════════════╝
                           │
                           ▼
                    触发破裂流程
                    (同Trion耗尽)
```

## 八、快照系统与新伤害系统的交互

```
  ┌─────────── 战斗体生命周期 ───────────────────────────────┐
  │                                                           │
  │  ① 激活 (TryActivate)                                    │
  │  ┌──────────────────────────────────────────┐            │
  │  │ SnapshotAll()                             │            │
  │  │   → 记录当前 Hediff 列表                   │            │
  │  │   → 记录装备/物品                          │            │
  │  │   → 记录需求值                             │            │
  │  │                                            │            │
  │  │ RemoveAllHediffsExceptExcluded()           │            │
  │  │   → Pawn进入"干净"健康状态                  │            │
  │  │                                            │            │
  │  │ AddHediff(BDP_CombatBodyActive)            │            │
  │  │   → preventsDeath + totalBleedFactor=0     │            │
  │  │   → 所有HediffComp自动激活                  │            │
  │  └──────────────────────────────────────────┘            │
  │                                                           │
  │  ② 战斗中                                                │
  │  ┌──────────────────────────────────────────┐            │
  │  │ Pawn.health.hediffSet:                    │            │
  │  │                                            │            │
  │  │   BDP_CombatBodyActive    ← 我们的守护者    │            │
  │  │   Hediff_Injury (左臂)    ← 原版伤口       │            │
  │  │   Hediff_Injury (躯干)    ← 原版伤口       │            │
  │  │   Hediff_MissingPart (右手) ← 原版缺失     │            │
  │  │   ...                                      │            │
  │  │                                            │            │
  │  │ 所有伤口都是原版类型                         │            │
  │  │ 健康面板完全正常显示                         │            │
  │  │ 其他模组可以正常读取伤口信息                  │            │
  │  └──────────────────────────────────────────┘            │
  │                                                           │
  │  ③ 解除 (Deactivate)                                     │
  │  ┌──────────────────────────────────────────┐            │
  │  │ RemoveAllHediffsExceptExcluded()           │            │
  │  │   → 清理 BDP_CombatBodyActive              │            │
  │  │   → 清理所有战斗中的 Hediff_Injury          │  ← 关键   │
  │  │   → 清理所有 Hediff_MissingPart             │  ← 关键   │
  │  │   → Pawn回到"干净"状态                      │            │
  │  │                                            │            │
  │  │ RestoreAll()                               │            │
  │  │   → 恢复激活前的 Hediff 列表                │            │
  │  │   → 恢复装备/物品                          │            │
  │  │   → 恢复需求值                             │            │
  │  │   → Pawn回到激活前的健康状态                │            │
  │  └──────────────────────────────────────────┘            │
  │                                                           │
  └───────────────────────────────────────────────────────────┘

  结论: 快照系统天然支持新方案
        战斗中的原版伤口在解除时被自动清理
        无需任何额外代码
```

## 九、与Biotech基因系统的设计类比

```
  ┌──── Biotech 基因效果模式 ────┐    ┌──── BDP 战斗体效果模式 ────┐
  │                               │    │                             │
  │ GeneDef                       │    │ HediffDef                   │
  │   ├─ capMods                  │    │   ├─ stages[0].capMods      │
  │   ├─ statOffsets              │    │   ├─ stages[0].statOffsets  │
  │   └─ abilities                │    │   └─ stages[0].*Factor     │
  │                               │    │                             │
  │ Gene.PostAdd()                │    │ Hediff.PostAdd()            │
  │   → 应用效果                   │    │   → 添加到pawn时生效        │
  │                               │    │                             │
  │ Gene.PostRemove()             │    │ 快照RestoreAll()            │
  │   → 移除效果                   │    │   → 移除时效果消失          │
  │                               │    │                             │
  │ Gene_Deathless                │    │ preventsDeath = true        │
  │   → 阻止死亡                   │    │   → 阻止死亡               │
  │                               │    │                             │
  │ HediffGiver_KeepHediff        │    │ HediffComp_TrionWoundDrain │
  │   → 定时检查并应用效果         │    │   → 定时计算伤口Trion流失   │
  │                               │    │                             │
  │ 核心理念:                      │    │ 核心理念:                   │
  │ "声明式效果 + 生命周期钩子"    │    │ "声明式效果 + 生命周期钩子"  │
  └───────────────────────────────┘    └─────────────────────────────┘

  模式一致性:
  1. 数据驱动（XML定义效果参数）
  2. 组件化（每个效果是独立的Comp）
  3. 生命周期管理（Add/Remove对称）
  4. 不修改原版代码（只添加/覆写）
```

## 十、Harmony Patch 最终清单

```
  ┌─── 保留的Patch ───────────────────────────────────────────┐
  │                                                            │
  │  Patch_EquipmentTracker_TryDropEquipment                  │
  │  ├─ 类型: Prefix                                          │
  │  ├─ 目标: Pawn_EquipmentTracker.TryDropEquipment          │
  │  ├─ 用途: 阻止触发体（武器）在战斗体激活时掉落             │
  │  └─ 原因: 原版不提供"特定装备不可掉落"的声明式配置         │
  │                                                            │
  │  Patch_CompRottable                                        │
  │  ├─ 类型: Postfix                                          │
  │  ├─ 目标: CompRottable.Active (getter)                    │
  │  ├─ 用途: 快照容器中物品不腐烂                             │
  │  └─ 原因: 快照系统需要，与伤害系统无关                     │
  │                                                            │
  └────────────────────────────────────────────────────────────┘

  ┌─── 新增的Patch ───────────────────────────────────────────┐
  │                                                            │
  │  Patch_Pawn_PostApplyDamage                                │
  │  ├─ 类型: Postfix （不阻止原版）                           │
  │  ├─ 目标: Pawn.PostApplyDamage                             │
  │  ├─ 用途: 伤害后消耗Trion + 检查破裂条件                   │
  │  └─ 原因: 需要知道实际伤害量来计算Trion消耗                │
  │                                                            │
  │  Patch_Pawn_PreApplyDamage_Collapsing                     │
  │  ├─ 类型: Postfix （修改absorbed引用）                     │
  │  ├─ 目标: Pawn.PreApplyDamage                              │
  │  ├─ 用途: Collapsing期间吸收所有伤害（无敌）               │
  │  └─ 原因: 延时破裂期间不应再受伤                           │
  │                                                            │
  └────────────────────────────────────────────────────────────┘

  ┌─── 删除的Patch ───────────────────────────────────────────┐
  │                                                            │
  │  Patch_DamageWorker_FinalizeAndAddInjury   ← 核心变化     │
  │  ├─ 原类型: Prefix (return false)                         │
  │  ├─ 原用途: 完全拦截原版伤害                               │
  │  └─ 删除原因: 不再拦截，让原版正常工作                     │
  │                                                            │
  │  Patch_HealthUtility_GetPartConditionLabel                │
  │  ├─ 原类型: Postfix                                       │
  │  ├─ 原用途: 用影子HP计算UI颜色                             │
  │  └─ 删除原因: 影子HP删除，原版HP = 显示HP                  │
  │                                                            │
  └────────────────────────────────────────────────────────────┘
```

## 十一、文件结构对比

```
  重构前 Combat/Damage/ 目录:              重构后 Combat/ 目录:
  ─────────────────────────                ─────────────────────────
  CombatBodyContext.cs        ← 删除
  CombatBodyDamageHandler.cs  ← 删除
  Handlers/                               Comps/
    CollapseHandler.cs        ← 删除        HediffComp_TrionDamageCost.cs    ← 新增
    PartDestructionHandler.cs ← 删除        HediffComp_TrionWoundDrain.cs    ← 新增
    ShadowHPHandler.cs        ← 删除        HediffComp_RuptureMonitor.cs     ← 新增
    TrionCostHandler.cs       ← 删除
    WoundHandler.cs           ← 删除
  HediffComp_CombatBodyDamageInterceptor.cs  ← 删除
  HediffCompProperties_CombatBodyDamageInterceptor.cs  ← 删除
  HediffComp_CombatWound.cs  ← 删除
  Hediff_CombatWound.cs      ← 删除
  Patch_DamageWorker_FinalizeAndAddInjury.cs  ← 删除
  Patch_EquipmentTracker_TryDropEquipment.cs  保留     Patches/
  ShadowHPTracker.cs          ← 删除        Patch_EquipmentTracker...cs      保留
  WoundAdapter.cs             ← 删除        Patch_Pawn_PostApplyDamage.cs    ← 新增
  UI/                                       Patch_Pawn_PreApplyDamage.cs     ← 新增
    Patch_HealthUtility...cs  ← 删除

  Hediff_CombatBodyActive.cs               ← 新增

  统计:
    删除: 16 个文件（~1400行）
    新增:  6 个文件（~280行）
    净减: 10 个文件（~1120行）
```

## 十二、灵活性与可扩展性

新架构的每个效果都是独立的HediffComp，添加新效果只需：

```
  1. 创建新的 HediffComp 子类
  2. 创建对应的 CompProperties 子类
  3. 在 XML 中添加到 BDP_CombatBodyActive 的 comps 列表

  示例 - 添加"战斗体受伤时产生Trion粒子效果":

  // C#
  public class HediffComp_TrionBleedVisual : HediffComp { ... }

  // XML
  <comps>
    <li Class="BDP.Combat.HediffCompProperties_TrionBleedVisual">
      <fleckDef>BDP_TrionSplash</fleckDef>
      <minDamageForEffect>5</minDamageForEffect>
    </li>
  </comps>

  零侵入式扩展！不需要修改任何已有代码。
```

---

## 历史修改记录
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-06 | 初版：重构后战斗体伤害系统的完整架构图文注解 | Claude Opus 4.6 |
