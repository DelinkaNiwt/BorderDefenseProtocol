# 旧 BDP 弹道宿主层与 Vanilla 适配层（第一版）

## 目的

这份记录专门回答五个问题：

- `Bullet_BDP` 在旧 BDP 里到底承担什么地位
- projectile pipeline 的真实边界是什么
- `Guided / Tracking / Explosion / Trail` 四类模块各自只负责什么
- `VanillaAdapter` 为什么是旧 BDP 里很关键的一层
- 对二次重构来说，弹道层应该吸取哪些东西，避开什么坑

## 一、先下结论

### 1. `Bullet_BDP` 是旧 BDP 里已经比较成熟的“宿主统一执行层”

它不是普通子弹上挂几个脚本。

它实际承担的是：

- 模块创建与缓存
- 每 tick 调度顺序 owner
- 飞行重定向唯一写入口 owner
- Phase 自动推导 owner
- 到达 / 命中 / Impact 最终执行 owner
- 视觉位置修饰 owner

换句话说：

- 模块不直接改底层飞行字段
- 真正握着 `origin / destination / ticksToImpact / Impact` 最终执行权的是 `Bullet_BDP`

这和前面读到的 `ShotPipeline` 很不一样。

### 2. 旧 BDP 真正把“模块只交意图，宿主统一执行”落实得更完整的地方，在弹道层，不在攻击层

远程攻击上游那边还停留在“半管线化”。

但子弹这一层已经明显更成熟：

- 模块接口分工清楚
- 宿主统一调度点清楚
- 真正危险的底层写入口被收口了

所以对二次重构来说，旧 BDP 更值得学的不是 `ShotPipeline`，而是 `Bullet_BDP` 这一套宿主思想。

## 二、`Bullet_BDP` 的真实结构

### 1. 它不是“只在 Impact 时跑模块”

它分成了清楚的阶段：

- `SpawnSetup`
- `TickInterval`
- `ImpactSomething`
- `Impact`
- 发射速度重初始化
- 序列化恢复

### 2. `SpawnSetup` 做的是“把一颗普通原版 projectile 接管成 BDP 弹道宿主”

它会做这些事：

- 首次生成时通过 `BDPModuleFactory.CreateModules(def)` 创建模块
- 按 `Priority` 排序
- 初始化三层目标：
  - `AimTarget`
  - `LockedTarget`
  - `CurrentTarget`
- 初始化发射时间
- 初始化穿透力
- 读取 `FlightRedirectConfig`
- 建立各管线接口缓存
- 初始化显示位置缓存
- 通知所有模块 `OnSpawn(this)`
- 配置 `VanillaAdapter` 策略
- 记录真实发射点

这里已经很清楚地说明：

- 模块不是自己挂载自己
- 宿主先统一建模，再通知模块加入运行

### 3. `BuildPipelineCache()` 的意义很大

它把模块按能力分组成：

- `IBDPLifecyclePolicy`
- `IBDPFlightIntentProvider`
- `IBDPPositionModifier`
- `IBDPVisualObserver`
- `IBDPArrivalPolicy`
- `IBDPHitResolver`
- `IBDPImpactHandler`
- `IBDPSpeedModifier`
- `IBDPPhaseTransitionObserver`

这意味着旧版这里的模块化，不是“一个万能模块接口里塞一堆 if”。

而是：

- 模块声明自己在哪些阶段参与
- 宿主按阶段统一调度

这就是比较健康的模块协议雏形。

## 三、`TickInterval` 管线已经很像真正的骨架

### 1. 阶段 0：PostLaunchInit

职责：

- 做 Launch 后延迟初始化
- 修正 SpawnSetup 时可能还没准备好的目标
- 先给视觉模块一次初始化机会

关键意义：

- 说明旧版明确承认 RimWorld 宿主时序本身不干净
- 所以它不是假装“所有数据都能在构造时一次准备好”
- 而是显式做 Launch 后补阶段

这很贴 RimWorld 现实。

### 2. 阶段 1：LifecycleCheck

职责：

- 让模块请求销毁
- 让模块请求修改 `LockedTarget`

但真正执行请求的是宿主。

这点很重要，因为它避免了：

- 模块自己随便 `Destroy()`
- 模块自己随便改宿主核心状态

### 3. 阶段 2：FlightIntent

职责：

- 让模块产出“下一步飞向哪”的意图
- 宿主只取第一个非空意图
- 最终统一调用 `ApplyFlightRedirect()`

这是整个弹道系统里最关键的收口点之一。

### 4. 阶段 3：回到 vanilla `base.TickInterval(delta)`

这点非常重要。

旧 BDP 在弹道层不是要完全取代 RimWorld。

它做的是：

- 在 vanilla 位置推进前准备好参数
- 然后把真正的移动、拦截检查、到达判定交还给原版

这正符合“从游戏出发，最终回归游戏”。

### 5. 阶段 4：PositionModifier

职责：

- 只修饰显示位置
- 不改逻辑位置

这是很健康的分层：

- 逻辑飞行和视觉偏移分开

### 6. 阶段 5：VisualObserve

职责：

- 只读观察
- 输出拖尾等视觉表现

这再次说明它不是让视觉模块乱改飞行逻辑。

## 四、`ImpactSomething` 和 `Impact` 的边界很清楚

### 1. `ImpactSomething` 负责“到达后的决策”

顺序是：

- 先过 `ArrivalPolicy`
- 再过 `VanillaAdapter.CheckBeforeImpact`
- 再过剩余 `HitResolver`
- 最后才 `base.ImpactSomething()`

关键事实：

- “继续飞”与“真正命中”是在这里决定的
- 真命中前还有一层 vanilla 兼容修正

### 2. `Impact` 负责“命中后效果落地”

顺序是：

- 先给 `IBDPImpactHandler` 机会处理
- 若某模块设置 `Handled=true`
- 则跳过 `base.Impact`
- 否则回到原版 `Impact`

说明：

- 旧版这里不是“所有模块都抢 Impact”
- 宿主仍然是那个决定最终是否回原版的人

## 五、`ApplyFlightRedirect()` 是弹道层最关键的红线

代码事实非常明确：

- `origin / destination / ticksToImpact` 只在这里写

它处理：

- 首段重定向不后退 origin
- 根据 `EffectiveSpeedTilesPerTick` 计算飞行参数
- 调用 `VanillaAdapter.ComputeAdaptedOrigin(...)`
- 近距离精确距离
- 远距离固定 tick 策略

这是旧版里最值得保留的纪律之一：

- 真正危险的底层飞行参数，必须只有一个统一入口能写

如果二次重构丢了这条纪律，弹道系统一定会重新散掉。

## 六、四个模块各自只负责什么

## 1. `GuidedModule`

真实职责：

- 接收锚点和最终目标
- 构建 `GuidedFlightController`
- 首 tick 提供一次飞向首锚点的意图
- 到达中间锚点时决定是否继续飞向下一段

它不负责：

- 自己改 `origin / destination`
- 自己调用 `Impact`
- 自己改底层飞行字段

结论：

- 它是真正的“路径规则模块”
- 不是子弹宿主

## 2. `TrackingModule`

真实职责：

- 飞行计时
- 超时 / 丢锁后的生命周期请求
- 每 tick 产出追踪方向意图
- 到达时判断要不要继续追踪
- 必要时重搜索新目标

关键观察：

- 它通过 `LifecycleContext` 请求销毁
- 通过 `FlightIntentContext` 请求新飞行意图
- 通过 `ArrivalContext` 请求继续飞
- 真执行仍由宿主完成

另外一个很重要的事实：

- 它的注释还写着自己参与 `IBDPHitResolver`
- 但真实代码已经不再实现 `IBDPHitResolver`
- 命中前兼容处理改由 `VanillaAdapter` 集中承担

这再次说明：

- 旧版注释不能直接当事实
- 必须以代码接口实现为准

## 3. `ExplosionModule`

真实职责非常单纯：

- 在 Impact 时根据配置做范围爆炸
- 替代默认单体伤害

它不负责：

- 飞行逻辑
- 目标逻辑
- 生命周期逻辑

这是很健康的“效果落地模块”。

## 4. `TrailModule`

真实职责也很单纯：

- `OnVisualInit` 时记录视觉起点
- `Observe` 时输出拖尾线段

它不负责：

- 飞行逻辑
- 命中逻辑
- 目标逻辑

这说明旧版弹道层里“视觉模块只做表现”这条边界是成立的。

## 七、`VanillaAdapter` 的地位比想象中更重要

### 1. 它不是小工具，而是“所有 vanilla 冲突的集中隔离层”

它自己写得很明确，专门负责三大冲突：

- origin 距离与沿途拦截的冲突
- usedTarget 与追踪命中的冲突
- SpawnSetup / Launch 时序与真实目标准备时机的冲突

这层非常关键，因为它做了一个正确动作：

- 不把“和 RimWorld 打架的 hack”散落到每个模块里
- 而是集中关在一个适配层里

这比到处埋特殊判断健康得多。

### 2. 它说明旧 BDP 已经意识到一个本质问题

不是所有复杂逻辑都该进“模块”。

有些东西本质上不是业务规则，而是：

- “如何和 RimWorld 原版机制和平共处”

这种东西就该单独隔离。

这对二次重构非常有启发。

### 3. `VanillaAdapter` 的真实职责

目前确认有三块：

- `ComputeAdaptedOrigin(...)`
  - 决定何时后退 origin 恢复原版拦截
- `TrySyncUsedTarget(...)`
  - 在命中前把 `usedTarget` 同步到真实锁定目标
- `CheckBeforeImpact(...)`
  - 在 Impact 前集中做“是否强制打地面”等兼容处理

所以它不是业务 owner，而是：

- 宿主和 RimWorld 原版机制之间的兼容缓冲层

## 八、速度修正说明 projectile pipeline 也不是完全自闭环

`Patch_Projectile_FireMode.cs` 说明了一件事：

- 弹道宿主层虽然已经很成熟
- 但仍需要外部 patch 在 `Projectile.Launch` 和 `Projectile.get_StartingTicksToImpact` 上接入

也就是说旧版 projectile pipeline 的真实形态是：

- `Bullet_BDP` 作为宿主核心
- 外加少量必要的 RimWorld patch 做接线

这点是正常的，不是缺陷。

因为 RimWorld 原版很多关键入口本来就必须 patch 才能接上。

## 九、模块注册入口也很清楚

`BDPMod` 静态构造里统一注册：

- `BeamTrailConfig -> TrailModule`
- `BDPGuidedConfig -> GuidedModule`
- `BDPExplosionConfig -> ExplosionModule`
- `BDPTrackingConfig -> TrackingModule`

说明：

- 模块不是在 XML 某处随意 new 出来的
- 注册入口集中在模组启动阶段

这对二次重构是个好信号：

- 以后模块能力注册，也应该有一个集中入口

## 十、这一层暴露出的几个重要问题

### 1. `VanillaAdapter.ExposeData()` 存在，但当前 `Bullet_BDP.ExposeData()` 没有看到调用它

这是代码事实，不是猜测。

目前 `Bullet_BDP.ExposeData()` 序列化了：

- `Phase`
- 三层目标
- 穿透力
- 发射时间
- 速度倍率
- 若干运行状态
- `modules`

但没看到：

- `vanillaAdapter.ExposeData()`

这意味着至少目前代码表面上存在一个风险：

- 适配层内部状态是否能完整跨存档恢复，不够确定

这里后面值得专门再核一下，不先下业务结论。

### 2. 注释更新速度落后于代码

例子很明显：

- `TrackingModule` 注释仍提到 `IBDPHitResolver`
- 真实实现里已经没有

这会误导后续设计阅读。

### 3. projectile pipeline 成熟，但还没有自然上推成统一攻击层

也就是说：

- 子弹层已经比较清楚
- 但上游攻击层还没有完全达到同等成熟度

这是旧版整体不均衡的一面。

## 十一、对二次重构的直接启发

### 1. 要保留“宿主统一执行，模块只交意图”这条纪律

尤其是以下红线应明确保留：

- 飞行底层参数只能统一入口写
- 最终命中和 Impact 只能宿主决策
- 模块只提请求，不直接执行危险动作

### 2. 要单独保留“宿主适配层”的概念

不要把所有和 RimWorld 原版打架的 hack：

- 塞进业务模块
- 或塞进事件总线

它们应该被单独隔离在“宿主适配层”。

### 3. 不要让事件总线碰弹道底层参数 owner

弹道这种强时序、高风险系统，不适合被松散事件直接驱动底层写入。

事件可以通知，
但真正写入：

- `origin`
- `destination`
- `ticksToImpact`
- `Impact`

必须收口在宿主。

### 4. 远程攻击层不应硬抄 projectile pipeline 的形状

应该学的是：

- 宿主纪律
- 适配层隔离
- 能力接口拆分

而不是把所有上游攻击行为机械套成同一套 per-tick 弹道接口。

## 十二、下一步阅读重点

建议继续细读：

- `ObstacleRouter.cs`
- `TargetSearcher.cs`
- `BDPTargetHelper.cs`
- `Patch_Pawn_TryGetAttackVerb.cs`
- `Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs`
- `Patch_VerbTracker_VerbsTick.cs`

目标：

- 把“攻击表达层”和“弹道宿主层”之间的桥接继续走通
- 尤其看清：
  - 目标选择
  - 自动攻击接线
  - Verb tick 补偿
  - projectile 宿主何时开始脱离上游控制
