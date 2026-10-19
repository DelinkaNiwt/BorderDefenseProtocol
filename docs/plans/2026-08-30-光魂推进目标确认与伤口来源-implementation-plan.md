# 光魂推进目标确认与伤口来源修复实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让光魂推进在射程外或建筑后仍可确认当前预览得到的有效终点，并让推进造成的钝伤在伤口 Hediff（健康状态）上显示正确的能力来源。

**Architecture:** 仅修改光魂推进专用 Verb 的目标确认逻辑，不改变其它冲刺能力；它复用现有路径解析结果，接受“至少有一格实际位移”的预览终点，因此射程外会落在最大射程内、建筑遮挡会落在建筑前最后可达格。伤害来源复用 BDP 现有 `SemanticRuntimeScope`（语义运行时作用域）与伤口补丁，让原版 `DamageWorker_AddInjury`（伤口伤害工作器）负责创建伤口并统一填入来源，同时覆盖新伤口和合并伤口。

**Tech Stack:** C# 7.3、.NET Framework 4.8、RimWorld 1.6 `Verb`/`DamageInfo`/`PawnFlyer` API、Harmony、PowerShell 源码边界测试。

---

### Task 1: 添加失败回归断言

**Files:**
- Modify: `Source/BDP.Tests/LightSoulPropulsionSourceBoundarySmokeTests.ps1`

**Step 1: Write the failing test**

在现有光魂推进源码边界测试中增加断言：

```powershell
Assert-True ($lightSoulVerbText -match 'override\s+bool\s+ValidateTarget') "光魂推进必须允许已预览的射程外或遮挡后终点确认。"
Assert-True ($lightSoulText -match 'SemanticRuntimeScope\.Push') "光魂推进伤害必须进入伤害语义作用域。"
Assert-True ($lightSoulText -match 'SemanticContext') "光魂推进伤害必须提供伤口来源语义。"
Assert-True ($lightSoulText -match 'DisplayLabel') "光魂推进伤害来源必须使用能力显示名称。"
Assert-True ($lightSoulText -match 'SourceKind\.AttackAction') "光魂推进伤害来源必须标记为攻击行为。"
```

**Step 2: Run test to verify it fails**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Source\BDP.Tests\LightSoulPropulsionSourceBoundarySmokeTests.ps1
```

Expected: FAIL，先报告缺少光魂推进专用 `ValidateTarget`（目标确认校验），说明当前仍走原版地面冲刺的严格整格目标校验。

### Task 2: 修复光魂推进目标确认

**Files:**
- Modify: `Source/BDP.Content/LightSoul/Propulsion/Verb_CastAbilityLightSoulPropulsion.cs`

**Step 1: Write minimal implementation**

增加光魂推进专用 `ValidateTarget`：先复用 `CanHitTarget` 的路径预览结果与 `IsApplicableTo`（能力适用性），再逐项执行能力效果组件的原版 `Valid` 校验；不要调用 `base.ValidateTarget`，因为基类地面冲刺校验要求鼠标目标本身在射程内且能完整抵达，会把已被预览截断的终点再次拒绝。另覆写 `TryStartCastOn`，在原版暖机的 `TryFindShootLineFromTo`（射击线查找）二次校验前，把请求目标替换为同一份路径解析得到的 `path.EndCell`；否则第一次点击确认虽通过，暖机阶段仍会因原始射程外/墙后位置失败而不释放能力。

```csharp
public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
{
    if (!CanHitTarget(target) || !IsApplicableTo(target, showMessages) || ability == null)
    {
        return false;
    }

    for (int i = 0; i < ability.EffectComps.Count; i++)
    {
        if (!ability.EffectComps[i].Valid(target, showMessages))
        {
            return false;
        }
    }

    return true;
}
```

这样射程外目标会确认到最大射程内终点，墙后目标会确认到墙前最后可达格；没有任何实际位移的目标仍不可确认。原版暖机、`TryCastShot`（施法执行）和效果组件都会消费同一实际终点，`DrawHighlight` 也继续使用 `path.EndCell`，所以目标确认、视觉预览和真正位移保持同一条路径。

**Step 2: Run test to verify it passes**

Run the Task 1 PowerShell test. Expected: target-confirmation assertions pass; source assertions remain failing until Task 3。

### Task 3: 为推进钝伤接入伤口来源语义

**Files:**
- Modify: `Source/BDP.Content/LightSoul/Propulsion/CompAbilityEffect_LightSoulPropulsion.cs`
- Modify: `Source/BDP.Content/LightSoul/Propulsion/PawnFlyer_LightSoulPropulsion.cs`

**Step 1: Write minimal implementation**

从能力 Def 的已解析显示标签传入飞行器；在 `ApplyBluntDamage` 的 `TakeDamage` 外包裹现有 `SemanticRuntimeScope.Push`，使用 `SemanticContext.DisplayLabel`、`SemanticSourceKind.AttackAction` 和 `FlyingPawn`。来源显示标签由本地化能力名称和释放者短标签组成，不在代码硬编码玩家文案。伤害数值、推动、体型阻挡和眩晕逻辑不改。

```csharp
using (SemanticRuntimeScope.Push(new SemanticContext
{
    DisplayLabel = BuildInjurySourceLabel(),
    SourceKind = SemanticSourceKind.AttackAction,
    Instigator = FlyingPawn
}))
{
    target.TakeDamage(new DamageInfo(
        resolvedDamageDef,
        damageAmount,
        armorPenetration,
        angle,
        FlyingPawn));
}
```

保持 BDP 已有伤口创建与合并补丁不变，避免重复复制 `PawnFlyer_BounceSlash` 的快照和手动写入逻辑。

**Step 2: Run test to verify it passes**

Run the Task 1 PowerShell test. Expected: all source and target-confirmation assertions pass。

### Task 4: 全量验证、日志和提交

**Files:**
- Modify: `日志/Agent工作日志/Agent日志101.md`

**Step 1: Run focused tests**

运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\Source\BDP.Tests\LightSoulPropulsionSourceBoundarySmokeTests.ps1
powershell -ExecutionPolicy Bypass -File .\Source\BDP.Tests\LightSoulPropulsionDefinitionSmokeTests.ps1
```

**Step 2: Run project verification**

按项目既有方式执行 Core/Content Release 构建、Core 隔离检查和 XML 解析；确认构建产物已更新且不暂存用户无关的贴图文件。

**Step 3: Update work log**

记录本次根因、修复点和验证结果，日志文件保持不超过 20 条。

**Step 4: Commit**

```powershell
git add -- Source/BDP.Tests/LightSoulPropulsionSourceBoundarySmokeTests.ps1 Source/BDP.Content/LightSoul/Propulsion/CompAbilityEffect_LightSoulPropulsion.cs Source/BDP.Content/LightSoul/Propulsion/PawnFlyer_LightSoulPropulsion.cs Source/BDP.Content/LightSoul/Propulsion/Verb_CastAbilityLightSoulPropulsion.cs 日志/Agent工作日志/Agent日志101.md 1.6/Assemblies/BDP.Content.dll 1.6/Assemblies/BDP.Content.pdb
git commit -m "fix: 修复光魂推进目标确认与伤口来源"
```
