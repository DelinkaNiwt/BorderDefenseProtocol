---
标题：FormalHostVerb 存档持久化设计分析
版本号: v1.1
更新日期: 2026-04-01
最后修改者: Claude Sonnet 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 分析 BDP formal host verb 存档时"not deep-saved"报错的根因，评估"真正存好读好攻击会话"的架构路径，并给出与现有 BDP 架构原则自洽的最小设计方案。
---

# FormalHostVerb 存档持久化设计分析

## 一、问题现象

在激活芯片后，处于攻击动作（暖机或 burst 中）时存档，读档后出现以下报错：

```
Object with load ID Verb_BDP_FormalHost_..._SubPrimary_Ranged is referenced
(xml node name: verbToUse) but is not deep-saved. This will cause errors during loading.

Could not resolve reference to object with loadID Verb_BDP_FormalHost_...
of type Verse.Verb. Was it compressed away, destroyed, had no ID number,
or not saved/loaded right? curParent=BDP RangedAttackExecution (Job XXXXX)

Could not resolve reference to object with loadID Verb_BDP_FormalHost_...
of type Verse.Verb. Was it compressed away, destroyed, had no ID number,
or not saved/loaded right? curParent=Verse.Stance_Warmup curPathRelToParent=
```

读档后还出现 NullReferenceException，源于 pawn tick（Irwin）。

---

## 二、根因分析

### 2.1 对象引用链

攻击进行中时，以下原版对象持有 BDP formal host verb 的引用：

| 持有者 | 字段 | 说明 |
|--------|------|------|
| `Verse.Stance_Warmup` | `verb` | pawn 进入暖机姿态时记录的 verb |
| `Verse.Job`（BDP 远程攻击 job） | `verbToUse` | JobDriver 发起攻击时传入的 verb |

RimWorld 存档系统序列化这两个字段时，会写入 verb 对象的 `loadID`（形如 `Verb_BDP_FormalHost_{ThingID}_{Slot}_Ranged`）。

### 2.2 verb 对象未在存档树中

`BdpVerb_FormalHostShoot` 实例的生命周期：
1. 在 `TriggerBodyVerbHostManager.EnsureFormalBindings()` 中通过 `new BdpVerb_FormalHostShoot()` 创建
2. 存放在 `TriggerBodyVerbHostManager.bindings` 字典里
3. **`TriggerBodyVerbHostManager` 自身不参与序列化**
4. **`CompTriggerBody.PostExposeData()` 未持久化这个管理器**

结论：verb 对象有稳定 loadID，但不在存档树中。RimWorld 存档系统要求：**任何被其他持久对象引用的对象，自身也必须 deep-saved**，否则存时警告、读时引用解析失败。

### 2.3 PostLoadRecovery 的二级 bug

`AttackExecutionPostLoadRecovery.HasStaleBdpBusyStance()` 的当前实现：

```csharp
private static bool HasStaleBdpBusyStance(Pawn pawn)
{
    if (!(pawn?.stances?.curStance is Stance_Busy busyStance))
        return false;
    return IsBdpFormalHostVerb(busyStance.verb);  // ← 关键
}
```

读档后 `busyStance.verb` 因引用解析失败变为 `null`，`IsBdpFormalHostVerb(null)` 返回 `false`，旧 stance 未被终止，后续 tick 时原版代码解引用 null verb → `NullReferenceException`。

`HasStaleBdpJob` 同时也检查了 `curJob.def == AttackExecutionJobDefs.RangedAttackExecution`，所以 job 侧不受此 bug 影响。

---

## 三、解决路径评估

### 3.1 方案 A：重启会话（当前代码尝试走的方向）

读档后终止旧会话，让下一次 AI 驱动重新发起攻击。

**优点：** 实现最简。
**缺点：**
- 当前代码有上述 bug，stance 未被正确终止导致 NullRef
- 存档时的 "not deep-saved" 警告无法消除
- 玩家体验差：攻击被中断

此方案即使修好 bug，仍然需要解决"verb 未 deep-saved"的存档警告。根因在于 verb 对象不在存档树，**无论选哪条路，都必须先把 verb 对象放入存档树**。

### 3.2 方案 B：真正存好读好（推荐）

让 formal host verb 壳像原版 VerbTracker 里的 verb 一样，成为存档树的一部分。

#### 关键洞察

Formal host verb 已经具备存档的所有前提条件：
- **稳定 loadID**：`BuildFormalHostLoadId()` 基于 ThingID + Slot 生成，确定且唯一
- **标准基类**：继承自 `Verb`，基类 `ExposeData()` 已处理 `verbState`、`loadID` 等核心字段
- **槽位身份**：`BdpFormalVerbHostSlot` 是静态枚举，存档安全

缺失的只有：
1. verb 对象本身未进入存档树
2. `HostResultId` 未被持久化（但它是 verb 和表达系统之间的唯一纽带）
3. 读档后 emission plan 需在消费点惰性重建（`WarmupComplete` 和 `TryCastShot` 中，使用 `HostResultId + currentTarget`）

#### 各业务状态的读档行为

| 存档时状态 | 读档后行为 | 体验质量 |
|-----------|-----------|---------|
| 暖机中（Warmup） | 原版已持久化 `ticksLeft` 和 `verb` 引用；T5 在 `WarmupComplete()` 中惰性重建 emission plan，暖机倒计时结束后正常发射 | ✅ 无缝续接 |
| Burst 进行中 | 原版已持久化 `burstShotsLeft`（剩余发数）和 `currentTarget`；T5 在 `TryCastShot()` 中惰性重建 emission plan，剩余发数自然消耗后 burst 结束 | ✅ 无缝续接 |
| Idle（攻击间隙） | 无状态需恢复 | ✅ 无影响 |


#### 不需要持久化的数据

| 数据 | 原因 |
|------|------|
| `verbProps`、`tool`、`maneuver` | 由 `Refresh(snapshot)` 从表达快照重新注入，是派生值 |
| `pendingVerbEmissionPlan`、`pendingEmissionWindows` | 读档后由 T5 在消费点（`WarmupComplete` / `TryCastShot`）使用已持久化的 `HostResultId + currentTarget` 惰性重建，无需持久化 |
| `AttackInstanceId`、`ResultId`、`SemanticContext` | 每次 cast 重新绑定 |
| `pendingWindowIndex`、`pendingWindowProjectilePlanIndex` | 随 emission plan 一起重置 |

#### 需要持久化的数据

| 数据 | 位置 | 理由 |
|------|------|------|
| verb 壳对象本身 | `TriggerBodyVerbHostManager` → `CompTriggerBody` | 存档树必须能解析此对象的引用 |
| `verbState` | `Verb.ExposeData()`（基类已有） | 暖机续接的关键 |
| `loadID` | `Verb.ExposeData()`（基类已有） | 引用解析的 key |
| `HostResultId` | `BdpVerb_Shoot.ExposeData()` 新增 | T5 在消费点重建 emission plan 的必要参数 |
| `burstShotsLeft` | `Verb.ExposeData()`（基类已有） | 剩余发数，burst 续接时不重置，直接作为发射上限 |
| `currentTarget` | `Verb.ExposeData()`（基类已有） | 攻击目标，T5 重建 emission plan 的必要入参 |

---

## 四、架构自洽性论证

### 4.1 verb 壳持久化符合 BDP 现有原则

BDP 的设计原则：执行壳不持有业务真值，业务真值在表达层。

将 verb 壳放入存档树**不违反此原则**：
- 我们只是让已存在的执行壳对象获得"在存档系统中有家"的能力
- 壳的业务真值（`verbProps`、`ResultId` 等）仍然由表达系统在 `Refresh()` 时注入，持久化后不改变这个流程
- `HostResultId` 是"这个壳对应哪个槽位绑定"的**身份标识**，不是业务计算结果，类比于原版 verb 知道自己挂在哪个 `VerbProperties` 上

### 4.2 与原版 VerbTracker 的对称性

原版流程：武器 → `CompEquippable` → `VerbTracker` → deep-saved → verb 对象在存档树

BDP 流程（修改后）：触发体 → `CompTriggerBody` → `TriggerBodyVerbHostManager` → deep-saved → verb 壳对象在存档树

两者结构完全对称，BDP 只是用自己管理的字典取代了原版 VerbTracker 的列表。这是架构上自然而然的补全，不是为了实现特定结果的强行设计。

### 4.3 读档后绑定恢复流程

```
CompTriggerBody.PostExposeData() [PostLoadInit 阶段]
  → EnsureInternalState()
  → 已保存的 verb 壳从存档恢复（带 loadID、verbState、HostResultId）
  → [新增] 对每个加载的 verb 壳调用 InitializeFormalHost(owner, slot)
  → RefreshProjectedOutputs()
      → Refresh(snapshot)
          → SyncFormalBinding() [重新注入 verbProps 等派生数据]
  → [PostLoadRecovery T4 修复后] 仅终止 verb 为 null 或 Available() = false 的 stance；有效暖机 stance 保留继续
```

---

## 五、设计边界

本方案的边界和保证：

- ✅ 修复 "not deep-saved" 存档警告
- ✅ 修复读档后 NullReferenceException
- ✅ 暖机中存档读档后无缝续接（剩余预热时间由原版持久化；T5 在 `WarmupComplete` 惰性重建 emission plan）
- ✅ Burst 中存档读档后剩余发数续接（`burstShotsLeft` 由原版持久化；T5 在 `TryCastShot` 惰性重建 emission plan，不重置发数）
- ✅ 不持久化 emission plan 对象本身（避免版本脆性；在消费点用 `HostResultId` 重建）
- ✅ 符合 BDP 架构：执行壳不持有业务真值，真值由表达系统重新注入

---

## 六、相关文件索引

| 文件 | 角色 |
|------|------|
| `Core/VerbHosting/TriggerBodyVerbHostManager.cs` | verb 壳的管理器，需新增序列化支持 |
| `Core/VerbHosting/BdpFormalVerbBinding.cs` | 单槽位绑定容器，持有 verb 壳引用 |
| `Core/Verbs/BdpVerb_Shoot.cs` | 需新增 `ExposeData()` 保存 `HostResultId` |
| `Core/Verbs/BdpVerb_FormalHostShoot.cs` | 需在加载后重调 `InitializeFormalHost()` |
| `Core/Verbs/BdpVerb_FormalHostMelee.cs` | 同上 |
| `Core/Trigger/State/CompTriggerBody.Lifecycle.cs` | PostExposeData，需集成 verb 壳序列化 |
| `Core/AttackExecution/AttackExecutionPostLoadRecovery.cs` | 修复 stance 识别 bug |
| `Patches/Patch_Pawn_ExposeData_PostLoadAttackRecovery.cs` | 调用入口，无需修改 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-04-01 | 初版，分析根因、评估方案、给出架构自洽设计 | Claude Sonnet 4.6 |
| v1.1 | 2026-04-01 | 修正暖机和 burst 续接描述；补充 T5（惰性重建 emission plan）；修正 PostLoadRecovery 角色；更新需持久化数据表 | Claude Sonnet 4.6 |
