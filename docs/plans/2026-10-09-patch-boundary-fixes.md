# BDP 补丁边界修复实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复审查 F01–F14，阻止无消费者普通路径产生业务写入或继承错误上下文。
**Architecture:** 使用已有组件、作用域、伤害结果设施和原版事件。基础层保持中性；护盾、浮空和黑触发器条件位于正式内容层。
**Tech Stack:** C#（C#语言）7.3、.NET Framework（框架）4.8、Harmony（方法补丁）、RimWorld（边缘世界）1.6。

## 任务与文件边界

1. **浮空与护盾（F01/F08/F11/F14）**
   - FloatingFlight/Patches/Patch_PawnPathFollower_Steps_FloatingFlight.cs：创建账本前准入；未知通行指令改写安全放弃。
   - GameComponent_FloatingFlight.cs、FloatingFlightStateController.cs：既有污染记录不能触发普通人物搬动；只保留有用状态。
   - Shield/HediffCompProperties_EnergyShield.cs：遵守 ignoreShields（忽略护盾）。
   - FloatingFlight 自有路径网格生命周期接 RoofChanged（屋顶变化）事件，删除 SetRoof（设置屋顶）全局补丁。
2. **正式内容其余（F02/F04/F12/F13）**
   - BlackTriggerInheritedTraitService.cs：实际移除 BDP 临时特性后才重算。
   - Patch_Pawn_HealthTracker_CombatBodyTriggerProtection.cs：倒地/重生上下文增加正常与异常幂等恢复。
   - Patch_ThingWithComps_SpecialDisplayStats_TriggerBodyWeaponInfo.cs：只替换已确认的原版占位武器统计，保留第三方条目。
   - Patch_WorkGiver_DoBill_ChipUnfinishedThing.cs：补类级标记，验证两个真实目标注册。
3. **基础层临时状态与攻击（F05/F06/F09，F10 身体版本/资格部分）**
   - Patch_FloatMenuUtility_GetRangedAttackAction.cs：保存真正写入组件及旧提示，对称恢复。
   - Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess.cs：只观察真实发射成功，保留近战未命中业务与能力排除。
   - HitFeedbackColorRuntime.cs、PawnBodyConstraintSignalHub.cs：收束静态强引用/版本寿命，不增加全量人物扫描。
   - TriggerBodyEquipmentEligibility.cs：无触发体且无活跃战斗体不进入失效清理。
4. **基础层伤害与爆炸（F03/F07，F10 承伤采集部分）**
   - DamageResolutionRuntime.cs、Patch_Thing_TakeDamage_BdpResolution.cs：显式请求、嵌套身份隔离、按需采集及对称退出。
   - SemanticRuntimeScope.cs：空值必须形成隔离边界。
   - ExplosionImpactDispatchContext.cs、Patch_Explosion_StartExplosion_BdpSemantics.cs：单个创建请求只绑定一次爆炸。
   - Patch_DamageWorker_ExplosionDamageThing_BdpSemantics.cs：恢复逐目标完整上下文，实际伤害身份从调用边界传递。
   - BdpProjectile.cs、BdpVerb_MeleeAttackDamage.cs，以及 LightSoul 推进/旋空波现有承伤调用者：更新必要请求接线，保留原版伤害工作器。
5. **整合与验证**
   - 针对性行为夹具与真实注册检查；受游戏运行环境限制的项目记录实测范围。
   - 四程序集正式编译、Core 依赖隔离检查、差异审查。
   - 修复结果文档逐项回填 F01–F14，更新当前工作日志，只提交本次改动。

## 执行约束

独立域按并行修复技能分工，禁止同时编辑共享文件。子任务不提交、不写总日志、不部署程序集；主任务统一审阅、编译和提交。只修报告范围，临时探路和无效尝试不留在最终项目。
