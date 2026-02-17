---
标题：伤害类型分类与DamageDef体系
版本号: v1.0
更新日期: 2026-02-11
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld全部~50种伤害类型的完整分类（Core+5DLC），含DamageDef 80+字段分析、DamageWorker 14子类继承树与特殊行为、3种护甲类别映射、护盾交互标志位决策矩阵、additionalHediffs两级附加机制、XML继承关系图、模组自定义伤害类型模板
---

# 伤害类型分类与DamageDef体系

**总览**：RimWorld通过`DamageDef`定义所有伤害类型，共约50种（Core ~36 + Biotech 3 + Anomaly 6 + Odyssey 7）。每种伤害类型的行为由三个维度决定：**护甲类别**（`armorCategory`：Sharp/Blunt/Heat/无，决定哪个护甲Stat防御）、**DamageWorker子类**（`workerClass`：14种，决定伤害如何分配到身体部位）、**行为标志位**（`isRanged`/`isExplosive`/`ignoreShields`等，决定护盾交互和系统行为）。

## 1. 伤害类型分类总表

按功能分6组。表中省略默认值（`isRanged=false`、`isExplosive=false`、`harmsHealth=true`等）。

### 1.1 近战物理伤害（12种）

| defName | 中文 | armorCat | Worker | hediff | 特殊机制 | DLC |
|---------|------|----------|--------|--------|---------|-----|
| **Cut** | 切割 | Sharp | Cut | Cut/Crack | 劈砍溢出(`cutCleaveBonus=1.4`) + `harmAllLayersUntilOutside` | Core |
| **Crush** | 碾压 | Blunt | AddInjury | Crush/Cut/Crack | 高过杀摧毁率(`overkillPct=0.4~1.0`) | Core |
| **Blunt** | 钝击 | Blunt | Blunt | Crush/Bruise/Crack | 内部冲击(`bluntInnerHitChance=0.4`) + 眩晕概率 + `buildingDmg×1.5` | Core |
| **Poke** | 戳刺 | Blunt | Stab | Crush/Bruise/Crack | `stabChanceOfForcedInternal=0.4` | Core |
| **Demolish** | 拆除 | Blunt | Blunt | Crush/Bruise/Crack | `buildingDmg×10` + `buildingDmgImpassable×0.75` | Core |
| **Stab** | 刺击 | Sharp | Stab | Stab/Crack | `stabChanceOfForcedInternal=0.6` | Core |
| **Scratch** | 抓挠 | Sharp | Scratch | Scratch/Crack | 分裂到2部位(`scratchSplitPct=0.67`) | Core |
| **ScratchToxic** | 毒性抓挠 | Sharp | Scratch | Scratch/Crack | +ToxicBuildup(`0.015/dmg`) | Core |
| **Bite** | 撕咬 | Sharp | Bite | Bite/Crack | 同Cut行为 | Core |
| **ToxicBite** | 毒性撕咬 | Sharp | Bite | Bite/Crack | +ToxicBuildup(`0.015/dmg`) | Core |
| **PorcupineBite** | 豪猪撕咬 | Sharp | Bite | Bite/Crack | +PorcupineQuill(同部位) | Odyssey |
| **PorcupineScratch** | 豪猪抓挠 | Sharp | Scratch | Scratch/Crack | +PorcupineQuill(同部位) | Odyssey |

### 1.2 远程物理伤害（11种）

| defName | 中文 | armorCat | Worker | hediff | 特殊机制 | DLC |
|---------|------|----------|--------|--------|---------|-----|
| **Bullet** | 子弹 | Sharp | AddInjury | Gunshot | `isRanged` + `harmAllLayersUntilOutside` + `makesAnimalsFlee` | Core |
| **BulletToxic** | 毒性子弹 | Sharp | AddInjury | Gunshot | +ToxicBuildup(`0.0065/dmg`) | Biotech |
| **Bullet_TraitTox** | 特质毒弹 | Sharp | AddInjury | Gunshot | +ToxicBuildup(`0.015/dmg`，更强) | Odyssey |
| **Bullet_TraitIncendiary** | 特质燃烧弹 | Sharp | AddInjury | Gunshot | `igniteChance`曲线(最高30%) | Odyssey |
| **Arrow** | 箭矢 | Sharp | AddInjury | Cut/Crack | `isRanged` | Core |
| **ArrowHighVelocity** | 高速箭矢 | Sharp | AddInjury | Stab | 改为刺伤hediff | Core |
| **RangedStab** | 远程刺击 | Sharp | Stab | Stab/Crack | `isRanged` + `stabChanceOfForcedInternal=0.6` | Core |
| **Bomb** | 炸弹 | Sharp | AddInjury | Shredded/Crack | `isExplosive` + `buildingDmgImpassable×4` + `plantDmg×4` | Core |
| **BombSuper** | 超级炸弹 | Sharp | AddInjury | Shredded/Crack | `defaultDamage=550` + `armorPen=1.30` | Core |
| **Thump** | 重击 | Sharp | AddInjury | Crush/Crack | `isExplosive` + `buildingDmgImpassable×15`(反建筑) | Core |
| **MiningBomb** | 采矿炸弹 | Sharp | MiningBomb | Shredded/Crack | `buildingDmgImpassable×30` + `plantDmg×10` | Odyssey |

### 1.3 热能/能量伤害（10种）

| defName | 中文 | armorCat | Worker | hediff | 特殊机制 | DLC |
|---------|------|----------|--------|--------|---------|-----|
| **Flame** | 火焰 | Heat | Flame | Burn | 点燃目标+地面起火 + `!hasForcefulImpact` + `!makesBlood` | Core |
| **Burn** | 灼烧 | Heat | AddInjury | Burn | 不点燃（与Flame区别） | Core |
| **AcidBurn** | 酸性灼烧 | **Sharp** | Flame | AcidBurn | 继承Flame但护甲改为Sharp | Core |
| **ElectricalBurn** | 电击灼烧 | Heat | Flame | ElectricalBurn | `minDamageToFragment=1` + 蓝色爆炸 | Anomaly |
| **VacuumBurn** | 真空灼烧 | (无) | AddInjury | VacuumBurn | `!makesBlood` + `armorPen=0` | Odyssey |
| **Vaporize** | 汽化 | Heat | Vaporize | Burn | `defaultDamage=800` + `armorPen=1` + `corpseDmg×0.1` | Core |
| **NociosphereVaporize** | Nociosphere汽化 | Heat | Vaporize | Burn | 同Vaporize，不同音效 | Anomaly |
| **Beam** | 光束 | Heat | AddInjury | BeamWound | `isRanged` + `buildingDmgImpassable×0.4` | Core |
| **BeamBypassShields** | 穿盾光束 | Heat | AddInjury | BeamWound | **`ignoreShields=true`**（唯一穿盾伤害） | Odyssey |
| **EnergyBolt** | 能量箭 | Sharp | AddInjury | EnergyBolt | `isRanged` + `igniteCellChance=1`(100%点燃地块) | Anomaly |

### 1.4 眩晕/控制伤害（5种）

| defName | 中文 | Worker | 特殊机制 | DLC |
|---------|------|--------|---------|-----|
| **Stun** | 眩晕 | Stun | `harmsHealth=false` + `causeStun` + `defaultDamage=20` | Core |
| **EMP** | 电磁脉冲 | Stun | `harmsHealth=false` + `causeStun` + `externalViolenceForMechanoids` + `stunResistStat=EMPResistance` + `stunAdaptation=2200` | Core |
| **NerveStun** | 神经眩晕 | Stun | `causeStun` + `stunAdaptation=240`(短) | Core |
| **MechBandShockwave** | 机械波冲击 | Stun | `causeStun` + `constantStunDuration=1200`(固定时长) | Biotech |
| **Nerve** | 神经伤害 | Nerve | 造成伤害 + 追加NerveStun眩晕 | Core |

### 1.5 环境/系统伤害（7种）

| defName | 中文 | Worker | hediff | 特殊机制 | DLC |
|---------|------|--------|--------|---------|-----|
| **Frostbite** | 冻伤 | Frostbite | Frostbite | `externalViolence=false` + `harmAllLayersUntilOutside` | Core |
| **TornadoScratch** | 龙卷风划伤 | AddInjury | Scratch/Crack | Sharp护甲 | Core |
| **Deterioration** | 劣化 | AddInjury | — | `!hasForcefulImpact` + `!makesBlood` + `!canInterruptJobs` | Core |
| **Mining** | 采矿 | AddInjury | — | 极简定义 | Core |
| **Rotting** | 腐烂 | AddInjury | — | 同Deterioration | Core |
| **Decayed** | 器官衰败 | AddInjury | Decayed | 代谢紊乱致死 | Core |
| **Digested** | 消化 | AddInjury | Digested | 被消化致死 | Anomaly |

### 1.6 功能性伤害（7种）

| defName | 中文 | Worker | 特殊机制 | DLC |
|---------|------|--------|---------|-----|
| **SurgicalCut** | 手术切口 | AddInjury | `armorCategory=null` + `!harmAllLayers` + `!hasForcefulImpact` | Core |
| **ExecutionCut** | 处决切割 | AddInjury | `execution=true` | Core |
| **Extinguish** | 灭火 | Extinguish | `harmsHealth=false` + `consideredHelpful` + hediff=CoveredInFirefoam | Core |
| **Smoke** | 烟雾 | AddInjury | `harmsHealth=false` + `defaultDamage=0` | Core |
| **ToxGas** | 毒气 | AddInjury | 同Smoke | Biotech |
| **DeadlifeDust** | 死灵尘埃 | AddInjury | 同Smoke | Anomaly |
| **Psychic** | 心灵伤害 | AddInjury | `externalViolence=true` + 3种hediff(普通/皮肤/固体) | Anomaly |

## 2. 护甲类别与防御Stat映射

3个`DamageArmorCategoryDef` + 无类别：

| 护甲类别 | 防御StatDef | 覆盖伤害类型 | 数量 |
|---------|-----------|------------|------|
| **Sharp（锐器）** | `ArmorRating_Sharp` | Cut, Stab, Scratch, Bite及其变体, Bullet系列, Arrow系列, RangedStab, Bomb系列, Thump, TornadoScratch, AcidBurn, EnergyBolt | ~23 |
| **Blunt（钝器）** | `ArmorRating_Blunt` | Blunt, Crush, Poke, Demolish | 4 |
| **Heat（热能）** | `ArmorRating_Heat` | Flame, Burn, ElectricalBurn, Vaporize系列, Beam系列 | ~7 |
| **(无)** | 无护甲防御 | Stun系列, Frostbite, SurgicalCut, ExecutionCut, Deterioration, Mining, Rotting, Smoke系列, Decayed, Digested, Psychic, VacuumBurn | ~16 |

> **注意**：AcidBurn继承自Flame（Heat父类）但覆盖`armorCategory`为Sharp——酸性伤害用锐器护甲防御，不用热能护甲。

护甲判定公式（详见[战斗伤害管线完整流程](战斗伤害管线完整流程.md)阶段5）：
```
effectiveArmor = max(armorRating - armorPenetration, 0)
roll = Rand.Value
roll < eA×0.5 → 完全偏转(damAmount=0)
roll < eA     → 半减(damAmount/=2, Sharp→Blunt转换)
else          → 全额穿透
```

## 3. DamageWorker继承树与特殊行为

### 3.1 继承树

```
DamageWorker (Verse, 基类)
├── DamageWorker_Stun          ← Stun, EMP, NerveStun, MechBandShockwave
├── DamageWorker_Extinguish    ← Extinguish
├── DamageWorker_AddGlobal     ← (原版未使用)
└── DamageWorker_AddInjury     ← Bullet, Bomb, Burn, Arrow, Crush等(默认)
    ├── DamageWorker_Cut       ← Cut
    ├── DamageWorker_Blunt     ← Blunt, Demolish
    ├── DamageWorker_Stab      ← Stab, RangedStab, Poke
    ├── DamageWorker_Scratch   ← Scratch, ScratchToxic, PorcupineScratch
    ├── DamageWorker_Bite      ← Bite, ToxicBite, PorcupineBite
    ├── DamageWorker_Flame     ← Flame, AcidBurn, ElectricalBurn
    ├── DamageWorker_Frostbite ← Frostbite
    ├── DamageWorker_Nerve     ← Nerve
    ├── DamageWorker_Vaporize  ← Vaporize, NociosphereVaporize
    └── DamageWorker_MiningBomb ← MiningBomb
```

### 3.2 各Worker特殊行为对比

| Worker | ChooseHitPart | ApplySpecialEffects | 关键DamageDef字段 |
|--------|--------------|--------------------|--------------------|
| **AddInjury** | 按Height+Depth筛选，coverageAbs加权随机 | 单部位伤害 | (基础行为) |
| **Cut** | 强制Outside | 劈砍溢出到相邻部位 | `cutExtraTargetsCurve`, `cutCleaveBonus` |
| **Blunt** | 强制Outside | 40%概率冲击内部固体子部位 + 眩晕概率 | `bluntInnerHitChance`, `bluntStunDuration`, 眩晕曲线 |
| **Stab** | 先选Outside，概率穿透Inside | 穿刺传播（内40%/外75%伤害） | `stabChanceOfForcedInternal` |
| **Scratch** | 强制Outside | 伤害分裂到命中部位+1个相邻部位 | `scratchSplitPercentage` |
| **Bite** | 强制Outside | 同Cut行为 | (继承Cut) |
| **Flame** | 按DamageInfo | 点燃目标Thing + 地面起火 | `scaleDamageToBuildingsBasedOnFlammability` |
| **Frostbite** | 仅Outside+有frostbiteVulnerability的部位 | 按frostbiteVulnerability加权 | (无额外字段) |
| **Nerve** | 同AddInjury | 造成伤害后追加NerveStun眩晕 | (无额外字段) |
| **Vaporize** | 同AddInjury | 内圈蒸发(摧毁部位) + 起火 | (无额外字段) |
| **MiningBomb** | 同AddInjury | 高建筑伤害 | (无额外字段) |
| **Stun** | N/A(不造成伤害) | 仅施加眩晕效果 | `causeStun`, `stunAdaptationTicks`, `constantStunDurationTicks` |
| **Extinguish** | N/A | 灭火 + 施加CoveredInFirefoam | `consideredHelpful` |

## 4. DamageDef关键字段分类表

DamageDef共80+字段（含爆炸视觉字段），按功能分8组。

### 4.1 基础标识

| 字段 | 类型 | 说明 |
|------|------|------|
| `workerClass` | Type | DamageWorker子类，默认`DamageWorker_AddInjury` |
| `deathMessage` | string | 致死时的死亡消息 |
| `combatLogRules` | RulePackDef | 战斗日志规则 |
| `impactSoundType` | ImpactSoundTypeDef | 命中音效类型 |

### 4.2 伤害属性

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `hediff` | HediffDef | — | 默认创建的Hediff |
| `hediffSkin` | HediffDef | — | 命中皮肤覆盖部位时的Hediff（覆盖hediff） |
| `hediffSolid` | HediffDef | — | 命中固体部位时的Hediff（覆盖hediff） |
| `harmAllLayersUntilOutside` | bool | false | 伤害是否穿透所有层直到外部 |
| `overkillPctToDestroyPart` | FloatRange | — | 过杀比例→摧毁部位概率曲线 |
| `minDamageToFragment` | int | — | 触发碎片化传播的最低伤害 |
| `defaultDamage` | int | -1 | 默认伤害量（武器未指定时使用） |
| `defaultArmorPenetration` | float | -1 | 默认穿甲值 |
| `defaultStoppingPower` | float | 0 | 默认停止力 |

### 4.3 行为标志

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `externalViolence` | bool | true | 是否算外部暴力（影响即死保护、死亡判定） |
| `externalViolenceForMechanoids` | bool | false | 对机械体是否算外部暴力（EMP专用） |
| `harmsHealth` | bool | true | 是否造成健康伤害（false=Stun/Smoke等） |
| `isRanged` | bool | false | 是否为远程伤害（影响CompShield拦截） |
| `isExplosive` | bool | false | 是否为爆炸伤害（影响CompShield拦截） |
| `ignoreShields` | bool | false | 是否绕过CompShield（仅BeamBypassShields=true） |
| `execution` | bool | false | 是否为处决（ExecutionCut专用） |
| `hasForcefulImpact` | bool | true | 是否有冲击力（false=火焰/手术等） |
| `makesBlood` | bool | true | 是否产生血迹 |
| `canInterruptJobs` | bool | true | 是否中断当前Job |
| `makesAnimalsFlee` | bool | false | 是否使动物逃跑 |
| `canUseDeflectMetalEffect` | bool | true | 是否可被金属偏转特效 |
| `consideredHelpful` | bool | false | 是否被视为有益（Extinguish专用） |

### 4.4 护甲交互

| 字段 | 类型 | 说明 |
|------|------|------|
| `armorCategory` | DamageArmorCategoryDef | 护甲类别（Sharp/Blunt/Heat/null） |

### 4.5 建筑/植物/尸体倍率

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `buildingDamageFactor` | float | 1.0 | 建筑伤害倍率 |
| `buildingDamageFactorPassable` | float | 1.0 | 可通行建筑伤害倍率 |
| `buildingDamageFactorImpassable` | float | 1.0 | 不可通行建筑伤害倍率 |
| `plantDamageFactor` | float | 1.0 | 植物伤害倍率 |
| `corpseDamageFactor` | float | 1.0 | 尸体伤害倍率 |

### 4.6 眩晕相关

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `causeStun` | bool | false | 是否造成眩晕 |
| `stunAdaptationTicks` | int | 0 | 眩晕适应时间（连续眩晕递减） |
| `constantStunDurationTicks` | int? | null | 固定眩晕时长（覆盖伤害量计算） |
| `stunResistStat` | StatDef | null | 眩晕抗性Stat（如EMPResistance） |

### 4.7 附加Hediff

| 字段 | 类型 | 说明 |
|------|------|------|
| `additionalHediffs` | List\<DamageDefAdditionalHediff\> | PostApplyDamage中全局添加（如ToxicBuildup） |
| `additionalHediffsThisPart` | List\<HediffDef\> | FinalizeAndAddInjury中同部位添加（如PorcupineQuill） |
| `applyAdditionalHediffsIfHuntingForFood` | bool | true | 狩猎时是否施加附加Hediff |

### 4.8 Worker特定字段

| 字段 | 类型 | Worker | 说明 |
|------|------|--------|------|
| `stabChanceOfForcedInternal` | float | Stab | 强制命中内部器官概率 |
| `cutExtraTargetsCurve` | SimpleCurve | Cut | 劈砍额外目标数量曲线 |
| `cutCleaveBonus` | float | Cut | 劈砍伤害加成倍率 |
| `bluntInnerHitChance` | float | Blunt | 内部冲击概率 |
| `bluntInnerHitDamageFractionToConvert` | FloatRange | Blunt | 转换为内伤的比例 |
| `bluntInnerHitDamageFractionToAdd` | FloatRange | Blunt | 额外内伤比例 |
| `bluntStunDuration` | float | Blunt | 眩晕持续时间 |
| `bluntStunChancePerDamagePctOfCorePartToHeadCurve` | SimpleCurve | Blunt | 头部眩晕概率曲线 |
| `bluntStunChancePerDamagePctOfCorePartToBodyCurve` | SimpleCurve | Blunt | 身体眩晕概率曲线 |
| `scratchSplitPercentage` | float | Scratch | 分裂伤害比例 |
| `scaleDamageToBuildingsBasedOnFlammability` | bool | Flame | 根据可燃性缩放建筑伤害 |
| `igniteChanceByTargetFlammability` | SimpleCurve | (任意) | 根据目标可燃性的点燃概率 |
| `igniteCellChance` | float | (任意) | 地块点燃概率 |

## 5. 护盾交互标志位详解

CompShield拦截判定条件（源码`CompShield.PostPreApplyDamage`）：

```csharp
if (!dinfo.Def.ignoreShields && (dinfo.Def.isRanged || dinfo.Def.isExplosive))
    → absorbed = true  // 拦截
```

**决策矩阵**：

| isRanged | isExplosive | ignoreShields | CompShield | CompProjectileInterceptor | 典型伤害 |
|----------|------------|---------------|------------|--------------------------|---------|
| true | false | false | **拦截** | **拦截**(投射物) | Bullet, Arrow, Beam |
| false | true | false | **拦截** | N/A(非投射物) | Bomb爆炸 |
| true | false | **true** | **穿透** | **拦截**(投射物) | BeamBypassShields |
| false | false | false | **穿透** | N/A | Cut, Blunt, Stab(近战) |
| false | false | false | **穿透** | N/A | Flame, Frostbite(环境) |

> **关键区别**：
> - `ignoreShields`仅影响CompShield，不影响CompProjectileInterceptor
> - CompProjectileInterceptor在投射物飞行阶段拦截，与DamageDef标志位无关
> - EMP不被CompShield拦截（`harmsHealth=false`，不走正常伤害流程），但会直接击破护盾（`energy=0`）

## 6. additionalHediffs附加机制

### 6.1 两个层级

| 层级 | 字段 | 触发位置 | 作用范围 | 典型场景 |
|------|------|---------|---------|---------|
| **全局** | `additionalHediffs` | 阶段7 PostApplyDamage | 全身（无指定部位） | ToxicBite→ToxicBuildup |
| **同部位** | `additionalHediffsThisPart` | 阶段6 FinalizeAndAddInjury | 与伤害同一部位 | PorcupineBite→PorcupineQuill |

### 6.2 DamageDefAdditionalHediff结构

```csharp
public class DamageDefAdditionalHediff  // Verse命名空间
{
    public HediffDef hediff;                    // 附加的Hediff
    public float severityPerDamageDealt;        // 每点伤害的Severity（与伤害量成正比）
    public float severityFixed;                 // 固定Severity（与伤害量无关）
    public StatDef victimSeverityScaling;       // 受害者Stat缩放（如ToxicResistance）
    public bool inverseStatScaling;             // 是否反向缩放（true=Stat越高Severity越低）
    public bool victimSeverityScalingByInvBodySize; // 是否按体型反比缩放
}
```

**Severity计算公式**（`Pawn_HealthTracker.PostApplyDamage`）：
```
severity = severityFixed > 0 ? severityFixed : totalDamageDealt × severityPerDamageDealt
if (victimSeverityScalingByInvBodySize) severity /= pawn.BodySize
if (victimSeverityScaling != null) severity *= pawn.GetStatValue(victimSeverityScaling)
if (inverseStatScaling) severity = 1/severity  // 实际是反向应用
```

### 6.3 原版使用实例

| DamageDef | additionalHediffs | severityPerDmg | Stat缩放 | 体型缩放 |
|-----------|-------------------|----------------|---------|---------|
| ScratchToxic | ToxicBuildup | 0.015 | ToxicResistance(反向) | 是 |
| ToxicBite | ToxicBuildup | 0.015 | ToxicResistance(反向) | 是 |
| BulletToxic | ToxicBuildup | 0.0065 | ToxicResistance(反向) | 是 |
| Bullet_TraitTox | ToxicBuildup | 0.015 | ToxicResistance(反向) | 是 |

| DamageDef | additionalHediffsThisPart | 说明 |
|-----------|--------------------------|------|
| PorcupineBite | PorcupineQuill | 豪猪刺嵌入命中部位 |
| PorcupineScratch | PorcupineQuill | 同上 |

## 7. XML继承关系图

DamageDef的`ParentName`继承树（仅显示有继承关系的）：

```
CutBase (Abstract)
├── Cut
├── SurgicalCut
└── ExecutionCut

BluntBase (Abstract)
├── Blunt
├── Poke
└── Demolish

StunBase (Abstract)
├── Stun
├── EMP
└── NerveStun

Scratch → ScratchToxic, PorcupineScratch
Bite → ToxicBite, PorcupineBite
Bullet → BulletToxic, Bullet_TraitTox, Bullet_TraitIncendiary
Arrow → ArrowHighVelocity, Nerve
Flame → Burn, AcidBurn, ElectricalBurn
Bomb → BombSuper, MiningBomb
Beam → BeamBypassShields
Vaporize → NociosphereVaporize
```

> **模组最佳实践**：自定义伤害类型优先通过`ParentName`继承现有DamageDef，只覆盖差异字段。这样自动继承Worker、护甲类别、音效等所有默认行为。

## 8. 模组自定义伤害类型XML模板

### 8.1 最简模板（继承现有父类）

```xml
<!-- 继承Bullet，添加自定义附加Hediff -->
<DamageDef ParentName="Bullet">
    <defName>MyMod_TrionBullet</defName>
    <label>trion bullet</label>
    <additionalHediffs>
        <li>
            <hediff>MyMod_TrionBurnout</hediff>
            <severityPerDamageDealt>0.01</severityPerDamageDealt>
        </li>
    </additionalHediffs>
</DamageDef>
```

### 8.2 完整自定义模板

```xml
<DamageDef>
    <defName>MyMod_TrionBeam</defName>
    <label>trion beam</label>
    <workerClass>DamageWorker_AddInjury</workerClass>
    <armorCategory>Heat</armorCategory>
    <hediff>MyMod_TrionBurn</hediff>
    <isRanged>true</isRanged>
    <isExplosive>false</isExplosive>
    <ignoreShields>false</ignoreShields>  <!-- true则穿透CompShield -->
    <makesAnimalsFlee>true</makesAnimalsFlee>
    <defaultDamage>15</defaultDamage>
    <defaultArmorPenetration>0.20</defaultArmorPenetration>
    <harmAllLayersUntilOutside>true</harmAllLayersUntilOutside>
    <impactSoundType>Bullet</impactSoundType>
    <additionalHediffs>
        <li>
            <hediff>MyMod_TrionExposure</hediff>
            <severityPerDamageDealt>0.02</severityPerDamageDealt>
            <victimSeverityScalingByInvBodySize>true</victimSeverityScalingByInvBodySize>
        </li>
    </additionalHediffs>
</DamageDef>
```

### 8.3 自定义DamageWorker（C#骨架）

```csharp
// 仅在现有Worker不满足需求时才自定义
public class DamageWorker_TrionBeam : DamageWorker_AddInjury
{
    // 重写部位选择逻辑
    protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
    {
        // 自定义命中部位选择
        return base.ChooseHitPart(dinfo, pawn);
    }

    // 重写特殊效果
    public override void ApplySpecialEffectsToPart(
        Pawn pawn, float totalDamage, DamageInfo dinfo, DamageResult result)
    {
        // 自定义伤害传播/附加效果
        base.ApplySpecialEffectsToPart(pawn, totalDamage, dinfo, result);
    }
}
```

## 9. 关键源码引用表

| 类 | 命名空间 | 关键方法/字段 | 职责 |
|----|---------|-------------|------|
| `DamageDef` | Verse | 80+字段 | 伤害类型定义 |
| `DamageDefAdditionalHediff` | Verse | hediff, severityPerDamageDealt等6字段 | 附加Hediff配置 |
| `DamageArmorCategoryDef` | Verse | armorRatingStat | 护甲类别定义 |
| `DamageWorker` | Verse | `Apply()` | 伤害应用基类 |
| `DamageWorker_AddInjury` | Verse | `ApplyDamageToPart()`, `ChooseHitPart()`, `ApplySpecialEffectsToPart()` | 造成Hediff_Injury的基础Worker |
| `DamageWorker_Cut` | Verse | `ApplySpecialEffectsToPart()` | 劈砍溢出 |
| `DamageWorker_Blunt` | Verse | `ApplySpecialEffectsToPart()` | 内部冲击+眩晕 |
| `DamageWorker_Stab` | Verse | `ChooseHitPart()` | 强制内部穿透 |
| `DamageWorker_Scratch` | Verse | `ApplySpecialEffectsToPart()` | 伤害分裂 |
| `DamageWorker_Flame` | Verse | `ApplySpecialEffectsToPart()` | 点燃目标 |
| `DamageWorker_Stun` | Verse | `Apply()` | 仅眩晕 |
| `DamageWorker_Vaporize` | Verse | `ApplySpecialEffectsToPart()` | 蒸发+起火 |
| `CompShield` | RimWorld | `PostPreApplyDamage()` | 护盾拦截（检查isRanged/isExplosive/ignoreShields） |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-11 | 创建文档：~50种伤害类型6组分类总表、3种护甲类别映射、14子类DamageWorker继承树与行为对比、DamageDef 80+字段8组分类表、护盾交互决策矩阵、additionalHediffs两级附加机制、XML继承关系图、模组自定义模板 | Claude Opus 4.6 |
