# Core Directory Layering Pass Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 把 `Core/Trigger` 和 `Core/CombatBody` 从单层平铺整理成贴玩法职责的分组目录，同时不改变玩法行为。

**Architecture:** 只重组目录和命名空间归属，不新增业务层。`Trigger` 按 `Access / State / Switching / Loadout / External / Projection` 分组，`CombatBody` 按 `Access / State / Flow / Bridge` 分组；`Trion / Expressions / Projections` 保持平铺，避免为整齐而整齐。

**Tech Stack:** C#, RimWorld mod, .NET Framework project, `dotnet msbuild`

---

### Task 1: Trigger 分组迁移

**Files:**
- Modify: `Source/BDP/Core/Trigger/*`

**Step 1: 建立目标分组目录**

建立：
- `Source/BDP/Core/Trigger/Access`
- `Source/BDP/Core/Trigger/State`
- `Source/BDP/Core/Trigger/Switching`
- `Source/BDP/Core/Trigger/Loadout`
- `Source/BDP/Core/Trigger/External`
- `Source/BDP/Core/Trigger/Projection`

**Step 2: 迁移文件到职责目录**

按职责迁移文件并保持命名空间为 `BDP.Core.Trigger`。

**Step 3: 编译确认**

Run: `dotnet msbuild Source/BDP/BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`
Expected: PASS

### Task 2: CombatBody 分组迁移

**Files:**
- Modify: `Source/BDP/Core/CombatBody/*`

**Step 1: 建立目标分组目录**

建立：
- `Source/BDP/Core/CombatBody/Access`
- `Source/BDP/Core/CombatBody/State`
- `Source/BDP/Core/CombatBody/Flow`
- `Source/BDP/Core/CombatBody/Bridge`

**Step 2: 迁移文件到职责目录**

按职责迁移文件并保持命名空间为 `BDP.Core.CombatBody`。

**Step 3: 编译确认**

Run: `dotnet msbuild Source/BDP/BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`
Expected: PASS

### Task 3: 记录与复扫

**Files:**
- Modify: `docs/02-研究记录/2026-03-25-新BDP三轮架构审查/*`
- Modify: `docs/02-研究记录/2026-03-25-新BDP三轮架构审查/16-全局目录与层级裁定-第一版.md`

**Step 1: 写执行记录**

记录迁移理由、目录职责和不迁移 `Trion / Expressions / Projections` 的原因。

**Step 2: 复扫目录**

确认目录结构符合职责，不留下空目录或误导目录。

**Step 3: 编译确认**

Run: `dotnet msbuild Source/BDP/BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`
Expected: PASS
