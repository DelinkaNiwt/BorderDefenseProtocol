# BDP 远程间接攻击许可 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将风刃远程斩击已经验证的非直视目标和投射物路径能力抽成所有 BDP 远程表达可复用的中性协议，并接入浮空自身许可；浮空许可不允许把屋顶下目标变成非直视目标。

**Architecture:** Core 提供远程表达能力标记、结构化间接攻击许可解析器、目标屋顶后置判定和 ProjectileInit 交互策略合并。Content 注册控制台与浮空许可接收器。目标合法性和投射物初始化查询同一份结构化许可结果；Core 不引用 FloatingFlight 或其它具体业务。

**Tech Stack:** C#、RimWorld 1.6 API、Harmony、PowerShell 静态契约测试、dotnet msbuild。

---

## Task 1: 先写失败的统一远程间接攻击契约测试

**Files:** `Source/BDP.Tests/RangedIndirectAttackPermissionSmokeTests.ps1`

测试锁定正式远程表达能力、结构化许可、目标后置屋顶判定、所有远程入口和 ProjectileInit 接线；先运行确认失败。

## Task 2: 把所有远程表达支持间接攻击固化在正式表达结果

**Files:** `Source/BDP/Core/Expressions/Model/FormalExpressionResult.cs`

增加只读 `SupportsIndirectTargeting` 属性。只有正式远程表达且有完整 `ResolvedVerbSpec` 时为 true，不增加每个芯片的重复 XML 开关。

## Task 3: 在 Core 建立结构化间接攻击许可设施

**Files:**
- `Source/BDP/Core/Permissions/IndirectRangedAttackPermissionContext.cs`
- `Source/BDP/Core/Permissions/IndirectRangedAttackPermissionGrant.cs`
- `Source/BDP/Core/Permissions/IIndirectRangedAttackPermissionResolver.cs`
- `Source/BDP/Core/Permissions/IndirectRangedAttackPermissionResolverRegistry.cs`
- `Source/BDP/Core/Permissions/IndirectRangedAttackPermissionAccess.cs`
- `Source/BDP/Core/Permissions/DirectTargetLineOfSightPermissionAccess.cs`

上下文携带 Pawn、结果标识、显式许可键、目标、远程能力标记、必要直射声明和 `TargetIsRoofed`。授权结果携带 `AllowRoofedTargets` 与可合并的 `ProjectileInteractionPolicy`。注册表按 OR 聚合，并在授权后执行屋顶后置判定；旧直射许可注册表保留以兼容既有接入。

## Task 4: 接入 Content 的控制台与浮空许可

**Files:**
- `Source/BDP.Content/OperatorConsole/DirectTargetLineOfSightPermissionReceiver.cs`
- `Source/BDP.Content/FloatingFlight/FloatingFlightIndirectAttackPermissionReceiver.cs`
- `Source/BDP.Content/ContentBootstrap.cs`

控制台复用既有地图许可，允许屋顶下目标但不新增投射物绕过策略。浮空接收器只读取 Pawn 自身 `BDP.FloatingFlight.SelfActive`，不允许屋顶下目标，并声明 `BypassObjectiveBlockers=true`。

## Task 5: 接入所有远程目标入口

**Files:** `AttackExecutionTargetingSource.cs`、`BdpVerb_AbilityShoot.cs`、`BdpVerb_FormalHostShoot.cs`、`AttackExecutionService.Stages.cs`

目标选择、Ability、FormalHost 和 dual 分侧均传入具体目标查询统一许可。必要直射远程表达只有在授权且目标未违反屋顶条件时放宽 LOS；原版射程和目标类型继续由原版 Verb 裁定。

## Task 6: 让 ProjectileInit 复用同一授权结果

**Files:** `Source/BDP/Core/AttackExecution/RangedProtocol/ProjectileInit/ProjectileInitStageService.cs`

模块贡献合并后，以实际来源正式表达和最终当前目标查询间接攻击许可，并合并返回的 `ProjectileInteractionPolicy`。只放行 BDP 自己的目标阻挡物路径，不全局关闭原版拦截器或护盾。

## Task 7: 回归、文档和提交

**Files:** `docs/04-架构评估/2026-09-15-远程间接攻击许可最终方案.md`、`日志/Agent工作日志/Agent日志273.md`

验证三份远程相关静态测试、Core/Content Debug 构建和 `git diff --check`，只暂存本计划涉及文件，提交 `feat: 统一远程间接攻击许可`。验收：浮空许可的非屋顶目标可选取且投射物可穿越 BDP 目标阻挡物；屋顶下不放宽 LOS；风刃专属模块保持；Core 不引用具体 FloatingFlight 类型。
