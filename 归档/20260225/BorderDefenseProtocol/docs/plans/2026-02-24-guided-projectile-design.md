---
标题：变化弹（Guided Projectile）系统设计
版本号: v1.0
更新日期: 2026-02-24
最后修改者: Claude Opus 4.6
标签: [文档][用户已确认][已完成][未锁定]
摘要: 类似境界触发者"毒蛇"的变化弹芯片设计，支持多步锚点瞄准和折线弹道飞行
---

## 1. 概述

实现一种新的远程芯片能力——变化弹（Viper），玩家可在地图上放置多个路径锚点，子弹沿锚点折线飞行。

核心特征：
- 硬折线转弯，匀速飞行
- 沿途正常命中判定（同普通子弹），可被拦截
- 拦截处触发 Impact，剩余路径取消
- 锚点间需视线，最终落点可以是障碍物

## 2. 操作流程

```
点击芯片Gizmo → 进入瞄准
  Shift+左键 → 放置锚点（需与上一点有视线，起点到此点直线距离≤射程）
  左键 → 确认最终目标并发射
  右键 → 取消全部
  达到 maxAnchors 上限 → 下次点击自动作为最终目标
```

左键逐发和右键齐射均兼容变化弹模式。

## 3. 架构设计

### 3.1 新增文件

| 文件 | 职责 |
|------|------|
| `GuidedFlightController.cs` | 引导飞行控制器（纯逻辑，组合模式） |
| `Verb_BDPGuided.cs` | 单发/连射变化弹 Verb（继承 Verb_BDPShoot） |
| `Verb_BDPGuidedVolley.cs` | 齐射变化弹 Verb（继承 Verb_BDPVolley） |
| `GuidedTargetingHelper.cs` | 多步瞄准静态工具类（共享瞄准逻辑） |

### 3.2 修改文件

| 文件 | 改动 |
|------|------|
| `Bullet_BDP.cs` | 加入 GuidedFlightController 字段 + Tick 委托 |
| `Projectile_ExplosiveBDP.cs` | 同上 |
| `WeaponChipConfig.cs` | 新增 supportsGuided/maxAnchors/anchorSpread 字段 |

### 3.3 不需要改动

- Gizmo（Command_BDPChipAttack）：不感知变化弹，只调用 verb
- 弹道 Def（ThingDefs_Projectiles.xml）：引导是运行时行为
- 拖尾系统：天然兼容（每 tick 照常产生线段）

## 4. GuidedFlightController（组合模式）

```
字段：
  List<Vector3> waypoints    // 路径点列表（含最终目标）
  int currentIndex = 0       // 当前目标索引
  bool IsGuided              // 是否处于引导模式

方法：
  Init(List<Vector3> waypoints)
  bool TryAdvanceWaypoint(Projectile proj)
    → 到达当前锚点时：重置 origin/destination/ticksToImpact，返回 true
    → 已到最终目标：返回false，弹道正常 Impact
```

任何弹道子类通过组合使用此控制器，不需要继承。未来新增变化炸裂弹、变化追踪弹只需加几行委托代码。

## 5. 弹道飞行逻辑

重写 `Tick()`，在 `ticksToImpact <= 0` 时：
1. 检查 `controller.TryAdvanceWaypoint(this)`
2. 返回 true → 重置飞行参数（origin、destination、ticksToImpact），继续飞行
3. 返回 false → 正常调用 `Impact()`

origin、destination、ticksToImpact 均为 Projectile 的 protected 字段，子类可直接访问，无需反射。

## 6. 散布处理

锚点偏移 + 递增系数：
```
actualAnchor[i] = anchor[i] + Random.insideUnitCircle * anchorSpread * (i / totalAnchors)
```

- 第一段偏移最小，最后一段偏移最大
- 齐射时每颗子弹独立计算偏移
- anchorSpread 在 WeaponChipConfig 中配置

## 7. 多步瞄准（GuidedTargetingHelper）

复用 `Find.Targeter.BeginTargeting(params, callback)` 回调形式：
- 回调中检查 `Event.current.shift`
- Shift 按下 → 存为锚点，校验视线和射程，重新 BeginTargeting
- Shift 未按 → 作为最终目标，创建 Job 发射
- 瞄准期间绘制：已放置锚点间的折线 + 锚点标记 + 鼠标预览线

## 8. 配置字段

WeaponChipConfig 新增：
```csharp
public bool supportsGuided = false;  // 是否支持变化弹
public int maxAnchors = 3;           // 最大锚点数（不含最终目标）
public float anchorSpread = 0.3f;    // 锚点散布基础半径
```

## 9. 射程规则

- 起点到最终目标的直线距离 ≤ 武器射程
- 锚点间需 GenSight.LineOfSight 视线检查
- 折线总路径长度不限制（可绕路）

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-24 | 初版设计文档 | Claude Opus 4.6 |
