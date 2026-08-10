# CombatBody 快照/回滚系统细粒度逻辑推演报告

**日期：** 2026-04-06

## 0. 这份报告解决什么问题

上一份推演报告回答的是“大方向是否等价”。

这份报告改为回答更细的问题：

> 把旧 BDP 的真实执行顺序一行一行摊开，再把新计划实施后的预期执行顺序一行一行摊开，在同一输入条件下，两个流程每一步会把 Pawn 改成什么样？最终结果能不能严格对齐？

本报告的判断标准不是“架构像不像”，而是：

- 同一输入下
- 到每个关键步骤后
- Pawn 身上的前台层、托管层、身体层、需求层
- 是否会落到同样的结果

---

## 1. 推演前提

## 1.1 旧 BDP 已确认的代码事实

本报告只基于这些旧代码事实：

- 激活主流程：`CombatBodyOrchestrator.Activate()`
- 退出主流程：`CombatBodyOrchestrator.Deactivate()`
- 快照核心：`CombatBodySnapshot`
- 排除规则：`BDPSnapshotConfigDef` + `BDPSnapshotConfig.xml`
- 托管防腐：`Patch_CompRottable_PreventRotInSnapshot`
- 前台预设初始化：`Gene_TrionGland.InitializeCombatApparel()`

## 1.2 新 BDP 的预期实现前提

本次推演不是对现状推演，而是对“计划实施完成后”推演。

也就是默认这些都已经实现：

- `CombatBodySnapshotState`
- `CombatBodySnapshotService`
- `CombatBodySnapshotPolicy`
- `CombatBodyFrontState`
- `PawnCombatBodyBridge` 已接回快照与前台替代层
- `Preset / MirrorOriginal` 两种前台模式都已补回
- 托管防腐补丁已补回

---

## 2. 旧 BDP 的真实激活流程

下面不是概括，是按旧代码里的真实顺序展开。

## 2.1 激活入口顺序

旧代码入口是：

```text
CombatBodyOrchestrator.Activate()
  1. ValidateActivation(...)
  2. AllocateTrion(...)
  3. ApplyTransformation(...)
  4. ActivateChips(...)
  5. RegisterMaintenance(...)
  6. runtime.State.TransitionToActive(...)
```

## 2.2 其中第 3 步 `ApplyTransformation(...)` 的真实内部顺序

```text
ApplyTransformation(pawn, snapshot)
  3.1 snapshot.SnapshotAll()
      3.1.1 SnapshotHediffs()
      3.1.2 SnapshotNeeds()
  3.2 snapshot.ApplyTransformation()
      3.2.1 TransferApparelToCombat()
      3.2.2 TransferInventoryToSnapshot()
  3.3 snapshot.RemoveAllHediffsExceptExcluded()
  3.4 pawn.health.AddHediff(BDP_CombatBodyActive)
  3.5 强制征召 Drafted = true
```

这意味着旧 BDP 的真实激活语义是：

### 激活前记录的东西

- 所有**非排除** Hediff
- 所有 Need 当前值

### 激活时被收起的东西

- 当前穿着的全部原始衣物
- 当前背包里的全部原始物品

### 激活时被顶到前台的东西

- `combatApparelContainer` 中已有的预设衣物
- 或根据 `MirrorOriginal` 临时生成的一组镜像副本

### 激活时被清走的东西

- 所有**非排除** Hediff

### 激活时额外加上的东西

- `BDP_CombatBodyActive`

---

## 3. 旧 BDP 的真实退出流程

## 3.1 退出入口顺序

旧代码入口是：

```text
CombatBodyOrchestrator.Deactivate()
  1. （紧急退出时）先做紧急脱离特效 / 传送 / 销毁芯片 / 解除征召
  2. CleanupCombatBodyState(...)
  3. 安全移除 BDP_CombatBodyActive
  4. ReleaseTriggerSystem(...)
  5. UnregisterMaintenance(...)
  6. snapshot.RestoreAll()
  7. FinalCleanupResidualHediffs(...)
  8. （紧急退出时）Trion枯竭 + BDP_Exhaustion
  9. runtime.State 转入 Cooldown
```

## 3.2 其中第 2 步 `CleanupCombatBodyState(...)` 的真实内部顺序

```text
CleanupCombatBodyState(...)
  2.1 ExtinguishFire()
  2.2 snapshot.RemoveAllHediffsExceptExcluded()
```

也就是说，旧 BDP 在恢复快照之前，会先：

- 灭火
- 把当前战斗期的非排除 Hediff 再清一遍

## 3.3 其中第 6 步 `snapshot.RestoreAll()` 的真实内部顺序

```text
RestoreAll()
  6.1 RestoreApparelFromSnapshot()
  6.2 RestoreInventoryFromSnapshot()
  6.3 RestoreNeeds()
  6.4 RestoreHediffs()
```

这四步顺序在旧版是明确写死的，不是模糊的。

---

## 4. 旧 BDP 在每一步后，Pawn 变成什么状态

为了方便对照，下面只看和快照/回滚系统直接相关的 6 个面：

- `A`：Pawn 前台衣物
- `B`：原始衣物托管容器
- `C`：原始背包托管容器
- `D`：战斗体前台衣物来源
- `E`：Pawn 当前非排除 Hediff 集合
- `F`：Need 当前值

## 4.1 正常激活，`Preset` 模式

### 旧 BDP 的逐步状态变化

```text
初始
A = 原始衣物在 Pawn 身上
B = 空
C = 空
D = combatApparelContainer 里已预存预设战斗体衣物
E = 进入前非排除 Hediff 集合
F = 进入前 Need 值

执行 3.1 SnapshotAll 后
A = 还在 Pawn 身上
B = 空
C = 空
D = 仍有预设战斗体衣物
E = Pawn 当前没变，但快照里已记录其副本
F = Pawn 当前没变，但快照里已记录其值

执行 3.2.1 TransferApparelToCombat 后
A = 原始衣物已从 Pawn 身上移除，战斗体衣物穿上
B = 原始衣物已进入 originalApparelContainer
C = 空
D = combatApparelContainer 被取空并穿到 Pawn 前台
E = 还没清
F = 还没变

执行 3.2.2 TransferInventoryToSnapshot 后
A = 战斗体衣物仍在前台
B = 原始衣物在托管容器
C = 原始背包物进入 originalInventoryContainer
D = 已在 Pawn 前台
E = 还没清
F = 还没变

执行 3.3 RemoveAllHediffsExceptExcluded 后
E = 当前只剩排除项

执行 3.4 AddHediff(BDP_CombatBodyActive) 后
E = 排除项 + BDP_CombatBodyActive
```

### 旧 BDP 的最终激活结果

```text
Pawn 前台看到：战斗体衣物
原始衣物：被托管
原始背包：被托管
非排除 Hediff：被清掉
Need：前台不变，但快照已记住旧值
```

---

## 5. 新计划实施后的预期激活流程

按当前计划，真正承接快照/回滚的是宿主桥。

预期入口会是：

```text
CombatBodySession 激活事务
  -> rawCombatBodyService.TryEnterActive(...)
     -> host.ApplyCombatBodyTransformation()
        -> PawnCombatBodyBridge.ApplyCombatBodyTransformation()
```

在这一步里，计划要求接回的内部顺序是：

```text
1. CaptureForActivation(pawn)
2. 收起原始前台层
3. 建立 CombatBodyFrontState 前台替代层
4. 清掉非排除 Hediff
5. 加上旧版等价的战斗体运行标记（这里当前计划文本没写死，但旧版确实有）
```

如果计划按“与旧版表象等价”落实，那么 `Preset` 模式下预期状态变化应是：

```text
初始
A = 原始衣物在 Pawn 身上
B = SnapshotState.originalApparelContainer 为空
C = SnapshotState.originalInventoryContainer 为空
D = FrontPreset 定义可解析出预设战斗体衣物
E = 进入前非排除 Hediff 集合
F = 进入前 Need 值

执行 CaptureForActivation 后
A = 还在 Pawn 身上
B = 若捕获与托管分离，则还空；若捕获时顺手托管，则原始衣物开始离场
C = 同上
E = SnapshotState 已记住旧 Hediff
F = SnapshotState 已记住旧 Need

执行“收起原始前台层”后
A = 原始衣物离开 Pawn
B = 原始衣物进入托管容器
C = 原始背包进入托管容器

执行“建立 FrontState 前台替代层”后
A = Pawn 前台变成战斗体衣物
D = FrontState 持有这次战斗体前台层

执行“清理非排除 Hediff”后
E = 当前只剩排除项

执行“加运行标记”后
E = 排除项 + 战斗体运行标记
```

### 这一段和旧 BDP 是否能对齐

**能对齐。**

但前提是下面这件事必须补进实现口径：

- 新实现也必须保留旧版“清理完非排除 Hediff 后，再加战斗体运行标记”这一步

如果这一步漏掉，就不算严格等价。

---

## 6. 旧版与新计划在退出阶段的逐步对照

## 6.1 旧 BDP 的逐步退出状态变化

假设当前 Pawn 正处于战斗体中：

```text
退出前
A = 战斗体前台衣物在 Pawn 身上
B = 原始衣物在托管容器
C = 原始背包在托管容器
E = 排除项 + BDP_CombatBodyActive + 战斗期新增伤口/残留
F = 战斗期间已经变化过
```

### 执行 CleanupCombatBodyState 后

```text
火焰类 Hediff = 被移除
战斗期非排除 Hediff = 被清掉
E = 只剩排除项 + BDP_CombatBodyActive
```

### 执行“安全移除 BDP_CombatBodyActive”后

```text
E = 只剩排除项
```

### 执行 RestoreAll 第 1 步：RestoreApparelFromSnapshot 后

```text
A = 战斗体前台衣物离场
A = 原始衣物重新穿回 Pawn
B = 原始衣物托管容器变空

同时：
- MirrorOriginal 模式：前台副本被销毁
- Preset 模式：战斗体衣物被放回 combatApparelContainer
```

### 执行 RestoreAll 第 2 步：RestoreInventoryFromSnapshot 后

```text
C = 原始背包托管容器变空
Pawn 背包 = 回到进入前清单
并恢复：
- wasNotForSale
- wasUnpackedCaravan
```

### 执行 RestoreAll 第 3 步：RestoreNeeds 后

```text
F = 回到进入前 Need 值
```

### 执行 RestoreAll 第 4 步：RestoreHediffs 后

```text
E = 先再清一次所有非排除 Hediff
E = 再把快照中的 Hediff 逐条加回
```

### 执行 FinalCleanupResidualHediffs 后

```text
如果还有“不在快照里、但又不是排除项”的残留 Hediff
=> 再清掉
```

这一步很关键：

- 它不是快照主恢复的一部分
- 但它确实是旧版退出结果的一部分

---

## 6.2 新计划实施后的预期退出流程

按计划，宿主恢复应是：

```text
RestoreFromCombatBody()
  1. 撤掉 FrontState 前台替代层
  2. 恢复 SnapshotState 原始基线
  3. 清空本次 snapshot/front
```

如果要与旧 BDP **严格对齐**，那这个“恢复 SnapshotState 原始基线”在落地时必须细化成：

```text
2.1 清理战斗期非排除 Hediff
2.2 安全移除战斗体运行标记
2.3 恢复原始衣物
2.4 恢复原始背包
2.5 恢复 Need
2.6 恢复 Hediff
2.7 做一轮残留 Hediff 最终清理
```

### 为什么必须细化到这一级

因为旧版的最终结果不是简单一句“恢复快照”。

旧版真实结果包含两层：

- **恢复快照里的东西**
- **再清掉不该残留的战斗期东西**

如果新计划只做前者，不做后者，就不能说和旧版结果严格一致。

---

## 7. 关键对比：当前计划里已经等价的部分

以下部分，按当前计划文本，已经足以推导出和旧版结果一致：

### 7.1 原始衣物托管与恢复

旧版：

- 记录 `wasLocked`
- 记录 `wasForced`
- 收起原衣物
- 退出时按原标记穿回

新计划：

- 明确写入同两项恢复标记
- 明确要求对称托管与恢复

**结论：这一块可以做到严格等价。**

### 7.2 原始背包托管与恢复

旧版：

- 记录 `wasNotForSale`
- 记录 `wasUnpackedCaravan`

新计划：

- 明确要求迁移同两项恢复标记

**结论：这一块可以做到严格等价。**

### 7.3 Hediff 排除规则

旧版排除：

- `PsychicAmplifier`
- `MechlinkImplant`
- `BDP_CombatBodyActive`
- `Verse.Hediff_Psylink`
- `Verse.Hediff_Mechlink`

新计划：

- 第一版明确迁移同一组事实

**结论：这一块可以做到严格等价。**

### 7.4 Need 恢复

旧版：

- 对每个 NeedDef 记录 `CurLevel`
- 退出时逐个写回

新计划：

- 明确要求只记录旧版实际保存过的 Need 当前值

**结论：这一块可以做到严格等价。**

### 7.5 托管期防腐

旧版：

- 只要 `holdingOwner.Owner is CombatBodySnapshot`
- `CompRottable.Active` 就被压成 `false`

新计划：

- 明确补回“只面向快照托管”的最小防腐补丁

**结论：这一块可以做到严格等价。**

---

## 8. 关键对比：当前计划里还没有写死、但旧版确实有的步骤

这部分才是这次细粒度推演最重要的发现。

## 8.1 战斗体运行标记的显式添加/移除

旧版真实流程里明确存在：

```text
激活时：
  RemoveAllHediffsExceptExcluded()
  -> AddHediff(BDP_CombatBodyActive)

退出时：
  CleanupCombatBodyState()
  -> RemoveHediff(BDP_CombatBodyActive)
  -> RestoreAll()
```

而当前计划文本里：

- 写了排除规则里有 `BDP_CombatBodyActive`
- 但没有把“激活时加、退出时单独安全移除”作为独立步骤写死

### 推演结论

**如果不把这一步补进计划，就不能说和旧版严格等价。**

原因很直接：

- 旧版把它当成“战斗体运行标记”
- 不是普通快照内容
- 也不是普通清理内容

它有单独时序。

## 8.2 旧版退出前的“灭火”

旧版真实存在：

```text
ExtinguishFire()
```

当前计划文本里没有单独写这个步骤。

### 推演结论

**如果你要求“相同条件下结果一致”，这一步也应该补回。**

因为旧版在退出前会把火焰类 Hediff 清掉。
新实现如果不做，玩家看到的退出后结果就会分叉。

## 8.3 旧版退出后的最终残留清理

旧版真实存在：

```text
FinalCleanupResidualHediffs()
```

它的语义不是“再恢复一次”，而是：

- 把不在快照里
- 又不是排除项
- 但还残留在 Pawn 身上的 Hediff

再清一遍。

当前计划文本里，这一步没有被单独写死。

### 推演结论

**如果不补这一步，结果一致性不能打满。**

## 8.4 旧版恢复顺序被写死为：

```text
Apparel -> Inventory -> Needs -> Hediffs
```

而当前设计/计划文本里，存在过不同说法：

- 有的地方说先背包再衣物
- 有的地方说身体和需求的先后还未完全钉死

### 推演结论

**如果你要“按旧逻辑结果等价”，恢复顺序必须统一写死。**

最稳妥的口径就是直接对齐旧版：

```text
1. 前台替代层撤场
2. 清理战斗期非排除 Hediff
3. 安全移除战斗体运行标记
4. 恢复原始衣物
5. 恢复原始背包
6. 恢复 Need
7. 恢复 Hediff
8. 最终残留清理
9. 清空 snapshot/front
```

---

## 9. 逐场景最终裁定

## 场景 A：正常激活，`Preset`

### 裁定

**在补齐运行标记时序后，可严格等价。**

## 场景 B：正常激活，`MirrorOriginal`

### 裁定

**在镜像副本继续保留“复制外观、去品质、退出销毁”后，可严格等价。**

## 场景 C：正常退出

### 裁定

**当前计划方向正确，但要补写 3 个旧版真实步骤后，才能说严格等价：**

- 战斗体运行标记单独移除
- 最终残留 Hediff 清理
- 固定恢复顺序

## 场景 D：紧急退出

### 裁定

**快照/回滚部分可以等价，但前提是旧版退出前清理链也得保留：**

- 灭火
- 清战斗期非排除 Hediff
- 单独移除运行标记
- 再做恢复

## 场景 E：读档后退出

### 裁定

**如果 `snapshotState` 和 `frontState` 都跟宿主位存档，并在读档后重建桥接链，则可以等价。**

---

## 10. 最终结论

现在可以给一个比上一份报告更严格的结论：

### 10.1 当前计划“方向上”是否能做出旧版等价表象

**可以。**

### 10.2 当前计划“按现有文字原样执行”是否已经足够保证严格等价

**还不够。**

不是方向错了，而是还差几处旧版真实步骤没有在计划里被写死：

- `BDP_CombatBodyActive` 的显式加/显式移除时序
- 退出前灭火
- 恢复后残留 Hediff 最终清理
- 恢复顺序完全钉死为旧版顺序

### 10.3 所以最后判断是什么

```text
结论不是“现在这份计划已经 100% 能保证和旧版同结果”。
更准确的结论是：

这份计划已经找对了架构落点，
但若想把“表象等价”从大方向成立推进到“逐流程结果严格成立”，
还需要把旧版这 4 个关键步骤正式补进实施计划。
```

---

## 11. 建议的下一步

最合适的下一步不是立刻写代码，而是先把实施计划补一刀，补成“严格等价版”：

### 必补 1

在激活流程里写死：

```text
清非排除 Hediff -> 再加战斗体运行标记
```

### 必补 2

在退出流程里写死：

```text
灭火 -> 清战斗期非排除 Hediff -> 单独移除运行标记
```

### 必补 3

把恢复顺序写死为：

```text
恢复衣物 -> 恢复背包 -> 恢复 Need -> 恢复 Hediff
```

### 必补 4

在恢复末尾补：

```text
FinalCleanupResidualHediffs
```

只要把这四刀补上，再做一次同样的细粒度推演，结论就能从：

- “大方向等价”

提升为：

- “逐步骤结果也可判定为等价”
