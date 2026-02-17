---
标题：各DLC能力实例与分类
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][未完成][未锁定]
摘要: RimWorld各DLC能力完整分类参考——Royalty超能力列表、Biotech基因能力、Ideology角色能力、Anomaly变异体能力、AbilityGroupDef共享冷却机制、能力获得途径汇总
---

## 1. 能力系统DLC分布总览

能力系统随Royalty DLC引入，Core本身不包含任何AbilityDef。

| DLC | 能力数量 | abilityClass | 成本系统 | 获得途径 |
|-----|---------|-------------|---------|---------|
| **Core** | 0 | — | — | — |
| **Royalty** | 20+ | `Psycast` | Psyfocus + Neural Heat | 心灵连接等级解锁 |
| **Biotech** | 6+ | `Ability` | Hemogen / 无 | GeneDef.abilities |
| **Ideology** | 5+ | `Ability` | 无（仅冷却） | Precept_Role |
| **Anomaly** | 5+ | `Ability` | 无 / 充能 | MutantDef / CompEquippableAbility |

## 2. Royalty超能力完整列表

所有超能力使用`Psycast`子类，通过`PsycastBase`/`PsycastBaseSkip`/`WordOfBase` XML模板统一配置。

### 2.1 按等级分组

#### Level 1（心灵连接1级）

| 能力 | defName | 效果 | Entropy | Psyfocus | 效果Comp |
|------|---------|------|---------|----------|---------|
| Burden（负重） | Burden | 减速目标 | 8 | 1% | GiveHediffPsychic |
| Stun（眩晕） | Stun | 眩晕目标 | 12 | 1% | GiveHediffPsychic |
| Solar Pinhole（太阳针孔） | SolarPinhole | 生成热源 | 5 | 1% | Spawn |
| Word of Trust（信任之言） | WordOfTrust | 降低囚犯抵抗 | 0 | 1% | OffsetPrisonerResistance |

#### Level 2

| 能力 | defName | 效果 | Entropy | Psyfocus | 效果Comp |
|------|---------|------|---------|----------|---------|
| Blinding Pulse（致盲脉冲） | BlindingPulse | 范围致盲 | 18 | 2% | GiveHediffPsychic |
| Vertigo Pulse（眩晕脉冲） | VertigoPulse | 范围眩晕 | 18 | 2% | GiveHediffPsychic |
| Painblock（止痛） | Painblock | 消除疼痛 | 0 | 2% | GiveHediffPsychic |
| Beckon（召唤） | Beckon | 强制目标接近 | 12 | 3% | GiveHediffPsychic |
| Waterskip（水传送） | Waterskip | 灭火 | 0 | 3% | Waterskip |

#### Level 3

| 能力 | defName | 效果 | Entropy | Psyfocus | 效果Comp |
|------|---------|------|---------|----------|---------|
| Chaos Skip（混乱传送） | ChaosSkip | 随机传送目标 | 20 | 3% | Teleport |
| Skip（传送） | Skip | 精确传送目标 | 25 | 3% | Teleport |
| Smokepop（烟雾弹） | Smokepop | 生成烟雾 | 0 | 3% | Smokepop |
| Focus（聚焦） | Focus | 增强视听移动 | 0 | 3% | GiveHediffPsychic |
| Word of Serenity（安宁之言） | WordOfSerenity | 终止精神崩溃 | 0 | 变动 | StopMentalState |
| Manhunter Pulse（猎杀脉冲） | ManhunterPulse | 驱使动物猎杀 | 35 | 5% | GiveMentalState |

#### Level 4

| 能力 | defName | 效果 | Entropy | Psyfocus | 效果Comp |
|------|---------|------|---------|----------|---------|
| Berserk（狂暴） | Berserk | 使目标狂暴 | 30 | 4% | GiveMentalState |
| Invisibility（隐身） | Invisibility | 使目标隐身 | 0 | 5% | GiveHediffPsychic |
| Neural Heat Dump（神经热量转移） | EntropyDump | 转移Entropy给目标 | 0 | 0 | TransferEntropy |
| Word of Joy（欢乐之言） | WordOfJoy | 提升心情 | 0 | 4% | GiveHediffPsychic |

#### Level 5

| 能力 | defName | 效果 | Entropy | Psyfocus | 效果Comp |
|------|---------|------|---------|----------|---------|
| Berserk Pulse（狂暴脉冲） | BerserkPulse | 范围狂暴 | 50 | 6% | GiveMentalState |
| Mass Chaos Skip（群体传送） | MassChaosSkip | 范围随机传送 | 40 | 5% | Teleport |
| Chunk Skip（石块传送） | Chunkskip | 传送石块砸击 | 30 | 5% | Chunkskip |
| Wallraise（升墙） | Wallraise | 升起岩墙 | 30 | 5% | Wallraise |
| Farskip（远距传送） | Farskip | 世界地图传送 | 0 | 6% | Farskip |
| Word of Inspiration（灵感之言） | WordOfInspiration | 给予灵感 | 0 | 5% | GiveInspiration |

#### Level 6

| 能力 | defName | 效果 | Entropy | Psyfocus | 效果Comp |
|------|---------|------|---------|----------|---------|
| Neuroquake（神经地震） | Neuroquake | 超大范围心灵攻击 | 极高 | 高 | Neuroquake |

### 2.2 超能力成本系统

**Psyfocus（心灵聚焦）**：
- 范围：0~1浮点值
- 补充：冥想（唯一途径）
- 消耗：施放超能力扣除`Ability_PsyfocusCost`
- 3个Band分段：0~0.25 / 0.25~0.5 / 0.5~1.0
- 高Band解锁高级能力但衰减更快

**Neural Heat（Entropy/神经热量）**：
- 范围：0~MaxEntropy（可超限至2×Max）
- 增加：施放超能力增加`Ability_EntropyGain`
- 恢复：自动恢复（`EntropyRecoveryRate` Stat）
- 超限阈值：4级（Overloaded/VeryOverloaded/Extreme/Overwhelming）

**Psycast.Activate()流程**：
```
1. 检查Entropy是否会溢出 → TryAddEntropy(def.EntropyGain)
2. 扣除Psyfocus → OffsetPsyfocusDirectly(-FinalPsyfocusCost)
3. 播放心灵视觉效果
4. 调用base.Activate() → 执行CompAbilityEffect.Apply()
```

## 3. Biotech基因能力完整列表

通过`GeneDef.abilities`字段赋予，使用`Ability`基类（非Psycast）。

| 能力 | defName | 关联基因 | 效果Comp | 成本 | 说明 |
|------|---------|---------|---------|------|------|
| Bloodfeed（吸血） | Bloodfeed | Bloodfeeder | BloodfeederBite | 无（补充Hemogen） | 吸取目标血液，补充Hemogen |
| Reimplant Xenogerm（基因植入） | ReimplantXenogerm | XenogermReimplanter | ReimplantXenogerm | 无 | 将自身Xenogene植入目标 |
| Longjump（远跳） | Longjump | Longjump | Teleport | 无 | 跳跃到目标位置 |
| Coagulate（凝血） | Coagulate | Coagulate | Coagulate | HemogenCost | 消耗Hemogen止血 |
| Acid Spray（酸液喷射） | AcidSpray | AcidSpray | SprayLiquid | 无 | 喷射酸液 |
| Fire Spew（喷火） | FireBreath | FireSpew | FireSpew | 无 | 喷射火焰 |
| Resurrect Mech（复活机械体） | ResurrectMech | — | ResurrectMech | 无 | 机械师复活机械体 |

**基因能力获得机制**：
```xml
<GeneDef>
  <defName>Bloodfeeder</defName>
  <abilities>
    <li>Bloodfeed</li>
  </abilities>
</GeneDef>
```
- 基因Active时自动获得能力（通过Hediff的abilities字段或Gene的PostAdd）
- 基因Inactive时能力自动移除
- 不需要自定义Gene子类

**Hemogen成本机制**：
- `CompAbilityEffect_HemogenCost`检查并扣除Hemogen
- 通过`CompProperties_AbilityHemogenCost.hemogenCost`配置消耗量
- 施放前检查`GeneUtility.HasEnoughHemogenToUse(pawn, cost)`

## 4. Ideology角色能力完整列表

通过`Precept_Role`赋予，使用`AbilityGroupDef`共享冷却。

| 能力 | defName | 角色 | 效果Comp | 共享冷却组 | 说明 |
|------|---------|------|---------|-----------|------|
| Convert（转化） | Convert | 道德向导 | Convert | Interaction | 尝试转化目标信仰 |
| Counsel（辅导） | Counsel | 道德向导 | Counsel | Interaction | 辅导目标，改善心情 |
| Reassure（安慰） | Reassure | 道德向导 | Reassure | Interaction | 安慰目标，减少负面思想 |
| PreachHealth（健康布道） | PreachHealth | 道德向导 | PreachHealth | Interaction | 布道健康，治疗轻伤 |
| Trial（审判） | Trial | 领袖 | — | — | 审判囚犯 |

### 4.1 AbilityGroupDef共享冷却机制

**核心概念**：同一`AbilityGroupDef`的能力共享冷却计时器。

```xml
<AbilityGroupDef>
  <defName>Interaction</defName>
  <cooldownTicks>60000</cooldownTicks>  <!-- 1天 -->
  <sendMessageOnCooldownComplete>true</sendMessageOnCooldownComplete>
</AbilityGroupDef>
```

**工作流程**：
1. 施放Convert → Convert进入冷却
2. 同时Counsel/Reassure/PreachHealth也进入冷却（共享Interaction组）
3. 冷却结束后所有组内能力同时可用

**源码机制**（`Ability.StartCooldown`）：
```csharp
if (def.groupDef != null)
{
    // 通知同组所有能力进入冷却
    foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
    {
        if (ability.def.groupDef == def.groupDef && ability != this)
        {
            ability.StartCooldown(cooldownTicks);
        }
    }
}
```

### 4.2 角色能力获得机制

```
Precept_Role (意识形态角色)
  → roleAbilities (List<AbilityDef>)
  → AbilitiesFor(pawn) 返回角色能力列表
  → Pawn_AbilityTracker.AllAbilitiesForReading 聚合
```

- 角色Active时能力可用
- 角色被移除时能力自动消失
- `requiredMemes`字段可限制特定模因才能使用

## 5. Anomaly变异体/装备能力完整列表

### 5.1 变异体能力

通过`MutantDef`赋予，变异体Pawn专属：

| 能力 | defName | 变异体类型 | 效果Comp | 说明 |
|------|---------|----------|---------|------|
| Unnatural Healing（非自然治愈） | UnnaturalHealing | 多种 | UnnaturalHealing | 治愈伤口 |
| Psychic Slaughter（心灵屠杀） | PsychicSlaughter | 多种 | PsychicSlaughter | 范围心灵攻击 |
| Corrosive Spray（腐蚀喷射） | CorrosiveSpray | 多种 | SprayLiquid | 喷射腐蚀液体 |
| Transmute（嬗变） | Transmute | 多种 | Transmute | 嬗变物质 |
| Fire Burst（火焰爆发） | FireBurst | 多种 | FireBurst | 范围火焰 |

**变异体能力白名单**：
```csharp
// Pawn_AbilityTracker.AllAbilitiesForReading
if (ModsConfig.AnomalyActive && pawn.IsMutant && pawn.mutant.Def.abilityWhitelist.Any())
{
    allAbilitiesCached = allAbilitiesCached
        .Where(a => pawn.mutant.Def.abilityWhitelist.Contains(a.def)).ToList();
}
```
变异体的`abilityWhitelist`过滤掉不在白名单中的能力。

### 5.2 装备能力

通过`CompEquippableAbility`/`CompEquippableAbilityReloadable`赋予：

| 能力 | 装备 | 效果Comp | 充能 | 说明 |
|------|------|---------|------|------|
| Metalblood Injection（金属血注射） | 特定装备 | GiveHediff | 可充能 | 注射金属血 |

**装备能力获得机制**：
```
Pawn装备武器
  → Pawn_AbilityTracker.AllAbilitiesForReading
    → pawn.equipment.Primary.TryGetComp<CompEquippableAbility>()
      → compEquippableAbility.AbilityForReading
```

## 6. 能力获得途径汇总

| # | 途径 | 代码路径 | 持久性 | 典型场景 |
|---|------|---------|--------|---------|
| 1 | **直接添加** | `pawn.abilities.GainAbility(def)` | 永久（保存/加载） | 代码直接赋予 |
| 2 | **Hediff赋予** | `hediff.def.abilities` | 随Hediff存在 | 心灵连接器→超能力 |
| 3 | **基因赋予** | `geneDef.abilities` | 随基因Active | 吸血基因→吸血能力 |
| 4 | **装备赋予** | `CompEquippableAbility` | 随装备穿戴 | 特殊武器→附加能力 |
| 5 | **服装赋予** | `apparel.AllAbilitiesForReading` | 随服装穿戴 | 特殊服装→附加能力 |
| 6 | **皇室爵位** | `pawn.royalty.AllAbilitiesForReading` | 随爵位等级 | 帝国爵位→超能力 |
| 7 | **意识形态角色** | `precept_Role.AbilitiesFor(pawn)` | 随角色任命 | 道德向导→社交能力 |
| 8 | **变异体** | `pawn.mutant.AllAbilitiesForReading` | 随变异状态 | 变异体→特殊能力 |

**选择建议**：

| 场景 | 推荐途径 | 原因 |
|------|---------|------|
| 永久能力 | 直接添加 | 保存/加载，不依赖外部条件 |
| 条件性能力 | Hediff赋予 | Hediff存在时有效，移除时消失 |
| 种族特性 | 基因赋予 | 与基因系统集成，支持遗传 |
| 物品附带 | 装备/服装赋予 | 穿戴时有效，卸下时消失 |
| 社会角色 | 角色赋予 | 与意识形态系统集成 |

## 7. XML配置示例

### 7.1 通过基因赋予能力

```xml
<!-- GeneDef中声明abilities -->
<GeneDef>
  <defName>MyCustomGene</defName>
  <label>custom ability gene</label>
  <abilities>
    <li>MyCustomAbility</li>
  </abilities>
</GeneDef>

<!-- 对应的AbilityDef -->
<AbilityDef>
  <defName>MyCustomAbility</defName>
  <label>custom ability</label>
  <description>A custom ability granted by a gene.</description>
  <iconPath>UI/Abilities/MyAbility</iconPath>
  <cooldownTicksRange>1200</cooldownTicksRange>
  <verbProperties>
    <verbClass>Verb_CastAbility</verbClass>
    <range>15</range>
    <warmupTime>1.5</warmupTime>
  </verbProperties>
  <comps>
    <li Class="CompProperties_AbilityGiveHediff">
      <compClass>CompAbilityEffect_GiveHediff</compClass>
      <hediffDef>MyCustomHediff</hediffDef>
      <durationMultiplier>Ability_Duration</durationMultiplier>
    </li>
  </comps>
  <statBases>
    <Ability_Duration>10</Ability_Duration>
  </statBases>
</AbilityDef>
```

### 7.2 通过Hediff赋予能力

```xml
<HediffDef>
  <defName>MyImplant</defName>
  <hediffClass>Hediff_Implant</hediffClass>
  <label>ability implant</label>
  <abilities>
    <li>MyImplantAbility</li>
  </abilities>
</HediffDef>
```

### 7.3 带AbilityGroupDef的共享冷却能力

```xml
<AbilityGroupDef>
  <defName>MyAbilityGroup</defName>
  <cooldownTicks>30000</cooldownTicks>
</AbilityGroupDef>

<AbilityDef>
  <defName>GroupAbility1</defName>
  <groupDef>MyAbilityGroup</groupDef>
  <!-- ... -->
</AbilityDef>

<AbilityDef>
  <defName>GroupAbility2</defName>
  <groupDef>MyAbilityGroup</groupDef>
  <!-- ... -->
</AbilityDef>
```

## 8. 关键源码引用表

| 符号 | 路径 | 说明 |
|------|------|------|
| `RimWorld.Psycast` | Psycast.cs | 超能力子类，Psyfocus/Entropy成本 |
| `RimWorld.Psycast.Activate` | Psycast.cs | 超能力激活，扣除成本 |
| `RimWorld.Psycast.CanCast` | Psycast.cs | 超能力可施放检查 |
| `RimWorld.Pawn_PsychicEntropyTracker` | Pawn_PsychicEntropyTracker.cs | Psyfocus/Entropy管理器 |
| `RimWorld.CompAbilityEffect_BloodfeederBite` | CompAbilityEffect_BloodfeederBite.cs | 吸血效果 |
| `RimWorld.CompAbilityEffect_Convert` | CompAbilityEffect_Convert.cs | 信仰转化效果 |
| `RimWorld.CompAbilityEffect_Counsel` | CompAbilityEffect_Counsel.cs | 辅导效果 |
| `RimWorld.CompAbilityEffect_Reassure` | CompAbilityEffect_Reassure.cs | 安慰效果 |
| `RimWorld.CompAbilityEffect_HemogenCost` | CompAbilityEffect_HemogenCost.cs | Hemogen成本检查 |
| `RimWorld.CompAbilityEffect_Teleport` | CompAbilityEffect_Teleport.cs | 传送/Skip效果 |
| `RimWorld.CompAbilityEffect_UnnaturalHealing` | CompAbilityEffect_UnnaturalHealing.cs | 非自然治愈 |
| `RimWorld.CompAbilityEffect_PsychicSlaughter` | CompAbilityEffect_PsychicSlaughter.cs | 心灵屠杀 |
| `RimWorld.CompEquippableAbility` | CompEquippableAbility.cs | 装备能力Comp |
| `RimWorld.CompEquippableAbilityReloadable` | CompEquippableAbilityReloadable.cs | 可充能装备能力 |
| `RimWorld.Precept_Role` | Precept_Role.cs | 意识形态角色（赋予能力） |
| `RimWorld.AbilityGroupDef` | AbilityGroupDef.cs | 能力组（共享冷却） |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 初始创建：4个DLC能力完整列表、超能力成本系统、基因能力机制、角色能力共享冷却、变异体能力白名单、能力获得途径汇总、XML配置示例 | Claude Opus 4.6 |
