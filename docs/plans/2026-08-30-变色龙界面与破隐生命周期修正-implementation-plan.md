# 变色龙界面与破隐生命周期修正 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修正变色龙 Hediff 标签、跨 Ability 宿主冷却进度基准、非远程视觉和攻击后破隐生命周期。

**Architecture:** Core 只扩展来源级冷却账本，保存每次实际释放的剩余时间和总时长，并通过通用 Ability 冷却总时长表面保持原版进度条比例。Content 只配置变色龙的原版显示参数，并在攻击事件中移除当前 Hediff；芯片仍保持激活。使用当前工作区 `main`，不创建分支或 worktree。

**Tech Stack:** C# 7.3、.NET Framework 4.8、RimWorld 1.6 Def/XML、Harmony、PowerShell 冒烟测试、`dotnet msbuild`。

---

### Task 1: 扩展通用冷却记录

**Files:**
- Modify: `Source/BDP/Core/Abilities/GameComponent_AbilityReleaseCooldownLedger.cs`
- Modify: `Source/BDP/Core/Abilities/AbilityReleaseCooldownService.cs`
- Create: `Source/BDP/Core/Abilities/Patch_Ability_CooldownTicksTotal_BdpReleaseCooldown.cs`
- Test: `Source/BDP.Tests/AbilityReleaseCooldownSmokeTests.ps1`

**Step 1: Write the failing test**

增加断言：账本记录包含总时长字段；释放登记读取 `CooldownTicksTotal`；恢复路径保留剩余时间并通过通用表面提供原始总时长；不出现变色龙专属常量。

**Step 2: Run test to verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/AbilityReleaseCooldownSmokeTests.ps1`

Expected: FAIL because the current record只保存结束 tick，且恢复调用不会保留原始总时长。

**Step 3: Write minimal implementation**

在记录中保存总冷却 tick；账本提供同一 Pawn 和冷却键的剩余/总时长查询；登记时保存 Ability 的实际值；恢复时继续调用 `StartCooldown(remaining)`；新增 Core Harmony getter postfix，仅对声明了通用冷却键且存在有效账本记录的 Ability 覆盖 `CooldownTicksTotal`。

**Step 4: Run test to verify it passes**

Run the same PowerShell test. Expected: `AbilityReleaseCooldownSmokeTests PASS`。

### Task 2: 修正变色龙 Def 和语言文案

**Files:**
- Modify: `1.6/Content/Defs/HediffDef/Chameleon.xml`
- Modify: `1.6/Content/Defs/AbilityDef/Chameleon.xml`
- Modify: `1.6/Content/Defs/ChipFunctionDef/Presets.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/DefInjected/HediffDef/Chameleon.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/DefInjected/AbilityDef/Chameleon.xml`
- Modify: `Languages/ChineseSimplified (简体中文)/Keyed/Chameleon.xml`
- Test: `Source/BDP.Tests/ChameleonDefinitionSmokeTests.ps1`

**Step 1: Write the failing test**

锁定 `showRemainingTime=true`、`targetable=false`、`range=0`、`drawAimPie=false`、`requireLineOfSight=false`，并锁定变色龙文案不再承诺自动关闭芯片。

**Step 2: Run test to verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/ChameleonDefinitionSmokeTests.ps1`

Expected: FAIL because这些节点当前不存在且旧文案仍存在。

**Step 3: Write minimal implementation**

只修改变色龙 Def 和语言包；保留预热时间、音效、Ability 共享冷却键、Hediff 20 秒持续时间及其他隐身配置。

**Step 4: Run test to verify it passes**

Run the same PowerShell test. Expected: `ChameleonDefinitionSmokeTests PASS`。

### Task 3: 攻击后只移除 Hediff

**Files:**
- Modify: `Source/BDP.Content/Chameleon/HediffComp_BdpInvisibility.cs`
- Delete: `Source/BDP.Content/Chameleon/ChameleonAttackShutdownService.cs`
- Modify: `Source/BDP.Tests/ChameleonAttackClosureBoundarySmokeTests.ps1`

**Step 1: Write the failing test**

将边界测试改为要求攻击事件仍由隐身组件订阅，但处理函数直接移除当前 `parent` Hediff，且不再查找来源槽位、调用 `RequestDeactivate` 或依赖关断服务。

**Step 2: Run test to verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/ChameleonAttackClosureBoundarySmokeTests.ps1`

Expected: FAIL because当前实现仍调用 `ChameleonAttackShutdownService`。

**Step 3: Write minimal implementation**

在 Content 隐身组件的攻击回调中直接调用 `Pawn.health.RemoveHediff(parent)`；保留攻击目标缓存刷新和事件订阅生命周期；删除旧关断服务及其无用的投影/槽位依赖。

**Step 4: Run test to verify it passes**

Run the same PowerShell test. Expected: `ChameleonAttackClosureBoundarySmokeTests PASS`。

### Task 4: 回归、构建、日志和提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志98.md`（如本文件达到20条则新建倒序日志文件）

**Step 1: Run focused regression tests**

运行变色龙、Ability 冷却、表达持续消耗、程序集隔离和布局相关测试，并用 PowerShell 解析 `1.6/Content/Defs` 下全部 XML。

**Step 2: Rebuild all assemblies**

依次运行 Core、Content、Development 的 Release `dotnet msbuild -t:Rebuild`。

**Step 3: Inspect the diff**

确认本次提交不包含既有 PSD、DLL/PDB 和用户新增贴图；检查无旧攻击后关断引用、无变色龙业务常量进入 Core。

**Step 4: Update work log**

按项目规则写入简洁的时间倒序工作日志。

**Step 5: Commit**

```powershell
git add <本次源代码、XML、测试、文档和日志文件>
git commit -m "fix: refine chameleon lifecycle and cooldown UI"
```
