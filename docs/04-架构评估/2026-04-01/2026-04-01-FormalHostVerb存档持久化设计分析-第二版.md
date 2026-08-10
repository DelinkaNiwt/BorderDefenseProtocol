---
标题：FormalHostVerb 存档持久化设计分析
版本号: v2.0
更新日期: 2026-04-01
最后修改者: Codex GPT-5
标签: [文档][用户已确认方向][已完成][未锁定]
摘要: 在 v1.1 基础上修正 FormalHostVerb 存档持久化方案中过于乐观的判断，明确区分“让壳进入存档树”“保住读档后第一次绑定不清空状态”“真正做到 burst 中段无缝续接”三件事，并给出可施工的 v2 设计边界。
---

# FormalHostVerb 存档持久化设计分析

## 一、需求方向

这次需求的概念方向不是“读档后别报错”，而是：

1. BDP 与原版攻击入口仍然彻底分家。
2. 原版触发体本体攻击自然保留，不被 BDP 替换。
3. BDP 表达系统产出的默认主攻击继续负责自动攻击。
4. BDP 既然是一套独立攻击体系，那么运行中的攻击会话也应属于这套体系自身，而不是一到存读档边界就掉回残缺状态。
5. 因此，目标是让 BDP formal host 会话像原版武器 verb 会话一样，能够跨存档连续。

---

## 二、v1.1 方案的有效结论

v1.1 里有三点判断是成立的，仍应保留：

### 2.1 根因判断成立

当前报错的直接根因仍然是：

- `Stance_Warmup.verb`
- `Job.verbToUse`

都在存档时引用了 BDP internal formal host verb，但这些 verb 壳对象本身不在 deep-save 树里，所以出现：

- `is referenced but is not deep-saved`
- `Could not resolve reference to object with loadID ...`

这部分结论不需要推翻。

### 2.2 formal host 壳必须进入存档树

如果要走“像原版一样无缝续接”这条路，formal host 壳就不能再只是 `TriggerBodyVerbHostManager` 内部内存对象。

它必须像原版 `CompEquippable -> VerbTracker -> verbs` 那样，在 BDP 自己的持久化链里有明确归属。

### 2.3 `HostResultId` 必须持久化

`HostResultId` 是 formal host 壳与表达结果之间的最小稳定纽带。

读档后：

- 重新注入 `verbProps`
- 重建发射计划
- 校验当前结果是否仍然成立

都离不开它。

---

## 三、v1.1 方案的关键缺口

v1.1 最大的问题不是方向错，而是把三件不同难度的事混成了一件：

1. 让 verb 壳对象能被存好读好
2. 让读档后第一次 rebind 不把已恢复状态清空
3. 让 burst 中段真正无缝续接

其中 1 相对直接，2 和 3 明显更难。

### 3.1 读档后第一次 `Refresh()` 会把已恢复 verb 状态重置掉

现代码里 [BdpVerb_FormalHostShoot.cs](/C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs) 的 `SyncFormalBinding()` 会在 binding 身份或声明面变化时调用 `Reset()`。

而原版 [Verb.Reset](/C:/NiwtGames/Tools/Rimworld/RimSearcher/Source/Verse/Verb.cs) 会清掉：

- `state`
- `currentTarget`
- `burstShotsLeft`
- `ticksToNextBurstShot`

这意味着：

- 即使 verb 壳对象本身读回来了
- 即使 `loadID`、`state`、`currentTarget` 都恢复了

只要读档后的第一次 `RefreshProjectedOutputs() -> verbHostManager.Refresh() -> SyncFormalBinding()` 触发 `Reset()`，这些恢复出来的运行态就会被立刻抹掉。

所以 v1.1 所说的“恢复壳后再 Refresh 即可保住状态”并不成立。

### 3.2 `HostResultId + currentTarget` 不足以实现 burst 中段无缝续接

现代码里 [BdpVerb_Shoot.cs](/C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source/BDP/Core/Verbs/BdpVerb_Shoot.cs) 真正消费的是：

- `pendingEmissionWindows`
- `pendingWindowIndex`
- `pendingWindowProjectilePlanIndex`

也就是说，burst 会话不只是“剩余几发”这么简单，还包含“已经消费到发射计划的哪个位置”。

如果只保存：

- `HostResultId`
- `currentTarget`
- `burstShotsLeft`

读档后最多只能重新知道“该打哪一套计划、还剩多少发”。  
但你并不知道原来已经消耗到 plan 的哪个游标。

结果就会出现两种错误之一：

1. 从头重放当前 burst 的前半段
2. 强行用剩余发数裁剪，但游标与计划错位

所以 v1.1 对“burst 中段无缝续接”的判断是过度乐观的。

### 3.3 `WarmupComplete()` 示例代码和现实现不兼容

v1.1 把 `WarmupComplete()` 简化成先惰性重建，再 `base.WarmupComplete()`。

但现代码的 [BdpVerb_Shoot.cs](/C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source/BDP/Core/Verbs/BdpVerb_Shoot.cs) 已经自己重写了 `WarmupComplete()`，里面包含：

- `ResolveRemainingWindowCount()`
- BDP 自己的状态推进
- 日志
- 射击经验逻辑

直接替换成 `base.WarmupComplete()` 会把现有语义绕掉，因此不能按 v1.1 示例原样落地。

---

## 四、v2 的正确拆分

v2 不再把“能读档不报错”和“burst 中段完全无缝续接”当成同一层问题，而是拆成三层：

### 4.1 P1：formal host 壳进入存档树

目标：

- 消除 `not deep-saved`
- 让 `Stance_Warmup.verb` / `Job.verbToUse` 的引用能解析到真实壳对象

这是无缝续接的必要条件，但不是充分条件。

### 4.2 P2：读档后的第一次 binding 注入必须保住已恢复状态

目标：

- 允许“同一槽位、同一结果、同一会话”的壳在第一次 post-load rebind 时保留 `state/currentTarget/burstShotsLeft`
- 仅在结果身份真的变了、声明面真的失效了时，才重置

这一步解决的是“明明存回来了，却在第一次 Refresh 时被自己清掉”。

### 4.3 P3：burst 中段无缝续接必须补齐会话游标

目标：

- 不只知道“这是谁”
- 还知道“已经打到哪”

因此必须把 burst 消费游标作为正式持久化状态的一部分，而不能只靠 `HostResultId` 猜。

---

## 五、v2 设计结论

### 5.1 需要持久化的数据

#### A. 壳对象基础身份

- formal host 壳对象本身
- `loadID`
- `state`
- `currentTarget`
- `currentDestination`
- `burstShotsLeft`
- `ticksToNextBurstShot`

这些大多由原版 `Verb.ExposeData()` 已经负责，只要对象本身进入存档树即可。

#### B. BDP 自身最小会话身份

- `HostResultId`

它负责把壳重新接回表达结果。

#### C. burst 精确续接所需游标

- `pendingWindowIndex`
- `pendingWindowProjectilePlanIndex`

必要时可附带：

- `pendingEmissionConsumedCount`

但这项更多是诊断辅助，不是 gameplay 真值的必须项。

### 5.2 不需要持久化的数据

- `verbProps`
- `tool`
- `maneuver`
- `pendingVerbEmissionPlan`
- `pendingEmissionWindows`
- `SemanticContext`

这些仍然应由表达系统和执行组装链在读档后重建，而不是整包硬存。

### 5.3 读档后的恢复规则

#### 暖机续接

当满足以下条件时允许继续：

- formal host 壳引用成功解析
- `HostResultId` 仍能命中当前表达结果
- 第一次 rebind 未被误重置

然后沿用原版 `Stance_Warmup` 继续倒计时，暖机结束时再惰性重建 emission plan。

#### burst 中段续接

当满足以下条件时允许继续：

- formal host 壳引用成功解析
- `HostResultId` 仍有效
- burst 游标已恢复
- 重新生成的 emission plan 与当前表达结果兼容

然后将新 plan 快进到已恢复的 cursor 位置，再继续剩余 burst。

#### 降级终止

只要任一关键条件失败，就不硬续：

- `HostResultId` 不存在
- 对应表达结果已失效
- binding 已切换到别的结果
- burst cursor 不合法

此时终止旧 BDP 会话，避免错续。

---

## 六、v2 对现有架构的要求

### 6.1 新要求一：manager 不只是“拥有壳”，还要“暴露壳的持久化入口”

[TriggerBodyVerbHostManager.cs](/C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol/Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs) 现在只负责：

- 创建壳
- 刷 binding
- tick 壳

v2 后它还要负责：

- 暴露 deep-save 集合
- 在 PostLoad 阶段按槽位重建 bindings

### 6.2 新要求二：formal host 的第一次 post-load rebind 要有“保状态”握手

不能再让 `RestoreShellsPostLoad()` 之后第一次 `Refresh()` 直接按现逻辑 `Reset()`。

必须新增一条明确语义：

- 这是“同壳、同结果、同会话”的 post-load 恢复
- 本轮只重注入派生声明面，不清空已恢复运行态

### 6.3 新要求三：`BdpVerb_Shoot` 要区分“计划内容”与“计划消费位置”

计划内容仍然可以重建。  
但消费位置不是派生值，必须持久化。

这就是 v1.1 漏掉的核心点。

---

## 七、可行度评估

### 7.1 做到“暖机中无缝续接”

可行度：高

因为暖机中真正要保住的是：

- stance 对 verb 的引用
- verb 的基础状态
- 读档后的第一次 rebind 不误重置

这条链在 v2 下是收敛的。

### 7.2 做到“burst 中段完全无缝续接”

可行度：中

原因不是做不到，而是要补齐 cursor 持久化与 plan 快进逻辑。

也就是说：

- 不是不能做
- 但绝不是 v1.1 写的那样“顺手惰性重建一下 plan 就够了”

### 7.3 整体路线判断

结论：

- 方向可行
- 需要修正施工方案
- 不应直接按 v1.1 开工

---

## 八、v2 最终结论

FormalHostVerb 持久化这条路可以走，而且值得走。  
但 v2 必须明确承认：

1. 把壳对象放进存档树，只解决“引用能不能读回来”
2. 保住第一次 post-load rebind，不清状态，才解决“读回来后会不会立刻被自己清掉”
3. 补齐 burst cursor，才真正谈得上“中段无缝续接”

所以 v2 的核心主张是：

**把“对象持久化”“首次重绑保态”“burst 游标续接”拆成三层设计，并按这个顺序实施。**

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-04-01 | 初版，分析根因、评估方案、给出架构自洽设计 | Claude Sonnet 4.6 |
| v1.1 | 2026-04-01 | 修正暖机和 burst 续接描述；补充 T5（惰性重建 emission plan） | Claude Sonnet 4.6 |
| v2.0 | 2026-04-01 | 拆分对象持久化、首次重绑保态、burst 游标续接三层问题；修正 v1.1 的关键乐观假设 | Codex GPT-5 |
