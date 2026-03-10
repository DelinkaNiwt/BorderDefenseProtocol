---
标题：武器身份代理系统（Weapon Identity Facade）设计方案
版本号: v1.0
更新日期: 2026-03-07
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][未完成][未锁定]
摘要: 解决触发器系统的"武器信息孤岛"问题，通过ThingComp stat管线桥接让触发体投射激活芯片的武器信息
---

## 1. 问题定义

### 1.1 核心痛点

BDP触发器系统的武器身份与RimWorld原版武器模型存在根本性的映射不匹配：

```
原版武器模型:  武器Thing ←→ 武器身份（1:1, 静态, ThingDef定义）
BDP武器模型:   触发体Thing → [芯片A + 芯片B] → 合成武器身份（1:N, 动态, 运行时组合）
```

触发体在原版系统眼中是一个"空壳武器"——它有 ThingDef.Verbs（占位用）和 CompEquippable，能被装备。但它的**真实战斗能力完全由运行时的芯片组合决定**。

芯片承载着完整的武器参数（VerbProperties、伤害、射程），但它**不在equipment槽位里**，原版系统不会向它查询任何武器信息。

### 1.2 影响清单

| 系统 | 当前表现 | 期望表现 |
|------|---------|---------|
| Info Card（i按钮） | 显示占位ThingDef空数据 | 显示当前激活芯片的武器参数 |
| 检视面板（左下角） | 无芯片状态 | 显示左/右手芯片名+核心参数 |
| Tooltip（悬停） | 无武器概要 | 简洁的武器配置概要 |
| 装备者Stat | 不反映芯片属性加成 | 反映激活芯片的stat修正 |
| TransformLabel | 始终"触发体" | "触发体 [星尘/陨石]" |
| 第三方DPS模组 | 读到占位Verb的零数据 | 可通过公开API获取真实数据 |
| 芯片Info Card | 仅显示物品基础信息 | 按类型展示完整武器/效果参数 |

### 1.3 设计原则

1. **纯新增，零修改风险** — 通过新建分部类实现ThingComp虚方法override，不触碰现有业务逻辑
2. **数据已就位** — VerbChipConfig等DefModExtension已包含所有需要的参数，只需"投射"
3. **与现有Gizmo层并行** — Gizmo负责交互操作，Display负责信息查询，关注点分离
4. **向后兼容** — 无激活芯片时返回null/空，不影响触发体的原有行为

## 2. 架构设计

### 2.1 新增文件结构

```
Source/BDP/
├── Trigger/
│   ├── Comps/
│   │   ├── CompTriggerBody.Display.cs      ← 【新建】触发体stat管线桥接
│   │   ├── CompTriggerBody.cs              （不变）
│   │   ├── CompTriggerBody.Fields.cs       （不变）
│   │   └── ...其他分部类...                （不变）
│   └── Data/
│       ├── ChipStatConfig.cs               ← 【新建】芯片stat修正DefModExtension
│       └── VerbChipConfig.cs               （不变）
│
1.6/Defs/
├── Core/
│   └── StatCategoryDefs_BDP.xml            ← 【新建】自定义StatCategory
└── Languages/
    └── ChineseSimplified/Keyed/
        └── BDP_Display.xml                 ← 【新建】展示翻译键
```

### 2.2 修改文件

```
Source/BDP/
├── Trigger/
│   └── Comps/
│       ├── CompProperties_TriggerChip.cs   ← 【修改】添加SpecialDisplayStats
│       └── CompTriggerBody.cs              ← 【修改】添加公开查询API
```

### 2.3 数据流

```
芯片ThingDef
  └─ VerbChipConfig (DefModExtension)
       ├─ primaryVerbProps.defaultProjectile.projectile.GetDamageAmount() → 伤害
       ├─ primaryVerbProps.range → 射程
       ├─ primaryVerbProps.warmupTime → 预热
       ├─ primaryVerbProps.burstShotCount → 连射
       ├─ trionCostPerShot → Trion消耗
       └─ ...其他参数...
  └─ ChipStatConfig (DefModExtension, 可选)
       ├─ equippedStatOffsets → 装备者stat加成
       └─ equippedStatFactors → 装备者stat倍率

CompTriggerBody.Display.cs
  ├─ TransformLabel() → 读取激活芯片名 → 动态标签
  ├─ CompInspectStringExtra() → 读取VerbChipConfig → 格式化检视信息
  ├─ CompTipStringExtra() → 读取激活芯片名 → 简洁提示
  ├─ SpecialDisplayStats() → 读取VerbChipConfig → StatDrawEntry列表
  ├─ GetStatOffset() → 聚合ChipStatConfig.equippedStatOffsets
  ├─ GetStatFactor() → 聚合ChipStatConfig.equippedStatFactors
  └─ GetStatsExplanation() → 列出各芯片的贡献明细
```

## 3. 实施阶段详述

### 阶段1：CompTriggerBody.Display.cs（P0 核心）

**目标**：让触发体的原版信息展示通道（Info Card、检视面板、Tooltip、标签）全部能展示激活芯片的武器信息。

**涉及虚方法**（全部在ThingComp中定义，CompTriggerBody当前无任何override）：

| 方法 | 调用者 | 功能 |
|------|--------|------|
| `TransformLabel(string)` | Thing.LabelNoCount | 修改物品显示名称 |
| `CompInspectStringExtra()` | Thing.GetInspectString | 左下角检视面板文本 |
| `CompTipStringExtra()` | Thing.GetTipString | 悬停Tooltip文本 |
| `SpecialDisplayStats()` | StatsReportUtility | Info Card的stat条目 |
| `GetStatOffset(StatDef)` | StatWorker.GetValueUnfinalized | stat加算修正 |
| `GetStatFactor(StatDef)` | StatWorker.GetValueUnfinalized | stat乘算修正 |
| `GetStatsExplanation(StatDef, StringBuilder, string)` | StatWorker | stat解释文本 |

**依赖的现有API**：
- `AllActiveSlots()` — CompTriggerBody.SlotManagement.cs
- `GetActiveSlot(SlotSide)` — CompTriggerBody.SlotManagement.cs
- `HasAnyActiveChip()` — CompTriggerBody.SlotManagement.cs
- `GetChipExtension<T>()` — CompTriggerBody.cs（通过ActivatingSlot读取当前芯片的DefModExtension）

**注意**：GetChipExtension<T>()依赖ActivatingSlot临时上下文，Display阶段需要直接从slot.loadedChip.def.GetModExtension<T>()读取，不走ActivatingSlot。

**实现要点**：

1. **TransformLabel** — 读取激活芯片标签拼接，处理单侧/双侧/同芯片情况
2. **CompInspectStringExtra** — 按侧格式化输出：`左手: 星尘 12dmg×5 射程40`
3. **CompTipStringExtra** — 最简化的 `左手: 星尘 | 右手: 陨石`
4. **SpecialDisplayStats** — 三组StatDrawEntry：
   - 配置概览（category: BDP_TriggerConfig）
   - 已装载芯片列表（category: BDP_ChipInfo）
   - 每个激活武器芯片的参数（category: Weapon_Ranged/Weapon_Melee，带侧别前缀）
5. **GetStatOffset/GetStatFactor** — 遍历AllActiveSlots，读取ChipStatConfig聚合

### 阶段2：芯片Info Card增强（P1）

**目标**：让芯片的Info Card按类型展示完整的武器/效果参数。

**实现位置**：在CompProperties_TriggerChip对应的运行时ThingComp中添加SpecialDisplayStats。

当前CompProperties_TriggerChip设置的compClass需要确认——如果是直接用ThingComp，需要创建一个具名的CompTriggerChip类。如果已有运行时Comp，直接在其中添加。

**按类型展示**：
- 所有芯片：activationCost、allocationCost、categories、warmup、delay等基础信息
- 武器芯片（有VerbChipConfig）：伤害、射程、预热、连射、Trion/发、齐射散布、引导、穿透力
- Hediff芯片（有HediffChipConfig）：hediffDef名称+描述
- Ability芯片（有AbilityChipConfig）：abilityDef名称+描述

### 阶段3：Stat管线桥接（P2）

**目标**：让芯片能通过XML声明式地影响装备者属性。

**新建ChipStatConfig DefModExtension**：
```csharp
public class ChipStatConfig : DefModExtension
{
    public List<StatModifier> equippedStatOffsets;  // 装备者stat加成
    public List<StatModifier> equippedStatFactors;  // 装备者stat倍率
}
```

**XML使用示例**：
```xml
<ThingDef>
  <defName>BDP_Chip_NanoShield</defName>
  <modExtensions>
    <li Class="BDP.Trigger.ChipStatConfig">
      <equippedStatOffsets>
        <ArmorRating_Sharp>0.15</ArmorRating_Sharp>
        <MoveSpeed>-0.3</MoveSpeed>
      </equippedStatOffsets>
    </li>
  </modExtensions>
</ThingDef>
```

### 阶段4：第三方兼容层（P3）

**目标**：为第三方模组提供查询接口。

**公开方法**（添加到CompTriggerBody.cs或Display.cs）：
```csharp
/// 获取当前激活芯片的合成VerbProperties（展示/查询用）
public List<VerbProperties> GetActiveWeaponVerbProperties()

/// 获取当前武器配置的估算DPS
public float GetActiveWeaponDPS()

/// 获取当前有效射程（取各侧最大值）
public float GetActiveWeaponRange()
```

**自定义StatCategory XML**：
```xml
<StatCategoryDef>
  <defName>BDP_ChipInfo</defName>
  <label>芯片信息</label>
  <displayOrder>2050</displayOrder>
</StatCategoryDef>

<StatCategoryDef>
  <defName>BDP_TriggerConfig</defName>
  <label>触发器配置</label>
  <displayOrder>2060</displayOrder>
</StatCategoryDef>
```

## 4. 风险评估

| 风险 | 等级 | 原因 | 缓解 |
|------|------|------|------|
| 破坏现有功能 | 极低 | 全部为ThingComp虚方法的新增override，不修改任何现有方法 | — |
| 性能影响 | 低 | SpecialDisplayStats仅在打开Info Card时调用；GetStatOffset/GetStatFactor在stat计算时调用，但逻辑简单（遍历少量激活槽位） | 可缓存 |
| CompFireMode集成 | 低 | 需要从芯片Thing读取CompFireMode来应用倍率，已有TryGetComp路径 | — |
| 翻译键缺失 | 无 | 使用Translate()前先定义翻译键 | 阶段4统一添加 |

## 5. 验证方法

### 每阶段编译验证
```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP"
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

### 游戏内验证清单
- [ ] 装备触发体 → TransformLabel显示"触发体"（无芯片时）
- [ ] 激活单侧芯片 → TransformLabel变为"触发体 [芯片名]"
- [ ] 激活双侧芯片 → TransformLabel变为"触发体 [A/B]"
- [ ] 检视面板 → 显示芯片名+伤害+射程
- [ ] 悬停tooltip → 显示简要配置
- [ ] 触发体Info Card → 显示完整武器参数（伤害、射程、预热、连射、Trion消耗）
- [ ] 芯片Info Card → 按类型显示武器/Hediff/Ability参数
- [ ] 切换芯片 → 所有信息动态更新
- [ ] 停用芯片 → 信息回退

---

## 历史修改记录
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-07 | 初始设计方案 | Claude Opus 4.6 |
