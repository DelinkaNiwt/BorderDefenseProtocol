---
标题：BDP 追踪模块设计方案
版本号: v1.0
更新日期: 2026-02-27
最后修改者: Claude Opus 4.6
标签: [文档][用户已确认][已完成][未锁定]
摘要: BDP模组追踪(Tracking)模块的架构设计，基于现有管线系统，实现实时追踪弹道
---

## 1. 需求概述

在 BDP 弹道管线中新增 TrackingModule，实现子弹发射后实时追踪目标。

核心约束：
- 复用原版飞行引擎，通过 IBDPPathResolver 每 tick 修改 destination
- 与 GuidedModule（折线弹道）共存：仅在末段（IsOnFinalSegment）激活追踪
- 双模式转向：Simple（角速度限幅）/ Smooth（角加速度+阻尼）
- 脱锁机制：超时自毁、目标丢失、角度超限、被拦截
- 目标丢失时可重新搜索附近敌人

## 2. 模块在管线中的位置

```
发射阶段:
  BDPModuleFactory 从 modExtensions 创建模块:
    GuidedModule    (Priority=10, 折线弹道)
    TrackingModule  (Priority=15, 追踪)      ← 新增
    ExplosionModule (Priority=50, 爆炸)
    TrailModule     (Priority=100, 拖尾)

每 Tick 管线执行:
  ① IBDPPathResolver  ← TrackingModule.ResolvePath() 修改 destination
  ② base.TickInterval()  原版引擎飞行 + 拦截检查
  ③ IBDPPositionModifier  显示位置修饰
  ④ IBDPTickObserver  ← TrackingModule.OnTick() 超时计数

到达时 (ticksToImpact ≤ 0):
  ⑤ IBDPArrivalHandler
     GuidedModule → 推进锚点 / 设置 IsOnFinalSegment
     TrackingModule → 目标未到达？重定向继续追踪
  ⑥ IBDPImpactHandler  爆炸/伤害
```

TrackingModule 实现 3 个管线接口：
- `IBDPPathResolver` — 每 tick 修改 destination（核心追踪逻辑）
- `IBDPTickObserver` — 计数飞行时间，处理超时
- `IBDPArrivalHandler` — 到达时判断是否需要继续追踪

## 3. 与 GuidedModule 的共存时序

共存场景（折线 + 末段追踪）：
```
发射 → 锚点1(直线) → 锚点2(直线) → 最终段(追踪激活) → 命中/超时
       GuidedModule 控制            TrackingModule 控制
       IsOnFinalSegment=false       IsOnFinalSegment=true
       TrackingModule: return       每tick修改destination
```

单独使用（无 GuidedModule）：
```
发射 → 直飞(delay tick) → 追踪激活 → 命中/超时
       trackingDelay期间   IsOnFinalSegment=true（默认）
       不追踪              持续转向跟随目标
```

## 4. 转向算法（双模式）

### Simple 模式（普通追踪弹）

参数：`maxTurnRate`（每tick最大转向角度）

```
每 tick:
  desiredAngle = atan2(target - current)
  angleDiff = desiredAngle - currentAngle
  actualTurn = clamp(angleDiff, -maxTurnRate, +maxTurnRate)
  currentAngle += actualTurn
  → destination = origin + dir(currentAngle) * distance
```

### Smooth 模式（超级追踪弹，追踪+追踪组合）

参数：`maxTurnRate` + `angularAccel` + `damping`

```
每 tick:
  desiredAngle = atan2(target - current)
  angleDiff = desiredAngle - currentAngle
  angAccel = clamp(朝向目标的加速, -angularAccel, +angularAccel)
  angularVelocity += angAccel
  angularVelocity = clamp(angularVelocity, -maxTurnRate, +maxTurnRate)
  angularVelocity *= damping
  currentAngle += angularVelocity
  → destination = origin + dir(currentAngle) * distance
```

## 5. 脱锁与重搜索流程

```
每 tick:
  飞行时间 > maxFlyingTicks? → 自毁
  目标有效(活着+在地图上)?
    是 → 角度 > maxLockAngle? → 脱锁，进入重搜索
         否 → 正常追踪
    否 → 进入重搜索

  重搜索(每 searchInterval tick):
    找到附近敌人? → 锁定新目标
    未找到? → 保持当前角度直飞
```

## 6. 新增与修改文件

新增文件（3个）：
- `Trigger/Projectiles/TrackingModule.cs` — 核心追踪模块
- `Trigger/Data/BDPTrackingConfig.cs` — XML配置 (DefModExtension)
- `Trigger/Projectiles/TargetSearcher.cs` — 目标搜索工具（解耦）

修改文件（1个）：
- `Trigger/Projectiles/Bullet_BDP.cs` — 新增 IBDPTickObserver 管线（如已存在则无需修改）

无需修改：GuidedModule、TrailModule、ExplosionModule、Verb层

## 7. BDPTrackingConfig 参数

```xml
<!-- 普通追踪弹 -->
<li Class="BDP.Trigger.BDPTrackingConfig">
  <turnMode>Simple</turnMode>
  <maxTurnRate>8</maxTurnRate>
  <trackingDelay>20</trackingDelay>
  <maxFlyingTicks>600</maxFlyingTicks>
  <maxLockAngle>120</maxLockAngle>
  <searchRadius>15</searchRadius>
  <searchInterval>30</searchInterval>
</li>

<!-- 超级追踪弹（追踪+追踪组合） -->
<li Class="BDP.Trigger.BDPTrackingConfig">
  <turnMode>Smooth</turnMode>
  <maxTurnRate>25</maxTurnRate>
  <angularAccel>5</angularAccel>
  <damping>0.95</damping>
  <trackingDelay>10</trackingDelay>
  <maxFlyingTicks>900</maxFlyingTicks>
  <maxLockAngle>180</maxLockAngle>
  <searchRadius>25</searchRadius>
  <searchInterval>15</searchInterval>
</li>
```

## 8. 参考模组

| 模组 | 方案 | 借鉴点 |
|------|------|--------|
| 机械族全面战争 | 自管飞行+角速度转向 | 三阶段速度、目标预判、搜索间隔、锁定互斥 |
| 联合重工 | 修改destination+角度锥 | 复用原版引擎的思路、maxHomingAngle |
| 天工铸造3 | 贝塞尔曲线 | 曲线弹道的视觉参考 |

---

## 历史修改记录
（注：摘要描述遵循奥卡姆剃刀原则）
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-27 | 初始设计，经用户确认 | Claude Opus 4.6 |
