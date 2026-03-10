# 范围指示器系统设计文档

**项目**: BorderDefenseProtocol
**日期**: 2026-03-10
**作者**: Claude Sonnet 4.6
**状态**: 设计阶段

---

## 一、概述

### 1.1 目标

为 BDP 模组实现范围指示器系统，在瞄准阶段实时显示武器/能力的影响范围，帮助玩家做出更好的战术决策。

### 1.2 核心需求

- **触发时机**: 瞄准阶段显示（玩家选择目标时）
- **适用类型**: 投射物、ability、特殊效果等所有类型
- **配置方式**: 通过 ModExtension 配置，支持多层级优先级
- **范围来源**: 从实际效果配置读取（如爆炸半径）
- **视觉样式**: 第一版实现圆形，预留接口支持未来扩展（扇形、弧形等）
- **多芯片支持**: 单侧攻击显示单个范围，双侧攻击显示两个范围（相同颜色，透明度叠加）

### 1.3 设计原则

- **YAGNI**: 第一版只实现圆形指示器，为未来扩展预留接口
- **向后兼容**: 现有芯片不配置时仍正常工作
- **最小侵入**: 不修改原版系统，不 Patch 原版方法
- **性能优先**: 配置缓存，提前退出，避免重复计算

---

## 二、架构设计

### 2.1 整体架构

```
配置层（XML Defs）
    ↓ 读取配置
Verb 层（绘制逻辑）
    ↓ 调用 RimWorld API
渲染层（GenDraw）
```

### 2.2 核心组件

#### 2.2.1 AreaIndicatorConfig（ModExtension）

配置范围指示器的外观和行为。

**字段：**
- `indicatorType`: 指示器类型（Circle/Sector/Arc/Rectangle）
- `radiusSource`: 半径来源（Explosion/Custom）
- `customRadius`: 自定义半径（仅当 radiusSource=Custom 时使用）
- `color`: 指示器颜色（RGBA）
- `fillStyle`: 填充样式（Outline/Filled）

**配置位置优先级（从高到低）：**
1. **投射物 Def** - 适用于爆炸弹、范围伤害投射物
2. **芯片 Def** - 适用于 ability、特殊效果芯片
3. **Verb Def** - 适用于通用 Verb 配置（备用）

#### 2.2.2 IAreaIndicator（接口）

为未来扩展预留的抽象接口。

**方法：**
```csharp
void Draw(IntVec3 center, Map map, AreaIndicatorConfig config);
```

**实现类：**
- `CircleAreaIndicator`: 圆形指示器（第一版）
- `SectorAreaIndicator`: 扇形指示器（未来）
- `ArcAreaIndicator`: 弧形指示器（未来）
- `RectangleAreaIndicator`: 矩形指示器（未来）

#### 2.2.3 Verb 层集成

**Verb_BDPRangedBase（基类）：**
- `DrawAreaIndicators(LocalTargetInfo target)`: 虚方法，默认不绘制
- `GetAreaIndicatorConfig()`: 按优先级读取配置
- 在 `StartAnchorTargeting()` 的 `highlightAction` 中调用

**Verb_BDPSingle（单侧）：**
- 重写 `DrawAreaIndicators()`
- 获取当前侧的投射物配置
- 绘制单个范围圈

**Verb_BDPDual（双侧）：**
- 重写 `DrawAreaIndicators()`
- 分别获取左右两侧的投射物配置
- 绘制两个范围圈（相同颜色，透明度叠加）

---

## 三、数据流

### 3.1 配置读取流程

```
用户点击攻击 Gizmo
    ↓
Verb.OrderForceTarget() / StartAnchorTargeting()
    ↓
Find.Targeter.BeginTargeting(highlightAction)
    ↓
每帧调用 highlightAction(target)
    ↓
Verb.DrawAreaIndicators(target)
    ↓
GetAreaIndicatorConfig() - 按优先级读取配置
    ├─ 1. 投射物 Def.GetModExtension<AreaIndicatorConfig>()
    ├─ 2. VerbChipConfig.areaIndicator
    └─ 3. VerbProperties.GetModExtension<AreaIndicatorConfig>()
    ↓
GetRadius(config) - 根据 radiusSource 获取半径
    ├─ RadiusSource.Explosion → 从 BDPExplosionConfig 读取
    └─ RadiusSource.Custom → 使用 config.customRadius
    ↓
CircleAreaIndicator.Draw(center, map, config)
    ↓
GenDraw.DrawRadiusRing(center, radius, color)
```

### 3.2 半径获取逻辑

```csharp
private float GetRadius(ThingDef projectileDef, AreaIndicatorConfig config)
{
    if (config.radiusSource == RadiusSource.Explosion)
    {
        var explosionConfig = projectileDef?.GetModExtension<BDPExplosionConfig>();
        if (explosionConfig != null)
            return explosionConfig.explosionRadius;
    }
    return config.customRadius;
}
```

### 3.3 单侧 vs 双侧数据流

**单侧（Verb_BDPSingle）：**
```
DrawAreaIndicators(target)
    ↓
GetTriggerComp() → GetActiveSlot(chipSide)
    ↓
获取该侧的投射物 Def
    ↓
读取配置并绘制一个范围圈
```

**双侧（Verb_BDPDual）：**
```
DrawAreaIndicators(target)
    ↓
GetTriggerComp() → GetActiveSlot(LeftHand)
    ↓
获取左侧投射物 Def → 绘制左侧范围圈
    ↓
GetActiveSlot(RightHand)
    ↓
获取右侧投射物 Def → 绘制右侧范围圈
    ↓
两个圈使用相同颜色，重叠部分透明度叠加
```

---

## 四、组件设计

### 4.1 AreaIndicatorConfig

```csharp
namespace BDP.Trigger
{
    /// <summary>
    /// 范围指示器配置（ModExtension）。
    /// 可配置在投射物 Def、芯片 Def、Verb Def 上。
    /// </summary>
    public class AreaIndicatorConfig : DefModExtension
    {
        /// <summary>指示器类型（第一版只支持 Circle）。</summary>
        public AreaIndicatorType indicatorType = AreaIndicatorType.Circle;

        /// <summary>半径来源。</summary>
        public RadiusSource radiusSource = RadiusSource.Explosion;

        /// <summary>自定义半径（仅当 radiusSource=Custom 时使用）。</summary>
        public float customRadius = 3.0f;

        /// <summary>指示器颜色（RGBA，默认半透明红色）。</summary>
        public Color color = new Color(1.0f, 0.3f, 0.3f, 0.35f);

        /// <summary>填充样式（第一版只支持 Filled）。</summary>
        public FillStyle fillStyle = FillStyle.Filled;
    }

    public enum AreaIndicatorType { Circle, Sector, Arc, Rectangle }
    public enum RadiusSource { Explosion, Custom }
    public enum FillStyle { Outline, Filled }
}
```

### 4.2 IAreaIndicator 接口

```csharp
namespace BDP.Trigger
{
    /// <summary>
    /// 范围指示器接口（为未来扩展预留）。
    /// </summary>
    public interface IAreaIndicator
    {
        void Draw(IntVec3 center, Map map, AreaIndicatorConfig config);
    }
}
```

### 4.3 CircleAreaIndicator

```csharp
namespace BDP.Trigger
{
    /// <summary>
    /// 圆形范围指示器实现。
    /// </summary>
    public class CircleAreaIndicator : IAreaIndicator
    {
        public void Draw(IntVec3 center, Map map, AreaIndicatorConfig config)
        {
            float radius = GetRadius(config);
            if (radius <= 0f) return;

            GenDraw.DrawRadiusRing(center, radius, config.color);
        }

        private float GetRadius(AreaIndicatorConfig config)
        {
            // 实现见"半径获取逻辑"
        }
    }
}
```

### 4.4 Verb_BDPRangedBase 集成

```csharp
/// <summary>
/// 绘制范围指示器（基类默认不绘制，由子类重写）。
/// </summary>
protected virtual void DrawAreaIndicators(LocalTargetInfo target)
{
    // 基类默认不绘制
}

/// <summary>
/// 按优先级读取范围指示器配置。
/// </summary>
protected AreaIndicatorConfig GetAreaIndicatorConfig()
{
    // 1. 优先从投射物读取
    var projectileDef = GetProjectileDef();
    var config = projectileDef?.GetModExtension<AreaIndicatorConfig>();
    if (config != null) return config;

    // 2. 其次从芯片读取
    var chipConfig = GetChipConfig();
    if (chipConfig?.areaIndicator != null)
        return chipConfig.areaIndicator;

    // 3. 最后从 Verb 读取
    return verbProps?.GetModExtension<AreaIndicatorConfig>();
}

/// <summary>
/// 修改 StartAnchorTargeting()，在 highlightAction 中添加范围指示器。
/// </summary>
public virtual void StartAnchorTargeting()
{
    // ... 现有代码 ...

    Action<LocalTargetInfo> highlightAction = target =>
    {
        GenDraw.DrawRadiusRing(caster.Position, weaponRange);
        DrawGuidedOverlay(caster, anchors, target, caster.Map);
        GenDraw.DrawTargetHighlight(target);

        // 新增：绘制范围指示器
        DrawAreaIndicators(target);
    };

    // ... 现有代码 ...
}
```

### 4.5 Verb_BDPSingle 实现

```csharp
protected override void DrawAreaIndicators(LocalTargetInfo target)
{
    var triggerComp = GetTriggerComp();
    if (triggerComp == null || !chipSide.HasValue) return;

    var slot = triggerComp.GetActiveSlot(chipSide.Value);
    var chipConfig = slot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
    var projectileDef = chipConfig?.GetPrimaryProjectileDef();

    if (projectileDef != null)
    {
        var indicatorConfig = projectileDef.GetModExtension<AreaIndicatorConfig>();
        if (indicatorConfig != null)
        {
            var indicator = new CircleAreaIndicator();
            indicator.Draw(target.Cell, caster.Map, indicatorConfig);
        }
    }
}
```

### 4.6 Verb_BDPDual 实现

```csharp
protected override void DrawAreaIndicators(LocalTargetInfo target)
{
    var triggerComp = GetTriggerComp();
    if (triggerComp == null) return;

    // 左侧范围
    var leftSlot = triggerComp.GetActiveSlot(SlotSide.LeftHand);
    var leftProjectileDef = leftSlot?.loadedChip?.def
        ?.GetModExtension<VerbChipConfig>()?.GetPrimaryProjectileDef();
    DrawSideIndicator(leftProjectileDef, target);

    // 右侧范围
    var rightSlot = triggerComp.GetActiveSlot(SlotSide.RightHand);
    var rightProjectileDef = rightSlot?.loadedChip?.def
        ?.GetModExtension<VerbChipConfig>()?.GetPrimaryProjectileDef();
    DrawSideIndicator(rightProjectileDef, target);
}

private void DrawSideIndicator(ThingDef projectileDef, LocalTargetInfo target)
{
    if (projectileDef == null) return;

    var indicatorConfig = projectileDef.GetModExtension<AreaIndicatorConfig>();
    if (indicatorConfig != null)
    {
        var indicator = new CircleAreaIndicator();
        indicator.Draw(target.Cell, caster.Map, indicatorConfig);
    }
}
```

---

## 五、错误处理和边界情况

### 5.1 配置缺失处理

**设计原则：**
- 配置缺失时静默跳过，不报错
- 不影响正常的瞄准和攻击流程
- 向后兼容：现有芯片不配置时仍正常工作

```csharp
protected override void DrawAreaIndicators(LocalTargetInfo target)
{
    var config = GetAreaIndicatorConfig();
    if (config == null) return; // 静默跳过
}
```

### 5.2 半径异常处理

**场景 1：爆炸配置不存在**
```csharp
if (config.radiusSource == RadiusSource.Explosion)
{
    var explosionConfig = projectileDef?.GetModExtension<BDPExplosionConfig>();
    if (explosionConfig == null)
    {
        // 回退到自定义半径
        return config.customRadius;
    }
    return explosionConfig.explosionRadius;
}
```

**场景 2：半径为负数或零**
```csharp
float radius = GetRadius(config);
if (radius <= 0f) return; // 提前退出
```

### 5.3 双侧攻击边界情况

- 只有一侧有配置 → 只绘制该侧
- 两侧都有配置 → 绘制两个圈（透明度叠加）
- 两侧都没有配置 → 不绘制
- 两侧范围完全相同 → 看起来像一个圈（可接受）
- 槽位为空 → 跳过该侧

### 5.4 性能优化

**优化 1：配置缓存**
```csharp
private AreaIndicatorConfig cachedConfig;
private int cachedConfigTick = -1;

protected AreaIndicatorConfig GetAreaIndicatorConfig()
{
    int currentTick = Find.TickManager.TicksGame;
    if (cachedConfigTick == currentTick && cachedConfig != null)
        return cachedConfig;

    cachedConfig = /* 按优先级读取 */;
    cachedConfigTick = currentTick;
    return cachedConfig;
}
```

**优化 2：提前退出**
- `if (config == null) return;`
- `if (radius <= 0f) return;`

**优化 3：避免重复计算**
- 半径只在配置读取时计算一次
- 颜色直接从配置读取

### 5.5 兼容性处理

- 只在 BDP Verb 的 `highlightAction` 中绘制
- 不修改原版 Targeter 系统
- 不 Patch 任何原版方法
- 不会与其他模组冲突

---

## 六、配置示例

### 6.1 投射物配置（爆炸弹）

```xml
<ThingDef ParentName="BaseBullet">
  <defName>BDP_Bullet_Explosive</defName>
  <label>炸裂弹</label>
  <thingClass>BDP.Projectiles.Bullet_BDP</thingClass>
  <projectile>
    <damageDef>Bomb</damageDef>
    <damageAmountBase>5</damageAmountBase>
    <speed>84</speed>
  </projectile>
  <modExtensions>
    <!-- 爆炸效果配置 -->
    <li Class="BDP.Projectiles.Config.BDPExplosionConfig">
      <explosionRadius>3.0</explosionRadius>
    </li>
    <!-- 范围指示器配置 -->
    <li Class="BDP.Trigger.AreaIndicatorConfig">
      <radiusSource>Explosion</radiusSource>
      <color>(1.0, 0.3, 0.3, 0.35)</color>
    </li>
  </modExtensions>
</ThingDef>
```

### 6.2 芯片配置（跳跃能力）

```xml
<ThingDef ParentName="BDP_ChipBase">
  <defName>BDP_ChipGrasshopper</defName>
  <label>蚱蜢芯片</label>
  <modExtensions>
    <li Class="BDP.Trigger.VerbChipConfig">
      <!-- 跳跃能力配置 -->
      <ability>
        <abilityClass>Verb_CastAbilityGrasshopper</abilityClass>
        <range>15.0</range>
      </ability>
      <!-- 范围指示器配置 -->
      <areaIndicator>
        <indicatorType>Circle</indicatorType>
        <radiusSource>Custom</radiusSource>
        <customRadius>15.0</customRadius>
        <color>(0.3, 0.8, 1.0, 0.35)</color>
      </areaIndicator>
    </li>
  </modExtensions>
</ThingDef>
```

---

## 七、测试策略

### 7.1 单元测试（手动测试）

**测试用例 1：单侧爆炸弹**
- 装备左手爆炸芯片
- 点击攻击 Gizmo
- 预期：显示红色半透明圆圈，半径 = 爆炸半径

**测试用例 2：双侧不同范围**
- 左手爆炸弹（半径 3.0）
- 右手爆炸弹（半径 5.0）
- 点击双侧攻击 Gizmo
- 预期：显示两个同色圆圈，重叠部分颜色更深

**测试用例 3：ability 范围**
- 装备跳跃芯片
- 点击跳跃 Gizmo
- 预期：显示蓝色半透明圆圈，半径 = 跳跃范围

**测试用例 4：无配置芯片**
- 装备普通弹芯片（无 AreaIndicatorConfig）
- 点击攻击 Gizmo
- 预期：不显示范围圈，正常瞄准

**测试用例 5：引导弹 + 范围指示器**
- 装备引导爆炸弹
- 点击攻击 Gizmo，放置锚点
- 预期：同时显示引导路径和范围圈

### 7.2 集成测试

**测试场景 1：实战测试**
- 在战斗中使用爆炸弹攻击敌人
- 验证范围圈准确显示爆炸影响范围
- 验证实际爆炸效果与显示范围一致

**测试场景 2：性能测试**
- 多个 pawn 同时瞄准
- 验证不卡顿，帧率正常

### 7.3 回归测试

**验证点：**
- 现有芯片（无配置）仍正常工作
- 引导弹系统不受影响
- 双侧攻击逻辑不受影响
- 射程环、锚点路径等现有视觉效果正常显示

---

## 八、实现计划

### 8.1 第一阶段：核心功能

1. 创建 `AreaIndicatorConfig` 类
2. 创建 `IAreaIndicator` 接口
3. 实现 `CircleAreaIndicator`
4. 在 `Verb_BDPRangedBase` 中添加 `DrawAreaIndicators()` 方法
5. 在 `Verb_BDPSingle` 中实现单侧绘制
6. 在 `Verb_BDPDual` 中实现双侧绘制
7. 修改 `StartAnchorTargeting()` 集成范围指示器

### 8.2 第二阶段：配置和测试

1. 为 `BDP_Bullet_Explosive` 添加配置
2. 为跳跃芯片添加配置
3. 手动测试所有测试用例
4. 性能测试和优化
5. 回归测试

### 8.3 第三阶段：文档和发布

1. 更新用户文档
2. 添加配置示例
3. 发布更新日志

---

## 九、未来扩展

### 9.1 扇形指示器

- 实现 `SectorAreaIndicator`
- 支持角度和方向配置
- 使用 `Graphics.DrawMesh()` + 自定义网格

### 9.2 弧形指示器

- 实现 `ArcAreaIndicator`
- 支持起始角度和结束角度配置

### 9.3 矩形指示器

- 实现 `RectangleAreaIndicator`
- 支持长度和宽度配置

### 9.4 动态效果

- 脉冲效果（半径周期性变化）
- 旋转效果（扇形旋转）
- 渐变效果（颜色渐变）

---

## 十、总结

本设计文档详细描述了 BDP 模组范围指示器系统的架构、组件、数据流、错误处理和测试策略。

**核心优势：**
- 架构清晰，易于维护和扩展
- 向后兼容，不影响现有功能
- 性能优化，不影响游戏帧率
- 配置灵活，支持多种类型和样式

**下一步：**
- 编写详细的实现计划（PLAN.md）
- 开始第一阶段的代码实现

---

**文档版本**: 1.0
**最后更新**: 2026-03-10
**审核状态**: 待审核
