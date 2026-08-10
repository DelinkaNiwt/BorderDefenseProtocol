# Trigger正式宿主Verb重构 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 彻底移除 Trigger 当前“运行时临时注入宿主 verb”的做法，把参与原版战斗会话的 BDP 宿主 verb 重构为原版 `VerbTracker` 可正式重建的稳定实体，同时保持 `TriggerSlotState` 为唯一业务真值。

**Architecture:** 当前报错的根因不是 slot/container 恢复顺序，而是 BDP 把会被原版 `Stance_Warmup` 跨存档持有的 combat verb 设计成了运行时动态注入对象。原版 `Stance_Busy` 会存 `verb` 引用，而原版 `VerbTracker` 只会重建由正式 `VerbProperties` / `Tools` 声明出来的 verb；因此动态宿主 verb 读档后丢失 `verbProps`，被原版判定为 `bugged verb`。整改后，BDP 必须提供少量固定、正式声明、可读档重建的宿主 verb 壳；业务状态仍只存在于 Trigger 真值与 expression 结果中，宿主层只负责“固定宿主槽位 -> 当前 formal result”的正式绑定。

**Tech Stack:** RimWorld 1.6 C#, Verse `VerbTracker` / `Stance_Busy` / `Scribe`, Harmony runtime patches, PowerShell smoke tests

---

## Scope

- 只处理 Trigger / Expression / VerbHosting / 自动战斗接入这条链。
- 不做旧档向下兼容，不保留任何过渡性双轨设计。
- 不改组合技、双持、远程协议的业务规则本身，只改它们接入原版 combat verb 生命周期的方式。

## Non-Goals

- 不新增大一统运行时状态对象。
- 不引入额外中间 owner 层或复杂迁移系统。
- 不在攻击层继续增加读档后的症状兜底补丁。

## Success Criteria

- 原版 `Stance_Warmup` 读档后不再出现 `bugged verb after loading`。
- BDP 自动远程入口读取到的是原版 `VerbTracker` 正式拥有的宿主 verb，而非运行时临时注入 verb。
- `TriggerSlotState` 仍然是唯一业务真值；verb 不得反向承载业务真值。
- `VerbHostManager` 不再承担“new verb + Add 到 AllVerbs”的职责，只承担固定宿主槽位与当前 formal result 的绑定。
- 读档后 expression / host / auto ranged 能自然重建，不依赖旧会话补洞。

## Hard Constraints

- 严禁为了兼容旧档保留临时注入宿主 verb 路线。
- 严禁同时保留“动态宿主 verb”和“正式宿主 verb”双轨实现。
- 严禁把业务字段迁移到 verb 实例中作为第二真值。
- 允许尽量兼容原版正式生命周期，但不为了兼容旧 BDP 中间态存档而污染新架构。

## Architecture Decisions

### Decision 1: 宿主 verb 必须成为原版正式运行时实体

只要一个 verb 会被原版 `Stance_Busy` / `Job` / `VerbTracker` 跨存档持有，它就必须满足：

- 由原版 `VerbTracker` 正式创建
- 拥有稳定 `loadID`
- 读档后可重新初始化 `verbProps`

禁止继续用运行时 `AllVerbs.Add(...)` 手工注入后再让原版 stance 持有。

### Decision 2: 宿主 verb 只做稳定入口壳，不做业务真值

正式宿主 verb 只允许持有：

- 固定宿主槽位身份
- 当前 owner / tracker / loadID
- 执行时需要的最小运行时上下文

正式宿主 verb 不允许持有：

- 芯片装载真值
- 激活真值
- expression 业务语义真值

这些仍然由 Trigger / Expression 正式层持有。

### Decision 3: HostManager 从对象工厂改成绑定器

`TriggerBodyVerbHostManager` 重构后只负责：

- 固定宿主槽位清单
- 当前槽位绑定哪个 formal result
- 当前槽位是否可用

不再负责：

- `new Verb`
- 运行时向 `VerbTracker.AllVerbs` add/remove
- 临时生成 runtime host 对象图

### Decision 4: 自动远程 / 自动近战入口只读取正式宿主槽位

自动入口不再依赖“当前有一条临时 host instance 恰好存在”，而是依赖：

- 原版 `VerbTracker` 中正式存在的宿主 verb
- Host binding 告诉它这条宿主 verb 当前对应哪个 formal result

## Files To Modify

### Trigger Formal Owner / Lifecycle

- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Reads.cs`

### Formal Verb Host Layer

- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Modify: `Source/BDP/Core/VerbHosting/VerbHostSurfaceAccess.cs`
- Modify: `Source/BDP/Core/VerbHosting/VerbHostInstance.cs`
- Delete or Rewrite: `Source/BDP/Core/VerbHosting/VerbHostBuildSpec.cs`
- Delete: `Source/BDP/Core/VerbHosting/VerbHostAutoProxyVerb.cs`
- Create: `Source/BDP/Core/VerbHosting/BdpFormalVerbHostSlot.cs`
- Create: `Source/BDP/Core/VerbHosting/BdpFormalVerbBinding.cs`
- Create: `Source/BDP/Core/VerbHosting/BdpFormalVerbBindingState.cs`

### Formal Host Verbs

- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`
- Create if needed: `Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs`
- Create if needed: `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`

### Expression / Attack Execution Integration

- Modify: `Source/BDP/Core/Expressions/Access/Surfaces/ExpressionFormalSurfaces.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedAttackExecutionContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/MeleeAttackExecutionContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`

### Harmony Patches

- Modify: `Source/BDP/Patches/Patch_Pawn_TryGetAttackVerb.cs`
- Modify: `Source/BDP/Patches/Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs`

### Tests / Docs

- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Create: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`
- Create: `docs/04-架构评估/2026-03-31/2026-03-31-Trigger正式宿主Verb重构完成报告-第一版.md`

## Task 1: Freeze The New Architecture Contract In Tests

**Files:**
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Create: `Source/BDP.Tests/FormalHostVerbSmokeTests.ps1`

**Step 1: Write the failing smoke assertions for formal host verbs**

新增断言，要求：

- `TriggerBodyVerbHostManager` 不再包含 `trackerVerbs.Add(verb)` 路径
- `TriggerBodyVerbHostManager` 不再包含 `Activator.CreateInstance(spec.VerbClass)` 路径
- `VerbHostBuildSpec` 不再作为宿主实例构造核心存在；若文件保留，必须不再承担 runtime verb 构造
- 自动远程入口不得再读取临时 `VerbHostInstance.Verb`

**Step 2: Add assertions for formal owner behavior**

断言：

- `CompTriggerBody` 必须暴露正式 `VerbProperties` 供原版 `VerbTracker` 初始化
- 正式宿主 verb 必须有稳定宿主槽位身份，不再依赖 runtime `ResultId -> new verb`

**Step 3: Run tests to verify they fail**

Run:

```powershell
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- FAIL
- 失败点明确落在“仍使用动态宿主 verb 注入”

## Task 2: Formalize Host Verb Slots

**Files:**
- Create: `Source/BDP/Core/VerbHosting/BdpFormalVerbHostSlot.cs`
- Create: `Source/BDP/Core/VerbHosting/BdpFormalVerbBinding.cs`
- Create: `Source/BDP/Core/VerbHosting/BdpFormalVerbBindingState.cs`
- Modify: `Source/BDP/Core/VerbHosting/VerbHostInstance.cs`

**Step 1: Introduce fixed host-slot identities**

创建固定宿主槽位枚举或轻量键类型，至少覆盖：

- MainPrimary
- MainSecondary
- SubPrimary
- SubSecondary
- DualPrimary
- DualSecondary
- ComboPrimary
- ComboSecondary

要求：

- 这是运行时正式入口身份，不是业务真值
- 数量固定，不能按 expression 结果动态增删

**Step 2: Introduce binding state object**

新增轻量绑定对象，只表达：

- HostSlot
- 当前绑定的 `ResultId`
- 当前是否可用
- 当前武器模式 / 结果概况（仅最小运行时需要）

禁止放入：

- 芯片真值
- slot 真值
- expression snapshot 真值副本

**Step 3: Make existing host instance model transitional only if necessary**

如果 `VerbHostInstance` 仍被上下游广泛使用，可把它改为“正式壳 verb + binding 视图”。

要求：

- 不得再把它当成 runtime 构造产物
- 若无必要，后续直接删除

**Step 4: Run smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- PASS on slot/binding model assertions

## Task 3: Make CompTriggerBody A Formal Verb Owner

**Files:**
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.cs`
- Modify: `Source/BDP/Core/Trigger/State/CompTriggerBody.Lifecycle.cs`

**Step 1: Add stable formal host-verb declarations**

让 `CompTriggerBody` 为原版 `VerbTracker` 提供固定宿主 verb 声明。

推荐方式：

- 通过正式 `VerbProperties` 返回固定宿主壳 verb 列表
- 不通过运行时 add/remove 注入

要求：

- 宿主槽位与 formal `VerbProperties` 一一对应
- loadID 必须由原版 `VerbTracker` 计算和持有

**Step 2: Ensure lifecycle does not rebuild formal verbs ad hoc**

读档、初始化、刷新投影时：

- 只更新 binding
- 不 `new` 新 verb
- 不 `Clear + rebuild tracker verbs`

**Step 3: Run failing-then-passing smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- PASS

## Task 4: Rewrite HostManager As A Binding Manager

**Files:**
- Modify: `Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Modify: `Source/BDP/Core/VerbHosting/VerbHostSurfaceAccess.cs`
- Delete or Rewrite: `Source/BDP/Core/VerbHosting/VerbHostBuildSpec.cs`

**Step 1: Delete dynamic verb construction path**

删除：

- `Translate(...) -> BuildSpec -> TryCreateVerb(...)`
- `RegisterHostedVerb(...)`
- `UnregisterHostedVerbs(...)`
- `trackerVerbs.Add(...)`

**Step 2: Replace with binding refresh**

`Refresh(snapshot)` 重构为：

- 按固定 `HostSlot` 解析当前 formal result
- 更新 binding state
- 更新正式壳 verb 的最小运行时绑定信息

要求：

- 不创建/销毁 verb
- 只更新 binding

**Step 3: Rewrite lookup surfaces**

`VerbHostSurfaceAccess` 改为查询：

- 某 `ResultId` 当前映射到哪个正式宿主槽位
- 当前主远程/主近战槽位的正式壳 verb 是什么

**Step 4: Run smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- PASS

## Task 5: Convert Bdp Verbs Into Formal Host Shells

**Files:**
- Modify: `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`
- Modify: `Source/BDP/Core/Verbs/BdpVerb_MeleeAttackDamage.cs`
- Create if needed: `Source/BDP/Core/Verbs/BdpVerb_FormalHostShoot.cs`
- Create if needed: `Source/BDP/Core/Verbs/BdpVerb_FormalHostMelee.cs`

**Step 1: Decide shell strategy**

优先保持类数量最小：

- 若现有 `BdpVerb_Shoot` / `BdpVerb_MeleeAttackDamage` 可直接转成正式壳 verb，就直接改
- 只有在职责过重、会污染执行路径时，才拆出 `FormalHost` 子类

**Step 2: Make host verbs read current binding instead of cached runtime truth**

正式宿主 verb 只允许长期持有：

- 自己的正式宿主槽位身份
- 对 owner / binding manager 的读取入口

执行时现读：

- 当前绑定的 `ResultId`
- 当前 formal result 对应的执行数据

**Step 3: Remove dependency on transient runtime host creation**

删除或收缩：

- 依赖 runtime `HostResultId` 初始化一整条宿主对象图
- 依赖外部“先造 host 再给 verb 塞上下文”的流程

要求：

- 宿主壳能在原版 `VerbTracker` 正式重建后自然工作

**Step 4: Run smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- PASS

## Task 6: Rewire AttackExecution To Formal Host Bindings

**Files:**
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Modify: `Source/BDP/Core/AttackExecution/RangedAttackExecutionContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/MeleeAttackExecutionContext.cs`
- Modify: `Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`

**Step 1: Make auto-ranged/auto-melee read formal host shells**

自动入口要改成：

- 读取正式宿主槽位的壳 verb
- 由 binding manager 决定当前 `ResultId`

不再：

- 找“临时 host instance 里那条 verb”

**Step 2: Rebuild execution contexts from binding states**

`RangedAttackExecutionContext` / `MeleeAttackExecutionContext` 获取 verb 的方式重构为：

- 先根据 `ResultId` 找正式宿主槽位
- 再拿该槽位对应的正式壳 verb

要求：

- 仍然从 formal result 出发
- 不反向把 verb 变成业务真值来源

**Step 3: Run smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
```

Expected:

- All PASS

## Task 7: Remove Transitional Proxy Route Completely

**Files:**
- Delete: `Source/BDP/Core/VerbHosting/VerbHostAutoProxyVerb.cs`
- Modify: `Source/BDP/Patches/Patch_Pawn_TryGetAttackVerb.cs`
- Modify: `Source/BDP/Patches/Patch_Pawn_MeleeVerbs_TryMeleeAttack.cs`

**Step 1: Delete proxy-verb architecture**

彻底删除：

- `VerbHostAutoProxyVerb`
- 任何“原版拿代理 verb，再翻译回正式执行”的路径

**Step 2: Patch side should only hand out formal host shells**

自动战斗 patch 只做：

- 原版无可用远程/近战 verb 时
- 尝试返回正式宿主壳 verb

要求：

- 不再 new 代理
- 不再桥接另一条 runtime verb

**Step 3: Run smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
```

Expected:

- PASS

## Task 8: Verify Against Original Failure Mode

**Files:**
- Test: `Source/BDP.Tests/*.ps1`
- Test: `Source/BDP/BDP.csproj`
- Test: in-game warmup save/load scenario

**Step 1: Run static smoke tests**

Run:

```powershell
& '.\Source\BDP.Tests\FormalHostVerbSmokeTests.ps1'
& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'
& '.\Source\BDP.Tests\ComboDefinitionBoundarySmokeTests.ps1'
& '.\Source\BDP.Tests\DefaultBurstParitySmokeTests.ps1'
& '.\Source\BDP.Tests\RangedProtocolBoundarySmokeTests.ps1'
```

Expected:

- All PASS

**Step 2: Build**

Run:

```powershell
$env:DOTNET_CLI_HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
$env:HOME='C:\NiwtDatas\Projects\RimworldModStudio\.dotnet_home'
dotnet msbuild 'C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal
```

Expected:

- PASS

**Step 3: Manual in-game verification**

Scenario:

1. 装备 Trigger 武器并激活远程芯片
2. 进入自动远程 warmup
3. 在 warmup 中存档
4. 读档

Expected:

- 不再出现 `Verse.Stance_Warmup had a bugged verb after loading`
- 不再出现 `自动远程入口缺少主远程宿主`
- 若 Trigger 真值合法，expression snapshot 不为空
- 原版 warmup 会话能继续自然推进

**Step 4: Regression check against architecture rules**

确认：

- 没有 runtime `AllVerbs.Add(...)`
- 没有动态宿主 verb 构造
- 没有 proxy verb
- 没有第二真值迁入 verb

## Task 9: Write Completion Report

**Files:**
- Create: `docs/04-架构评估/2026-03-31/2026-03-31-Trigger正式宿主Verb重构完成报告-第一版.md`

**Step 1: Record final architecture**

写清楚：

- 为什么 runtime 注入宿主 verb 必然会破坏原版读档恢复
- 为什么“正式宿主壳 + 真值驱动 binding”符合单一真值原则
- 为什么这不是为了报错结果做 patch，而是对原版正式生命周期的对齐

**Step 2: Record evidence**

记录：

- smoke tests
- build
- manual warmup save/load verification

## Risks

- `CompTriggerBody` 作为正式 verb owner 的改造可能暴露出当前 `VerbProperties` 暴露面不足，需要补最小 owner 协议。
- 某些执行上下文若默认依赖“当前 host instance 一定包含具体 verb 对象”，需要改成“先找正式宿主槽位，再找壳 verb”。
- 若 `BdpVerb_Shoot` 当前承担的即时上下文过多，可能需要拆出更干净的 formal host shell 类，但应以最小化类数量为原则。

## Definition Of Done

- 动态注入宿主 verb 路线被彻底删除
- 参与原版战斗会话的 BDP 宿主 verb 全部成为原版 `VerbTracker` 可正式重建对象
- `TriggerSlotState` 仍是唯一业务真值
- HostManager 只承担 binding，不再承担 verb 工厂职责
- warmup 存档再读档不再出现 `bugged verb after loading`
