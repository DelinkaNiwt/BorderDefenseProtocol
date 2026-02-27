---
标题：Bullet_BDP 宿主API参考手册——模块开发者指南
版本号: v1.0
更新日期: 2026-02-27
最后修改者: Claude Sonnet 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 列举 Bullet_BDP 宿主上所有可供模块访问的属性、方法、管线上下文，作为模块设计的参考说明书
---

# Bullet_BDP 宿主API参考手册

> 适用对象：实现 `IBDPProjectileModule` 及其管线子接口的模块开发者。
> 继承链：`Bullet_BDP → Bullet → Projectile → ThingWithComps → Thing → Entity`

---

## 一、模块可访问的宿主属性（完整清单）

### 1.1 继承自 Projectile / Bullet（原版引擎）

模块通过 `host.XXX` 直接访问。这些属性由引擎维护，模块只读。

| 属性名 | 类型 | 访问方式 | 读写 | 说明 |
|--------|------|----------|------|------|
| `Launcher` | `Thing` | `host.Launcher` | R | 发射者（BDP场景下通常是Pawn） |
| `EquipmentDef` | `ThingDef` | `host.EquipmentDef` | R | 武器/芯片的ThingDef（Verb层传入chipEquipment） |
| `DamageDef` | `DamageDef` | `host.DamageDef` | R | 伤害类型 |
| `DamageAmount` | `int` | `host.DamageAmount` | R | 最终伤害值（已含品质/FireMode修正） |
| `ArmorPenetration` | `float` | `host.ArmorPenetration` | R | 护甲穿透值 |
| `DrawPos` | `Vector3` | `host.DrawPos` | R | 当前显示位置（已经过PositionModifier修饰） |
| `ExactPosition` | `Vector3` | `host.ExactPosition` | R | 逻辑位置（未经修饰的引擎计算位置） |
| `ExactRotation` | `Quaternion` | `host.ExactRotation` | R | 当前朝向四元数 |
| `DistanceCoveredFraction` | `float` | `host.DistanceCoveredFraction` | R | 飞行进度 0→1（等同FlightProgress） |
| `DistanceCoveredFractionArc` | `float` | `host.DistanceCoveredFractionArc` | R | 弧线飞行进度（抛物线用） |
| `StartingTicksToImpact` | `float` | `host.StartingTicksToImpact` | R | 总飞行tick数（由距离和速度决定） |
| `DestinationCell` | `IntVec3` | `host.DestinationCell` | R | 目标格子 |
| `ArcHeightFactor` | `float` | `host.ArcHeightFactor` | R | 弧线高度系数（0=平射） |
| `HitFlags` | `ProjectileHitFlags` | `host.HitFlags` | R | 命中标志（可命中目标/非目标/世界） |
| `Map` | `Map` | `host.Map` | R | 当前地图 |
| `Position` | `IntVec3` | `host.Position` | R | 当前格子位置 |
| `def` | `ThingDef` | `host.def` | R | 投射物ThingDef |

### 1.2 通过 def 访问的配置属性（ProjectileProperties）

模块通过 `host.def.projectile.XXX` 访问。Def级只读配置。

| 属性名 | 类型 | 访问方式 | 说明 |
|--------|------|----------|------|
| `speed` | `float` | `host.def.projectile.speed` | 基础飞行速度（格/秒） |
| `flyOverhead` | `bool` | `host.def.projectile.flyOverhead` | 是否高角抛物线（迫击炮式） |
| `arcHeightFactor` | `float` | `host.def.projectile.arcHeightFactor` | 弧线高度系数 |
| `stoppingPower` | `float` | `host.def.projectile.stoppingPower` | 停止力（击退相关） |
| `damageAmountBase` | `int` | `host.def.projectile.damageAmountBase` | 基础伤害（未含品质修正） |
| `armorPenetrationBase` | `float` | `host.def.projectile.armorPenetrationBase` | 基础护甲穿透 |
| `damageDef` | `DamageDef` | `host.def.projectile.damageDef` | 伤害类型Def |
| `explosionRadius` | `float` | `host.def.projectile.explosionRadius` | 原版爆炸半径（BDP用BDPExplosionConfig替代） |
| `extraDamages` | `List<ExtraDamage>` | `host.def.projectile.extraDamages` | 附加伤害列表 |
| `spinRate` | `float` | `host.def.projectile.spinRate` | 旋转速率（视觉） |

### 1.3 通过 def 访问的BDP扩展配置（DefModExtension）

模块通过 `host.def.GetModExtension<T>()` 访问。

| 配置类 | 用途 | 关联模块 |
|--------|------|----------|
| `BDPGuidedConfig` | 标记类，存在即启用引导飞行 | GuidedModule |
| `BDPExplosionConfig` | 爆炸半径、爆炸伤害类型 | ExplosionModule |
| `BeamTrailConfig` | 拖尾宽度、颜色、衰减参数 | TrailModule |
| *(预留)* | 未来模块的配置扩展点 | — |

### 1.4 Bullet_BDP 自有字段（BDP扩展状态）

模块通过 `host.XXX` 直接访问。标注了写入者的字段，其他模块应视为只读。

| 字段名 | 类型 | 读写 | 写入者 | 默认值 | 说明 |
|--------|------|------|--------|--------|------|
| `FinalTarget` | `LocalTargetInfo` | R/W | GuidedModule | =intendedTarget | 真实最终目标（引导弹覆盖） |
| `IsOnFinalSegment` | `bool` | R/W | GuidedModule | true | 是否在最终飞行段 |
| `IsTracking` | `bool` | R/W | PathResolver模块 | false | 是否正在追踪目标 |
| `TrackingTarget` | `LocalTargetInfo` | R/W | PathResolver模块 | Invalid | 追踪目标（可中途切换） |
| `PassthroughPower` | `float` | R/W | ImpactHandler模块 | 由芯片配置初始化 | 穿体穿透剩余力（每次穿透递减） |
| `PassthroughCount` | `int` | R/W | ImpactHandler模块 | 0 | 已穿透实体次数 |
| `LaunchTick` | `int` | R | 宿主SpawnSetup | 自动写入 | 发射时的游戏tick |

> **穿体穿透 vs 护甲穿透**：`PassthroughPower` 表示子弹穿过目标继续飞行的能力（穿体），
> 与原版 `ArmorPenetration`（护甲穿透，决定伤害是否被甲减免）是两个独立概念。

### 1.5 Bullet_BDP 提供的方法

| 方法 | 签名 | 调用者 | 说明 |
|------|------|--------|------|
| `RedirectFlight` | `(Vector3 newOrigin, Vector3 newDest)` | 模块 | 重定向飞行（重置origin/destination/ticksToImpact） |
| `ReinitFlight` | `(float speedMult)` | Harmony Patch | 速度修正（修改destination使飞行时间变化） |
| `DispatchSpeedModifiers` | `(float speedMult)` | Harmony Patch | 速度管线入口（分发IBDPSpeedModifier后调用ReinitFlight） |
| `GetModule<T>` | `() → T` | Verb层/模块 | 获取指定类型的模块实例 |
| `GetCapability<T>` | `() → T` | 预留 | 查询是否具备某管线能力 |

---

## 二、管线上下文结构体

模块通过 `ref` 参数读写上下文，控制管线流程。

| 结构体 | 阶段 | 只读字段 | 可写字段 | 短路机制 |
|--------|------|----------|----------|----------|
| `PathContext` | 1-路径解析 | `Origin` | `Destination` | 无（链式叠加） |
| `PositionContext` | 3-位置修饰 | `LogicalPosition`, `Progress` | `DrawPosition` | 无（链式叠加） |
| `ArrivalContext` | 5-到达决策 | — | `Continue` | ✅ Continue=true 立即短路 |
| `ImpactContext` | 6-命中效果 | `HitThing`, `BlockedByShield` | `Handled` | ✅ Handled=true 立即短路 |
| `SpeedContext` | 发射时 | `BaseSpeedMult` | `SpeedMult` | 无（链式叠加） |

---

## 三、管线执行顺序

```
每tick：
  1. PathResolver     → 修改 destination（链式叠加）
  2. base.TickInterval → 引擎位置计算 + 拦截检查 + 到达判定
  3. PositionModifier  → 修饰显示位置（链式叠加）
  4. TickObserver      → 只读通知（拖尾/视觉/音效）

到达时（ticksToImpact ≤ 0）：
  5. ArrivalHandler    → 到达决策（Continue=true → 短路跳过Impact）
  6. ImpactHandler     → 命中效果（Handled=true → 短路跳过base.Impact）

发射时（一次性）：
  7. SpeedModifier     → 速度修正（链式叠加）
```

同一阶段内，模块按 `Priority` 升序执行（10→50→100）。

---

## 四、常用推导值速查

以下值不需要宿主存储，模块自行一行计算即可：

| 需求 | 计算方式 | 备注 |
|------|----------|------|
| 飞行进度 0→1 | `host.DistanceCoveredFraction` | 原版属性，直接用 |
| 飞行方向 Vector3 | `(destination - origin).normalized` | 需通过管线上下文间接获取，或用 `host.ExactRotation` |
| 已飞行距离 | `host.DistanceCoveredFraction * totalDist` | totalDist 见下行 |
| 总飞行距离 | `(destination - origin).magnitude` | 通过 PathContext 可获取 origin/destination |
| 是否高角抛物线 | `host.def.projectile.flyOverhead` | Def级配置 |
| 弧线高度 | `host.ArcHeightFactor` | 原版属性 |
| 发射者Pawn | `host.Launcher as Pawn` | BDP场景下 launcher 就是 Pawn |
| 芯片ThingDef | `host.EquipmentDef` | Verb层传入的 chipEquipment.def |
| 基础伤害 | `host.DamageAmount` | 原版属性 |
| 伤害类型 | `host.DamageDef` | 原版属性 |
| 护甲穿透 | `host.ArmorPenetration` | 原版属性（区别于穿体穿透） |
| 存活时长(tick) | `Find.TickManager.TicksGame - host.LaunchTick` | 需 LaunchTick（新增字段） |

---

## 五、扩展预留

以下是当前未实现但架构已预留接口的管线阶段，未来模块可直接实现：

| 管线接口 | 状态 | 预期用途 |
|----------|------|----------|
| `IBDPPathResolver` | 接口已定义，无实现 | 追踪弹、锁定目标、风偏 |
| `IBDPPositionModifier` | 接口已定义，无实现 | 抛物线高度、弹道抖动、螺旋 |
| `IBDPSpeedModifier` | 接口已定义，无实现 | 加速/减速弹、重力衰减 |
| `IBDPTickObserver` | 已有 TrailModule | 音效、粒子、光照 |
| `IBDPArrivalHandler` | 已有 GuidedModule | 穿透继续飞、分裂、延迟引爆 |
| `IBDPImpactHandler` | 已有 ExplosionModule | 范围伤害、DOT、生成物 |

如需新增管线阶段（如 `IBDPDamageModifier` 修改最终伤害），步骤：
1. 在 `Pipeline/` 下定义接口 + Context 结构体
2. 在 `Bullet_BDP` 中添加缓存列表 + `BuildPipelineCache` 注册
3. 在对应生命周期方法中添加分发循环
4. 更新本文档

---

## 历史修改记录
（注：摘要描述遵循奥卡姆剃刀原则）
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-27 | 初版：整理全部宿主API、管线上下文、推导值速查、扩展预留 | Claude Sonnet 4.6 |

