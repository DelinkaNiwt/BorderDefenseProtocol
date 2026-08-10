# 2026-04-02 新 BDP 战斗体-触发器-Trion 接线设计 v3

## 1. 文档目的

本文档只做一件事：

- 在 **不改变旧 BDP 已实现业务语义** 的前提下，
- 用 **新 BDP 当前架构的边界和设施**，
- 把 `战斗体系统`、`触发器系统`、`Trion 资源系统` 接起来。

本文档分两层写：

- `旧 BDP 业务基线`
  - 只写旧模组代码里已经实现过的规则。
- `新 BDP 架构映射`
  - 只写“这些旧规则在新架构里该落到哪里”。
  - 这部分是实现策略，不等于旧模组本身已有同名结构。

## 2. 设计约束

本设计严格遵守以下约束：

1. 业务语义必须来自旧 BDP 已实现代码，禁止凭空新增玩法规则。
2. 新 BDP 的实现要保持边界清晰：
   - `CombatBody` 只做战斗体相位真值与宿主变换。
   - `Trigger` 只做槽位、切换、发布真值。
   - `Trion` 只做资源账本。
3. 跨系统联动必须通过薄接线层或正式 surface 完成，避免彼此直接吞并职责。
4. 不引入旧 BDP 没有实现过的规则，例如：
   - 卸下触发体后战斗体继续存在
   - 战斗体基础锁定费
   - 战斗中强制禁止装卸芯片

## 3. 旧 BDP 业务基线

### 3.1 总关系图

```text
[触发体]
   |
   +--> 装/卸芯片 --> 更新 Trion.Reserved
   |
   +--> 开战斗体时提供已装芯片配置

[战斗体]
   |
   +--> 开启时正式锁定 Trion.Allocated
   +--> 开启后才允许芯片激活/攻击入口/自动攻击
   +--> 关闭时决定 Trion 如何结算

[Trion]
   |
   +--> Reserved  = 当前装载配置的预占用
   +--> Allocated = 本次战斗体已正式锁定
   +--> Available = 当前还能花的量
```

### 3.2 规则表

| 编号 | 旧 BDP 已实现规则 | 代码依据 |
| --- | --- | --- |
| R1 | 开战斗体前必须装备主武器触发体，否则直接 veto。 | `CombatBodyActivationChecker.cs` |
| R2 | 装芯片、卸芯片、装备、卸下时，触发器会同步 `ReservedAllocation`。 | `CompTriggerBody.SlotManagement.cs`，`CompTriggerBody.Lifecycle.cs` |
| R3 | 开战斗体时真正锁定的量，是“当时所有已装芯片的 allocationCost 总和”。 | `CombatBodyOrchestrator.cs`，`CompTriggerBody.CombatBodySupport.cs` |
| R4 | 战斗体开启后，触发器进入 `IsCombatBodyActive`，这是芯片激活的硬门。 | `CompTriggerBody.CombatBodySupport.cs`，`CompTriggerBody.Activation.cs` |
| R5 | 战斗体开启后，旧 BDP 只自动激活特殊槽，不自动激活左右手槽。 | `CombatBodyOrchestrator.cs`，`CompTriggerBody.CombatBodySupport.cs` |
| R6 | 芯片激活时单独支付 `activationCost`，并可注册芯片持续消耗。 | `CompTriggerBody.Activation.cs` |
| R7 | 战斗体开启后冻结自然恢复，注册战斗体维持消耗，并监听 `AvailableDepleted`。 | `CombatBodyOrchestrator.cs`，`CompTrion.cs` |
| R8 | 自动攻击和攻击入口只认当前装备中的主武器触发体，且受 `IsCombatBodyActive` 门控。 | `Patch_Pawn_TryGetAttackVerb.cs`，`Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs`，`CompTriggerBody.GizmoGeneration.cs` |
| R9 | 破裂先进入 `Collapsing`，90 ticks 后再真正紧急解除。 | `CombatBodyOrchestrator.cs`，`Hediff_CombatBodyCollapsing.cs` |
| R10 | 手动关闭：释放 `Allocated`。崩解关闭：`ForceDeplete()`，不返还。 | `CompTriggerBody.CombatBodySupport.cs`，`CombatBodyOrchestrator.cs`，`CompTrion.cs` |
| R11 | 触发体被卸下时，如果战斗体仍开着，就直接解除战斗体。 | `CompTriggerBody.Lifecycle.cs`，`Gene_TrionGland.cs` |
| R12 | 战斗中仍然允许装卸芯片；装卸只影响 `Reserved`，不回写本次 `Allocated`。 | `CompTriggerBody.SlotManagement.cs` |

### 3.3 旧 BDP 主链图

```text
[装/卸芯片]
      |
      v
[Trigger 同步 Reserved]
      |
      | Trigger On
      v
[检查]
- 装备了触发体
- 没在冷却
- Trion 付得起 Reserved
      |
      v
[Trigger 重新计算已装芯片总占用]
      |
      v
[Trion.Allocate(total)]
      |
      +--> 战斗体变身
      +--> 自动激活特殊槽
      +--> 冻结恢复
      +--> 注册维持消耗
      +--> 监听 AvailableDepleted
      |
      v
[战斗体 Active]
      |
      +--> 芯片现在才允许激活
      +--> 自动攻击现在才接管
      +--> 攻击入口现在才有效
      |
      +--> 手动关闭 -> Release(Allocated)
      |
      +--> 破裂 -> Collapsing -> 90 ticks -> ForceDeplete()
      |
      +--> 触发体卸下 -> 直接解除战斗体
```

## 4. 新 BDP 架构映射

### 4.1 真值归属不变

新 BDP 接线后，三套真值归属必须保持如下：

```text
[CompCombatBodyHost]
  └─ CombatBodyState / CombatBodyService
     负责：
     - 当前处于 Inactive / Active / Collapsing / Cooldown
     - 宿主进入/退出战斗体

[CompTriggerBody]
  └─ Trigger 槽位 / 切换 / 已发布投影
     负责：
     - 装了什么芯片
     - 哪个槽位当前激活
     - 当前发布了哪些攻击结果与入口

[CompTrion]
  └─ Trion 资源账本
     负责：
     - Cur / Max
     - Reserved / Allocated / Available
     - drain 注册
     - frozen
```

这里不再把旧 BDP 的 `Trigger.IsCombatBodyActive` 作为独立真值搬过来。

新 BDP 中，等价的业务门控真值应改为：

- `CombatBody.Phase == Active`

也就是：

- 业务语义保留
- 真值 owner 改为 `CombatBody`
- `Trigger` 不再镜像维护第二份“战斗体是否已开”的布尔值

### 4.2 新增薄接线层

新增一个仅用于跨系统事务的薄层：

```text
Core/CombatBodySession/
  CombatBodySessionService.cs
  CombatBodySessionPolicy.cs
  CombatBodySessionExitMode.cs
```

它的职责只有四类：

1. 串起开战斗体主链
2. 串起手动关闭与崩解关闭主链
3. 处理“触发体被卸下 -> 强制解除战斗体”
4. 统一做战斗体对 Trigger 发布面的门控

它不持有新的长期业务真值，不复制：

- CombatBody phase
- Trigger 槽位状态
- Trion 资源账本

### 4.3 新架构关系图

```text
Pawn
├─ CompCombatBodyHost
│  ├─ CombatBodyState
│  ├─ CombatBodyService        // 原始战斗体相位服务
│  └─ CombatBodySessionService     // 新增：跨系统接线层
│
├─ CompTrion
│  └─ TrionService
│
└─ equipment.Primary
   └─ CompTriggerBody
      ├─ Trigger slot truth
      ├─ Trigger switch truth
      └─ Trigger runtime publication
```

## 5. 新 BDP 中每条旧逻辑怎么落

### 5.1 预占用同步

#### 旧逻辑

- 芯片装卸、装备、卸下时，Trigger 直接把“当前已装芯片总占用”同步到 Trion `ReservedAllocation`。

#### 新架构落点

- 继续放在 `CompTriggerBody` 内部完成。
- 不通过 `CombatBodySessionService` 转手。

#### 原因

- 这是 `Trigger loadout -> Trion reserved` 的直接投影。
- 旧 BDP 本来就是 Trigger 负责。
- 这一步不需要战斗体 phase 参与。

#### 设计要求

1. `CompTriggerBody.TryLoadChip`
   - 成功后重算已装芯片总占用
   - 写入 `Trion.SetReserved(total)`
2. `CompTriggerBody.TryUnloadChip`
   - 成功后同样重算并写入 `Reserved`
3. `Notify_Equipped`
   - 重新同步一次 `Reserved`
4. `Notify_Unequipped`
   - 先处理战斗体解除，再把 `Reserved` 清为 0

#### 新 BDP 特有实现注意

- 由于新 Trigger 有绑定镜像槽位，计算总占用时必须避免双持镜像重复计费。
- 计费遍历必须只算“控制根槽”或按 `LoadedChip.ThingID` 去重。

### 5.2 开战斗体主链

#### 旧逻辑

- 先检查能否开
- 再正式锁定 Trion
- 再变身
- 再自动激活特殊槽
- 再注册维持消耗与 `AvailableDepleted`

#### 新架构落点

- 由 `CombatBodySessionService.TryActivate()` 承接完整跨系统事务。
- 原 `CombatBodyService.TryActivate()` 退回为“原始战斗体相位切换与宿主变换”。

#### 设计要求

`CombatBodySessionService.TryActivate()` 顺序固定为：

```text
1. 读取 CombatBodyReader，确认 CanActivate()
2. 读取当前主武器 CompTriggerBody，确认存在
3. 读取 TrionReader.Reserved，确认 CanAfford(Reserved)
4. 用 Trigger 当前 loadout 重新计算正式锁定量 allocateAmount
5. Trion.Allocate(allocateAmount)
6. CombatBody 原始服务进入 Active，记录 allocateAmount
7. Trigger 请求激活 Special 侧
8. Trion.RegisterDrain(战斗体维持消耗)
9. Trion.SetFrozen(true)
10. 订阅 Trion.AvailableDepleted
11. 请求 Trigger 刷新已发布投影
```

#### 保留的旧语义

- 开战斗体真正锁定的不是 `Reserved` 这个字段本身，而是“当前 loadout 再计算出的正式总占用”。
- 自动激活只作用于 `Special`。

### 5.3 芯片激活主链

#### 旧逻辑

- 战斗体没开时不能激活芯片。
- 芯片激活前会检查激活费是否足够。
- 正式激活后支付 `activationCost`，并注册芯片持续消耗。

#### 新架构落点

- `CompTriggerBody.RequestActivate()` 保留为 Trigger 自己的入口。
- 但入口前加一个 `CombatBodySessionPolicy.CanActivateSlot(ownerPawn)` 守卫。
- 守卫逻辑只认 `CombatBody.Phase == Active`。

#### 设计要求

```text
if CombatBody.Phase != Active:
    拒绝 RequestActivate

else:
    继续走 Trigger 现有切换状态机
    在正式提交激活时支付 activationCost
    在正式提交激活时注册芯片 drain
```

#### 这里不做的事

- 不新增“战斗中禁止装卸芯片”
- 不新增“未开战斗体也能预热激活”

### 5.4 攻击入口 / 手动入口 / 自动攻击

#### 旧逻辑

- 自动远程只在当前主武器 Trigger 且 `IsCombatBodyActive` 时接管。
- 自动近战同理。
- 攻击按钮来自当前装备中的 Trigger。

#### 新架构落点

- 不在很多地方重复写 `if phase != Active return`。
- 统一把门控落在 `Trigger 已发布战斗投影` 这一层。

#### 设计要求

新增一个发布门控规则：

```text
只有当：
- OwnerPawn 存在
- 当前主武器就是这个 CompTriggerBody
- CombatBody.Phase == Active

TriggerRuntimeCoordinator 才允许发布非空 combat projection。
否则发布空 projection。
```

#### 这样会自然得到的结果

- 手动攻击按钮消失
- 自动攻击拿不到主攻击 verb
- formal host 不再对外提供有效攻击入口
- Trigger 槽位真值仍然保留，不会被误清空

### 5.5 手动关闭

#### 旧逻辑

- 关闭所有芯片
- 释放已锁定 Trion
- 取消冻结
- 注销战斗体维持消耗
- 恢复真身

#### 新架构落点

- 由 `CombatBodySessionService.RequestDeactivate(Manual)` 承接。

#### 设计要求

```text
1. 请求 Trigger 关闭 Main / Sub / Special
2. Trion.UnregisterDrain(战斗体维持消耗)
3. 取消 AvailableDepleted 订阅
4. Trion.Release(CombatBody.AllocatedTrion)
5. Trion.SetFrozen(false)
6. CombatBody 原始服务退出到 Cooldown/Inactive
7. 请求 Trigger 刷新已发布投影
```

### 5.6 崩解关闭

#### 旧逻辑

- `AvailableDepleted` 或 Hediff 轮询条件触发 `Collapsing`
- 打断当前动作
- 进入 90 ticks 延迟
- 延迟结束后走紧急解除
- `ForceDeplete()`

#### 新架构落点

- `CombatBodySessionService` 负责把 `AvailableDepleted` 翻译成 `TriggerCollapse(reason)`。
- `CompCombatBodyHost.CompTick()` 负责在 `Collapsing` 倒计时结束时调用 `RequestDeactivate(Emergency)`。

#### 设计要求

```text
AvailableDepleted
   -> CombatBodySessionService.TriggerCollapse("Trion可用值耗尽")

CombatBody.Phase == Collapsing
and GetCollapseRemaining() <= 0
   -> CombatBodySessionService.RequestDeactivate(Emergency)

Emergency deactivate:
   1. 请求 Trigger 关闭全部槽位
   2. 注销战斗体维持消耗
   3. 取消 AvailableDepleted 订阅
   4. Trion.Release(CombatBody.AllocatedTrion)
   5. Trion.ConsumeUntilDepleted(当前可用量)
   6. CombatBody 原始服务退出到 Cooldown
   7. 请求 Trigger 刷新已发布投影
```

说明：

- 旧 BDP 是 `ForceDeplete()` 一步把 `cur=0`、`allocated=0`。
- 新 BDP 若不新增 `ForceDeplete()` 正式接口，也必须实现等价结果。
- 等价目标只有一个：**紧急关闭后不返还资源，当前战斗体剩余资源全部清空。**

### 5.7 触发体被卸下

#### 旧逻辑

- 触发体被卸下时，如果战斗体开着，直接解除战斗体。
- 不存在“战斗还在，只是入口暂停”。

#### 新架构落点

- 继续把这条规则放在 `CompTriggerBody.Notify_Unequipped(pawn)`。
- 通过 `CombatBodySurfaceAccess.ResolveCommands(pawn)` 向战斗体正式命令面发起关闭请求。

#### 设计要求

```text
Notify_Unequipped(pawn):
    if CombatBody.Phase == Active:
        RequestDeactivate(Manual)
    SyncReserved(0)
    清空 Trigger 已发布投影
```

说明：

- 这里不引入“记录 sessionTriggerThingId，等待装回恢复”。
- 这是为了严格保持旧 BDP 已实现语义。

## 6. 新增与调整的正式接口

### 6.1 新增

```text
Core/CombatBodySession/CombatBodySessionService.cs
Core/CombatBodySession/CombatBodySessionPolicy.cs
Core/CombatBodySession/CombatBodySessionExitMode.cs
```

### 6.2 调整

1. `CompCombatBodyHost`
   - 持有 `CombatBodyService` 与 `CombatBodySessionService`
   - 对外 surface 返回 `CombatBodySessionService`
   - 增加轻量 `CompTick()`，只负责：
     - 收尾 `Collapsing`
     - 处理战斗体阶段相关接线逻辑

2. `CombatBodyService`
   - 收缩为原始相位服务
   - 不再直接操作：
     - `Trion.Allocate/Release`
     - `Trigger.RequestDeactivate`
     - `SetFrozen`
     - `RegisterDrain`
     - `AvailableDepleted` 订阅

3. `TriggerRuntimeCoordinator`
   - 增加 `CombatBodySessionStateChanged` dirty 原因
   - 重建发布前先判断：
     - 当前主武器是不是本 Trigger
     - `CombatBody.Phase == Active`

4. `TriggerInteractionReason`
   - 增加一个新的正式原因码：
     - `BattleModeUnavailable`
   - 用于对外解释：
     - 战斗体未开启
     - 当前攻击入口不可用

## 7. 文件落点

| 文件 | 变更职责 |
| --- | --- |
| `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs` | 持有原始 `CombatBodyService` 与新增 `CombatBodySessionService`，并加轻量 `CompTick()` |
| `Source/BDP/Core/CombatBody/Flow/CombatBodyCoordinator.cs` | 收缩为原始相位与宿主变换服务 |
| `Source/BDP/Core/CombatBody/Access/Surfaces/CombatBodySurfaceAccess.cs` | 对外仍给 CombatBody surface，但底层改接 `CombatBodySessionService` |
| `Source/BDP/Core/Trigger/State/CompTriggerBody.cs` | 在 `RequestActivate` 前增加战斗体门控；补“刷新发布”入口 |
| `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs` | `Notify_Unequipped` 触发战斗体解除；装备/卸下继续同步 `Reserved` |
| `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs` | 对交互语义增加 `BattleModeUnavailable` 门控 |
| `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeCoordinator.cs` | phase 门控非空/空 projection 发布 |
| `Source/BDP/Core/Trigger/Runtime/ProjectionDirtyReason.cs` | 新增 `CombatBodySessionStateChanged` |
| `Source/BDP/Core/Trigger/Interaction/TriggerInteractionReason.cs` | 新增 `BattleModeUnavailable` |
| `Source/BDP/Core/Trion/*` | 不新增 owner；如需紧急清空能力，只加最小等价正式接口 |

## 8. 明确不做的事

以下内容不属于本次设计，因为旧 BDP 没有对应已实现依据，或当前新 BDP 基础还不够：

1. 不设计“卸下触发体后战斗继续，装回恢复入口”
2. 不设计战斗体基础锁定费
3. 不把战斗中装卸芯片改成禁止
4. 不在本次接线中新增新的武器使用费合同字段
5. 不在本次接线中补完整真身快照恢复细节和紧急脱离外观表现

## 9. 实施顺序

### Phase 1：打通旧 BDP 主链语义

1. 让 `CombatBodySurfaceAccess` 背后改接 `CombatBodySessionService`
2. 把 `CombatBodyService` 里直接碰 `Trigger/Trion` 的逻辑抽走
3. 保留 Trigger 侧 `Reserved` 同步
4. 接通 `TryActivate -> Allocate -> Active -> Special -> Freeze/Drain`
5. 接通手动关闭
6. 接通 `Notify_Unequipped -> 直接解除战斗体`
7. 接通 phase 门控下的空/非空 projection 发布

### Phase 2：补齐旧 BDP 战斗消耗链

1. 芯片激活成本正式支付
2. 芯片持续消耗正式注册/注销
3. `AvailableDepleted -> Collapsing -> EmergencyDeactivate`
4. 伤害消耗与伤口 drain 接回 Trion 正式账本

## 10. 旧 BDP 依据文件清单

- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/CombatBodyActivationChecker.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/CombatBodyOrchestrator.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/Hediff_CombatBodyActive.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/Hediff_CombatBodyCollapsing.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/Comps/HediffComp_TrionDamageCost.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Combat/Comps/HediffComp_TrionWoundDrain.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Core/Comps/CompTrion.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Core/Genes/Gene_TrionGland.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.Activation.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.CombatBodySupport.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.GizmoGeneration.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.Lifecycle.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Comps/CompTriggerBody.SlotManagement.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Patches/Patch_Pawn_TryGetAttackVerb.cs`
- `BorderDefenseProtocol.Legacy/Source/BDP/Trigger/Patches/Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs`

