---
标题：Hediff分类与继承体系
版本号: v2.0
更新日期: 2026-02-11
最后修改者: Claude Sonnet 4.5
标签：[文档][用户未确认][已完成][未锁定]
摘要: 详细解析RimWorld中Hediff类的完整继承体系、8大功能分类、核心字段属性及XML配置模式
---

# Hediff分类与继承体系

**总览**：Hediff（Health Difference）是RimWorld健康系统的核心类，代表Pawn身上的任何健康状态变化，包括伤害、疾病、植入体、药物效果等。Hediff采用面向对象的继承体系，通过基类Hediff和中间类HediffWithComps派生出30+个专用子类，每个子类负责特定类型的健康效果。所有Hediff实例由HediffDef定义，支持通过XML配置实现模组化扩展。

## 一、Hediff基类核心结构

### 1.1 核心字段

| 字段名 | 类型 | 说明 |
|--------|------|------|
| def | HediffDef | Hediff的定义，包含所有配置数据 |
| pawn | Pawn | 拥有此Hediff的Pawn（运行时引用，不保存） |
| part | BodyPartRecord | 影响的身体部位（可为null表示全身效果） |
| severityInt | float | 严重程度的内部存储值 |
| ageTicks | int | Hediff存在的游戏tick数 |
| tickAdded | int | 添加时的游戏tick（用于计算年龄） |
| sourceLabel | string | 来源标签（如武器名称） |
| sourceDef | ThingDef | 来源物品定义 |
| sourceHediffDef | HediffDef | 来源Hediff定义 |
| causesNoPain | bool | 是否不产生疼痛 |
| visible | bool | 是否在UI中可见 |
| canBeThreateningToPart | bool | 是否可能威胁部位生存 |

### 1.2 核心属性

**Severity属性**（L239-269）：
```csharp
public virtual float Severity
{
    get => severityInt;
    set
    {
        // 1. 致死检查：达到lethalSeverity时锁定值
        if (IsLethal && value >= def.lethalSeverity)
        {
            value = def.lethalSeverity;
            flag = true;
        }

        // 2. 伤害整数变化检查（用于触发通知）
        bool flag2 = this is Hediff_Injury && value > severityInt
                     && Mathf.RoundToInt(value) != Mathf.RoundToInt(severityInt);

        // 3. 阶段变化检查
        int curStageIndex = CurStageIndex;
        severityInt = Mathf.Clamp(value, def.minSeverity, def.maxSeverity);

        // 4. 触发阶段变化回调
        if (CurStageIndex != curStageIndex)
            OnStageIndexChanged(CurStageIndex);

        // 5. 通知健康系统更新
        if ((CurStageIndex != curStageIndex || flag || flag2)
            && pawn.health.hediffSet.hediffs.Contains(this))
        {
            pawn.health.Notify_HediffChanged(this);
            pawn.needs.mood?.thoughts.situational.Notify_SituationalThoughtsDirty();
        }
    }
}
```

**其他关键属性**：
- `CurStage`：当前阶段（HediffStage），根据Severity从def.stages中选择
- `CurStageIndex`：当前阶段索引，通过`def.StageAtSeverity(Severity)`计算
- `Part`：身体部位属性，带setter验证逻辑
- `IsLethal`：是否可致死（def.lethalSeverity > 0）
- `BleedRate`：出血速率（虚方法，由子类重写）
- `PainOffset`：疼痛偏移量（从CurStage获取）
- `CapMods`：能力修正器列表（从CurStage获取）

## 二、完整继承树

### 2.1 继承层级结构

```
Hediff（基类）
├── 直接子类（无Comp系统）
│   ├── Hediff_SleepSuppression（睡眠抑制）
│   ├── Hediff_Scaria（狂暴病）
│   ├── Hediff_BloodRage（血怒）
│   ├── Hediff_FrenzyField（狂乱力场）
│   ├── Hediff_LightExposure（光照暴露）
│   ├── Hediff_DarknessExposure（黑暗暴露）
│   ├── Hediff_MeatHunger（肉食饥饿）
│   ├── Hediff_RapidRegeneration（快速再生）
│   ├── Hediff_Inhumanized（非人化）
│   ├── Hediff_SentienceCatalyst（感知催化剂）
│   ├── Hediff_CubeInterest（立方体兴趣）
│   ├── Hediff_CubeWithdrawal（立方体戒断）
│   ├── Hediff_BandNode（乐队节点）
│   └── Hediff_DeathrestEffect（死眠效果）
│
└── HediffWithComps（带组件系统的中间类）
    ├── 伤害与部位类
    │   ├── Hediff_Injury（伤害）
    │   ├── Hediff_MissingPart（缺失部位）
    │   └── Hediff_Implant（植入体）
    │       └── Hediff_AddedPart（添加部位）
    │
    ├── 疾病与状态类
    │   ├── Hediff_Hangover（宿醉）
    │   ├── Hediff_DuplicateSickness（复制病）
    │   └── Hediff_ChemicalDependency（化学依赖）
    │
    ├── 药物效果类
    │   ├── Hediff_High（药物高潮）
    │   └── Hediff_Addiction（成瘾）
    │
    ├── 精神与能力类
    │   ├── Hediff_PsychicTrance（心灵恍惚）
    │   ├── Hediff_Level（等级类，如Psylink）
    │   └── Hediff_Psylink（心灵连接）
    │
    ├── DLC特定类
    │   ├── Hediff_Deathrest（死眠 - Biotech）
    │   ├── Hediff_DeathRefusal（拒绝死亡 - Biotech）
    │   ├── Hediff_Mechlink（机械连接 - Biotech）
    │   ├── Hediff_VatLearning（培养槽学习 - Biotech）
    │   ├── Hediff_HemogenCraving（血原渴望 - Biotech）
    │   ├── Hediff_Shambler（蹒跚者 - Anomaly）
    │   ├── Hediff_MetalhorrorImplant（金属恐怖植入体 - Anomaly）
    │   └── Hediff_ShardHolder（碎片持有者 - Anomaly）
    │
    ├── 特殊效果类
    │   ├── Hediff_HeartAttack（心脏病发作）
    │   ├── Hediff_CoveredInFirefoam（覆盖泡沫）
    │   ├── Hediff_DisruptorFlash（干扰闪光）
    │   ├── Hediff_PainField（疼痛力场）
    │   ├── Hediff_PorcupineQuill（豪猪刺）
    │   └── Hediff_RotStinkExposure（腐臭暴露）
    │
    └── 继承中间类
        ├── HediffWithTarget（带目标的Hediff）
        └── HediffWithParents（带父级的Hediff）
```

### 2.2 HediffWithComps的重要性

HediffWithComps是最重要的中间类，为Hediff引入了**组件系统**（Comp System）：

```csharp
public class HediffWithComps : Hediff
{
    public List<HediffComp> comps;  // 组件列表

    // 组件初始化、Tick分发、生命周期管理等
}
```

**组件系统的优势**：
- **模块化**：通过HediffComp实现功能解耦（如免疫、消失、给予其他Hediff等）
- **可配置**：通过HediffCompProperties在XML中配置组件行为
- **可扩展**：模组可自定义HediffComp子类实现新功能

## 三、8大功能分类

### 3.1 分类表

| 分类 | 代表子类 | 核心特征 | 典型用途 |
|------|---------|---------|---------|
| **1. 伤害类** | Hediff_Injury | 有出血率、可治疗、影响部位健康 | 枪伤、刀伤、烧伤、冻伤 |
| **2. 植入体类** | Hediff_Implant<br>Hediff_AddedPart | 永久性、可移除、修改能力 | 仿生眼、心灵连接器、义肢 |
| **3. 疾病类** | HediffWithComps<br>+Immunizable | 免疫竞赛、可治疗、有阶段 | 流感、瘟疫、疟疾、肌肉寄生虫 |
| **4. 药物效果类** | Hediff_High<br>Hediff_Addiction | 化学需求关联、耐受性、成瘾 | 药物高潮、成瘾、戒断症状 |
| **5. 精神状态类** | Hediff_PsychicTrance<br>Hediff_Level | 影响意识、能力等级、特殊行为 | 心灵恍惚、Psylink等级 |
| **6. 缺失与损伤** | Hediff_MissingPart | 永久性、降低能力、可替换 | 缺失手臂、眼睛、器官 |
| **7. DLC特定** | Hediff_Deathrest<br>Hediff_Mechlink | DLC专属机制、复杂交互 | 死眠、机械连接、血族能力 |
| **8. 临时效果** | Hediff_Hangover<br>Hediff_CoveredInFirefoam | 短期存在、自动消失 | 宿醉、泡沫覆盖、力场效果 |

### 3.2 详细分类解析

#### 3.2.1 伤害类（Hediff_Injury）

**核心特征**：
- 重写`BleedRate`属性返回出血速率
- 支持治疗（tendable）和自然愈合
- Severity代表伤害程度（整数部分影响UI显示）
- 可导致部位损毁或失血死亡

**典型配置**：
```xml
<!-- 伤害通常由DamageWorker动态创建，而非预定义HediffDef -->
<HediffDef>
  <defName>Gunshot</defName>
  <hediffClass>Hediff_Injury</hediffClass>
  <tendable>true</tendable>
  <comps>
    <li Class="HediffCompProperties_TendDuration">
      <severityPerDayTended>-0.8</severityPerDayTended>
    </li>
    <li Class="HediffCompProperties_GetsPermanent">
      <permanentLabel>scar</permanentLabel>
    </li>
  </comps>
</HediffDef>
```

#### 3.2.2 植入体类（Hediff_Implant / Hediff_AddedPart）

**核心特征**：
- `countsAsAddedPartOrImplant = true`
- 通过`addedPartProps`定义部位效率（partEfficiency）
- 可通过手术移除并掉落物品（spawnThingOnRemoved）
- Hediff_AddedPart会在PostAdd时替换缺失部位

**XML抽象父模板**：
```xml
<HediffDef Name="ImplantHediffBase" Abstract="True">
  <hediffClass>Hediff_Implant</hediffClass>
  <defaultLabelColor>(0.6, 0.6, 1.0)</defaultLabelColor>
  <isBad>false</isBad>
  <priceImpact>true</priceImpact>
  <countsAsAddedPartOrImplant>true</countsAsAddedPartOrImplant>
  <allowMothballIfLowPriorityWorldPawn>true</allowMothballIfLowPriorityWorldPawn>
</HediffDef>
```

**典型实例**（仿生眼的ThingDef配置）：
```xml
<ThingDef ParentName="BodyPartBionicBase">
  <defName>BionicEye</defName>
  <label>bionic eye</label>
  <description>An installed bionic eye...</description>
  <addedPartProps>
    <solid>true</solid>
    <partEfficiency>1.25</partEfficiency>  <!-- 125%效率 -->
    <betterThanNatural>true</betterThanNatural>
  </addedPartProps>
</ThingDef>
```

#### 3.2.3 疾病类（使用HediffComp_Immunizable）

**核心特征**：
- 使用`HediffComp_Immunizable`实现免疫竞赛机制
- 有多个阶段（stages），随Severity变化
- `lethalSeverity = 1.0`时达到致死
- 免疫度达到1.0时开始负向Severity变化

**典型实例**（Flu流感）：
```xml
<HediffDef ParentName="InfectionBase">
  <defName>Flu</defName>
  <label>flu</label>
  <makesSickThought>true</makesSickThought>
  <lethalSeverity>1</lethalSeverity>
  <tendable>true</tendable>
  <comps>
    <li Class="HediffCompProperties_TendDuration">
      <baseTendDurationHours>12</baseTendDurationHours>
      <severityPerDayTended>-0.0773</severityPerDayTended>
    </li>
    <li Class="HediffCompProperties_Immunizable">
      <severityPerDayNotImmune>0.2488</severityPerDayNotImmune>
      <immunityPerDaySick>0.2388</immunityPerDaySick>
      <severityPerDayImmune>-0.4947</severityPerDayImmune>
      <immunityPerDayNotSick>-0.06</immunityPerDayNotSick>
    </li>
  </comps>
  <stages>
    <li>
      <label>minor</label>
      <capMods>
        <li><capacity>Consciousness</capacity><offset>-0.05</offset></li>
        <li><capacity>Breathing</capacity><offset>-0.1</offset></li>
      </capMods>
    </li>
    <li>
      <minSeverity>0.666</minSeverity>
      <label>major</label>
      <vomitMtbDays>1.5</vomitMtbDays>
      <capMods>
        <li><capacity>Consciousness</capacity><offset>-0.1</offset></li>
      </capMods>
    </li>
    <li>
      <minSeverity>0.833</minSeverity>
      <label>extreme</label>
      <lifeThreatening>true</lifeThreatening>
      <capMods>
        <li><capacity>Consciousness</capacity><offset>-0.15</offset></li>
      </capMods>
    </li>
  </stages>
</HediffDef>
```

#### 3.2.4 药物效果类（Hediff_High / Hediff_Addiction）

**核心特征**：
- 关联`chemicalNeed`（化学需求）
- Hediff_High代表药物高潮效果
- Hediff_Addiction代表成瘾状态
- 通过`HediffComp_SeverityPerDay`实现自动消退

#### 3.2.5 精神状态类

**核心特征**：
- 影响意识（Consciousness）能力
- 可能阻止社交互动（blocksSocialInteraction）
- Hediff_Level用于等级化能力（如Psylink）

#### 3.2.6 缺失与损伤（Hediff_MissingPart）

**核心特征**：
- 代表身体部位的完全缺失
- 降低相关能力至0或部分值
- 可通过Hediff_AddedPart替换

#### 3.2.7 DLC特定类

**核心特征**：
- 依赖特定DLC的游戏机制
- 通常有复杂的自定义逻辑
- 例如：Hediff_Deathrest管理死眠建筑交互

#### 3.2.8 临时效果类

**核心特征**：
- 使用`HediffComp_Disappears`自动消失
- 通常不可治疗、不致死
- 短期影响Pawn状态

## 四、HediffDef核心字段表

### 4.1 基础定义字段

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| hediffClass | Type | typeof(Hediff) | C#类类型 |
| label | string | - | 显示名称 |
| description | string | - | 描述文本 |
| defaultLabelColor | Color | - | 标签颜色 |
| isBad | bool | true | 是否为负面效果 |

### 4.2 Severity相关字段

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| initialSeverity | float | 0.5 | 初始严重程度 |
| lethalSeverity | float | -1 | 致死严重程度（-1表示不致死） |
| minSeverity | float | 0 | 最小严重程度 |
| maxSeverity | float | float.MaxValue | 最大严重程度 |
| stages | List\<HediffStage\> | null | 阶段列表 |

### 4.3 治疗与交互字段

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| tendable | bool | false | 是否可治疗 |
| chronic | bool | false | 是否为慢性病 |
| makesSickThought | bool | false | 是否产生生病想法 |
| makesAlert | bool | true | 是否产生警报 |
| preventsDeath | bool | false | 是否阻止死亡 |

### 4.4 组件与能力字段

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| comps | List\<HediffCompProperties\> | null | 组件属性列表 |
| hediffGivers | List\<HediffGiver\> | null | Hediff给予器列表 |
| abilities | List\<AbilityDef\> | null | 能力定义列表 |

### 4.5 植入体相关字段

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| countsAsAddedPartOrImplant | bool | false | 是否算作植入体 |
| spawnThingOnRemoved | ThingDef | null | 移除时生成的物品 |
| priceImpact | bool | false | 是否影响Pawn价格 |
| defaultInstallPart | BodyPartDef | null | 默认安装部位 |

### 4.6 特殊行为字段

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| blocksSocialInteraction | bool | false | 是否阻止社交 |
| blocksSleeping | bool | false | 是否阻止睡眠 |
| preventsCrawling | bool | false | 是否阻止爬行 |
| preventsPregnancy | bool | false | 是否阻止怀孕 |
| isInfection | bool | false | 是否为感染 |

## 五、开发者要点

> **关键认知**：
>
> 1. **继承选择**：
>    - 需要组件系统（Comp）→ 继承HediffWithComps
>    - 简单状态效果 → 直接继承Hediff
>    - 植入体/义肢 → 继承Hediff_Implant或Hediff_AddedPart
>
> 2. **Severity机制**：
>    - Severity的setter有复杂逻辑（阶段变化、通知系统）
>    - 直接修改severityInt会跳过这些逻辑，导致bug
>    - 使用`Severity`属性而非`severityInt`字段
>
> 3. **组件系统**：
>    - HediffComp是功能模块化的核心
>    - 60+个内置Comp类型（Immunizable、Disappears、GiveHediff等）
>    - 模组可通过自定义Comp扩展功能
>
> 4. **XML配置模式**：
>    - 使用Abstract="True"创建抽象父模板
>    - 通过ParentName继承父模板配置
>    - inspect工具会自动合并继承链显示最终配置
>
> 5. **部位关联**：
>    - part字段可为null（全身效果）
>    - 部位特定效果需要正确设置part
>    - Hediff_AddedPart会自动替换MissingPart
>
> 6. **生命周期**：
>    - PostAdd：添加时调用（初始化逻辑）
>    - Tick/PostTick：每tick调用（持续效果）
>    - PostRemoved：移除时调用（清理逻辑）
>    - 组件有对应的CompPostPostAdd、CompPostTick等
>
> 7. **阶段系统**：
>    - stages按minSeverity升序排列
>    - CurStage根据当前Severity自动选择
>    - 阶段变化会触发OnStageIndexChanged回调
>
> 8. **性能考虑**：
>    - 避免在Tick中进行重计算
>    - 使用HediffComp_SeverityPerDay而非手动Tick修改Severity
>    - 大量Hediff时考虑使用tickerType控制更新频率

## 六、源码引用

| 类/方法 | 文件路径 | 说明 |
|--------|---------|------|
| Hediff | C:/NiwtGames/Tools/Rimworld/RimSearcher/Source/Verse/Hediff.cs | 基类定义 |
| Hediff.Severity | Hediff.cs:239-269 | Severity属性setter逻辑 |
| HediffDef | C:/NiwtGames/Tools/Rimworld/RimSearcher/Source/Verse/HediffDef.cs | Def定义类 |
| HediffWithComps | C:/NiwtGames/Tools/Rimworld/RimSearcher/Source/Verse/HediffWithComps.cs | 组件系统中间类 |
| Hediff_Injury | C:/NiwtGames/Tools/Rimworld/RimSearcher/Source/Verse/Hediff_Injury.cs | 伤害类 |
| Hediff_Implant | C:/NiwtGames/Tools/Rimworld/RimSearcher/Source/Verse/Hediff_Implant.cs | 植入体类 |
| Hediff_AddedPart | C:/NiwtGames/Tools/Rimworld/RimSearcher/Source/Verse/Hediff_AddedPart.cs | 添加部位类 |
| ImplantHediffBase | C:/NiwtGames/Tools/Rimworld/RimSearcher/Data/Core/Defs/HediffDefs/BodyParts/Hediffs_BodyParts_Base.xml | 植入体XML模板 |
| Flu | C:/NiwtGames/Tools/Rimworld/RimSearcher/Data/Core/Defs/HediffDefs/Hediffs_Local_Infections.xml | 疾病配置实例 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v2.0 | 2026-02-11 | 基于RimSearcher工具从0开始重新生成，包含完整继承树、8大分类、核心字段表和典型XML实例 | Claude Sonnet 4.5 |
