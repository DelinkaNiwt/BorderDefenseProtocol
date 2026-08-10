# CombatBody 快照/回滚系统实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 在新 BDP 现有 `CombatBody` 宿主接缝上，重建旧 BDP 的快照、托管、前台替代、退出恢复能力，并让游戏中的玩家可见表象与旧 BDP 一致。

**Architecture:** 本次只在 `CompCombatBodyHost -> PawnCombatBodyBridge -> CombatBody/Snapshot` 这条宿主链上落地，不把快照逻辑塞进 `CombatBodyService`，也不把恢复逻辑塞进 `CombatBodySessionService`。原始基线保全与恢复由快照子系统负责，战斗体前台接管与撤场由前台替代层负责；两者都挂在宿主位，不新增第四个真值 owner，不引入通用备份框架。

**Tech Stack:** C#、RimWorld/Verse、Harmony、PowerShell smoke tests、`dotnet msbuild`

---

## 实施红线

- 只做 `CombatBody` 宿主侧的快照/回滚与前台替代，不把 `Trigger`、`Trion`、崩解、冷却业务再揉回来。
- 不重建旧版 `CombatBodySnapshot` 那种“大对象包办一切”。
- 不依赖基因作为本次正式实现前提。
- 旧版挂在基因扩展上的“战斗体前台预设外观来源”，迁到新 BDP 的 `CombatBody` 正式设施里。
- 允许为旧版表象等价补回最小设置项，但禁止顺手扩成新的大设置系统。
- 快照/回滚子系统本身只做两件事：
  - 记录进入前基线
  - 恢复进入前基线
- 何时调用记录、何时调用恢复、激活态标记何时挂上/摘下、战斗体如何收尾，都属于外部调用方，不属于快照/回滚子系统本身。

## 严格等价补充红线

- 本计划要求的不是“大方向像”，而是旧版关键步骤级结果也要对齐。
- 对快照/回滚子系统本身，严格等价只体现在：
  - 记录哪些东西
  - 不记录哪些东西
  - 恢复哪些东西
  - 恢复顺序是否与旧版一致
- `Hediff` 排除规则不能退化成代码里写死的一张列表；旧版是数据驱动，新的正式实现也必须保持数据驱动。
- `BDP_CombatBodyActive` 虽然与快照流程发生时序邻接，但它本身不是快照/回滚子系统的职责对象。

## 旧版等价目标

实施完成后，游戏内必须满足这些旧版表象：

- 激活战斗体时，原本穿着与携带内容先被正式收起。
- 激活战斗体时，Pawn 前台出现战斗体替代层。
- 战斗体运行期间，原始内容被托管，不在前台混显。
- 退出战斗体时，先撤掉战斗体前台替代层。
- 退出战斗体时，再把进入前的原始穿着、携带、可恢复身体状态、可恢复需求值恢复回来。
- 被托管的可腐烂内容在托管期间不继续腐烂。
- 存档/读档后，只要这次战斗体会话仍有效，就还能按正式顺序恢复。

## 旧版事实迁移表

- 旧版原始基线快照来源：
  - `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`
- 旧版纳入/排除规则来源：
  - `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/BDPSnapshotConfigDef.cs`
  - `模组工程/BorderDefenseProtocol.Legacy/1.6/Defs/Combat/BDPSnapshotConfig.xml`
- 旧版托管期防腐来源：
  - `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/Patch_CompRottable.cs`
- 旧版前台模式来源：
  - `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/BDPModSettings.cs`
- 旧版前台装备定义来源：
  - `模组工程/BorderDefenseProtocol.Legacy/1.6/Defs/Combat/ThingDefs_CombatApparel.xml`
- 旧版预设前台衣物清单来源：
  - `模组工程/BorderDefenseProtocol.Legacy/1.6/Defs/Core/GeneDefs/GeneDef_TrionGland.xml`

新 BDP 不复刻这些旧落点，只迁移其中“行为事实”：

- 快照与恢复行为迁到 `CombatBody/Snapshot/*`
- 前台替代层迁到 `CombatBody` 宿主位
- 旧版挂在基因上的单一预设外观，迁成 `CombatBody` 自己的正式预设定义

### Task 1: 先锁定边界与回归口

**Files:**
- Create: `Source/BDP.Tests/CombatBodySnapshotRestoreSmokeTests.ps1`
- Create: `Source/BDP.Tests/CombatBodyFrontReplacementSmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodySessionThinFacadeBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodySessionContractsSmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`

**Step 1: 写失败的结构烟雾测试**
- 断言将新增 `CombatBody/Snapshot/` 目录与三个正式组件：
  - `CombatBodySnapshotState`
  - `CombatBodySnapshotService`
  - `CombatBodySnapshotPolicy`
- 断言 `CombatBodyFrontState` 是正式内容，不继续混在快照状态里。
- 断言 `CombatBodyService` 仍只通过 `ICombatBodyHost.ApplyCombatBodyTransformation()` / `RestoreFromCombatBody()` 做宿主调用。
- 断言 `CombatBodySessionService` 不直接读写快照状态，不直接承接衣物/背包/Hediff/Need 恢复。
- 断言紧急退出路径最终仍走宿主恢复，而不是把恢复逻辑塞回 Session 层。

**Step 2: 运行测试确认当前失败**

Run: `& '.\Source\BDP.Tests\CombatBodySnapshotRestoreSmokeTests.ps1'`  
Expected: FAIL，因为快照目录与正式组件尚不存在。

Run: `& '.\Source\BDP.Tests\CombatBodyFrontReplacementSmokeTests.ps1'`  
Expected: FAIL，因为前台替代层正式状态尚不存在。

Run: `& '.\Source\BDP.Tests\CombatBodySessionThinFacadeBoundarySmokeTests.ps1'`  
Expected: FAIL 或提示当前边界尚未覆盖快照能力。

### Task 2: 建立最小快照状态壳，不碰会话层职责

**Files:**
- Create: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotState.cs`
- Create: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotPolicy.cs`
- Create: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotService.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`

**Step 1: 先写最小状态模型**
- 在 `CombatBodySnapshotState` 中只放本次激活要托管与恢复的数据：
  - 原始衣物托管容器
  - 原始背包托管容器
  - 衣物恢复标记
  - 物品恢复标记
  - Hediff 基线记录
  - Need 基线记录
  - `hasSnapshot` 标记
- 不把 `CombatBodyPhase`、`Trion`、`Trigger`、崩解原因、前台替代层塞进这里。

**Step 2: 写最小 policy**
- 让 `CombatBodySnapshotPolicy` 只做“纳入/排除判断”。
- 不把排除规则写死在 C# 常量里。
- 直接按旧版机制重建为数据驱动：
  - 新建 `CombatBodySnapshotConfigDef`
  - 读取 `DefDatabase<CombatBodySnapshotConfigDef>.AllDefs`
  - 合并 `excludedHediffs`
  - 合并 `excludedHediffClasses`
- 第一版默认配置值与旧版保持一致：
  - `PsychicAmplifier`
  - `MechlinkImplant`
  - `BDP_CombatBodyActive`
  - `Verse.Hediff_Psylink`
  - `Verse.Hediff_Mechlink`

**Step 2.5: 补默认配置 Def**
- 新建默认配置 XML，作为旧版 `BDPSnapshotConfig.xml` 的正式迁移落点。
- 允许后续其他 Def 继续追加排除规则，保持与旧版一样的动态扩展能力。

**Step 3: 写最小 service 壳**
- 在 `CombatBodySnapshotService` 暴露两个入口：
  - `CaptureForActivation(Pawn pawn)`
  - `RestoreForDeactivation(Pawn pawn)`
- 第一刀先只把调用顺序搭起来，不急着一次写全细节。

**Step 4: 把快照状态正式挂到宿主位**
- `CompCombatBodyHost` 新增：
  - `snapshotState`
  - `snapshotPolicy`
  - `snapshotService`
- `PostExposeData()` 先把 `snapshotState` 纳入存读档。
- 仍不让 `CombatBodySessionService` 知道这些对象。

**Step 5: 运行最小结构测试**

Run: `& '.\Source\BDP.Tests\CombatBodySnapshotRestoreSmokeTests.ps1'`  
Expected: PASS on structure assertions, even if具体恢复行为还未全通。

### Task 3: 接回宿主桥，让激活/退出真正经过快照链

**Files:**
- Modify: `Source/BDP/Core/CombatBody/Bridge/PawnCombatBodyBridge.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/ICombatBodyHost.cs`（仅在确有必要时；能不改就不改）

**Step 1: 重构桥接构造**
- 让 `PawnCombatBodyBridge` 拿到宿主位需要的快照/前台状态，而不是只拿一个 `Pawn`。
- 优先改构造注入，不新增全局定位器。

**Step 2: 接入激活顺序**
- `ApplyCombatBodyTransformation()` 内固定顺序：
  1. 捕获原始基线
  2. 收起原始前台内容
  3. 建立战斗体前台替代层
- 其中快照子系统只负责第 1 步里的“捕获基线”。
- 清理 `Hediff`、添加 `BDP_CombatBodyActive` 若要继续存在，也属于外部宿主/战斗体流程，不属于快照子系统。

**Step 3: 接入退出顺序**
- `RestoreFromCombatBody()` 内固定顺序：
  1. 撤掉战斗体前台替代层
-  2. 恢复原始基线
-  3. 清空本次快照
- 其中快照子系统只负责第 2 步里的“恢复基线”。
- `ExtinguishFire`、战斗期 `Hediff` 清理、`BDP_CombatBodyActive` 移除、最终残留清理，若外部系统仍需要，属于外部流程责任，不属于快照子系统。

**Step 4: 跑边界测试**

Run: `& '.\Source\BDP.Tests\CombatBodySessionThinFacadeBoundarySmokeTests.ps1'`  
Expected: PASS，说明快照逻辑落在宿主桥，而不是 Session 层。

Run: `& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'`  
Expected: PASS，说明 `CombatBodyService` 对外行为口没有变胖。

### Task 4: 实现原始穿戴与原始背包的对称托管/恢复

**Files:**
- Modify: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotState.cs`
- Modify: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotService.cs`

**Step 1: 实现穿戴捕获**
- 激活前遍历 Pawn 当前穿着。
- 记录恢复所需的旧标记：
  - `wasLocked`
  - `wasForced`
- 把原始衣物移入快照托管容器。

**Step 2: 实现背包捕获**
- 激活前遍历 Pawn 当前携带物。
- 记录恢复所需旧标记：
  - `wasNotForSale`
  - `wasUnpackedCaravan`
- 把原始物品移入快照托管容器。

**Step 3: 实现退出恢复**
- 退出恢复顺序必须和旧版写死为同一顺序：
  - 先恢复原始衣物
  - 再恢复原始背包
  - 再恢复 Need
  - 最后恢复 Hediff
- 恢复时把旧标记按原值写回。
- 恢复完成后清空托管容器和状态字典。

**Step 4: 处理“只恢复进入前基线”**
- 恢复时只按快照里的原始清单恢复。
- 不尝试保留战斗体期间临时产生的原始层变化。
- 不做“合并当前真值”和“智能保留新东西”。

**Step 5: 跑针对性测试**

Run: `& '.\Source\BDP.Tests\CombatBodySnapshotRestoreSmokeTests.ps1'`  
Expected: PASS，断言原始衣物/背包与旧标记的对称托管存在。

### Task 5: 实现 Hediff 与 Need 的正式基线恢复

**Files:**
- Modify: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotState.cs`
- Modify: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotPolicy.cs`
- Modify: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotService.cs`

**Step 1: 把旧版 Hediff 记录字段逐项迁进来**
- 按旧实现只记录旧版明确保存过的字段。
- 不新增任何旧版没保存的“推测性字段”。
- 旧版 `HediffRecord` 的字段迁移必须逐项对照 `CombatBodySnapshot.cs` 实现完成。

**Step 2: 激活时捕获可恢复 Hediff 基线**
- 只纳入 policy 允许的 Hediff。
- 捕获后按旧版语义清掉非排除项，为战斗体表现层腾出空间。

**Step 3: 激活时捕获 Need 基线**
- 记录旧版实际保存过的 Need 当前值。
- 不新增其它生活系统的镜像或冻结层。

**Step 4: 退出时恢复 Hediff 与 Need**
- 恢复顺序不再写成抽象语义，直接按旧版真实顺序写死：
  - 恢复衣物
  - 恢复背包
  - 恢复 Need
  - 恢复 Hediff
- 只恢复快照里有记录的可恢复项。
- 不恢复被 policy 明确排除的东西。
- 这里讨论的“恢复”严格限于快照中记录过的原始基线，不包含战斗体退出收尾动作。

**Step 5: 跑测试**

Run: `& '.\Source\BDP.Tests\CombatBodySnapshotRestoreSmokeTests.ps1'`  
Expected: PASS，断言旧版排除规则与 Hediff/Need 基线恢复路径存在。

### Task 6: 把战斗体前台替代层从快照里正式拆出来

**Files:**
- Create: `Source/BDP/Core/CombatBody/Bridge/CombatBodyFrontState.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/PawnCombatBodyBridge.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`

**Step 1: 建立前台状态**
- `CombatBodyFrontState` 只保存：
  - 当前替代前台物列表
  - 退出时清理所需的最小信息
  - `isApplied` 标记

**Step 2: 激活时建立前台替代层**
- 建立方式只允许两种旧版表象：
  - `Preset`
  - `MirrorOriginal`
- 这层是战斗体正式前台，不回塞到快照状态里。

**Step 2.5: 把旧版前台装备专门配置逐项落到新实现语义**
- `Preset` 模式下，不在桥里手写这些数值，而是继续由正式 `ThingDef` 承载：
  - 贴图：`graphicData.texPath`
  - 穿着贴图：`apparel.wornGraphicPath`
  - 覆盖部位：`bodyPartGroups`
  - 穿戴层：`layers`
  - 标签：`tags`
  - 是否可损：`useHitPoints`
  - 最大耐久：`MaxHitPoints`
  - 质量：通过 `ApparelNoQualityBase` 保持“无品质”
  - 可染色能力：通过父类继承的 `CompColorable`
  - 其它数值：护甲、保温、移速、质量、阻燃、腐朽率、交易性等
- `MirrorOriginal` 模式下，必须按旧代码事实逐项保留：
  - 复制 `def`
  - 复制 `Stuff`
  - 复制 `CompColorable` 的当前颜色
  - 复制 `StyleDef`
  - 显式移除 `CompQuality`
  - 不复制原件当前品质
  - 不把原件当前耐久值回写到副本上

**Step 3: 退出时撤场**
- 先把当前前台替代层安全移除。
- `MirrorOriginal` 生成的临时副本退出时销毁。
- `Preset` 模式使用的正式前台物退出时回到前台状态持有或做最小清理，不污染原始快照托管区。

**Step 4: 跑测试**

Run: `& '.\Source\BDP.Tests\CombatBodyFrontReplacementSmokeTests.ps1'`  
Expected: PASS，断言前台替代层是正式独立状态，不再混在 snapshot 里。

### Task 7: 给前台预设来源和预设装备本体一个新的正式落点，但不回到基因

**Files:**
- Create: `Source/BDP/Core/CombatBody/Bridge/CombatBodyFrontPresetDef.cs`
- Create: `1.6/Defs/Core/CombatBodyFrontPresetDefs.xml`
- Create: `1.6/Defs/Core/ThingDefs_CombatBodyFrontApparel.xml`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompProperties_CombatBodyHost.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/PawnCombatBodyBridge.cs`

**Step 1: 新建正式预设 Def**
- 定义 `CombatBodyFrontPresetDef`，只承载前台预设衣物清单。
- 第一版只迁移旧版单一已知预设事实：
  - `BDP_CombatBodyArmor`

**Step 1.5: 迁移旧版预设装甲本体 Def**
- 把旧版 `ThingDefs_CombatApparel.xml` 中 `BDP_CombatBodyArmor` 的正式定义迁入新 BDP。
- 第一版至少保持与旧版同一组显式字段：
  - `ParentName=ApparelNoQualityBase`
  - `graphicData.texPath=Things/Pawn/Humanlike/Apparel/Duster/Duster`
  - `apparel.wornGraphicPath=Things/Pawn/Humanlike/Apparel/Duster/Duster`
  - `MaxHitPoints=99999`
  - `Mass=0`
  - `Flammability=0`
  - `DeteriorationRate=0`
  - `ArmorRating_Sharp=1.20`
  - `ArmorRating_Blunt=0.50`
  - `ArmorRating_Heat=0.60`
  - `Insulation_Cold=20`
  - `Insulation_Heat=10`
  - `EquipDelay=0`
  - `MoveSpeed=0.20`
  - `useHitPoints=false`
  - `smeltable=false`
  - `bodyPartGroups={Torso,Shoulders,Arms,Legs}`
  - `layers={Middle,Shell}`
  - `tags={BDP_CombatBody}`
  - `CompProperties_Forbiddable`
  - `tradeability=None`

**Step 2: 给宿主配置一个预设指针**
- 在 `CompProperties_CombatBodyHost` 增加 `frontPresetDefName`。
- 默认指向 `BDP_DefaultCombatBodyFrontPreset`。
- 这让前台来源成为 `CombatBody` 自己的配置，而不是基因扩展。

**Step 3: 前台建立时按模式取源**
- `Preset`：从 `frontPresetDefName` 指向的正式预设取前台物。
- `MirrorOriginal`：按进入前原始穿着生成外观镜像副本。

**Step 4: 跑测试**

Run: `& '.\Source\BDP.Tests\CombatBodyFrontReplacementSmokeTests.ps1'`  
Expected: PASS，断言前台预设来源已经迁出基因。

**Step 5: 增加装备定义等价断言**
- 断言新仓中存在 `BDP_CombatBodyArmor` 的正式 `ThingDef`
- 断言上述关键字段没有丢
- 断言 `MirrorOriginal` 分支仍存在：
  - `Stuff` 复制
  - 颜色复制
  - `StyleDef` 复制
  - `CompQuality` 移除

### Task 8: 最小补回旧版表象所需设置，但不扩成大系统

**Files:**
- Create: `Source/BDP/BDPModSettings.cs`
- Modify: `Source/BDP/BDPMod.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/PawnCombatBodyBridge.cs`
- Modify: `Source/BDP.Tests/CombatBodyFrontReplacementSmokeTests.ps1`

**Step 1: 只补一个设置项**
- 新建最小 `BDPModSettings`。
- 只放一个正式设置：
  - `combatApparelMode`

**Step 2: 补最小设置入口**
- `BDPMod` 持有 `GetSettings<BDPModSettings>()`
- 若当前项目没有现成设置窗口，就只补最小读写与最小展示，不顺手扩展其它设置。

**Step 3: 前台建立时读这个设置**
- 让 `PawnCombatBodyBridge` 在激活前台替代层时读取：
  - `Preset`
  - `MirrorOriginal`
- 保持与旧版玩家可见模式一致。

**Step 4: 跑测试**

Run: `& '.\Source\BDP.Tests\CombatBodyFrontReplacementSmokeTests.ps1'`  
Expected: PASS，断言模式切换入口存在。

### Task 9: 补托管期保护，不让快照容器变成暗坑

**Files:**
- Create: `Source/BDP/Patches/Patch_CompRottable_CombatBodySnapshot.cs`
- Modify: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotState.cs`
- Modify: `Source/BDP.Tests/CombatBodySnapshotRestoreSmokeTests.ps1`

**Step 1: 明确快照托管容器的宿主类型**
- 让补丁能准确识别“当前物品是否处于 CombatBody 快照托管中”。

**Step 2: 迁移旧版最小防腐语义**
- 仅在物品被快照托管时，把 `CompRottable.Active` 压成 `false`。
- 不扩成更大的“统一仓储保护系统”。

**Step 3: 跑测试**

Run: `& '.\Source\BDP.Tests\CombatBodySnapshotRestoreSmokeTests.ps1'`  
Expected: PASS，断言托管期保护补丁存在并且只面向快照托管生效。

### Task 10: 补存读档恢复与针对性回归

**Files:**
- Modify: `Source/BDP/Core/CombatBody/Bridge/CompCombatBodyHost.cs`
- Modify: `Source/BDP/Core/CombatBody/Bridge/CombatBodyFrontState.cs`
- Modify: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotState.cs`
- Modify: `Source/BDP.Tests/CombatBodySnapshotRestoreSmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodyCollapseEmergencySmokeTests.ps1`
- Modify: `Source/BDP.Tests/CombatBodyTriggerTrionIntegrationSmokeTests.ps1`

**Step 1: 让宿主位存读档后能重建运行时链**
- `CompCombatBodyHost.PostExposeData()` 序列化：
  - `snapshotState`
  - `frontState`
- `PostLoadInit` 后重建：
  - `snapshotPolicy`
  - `snapshotService`
  - `PawnCombatBodyBridge`

**Step 2: 做紧急退出回归**
- 确认 `Collapsing -> Emergency Deactivate` 时，快照/回滚子系统仍只承担：
  - 前台撤场后的原始基线恢复
- 其余紧急退出收尾继续留在外部系统，不回灌进快照子系统。

**Step 3: 做 Trigger/Trion 边界回归**
- 断言本次快照/回滚实现没有把：
  - Trigger 槽位真值
  - Trion 账本真值
  - Session 事务顺序
 重新吞回宿主实现里。

**Step 4: 跑所有相关 smoke tests**

Run: `& '.\Source\BDP.Tests\CombatBodySnapshotRestoreSmokeTests.ps1'`  
Expected: PASS

Run: `& '.\Source\BDP.Tests\CombatBodyFrontReplacementSmokeTests.ps1'`  
Expected: PASS

Run: `& '.\Source\BDP.Tests\CombatBodySessionContractsSmokeTests.ps1'`  
Expected: PASS

Run: `& '.\Source\BDP.Tests\CombatBodySessionThinFacadeBoundarySmokeTests.ps1'`  
Expected: PASS

Run: `& '.\Source\BDP.Tests\CombatBodyCollapseEmergencySmokeTests.ps1'`  
Expected: PASS

Run: `& '.\Source\BDP.Tests\CombatBodyTriggerTrionIntegrationSmokeTests.ps1'`  
Expected: PASS

### Task 11: 把旧版动态排除配置正式迁进新仓

**Files:**
- Create: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotConfigDef.cs`
- Create: `1.6/Defs/Core/CombatBodySnapshotConfigDefs.xml`
- Modify: `Source/BDP/Core/CombatBody/Snapshot/CombatBodySnapshotPolicy.cs`
- Modify: `Source/BDP.Tests/CombatBodySnapshotRestoreSmokeTests.ps1`

**Step 1: 建立正式 Def**
- 定义：
  - `excludedHediffs`
  - `excludedHediffClasses`
- 与旧版 `BDPSnapshotConfigDef` 语义保持一致。

**Step 2: Policy 改为读 DefDatabase**
- 用 `DefDatabase<CombatBodySnapshotConfigDef>.AllDefs` 构建缓存。
- 支持多个配置 Def 合并。
- 类名排除继续使用“父类可覆盖子类”的匹配方式。

**Step 3: 默认配置与旧版一致**
- 默认 XML 迁移旧版那组排除项。
- 但保留继续追加其他 Def 的能力。

**Step 4: 跑测试**

Run: `& '.\Source\BDP.Tests\CombatBodySnapshotRestoreSmokeTests.ps1'`  
Expected: PASS，断言排除规则不是代码写死，而是正式 Def 配置驱动。

### Task 12: 最小编译与游戏内验收清单

**Files:**
- No code changes required unless verification reveals a concrete issue

**Step 1: 最小编译**

Run: `dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal`  
Expected: BUILD SUCCEEDED

**Step 2: 游戏内手测清单**
- 进入战斗体前，穿上多件衣物并携带可腐烂物。
- 激活战斗体，确认原始穿戴与携带层退场。
- 观察前台替代层是否按当前模式出现：
  - `Preset`
  - `MirrorOriginal`
- 在战斗体期间制造 Hediff/Need 变化后退出，确认只恢复进入前基线，不恢复排除项。
- 退出战斗体，确认原始穿戴/携带/身体/需求恢复。
- 紧急退出一遍，确认顺序仍对。
- 激活后存档、读档、退出，确认仍能正式恢复。

**Step 3: 验收结论**
- 只有当 smoke tests 通过、编译通过、手测表象与旧版一致时，才算本计划完成。

## 设计文档是否需要修改

需要一处小修，不需要重写：

- 只补一条设计澄清：**本次实现不依赖基因；旧版基因扩展上的前台预设来源迁到 `CombatBody` 正式设施。**
- 其余设计主干不需要推倒重来。

## 执行顺序建议

严格按下面顺序推进，不要跳：

1. 先补 smoke tests 锁边界
2. 再建快照状态/服务壳
3. 再把桥接顺序接通
4. 再做原始衣物/背包对称恢复
5. 再做 Hediff/Need 基线恢复
6. 再拆出正式前台替代层
7. 再补前台预设来源与模式设置
8. 最后补托管保护、存读档、编译与手测

Plan complete and saved to `docs/plans/2026-04-06-combatbody-snapshot-restore-implementation-plan.md`. Two execution options:

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

**Which approach?**
