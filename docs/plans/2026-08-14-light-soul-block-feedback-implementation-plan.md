# 光魂抵挡反馈与芯片按钮独立控制 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修正光魂抵挡特效方向，增加可配置盾面受击回弹，并阻止两枚芯片的形态、姿态按钮互相聚合。

**Architecture:** Content 护盾组件统一解析一次攻击行进角，并沿用原版偏转 Effecter；Core 仅增加按表达结果 ID 保存和消费短暂视觉位移的中性能力。光魂通过 Hediff XML 配置回弹参数，两个内容 Gizmo 明确禁用原版聚合。

**Tech Stack:** RimWorld 1.6、C# 7.3、Harmony、XML、PowerShell 冒烟测试

---

### Task 1: 建立失败回归测试

**Files:**
- Create: `Source/BDP.Tests/LightSoulBlockFeedbackSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ChipModeGizmoContentSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ChipStanceGizmoContentSmokeTests.ps1`

**Steps:**

1. 为统一攻击角、有效原版 Effecter 目标、精确 offset、表达视觉位移和两个按钮的 `groupable = false` 写结构断言。
2. 运行三个测试，确认它们因所需实现尚不存在而失败。

### Task 2: 修正攻击方向与命中特效位置

**Files:**
- Modify: `Source/BDP.Content/Shield/EnergyShieldBlockPolicy.cs`
- Modify: `Source/BDP.Content/Shield/HediffComp_EnergyShield.cs`
- Modify: `Source/BDP.Content/Shield/EnergyShieldEffectPlayer.cs`

**Steps:**

1. 增加近战攻击者方向解析，并在 `TryBlockDamage` 中只解析一次攻击行进角。
2. 让角度判定、命中点和表现共用该角度。
3. 以 Pawn 和攻击者作为原版 Effecter 的 A/B；用 `Effecter.offset` 保留精确盾面坐标并抵消原版固定目标偏移。
4. 运行护盾反馈测试，确认方向和特效结构断言通过。

### Task 3: 增加中性表达视觉冲量

**Files:**
- Create: `Source/BDP/Core/Trigger/Runtime/ExpressionVisualImpulse.cs`
- Create: `Source/BDP/Core/Trigger/Access/Surfaces/ExpressionVisualFeedbackAccess.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerVisualRuntimeStateOwner.cs`
- Modify: `Source/BDP/Core/Trigger/Runtime/TriggerRuntimeServices.cs`
- Modify: `Source/BDP/Patches/Patch_PawnRenderUtility_DrawEquipmentAiming_BdpVisual.cs`

**Steps:**

1. 以结果 ID、起始 tick、方向、距离、时长表示一次瞬时位移。
2. 提供接受表达 Hediff 的公开通知口，由 Core 内部映射到该 Hediff 当前绑定的结果 ID。
3. 在当前 Trigger 的视觉运行时 owner 中记录冲量，并在投影重置时自然清空。
4. 绘制常驻条目时解析当前 tick 位移，只修改已解析姿态的主贴图和附加层位置。
5. 运行视觉运行时与护盾反馈测试。

### Task 4: 让光魂抵挡触发回弹

**Files:**
- Modify: `Source/BDP.Content/Shield/HediffCompProperties_EnergyShield.cs`
- Modify: `Source/BDP.Content/Shield/HediffComp_EnergyShield.cs`
- Modify: `1.6/Content/Defs/HediffDef/LightSoul.xml`

**Steps:**

1. 增加默认关闭的 XML 参数 `blockVisualImpulseTicks` 与 `blockVisualImpulseDistance`。
2. 成功抵挡后按攻击行进方向发布冲量；参数未启用时不产生额外行为。
3. 灵活姿态和举盾姿态都配置为 8 tick、0.08 格。
4. 运行护盾反馈测试。

### Task 5: 禁止芯片控制按钮聚合

**Files:**
- Modify: `Source/BDP.Content/Trigger/UI/ChipModes/ChipModeGizmoProvider.cs`
- Modify: `Source/BDP.Content/Trigger/UI/ChipStances/ChipStanceGizmoProvider.cs`

**Steps:**

1. 两类命令构造时显式设置 `groupable = false`。
2. 运行形态与姿态 Gizmo 测试，确认独立实例不会被原版合并。

### Task 6: 全量验证、日志和提交

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志46.md`

**Steps:**

1. 构建 `Source/BDP/BDP.csproj` 与 `Source/BDP.Content/BDP.Content.csproj`。
2. 运行新增测试及相关护盾、视觉、Gizmo 冒烟测试。
3. 检查 git diff，只保留本计划内改动。
4. 在工作日志首部追加本次记录，保持倒序且不超过 20 条。
5. 提交实现，提交信息使用中文任务语义。
