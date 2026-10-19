# SpecialPrimary formal Verb host implementation plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 为特殊槽唯一的主武器 Verb 表达补齐正式宿主，使远程和近战特殊槽 Verb 都能复用现有手动、自动和存档攻击链。

**Architecture:** 在 Core.dll 的正式宿主模型中追加一个 `SpecialPrimary` 槽位，并让表达来源、默认主攻击选择、战斗投影和宿主管理器使用同一固定身份。特殊槽仍由现有快照规则保留最多一条武器结果并替代 Main/Sub；不增加特殊槽专用执行分支，也不修改当前 Ability 型内容。

**Tech Stack:** RimWorld 1.6、C# 7.3、.NET Framework 4.8、MSBuild、PowerShell 冒烟测试。

---

## 已确认规则

| 规则 | 实施结论 |
|---|---|
| 特殊槽出现武器表达 | 作为当前默认主武器，替代 Main/Sub 武器表达 |
| 特殊槽武器数量 | 最多保留一条主武器入口 |
| 远程与近战 | 共用 `SpecialPrimary` 宿主身份，按 `WeaponMode` 进入已有远程壳/近战壳 |
| 特殊槽副武器 | 不新增 `SpecialSecondary` |
| 当前风刃远程能力 | 仍是 Ability，不修改具体内容 |
| 存档顺序 | 现有八个宿主不重排，新槽位追加到末尾 |

## 当前根因定位

1. `ExpressionSnapshotBuilder` 已经清除 Main/Sub 武器结果，并通过 `KeepSingleWeaponResult` 保留一条 Special 武器结果。
2. `ExpressionSourceCollector.ResolveSingleSideHostSlot` 对 Special 返回 `None`，导致 Verb 来源没有表达层宿主身份。
3. `SingleSideExpressionBuilder.BuildExecutionSlotKey` 对 Special 生成模糊的 `Primary`/`Secondary` 键。
4. `DefaultPrimaryExpressionSelector.IsSingleSidePrimary` 排除了 `ExpressionOriginKind.Special`。
5. `TriggerBodyVerbHostManager.TryResolveFormalHostSlot` 没有 Special 映射，导致投影索引和 binding（绑定状态）都丢失。
6. 现有 AttackExecution 已按结果标识解析宿主，因此不需要复制特殊槽攻击执行逻辑。

## 实施步骤

### Task 1: 扩展固定宿主契约

**Files:**

- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/VerbHosting/BdpFormalVerbHostSlot.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Model/ExpressionVerbHostSlot.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trigger/State/CompTriggerBody.FormalHosts.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1`

**Step 1: 写失败的契约测试**

新增 PowerShell 测试，断言：

- `BdpFormalVerbHostSlot` 存在 `SpecialPrimary`。
- `ExpressionVerbHostSlot` 存在 `SpecialPrimaryVerb`。
- `ExpressionVerbHostSlot.SpecialPrimaryVerb` 使用追加值，不改变已有 1–8 的数值。
- `CompTriggerBody.FormalHostSlots` 将 `SpecialPrimary` 放在 `ComboSecondary` 之后。
- 固定槽位中不存在 `SpecialSecondary`。

**Step 2: 运行测试确认失败**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1"
```

Expected: FAIL，报告缺少 `SpecialPrimary` 契约。

**Step 3: 实现最小契约扩展**

- 在 `BdpFormalVerbHostSlot` 末尾追加 `SpecialPrimary`。
- 在 `ExpressionVerbHostSlot` 末尾追加 `SpecialPrimaryVerb = 9`。
- 在 `FormalHostSlots` 数组末尾追加 `BdpFormalVerbHostSlot.SpecialPrimary`。
- 不重排、不改名已有槽位。

**Step 4: 运行测试确认通过**

Run 同 Step 2。

Expected: `SpecialPrimaryVerbHostSmokeTests PASS`。

**Step 5: Commit**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP/Core/VerbHosting/BdpFormalVerbHostSlot.cs" "模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Model/ExpressionVerbHostSlot.cs" "模组工程/BorderDefenseProtocol/Source/BDP/Core/Trigger/State/CompTriggerBody.FormalHosts.cs" "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1"
git commit -m "feat: add special primary formal verb host slot"
```

### Task 2: 接通 Special 主武器的表达来源和默认选择

**Files:**

- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Pipeline/ExpressionSourceCollector.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Pipeline/SingleSideExpressionBuilder.cs`
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Projection/DefaultPrimaryExpressionSelector.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1`

**Step 1: 在契约测试中补充失败断言**

增加源码契约断言：

- `ResolveSingleSideHostSlot` 对 `TriggerSide.Special` 的 Primary Verb 返回 `ExpressionVerbHostSlot.SpecialPrimaryVerb`。
- Special Secondary 不被映射到任何宿主。
- `BuildExecutionSlotKey` 对 Special Primary 返回 `SpecialPrimary`，不再返回裸的 `Primary`/`Secondary`。
- `IsSingleSidePrimary` 承认 `ExpressionOriginKind.Special`。

Expected: FAIL，因为当前实现仍返回 `None`、裸键，并排除 Special。

**Step 2: 实现来源映射**

在 `ExpressionSourceCollector.ResolveSingleSideHostSlot` 中追加 Special 分支：

```csharp
case TriggerSide.Special:
    return declaration.VerbAttackRole == VerbAttackRole.Primary
        ? ExpressionVerbHostSlot.SpecialPrimaryVerb
        : ExpressionVerbHostSlot.None;
```

这一步只给 Special Primary 分配宿主；Special Secondary 保持不可执行，符合最多一条主武器入口的规则。

**Step 3: 实现稳定执行键**

在 `SingleSideExpressionBuilder.BuildExecutionSlotKey` 中追加 Special 分支：

```csharp
if (material.Side == TriggerSide.Special)
{
    return material.VerbAttackRole == VerbAttackRole.Primary
        ? "SpecialPrimary"
        : null;
}
```

不要为 Special 生成 `SpecialSecondary`。

**Step 4: 扩展默认主表达选择**

在 `DefaultPrimaryExpressionSelector.IsSingleSidePrimary` 的来源条件中加入 `ExpressionOriginKind.Special`。

现有快照覆盖规则已经清除了 Main/Sub 武器结果，因此该选择器加入 Special 后，`PrimaryRanged` 或 `PrimaryMelee` 会按武器模式选中唯一的特殊槽武器结果。双武器和组合技现有优先级保持不变。

**Step 5: 运行测试确认通过**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1"
```

Expected: `SpecialPrimaryVerbHostSmokeTests PASS`。

**Step 6: Commit**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Pipeline/ExpressionSourceCollector.cs" "模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Pipeline/SingleSideExpressionBuilder.cs" "模组工程/BorderDefenseProtocol/Source/BDP/Core/Expressions/Projection/DefaultPrimaryExpressionSelector.cs" "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1"
git commit -m "feat: project special primary verb expressions"
```

### Task 3: 接通战斗投影和正式宿主绑定

**Files:**

- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs`
- Verify only: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Trigger/Projection/TriggerCombatProjectionBuilder.cs`
- Verify only: `模组工程/BorderDefenseProtocol/Source/BDP/Core/AttackExecution/AttackExecutionSurfaceAccess.cs`
- Verify only: `模组工程/BorderDefenseProtocol/Source/BDP/Core/AttackExecution/AttackExecutionTargetingSource.cs`
- Test: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1`

**Step 1: 补充失败断言**

断言 `TriggerBodyVerbHostManager.TryResolveFormalHostSlot` 对以下结果成立：

- `CompositeKind=None`。
- `OriginKind=Special`。
- `VerbAttackRole=Primary`。
- 输出 `BdpFormalVerbHostSlot.SpecialPrimary`。

同时断言该方法不会把 Special Secondary 映射到新槽位。

Expected: FAIL，因为 manager 当前对 Special 直接返回 false。

**Step 2: 实现正式宿主映射**

在 `TryResolveFormalHostSlot` 中，在 Main/Sub 分支前追加 Special Primary 分支：

```csharp
if (result.OriginKind == ExpressionOriginKind.Special
    && result.VerbAttackRole == VerbAttackRole.Primary)
{
    slot = BdpFormalVerbHostSlot.SpecialPrimary;
    return true;
}
```

不在 `TriggerCombatProjectionBuilder` 中添加重复的 Special 判断；它继续复用 manager 的唯一映射口建立 `ResultIdToFormalSlot`。

**Step 3: 验证现有壳自动覆盖新槽位**

确认 `CompTriggerBody.FormalHostSlots` 被 `TriggerBodyVerbHostManager` 用于：

- 远程壳创建。
- 近战壳创建。
- fallback VerbProperties（回退 Verb 属性）声明。
- binding 刷新。
- 活跃 Tick 队列。
- 深度存档和读档恢复。

除枚举列表追加外，不复制新管理器、新壳或新生命周期。

**Step 4: 验证攻击执行复用现有路径**

确认以下代码仍按结果标识取得 `SpecialPrimary` 绑定，并按 `WeaponMode` 走远程或近战：

- `AttackExecutionTargetingSource.ResolveCurrentContext`。
- `AttackExecutionSurfaceAccess.TryResolveAutoPrimaryVerb`。
- `AttackExecutionSurfaceAccess.TryExecuteAutoMelee`。

如果验证结果显示这些路径不依赖 Main/Sub 字面名称，不修改它们。

**Step 5: 运行测试确认通过**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/FormalHostVerbSmokeTests.ps1"
```

Expected: 两个测试均输出 `PASS`。

**Step 6: Commit**

```powershell
git add -- "模组工程/BorderDefenseProtocol/Source/BDP/Core/VerbHosting/TriggerBodyVerbHostManager.cs" "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1"
git commit -m "fix: bind special primary verbs to formal hosts"
```

### Task 4: 编译和回归验证

**Files:**

- Verify: `模组工程/BorderDefenseProtocol/Source/BDP/BDP.csproj`
- Verify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/FujinSpecialSlotPreloadSmokeTests.ps1`
- Verify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/AutoRangedHostSessionIsolationSmokeTests.ps1`
- Verify: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/BodyPartDisintegrationSmokeTests.ps1`

**Step 1: 编译 Core.dll**

Run:

```powershell
dotnet build "模组工程/BorderDefenseProtocol/Source/BDP/BDP.csproj" -c Release
```

Expected: 构建成功，并通过已有 Core 隔离门禁。

**Step 2: 运行相关源码/XML 冒烟测试**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/FormalHostVerbSmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/FujinSpecialSlotPreloadSmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/AutoRangedHostSessionIsolationSmokeTests.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -File "模组工程/BorderDefenseProtocol/Source/BDP.Tests/BodyPartDisintegrationSmokeTests.ps1"
```

Expected: 全部输出 `PASS`。

**Step 3: 游戏内验证远程 Special Primary**

使用一个测试芯片把远程 `PrimaryVerb` 放入特殊槽，确认：

- 入口图标存在。
- 点击入口可以选择 Pawn（角色），不能选择范围攻击专用目标逻辑之外的额外目标类型。
- 发射和命中使用特殊槽远程 Verb。
- Main/Sub 武器不会同时成为默认主攻击。

**Step 4: 游戏内验证近战 Special Primary**

使用现有风刃实体刀的 `PrimaryVerb` 形态，确认：

- 手动选择实体目标后使用实刃。
- 自动近战攻击使用特殊槽近战 Verb。
- 不会回退到 Main/Sub 或原版其他近战工具。

**Step 5: 游戏内验证无回归**

- 当前风刃·远程斩击 Ability 仍正常。
- 普通 Main/Sub 远程和近战入口不变。
- 双武器和组合技入口不变。
- 旧存档加载后 formal host（正式宿主）列表没有错位。

**Step 6: Commit**

```powershell
git status --short
git add -- "模组工程/BorderDefenseProtocol/Source/BDP.Tests/SpecialPrimaryVerbHostSmokeTests.ps1"
git commit -m "test: verify special primary verb host regression"
```

提交时只暂存本计划涉及的文件，不纳入工作区已有的 DLL、PDB、obj、临时素材或其他未相关文档。

## 预期影响

### 正面

- 修复特殊槽远程和近战 `PrimaryVerb` 入口“有图标但无目标/不自动攻击”的共同根因。
- 将特殊槽主武器纳入正式宿主抽象，未来业务只需提供标准表达，不再追加专用执行逻辑。
- 远程和近战共享同一宿主身份，避免按武器类型复制 Special 专用链路。

### 负面与控制

- 每个 TriggerBody 多持有一组远程/近战正式宿主壳，带来少量固定对象和存档字段增长；这是正式宿主表扩展的必要成本。
- 正式宿主列表长度改变，旧存档需要依赖现有深度存档缺项恢复和刷新流程；通过“只追加、不重排”控制风险。
- 如果未来误配置多个 Special Primary Verb，现有快照规则仍只保留第一条；测试会锁定“最多一条”的约束，不额外设计多入口策略。

## 交付后记录

完成后按项目约束写入：

`C:\NiwtDatas\Projects\RimworldModStudio\日志\Agent工作日志`

日志需记录根因、实际修改文件、测试结果和未完成的游戏实测项，并保持单个日志文件不超过 20 条。
