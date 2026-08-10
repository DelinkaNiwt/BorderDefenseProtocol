# Trigger 正式宿主 Verb 重构完成报告

## 1. 本次整改解决了什么

本次整改针对的不是“读档时报错这个结果”，而是 Trigger 参与原版战斗会话时的宿主建模方式错误：

- 旧架构把会被原版 `Stance_Busy / Stance_Warmup` 持有的 combat verb 做成了运行时临时注入对象。
- 原版 `VerbTracker` 只会正式重建 owner 声明出来的 verb，不会替 BDP 重新补建这些临时注入 verb。
- 因此读档后旧动态宿主 verb 丢失 `verbProps`，被原版判定为 `bugged verb after loading`，随后 warmup tick 再访问该 verb 时触发空引用。

本次重构把这条错误链条从源头切断：

- `CompTriggerBody` 现在直接向原版 `VerbTracker` 声明固定的 formal host `VerbProperties`。
- 原版 `VerbTracker` 正式创建并持有这些宿主壳 verb。
- Trigger/Expression 仍然是业务真值来源。
- 宿主层只负责“固定槽位 -> 当前结果”的 binding。

## 2. 现在的架构形态

### 2.1 单一真值边界没有变化

- `TriggerSlotState` 仍然是 Trigger 装载/激活业务真值。
- Expression snapshot 仍然是当前攻击结果真值。
- Verb 不再承载业务真值，只承载原版战斗会话所需的稳定入口身份。

### 2.2 宿主层已经从工厂改成 binding manager

- `TriggerBodyVerbHostManager` 不再 `new Verb`。
- 不再向 `VerbTracker.AllVerbs` 动态 `Add/Remove`。
- 不再维护“runtime host instance -> real verb”的对象图。
- 现在只维护固定 `BdpFormalVerbHostSlot` 与 `BdpFormalVerbBindingState`。

### 2.3 原版生命周期已对齐

- `CompTriggerBody` 现在声明固定 formal host `VerbProperties`。
- 原版 `VerbTracker` 会正式创建 `BdpVerb_FormalHostShoot / BdpVerb_FormalHostMelee`。
- binding 刷新时只同步当前结果对应的最小战斗表面。
- 自动远程入口直接返回正式宿主壳 verb，不再经过 proxy verb。

## 3. 已落地改动

- 新增 `BdpFormalVerbHostSlot`
- 新增 `BdpFormalVerbBinding`
- 新增 `BdpFormalVerbBindingState`
- 新增 `BdpVerb_FormalHostShoot`
- 新增 `BdpVerb_FormalHostMelee`
- 新增 `CompTriggerBody.FormalHosts.cs`
- 重写 `TriggerBodyVerbHostManager` 为 binding manager
- 重写 `VerbHostSurfaceAccess` 为 formal binding/formal shell 读取口
- 重写自动远程与自动近战入口对 formal binding 的读取
- 删除 `VerbHostAutoProxyVerb`
- 删除旧动态宿主残件 `VerbHostInstance / VerbHostBuildSpec / VerbHostSlot`

## 4. 证据

### 4.1 烟雾测试

已通过：

- `TriggerSingleTruthSmokeTests.ps1`
- `FormalHostVerbSmokeTests.ps1`
- `ComboDefinitionBoundarySmokeTests.ps1`
- `DefaultBurstParitySmokeTests.ps1`
- `RangedProtocolBoundarySmokeTests.ps1`

### 4.2 构建

已通过：

- `dotnet msbuild Source/BDP/BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`

## 5. 仍需游戏内验证

本地静态验证已经说明：

- 动态注入宿主 verb 路线已删除
- proxy verb 路线已删除
- 原版 `VerbTracker` 已有稳定 formal host 壳可重建

但最终必须由游戏内 warmup 存读档场景确认：

1. 装备 Trigger 武器并进入自动远程 warmup
2. 在 warmup 中存档
3. 立即读档

预期：

- 不再出现 `had a bugged verb after loading`
- 不再出现 `Stance_Warmup.AimDir()` 那条空引用链
- 若当前 Trigger 真值与 expression 结果合法，原版 warmup 会继续推进

## 6. 结论

这次整改不是给结果补丁，而是把 Trigger 宿主层重新放回原版正式生命周期中：

- 真值仍在 Trigger / Expression
- 原版会话入口回到原版 `VerbTracker`
- 宿主层只做稳定 binding

如果游戏内 warmup 存读档验证通过，那么这次报错在预期中就不该再出现。
