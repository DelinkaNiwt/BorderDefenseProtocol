---
标题：Severity严重程度机制
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld Hediff Severity机制的完整解析，包含字段速查、生命周期、7种变化机制详解、免疫竞赛、Tick更新链和4个XML实例
---

# Severity严重程度机制

**总览**：Severity是Hediff的核心数值轴——它驱动阶段（Stage）切换、效果强度变化、自动移除（≤0）和致死判定（≥lethalSeverity），是理解疾病进程、药物消退、伤口愈合等一切动态Hediff的关键。

## HediffDef中的Severity字段速查

| 字段 | 类型 | 默认值 | 作用 |
|------|------|--------|------|
| `initialSeverity` | float | 0.5f | 添加Hediff时的初始严重度 |
| `lethalSeverity` | float | -1f | 致死阈值（-1表示不致死） |
| `minSeverity` | float | 0f | 严重度下限 |
| `maxSeverity` | float | float.MaxValue | 严重度上限 |

## Severity生命周期

```
添加Hediff → 设置initialSeverity
    ↓
每Tick各Comp累加severityAdjustment → Severity += adjustment
    ↓
Severity setter内部处理：
  ├─ Clamp到 [minSeverity, maxSeverity]
  ├─ 检测CurStageIndex是否变化 → 触发OnStageIndexChanged
  ├─ 通知 pawn.health.Notify_HediffChanged + 情绪更新
  └─ 判定：
       ├─ Severity ≤ 0 → ShouldRemove=true → 移除Hediff
       └─ Severity ≥ lethalSeverity → 致死（锁定值）
```

## Stage阶段映射机制

每个HediffDef可定义多个HediffStage，每个Stage有`minSeverity`阈值。阶段按minSeverity升序排列，`StageAtSeverity()`从后向前遍历，返回第一个`severity >= stage.minSeverity`的阶段。这意味着Severity越高，进入越靠后（通常越严重）的阶段。

## 7种Severity变化机制总表

| # | 机制 | 类型 | 触发方式 | 关键字段 | 典型实例 |
|---|------|------|---------|---------|---------|
| 1 | **SeverityPerDay** | HediffComp | 每200 ticks更新 | `severityPerDay`（固定）或`severityPerDayRange`（随机范围） | GoJuiceHigh: -0.75/天（约16h消退） |
| 2 | **Immunizable** | HediffComp | 每200 ticks更新 | `severityPerDayNotImmune`（恶化）/ `severityPerDayImmune`（康复） | Flu: 未免疫时恶化，免疫后康复 |
| 3 | **TendDuration** | HediffComp | 医疗治疗后持续 | `severityPerDayTended` × tendQuality | Flu: 治疗加速康复 |
| 4 | **GrowthMode** | HediffComp | 每5000 ticks检查模式切换 | Growing/Stable/Remission三模式 | Carcinoma: 癌症生长/稳定/缓解 |
| 5 | **SelfHeal** | HediffComp | 每`healIntervalTicksStanding` ticks | `healAmount`（默认1.0） | 伤口自愈 |
| 6 | **Disappears** | HediffComp | 倒计时归零 | `disappearsAfterTicks` | 不修改Severity，直接移除Hediff |
| 7 | **外部系统直接控制** | 非Comp | 游戏引擎代码驱动 | 无Comp参与 | Hypothermia/Heatstroke: 温度系统直接写入Severity |

## 各机制详解

### 1. HediffComp_SeverityPerDay — 固定每日变化（最常用）

继承`HediffComp_SeverityModifierBase`（更新间隔200 ticks）。字段`severityPerDay`为固定值，`severityPerDayRange`为随机范围（二选一）。支持`minAge`年龄门槛和`mechanitorFactor`机械师修正。最终值还会乘以当前Stage的`severityGainFactor`。正值=恶化，负值=消退。

### 2. HediffComp_Immunizable — 免疫系统交互（疾病核心）

疾病的核心机制——Severity与免疫值的竞赛：
- 未获得免疫时：以`severityPerDayNotImmune`速率恶化（正值）
- 完全免疫后：以`severityPerDayImmune`速率康复（负值）
- 免疫增长速率：`immunityPerDaySick`（生病时）/ `immunityPerDayNotSick`（未生病时）
- 支持`severityFactorsFromHediffs`：其他Hediff可作为因子影响恶化速率
- 免疫值受Pawn的`ImmunityGainSpeed`属性影响

### 3. HediffComp_TendDuration — 医疗治疗

医生治疗后的持续效果：`severityPerDayTended × tendQuality = 实际每日变化`。tendQuality = 基础质量 ± 0.25随机波动。治疗有效期为`baseTendDurationHours`小时，过期需重新治疗。`TendIsPermanent`模式下一次治疗永久有效。

### 4. HediffComp_GrowthMode — 慢性病三模式（癌症等）

三种模式循环切换：
- **Growing**：以`severityPerDayGrowing`速率恶化
- **Stable**：Severity不变
- **Remission**：以`severityPerDayRemission`速率缓解

模式切换：每5000 ticks检查，MTB（Mean Time Between）= 100天。癌症特殊：受Pawn的`CancerRate`属性影响。

### 5. HediffComp_SelfHeal — 伤口自愈

简单计数器机制：每`healIntervalTicksStanding`（默认50）ticks减少`healAmount`（默认1.0）Severity。不继承SeverityModifierBase，独立实现。

### 6. HediffComp_Disappears — 定时消失

倒计时`disappearsAfterTicks`，归零时通过`CompShouldRemove`移除Hediff。不修改Severity值本身。支持暂停（`DisappearsPausable`子类）。

### 7. 外部系统直接控制

温度系统直接修改Hypothermia（低温症）和Heatstroke（中暑）的Severity，无任何Comp参与，由游戏引擎代码驱动。

## 免疫系统（ImmunityHandler）与Severity的竞赛

```
ImmunityHandler 管理 ImmunityRecord 列表：
  每条记录 = hediffDef + immunity值(0~1)

免疫增长公式：
  immunity += immunityPerDaySick × ImmunityGainSpeed × (delta / 60000)

关键阈值：
  immunity ≥ 0.6 (60%) → 完全阻止该疾病再感染
  immunity = 1.0 (100%) → Immunizable切换到severityPerDayImmune（康复模式）

免疫衰减：
  不再生病时以 immunityPerDayNotSick 速率衰减
```

疾病的生死取决于这场竞赛：Severity先到lethalSeverity（死亡），还是免疫值先到1.0（康复）。

## Tick更新链

```
Pawn.Tick()
  → HealthTick()
    → HealthTickInterval(delta)
        ├─ ImmunityHandler.ImmunityHandlerTickInterval(delta)  // 免疫值更新
        └─ foreach hediff:
             ├─ severityAdjustment = 0f
             ├─ foreach comp → CompPostTick(ref severityAdjustment)  // 各Comp累加
             └─ Severity += severityAdjustment  // 一次性应用
```

## 4个XML实例解析

### 实例1：Flu（流感）— 免疫竞赛典型
```xml
<HediffDef ParentName="DiseaseBase">
  <defName>Flu</defName>
  <lethalSeverity>1</lethalSeverity>       <!-- Severity=1时致死 -->
  <stages>                                  <!-- 3个阶段 -->
    <li><label>minor</label><minSeverity>0</minSeverity></li>
    <li><label>major</label><minSeverity>0.333</minSeverity></li>
    <li><label>extreme</label><minSeverity>0.778</minSeverity></li>
  </stages>
  <comps>
    <li Class="HediffCompProperties_Immunizable">
      <severityPerDayNotImmune>0.72</severityPerDayNotImmune>   <!-- 未免疫：恶化 -->
      <severityPerDayImmune>-0.72</severityPerDayImmune>        <!-- 免疫后：康复 -->
      <immunityPerDaySick>0.82</immunityPerDaySick>             <!-- 生病时免疫增长 -->
    </li>
    <li Class="HediffCompProperties_TendDuration">
      <severityPerDayTended>-0.35</severityPerDayTended>        <!-- 治疗加速康复 -->
    </li>
  </comps>
</HediffDef>
```

### 实例2：Carcinoma（癌症）— 生长模式
```xml
<HediffDef ParentName="ChronicDiseaseBase">
  <defName>Carcinoma</defName>
  <initialSeverity>0.3</initialSeverity>    <!-- 初始30%严重度 -->
  <stages>                                   <!-- 6个阶段，severity=1时destroyPart -->
    <li><label>minor</label><minSeverity>0</minSeverity></li>
    ...
    <li><label>extreme</label><minSeverity>0.85</minSeverity>
      <destroyPart>true</destroyPart>        <!-- 最终阶段摧毁部位 -->
    </li>
  </stages>
  <comps>
    <li Class="HediffCompProperties_GrowthMode">
      <severityPerDayGrowing>0.0045</severityPerDayGrowing>     <!-- 生长：极慢恶化 -->
      <severityPerDayRemission>-0.0045</severityPerDayRemission> <!-- 缓解：极慢好转 -->
    </li>
    <li Class="HediffCompProperties_TendDuration">...</li>
  </comps>
</HediffDef>
```

### 实例3：GoJuiceHigh（兴奋剂药效）— 自然消退
```xml
<HediffDef>
  <defName>GoJuiceHigh</defName>
  <hediffClass>Hediff_High</hediffClass>
  <isBad>false</isBad>                      <!-- 正面效果 -->
  <initialSeverity>0.5</initialSeverity>
  <comps>
    <li Class="HediffCompProperties_SeverityPerDay">
      <severityPerDay>-0.75</severityPerDay> <!-- 每天-0.75，约16小时消退 -->
    </li>
  </comps>
</HediffDef>
<!-- Severity ≤ 0 → ShouldRemove → 自动移除 -->
```

### 实例4：Hypothermia（低温症）— 外部系统驱动
```xml
<HediffDef>
  <defName>Hypothermia</defName>
  <lethalSeverity>1</lethalSeverity>         <!-- Severity=1时致死 -->
  <stages>                                    <!-- 5个阶段 -->
    <li><label>minor</label><minSeverity>0</minSeverity></li>
    <li><label>serious</label><minSeverity>0.3</minSeverity></li>
    <li><label>severe</label><minSeverity>0.5</minSeverity></li>
    <li><label>extreme</label><minSeverity>0.7</minSeverity></li>
    <li><label>life-threatening</label><minSeverity>0.85</minSeverity></li>
  </stages>
  <!-- 无任何Comp！Severity完全由温度系统C#代码直接控制 -->
</HediffDef>
```

> **开发者要点**：
> 1. **Severity是Hediff的时间轴**：几乎所有动态Hediff的行为都围绕Severity展开——阶段切换、效果强度、移除/致死判定
> 2. **Comp组合决定变化模式**：同一个Hediff可以叠加多个Severity变化Comp（如Flu同时有Immunizable + TendDuration），它们在每个Tick中各自贡献severityAdjustment，最终一次性累加
> 3. **免疫竞赛是疾病核心**：疾病的生死取决于Severity vs 免疫值的竞赛速度，治疗（TendDuration）通过额外的负向severityAdjustment帮助玩家赢得竞赛
> 4. **Stage设计要点**：阶段的minSeverity阈值定义了"什么时候切换效果"，从后向前匹配意味着高Severity优先匹配高阶段
> 5. **模组开发建议**：自定义Hediff的Severity变化优先使用现有Comp（SeverityPerDay最常用），只有现有Comp无法满足需求时才考虑自定义Comp或直接在C#中操作Severity

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 从3.1笔记第1章第3点抽取为独立知识文档 | Claude Opus 4.6 |
