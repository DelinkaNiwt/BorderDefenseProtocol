# 光魂注视警戒朝向交还设计

## 目标

手动“注视警戒”继续保存玩家指定的目标；目标暂时离开射程或视线时不结束 Job（作业），但立即停止接管人物朝向，让原版朝向系统恢复默认表现。目标重新可注视后，自动恢复面向原目标。

## 根因

当前自定义 `JobDriver_LightSoulGuardWatch`（注视警戒作业驱动）把无限持续的 Toil（作业步骤）固定设置为 `handlingFacing = true`。目标不可命中时，它停止调用原版 `FaceTarget`（面向目标），却仍阻止 `Pawn_RotationTracker`（人物朝向追踪器）运行，因此人物冻结在最后朝向。

原版强制攻击只在 Warmup/Cooldown（瞄准/冷却姿态）实际占用朝向；目标不可攻击且回到 Mobile（空闲姿态）后，朝向交还原版。目标锁定与朝向占用是两件事。

## 设计

- 保留自定义 Job，它只承担原版没有的“非攻击但保存同一手动目标”职责。
- 每 tick（游戏刻）继续用正式 Verb（行为器）的 `CanHitTarget` 判断射程和视线。
- 当前可注视时：把该 Toil 的 `handlingFacing` 设为 `true`，调用原版 `FaceTarget`。
- 当前不可注视时：把 `handlingFacing` 设为 `false`，不写入 Rotation（朝向），让原版 `Pawn_RotationTracker` 完整接管。
- 不修改目标有效性、自动索敌、原版等待作业补丁、朝向更新补丁、攻击门禁或 XML（可扩展标记语言）参数。

## 验证

- 回归测试确认朝向接管值来自当前 `CanHitTarget`，不再永久为真。
- 回归测试继续确认失去射程/视线不会结束 Job，目标恢复后继续调用原版 `FaceTarget`。
- 运行注视警戒、暴力禁用和光魂芯片相关测试，并发布编译 Content.dll（内容程序集）。
