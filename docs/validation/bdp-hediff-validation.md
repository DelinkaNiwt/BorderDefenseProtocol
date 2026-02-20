---
标题：Hediff类API校验报告
版本号: v1.0
更新日期: 2026-02-17
最后修改者: Claude Sonnet 4.5
标签：#技术校验 #API验证 #Hediff系统 #RimWorld
摘要: 验证RimWorld的Hediff类及其分级效果系统，确认设计文档中Hediff_TrionDepletion的实现方案是否符合游戏引擎API
---

# Hediff类API校验报告

## 1. HediffWithComps类基本信息

### 命名空间与继承关系
- **命名空间**: `Verse`
- **完整类名**: `Verse.HediffWithComps`
- **继承关系**: `HediffWithComps` → `Hediff` → `IExposable`
- **源码位置**: `C:/NiwtGames/Tools/Rimworld/RimSearcher/Source/Verse/HediffWithComps.cs`

### 类职责
`HediffWithComps`是带有组件系统的Hediff基类，允许通过`HediffComp`扩展功能。它继承自`Hediff`基类，添加了组件管理能力。

### 设计文档匹配度
✓ **完全匹配** - 设计文档中使用`HediffWithComps`作为基类是正确的。

---

## 2. Severity属性

### 属性签名
```csharp
public virtual float Severity
{
    get { return severityInt; }
    set
    {
        // 处理致命阈值
        if (IsLethal && value >= def.lethalSeverity)
        {
            value = def.lethalSeverity;
        }

        int curStageIndex = CurStageIndex;
        severityInt = Mathf.Clamp(value, def.minSeverity, def.maxSeverity);

        // 阶段变化时触发通知
        if (CurStageIndex != curStageIndex)
        {
            OnStageIndexChanged(CurStageIndex);
        }
    }
}
```

### 属性特性
- **类型**: `float`
- **范围**: 由`HediffDef.minSeverity`和`HediffDef.maxSeverity`限定
- **默认范围**: 通常为0.0~1.0，但可自定义
- **存储字段**: `protected float severityInt`

### CurStage选择机制
```csharp
public virtual HediffStage CurStage
{
    get
    {
        if (!def.stages.NullOrEmpty())
        {
            return def.stages[CurStageIndex];
        }
        return null;
    }
}

public virtual int CurStageIndex => def.StageAtSeverity(Severity);
```

**StageAtSeverity算法**（来自HediffDef）:
```csharp
public int StageAtSeverity(float severity)
{
    if (stages == null) return 0;

    // 从最高阶段向下查找第一个满足minSeverity的阶段
    for (int num = stages.Count - 1; num >= 0; num--)
    {
        if (severity >= stages[num].minSeverity)
        {
            return num;
        }
    }
    return 0;
}
```

### 设计文档匹配度
✓ **完全匹配** - 设计文档中的Severity映射机制（Percent=30% → Severity=0.0）完全符合引擎设计。

---

## 3. HediffDef.stages配置

### stages字段类型
```csharp
public List<HediffStage> stages;
```

### HediffStage.minSeverity字段
```csharp
public class HediffStage
{
    public float minSeverity;  // 默认值为0
    public string label;
    public List<PawnCapacityModifier> capMods = new List<PawnCapacityModifier>();
    public List<StatModifier> statOffsets;
    public List<StatModifier> statFactors;
    // ... 其他字段
}
```

### Stage选择逻辑
1. 引擎从**最后一个stage向前遍历**
2. 返回第一个`severity >= stage.minSeverity`的阶段
3. 如果所有阶段都不满足，返回第0个阶段（默认阶段）

### XML配置示例（来自设计文档）
```xml
<stages>
  <li>  <!-- 阶段0: Severity 0.0~0.49 -->
    <!-- minSeverity默认为0 -->
    <label>Trion不足</label>
    <capMods>
      <li><capacity>Moving</capacity><offset>-0.10</offset></li>
    </capMods>
  </li>
  <li>  <!-- 阶段1: Severity 0.5~0.89 -->
    <minSeverity>0.5</minSeverity>
    <label>Trion严重不足</label>
    <capMods>
      <li><capacity>Moving</capacity><offset>-0.20</offset></li>
      <li><capacity>Manipulation</capacity><offset>-0.10</offset></li>
    </capMods>
  </li>
  <li>  <!-- 阶段2: Severity 0.9~1.0 -->
    <minSeverity>0.9</minSeverity>
    <label>Trion枯竭</label>
    <capMods>
      <li><capacity>Consciousness</capacity><offset>-0.10</offset></li>
      <li><capacity>Moving</capacity><offset>-0.30</offset></li>
    </capMods>
  </li>
</stages>
```

### 设计文档匹配度
✓ **完全匹配** - 设计文档中的stages配置完全符合引擎规范。

---

## 4. capMods机制

### PawnCapacityModifier类型
```csharp
public class PawnCapacityModifier
{
    public PawnCapacityDef capacity;  // 能力类型
    public float offset;              // 偏移量（加减）
    public float setMax = 999f;       // 最大值限制
    public float postFactor = 1f;     // 后置乘数
    public StatDef statFactorMod;     // 统计修正器
}
```

### capacity枚举值（PawnCapacityDefOf）
```csharp
public static class PawnCapacityDefOf
{
    public static PawnCapacityDef Consciousness;    // 意识
    public static PawnCapacityDef Sight;            // 视力
    public static PawnCapacityDef Hearing;          // 听力
    public static PawnCapacityDef Moving;           // 移动
    public static PawnCapacityDef Manipulation;     // 操作
    public static PawnCapacityDef Talking;          // 说话
    public static PawnCapacityDef Breathing;        // 呼吸
    public static PawnCapacityDef BloodFiltration;  // 血液过滤
    public static PawnCapacityDef BloodPumping;     // 血液泵送
}
```

### offset字段说明
- **类型**: `float`
- **作用**: 直接加减能力值
- **范围**: 通常-1.0~+1.0（-100%~+100%）
- **计算**: 最终能力值 = 基础值 + offset

### 设计文档中的使用
```xml
<capMods>
  <li><capacity>Moving</capacity><offset>-0.10</offset></li>
  <li><capacity>Manipulation</capacity><offset>-0.10</offset></li>
  <li><capacity>Consciousness</capacity><offset>-0.10</offset></li>
</capMods>
```

### 设计文档匹配度
✓ **完全匹配** - 设计文档中使用的Moving、Manipulation、Consciousness都是有效的PawnCapacityDef，offset值范围合理。

---

## 5. statOffsets机制

### HediffStage.statOffsets类型
```csharp
public List<StatModifier> statOffsets;
```

### StatModifier结构
```csharp
public class StatModifier
{
    public StatDef stat;   // 统计类型
    public float value;    // 修正值
}
```

### 如何影响Stat系统
1. **statOffsets**: 加法修正，直接加减统计值
2. **statFactors**: 乘法修正，乘以统计值
3. 引擎在计算Pawn统计值时会遍历所有Hediff的当前阶段，累加statOffsets和statFactors

### XML配置示例
```xml
<statOffsets>
  <li><stat>RimWT_TrionRecoveryRate</stat><value>-0.10</value></li>
</statOffsets>
```

### 设计文档匹配度
✓ **完全匹配** - 设计文档中使用statOffsets影响自定义Stat（RimWT_TrionRecoveryRate）是正确的用法。

---

## 6. Hediff添加/移除API

### AddHediff签名
```csharp
// 方法1：通过HediffDef创建并添加
public Hediff AddHediff(
    HediffDef def,
    BodyPartRecord part = null,
    DamageInfo? dinfo = null,
    DamageWorker.DamageResult result = null
)

// 方法2：直接添加Hediff实例
public void AddHediff(
    Hediff hediff,
    BodyPartRecord part = null,
    DamageInfo? dinfo = null,
    DamageWorker.DamageResult result = null
)
```

**使用示例**:
```csharp
// 通过Def添加
Hediff hediff = pawn.health.AddHediff(HediffDefOf.RimWT_Hediff_TrionDepletion);

// 或先创建再添加
Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.RimWT_Hediff_TrionDepletion, pawn);
hediff.Severity = 0.5f;
pawn.health.AddHediff(hediff);
```

### GetFirstHediffOfDef签名
```csharp
public Hediff GetFirstHediffOfDef(
    HediffDef def,
    bool mustBeVisible = false
)
```

**使用示例**:
```csharp
Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(
    HediffDefOf.RimWT_Hediff_TrionDepletion
);
```

### RemoveHediff签名
```csharp
public void RemoveHediff(Hediff hediff)
```

**使用示例**:
```csharp
Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(
    HediffDefOf.RimWT_Hediff_TrionDepletion
);
if (hediff != null)
{
    pawn.health.RemoveHediff(hediff);
}
```

### 设计文档匹配度
✓ **完全匹配** - 设计文档中提到的API使用方式完全正确。

**注意事项**:
- `AddHediff`位于`Pawn_HealthTracker`（`pawn.health`）
- `GetFirstHediffOfDef`位于`HediffSet`（`pawn.health.hediffSet`）
- `RemoveHediff`位于`Pawn_HealthTracker`（`pawn.health`）

---

## 7. 社区模组使用示例

### 示例1: VFEP_HypothermicSlowdown（原版扩展框架）
**模组**: VanillaExpandedFramework_原版扩展框架

**特点**: 5阶段分级Hediff，展示了完整的severity到stage映射

```xml
<HediffDef>
  <defName>VFEP_HypothermicSlowdown</defName>
  <label>hypothermic slowdown</label>
  <hediffClass>HediffWithComps</hediffClass>
  <lethalSeverity>1</lethalSeverity>
  <stages>
    <li>
      <label>minor</label>
      <becomeVisible>false</becomeVisible>
    </li>
    <li>
      <label>minor</label>
      <minSeverity>0.04</minSeverity>
      <capMods>
        <li><capacity>Manipulation</capacity><offset>-0.08</offset></li>
      </capMods>
    </li>
    <li>
      <label>moderate</label>
      <minSeverity>0.2</minSeverity>
      <capMods>
        <li><capacity>Moving</capacity><offset>-0.1</offset></li>
        <li><capacity>Manipulation</capacity><offset>-0.2</offset></li>
      </capMods>
    </li>
    <li>
      <label>serious</label>
      <minSeverity>0.35</minSeverity>
      <painOffset>0.15</painOffset>
      <capMods>
        <li><capacity>Moving</capacity><offset>-0.3</offset></li>
        <li><capacity>Manipulation</capacity><offset>-0.5</offset></li>
      </capMods>
    </li>
    <li>
      <label>extreme</label>
      <minSeverity>0.62</minSeverity>
      <lifeThreatening>true</lifeThreatening>
      <painOffset>0.30</painOffset>
      <capMods>
        <li><capacity>Moving</capacity><offset>-0.5</offset></li>
        <li><capacity>Manipulation</capacity><setMax>0.1</setMax></li>
      </capMods>
    </li>
  </stages>
</HediffDef>
```

**关键点**:
- 使用minSeverity定义阶段边界
- 同时使用capMods和painOffset
- 最后阶段使用setMax限制能力上限

### 示例2: TW_Overdrive_M（机械族全面战争）
**模组**: MechanoidsTotalWarfare_机械族全面战争

**特点**: 4阶段buff型Hediff，展示了statOffsets和statFactors的组合使用

```xml
<HediffDef ParentName="NCL_TotalWarfareHediffBase">
  <defName>TW_Overdrive_M</defName>
  <label>机械超载</label>
  <hediffClass>Hediff_High</hediffClass>
  <maxSeverity>1</maxSeverity>
  <stages>
    <li>
      <minSeverity>0.01</minSeverity>
      <label>超载</label>
      <statFactors>
        <RangedCooldownFactor>0.2</RangedCooldownFactor>
        <IncomingDamageFactor>0.4</IncomingDamageFactor>
      </statFactors>
      <statOffsets>
        <MoveSpeed>+4</MoveSpeed>
        <MeleeHitChance>+10</MeleeHitChance>
      </statOffsets>
      <capMods>
        <li><capacity>Consciousness</capacity><offset>+3</offset></li>
      </capMods>
    </li>
    <li>
      <minSeverity>0.20</minSeverity>
      <label>能量过剩</label>
      <capMods>
        <li><capacity>Consciousness</capacity><offset>+2</offset></li>
      </capMods>
    </li>
    <li>
      <minSeverity>0.40</minSeverity>
      <label>逐渐稳定</label>
      <capMods>
        <li><capacity>Consciousness</capacity><offset>+1</offset></li>
      </capMods>
    </li>
    <li>
      <minSeverity>0.80</minSeverity>
      <label>稳定</label>
      <statOffsets>
        <MoveSpeed>+0.5</MoveSpeed>
      </statOffsets>
    </li>
  </stages>
</HediffDef>
```

**关键点**:
- 展示了statFactors（乘法修正）和statOffsets（加法修正）的组合
- 不同阶段可以有不同的效果组合
- 使用自定义hediffClass（Hediff_High）

### 示例3: CMC_HealingSE（天工铸造3）
**模组**: CeleTechArsenalMKIII_天工铸造3

**特点**: 简单的单阶段负面效果

```xml
<HediffDef>
  <hediffClass>HediffWithComps</hediffClass>
  <defName>CMC_HealingSE</defName>
  <label>healing side effect</label>
  <initialSeverity>1</initialSeverity>
  <maxSeverity>1</maxSeverity>
  <stages>
    <li>
      <capMods>
        <li><capacity>Manipulation</capacity><offset>-0.2</offset></li>
        <li><capacity>Breathing</capacity><offset>-0.35</offset></li>
      </capMods>
    </li>
  </stages>
</HediffDef>
```

**关键点**:
- 单阶段Hediff也需要stages列表
- 使用initialSeverity设置初始严重度

---

## 8. 总结

### 总体匹配度
✓ **100%匹配** - 设计文档中的Hediff_TrionDepletion实现方案完全符合RimWorld引擎API规范。

### 验证结论

#### ✓ 正确的设计点
1. **类继承**: 使用`HediffWithComps`作为基类是正确的
2. **Severity映射**: Severity 0.0~1.0映射到Trion 30%~0%的逻辑合理
3. **Stage配置**: 三阶段配置（0.0, 0.5, 0.9）符合引擎的StageAtSeverity算法
4. **capMods使用**: Moving、Manipulation、Consciousness都是有效的PawnCapacityDef
5. **statOffsets使用**: 影响自定义Stat（RimWT_TrionRecoveryRate）的方式正确
6. **API调用**: AddHediff、GetFirstHediffOfDef、RemoveHediff的使用方式正确

#### 发现的问题
**无** - 未发现任何与引擎API不符的设计。

### 建议修改
**无需修改** - 当前设计完全符合RimWorld引擎规范，可以直接实施。

### 额外建议

#### 1. Severity更新频率
设计文档中提到"Need_Trion在NeedInterval中添加/移除/调整"，建议：
- NeedInterval默认为150 ticks（2.5秒）
- Severity调整应该平滑，避免频繁跨越阶段边界导致UI闪烁

#### 2. Hediff可见性
建议在第一个阶段添加：
```xml
<li>
  <label>Trion不足</label>
  <becomeVisible>true</becomeVisible>  <!-- 确保可见 -->
  <capMods>...</capMods>
</li>
```

#### 3. 性能优化
- 使用`GetFirstHediffOfDef`前先检查`pawn.health.hediffSet.hediffs`是否为空
- 缓存HediffDef引用，避免重复查找

#### 4. 调试支持
建议添加：
```xml
<HediffDef>
  <defName>RimWT_Hediff_TrionDepletion</defName>
  <debugLabelExtra>Sev: {0:F2}</debugLabelExtra>  <!-- 显示Severity -->
  ...
</HediffDef>
```

---

## 历史修改记录
（注：摘要描述遵循奥卡姆剃刀原则）
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-17 | 完成Hediff类API校验，验证Hediff_TrionDepletion设计方案 | Claude Sonnet 4.5 |
