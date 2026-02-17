---
标题：Ability系统基本结构
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][未完成][未锁定]
摘要: RimWorld能力系统完整技术参考——三层架构、AbilityDef完整字段表、Ability核心方法、CompAbilityEffect继承树、施放流程调用链、XML配置示例
---

## 1. 三层架构总览

| 层级 | 类 | 命名空间 | 行数 | 职责 |
|------|---|---------|------|------|
| **定义层** | `AbilityDef` | RimWorld | 440 | 继承Def，50+字段定义能力的所有静态属性 |
| **实例层** | `Ability` | RimWorld | 1309 | 运行时对象，实现`IVerbOwner`/`IExposable`，管理冷却/充能/施放 |
| **管理层** | `Pawn_AbilityTracker` | RimWorld | 189 | Pawn级管理器，聚合7个来源，提供增删查API |

辅助类：

| 类 | 命名空间 | 行数 | 职责 |
|---|---------|------|------|
| `AbilityComp` | RimWorld | 51 | 能力组件抽象基类，生命周期回调 |
| `CompAbilityEffect` | RimWorld | 148 | 效果组件基类，30+子类实现具体效果 |
| `Psycast` | RimWorld | 189 | Ability唯一子类，超能力专用（Psyfocus/Entropy成本） |
| `Command_Ability` | RimWorld | — | 能力Gizmo（命令按钮） |
| `Command_Psycast` | RimWorld | — | 超能力专用Gizmo（继承Command_Ability） |
| `Verb_CastAbility` | RimWorld | — | 能力施放Verb |
| `AbilityUtility` | RimWorld | — | 工具类（MakeAbility、验证方法等） |
| `AbilityGroupDef` | RimWorld | — | 能力组定义（共享冷却） |
| `AbilityCategoryDef` | RimWorld | — | 能力分类定义（UI排序） |

## 2. AbilityDef完整字段表

### 2.1 核心字段

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `abilityClass` | Type | `typeof(Ability)` | 运行时实例类（Psycast用`typeof(Psycast)`） |
| `gizmoClass` | Type | `typeof(Command_Ability)` | Gizmo类 |
| `comps` | List\<AbilityCompProperties\> | 空列表 | 组件列表（效果、成本等） |
| `category` | AbilityCategoryDef | null | 能力分类（影响UI排序） |
| `displayOrder` | int | 0 | 分类内排序 |
| `statBases` | List\<StatModifier\> | null | Stat基础值（Psyfocus成本、Entropy增益、效果半径、持续时间等） |
| `verbProperties` | VerbProperties | — | Verb配置（射程、预热时间、目标参数等） |
| `hotKey` | KeyBindingDef | null | 快捷键绑定 |
| `jobDef` | JobDef | null | 关联的Job定义 |

### 2.2 冷却与充能

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `cooldownTicksRange` | IntRange | — | 冷却时间范围（ticks） |
| `charges` | int | -1 | 充能次数（-1=无限） |
| `cooldownPerCharge` | bool | false | 每次充能独立冷却 |
| `hasExternallyHandledCooldown` | bool | false | 冷却由外部系统管理 |
| `sendLetterOnCooldownComplete` | bool | false | 冷却完成时发送信件 |
| `sendMessageOnCooldownComplete` | bool | false | 冷却完成时发送消息 |

### 2.3 目标与AI

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `targetRequired` | bool | true | 是否需要目标 |
| `targetWorldCell` | bool | false | 目标是否为世界地图格子 |
| `canUseAoeToGetTargets` | bool | true | 是否可用AOE获取目标 |
| `useAverageTargetPositionForWarmupEffecter` | bool | false | 预热特效使用平均目标位置 |
| `aiCanUse` | bool | false | AI是否可使用此能力 |
| `ai_SearchAOEForTargets` | bool | false | AI是否搜索AOE目标 |
| `ai_IsOffensive` | bool | true | AI视为攻击性能力 |
| `ai_IsIncendiary` | bool | true | AI视为燃烧性能力 |

### 2.4 分组与等级

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `groupDef` | AbilityGroupDef | null | 能力组（共享冷却） |
| `groupAbility` | bool | false | 是否为组能力 |
| `overrideGroupCooldown` | bool | false | 是否覆盖组冷却 |
| `level` | int | 0 | 能力等级（超能力用，对应心灵连接等级） |
| `requiredMemes` | List\<MemeDef\> | null | 需要的意识形态模因 |

### 2.5 视觉与音效

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `warmupMote` | ThingDef | null | 预热Mote |
| `warmupEffecter` | EffecterDef | null | 预热Effecter |
| `emittedFleck` | FleckDef | null | 发射Fleck |
| `emissionInterval` | int | 0 | Fleck发射间隔 |
| `warmupStartSound` | SoundDef | null | 预热开始音效 |
| `warmupSound` | SoundDef | null | 预热持续音效 |
| `warmupPreEndSound` | SoundDef | null | 预热即将结束音效 |
| `warmupPreEndSoundSeconds` | float | 0 | 预热结束前多少秒播放 |
| `warmupMoteSocialSymbol` | string | null | 社交符号纹理路径 |
| `moteDrawOffset` | Vector3 | — | Mote绘制偏移 |
| `moteOffsetAmountTowardsTarget` | float | 0 | Mote朝向目标偏移量 |

### 2.6 显示与行为

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `displayGizmoWhileUndrafted` | bool | false | 非征召时显示Gizmo |
| `disableGizmoWhileUndrafted` | bool | true | 非征召时禁用Gizmo |
| `showWhenDrafted` | bool | true | 征召时显示 |
| `showOnCharacterCard` | bool | true | 角色卡片显示 |
| `showGizmoOnWorldView` | bool | false | 世界视图显示Gizmo |
| `showCastingProgressBar` | bool | false | 显示施放进度条 |
| `hostile` | bool | true | 是否为敌对能力 |
| `casterMustBeCapableOfViolence` | bool | true | 施放者必须能执行暴力 |
| `writeCombatLog` | bool | false | 写入战斗日志 |
| `stunTargetWhileCasting` | bool | false | 施放时眩晕目标 |
| `waitForJobEnd` | bool | false | 等待Job结束 |
| `confirmationDialogText` | string | null | 确认对话框文本 |
| `iconPath` | string | null | 图标路径 |
| `uiOrder` | float | 0 | UI排序 |

### 2.7 超能力专用

| 字段/属性 | 类型 | 说明 |
|----------|------|------|
| `showPsycastEffects` | bool | 显示超能力视觉效果（默认true） |
| `detectionChanceOverride` | float | 帝国检测概率覆盖（-1=使用Stat） |
| `IsPsycast` | bool（属性） | 是否为超能力（检查abilityClass是否为Psycast） |
| `EntropyGain` | float（属性） | Entropy增益（从statBases读取） |
| `PsyfocusCost` | float（属性） | Psyfocus成本（从statBases读取） |
| `EffectRadius` | float（属性） | 效果半径（从statBases读取） |
| `RequiredPsyfocusBand` | int（属性） | 所需Psyfocus Band等级 |

## 3. Ability类核心方法

### 3.1 生命周期

| 方法 | 说明 |
|------|------|
| `Ability(Pawn pawn)` | 构造函数 |
| `Ability(Pawn pawn, AbilityDef def)` | 带定义的构造函数，初始化Verb和Comps |
| `AbilityTick()` | 每Tick调用，更新冷却/预热/Comps |
| `ExposeData()` | 保存/加载（冷却Tick、充能数、Comps） |

### 3.2 施放流程

| 方法 | 说明 |
|------|------|
| `Activate(LocalTargetInfo target, LocalTargetInfo dest)` | 激活能力（本地目标） |
| `Activate(GlobalTargetInfo target)` | 激活能力（世界目标） |
| `ApplyEffects(effects, target, dest)` | 应用效果Comps（virtual，Psycast重写添加免疫检查） |
| `QueueCastingJob(target, dest)` | 排队施放Job |
| `StartCooldown(int ticks)` | 启动冷却 |

### 3.3 状态查询

| 属性/方法 | 返回类型 | 说明 |
|----------|---------|------|
| `CanCast` | AcceptanceReport | 是否可施放（冷却、充能、Comps检查） |
| `Casting` | bool | 是否正在施放 |
| `CooldownTicksRemaining` | int | 剩余冷却Ticks |
| `CooldownTicksTotal` | int | 总冷却Ticks |
| `GizmoDisabled(out reason)` | bool | Gizmo是否禁用（含原因） |
| `CanApplyOn(target)` | bool | 是否可对目标施放 |
| `AICanTargetNow(target)` | bool | AI是否可对目标施放 |

### 3.4 Gizmo

| 方法 | 说明 |
|------|------|
| `GetGizmos()` | 返回Command_Ability Gizmo |
| `GetGizmosExtra()` | 返回额外Gizmo |
| `GizmosVisible()` | Gizmo是否可见 |

## 4. Pawn_AbilityTracker API速查

| 方法/属性 | 说明 |
|----------|------|
| `AllAbilitiesForReading` | 聚合7个来源的所有能力（缓存，脏标记刷新） |
| `GainAbility(AbilityDef)` | 添加直接能力（去重） |
| `RemoveAbility(AbilityDef)` | 移除直接能力 |
| `GetAbility(AbilityDef, includeTemporary)` | 获取能力实例 |
| `AICastableAbilities(target, offensive)` | AI可施放的能力列表 |
| `GetGizmos()` | 返回所有能力的Gizmo |
| `AbilitiesTick()` | Tick所有能力 |
| `Notify_TemporaryAbilitiesChanged()` | 标记缓存脏（来源变化时调用） |

## 5. CompAbilityEffect完整子类继承树

```
AbilityComp (抽象基类, 51行)
│   Initialize(), CompTick(), CompGetGizmosExtra()
│
└── CompAbilityEffect (效果基类, 148行)
    │   Apply(target, dest), Valid(target), CanApplyOn(target)
    │   AICanTargetNow(target), DrawEffectPreview(target)
    │
    ├── CompAbilityEffect_WithDuration (带持续时间基类)
    │   ├── CompAbilityEffect_GiveHediff (给予Hediff)
    │   │   └── CompAbilityEffect_GiveHediffPsychic (给予心灵Hediff)
    │   └── ...
    │
    ├── CompAbilityEffect_WithDest (带目的地基类)
    │   └── CompAbilityEffect_Teleport (传送/Skip)
    │
    ├── 状态施加类
    │   ├── CompAbilityEffect_GiveMentalState (给予精神状态)
    │   ├── CompAbilityEffect_GiveRandomHediff (给予随机Hediff)
    │   ├── CompAbilityEffect_GiveInspiration (给予灵感)
    │   ├── CompAbilityEffect_StopMentalState (停止精神状态)
    │   └── CompAbilityEffect_StopManhunter (停止猎杀)
    │
    ├── 传送类
    │   ├── CompAbilityEffect_Farskip (远距传送)
    │   ├── CompAbilityEffect_Chunkskip (石块传送)
    │   └── CompAbilityEffect_Waterskip (水传送)
    │
    ├── 伤害类
    │   ├── CompAbilityEffect_Explosion (爆炸)
    │   ├── CompAbilityEffect_FireSpew (喷火)
    │   ├── CompAbilityEffect_FireBurst (火焰爆发)
    │   ├── CompAbilityEffect_SprayLiquid (喷射液体)
    │   ├── CompAbilityEffect_Flashstorm (闪电风暴)
    │   ├── CompAbilityEffect_PsychicSlaughter (心灵屠杀)
    │   ├── CompAbilityEffect_Burner (燃烧)
    │   └── CompAbilityEffect_LaunchProjectile (发射投射物)
    │
    ├── 治疗类
    │   ├── CompAbilityEffect_FixWorstHealthCondition (治疗最严重状况)
    │   ├── CompAbilityEffect_UnnaturalHealing (非自然治愈)
    │   ├── CompAbilityEffect_Coagulate (凝血)
    │   └── CompAbilityEffect_RemoveHediff (移除Hediff)
    │
    ├── 社交类
    │   ├── CompAbilityEffect_Convert (转化信仰)
    │   ├── CompAbilityEffect_Counsel (辅导)
    │   ├── CompAbilityEffect_Reassure (安慰)
    │   ├── CompAbilityEffect_BloodfeederBite (吸血)
    │   ├── CompAbilityEffect_PreachHealth (健康布道)
    │   └── CompAbilityEffect_OffsetPrisonerResistance (降低囚犯抵抗)
    │
    ├── 资源类
    │   ├── CompAbilityEffect_TransferEntropy (转移神经热量)
    │   └── CompAbilityEffect_HemogenCost (血源素消耗)
    │
    ├── 视觉类
    │   ├── CompAbilityEffect_FleckOnTarget (目标Fleck)
    │   ├── CompAbilityEffect_MoteOnTarget (目标Mote)
    │   ├── CompAbilityEffect_EffecterOnTarget (目标Effecter)
    │   ├── CompAbilityEffect_EffecterOnCaster (施放者Effecter)
    │   ├── CompAbilityEffect_Smokepop (烟雾弹)
    │   ├── CompAbilityEffect_Firefoampop (泡沫弹)
    │   └── CompAbilityEffect_Wallraise (升墙)
    │
    └── 其他
        ├── CompAbilityEffect_StartRitual (开始仪式)
        ├── CompAbilityEffect_Spawn (生成物体)
        ├── CompAbilityEffect_ReimplantXenogerm (基因再植入)
        ├── CompAbilityEffect_ResurrectMech (复活机械体)
        ├── CompAbilityEffect_Neuroquake (神经地震)
        ├── CompAbilityEffect_Transmute (嬗变)
        ├── CompAbilityEffect_ConsumeLeap (消耗跳跃)
        ├── CompAbilityEffect_TrainRandomSkill (训练随机技能)
        └── CompAbilityEffect_BrainDamageChance (脑损伤概率)
```

## 6. CompProperties_AbilityEffect关键字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `psychic` | bool | 是否为心灵效果（影响PsychicSensitivity检查） |
| `goodwillImpact` | int | 好感度影响 |
| `screenShakeIntensity` | float | 屏幕震动强度 |
| `clamorType` | ClamorDef | 喧闹类型 |
| `clamorRadius` | float | 喧闹半径 |
| `sound` / `soundMale` / `soundFemale` | SoundDef | 音效（支持性别区分） |
| `message` | string | 消息文本 |
| `messageType` | MessageTypeDef | 消息类型 |
| `sendLetter` | bool | 是否发送信件 |
| `customLetterLabel` / `customLetterText` | string | 自定义信件 |
| `availableWhenTargetIsWounded` | bool | 目标受伤时是否可用 |
| `canTargetBaby` | bool | 是否可对婴儿使用 |
| `canTargetBosses` | bool | 是否可对Boss使用 |
| `applicableToMechs` | bool | 是否对机械体有效 |
| `applyGoodwillImpactToLodgers` | bool | 是否对寄宿者应用好感度影响 |

## 7. 施放流程详细调用链

```
1. 玩家点击Gizmo
   Command_Ability.ProcessInput()
     → Ability.QueueCastingJob(target, dest)
       → pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(def.jobDef ?? JobDefOf.CastAbilityOnThing))

2. Job执行
   JobDriver_CastAbilityOnThing / JobDriver_CastAbilityOnWorldCell
     → verb.TryStartCastOn(target)
       → Verb_CastAbility.TryCastShot()

3. Verb施放
   Verb_CastAbility.TryCastShot()
     → ability.Activate(target, dest)

4. 能力激活
   Ability.Activate(target, dest)
     ├── [Psycast重写] 扣除Entropy + Psyfocus
     ├── ApplyEffects(EffectComps, targets, dest)
     │   └── 遍历每个CompAbilityEffect
     │       ├── effect.CanApplyOn(target) 检查
     │       └── effect.Apply(target, dest) 应用效果
     ├── PostApplied(targets, map)
     └── StartCooldown()

5. 冷却管理
   Ability.AbilityTick()
     ├── cooldownTicks-- (每Tick递减)
     ├── cooldownTicks <= 0 → 冷却结束
     └── groupDef?.Notify_AbilityCooldownStarted() (组冷却同步)
```

## 8. Verb_CastAbility机制

`Verb_CastAbility`继承`Verb`，是能力施放的Verb实现：
- `TryCastShot()`：调用`ability.Activate()`
- 使用`verbProperties`配置射程、预热时间、目标参数
- 预热期间显示warmupMote/warmupEffecter
- 支持AOE目标选择（`canUseAoeToGetTargets`）

## 9. Command_Ability Gizmo机制

`Command_Ability`继承`Command`，是能力的UI按钮：
- 显示能力图标、冷却进度、充能数
- 点击后进入目标选择模式
- `GizmoOnGUI()`绘制冷却覆盖层
- `ProcessInput()`处理点击事件
- Psycast使用`Command_Psycast`子类（额外显示Psyfocus/Entropy信息）

## 10. XML配置示例

### 10.1 简单能力（基因能力，无Psycast成本）

```xml
<AbilityDef>
  <defName>FireBreath</defName>
  <label>fire breath</label>
  <description>Breathe a cone of fire at the target area.</description>
  <iconPath>UI/Abilities/FireBreath</iconPath>
  <cooldownTicksRange>600</cooldownTicksRange>
  <verbProperties>
    <verbClass>Verb_CastAbility</verbClass>
    <range>9.9</range>
    <warmupTime>1.0</warmupTime>
    <targetParams>
      <canTargetLocations>true</canTargetLocations>
      <canTargetPawns>false</canTargetPawns>
    </targetParams>
  </verbProperties>
  <comps>
    <li Class="CompProperties_AbilityFireSpew">
      <range>9.9</range>
      <lineWidthEnd>3</lineWidthEnd>
      <damAmount>10</damAmount>
    </li>
  </comps>
</AbilityDef>
```

### 10.2 超能力（Psycast，继承PsycastBase模板）

```xml
<AbilityDef ParentName="PsycastBase">
  <defName>Stun</defName>
  <label>stun</label>
  <description>Momentarily disrupt motor function in the target's brain.</description>
  <level>1</level>
  <iconPath>UI/Abilities/Stun</iconPath>
  <statBases>
    <Ability_EntropyGain>12</Ability_EntropyGain>
    <Ability_PsyfocusCost>0.01</Ability_PsyfocusCost>
    <Ability_Duration>2</Ability_Duration>
  </statBases>
  <verbProperties>
    <verbClass>Verb_CastAbility</verbClass>
    <range>20</range>
    <warmupTime>0.25</warmupTime>
    <targetParams>
      <canTargetSelf>false</canTargetSelf>
    </targetParams>
  </verbProperties>
  <comps>
    <li Class="CompProperties_AbilityGiveHediff">
      <compClass>CompAbilityEffect_GiveHediffPsychic</compClass>
      <hediffDef>PsychicStun</hediffDef>
      <psychic>true</psychic>
      <applicableToMechs>false</applicableToMechs>
    </li>
  </comps>
</AbilityDef>
```

### 10.3 带共享冷却的角色能力（Ideology）

```xml
<AbilityDef ParentName="AbilityTouchBase">
  <defName>Convert</defName>
  <label>convert</label>
  <description>Attempt to convert someone to your ideology.</description>
  <groupDef>Interaction</groupDef>
  <cooldownTicksRange>60000</cooldownTicksRange>
  <displayGizmoWhileUndrafted>true</displayGizmoWhileUndrafted>
  <disableGizmoWhileUndrafted>false</disableGizmoWhileUndrafted>
  <comps>
    <li Class="CompProperties_AbilityEffect">
      <compClass>CompAbilityEffect_Convert</compClass>
    </li>
  </comps>
</AbilityDef>
```

## 11. 关键源码引用表

| 符号 | 路径 | 行数 | 说明 |
|------|------|------|------|
| `RimWorld.AbilityDef` | AbilityDef.cs | 440 | 能力定义，50+字段 |
| `RimWorld.Ability` | Ability.cs | 1309 | 能力运行时实例 |
| `RimWorld.Pawn_AbilityTracker` | Pawn_AbilityTracker.cs | 189 | 能力管理器，7来源聚合 |
| `RimWorld.AbilityComp` | AbilityComp.cs | 51 | 能力组件抽象基类 |
| `RimWorld.CompAbilityEffect` | CompAbilityEffect.cs | 148 | 效果组件基类 |
| `RimWorld.Psycast` | Psycast.cs | 189 | 超能力子类 |
| `RimWorld.Command_Ability` | Command_Ability.cs | — | 能力Gizmo |
| `RimWorld.Command_Psycast` | Command_Psycast.cs | — | 超能力Gizmo |
| `RimWorld.Verb_CastAbility` | Verb_CastAbility.cs | — | 能力施放Verb |
| `RimWorld.AbilityUtility` | AbilityUtility.cs | — | 工具类 |
| `RimWorld.AbilityGroupDef` | AbilityGroupDef.cs | — | 能力组定义 |
| `RimWorld.CompProperties_AbilityEffect` | CompProperties_AbilityEffect.cs | — | 效果组件属性 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 初始创建：三层架构、AbilityDef完整字段表、Ability核心方法、Pawn_AbilityTracker API、CompAbilityEffect继承树、施放流程调用链、XML配置示例 | Claude Opus 4.6 |
