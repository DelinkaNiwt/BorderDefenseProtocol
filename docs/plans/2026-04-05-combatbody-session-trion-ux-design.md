# CombatBody Session 命名与 Trion 提示/调试设计

> 本稿用于落实三个已确认需求：`CombatBodySession` 全量改名、远程 Trion 不足提示去刷屏、Trion 调试 gizmo 增补。

**目标：**
- 把当前误导性的 `CombatBodySession` 命名收敛为更贴近实际职责的 `CombatBodySession`
- 让远程 Trion 不足提示改为“首次提示，恢复后重置”
- 给 Pawn 侧 Trion GUI 增加开发调试按钮，并通过正式命令口做受约束写入

## 1. 命名判断

当前 `CombatBodySessionService` 实际职责是：
- 作为 `CombatBodySurfaceAccess` 背后的正式 reader / commands / events surface
- 收口 `CombatBody` 激活、关闭、崩解时对 `Trigger` / `Trion` 的跨系统顺序
- 不承担远程攻击逐轮扣费，也不承载更大范围“整体战斗会话”真值

因此它更接近：
- `CombatBodySessionService`
- `CombatBodySessionPolicy`
- `CombatBodySessionTrionBinding`
- `CombatBodySessionExitMode`

结论：做全量改名，避免后续把它误解成更高层战斗总控。

## 2. 远程 Trion 提示策略

当前问题不在闸门判断本身，而在提示出口：只要 AI/持续攻击重复尝试，就会重复弹“Trion 不足，无法发射”。

本次采用：
- **首次失败提示一次**
- **持续不足期间只拒绝，不再提示**
- **只要后续任一正式闸门检查重新判定“够用”，立即清除已提示状态**
- **再次变回不足时，再允许弹一次**

不采用纯时间节流，避免“明明一直没恢复，却每隔几秒又弹一次”。

实现位置：
- 保持 `RangedAttackTrionGate` 继续只返回结果
- 在 `BdpVerb_Shoot` 的提示出口增加按 `Pawn` 维度的不足提示锁存

## 3. Trion 调试写入口

当前正式 `Trion` 命令面只支持：
- 扣减
- 预占用
- 正式锁定
- 释放
- drain / frozen

缺少“开发调试时安全地修改当前值”的正式入口。

本次新增正式命令：
- `AdjustCurrent(float delta)`
- `TrySetCurrent(float target, out string rejectMessage)`

约束规则：
- 上限始终不超过 `max`
- 非战斗体激活时，下限为 `0`
- 战斗体激活时，下限为当前 `allocated`

语义细化：
- `+50`：正常上调，超过 `max` 时夹到 `max`
- `-50`：若会跌破下限，则夹到下限并提示
- `MAX`：直接设为 `max`
- `0`：若当前下限大于 `0`，则拒绝并提示，不做修改

## 4. 调试 gizmo 挂载位置

维持边界不变：
- `Gizmo_TrionStatus` 继续只做显示
- 调试按钮仍由 Pawn 侧 gizmo 组装桥追加

因此按钮挂在：
- `Gene_TrionGland`
- `TrionGeneGizmoBridge`

只在开发模式下显示四个按钮：
- `+50`
- `-50`
- `MAX`
- `0`

## 5. 测试策略

先改/补 smoke tests，再改实现：
- 命名契约 smoke tests：改为 `CombatBodySession*`
- 远程 Trion 提示 smoke test：锁定“首次提示，恢复后重置”语义
- Trion 正式写入口 smoke test：锁定调试写接口存在与下界约束
- gizmo smoke test：锁定 dev 模式下的四个按钮和行为边界

