# 正式远程宿主参数缓存失效修复设计

## 目标

修复正式远程宿主复用同一个原版 `Verb`（武器行为对象）时，切换冲锋枪、左轮等武装构型后仍使用旧连发间隔缓存的问题。

## 已确认根因

原版 `Verb.TicksBetweenBurstShots`（连发间隔解析值）和 `Verb.BurstShotCount`（连发数量解析值）使用私有惰性缓存。BDP 的 `BdpVerb_FormalHostShoot.SyncFormalBinding` 在绑定变化时会调用 `Reset()`，随后替换 `verbProps`，但原版 `Verb.Reset()` 不清理这两个缓存。于是同一 formal host（正式宿主壳）可能把上一种构型的参数带到下一种构型。

日志已确认左轮出现 `declaredInterval=15, resolvedInterval=5`，而冲锋枪为 `5/5`；双持左轮两侧因此按旧的 5 Tick（游戏时钟单位）调度。

## 修复方案

在 `BdpVerb_FormalHostShoot.SyncFormalBinding` 的绑定切换边界清理原版两个缓存，然后再写入新的 `verbProps`。清理通过已有 Harmony（运行时补丁库）依赖提供的字段访问能力集中封装，不改双持交替计划、不修改 Def 参数、不复制原版射击流程。

清理范围包含两个缓存：连发间隔和连发数量。当前症状只暴露了间隔缓存，但两者都依赖可变的 `verbProps`，必须在同一边界保持一致。

## 验证

先用静态冒烟测试证明修复代码存在并位于 formal host 绑定切换边界，再编译 Core/Content。最终需要在游戏内按两种切换顺序测试单双持，并确认日志满足：

```text
冲锋枪：declaredInterval=5,  resolvedInterval=5
左轮：  declaredInterval=15, resolvedInterval=15
```
