# 远程攻击接入 Trion 消耗设计（第一版）

## 目的

这份文档回答一个具体问题：

- BDP 远程攻击系统如何以最小改动接入 Trion 消耗

这份文档不回答：

- 具体数值应该配多少
- 近战、Ability、Hediff 是否同步接入
- 本次是否顺带统一所有攻击资源事务

这份设计的目标是：

- 满足“预热前先检查、发射前再原子检查并扣费”的业务需求
- 保持 Trion 真值边界不变
- 保持远程协议与 Verb 会话职责清晰
- 把扣费单位稳定定义为“一轮射击一次”

## 一、现状事实

当前仓库里已经存在三条可直接复用的正式能力链：

### 1. Trion 正式读写链已经存在

- `CompTrion` 是资源真值 owner
- `ITrionCommands` 已经提供：
  - `CanAfford`
  - `TryConsume`
  - `RegisterDrain`
  - `UnregisterDrain`
- 外部系统统一通过 `TrionSurfaceAccess.ResolveCommands(pawn)` 获取正式写口

这意味着：

- 本次不需要新增第二套资源系统
- 本次不需要新增新的资源 owner
- 本次不需要把扣费逻辑塞回 `CompTrion` 之外的真值层

### 2. 表达结果里已经有来源级 Trion 参数

`FormalExpressionResult.Trion` 当前携带 `ExpressionSourceTrionConfig`，其中已有：

- `ActivationCost`
- `UseCost`
- `SustainCost`
- `MinimumRequired`

这意味着：

- 当前架构已经承认“表达来源级的资源语义”
- 远程动作每轮消耗，天然更接近 `UseCost`
- 发动门槛天然更接近 `MinimumRequired`

### 3. 远程协议里已经预留了资源成本挂点

`PrepareRecord` 已经存在：

- `ResourceCost`
- `SkipResourceConsumption`

这意味着：

- 协议层已经预留了“本轮动作需要支付多少资源”的正式位置
- 当前缺的不是数据模型，而是把这条语义接到正式执行链上

## 二、需求抽象

把用户描述翻译成架构语义，本次需求本质上是：

### 1. 两阶段判定

远程动作不是“点下去立刻扣费”，而是：

1. 动作准入检查
2. 发射前最终提交

因此不能只在一个点判断。

### 2. 提交点必须是发射前

预热只是准备阶段，不代表本轮攻击已经成功进入发射事实。

因此：

- 预热开始前只做准入检查
- 真正扣费发生在“即将发射”的边界

### 3. 计费单位是“每轮射击”

这里的一轮，不是单发 projectile，也不是单个 `ProjectileInitPlan`。

这里的一轮应定义为：

- 当前一次正式 `RangedVerbEmissionPlan` 对应的宿主发射会话
- 若该会话是 burst，则整轮只扣一次
- 同一轮里的多发、多窗口、不重复扣费

### 4. 失败必须可见

若 Trion 不足：

- 不能只是内部失败日志
- 需要拒绝动作或终止当前轮
- 需要给玩家明确提示

## 三、方案比较

### 方案 A：直接把全部 Trion 规则塞进 `BdpVerb_Shoot`

做法：

- `TryStartCastOn` 直接读表达结果 Trion
- `TryCastShot` 直接做二次判定和扣费

优点：

- 改动最少
- 落地最快

缺点：

- `Verb` 会直接承担“数值来源解释 + 资源事务 + 用户提示”
- 后续近战或 Ability 若接入同类需求，容易继续复制逻辑
- 不利于保持协议层与宿主层边界清晰

结论：

- 可以做
- 但不是最中性的架构落点

### 方案 B：协议算成本，Verb 只调用轻量 Trion 闸门

做法：

- `Prepare` 阶段负责产出本轮 `ResourceCost`
- 新增一个很薄的远程 Trion 闸门/协调器，只负责：
  - 准入检查
  - 发射前最终提交
  - 失败原因输出
- `BdpVerb_Shoot` 只在两个固定时点调用它

优点：

- 资源数值来源仍归协议/表达层
- 资源真值事务仍归 Trion 子系统
- `Verb` 只负责会话时序，不负责解释成本来源
- 扩展到其他攻击类型时可复用同一模式

缺点：

- 比方案 A 多一个很薄的协调对象

结论：

- 这是本次最推荐方案

### 方案 C：新建统一攻击资源事务层

做法：

- 远程、近战、Ability 都先经过统一攻击资源事务服务

优点：

- 长期统一

缺点：

- 对当前需求过重
- 需要重新划更多边界
- 容易把一个简单补丁演变成系统重构

结论：

- 当前不推荐

## 四、推荐设计

本次采用方案 B。

### 1. 数值来源

本轮远程射击成本来自当前表达结果里的来源级 Trion 配置：

- `UseCost`：本轮发射成本
- `MinimumRequired`：动作准入与发射前最终门槛

这里不新增远程攻击专用配置块。

原因：

- 现有表达结果已经承认来源级 Trion 语义
- 当前需求描述的是“这条攻击来源每轮使用的资源成本”
- 这与 `UseCost` 的语义天然一致

### 2. 成本裁定边界

成本裁定发生在远程协议 `Prepare` 阶段。

协议层职责是：

- 从当前 `FormalExpressionResult.Trion` 读取来源级 Trion 参数
- 生成本轮 `PrepareRecord.ResourceCost`
- 携带本轮最低门槛要求

协议层不负责：

- 直接扣除 Trion
- 弹 UI 提示
- 管理 burst 会话是否已经扣过本轮成本

### 3. 事务提交边界

事务提交仍然直接走：

- `TrionSurfaceAccess.ResolveCommands(pawn)`
- `ITrionCommands.CanAfford(...)`
- `ITrionCommands.TryConsume(...)`

本次不新增：

- CombatBodySession 代理事务入口
- AttackExecution 通用资源事务总线
- Projectile 级扣费入口

### 4. 会话编排边界

`BdpVerb_Shoot` 保持“远程宿主会话驱动者”身份，只负责：

- 在进入 warmup 前发起一次准入检查
- 在本轮第一发真正发射前发起一次最终提交
- 记录“本轮已扣费”状态，避免 burst 内重复扣费
- 在失败时终止当前轮并清理待发射计划

`BdpVerb_Shoot` 不负责：

- 推导成本数值来自哪里
- 改写 Trion 真值规则
- 解释表达条目如何配置

## 五、正式时序

### 阶段 1：动作准入检查

时点：

- `TryStartCastOn`
- 或严格说，在允许进入 warmup 之前

行为：

1. 读取当前轮需要的 Trion 成本与最低门槛
2. 检查当前可用 Trion 是否满足准入
3. 若不满足：
   - 直接拒绝进入本轮动作
   - 不开始 warmup
   - 给出“Trion 不足”提示
4. 若满足：
   - 允许进入 warmup
   - 此时不扣费

### 阶段 2：发射前最终提交

时点：

- `TryCastShot`
- 且只在当前轮第一次真正发射前执行

行为：

1. 再次读取当前轮成本与最低门槛
2. 再次确认是否足够
3. 若不足：
   - 终止本轮发射
   - 清空或终止当前待发射计划
   - 给出“Trion 不足”提示
   - 本轮不扣费
4. 若足够：
   - 调用 `TryConsume(ResourceCost)`
   - 成功后立刻标记“本轮已扣费”
   - 然后才允许本轮第一发射出

### 阶段 3：burst 内推进

时点：

- 当前轮第一发成功提交之后

行为：

- 后续同轮窗口继续走原有 burst 推进
- 不再对每发 projectile 重复扣费
- 直到本轮发射计划结束，才清掉“本轮已扣费”标记

### 阶段 4：异常回收

若出现以下情况：

- 预热中断
- 目标失效
- formal host 会话失效
- projection version 失效
- 尚未进入本轮第一发

则：

- 本轮不扣费
- 因为真正提交点还没有发生

## 六、为什么这不会破坏边界

### 1. 不碰真值 owner

`CompTrion` 仍然只负责资源账本。

### 2. 不把 CombatBodySession 拉进远程攻击扣费

战斗体会话继续管战斗体自己的资源协作，不扩成通用攻击扣费中枢。

### 3. 不把 projectile 变成资源边界

扣费单位是“每轮会话”，不是“每发投射物”。

所以不应把事务提交放进 projectile spawn 或 flight protocol。

### 4. 不新增第二套远程资源配置

继续复用 `FormalExpressionResult.Trion`，避免同一条攻击来源存在两套冲突配置。

## 七、需要补的小型正式对象

推荐新增一个很薄的远程 Trion 闸门结果对象，例如：

- `RangedAttackTrionGateResult`

它只回答：

- 是否允许进入 warmup
- 是否允许提交本轮发射
- 失败原因码是什么
- 需要向玩家展示什么提示

这样做的好处是：

- 不把失败原因散成魔法字符串
- 后续近战或 Ability 若接入，也能复用“原因码 + UI 翻译”模式

这仍然是小补充，不是大框架。

## 八、明确非目标

本次不做：

- 每发 projectile 扣一次
- 每个 `ProjectileInitPlan` 单独扣费
- 把远程攻击统一抽象成新的全局资源事务层
- 把 CombatBodySession 改造成攻击扣费中心
- 顺带统一近战 / Ability / Hediff 的资源生命周期

## 九、最终结论

这次补充从架构上看是小而稳的：

- Trion 真值边界不变
- 表达层配置来源不变
- 远程协议只补成本裁定
- Verb 会话只补两个固定时点的事务调用

所以它不会明显打破系统边界，也不会带来高耦合。

它本质上只是把已经存在的三块正式能力接起来：

1. 表达结果里的来源级 Trion 配置
2. 远程协议 `PrepareRecord.ResourceCost`
3. Trion 正式事务接口 `ITrionCommands`

这是一个合理、克制、可渐进扩展的设计落点。

