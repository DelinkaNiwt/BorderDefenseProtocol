---
标题：Pawn死亡判定与致死方式
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld中Pawn死亡判定的完整机制——核心调用链、ShouldBeDead()的5个判定条件、致命能力汇总、典型致死Hediff、deathMtbDays随机死亡、倒地即死概率、疼痛休克机制、死亡预防机制
---

# Pawn死亡判定与致死方式

**总览**：RimWorld中Pawn的死亡由`Pawn_HealthTracker.CheckForStateChange()`统一调度——每当Hediff增减或伤害应用后触发，通过`ShouldBeDead()`的5个条件判定是否致死，再决定执行死亡、死眠替代或倒地。此外还有`deathMtbDays`随机死亡和倒地即死两条独立致死路径。

## 1. 核心调用链

```
AddHediff / RemoveHediff / PostApplyDamage / Notify_HediffChanged
  → CheckForStateChange(dinfo, hediff)
    ├── ShouldBeDead()                          ← 5个判定条件
    │   ├── HasPreventsDeath                    ← 死亡豁免检查
    │   ├── Hediff.CauseDeathNow()             ← 致死Hediff检查
    │   ├── ShouldBeDeadFromRequiredCapacity()  ← 致命能力检查
    │   ├── CalculatePartEfficiency(corePart)   ← 核心部位效率检查
    │   └── ShouldBeDeadFromLethalDamageThreshold() ← 累计伤害阈值
    ├── ShouldBeDeathrestingOrInComa()          ← Biotech死亡替代
    │   └── ForceDeathrestOrComa()
    ├── pawn.Kill(dinfo, hediff)                ← 执行死亡
    └── ShouldBeDowned()                        ← 倒地判定
        └── 倒地即死概率检查
```

**触发时机**：`CheckForStateChange`在以下场景被调用：
- `AddHediff` — 添加任何Hediff后
- `RemoveHediff` — 移除Hediff后
- `PostApplyDamage` — 伤害应用后
- `Notify_HediffChanged` — Hediff状态变化时（如Severity变化导致Stage切换）

## 2. ShouldBeDead()的5个判定条件

源码位置：`Verse.Pawn_HealthTracker.ShouldBeDead()`

判定按以下顺序执行，**短路求值**——任一条件满足即返回：

| # | 条件 | 判定逻辑 | 返回值 | 说明 |
|---|------|---------|--------|------|
| 0 | **已死亡** | `Dead == true` | `true` | 防止重复判定 |
| 1 | **死亡豁免** | `hediffSet.HasPreventsDeath` | `false` | 有`preventsDeath=true`的Hediff时直接豁免 |
| 2 | **Hediff致死严重度** | 任一Hediff的`CauseDeathNow()` == true | `true` | Severity ≥ lethalSeverity |
| 3 | **致命能力降为0** | `ShouldBeDeadFromRequiredCapacity()` != null | `true` | 肉体5种/机械体3种致命能力 |
| 4 | **核心部位效率为0** | `CalculatePartEfficiency(corePart)` ≤ 0.0001f | `true` | Torso效率归零 |
| 5 | **累计伤害超阈值** | `ShouldBeDeadFromLethalDamageThreshold()` | `true` | 所有Injury的Severity总和 ≥ 阈值 |

### 2.1 条件1：死亡豁免（HasPreventsDeath）

```csharp
// HediffSet.HasPreventsDeath
// 遍历所有Hediff，检查是否有 def.preventsDeath == true
if (hediffSet.HasPreventsDeath) return false;
```

拥有`preventsDeath=true`的Hediff时，**完全跳过所有死亡判定**，直接返回false。

**典型实例**：
- `DeathRefusal`（Anomaly DLC）— 拒绝死亡，触发后消耗自身并复活Pawn

### 2.2 条件2：Hediff致死严重度（CauseDeathNow）

```csharp
// Hediff.CauseDeathNow()
public virtual bool CauseDeathNow()
{
    if (IsLethal)  // def.lethalSeverity > 0f && canBeThreateningToPart
    {
        return Severity >= def.lethalSeverity;
    }
    return false;
}
```

遍历所有Hediff，只要有一个的`CauseDeathNow()`返回true就判定死亡。

**关键字段**：
- `HediffDef.lethalSeverity`：致死阈值（float，-1表示无致死性）
- `Hediff.canBeThreateningToPart`：是否可对部位构成威胁（受`onlyLifeThreateningTo`限制）

### 2.3 条件3：致命能力降为0（ShouldBeDeadFromRequiredCapacity）

```csharp
// 遍历所有PawnCapacityDef
// 肉体检查 lethalFlesh，机械体检查 lethalMechanoids
if ((pawn.RaceProps.IsFlesh ? cap.lethalFlesh : cap.lethalMechanoids)
    && !capacities.CapableOf(cap))
    return cap;  // 返回致死的能力Def
```

详见第3节致命能力汇总表。

### 2.4 条件4：核心部位效率为0

```csharp
if (PawnCapacityUtility.CalculatePartEfficiency(hediffSet, pawn.RaceProps.body.corePart) <= 0.0001f)
    return true;
```

核心部位（corePart，通常是Torso）的效率降为0时判定死亡。这通常发生在Torso被摧毁（如MissingBodyPart）时。

### 2.5 条件5：累计伤害超阈值（ShouldBeDeadFromLethalDamageThreshold）

```csharp
// 累加所有 Hediff_Injury 的 Severity
float totalInjury = Σ hediff_injury.Severity;
return totalInjury >= LethalDamageThreshold;

// LethalDamageThreshold = 150f * pawn.HealthScale
```

**公式**：`致死阈值 = 150 × HealthScale`

| 种族 | HealthScale | 致死阈值 |
|------|------------|---------|
| 人类 | 1.0 | 150 |
| 大象 | 4.0 | 600 |
| 松鼠 | 0.18 | 27 |

> **注意**：只统计`Hediff_Injury`类型（物理伤害），不包括疾病、植入体等其他Hediff。

## 3. 致命能力汇总表（PawnCapacityDef）

源码位置：`Core/Defs/PawnCapacityDefs/PawnCapacity.xml`

| 能力 | defName | lethalFlesh | lethalMechanoids | 说明 |
|------|---------|-------------|-----------------|------|
| **意识** | Consciousness | ✓ | ✓ | 肉体+机械体都致死。脑部损伤、麻醉过量等 |
| **血液循环** | BloodPumping | ✓ | ✓ | 肉体+机械体都致死。心脏摧毁等 |
| **血液过滤** | BloodFiltration | ✓ | ✓ | 肉体+机械体都致死。双肾摧毁等 |
| **呼吸** | Breathing | ✓ | ✗ | 仅肉体致死。双肺摧毁等 |
| **代谢** | Metabolism | ✓ | ✗ | 仅肉体致死。消化系统完全丧失 |

**判定逻辑**：`capacities.CapableOf(cap)` 返回false时（能力值为0），该能力被视为丧失。

## 4. 典型致死Hediff示例表

| HediffDef | 中文名 | lethalSeverity | 致死机制 | 来源 |
|-----------|--------|---------------|---------|------|
| `BloodLoss` | 失血 | 1.0 | Severity达到1.0时致死 | 出血伤口持续累积 |
| `ToxicBuildup` | 毒素积累 | 1.0 | Severity达到1.0时致死 | 毒素环境、毒液攻击 |
| `Hypothermia` | 低温症 | 1.0 | Severity达到1.0时致死 | 温度系统直接写入Severity |
| `Heatstroke` | 中暑 | 1.0 | Severity达到1.0时致死 | 温度系统直接写入Severity |
| `DrugOverdose` | 药物过量 | 1.0 | Severity达到1.0时致死 | 短时间内服用过多药物 |
| `WoundInfection` | 伤口感染 | 1.0 | Severity达到1.0时致死 | 未治疗的伤口恶化 |
| `BloodRot` | 血腐病 | 1.0 | Severity达到1.0时致死 | Royalty DLC疾病 |
| `Plague` | 瘟疫 | 1.0 | Severity达到1.0时致死 | 免疫竞赛失败 |

> **规律**：绝大多数致死Hediff的`lethalSeverity`都是`1.0`，即Severity从0增长到1.0的过程就是"死亡倒计时"。

## 5. 其他致死路径

### 5.1 deathMtbDays随机死亡

源码位置：`Verse.Hediff.TickInterval()` → `DoMTBDeath()`

```csharp
// 每200 ticks检查一次
if (curStage.deathMtbDays > 0f && pawn.IsHashIntervalTick(200, delta)
    && Rand.MTBEventOccurs(curStage.deathMtbDays, 60000f, 200f))
{
    DoMTBDeath();
}
```

**机制**：HediffStage的`deathMtbDays`字段定义了"平均每X天触发一次死亡"的概率。每200 ticks（约3.3秒游戏时间）检查一次。

**Deathless基因豁免**：
```csharp
// DoMTBDeath()
if (!curStage.mtbDeathDestroysBrain && ModsConfig.BiotechActive)
{
    if (pawn.genes?.HasActiveGene(GeneDefOf.Deathless))
        return;  // Deathless基因豁免随机死亡
}
```

- 如果`mtbDeathDestroysBrain = false`且Pawn有Deathless基因 → 豁免
- 如果`mtbDeathDestroysBrain = true` → 即使有Deathless基因也会死亡，且死后摧毁大脑（无法复活）

**典型实例**：心脏病发作（HeartAttack）的严重阶段有`deathMtbDays`。

### 5.2 倒地即死（Death on Downed）

源码位置：`Verse.Pawn_HealthTracker.CheckForStateChange()`

当非玩家阵营的Pawn倒地时，有概率直接死亡而非进入倒地状态：

```
倒地即死概率计算优先级（从高到低）：
1. overrideDeathOnDownedChance ≥ 0  → 使用覆盖值
2. 变异体(Mutant) + deathOnDownedChance ≥ 0  → 使用变异体定义值
3. Deathless基因/preventsDeath + 玩家阵营  → 0%（不会倒地即死）
4. PawnKindDef.overrideDeathOnDownedChance  → 使用种类定义值
5. Anomaly实体阵营  → 按威胁点数曲线计算
6. 动物  → 50%
7. 机械体  → 100%（必定倒地即死）
8. 人形（非殖民者）→ 按人口意图曲线 × 难度系数
```

**关键排除条件**（不触发倒地即死）：
- 玩家阵营或玩家囚犯
- 野人（WildMan）
- 停用状态的Pawn
- 非外部暴力导致的倒地（除非Hediff有`canApplyDodChanceForCapacityChanges`）
- `forceDowned = true`时

**Deathless/preventsDeath特殊处理**：如果Pawn有Deathless基因或preventsDeath Hediff，倒地即死不会直接Kill，而是摧毁大脑（AddHediff MissingBodyPart到Brain）。

### 5.3 直接Kill调用

某些游戏逻辑直接调用`pawn.Kill()`，绕过`ShouldBeDead()`判定：
- **处决**：玩家手动处决囚犯
- **事件脚本**：某些事件直接杀死Pawn
- **HediffComp_DestroyOrgan**：摧毁器官导致死亡
- **forceDeathOnDowned**：PawnKindDef标记为倒地即死
- **HediffStage.destroyPart**：阶段触发摧毁部位

## 6. 疼痛休克机制（间接致死路径）

疼痛本身不直接致死，但通过"疼痛休克→倒地→倒地即死"形成间接致死路径。

### 6.1 疼痛计算（CalculatePain）

源码位置：`Verse.HediffSet.CalculatePain()`

```
总疼痛 = Clamp(
    (Σ hediff.PainOffset + genes.PainOffset + Σ trait.painOffset)
    × Π hediff.PainFactor × genes.PainFactor × Π trait.painFactor,
    0, 1
)
```

- **PainOffset**：加法叠加（各Hediff的疼痛贡献）
- **PainFactor**：乘法叠加（止痛器、基因等的疼痛倍率）
- 非肉体Pawn（机械体）疼痛恒为0
- 最终值钳制在[0, 1]范围

### 6.2 疼痛休克判定（InPainShock）

源码位置：`Verse.Pawn_HealthTracker.InPainShock`

```csharp
public bool InPainShock
{
    get
    {
        if (!pawn.kindDef.ignoresPainShock)
            return hediffSet.PainTotal >= pawn.GetStatValue(StatDefOf.PainShockThreshold);
        return false;
    }
}
```

- **PainShockThreshold**默认值：0.8（80%疼痛触发休克）
- `ignoresPainShock`：某些PawnKind免疫疼痛休克

### 6.3 倒地判定（ShouldBeDowned）

源码位置：`Verse.Pawn_HealthTracker.ShouldBeDowned()`

```csharp
// 以下任一条件满足即倒地：
// 1. InPainShock == true（疼痛休克）
// 2. !capacities.CanBeAwake（无法保持清醒，如意识过低）
// 3. !capacities.CapableOf(Moving) && !doesntMove（无法移动）
// 4. CurLifeStage.alwaysDowned（生命阶段强制倒地，如婴儿）
```

### 6.4 间接致死链

```
大量伤害 → 高疼痛 → InPainShock → ShouldBeDowned → 倒地
  → 非玩家阵营 → 倒地即死概率检查 → pawn.Kill()
```

## 7. 死亡预防机制

### 7.1 preventsDeath（完全死亡豁免）

```xml
<HediffDef>
    <defName>DeathRefusal</defName>
    <preventsDeath>true</preventsDeath>
    <!-- Anomaly DLC -->
</HediffDef>
```

- 在`ShouldBeDead()`的**最优先位置**检查
- 有此Hediff时，所有5个死亡条件都被跳过
- `DeathRefusal`触发后会消耗自身并执行复活逻辑

### 7.2 Biotech死眠/再生昏迷

源码位置：`Verse.Pawn_HealthTracker.CheckForStateChange()`

```csharp
if (ShouldBeDead())
{
    if (ShouldBeDeathrestingOrInComa())
    {
        ForceDeathrestOrComa(dinfo, hediff);  // 替代死亡
    }
    else if (!pawn.Destroyed)
    {
        pawn.Kill(dinfo, hediff);  // 真正死亡
    }
}
```

- 当Pawn满足死亡条件但有死眠/再生昏迷资格时，进入替代状态而非死亡
- 这是Biotech DLC的Sanguophage（血族）和Deathless基因的核心机制

### 7.3 Deathless基因

- **豁免deathMtbDays随机死亡**：除非`mtbDeathDestroysBrain=true`
- **倒地即死特殊处理**：不直接Kill，而是摧毁大脑
- **死眠替代**：满足条件时进入死眠而非死亡

## 8. 开发者要点

1. **死亡判定是被动触发的**：不是每帧检查，而是在Hediff增减/伤害应用后由`CheckForStateChange`触发。模组如果直接修改Severity而不通过标准API，可能不会触发死亡判定
2. **preventsDeath优先级最高**：它在所有死亡条件之前检查，是最可靠的死亡预防手段。模组设计"不死"效果时应使用此字段
3. **致命能力是肉体/机械体分开的**：`lethalFlesh`和`lethalMechanoids`独立配置，模组新增PawnCapacityDef时需注意区分
4. **累计伤害阈值与HealthScale成正比**：大型生物更难被"磨死"，小型生物更脆弱。模组自定义种族时HealthScale直接影响生存能力
5. **倒地即死是重要的间接致死路径**：非玩家Pawn的死亡很多通过此路径发生，模组设计敌人时需考虑`overrideDeathOnDownedChance`
6. **deathMtbDays是概率性的**：不是确定性致死，适合设计"有风险但不必然致死"的状态（如心脏病发作）
7. **疼痛休克→倒地→倒地即死**：这条间接链意味着高疼痛的Hediff对非玩家Pawn有间接致死效果

## 9. 关键源码引用表

| 文件 | 关键内容 |
|------|---------|
| `Verse/Pawn_HealthTracker.cs` | `CheckForStateChange`、`ShouldBeDead`、`ShouldBeDeadFromRequiredCapacity`、`ShouldBeDeadFromLethalDamageThreshold`、`InPainShock`、`ShouldBeDowned`、`LethalDamageThreshold` |
| `Verse/Hediff.cs` | `CauseDeathNow`、`IsLethal`、`DoMTBDeath`、`TickInterval`中的deathMtbDays检查 |
| `Verse/HediffDef.cs` | `lethalSeverity`、`preventsDeath`、`onlyLifeThreateningTo` |
| `Verse/HediffStage.cs` | `deathMtbDays`、`mtbDeathDestroysBrain` |
| `Verse/PawnCapacityDef.cs` | `lethalFlesh`、`lethalMechanoids` |
| `Verse/HediffSet.cs` | `HasPreventsDeath`、`CalculatePain` |
| `Verse/PawnCapacityUtility.cs` | `CalculatePartEfficiency`、`CalculateCapacityLevel` |
| `Core/Defs/PawnCapacityDefs/PawnCapacity.xml` | 5种致命能力的XML定义 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 初版：核心调用链、5个死亡判定条件、致命能力汇总、典型致死Hediff、其他致死路径、疼痛休克机制、死亡预防机制 | Claude Opus 4.6 |
