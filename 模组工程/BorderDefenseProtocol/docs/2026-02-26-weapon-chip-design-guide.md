---
标题：武器芯片设计说明书
版本号: v1.0
更新日期: 2026-02-26
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][未完成][未锁定]
摘要: 基于BDP现有架构基础设施，设计和实现一个武器芯片的全链路决策流程模板（近战+远程）。面向人类设计者和AI助手双用。
---

# 武器芯片设计说明书

## 1. 概述

### 1.1 文档目的

本文档是一份**决策驱动的设计模板**，用于在BDP现有架构基础设施上设计和实现一个武器芯片。

使用方式：按顺序走过每个决策点，在填空处填入你的设计值，最终输出完整的XML定义和测试计划。

### 1.2 适用范围

- 仅覆盖**武器芯片**（`WeaponChipEffect`），包括近战和远程两个子类型
- 不覆盖护盾芯片（`ShieldChipEffect`）和辅助芯片（`UtilityChipEffect`）
- 假设所有架构基础设施已就位，不涉及框架层修改

### 1.3 前置知识

设计者需了解以下已实现的系统：

| 系统 | 核心类 | 职责 |
|------|--------|------|
| 芯片效果协议 | `IChipEffect` | 统一的Activate/Deactivate/Tick/CanActivate接口 |
| 武器芯片效果 | `WeaponChipEffect` | 向触发体注入Verb/Tool配置 |
| 芯片Comp | `TriggerChipComp` + `CompProperties_TriggerChip` | 芯片物品的通用参数和效果实例 |
| 武器配置 | `WeaponChipConfig`（DefModExtension） | 武器芯片的特化数据（Verb/Tool/齐射/变化弹） |
| 触发体核心 | `CompTriggerBody` | 槽位管理、切换状态机、Verb重建 |
| 双武器合成 | `DualVerbCompositor` | 左右手Verb/Tool合成规则 |
| 投射物宿主 | `Bullet_BDP` + 模块管线 | 拖尾/爆炸/引导等投射物行为 |
| 模块工厂 | `BDPModuleFactory` | 根据DefModExtension自动创建投射物模块 |

### 1.4 架构约束（设计时必须遵守）

1. **芯片是无状态物品**：`IChipEffect`实现类不持有运行时数据，所有状态存在Pawn/Hediff/CompTriggerBody中
2. **武器数据不放ThingDef.Verbs/tools**：必须放在`WeaponChipConfig`（DefModExtension）中，否则`IsWeapon=true`导致引擎误判
3. **芯片Verb脱离VerbTracker**：芯片Verb由`CompTriggerBody.RebuildVerbs`手动创建，不进入`VerbTracker.AllVerbs`
4. **近战Verb必须用`Verb_BDPMelee`**：标准`Verb_MeleeAttackDamage`会用触发体ThingDef作为weapon，导致伤害来源显示错误
5. **远程Verb必须用`Verb_BDPShoot`**（或其子类）：确保与BDP的Gizmo和Trion消耗系统兼容

---

## 2. 决策流程总览

```
开始设计武器芯片
    │
    ├─ D0: 概念定义（名称、描述、设计意图）
    │
    ├─ D1: 近战 or 远程？
    │   │
    │   ├─ 近战 ─→ D2: Tool配置 → D3: 连击参数
    │   │
    │   └─ 远程 ─→ D4: VerbProperties配置
    │              → D5: 投射物设计（选已有 or 新建）
    │              → D6: 齐射开关
    │              → D7: 变化弹开关
    │
    ├─ D_通用（两条路径汇合）:
    │   ├─ D8: Trion经济参数
    │   ├─ D9: 时间参数
    │   └─ D10: 约束参数
    │
    ├─ 组装XML定义
    │
    └─ 测试验证
```

---

## 3. 决策点详解

### D0: 概念定义

在开始任何技术决策之前，先明确芯片的设计意图。

```
芯片名称:     _______________
一句话描述:   _______________
设计意图:     _______________（这个芯片要解决什么战斗场景？填补什么空缺？）
defName:      BDP_Chip_______（命名规范：BDP_Chip_ + 功能描述）
```

### D1: 近战 or 远程？

```
选择: [ ] 近战（通过tools注入Tool配置，使用Verb_BDPMelee）
      [ ] 远程（通过verbProperties注入VerbProperties，使用Verb_BDPShoot）
```

**判断依据：**
- 近战：伤害在接触距离施加，通过Tool.capacities（Cut/Blunt/Stab）定义攻击方式
- 远程：发射投射物，有射程/瞄准时间/弹道等概念

选择后进入对应分支 →

---

## 4. 近战分支

### D2: Tool配置

Tool定义了近战攻击的物理属性。每个武器芯片至少需要一个Tool。

```
Tool.label:        _______________（攻击部位名称，如"刃"、"锤头"、"爪"）
Tool.capacities:   [ ] Cut（斩击）  [ ] Blunt（钝击）  [ ] Stab（刺击）
Tool.power:        _____ （伤害值，参考：短剑=11, 长剑=13, 锤=14）
Tool.cooldownTime: _____ 秒（攻击冷却，参考：短剑=1.6, 长剑=2.0, 锤=2.6）
```

**约束：**
- `capacities`决定了DamageDef（Cut→Cut, Blunt→Blunt, Stab→Stab），影响护甲计算
- `power`和`cooldownTime`共同决定DPS：DPS = power / cooldownTime
- 多个Tool可以定义多种攻击方式（如"刃"Cut + "柄"Blunt），引擎随机选择

**DPS参考表（原版武器）：**

| 武器 | power | cooldown | DPS | capacity |
|------|-------|----------|-----|----------|
| 短剑 | 11 | 1.6s | 6.9 | Stab |
| 长剑 | 13 | 2.0s | 6.5 | Cut |
| 大锤 | 14 | 2.6s | 5.4 | Blunt |
| 枪刺 | 9 | 1.6s | 5.6 | Stab |

### D3: 连击参数

连击让一次攻击动作中连续打出多次伤害。

```
meleeBurstCount:    _____ 次（默认1=单次攻击，>1启用连击）
meleeBurstInterval: _____ ticks（连击间隔，默认12≈0.2秒。仅burstCount>1时有效）
```

**设计指南：**
- 连击数×单次伤害 = 总爆发伤害，但连击期间角色无法移动
- 高连击+低单伤 = 快速连打风格（如拳击）
- 低连击+高单伤 = 重击风格（如大锤）
- `meleeBurstInterval`越小连击越快，但太小（<3）视觉上难以区分

**连击与DualVerbCompositor的交互：**
- 双近战组合时，合成Verb的`burstShotCount` = 左burst + 右burst
- 合成Verb的`ticksBetweenBurstShots` = max(左interval, 右interval)
- 这意味着两把快刀组合后连击数翻倍，但间隔取较慢的那把

→ 近战分支完成，跳转到 [D8: Trion经济参数](#d8-trion经济参数)

---

## 5. 远程分支

### D4: VerbProperties配置

VerbProperties定义了远程攻击的射击属性。

```
verbClass:              Verb_BDPShoot（固定，不可更改）
defaultProjectile:      _______________（投射物defName，见D5）
warmupTime:             _____ 秒（瞄准时间，参考：突击步枪=1.5, 狙击=2.5, 手枪=0.3）
range:                  _____ 格（射程，参考：手枪=12, 步枪=26, 狙击=45）
burstShotCount:         _____ 发（连射数，参考：突击步枪=3, 微冲=6, 狙击=1）
ticksBetweenBurstShots: _____ ticks（连射间隔，参考：步枪=10, 微冲=5）
soundCast:              _______________（射击音效，如Shot_ChargeRifle, Shot_SniperRifle）
muzzleFlashScale:       _____ （枪口闪光大小，参考：步枪=9, 狙击=12）
hasStandardCommand:     true（固定，由架构在合成时覆盖为false）
```

**约束：**
- `verbClass`必须是`Verb_BDPShoot`或其子类，不可用原版`Verb_Shoot`
- `defaultProjectile`必须指向一个有效的ThingDef（见D5投射物设计）
- `warmupTime`是玩家体验的关键参数：太长=笨重感，太短=无反馈感

**远程DPS参考（原版武器）：**

| 武器 | 伤害 | burst | 间隔 | warmup | cooldown | 射程 |
|------|------|-------|------|--------|----------|------|
| 突击步枪 | 11 | 3 | 10t | 1.5s | 1.8s | 30 |
| 狙击步枪 | 23 | 1 | - | 2.5s | 2.4s | 45 |
| 微型冲锋枪 | 7 | 6 | 5t | 0.3s | 1.2s | 18 |
| 充能步枪 | 16 | 1 | - | 1.5s | 1.4s | 26 |

### D5: 投射物设计

每个远程芯片需要一个投射物ThingDef。可以复用已有的，也可以新建。

```
选择: [ ] 复用已有投射物（填写defName: _______________）
      [ ] 新建投射物（继续填写以下内容）
```

**新建投射物时的决策：**

```
defName:              BDP_Bullet_______（命名规范：BDP_Bullet_ + 描述）
thingClass:           Bullet_BDP（固定，不可更改）
projectile.damageDef: [ ] Bullet（普通弹道伤害）
                      [ ] Bomb（爆炸伤害，通常配合ExplosionModule）
                      [ ] 其他: _______________
projectile.damageAmountBase: _____ （基础伤害值）
projectile.speed:            _____ （飞行速度，参考：步枪弹=70, 狙击弹=90, 脉冲弹=55）
```

**投射物模块组合（通过DefModExtension挂载，由BDPModuleFactory自动创建）：**

```
[ ] 拖尾效果（BeamTrailConfig）
    segmentDuration: _____ ticks（拖尾段持续时间，参考：24-30）
    trailWidth:      _____ （拖尾宽度，参考：0.13-0.165）
    trailColor:      (R, G, B, A)  → _______________（如"(0.65, 1.0, 0.3, 1.0)"绿色）
    startOpacity:    _____ （起始不透明度，通常1.0）
    decayTime:       _____ 秒（衰减时间，参考：0.8）
    decaySharpness:  _____ （衰减锐度，参考：10.0）

[ ] 爆炸效果（BDPExplosionConfig）
    explosionRadius: _____ 格（爆炸半径，参考：1.2-2.5）
    注意：damageDef通常配合设为Bomb

[ ] 引导飞行（BDPGuidedConfig）
    （无额外参数，存在即启用。锚点数/散布由WeaponChipConfig控制）
    注意：需要芯片的supportsGuided=true才会生效
```

**模块优先级（已内置，无需配置）：**
- GuidedModule: 10（路径修改最先执行）
- ExplosionModule: 50（命中效果）
- TrailModule: 100（视觉效果最后执行）

### D6: 齐射开关

齐射模式让所有连射子弹在同一tick内一齐发射（右键Gizmo触发）。

```
supportsVolley:     [ ] 是  [ ] 否（默认否）
volleySpreadRadius: _____ 格（齐射散布半径，0=无偏移，0.3=轻微，0.6=明显。仅齐射时有效）
```

**设计指南：**
- 齐射适合"一波流"风格的芯片（如霰弹、火箭弹幕）
- `volleySpreadRadius`影响齐射的视觉散布，不影响命中判定
- 齐射与普通连射共享`burstShotCount`，区别在于发射时机（同时 vs 逐发）

### D7: 变化弹开关

变化弹让玩家在射击前设置多个锚点，子弹沿折线弹道飞行。

```
supportsGuided:  [ ] 是  [ ] 否（默认否）
maxAnchors:      _____ 个（最大锚点数，不含最终目标。参考：3）
anchorSpread:    _____ 格（锚点散布半径，参考：0.3。每颗子弹独立计算偏移）
```

**约束：**
- 变化弹需要投射物挂载`BDPGuidedConfig`模块（见D5）
- `maxAnchors`决定弹道复杂度：1=简单拐弯，3=复杂折线
- `anchorSpread`按递增系数偏移：第一段偏移最小，最后一段偏移最大

**D5与D7联动提示：**

变化弹由两层配置协作：芯片上的`supportsGuided`控制"让不让玩家画路线"，投射物上的`BDPGuidedConfig`控制"子弹会不会拐弯"。分离是为了支持投射物复用（多个芯片共享同一颗子弹，各自决定是否开启引导）。如果投射物是芯片专属的，这两个应联动配置——开一个就开另一个。

→ 远程分支完成，继续通用决策 ↓

---

## 6. 通用决策（近战/远程共享）

### D8: Trion经济参数

Trion是BDP的能量系统，芯片的Trion消耗决定了它的"使用成本"。

```
activationCost:   _____ （激活时一次性消耗，0=免费激活。参考：近战3-5, 远程3-10）
allocationCost:   _____ （Trion占用/锁定量，激活期间持续占用。参考：近战5-8, 远程10-15）
drainPerDay:      _____ （每天持续消耗，0=无持续消耗。参考：0-10）
trionCostPerShot: _____ （每发射击消耗，仅远程有效，0=无消耗。参考：0-2）
```

**设计指南：**
- `activationCost`是"开机费"——高值适合强力但不频繁切换的芯片
- `allocationCost`是"占用费"——高值限制同时激活的芯片数量（Trion总量有限）
- `drainPerDay`是"维持费"——适合持续型效果，迫使玩家在长期战斗中做取舍
- `trionCostPerShot`是"弹药费"——高值适合高伤害低射速武器，低值适合扫射型

**经济平衡公式（参考）：**
- 芯片强度 ≈ activationCost + allocationCost × 0.5 + drainPerDay × 0.1
- 远程额外：+ trionCostPerShot × burstShotCount × 预期射击次数

### D9: 时间参数

```
activationWarmup:  _____ ticks（激活预热，0=立即激活。参考：48≈0.8秒）
deactivationDelay: _____ ticks（关闭后摇，0=瞬间关闭。参考：12≈0.2秒）
```

**设计指南：**
- `activationWarmup`影响切换手感：0=即时切换（灵活），48=有预热（有仪式感）
- `deactivationDelay`影响切换节奏：后摇期间旧芯片仍isActive=true
- 切换总时长 = deactivationDelay（旧芯片后摇）+ max(switchCooldownTicks, activationWarmup)（新芯片前摇）
- 强力芯片建议设较长warmup，弱芯片建议0或很短

### D10: 约束参数

```
isDualHand:     [ ] 是  [ ] 否（默认否。是=激活后锁定双侧，另一侧不可独立操作）
minOutputPower: _____ （最低输出功率要求，0=无要求。>0时CanActivate检查Pawn的TrionOutputPower）
exclusionTags:  _______________（互斥标签列表，逗号分隔。如"heavy,explosive"。空=无互斥）
```

**设计指南：**
- `isDualHand=true`适合重型武器（大剑、火箭筒），激活后占用双手
- `minOutputPower`用于限制高级芯片只能被高等级角色使用
- `exclusionTags`用于防止不合理的芯片组合（如两把重武器同时激活）

---

## 7. XML模板

### 7.1 近战武器芯片模板

```xml
<ThingDef>
  <defName>BDP_Chip_{名称}</defName>
  <label>{中文标签}</label>
  <description>{描述文本}</description>
  <category>Item</category>
  <thingClass>ThingWithComps</thingClass>
  <graphicData>
    <texPath>Things/Trigger/{贴图路径}</texPath>
    <graphicClass>Graphic_Single</graphicClass>
  </graphicData>
  <statBases>
    <Mass>0.1</Mass>
    <MarketValue>{市场价值}</MarketValue>
  </statBases>
  <comps>
    <li Class="BDP.Trigger.CompProperties_TriggerChip">
      <chipEffectClass>BDP.Trigger.WeaponChipEffect</chipEffectClass>
      <!-- D8: Trion经济 -->
      <activationCost>{D8.activationCost}</activationCost>
      <allocationCost>{D8.allocationCost}</allocationCost>
      <drainPerDay>{D8.drainPerDay}</drainPerDay>
      <!-- D9: 时间 -->
      <activationWarmup>{D9.activationWarmup}</activationWarmup>
      <deactivationDelay>{D9.deactivationDelay}</deactivationDelay>
      <!-- D10: 约束（按需填写，不需要的删除） -->
      <isDualHand>{D10.isDualHand}</isDualHand>
      <minOutputPower>{D10.minOutputPower}</minOutputPower>
      <exclusionTags>
        <li>{D10.tag1}</li>
      </exclusionTags>
    </li>
  </comps>
  <modExtensions>
    <li Class="BDP.Trigger.WeaponChipConfig">
      <!-- D3: 连击 -->
      <meleeBurstCount>{D3.meleeBurstCount}</meleeBurstCount>
      <meleeBurstInterval>{D3.meleeBurstInterval}</meleeBurstInterval>
      <!-- D2: Tool配置 -->
      <tools>
        <li>
          <label>{D2.label}</label>
          <capacities><li>{D2.capacity}</li></capacities>
          <power>{D2.power}</power>
          <cooldownTime>{D2.cooldownTime}</cooldownTime>
        </li>
      </tools>
    </li>
  </modExtensions>
</ThingDef>
```

### 7.2 远程武器芯片模板

```xml
<ThingDef>
  <defName>BDP_Chip_{名称}</defName>
  <label>{中文标签}</label>
  <description>{描述文本}</description>
  <category>Item</category>
  <thingClass>ThingWithComps</thingClass>
  <graphicData>
    <texPath>Things/Trigger/{贴图路径}</texPath>
    <graphicClass>Graphic_Single</graphicClass>
  </graphicData>
  <statBases>
    <Mass>0.1</Mass>
    <MarketValue>{市场价值}</MarketValue>
  </statBases>
  <comps>
    <li Class="BDP.Trigger.CompProperties_TriggerChip">
      <chipEffectClass>BDP.Trigger.WeaponChipEffect</chipEffectClass>
      <!-- D8: Trion经济 -->
      <activationCost>{D8.activationCost}</activationCost>
      <allocationCost>{D8.allocationCost}</allocationCost>
      <drainPerDay>{D8.drainPerDay}</drainPerDay>
      <!-- D9: 时间 -->
      <activationWarmup>{D9.activationWarmup}</activationWarmup>
      <deactivationDelay>{D9.deactivationDelay}</deactivationDelay>
      <!-- D10: 约束（按需填写） -->
      <isDualHand>{D10.isDualHand}</isDualHand>
      <minOutputPower>{D10.minOutputPower}</minOutputPower>
      <exclusionTags>
        <li>{D10.tag1}</li>
      </exclusionTags>
    </li>
  </comps>
  <modExtensions>
    <li Class="BDP.Trigger.WeaponChipConfig">
      <!-- D6: 齐射 -->
      <supportsVolley>{D6.supportsVolley}</supportsVolley>
      <volleySpreadRadius>{D6.volleySpreadRadius}</volleySpreadRadius>
      <!-- D7: 变化弹 -->
      <supportsGuided>{D7.supportsGuided}</supportsGuided>
      <maxAnchors>{D7.maxAnchors}</maxAnchors>
      <anchorSpread>{D7.anchorSpread}</anchorSpread>
      <!-- D8补充: 每发消耗 -->
      <trionCostPerShot>{D8.trionCostPerShot}</trionCostPerShot>
      <!-- D4: VerbProperties -->
      <verbProperties>
        <li>
          <verbClass>BDP.Trigger.Verb_BDPShoot</verbClass>
          <hasStandardCommand>true</hasStandardCommand>
          <defaultProjectile>{D5.projectileDefName}</defaultProjectile>
          <warmupTime>{D4.warmupTime}</warmupTime>
          <range>{D4.range}</range>
          <burstShotCount>{D4.burstShotCount}</burstShotCount>
          <ticksBetweenBurstShots>{D4.ticksBetweenBurstShots}</ticksBetweenBurstShots>
          <soundCast>{D4.soundCast}</soundCast>
          <muzzleFlashScale>{D4.muzzleFlashScale}</muzzleFlashScale>
        </li>
      </verbProperties>
    </li>
  </modExtensions>
</ThingDef>
```

### 7.3 投射物ThingDef模板（仅新建投射物时需要）

```xml
<ThingDef ParentName="BaseBullet">
  <defName>BDP_Bullet_{名称}</defName>
  <label>{中文标签}</label>
  <thingClass>BDP.Trigger.Bullet_BDP</thingClass>
  <graphicData>
    <texPath>Things/Projectile/{贴图路径}</texPath>
    <graphicClass>Graphic_Single</graphicClass>
  </graphicData>
  <projectile>
    <damageDef>{D5.damageDef}</damageDef>
    <damageAmountBase>{D5.damageAmountBase}</damageAmountBase>
    <speed>{D5.speed}</speed>
  </projectile>
  <modExtensions>
    <!-- 按需添加模块（不需要的删除整个<li>） -->

    <!-- 拖尾效果 -->
    <li Class="BDP.Trigger.BeamTrailConfig">
      <segmentDuration>{segmentDuration}</segmentDuration>
      <trailWidth>{trailWidth}</trailWidth>
      <trailColor>{trailColor}</trailColor>
      <startOpacity>{startOpacity}</startOpacity>
      <decayTime>{decayTime}</decayTime>
      <decaySharpness>{decaySharpness}</decaySharpness>
    </li>

    <!-- 爆炸效果 -->
    <li Class="BDP.Trigger.BDPExplosionConfig">
      <explosionRadius>{explosionRadius}</explosionRadius>
    </li>

    <!-- 引导飞行（存在即启用，无额外参数） -->
    <li Class="BDP.Trigger.BDPGuidedConfig" />
  </modExtensions>
</ThingDef>
```

---

## 8. 测试Checklist

### 8.1 基础功能测试

```
[ ] 芯片可通过dev spawn生成
[ ] 芯片可装载到触发体槽位
[ ] 芯片激活时Trion正确扣除activationCost
[ ] 芯片激活时allocationCost正确占用
[ ] 芯片关闭时allocationCost正确释放
[ ] activationWarmup期间UI显示前摇进度条
[ ] deactivationDelay期间旧芯片仍isActive=true
```

### 8.2 近战专项测试

```
[ ] 激活后Pawn可执行近战攻击
[ ] 伤害类型正确（Cut/Blunt/Stab）
[ ] 伤害值与Tool.power一致
[ ] 连击数与meleeBurstCount一致
[ ] 连击间隔与meleeBurstInterval一致
[ ] 伤害来源显示为芯片名称（非触发体名称）
[ ] 关闭后近战攻击回退到触发体默认"柄"
```

### 8.3 远程专项测试

```
[ ] 激活后Pawn可执行远程攻击
[ ] 投射物正确生成（defName匹配）
[ ] 射程与range一致
[ ] 连射数与burstShotCount一致
[ ] trionCostPerShot每发正确扣除
[ ] 拖尾效果正确显示（如配置）
[ ] 爆炸效果正确触发（如配置）
[ ] 齐射模式正确触发（如supportsVolley=true）
[ ] 变化弹锚点设置正确（如supportsGuided=true）
```

### 8.4 双武器组合测试

```
[ ] 左右手各装一个近战芯片 → 双近战Verb正确合成
[ ] 左右手各装一个远程芯片 → 双远程Verb正确合成
[ ] 左近战+右远程 → 混合模式正确（远程isPrimary）
[ ] 左右手装相同芯片 → 独立Verb只保留1个（sameChip逻辑）
[ ] 双武器合成后burst参数正确（左burst+右burst）
```

### 8.5 边界情况测试

```
[ ] 存档/读档后芯片状态正确恢复
[ ] Trion不足时CanActivate返回false，UI灰显
[ ] minOutputPower不满足时无法激活
[ ] exclusionTags冲突时无法激活
[ ] isDualHand=true时另一侧被锁定
[ ] 切换过程中（WindingDown/WarmingUp）不可重复操作
```

---

## 9. 快速参考：字段速查表

### CompProperties_TriggerChip 字段

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| chipEffectClass | Type | — | 固定填`BDP.Trigger.WeaponChipEffect` |
| activationCost | float | 0 | 激活一次性Trion消耗 |
| allocationCost | float | 0 | Trion占用量 |
| drainPerDay | float | 0 | 每天持续Trion消耗 |
| activationWarmup | int | 0 | 激活预热(ticks) |
| deactivationDelay | int | 0 | 关闭后摇(ticks) |
| isDualHand | bool | false | 是否双手芯片 |
| minOutputPower | float | 0 | 最低输出功率要求 |
| exclusionTags | List\<string\> | null | 互斥标签列表 |

### WeaponChipConfig 字段（DefModExtension）

| 字段 | 类型 | 默认值 | 适用 | 说明 |
|------|------|--------|------|------|
| tools | List\<Tool\> | null | 近战 | 近战Tool配置 |
| meleeBurstCount | int | 1 | 近战 | 连击数 |
| meleeBurstInterval | int | 12 | 近战 | 连击间隔(ticks) |
| verbProperties | List\<VerbProperties\> | null | 远程 | 远程Verb配置 |
| trionCostPerShot | float | 0 | 远程 | 每发Trion消耗 |
| supportsVolley | bool | false | 远程 | 是否支持齐射 |
| volleySpreadRadius | float | 0 | 远程 | 齐射散布半径(格) |
| supportsGuided | bool | false | 远程 | 是否支持变化弹 |
| maxAnchors | int | 3 | 远程 | 最大锚点数 |
| anchorSpread | float | 0.3 | 远程 | 锚点散布半径(格) |

### 投射物模块配置（DefModExtension）

| 模块 | 配置类 | 优先级 | 说明 |
|------|--------|--------|------|
| 拖尾 | BeamTrailConfig | 100 | 视觉拖尾效果 |
| 爆炸 | BDPExplosionConfig | 50 | 命中时爆炸 |
| 引导 | BDPGuidedConfig | 10 | 折线弹道飞行 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-26 | 创建武器芯片设计说明书，覆盖近战+远程全链路决策流程 | Claude Opus 4.6 |
