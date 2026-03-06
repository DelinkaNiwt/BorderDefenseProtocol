# BDP 弹道模块开发约束

> 本文档是 v5 管线架构的强制性开发规范。
> 所有新增/修改模块的代码必须遵守以下规则。

---

## 核心原则

**模块只产出意图，宿主统一执行。**

模块是"顾问"，不是"操作员"。模块通过 Context 结构体表达建议，
宿主（Bullet_BDP）在固定调度点读取建议并决定是否执行。

---

## 红线规则（违反即打回）

### R1. 禁止直写飞行参数
`origin`、`destination`、`ticksToImpact` 只允许在 `Bullet_BDP.ApplyFlightRedirect()` 中赋值。
模块需要改变飞行方向时，写 `ctx.Intent.TargetPosition`，由宿主调用 ApplyFlightRedirect。

### R2. 禁止调用生命周期方法
模块内禁止调用 `host.Destroy()`、`host.Impact()`、`host.DeSpawn()`。
需要销毁时写 `ctx.RequestDestroy = true` + `ctx.DestroyReason`。

### R3. 禁止写 Phase
`Phase` 的 setter 是 `private set`，只有宿主内部可写。
模块通过 `ctx.RequestPhaseChange = FlightPhase.Xxx` 请求转换，宿主决定是否采纳。

### R4. 禁止写命中判定字段
模块内禁止直接赋值 `host.usedTarget`。
需要修正命中时写 `ctx.OverrideTarget` 或 `ctx.ForceGround`。

### R5. 模块间零直接通信
模块 A 禁止持有模块 B 的引用、禁止调用模块 B 的方法、禁止读模块 B 的字段。
模块间唯一的协作媒介是 `host.Phase`（只读）。
如果你发现自己需要读另一个模块的状态，说明设计有问题——应该把该状态提升到 Phase 或宿主公共属性上。

### R6. VisualObserver 零副作用
实现 `IBDPVisualObserver.Observe()` 的代码禁止修改宿主任何状态。
只允许读取宿主位置/Phase等信息用于创建视觉效果（拖尾、粒子等）。

---

## 黄线规则（需要充分理由才可突破）

### Y1. 宿主公共属性写入需审批
`host.TrackingTarget`、`host.FinalTarget` 等公共属性，模块可以写入，
但必须在注释中说明写入时机和理由。新增可写属性需要评估是否会引入隐式耦合。

### Y2. 避免在模块中缓存宿主引用
模块不应在字段中长期持有 `Bullet_BDP host` 引用。
所有管线方法都会传入 host 参数，用参数即可。长期持有会诱导在非调度时机操作宿主。

### Y3. 新增管线接口需同步更新宿主调度
如果现有 7 个管线接口不够用，可以新增接口，但必须：
- 在 Bullet_BDP 中新增对应的调度阶段
- 在 BuildPipelineCache() 中新增缓存
- 在本文档中补充接口说明

---

## 检查清单（Code Review 用）

新增/修改模块时，逐条检查：

```
[ ] 模块内无 host.origin / host.destination / host.ticksToImpact 赋值
[ ] 模块内无 host.Destroy() / host.Impact() / host.DeSpawn() 调用
[ ] 模块内无 Phase 赋值（只读 ctx.CurrentPhase 或 host.Phase）
[ ] 模块内无 host.usedTarget 赋值
[ ] 模块内无其他模块的引用或方法调用
[ ] VisualObserver 实现无任何宿主状态修改
[ ] 所有意图通过 ctx 结构体表达
[ ] 日志使用 TrackingDiag.Enabled 守卫，热路径加 Interval 间隔
[ ] ExposeData() 序列化了所有需要存档的私有字段
```

---

## 管线接口速查

| 接口 | 调度时机 | 模块职责 | 禁止操作 |
|------|----------|----------|----------|
| IBDPLifecyclePolicy | TickInterval 阶段1 | 计时、超时判断、丢锁检测 | Destroy, Phase写入 |
| IBDPFlightIntentProvider | TickInterval 阶段2 | 产出飞行方向意图 | 写origin/dest/TTI |
| IBDPPositionModifier | TickInterval 阶段4 | 修饰显示位置（弧线等） | 写实际飞行参数 |
| IBDPVisualObserver | TickInterval 阶段5 | 视觉效果（拖尾、粒子） | 任何宿主状态修改 |
| IBDPArrivalPolicy | ImpactSomething 阶段6 | 到达时决定继续飞/命中 | Destroy, Impact |
| IBDPHitResolver | ImpactSomething 阶段7 | 修正命中目标 | 写usedTarget |
| IBDPImpactHandler | Impact 阶段8 | 命中后效果（爆炸、穿透） | 写飞行参数 |
| IBDPSpeedModifier | Launch时一次性 | 修正初始速度 | 写origin/dest |
