# 旧 BDP 攻击执行层与 ShotPipeline 地位（第一版）

## 目的

这份记录专门回答四个问题：

- `ShotPipeline` 在旧 BDP 里到底是不是“真实射击执行层”
- `Verb_BDPRangedBase` 到底接管了哪些宿主职责
- 旧代码里“手动锚点 / 自动绕行 / Trion 消耗 / 弹道注入”分别落在哪
- 为什么这里看起来像统一协议，但实际又没有完全落成

## 一、先下结论

### 1. `ShotPipeline` 不是旧 BDP 的完整攻击执行层

它更准确的地位是：

- 一个“远程射击意图整理层”
- 负责把部分瞄准 / 发射模块的意图合并成结果
- 但不真正拥有完整的开火流程

真实发射主体仍然是：

- `Verb_BDPRangedBase.TryCastShotCore()`
- `Verb_BDPRangedBase.LaunchProjectile()`
- 各个远程 Verb 子类自己的 `ExecuteFire()`

也就是说：

- 管线负责“给宿主准备一些结果”
- 宿主自己仍然握着真正的发射权

### 2. 旧 BDP 这里是“半管线化”，不是“已完成的统一协议”

从代码事实看，旧版处在一种中间状态：

- 注释和结构已经明显在往“模块只交意图，宿主统一执行”走
- 但真实业务仍大量停留在旧的 Verb 逻辑里
- 所以会出现“结构看着像统一了，实际没有统一到底”的情况

这点对二次重构很重要：

- 不能把这里当成熟答案直接照抄
- 但也不能忽视这里已经摸到的正确方向

## 二、`ShotPipeline` 真正做了什么

### 1. `ShotPipeline.Build()` 只是在拼模块表

当前默认配置里：

- Aim 模块：
  - `LosCheckModule`
  - `AutoRouteAimModule`
  - `AnchorAimModule`
- Fire 模块：
  - `VolleySpreadModule`
  - `TrionCostModule`
  - `FlightDataModule`
  - `AutoRouteFireModule`

说明：

- 它是一个无状态构建器
- 真正有状态的数据放在 `ShotSession`

### 2. `ShotSession` 是远程射击过程的临时状态容器

它持有：

- 只读快照：`ShotContext`
- Aim 阶段结果：`AimIntents`、`AimResult`
- Fire 阶段结果：`FireIntents`、`FireResult`
- 瞄准交互累积数据：`AnchorPath`
- 模块间共享数据：`RouteResult`、`SharedData`

结论：

- `ShotSession` 更像“远程射击这一轮的工作台”
- 不是全局规则 owner
- 也不是攻击系统的统一真值中心

### 3. `ShotContext` 的价值是真正把“入口快照”收拢到一处

它会快照：

- 施法者
- 触发体
- 当前目标
- 当前 Verb
- 当前芯片配置
- 侧别
- 当前芯片 Thing
- 当前投射物 Def

这部分很有价值，因为它已经在做一件正确的事：

- 模块不再到处自己乱查状态
- 宿主先把一轮射击需要的入口信息整理好，再交给模块读

这就是以后值得保留的“统一入口快照”思路。

## 三、Aim 阶段和 Fire 阶段到底有没有真正跑起来

### 1. Aim 阶段确实实现了合并逻辑

`ShotPipeline.ExecuteAim()` 会：

- 清空旧 `AimIntents`
- 依次执行所有 `IShotAimModule.ResolveAim()`
- 再调用 `MergeAimIntents()`
- 产出 `AimResult`

合并规则已经比较清楚：

- 任一模块要求中止，整体中止
- 目标可被覆盖
- 瞄准偏移累加
- 锚点路径后写覆盖
- 精度倍率累乘
- 强制偏移半径取最大值

### 2. Fire 阶段也实现了合并逻辑

`ShotPipeline.ExecuteFire()` 会：

- 清空旧 `FireIntents`
- 依次执行所有 `IShotFireModule.OnFire()`
- 再调用 `MergeFireIntents()`
- 产出 `FireResult`

合并规则同样明确：

- 任一模块要求中止，整体中止
- 投射物覆盖后写优先
- 散布累加
- 伤害 / 速度倍率累乘
- Trion 消耗累加
- 自动绕行标记聚合

### 3. 但代码事实表明：这两个阶段没有真正完整接入真实发射链

目前确认到的事实：

- `DrawHighlight()` 会显式调用 `ShotPipeline.ExecuteAim()`
- 也就是瞄准预览阶段确实在跑 Aim 管线
- 但 `Verb_BDPRangedBase.TryCastShot()` 本身只是：
  - 确保有 `activeSession`
  - 直接调用子类 `ExecuteFire(activeSession)`

我在当前已读到的真实代码里，没有看到：

- `TryCastShot()` 先统一执行 `ExecuteAim()`
- 再统一执行 `ExecuteFire()`
- 再由宿主依据 `AimResult` / `FireResult` 完整开火

这意味着：

- 当前远程真实开火路径并不是完全由 `ShotPipeline` 驱动
- 至少在现有代码事实里，它更像“部分瞄准支持 + 结果注入辅助层”

这是本轮最重要的结论之一。

## 四、`Verb_BDPRangedBase` 才是远程攻击宿主

### 1. `TryCastShot()` 只是改了调度入口，不等于把宿主职责移交给管线

`Verb_BDPRangedBase.TryCastShot()` 做的事很少：

- 初始化管线
- 保证 `activeSession` 存在
- 调用子类 `ExecuteFire(session)`

这说明它没有把“统一发射”整体交给管线，而是只是把子类调度入口标准化了。

### 2. 真实发射仍由 `TryCastShotCore()` / `LaunchProjectile()` 执行

这里仍然直接负责：

- LOS 检查
- 原版 `ShootLine` 生成
- 通知装备级 comp
- 枪口位置计算
- `Projectile` 实例生成
- 原版 miss / wild miss / cover miss / intended target 逻辑
- `proj.Launch(...)`
- 发射后回调 `OnProjectileLaunched(proj)`

结论：

- 这些全是“宿主最终执行权”
- `ShotPipeline` 并没有接管这些底层行为

### 3. `OnProjectileLaunched()` 只是把结果注入 `Bullet_BDP`

默认实现只做一件事：

- `bdp.InjectShotData(activeSession.AimResult, activeSession.FireResult, activeSession.RouteResult)`

所以这里的真实关系是：

- Verb 先按旧逻辑把子弹发出来
- 发出来以后，再把瞄准 / 射击结果塞给 `Bullet_BDP`

这不是“管线负责发射”

这更像：

- “宿主先发射”
- “管线结果再注入弹道后处理”

## 五、瞄准模块和发射模块的真实边界

### 1. `AnchorAimModule` 是比较健康的一块

它做的事比较克制：

- 渲染锚点路径预览
- 从 `session.AnchorPath` 读取用户已放下的锚点
- 产出 `AimIntent.AnchorPoints`

也就是说：

- 它只描述“用户的锚点意图”
- 不自己发射，不自己碰弹道

这部分很接近以后该保留的模块边界。

### 2. `AutoRouteAimModule` 也是“产出路径意图”，方向基本对

它会：

- 判断当前是否允许自动绕行
- 无直接 LOS 时计算左右两条绕行路径
- 把结果写入 `session.RouteResult`
- 选一侧锚点写进 `AimIntent.AnchorPoints`

这说明它的真实职责是：

- 生成绕行方案
- 不直接碰弹道飞行

这也是比较健康的。

### 3. `TrionCostModule` 暴露出“设计方向对，但接线没接完”

这个模块会：

- 读芯片消耗
- 检查 Trion 是否足够
- 不足则返回 `AbortShot`
- 足够则把 `TrionCost` 写入 `FireIntent`

关键问题在于它自己的注释就写着：

- “实际消耗由宿主在 TryCastShot 后执行”

但我目前读到的真实远程子类：

- `Verb_BDPSingle`
- `Verb_BDPDual`
- `Verb_BDPCombo`

它们仍然直接用：

- `ChipUsageCostHelper.CanAffordUsage(...)`
- `ChipUsageCostHelper.ConsumeUsageCost(...)`
- 或直接 `CompTrion.TryConsume(...)`

结论：

- `TrionCostModule` 的“统一消耗协议”并没有真正成为实际唯一通道
- 它更像未完全落地的尝试

### 4. `FlightDataModule` 和 `AutoRouteFireModule` 也有类似问题

它们会把：

- 手动引导路径
- 自动绕行标志

转成 `session` 里的后续数据。

但当前 `Bullet_BDP.InjectShotData()` 实际读取的是：

- `aimResult`
- `fireResult`
- `routeResult`

并没有看到它去消费 `session.SharedData["FlightData"]`

结论：

- `FlightDataModule` 有明显“中间设计遗留”痕迹
- 它代表一种想法，但没有形成稳定唯一通道

## 六、`Bullet_BDP` 的位置也要看准

### 1. `Bullet_BDP` 已经明显比 ShotPipeline 更接近成熟宿主

它的核心原则写得很清楚：

- 模块只产出意图
- 宿主统一执行
- `origin/destination/ticksToImpact` 只允许宿主统一写
- `Destroy()/Impact()` 只允许宿主内部调用

这说明旧 BDP 真正把“宿主统一执行”落实得更完整的地方，其实是在弹道层。

### 2. `InjectShotData()` 的地位是“把上游的瞄准 / 发射结果投递给弹道宿主”

它处理顺序很明确：

- 优先自动绕行双路径左右分配
- 其次自动绕行单路径
- 再其次手动锚点路径
- 否则保持直射

所以旧版真实结构更像：

- 上游 Verb 决定“这一发要不要打、怎么生成子弹”
- 下游 `Bullet_BDP` 决定“子弹之后怎么飞、怎么转相、怎么命中”

这条边界比 `ShotPipeline` 自己的边界更清晰。

## 七、为什么 `Verb_BDPMelee` 没接入这条管线

### 1. 因为它本质上不属于“投射物注入链”

近战真实依赖的是：

- `Verb_MeleeAttackDamage`
- `Tool`
- `Maneuver`
- 原版近战命中与伤害结算
- Burst 时序与 JobDriver 手动推进

它的核心问题不是：

- 如何组织投射物飞行

而是：

- 如何把芯片近战表达适配回原版近战链
- 如何让脱离 `VerbTracker` 的自定义近战 Verb 正常 burst

### 2. 旧版近战真正的宿主问题是“引擎时序适配”，不是“意图合并”

目前已确认它重点处理的是：

- `ShotsPerBurst` 修正
- `OrderForceTarget()` 改走自定义 Job
- `VerbTick()` 不会自动推进的问题
- `Stance_Cooldown` / `FullBodyBusy` 的时序冲突
- `tool` / `maneuver` 手动补齐

结论：

- 近战没接入 `ShotPipeline` 不一定是缺点
- 更像是因为它根本面对的是另一类宿主问题

也因此，二次重构时不能为了形式统一，把近战硬塞进“远程射击管线”。

## 八、对二次重构最有价值的吸取点

### 1. 要保留“模块只交意图，宿主统一执行”这个方向

这是对的。

但要注意：

- 宿主必须是真宿主
- 不能只在注释里统一，代码里还是多处各自执行

### 2. 要把“入口快照”和“过程状态”分开

旧版这里比较好的苗头是：

- `ShotContext` 做入口快照
- `ShotSession` 做本轮状态容器

这个思路值得保留。

### 3. 不能让“统一协议”只停留在结果对象层

旧版现在的问题就是：

- `AimIntent / FireIntent / AimResult / FireResult` 看起来很完整
- 但真正的发射、消耗、注入、回退逻辑仍没完全统一到这条链上

所以二次重构时必须避免：

- 对象很漂亮
- 实际业务还是散在老地方

### 4. 远程与近战应共享上层攻击语言，但不应强行共用同一条底层执行链

基于旧代码事实，我现在更倾向于：

- 上层共享“攻击表达 / 入口 / 主攻击投影 / 规则声明”
- 远程回到 `Verb + Projectile` 宿主链
- 近战回到 `Verb_MeleeAttackDamage + JobDriver` 宿主链

而不是做一个抽象得过头的大一统底层。

这也更符合用户要求的“从 RimWorld 出发，最终回到 RimWorld”。

## 九、这轮阅读后最需要警惕的误判

### 1. 不能因为有 `ShotPipeline` 就误判旧版已经完成统一执行层

没有。

目前更像：

- 瞄准和部分发射规则被模块化了
- 但完整业务还没完全迁进去

### 2. 不能把注释里的“v16.0管线重构”直接当事实

代码事实比注释更重要。

当前真实情况是：

- 部分重构发生了
- 但统一执行层并没有完全形成

### 3. 不能把 `Bullet_BDP` 和 `ShotPipeline` 混为一谈

二者地位不同：

- `ShotPipeline` 更像上游远程射击辅助层
- `Bullet_BDP` 才更像已经落地的弹道宿主层

## 十、下一步阅读重点

接下来应继续细读：

- `Bullet_BDP` 剩余主体
- `IBDPProjectileModule` 及 projectile pipeline 各接口
- `GuidedModule`
- 命中 / 伤害 / 爆炸 / 追踪相关 projectile 模块

目标是继续走通这条链：

- 攻击表达
- 发射投递
- 弹道飞行
- 命中解析
- 效果落地

这样二次重构时，才能知道哪些东西应该留在“攻击执行层”，哪些东西应该留在“弹道宿主层”。
