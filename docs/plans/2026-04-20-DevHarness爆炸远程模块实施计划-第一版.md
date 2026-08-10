# DevHarness（开发测试模组）爆炸远程模块 Implementation Plan（实施计划）

> **For Claude（给 Claude）:** REQUIRED SUB-SKILL（必需子技能）: Use superpowers:executing-plans to implement this plan task-by-task（使用 executing-plans 按任务逐步执行）。

**Goal（目标）:** 在 `BorderDefenseProtocol.DevHarness（BDP 开发测试模组）` 增加可配置爆炸半径、带瞄准范围预览的远程爆炸模块。

**Architecture（架构）:** 新增测试模组专用 `ExplosiveModule（爆炸模块）`，只实现 `Preview（预览）` 与 `Impact（终结结算）` 两段。预览阶段提交 `CellGroup（格子组）` 绘制项，颜色固定灰白色且不做 `XML（配置）` 字段；结算阶段提交 `AreaEffectPlan（范围效果计划）`，真实爆炸继续由主模组现有 `BdpProjectile（BDP 投射物宿主）` 调用原版 `GenExplosion.DoExplosion（原版爆炸执行接口）`。投射物继续复用现有远程类测试芯片共用投射物，不新增投射物 `Def（定义）`。

**Tech Stack（技术栈）:** C# / RimWorld `GenExplosion（原版爆炸工具）` / BDP `RangedModules（远程模块）` / XML `Def（定义文件）` / PowerShell（脚本测试）

---

## 执行约束

- 本计划不要求当前会话执行 `git commit（提交）`；只有用户明确要求提交时才提交。
- 所有实现只落在 `BorderDefenseProtocol.DevHarness（BDP 开发测试模组）`，除测试脚本外不改主模组代码。
- 不新增主模组公共接口，除非实现时发现现有接口无法表达需求。

---

### Task 1: 写爆炸模块结构烟雾测试

**Files（文件）:**

- Create（创建）: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessExplosiveRangedModuleSmokeTests.ps1`
- Check（检查）: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/RangedModules/Samples`
- Check（检查）: `模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/RangedAttackModuleDefs_Test.xml`
- Check（检查）: `模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_Combat.xml`

**Step 1: Write the failing test（写失败测试）**

脚本断言：

- 存在 `ExplosiveModule.cs（爆炸模块源码）`。
- 模块实现 `IPreviewStageModule（预览阶段模块）`。
- 模块实现 `IImpactStageModule（终结结算阶段模块）`。
- 配置类继承 `RangedModuleConfigNode（远程模块配置节点）`。
- XML 存在 `BDP_TestRangedExplosiveModule（测试远程爆炸模块）`。
- XML 存在 `BDP_TestChipExplosiveRanged（测试爆炸远程芯片）`。

**Step 2: Run test to verify it fails（运行测试确认失败）**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessExplosiveRangedModuleSmokeTests.ps1'
```

Expected（期望）:

```text
FAIL（失败）：爆炸模块源码或 XML 定义尚不存在。
```

---

### Task 2: 新增爆炸模块源码

**Files（文件）:**

- Create（创建）: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/RangedModules/Samples/ExplosiveModule.cs`

**Step 1: Add runtime class（新增运行时类）**

实现：

- `IRangedAttackModuleRuntime（远程模块运行时）`
- `IPreviewStageModule（预览阶段模块）`
- `IImpactStageModule（终结结算阶段模块）`

**Step 2: Add config class（新增配置类）**

新增 `ExplosiveModuleConfig（爆炸模块配置）` 字段：

- `ExplosionRadius（爆炸半径）`
- `DamageDef（伤害类型）`
- `DamageAmount（伤害量）`
- `ArmorPenetration（护甲穿透）`
- `SuppressBaselineImpact（抑制基线命中）`

**Step 3: Add preview behavior（新增预览行为）**

逻辑：

```text
record.Target（当前瞄准目标）
    ↓
DamageDefOf.Bomb.Worker.ExplosionCellsToHit（原版爆炸覆盖格计算）
    ↓
PreviewDrawItemKind.CellGroup（格子组预览）
```

同时：

```text
record.UseVanillaFieldRadius = false
```

预览颜色固定灰白色，直接写在模块代码里，不新增 `XML（配置）` 字段。

**Step 4: Add impact behavior（新增爆炸结算行为）**

逻辑：

```text
contribution.SuppressBaselineImpact = config.SuppressBaselineImpact
contribution.HasAreaEffect = true
contribution.OverrideAreaEffect = new AreaEffectPlan（范围效果计划）
```

中心：

```text
context.Projectile.Position（投射物当前位置）
```

伤害与护甲穿透：

```text
配置有效值优先，否则回退当前投射物数值。
```

---

### Task 3: 新增 XML 定义

**Files（文件）:**

- Modify（修改）: `模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/RangedAttackModuleDefs_Test.xml`
- Modify（修改）: `模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_Combat.xml`

**Step 1: Add module Def（新增模块定义）**

新增：

```text
BDP_TestRangedExplosiveModule（测试远程爆炸模块）
```

默认配置建议：

```text
ExplosionRadius（爆炸半径） = 2.9
DamageDef（伤害类型） = Bomb（爆炸）
DamageAmount（伤害量） = 18
SuppressBaselineImpact（抑制基线命中） = true
```

**Step 2: Add chip Def（新增芯片定义）**

新增：

```text
BDP_TestChipExplosiveRanged（测试爆炸远程芯片）
```

挂载：

```xml
<RangedModules>
  <li>
    <moduleDef>BDP_TestRangedExplosiveModule</moduleDef>
  </li>
</RangedModules>
```

并保持：

```text
defaultProjectile（默认投射物）继续复用现有远程类测试芯片使用的共享投射物。
```

---

### Task 4: 跑专项测试与构建

**Files（文件）:**

- Verify（验证）: `模组工程/BorderDefenseProtocol/Source/BDP.Tests/DevHarnessExplosiveRangedModuleSmokeTests.ps1`
- Build（构建）: `模组工程/BorderDefenseProtocol.DevHarness/Source/BDP.DevHarness/BDP.DevHarness.csproj`

**Step 1: Run smoke test（运行烟雾测试）**

Run（运行）:

```powershell
& '.\Source\BDP.Tests\DevHarnessExplosiveRangedModuleSmokeTests.ps1'
```

Expected（期望）:

```text
PASS（通过）
```

**Step 2: Build DevHarness（构建开发测试模组）**

Run（运行）:

```powershell
dotnet msbuild '..\BorderDefenseProtocol.DevHarness\Source\BDP.DevHarness\BDP.DevHarness.csproj'
```

Expected（期望）:

```text
Build succeeded（构建成功）
```

---

### Task 5: 游戏内验收

**Files（文件）:**

- Check（检查）: `模组工程/BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_Combat.xml`

**Step 1: Load chip（装载芯片）**

在游戏内给测试角色装载：

```text
BDP_TestChipExplosiveRanged（测试爆炸远程芯片）
```

**Step 2: Aim preview（瞄准预览）**

验收：

- 鼠标悬停目标点时显示爆炸覆盖格。
- 覆盖范围与配置半径一致。
- 不出现重复的原版范围圈。

**Step 3: Impact explosion（命中爆炸）**

验收：

- 子弹命中目标、墙体、地面或自然终结时，在实际终结点爆炸。
- 爆炸伤害走原版 `GenExplosion.DoExplosion（原版爆炸执行接口）`。
- 默认没有额外单体子弹伤害叠加。

---

## 完成定义

- 代码能编译。
- 烟雾测试通过。
- XML 能被解析。
- 游戏内瞄准有范围预览。
- 游戏内命中有爆炸。
- 主模组公共协议没有为这个业务新增专用分支。
