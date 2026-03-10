# Hediff护盾系统设计文档

## 元信息
- **创建日期**: 2026-03-09
- **设计者**: Claude Opus 4.6
- **状态**: 已批准
- **标签**: [架构设计] [护盾系统] [Hediff] [Trion消耗]

---

## 概述

设计并实现一个基于Hediff的护盾系统，替代旧的CompProjectileInterceptor方案。新护盾系统以Hediff为入口，支持方向判定、成功率配置、Trion消耗和视觉特效。

## 需求

### 功能需求
1. **Hediff入口**: Hediff存在即激活，由触发器/芯片控制添加/移除
2. **射击不受限**: 内部可朝外开枪，护盾不干涉Verb系统
3. **方向判定**: 完整实现可配置的角度范围系统（XML配置）
4. **成功率**: 固定成功率（XML配置）
5. **Trion消耗**: 公式 `Trion扣除 = 伤害值 × 减免因子`
6. **视觉特效**: 暂时使用灵能折跃护盾特效（Skip_Entry），预留接口

### 非功能需求
- 代码简洁，遵循"最简原则"
- 与现有Trion系统、触发器系统无缝集成
- 易于扩展和维护

---

## 架构设计

### 核心类结构

```
Hediff_BDPShield (继承 HediffWithComps)
├── HediffComp_BDPShield (核心逻辑组件)
│   ├── PreApplyDamage (伤害拦截)
│   ├── CheckAngle (方向判定)
│   ├── CheckBlockChance (成功率判定)
│   ├── ConsumeTrion (Trion消耗)
│   └── PlayBlockEffect (特效播放)
└── HediffCompProperties_BDPShield (XML配置)
    ├── 方向/角度配置
    ├── 成功率配置
    ├── Trion消耗配置
    └── 特效配置
```

### 文件组织

```
Source/BDP/Combat/
├── Hediff_BDPShield.cs              # 护盾hediff主类
├── Comps/
│   ├── HediffComp_BDPShield.cs      # 核心逻辑组件
│   └── HediffCompProperties_BDPShield.cs  # 配置类
└── ShieldEffectPlayer.cs            # 特效播放器（预留接口）
```

---

## 核心组件设计

### HediffComp_BDPShield

护盾系统的核心逻辑组件，负责所有拦截和判定。

#### PreApplyDamage（伤害拦截入口）

```csharp
public void PreApplyDamage(ref DamageInfo dinfo, ref bool absorbed)
{
    if (absorbed) return;  // 已被其他系统吸收

    // 1. 检查护盾是否激活
    if (!IsShieldActive) return;

    // 2. 检查伤害类型是否可拦截
    if (!Props.CanAbsorb(dinfo.Def)) return;

    // 3. 方向判定
    if (!CheckAngle(dinfo.Angle)) return;

    // 4. 成功率判定
    if (!CheckBlockChance()) return;

    // 5. Trion消耗
    if (!ConsumeTrion(dinfo.Amount))
    {
        Break();  // Trion不足，护盾失效
        return;
    }

    // 6. 吸收伤害
    absorbed = true;
    dinfo.SetAmount(0f);

    // 7. 播放特效
    PlayBlockEffect(dinfo);
}
```

#### CheckAngle（方向判定）

```csharp
private bool CheckAngle(float damageAngle)
{
    if (!Props.enableAngleCheck) return true;

    // 获取pawn朝向
    float pawnRotation = Pawn.Rotation.AsAngle;

    // 计算相对角度
    float relativeAngle = Mathf.DeltaAngle(pawnRotation, damageAngle);

    // 判断是否在允许的角度范围内
    float minAngle = Props.blockAngleOffset - Props.blockAngleRange / 2f;
    float maxAngle = Props.blockAngleOffset + Props.blockAngleRange / 2f;

    return relativeAngle >= minAngle && relativeAngle <= maxAngle;
}
```

#### CheckBlockChance（成功率判定）

```csharp
private bool CheckBlockChance()
{
    if (Props.blockChance >= 1f) return true;
    return Rand.Value < Props.blockChance;
}
```

#### ConsumeTrion（Trion消耗）

```csharp
private bool ConsumeTrion(float damageAmount)
{
    var trionComp = Pawn.GetComp<CompTrion>();
    if (trionComp == null) return false;

    // 公式：Trion扣除 = 伤害值 × 减免因子
    float trionCost = damageAmount * Props.trionCostMultiplier;

    // 从可用量（Available）中消耗
    return trionComp.Consume(trionCost);
}
```

#### PlayBlockEffect（特效播放）

```csharp
private void PlayBlockEffect(DamageInfo dinfo)
{
    // 计算特效位置（攻击来源方向）
    Vector3 impactPos = Pawn.TrueCenter() +
        Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle).RotatedBy(180f) * 0.5f;

    // 调用特效播放器
    ShieldEffectPlayer.PlayBlockEffect(impactPos, Pawn.Map, Props.blockEffectDef);
}
```

---

## 配置系统设计

### HediffCompProperties_BDPShield

```csharp
public class HediffCompProperties_BDPShield : HediffCompProperties
{
    // ── 方向/角度配置 ──
    public bool enableAngleCheck = false;        // 是否启用方向判定
    public float blockAngleRange = 360f;         // 抵挡角度范围（度）
    public float blockAngleOffset = 0f;          // 角度偏移（0=正前方）

    // ── 成功率配置 ──
    public float blockChance = 1f;               // 抵挡成功率（0-1）

    // ── Trion消耗配置 ──
    public float trionCostMultiplier = 0.5f;     // 伤害转Trion倍率

    // ── 特效配置 ──
    public EffecterDef blockEffectDef;           // 抵挡特效Def（预留接口）

    // ── 伤害类型过滤 ──
    public List<DamageDef> absorbDamageTypes;    // 可拦截的伤害类型
    public List<DamageDef> ignoreDamageTypes;    // 忽略的伤害类型
}
```

### XML配置示例

```xml
<HediffDef>
  <defName>BDP_Shield</defName>
  <label>战斗护盾</label>
  <hediffClass>BDP.Combat.Hediff_BDPShield</hediffClass>
  <comps>
    <li Class="BDP.Combat.HediffCompProperties_BDPShield">
      <!-- 方向判定：仅前方180度 -->
      <enableAngleCheck>true</enableAngleCheck>
      <blockAngleRange>180</blockAngleRange>
      <blockAngleOffset>0</blockAngleOffset>

      <!-- 成功率：90% -->
      <blockChance>0.9</blockChance>

      <!-- Trion消耗：受到20点伤害，消耗10 Trion (20 × 0.5) -->
      <trionCostMultiplier>0.5</trionCostMultiplier>

      <!-- 特效 -->
      <blockEffectDef>Skip_Entry</blockEffectDef>
    </li>
  </comps>
</HediffDef>
```

---

## 特效系统设计

### ShieldEffectPlayer（特效播放器）

```csharp
/// <summary>
/// 护盾特效播放器（预留接口）
/// </summary>
public static class ShieldEffectPlayer
{
    /// <summary>
    /// 播放护盾抵挡特效
    /// </summary>
    public static void PlayBlockEffect(Vector3 position, Map map, EffecterDef effectDef = null)
    {
        // 使用配置的特效，如果未配置则使用默认特效
        EffecterDef def = effectDef ?? EffecterDefOf.Skip_Entry;

        // 生成特效
        Effecter effecter = def.Spawn();
        effecter.Trigger(new TargetInfo(position.ToIntVec3(), map), TargetInfo.Invalid);
        effecter.Cleanup();
    }
}
```

**设计要点**：
- 静态类，无需实例化
- 接受 `EffecterDef` 参数，支持XML配置
- 默认使用 `Skip_Entry`（灵能折跃护盾特效）
- 未来可扩展支持自定义特效、音效、粒子等

---

## 数据流设计

### 伤害拦截流程

```
攻击发生
    ↓
Pawn.PreApplyDamage (RimWorld原版钩子)
    ↓
遍历所有Hediff的Comps
    ↓
HediffComp_BDPShield.PreApplyDamage
    ↓
┌─────────────────────────────────┐
│ 1. 护盾激活检查                  │
│ 2. 伤害类型检查                  │
│ 3. 方向判定 (CheckAngle)         │
│ 4. 成功率判定 (CheckBlockChance) │
└─────────────────────────────────┘
    ↓
ConsumeTrion
    ↓
CompTrion.Consume(trionCost)
    ↓
┌─────────────────┐
│ Available足够？  │
└─────────────────┘
    ↓ 是              ↓ 否
absorbed = true    Break()
dinfo.Amount = 0   护盾失效
    ↓
PlayBlockEffect
```

### Trion消耗流程

```
HediffComp_BDPShield.ConsumeTrion(damageAmount)
    ↓
计算消耗量: trionCost = damageAmount × trionCostMultiplier
    ↓
获取CompTrion: pawn.GetComp<CompTrion>()
    ↓
CompTrion.Consume(trionCost)  // 从Available（可用量）扣除
    ↓
┌─────────────────────────┐
│ Available >= trionCost ? │
└─────────────────────────┘
    ↓ 是                    ↓ 否
cur -= trionCost        返回 false
返回 true                  ↓
                      护盾失效 (Break)
```

---

## 系统集成

### 与Trion系统集成
- 通过 `CompTrion.Consume()` 从可用量（Available = cur - allocated）扣除
- 与现有的 `HediffComp_TrionDamageCost` 机制一致
- 不干涉占用量（allocated）

### 与触发器系统集成
- 触发器/芯片通过 `pawn.health.AddHediff(BDP_DefOf.BDP_Shield)` 添加护盾
- 触发器/芯片失效时通过 `pawn.health.RemoveHediff()` 移除护盾
- 护盾hediff独立于战斗体系统，可单独使用

### 与特效系统集成
- 通过 `ShieldEffectPlayer` 统一管理特效播放
- 支持XML配置不同的 `EffecterDef`
- 预留接口，未来可扩展自定义特效

---

## 技术决策

### 为什么选择纯HediffComp方案？
1. **架构清晰**: 所有逻辑集中在一个组件中
2. **参考成熟**: VEF已验证此架构的可行性
3. **无需补丁**: 不需要Harmony补丁，减少兼容性风险
4. **易于维护**: 代码集中，调试方便

### 为什么不阻止射击？
- 用户明确需求："内部可朝外开枪"
- 护盾只负责防御，不干涉攻击行为
- 不实现 `AllowVerbCast` 接口，保持简洁

### 为什么简化Trion消耗公式？
- 原公式 `(damage / base) × multiplier` 过于复杂
- 新公式 `damage × multiplier` 更直观
- 减少配置参数，降低理解成本

---

## 实现清单

### 核心文件
- [ ] `Hediff_BDPShield.cs` - 护盾hediff主类
- [ ] `HediffComp_BDPShield.cs` - 核心逻辑组件
- [ ] `HediffCompProperties_BDPShield.cs` - 配置类
- [ ] `ShieldEffectPlayer.cs` - 特效播放器

### XML定义
- [ ] `HediffDefs_Shield.xml` - 护盾hediff定义
- [ ] 更新 `BDP_DefOf.cs` - 添加护盾def引用

### 测试
- [ ] 测试方向判定（前方、后方、侧面）
- [ ] 测试成功率（90%、50%、100%）
- [ ] 测试Trion消耗（不同伤害值）
- [ ] 测试特效播放（不同攻击角度）
- [ ] 测试与触发器系统集成

---

## 历史记录

### 2026-03-09
- **创建**: 初始设计文档
- **设计者**: Claude Opus 4.6
- **变更**:
  - 确定纯HediffComp架构
  - 简化Trion消耗公式
  - 完成核心组件设计
  - 完成配置系统设计
  - 完成特效系统设计
