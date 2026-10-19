# 光魂推进预览与行动时序调整实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 恢复光魂推进的最大射程圆圈，允许射程外和建筑遮挡后的正确直线预览，并把敌人眩晕和释放者落地僵直调整为约 2 秒与 1 秒。

**Architecture:** 保留现有专用光魂推进 Verb 的严格施法校验；仅让它把“存在可预览冲刺路径”的位置报告给原版 Targeter（目标器），从而获得鼠标位置并绘制预览。实际点击仍走 `ValidateTarget`，不允许超射程或穿过建筑。释放者使用 `VerbProperties.defaultCooldownTime`（原版 Verb 冷却参数）和 `Stance_Cooldown`（原版动作冷却姿态）；由于 Pawn 会在 `TryCastShot` 内进入飞行器，专用 Verb 在能力成功启动后补齐原版收尾时序。敌人眩晕使用现有 `StunHandler`。

**Tech Stack:** C# 7.3、.NET Framework 4.8、RimWorld 1.6 `Verb`/`Targeter`/`PawnFlyer` API、XML Def、PowerShell 冒烟测试。

---

### Task 1: 先建立失败测试

**Files:**
- Modify: `Source/BDP.Tests/LightSoulPropulsionDefinitionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/LightSoulPropulsionSourceBoundarySmokeTests.ps1`

**Expected assertions:**

- 敌人眩晕配置为 120 tick；
- 光魂推进 Verb 配置为 1 秒原版动作冷却；
- 专用 Verb 恢复最大射程圆圈；
- 专用 Verb 提供“预览可见性”目标判定；
- 严格 `ValidateTarget` 仍然存在。

### Task 2: 实现最小改动

**Files:**
- Modify: `1.6/Content/Defs/AbilityDef/LightSoulPropulsion.xml`
- Modify: `1.6/Content/Defs/ThingDef/PawnFlyer_LightSoulPropulsion.xml`
- Modify: `Source/BDP.Content/LightSoul/Propulsion/Verb_CastAbilityLightSoulPropulsion.cs`

**Implementation:**

- 将 `stunTicks` 改为 120；
- 在能力 Verb 配置中加入 `defaultCooldownTime` 固定值 1 秒；专用 Verb 在飞行器生成前后正确接续 `Stance_Cooldown`；
- 在专用 Verb 中绘制原版最大射程圈；
- `CanHitTarget` 只为有可移动终点的位置返回预览可见，保留 `CanHitTargetFrom`/`ValidateTarget` 的严格范围与建筑校验；
- 对无实际移动路径的位置仍不显示虚假预览。

### Task 3: 验证和提交

运行定义测试、源码边界测试、Core/Content Release 构建、Core 隔离检查和 XML 解析；更新 `日志/Agent工作日志`，提交本次改动。最后需完全重启游戏加载存档，确认圆圈、射程外预览、墙后预览、敌人眩晕 2 秒和释放者僵直 1 秒。
