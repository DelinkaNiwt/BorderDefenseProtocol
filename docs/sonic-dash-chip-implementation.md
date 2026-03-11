# 爆音冲刺芯片 - 实现说明（v2 - 直线冲刺版本）

## 概述
基于米莉拉模组的"Hyper-velocity Flight"和联合重工模组的"涡喷冲刺"能力，为BDP芯片系统实现了**真正的直线快速冲刺**功能。

## 版本历史

### v2 (当前版本) - 直线冲刺
- **实现方式**: 自定义`PawnFlyer_SonicDash`类
- **飞行轨迹**: 直线（线性插值）
- **速度**: 非常快（flightSpeed=30）
- **高度**: 固定低空（接近地面）

### v1 (已废弃) - 弧线跳跃
- **实现方式**: 原版`PawnFlyer`类
- **问题**: 依然是弧线跳跃，速度不够快
- **原因**: 原版使用抛物线算法计算飞行轨迹

## 实现方式

### 1. 核心差异分析

#### 原版PawnFlyer（弧线跳跃）
```csharp
// 原版使用抛物线曲线
float t2 = def.pawnFlyer.Worker.AdjustedProgress(t);
effectiveHeight = def.pawnFlyer.Worker.GetHeight(t2);
groundPos = Vector3.LerpUnclamped(startVec, DestinationPos, t2);
```

#### 我们的实现（直线冲刺）
```csharp
// 线性插值 - 直线移动
float progress = (float)ticksFlying / totalTime;
Vector3 position = Vector3.Lerp(startVec, DestinationPos, progress);
position.y = FlatAltitude; // 固定高度
```

### 2. 核心文件

#### C#代码
- `PawnFlyer_SonicDash.cs` - 自定义飞行者类
  - 重写`DrawPos`属性使用线性位置计算
  - `GetLinearPosition()`方法实现直线插值
  - 固定飞行高度（低空）

#### XML定义
- `AbilityDefs_SonicDash.xml` - 能力和飞行者定义
  - `BDP_Ability_SonicDash` - 爆音冲刺能力
  - `BDP_PawnFlyer_SonicDash` - 使用自定义类`BDP.Trigger.PawnFlyer_SonicDash`

- `ThingDefs_Chips.xml` - 芯片物品定义
  - `BDP_Chip_SonicDash` - 爆音冲刺芯片

### 3. 关键参数

```xml
<pawnFlyer>
  <flightSpeed>30</flightSpeed>         <!-- 非常快的速度 -->
  <flightDurationMin>0.3</flightDurationMin>  <!-- 最短0.3秒 -->
  <heightFactor>0.3</heightFactor>      <!-- 低空（虽然被代码重写） -->
</pawnFlyer>

<cooldownTicksRange>300</cooldownTicksRange>  <!-- 5秒冷却 -->
<activationCost>10</activationCost>            <!-- 激活消耗10 Trion -->
```

## 与其他模组的对比

| 特性 | 原版跳跃 | 米莉拉爆音飞行 | 联合重工涡喷冲刺 | BDP爆音冲刺 |
|------|---------|--------------|----------------|------------|
| 飞行轨迹 | 弧线 | 直线 | 直线 | **直线** |
| 实现方式 | 原版PawnFlyer | 自定义Thing | 自定义PawnFlyer | **自定义PawnFlyer** |
| 代码复杂度 | 0行 | ~400行 | ~130行 | **~35行** |
| 位置计算 | 抛物线曲线 | 自定义算法 | 线性插值 | **线性插值** |
| 高度控制 | 动态曲线 | 自定义 | 固定 | **固定** |
| 碰撞检测 | 无 | 无 | 有（击退敌人） | 无 |

## 使用方法

### 游戏内测试步骤

1. **获取芯片**
   ```
   开发模式 → 物品生成 → BDP_Chip_SonicDash
   ```

2. **装备芯片**
   - 打开战斗体界面
   - 将芯片插入左手或右手槽位
   - 激活芯片

3. **使用能力**
   - 选中角色
   - 点击"Sonic Dash"能力图标
   - 选择目标位置（24.9格范围内）
   - 角色**直线快速冲刺**到目标位置

### 预期效果（v2）
- ✅ **直线移动**（不是弧线跳跃）
- ✅ **非常快的速度**（约0.3-0.8秒完成）
- ✅ **低空飞行**（接近地面，像瞬移冲刺）
- ✅ 消耗10点Trion
- ✅ 5秒冷却时间

## 技术细节

### 为什么原版是弧线？

原版`PawnFlyer`使用`PawnFlyerProperties.Worker`来计算飞行轨迹：
- `AdjustedProgress(t)` - 返回调整后的进度（非线性）
- `GetHeight(t)` - 返回抛物线高度

这导致了弧线跳跃效果。

### 如何实现直线？

通过重写`DrawPos`属性，使用简单的线性插值：
```csharp
public override Vector3 DrawPos => GetLinearPosition();

private Vector3 GetLinearPosition()
{
    float progress = (float)ticksFlying / totalTime;
    Vector3 position = Vector3.Lerp(startVec, DestinationPos, progress);
    position.y = FlatAltitude; // 固定高度
    return position;
}
```

## 扩展建议

1. **添加冲刺特效**
   - 在`Tick()`中添加尾迹粒子
   - 参考联合重工的`SpawnTrailMotes`

2. **添加碰撞伤害**
   - 在`Tick()`中检测路径上的敌人
   - 造成伤害和击退效果

3. **添加音效**
   - 冲刺开始音效
   - 冲刺过程中的音效
   - 落地音效

## 注意事项

1. **图标**: 当前使用占位符图标，需要创建专用图标
2. **平衡性**: 速度和冷却时间可能需要根据游戏测试调整
3. **兼容性**: 与其他移动能力（跳跃背包等）可能需要测试

---
*实现日期: 2026-03-08*
*版本: v2 (直线冲刺)*
*基于: 米莉拉模组 + 联合重工模组*
