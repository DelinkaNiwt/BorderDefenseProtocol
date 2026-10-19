# 统一投射物拦截判定 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让 BDP 自定义阻挡扫描与原版 `ProjectileHitFlags` 使用同一份“额外许可下是否放行非目标”的判定，同时保证没有额外许可的普通投射物完全保持原版行为。

**Architecture:** Core 新增中立的 `ProjectileInterceptionDecision`，只负责把冻结的 `ProjectileInteractionPolicy`解释为“非目标是否放行”以及对应的原版 flags 适配。发射入口调用 flags 适配器，BDP 几何扫描调用同一判定器；不修改原版 `TickInterval`、`CheckForFreeInterceptBetween` 或 `CanHit`，也不添加 Harmony 拦截补丁。`flyOverhead` 继续作为原版越顶投射物的独立语义保留。

**Tech Stack:** RimWorld 1.6、C# 7.3、Verse/RimWorld `ProjectileHitFlags`、PowerShell 静态契约测试、`dotnet msbuild`。

---

### Task 1: 先写并运行失败的碰撞共享契约测试

**Files:**
- Create: `Source/BDP.Tests/ProjectileInterceptionDecisionSmokeTests.ps1`

**Step 1: Write the failing test**

断言 Core 存在统一判定器和语义字段，浮空许可使用该字段，发射入口与 `SegmentCollisionService` 都调用统一判定器，同时没有新增原版 `Projectile` Harmony 补丁文件。

**Step 2: Run test to verify it fails**

Run: `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\ProjectileInterceptionDecisionSmokeTests.ps1`

Expected: FAIL，因为统一判定器和新语义字段尚不存在。

### Task 2: 建立 Core 统一判定器

**Files:**
- Create: `Source/BDP/Core/Projectiles/Interaction/ProjectileInterceptionDecision.cs`
- Modify: `Source/BDP/Core/Projectiles/Interaction/ProjectileInteractionPolicy.cs`
- Modify: `Source/BDP/Core/Permissions/IndirectRangedAttackPermissionResolverRegistry.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedProtocol/ProjectileInit/ProjectileInitStageService.cs`

**Step 1: Write minimal implementation**

把原先只描述 BDP 自定义扫描的 `BypassObjectiveBlockers` 改为中性的 `AllowNonTargetPassThrough`。统一判定器提供两类入口：

- 将“明确命中目标”的原版 flags 去掉 `NonTargetWorld` 与 `NonTargetPawns`，保留 `IntendedTarget`；无许可或无实体目标时原样返回。
- 判断当前 BDP 扫描发现的世界物体是否是应放行的非目标；最终目标不放行。

所有策略 OR 合并逻辑同步合并新字段，保持其它拦截器、护盾策略不变。

**Step 2: Run the focused test**

Run: `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\ProjectileInterceptionDecisionSmokeTests.ps1`

Expected: 仍可能因接线尚未完成而 FAIL，但失败点应从 Core 判定器转移到调用方。

### Task 3: 让浮空与已有风刃策略声明同一语义

**Files:**
- Modify: `Source/BDP.Content/FloatingFlight/FloatingFlightIndirectAttackPermissionReceiver.cs`
- Modify: `Source/BDP.Content/RangedModules/DistanceRevector/DistanceRevectorModule.cs`
- Modify: `Source/BDP.Tests/FujinCoreContractSmokeTests.ps1`
- Modify: `Source/BDP.Tests/RangedIndirectAttackPermissionSmokeTests.ps1`

**Step 1: Update policy producers**

浮空许可和风刃距离重定向都声明 `AllowNonTargetPassThrough=true`；控制台许可保持不声明，普通远程表达继续走原版 flags。

**Step 2: Run policy tests**

Run: `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\FujinCoreContractSmokeTests.ps1`

Expected: PASS。

### Task 4: 接入 BDP 自定义几何扫描

**Files:**
- Modify: `Source/BDP/Core/Projectiles/BdpProjectile.cs`
- Modify: `Source/BDP/Core/Projectiles/RangedFlightProtocol/Collision/SegmentCollisionService.cs`

**Step 1: Use the shared decision**

扫描遇到完整填充世界物体时调用 `ProjectileInterceptionDecision`；有额外许可且该物体不是最终目标时不升级为客观阻挡。原有 `flyOverhead` 仍可跳过整段客观阻挡扫描，但普通许可不再通过单独的 `ShouldBypassObjectiveBlockers` 直接跳过扫描。

**Step 2: Run collision contract tests**

Run: `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\ProjectileInterceptionDecisionSmokeTests.ps1`

Expected: PASS。

### Task 5: 接入原版 flags 适配，不修改原版拦截方法

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/Abilities/BdpVerb_AbilityShoot.cs`

**Step 1: Adapt only intended-target launch branches**

普通子弹、强制偏射、随机偏射和掩体拦截分支继续使用原有 flags。明确目标发射分支和 Ability 发射分支统一调用 `ProjectileInterceptionDecision` 生成 flags；无许可时返回原值，有许可时只移除非目标类别。

**Step 2: Run focused tests**

Run: `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Source\BDP.Tests\ProjectileInterceptionDecisionSmokeTests.ps1`

Expected: PASS。

### Task 6: 编译、边界检查和回归

**Files:**
- Modify: `docs/04-架构评估/2026-09-15-远程间接攻击许可最终方案.md`
- Create/Modify: `日志/Agent工作日志/Agent日志276.md`

**Step 1: Run static tests**

Run: `Get-ChildItem .\Source\BDP.Tests\*Projectile*SmokeTests.ps1 | ForEach-Object { pwsh -NoProfile -ExecutionPolicy Bypass -File $_.FullName }`

Expected: all selected projectile tests PASS。

**Step 2: Build Core and Content**

Run: `dotnet msbuild .\Source\BDP\BDP.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal`

Run: `dotnet msbuild .\Source\BDP.Content\BDP.Content.csproj -p:Configuration=Debug -p:UseSharedCompilation=false -t:Build -v:minimal`

Expected: both builds succeed; Core isolation check passes。

**Step 3: Verify diff and commit**

Run: `git diff --check`

Commit: `git add <本计划涉及文件> && git commit -m "feat: 统一投射物额外许可拦截判定"`

最终必须明确：没有额外许可的普通投射物没有新的 flags 或扫描分支；本次没有给原版 `Projectile` 方法打 Harmony 补丁。
