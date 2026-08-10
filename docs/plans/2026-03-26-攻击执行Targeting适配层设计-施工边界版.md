# 攻击执行 Targeting 适配层设计（施工边界版）

## 1. 目的

这份文档只钉 1 件事：

- 方案 1 继续成立。
- BDP（Border Defense Protocol，边境防卫协议）自己仍然持有正式执行边界。
- 但手动攻击的目标选择交互，要兼容原版 `Targeter`（目标选择器）、`OnGUI`（目标指示图标绘制）和 `OrderForceTarget`（正式下达强制攻击目标命令）这三段语义。

这不是把正式执行边界退回裸 `Verb`（原版攻击动作对象）。

## 2. 一句话边界

```text
玩家看到什么 -> 尽量像原版
系统真正怎么执行 -> 仍然是 BDP 正式执行链
```

## 3. 当前错误点

现在的问题不是两个，而是同一条链路歪了：

```text
手动按钮
    -> callback Targeting（回调式选目标）
    -> AttackExecution.TryExecute(...)
    -> verb.TryStartCastOn(...)
    -> 只打一发
    -> 图标也只能走通用红准心
```

错在两处：

1. 目标选择没走 `targetingSource`（目标选择源）语义。
2. 点下目标后没走“正式下令”，而是直接“当场打一手”。

## 4. 正确链路

方案 1 下，正确链路应是：

```text
手动按钮
    -> AttackExecutionTargetingSource
        -> Find.Targeter.BeginTargeting(source)
            -> 原版 Targeter
                -> source.OnGUI(...)
                -> source.ValidateTarget(...)
                -> source.OrderForceTarget(...)
                    -> AttackExecution.TryExecute(...)
                        -> BDP 正式执行分发
```

关键点：

- `Targeter` 继续用原版。
- `OnGUI(...)` 继续吃原版语义。
- `OrderForceTarget(...)` 继续保留“正式下令”的入口语义。
- 但 `OrderForceTarget(...)` 落地后，不是回裸 `Verb` 做正式真值，而是回 `AttackExecution`（攻击执行边界）。

## 5. 通用适配层的准确定义

新增：

- `AttackExecutionTargetingSource`（攻击执行目标选择适配源）

它的定位不是：

- 原版执行托管层
- 手动入口临时补丁层

它的定位是：

- BDP formal result（正式表达结果）到原版 `ITargetingSource`（目标选择源接口）的通用翻译层

说白一点：

```text
BDP 这边说：我现在有一个可攻击结果
原版 Targeter 这边说：那你给我一个 targetingSource
适配层的职责就是把这两句话接起来
```

## 6. 这层该借什么，不该借什么

### 可以借的

可以借当前 resolved `Verb`（当前解析到的运行时攻击动作对象）来提供原版交互语义：

- `targetParams`
- `CanHitTarget(...)`
- `ValidateTarget(...)`
- `DrawHighlight(...)`
- `OnGUI(...)`
- `UIIcon`

这样合法目标时，原版攻击图标能自然回来；非法目标时，禁止图标也能自然回来。

### 不能借的

不能把当前 resolved `Verb` 重新抬成正式执行边界。

也就是不能变成：

```text
玩家点目标
    -> source.OrderForceTarget(...)
        -> verb.OrderForceTarget(...)
            -> 原版成了正式真值
```

这会把方案 1 直接打穿。

## 7. `OrderForceTarget(...)` 在方案 1 里的真实含义

在这里，`OrderForceTarget(...)` 要兼容的是“入口语义”，不是“真值归属”。

它应该表示：

```text
玩家已经正式指定目标
现在生成一张正式攻击订单
```

所以它应该做的是：

```text
OrderForceTarget(target)
    -> AttackExecution.TryExecute(request)
```

而不是：

```text
OrderForceTarget(target)
    -> verb.TryStartCastOn(target)
```

也不是：

```text
OrderForceTarget(target)
    -> verb.OrderForceTarget(target)
```

前者会回到“只打一发”。
后者会把正式执行真相重新交回原版。

## 8. 为什么这层必须是通用层

因为它解决的不是“手动按钮怎么画图标”这种局部问题。

它解决的是：

```text
formal result
    如何进入原版 targetingSource 世界
    同时又不破坏 BDP 的正式执行边界
```

所以它应该天然服务：

- 手动入口
- 以后任何需要原版 Targeter 交互语义的入口

第一批只先接手动入口，不代表它是手动特供件。

## 9. 第一版只做哪些东西

第一版只收这几个最小件：

1. `AttackExecutionTargetingSource`
2. `Command_BdpManualEntryTarget`（BDP 手动入口目标选择命令）
3. `AttackDispatchIntent`（攻击派单意图）
4. `AttackExecutionRequest` 增加 `DispatchIntent`
5. `DefaultManualEntryGizmoResolver` 改走 `BeginTargeting(source)`

## 10. 第一版明确不做哪些东西

- 不把正式执行真值退回裸 `Verb`
- 不为了图标问题顺手重做整套 UI
- 不提前做多目标、多段瞄准
- 不为模组体量发明过重的框架

## 11. 最终裁定

这次施工的正式边界就是：

```text
兼容原版 Targeter / OnGUI / OrderForceTarget 语义
但不把正式执行边界交还原版 Verb
```

更完整地说：

```text
原版负责：
- 目标选择交互体验
- 合法性图标反馈
- “我现在是在正式指定攻击目标”这层入口语义

BDP 负责：
- 正式攻击请求
- 正式执行分发
- 远程 / 爆炸 / 近战的后续运行真值
```

只要这条边界不歪，前面两个现象会沿着同一条根因自然收口，而不是靠两个结果补丁去修。
