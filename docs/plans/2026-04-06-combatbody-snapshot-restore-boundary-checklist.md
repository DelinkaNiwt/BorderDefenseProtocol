# CombatBody 快照回滚子系统纯边界清单

**日期：** 2026-04-06

## 1. 目的

这份清单只回答一个问题：

> 只看旧版 `CombatBodySnapshot` 自己，以及外部调用它的最小关系，快照回滚子系统到底负责什么，不负责什么？

这里故意不讨论：

- 战斗体整体激活流程
- `Trigger`
- `Trion`
- 冷却
- 崩解
- 战斗体收尾

只谈：

- 记录
- 恢复

以及与这两件事直接贴边、但不应混成一坨的东西。

---

## 2. 旧版里，快照对象自己有哪些公开入口

旧版 `CombatBodySnapshot` 自己公开了这几个入口：

- `SnapshotAll()`
- `ApplyTransformation()`
- `RemoveAllHediffsExceptExcluded()`
- `RestoreAll()`
- `TakeSnapshotAndActivate()`
- `RestoreSnapshotAndDeactivate()`

其中真正属于“记录/恢复”这对职责核心的，是：

- `SnapshotAll()`
- `RestoreAll()`

其余几个只是旧版把更多事情混进了同一个类里。

---

## 3. 它真正记录了什么

下面这些，都是旧版 `CombatBodySnapshot` 直接从 Pawn 当前状态里取出来，保存进自己内部的东西。

## 3.1 原始衣物本体

记录方式：

- 把 Pawn 当前穿着的原始衣物移出 Pawn
- 放进 `originalApparelContainer`

这意味着它记录的是：

- **原始衣物实体本体**

不是只记一个引用清单或 Def 清单。

---

## 3.2 原始衣物恢复标记

旧版对每件原始衣物还会额外记录：

- `wasLocked`
- `wasForced`

也就是：

- 这件衣物原来是否被锁定
- 这件衣物原来是否被强制穿戴

---

## 3.3 原始背包物本体

记录方式：

- 把 Pawn 当前背包内的原始物品移出 Pawn
- 放进 `originalInventoryContainer`

这意味着它记录的是：

- **原始背包物实体本体**

---

## 3.4 原始背包物恢复标记

旧版对每件原始背包物还会额外记录：

- `wasNotForSale`
- `wasUnpackedCaravan`

---

## 3.5 可恢复 Hediff 基线

旧版会把符合条件的 `Hediff` 记录成 `HediffRecord`。

它记录的字段，严格只有这 13 个：

- `defName`
- `severity`
- `bodyPartDefName`
- `bodyPartIndex`
- `ageTicks`
- `level`
- `isPermanent`
- `painCategory`
- `sourceLabel`
- `sourceDefName`
- `sourceToolLabel`
- `isFresh`
- `lastInjuryDefName`

---

## 3.6 可恢复 Need 基线

旧版只记录：

- `NeedDef -> CurLevel`

没有记录更多生活系统派生状态。

---

## 4. 它不记录什么

下面这些，在已核旧代码里，不是 `CombatBodySnapshot` 记录的内容。

## 4.1 不记录战斗体相位

它不记录：

- Active
- Cooldown
- Collapsing

这些不在 `CombatBodySnapshot` 内。

## 4.2 不记录 `Trion` 真值

它不记录：

- 当前量
- 已占用量
- 冻结状态
- 消耗登记

## 4.3 不记录 `Trigger` 真值

它不记录：

- 槽位状态
- 激活状态
- 装载状态

## 4.4 不记录冷却、崩解、退出原因

它不记录：

- 冷却时长
- 崩解原因
- 是否紧急退出

## 4.5 不记录征召状态

它不记录：

- `Drafted = true/false`

## 4.6 不记录 UI / 投影 / 提示状态

它不记录：

- 图标切换
- 提示文案
- 投影刷新状态

## 4.7 不把 `BDP_CombatBodyActive` 当成原始基线记录

因为它在排除规则里，所以旧版不会把它记进“原始可恢复基线”。

这件事很重要，但它不等于“这个 Hediff 属于快照系统职责”。

这里只能得出一个更窄的事实：

- **它不属于快照对象要恢复的原始基线**

---

## 5. 有一个容易混淆的东西：`combatApparelContainer`

旧版 `CombatBodySnapshot` 里确实还持有：

- `combatApparelContainer`

但它和前面那些“记录下来的原始基线”不是一类东西。

## 5.1 它是什么

它承载的是：

- 战斗体前台装备来源
- 或镜像模式下生成出来的战斗体前台副本

## 5.2 它不是什么

它不是：

- 进入前从 Pawn 身上记录下来的原始基线

所以严格说：

- 旧版快照类里混进了一个**前台替代层容器**
- 这不是“原始基线记录”的一部分

---

## 6. 它真正恢复了什么

只看 `RestoreAll()`，旧版 `CombatBodySnapshot` 真正恢复的是四类东西。

## 6.1 恢复原始衣物

它会：

- 先把当前前台战斗体衣物从 Pawn 身上拿下
- 然后把 `originalApparelContainer` 里的原始衣物穿回去
- 同时恢复：
  - `wasLocked`
  - `wasForced`

## 6.2 恢复原始背包

它会：

- 把 `originalInventoryContainer` 里的原始物品放回 Pawn 背包
- 同时恢复：
  - `wasNotForSale`
  - `wasUnpackedCaravan`

## 6.3 恢复 Need

它会：

- 按 `NeedDef` 找回当前 Need
- 把 `CurLevel` 写回

## 6.4 恢复 Hediff

它会：

- 先移除当前所有非排除 `Hediff`
- 再按快照里的 `HediffRecord` 逐条加回

---

## 7. 它不恢复什么

只看旧版 `CombatBodySnapshot` 自己，下面这些不是它恢复的。

## 7.1 不恢复 `BDP_CombatBodyActive`

旧版对它的处理是：

- 外部流程在 `RestoreAll()` 之前单独移除

不是 `CombatBodySnapshot` 自己恢复/移除。

## 7.2 不恢复火焰清理

旧版灭火发生在外部：

- `CombatBodyOrchestrator.CleanupCombatBodyState()`

不是 `CombatBodySnapshot.RestoreAll()`

## 7.3 不恢复残留 Hediff 最终清理

旧版的 `FinalCleanupResidualHediffs(...)` 是外部在 `RestoreAll()` 之后做的。

不是 `CombatBodySnapshot` 自己做的。

## 7.4 不恢复 Trigger / Trion / 征召 / 冷却

这些都不在 `CombatBodySnapshot.RestoreAll()` 里。

---

## 8. 旧版里，快照对象自己“多做了”的那一件事

严格说，旧版 `CombatBodySnapshot` 不只是“记录/恢复”。

它还多带了一块：

- `RemoveAllHediffsExceptExcluded()`

这件事语义上更像：

- 为战斗体运行态腾出身体层

而不是：

- 记录原始基线
- 恢复原始基线

所以从子系统边界看，它属于：

- **旧版类里混入的相邻动作**

而不属于最纯的“快照回滚核心职责”。

---

## 9. 最窄边界结论

如果把旧版快照回滚子系统收缩到最本质，它其实只做下面这对动作：

```text
记录：
  - 原始衣物本体 + 衣物恢复标记
  - 原始背包本体 + 背包恢复标记
  - 可恢复 Hediff 基线
  - 可恢复 Need 基线

恢复：
  - 原始衣物
  - 原始背包
  - Need
  - Hediff
```

除此之外：

- `combatApparelContainer` 是混进来的前台替代层容器
- `RemoveAllHediffsExceptExcluded()` 是混进来的相邻动作
- `CombatBodyActive`、收尾、征召、Trigger、Trion、冷却，都不属于这个子系统本体

---

## 10. 对新实现的直接约束

因此，新版快照回滚子系统如果要保持边界干净，应当满足：

- 自己只提供：
  - 记录
  - 恢复
- 前台替代层单独建模
- 外部运行态标记单独处理
- 战斗体收尾单独处理
- 不把外部流程责任再灌回快照类

这才是“把旧版事实抽干净后的正确边界”。
