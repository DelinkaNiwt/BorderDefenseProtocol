# Trigger 身体约束语义解析实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 把 Trigger 槽位禁用判定从“身体部位字符串猜测”改成“可操作肢体链语义解析”，从根上消除 Milira / 自定义种族 body 定义导致的误判，同时不破坏现有信号与同步边界。

**Architecture:** 这次只替换 `BodyConstraints` 内部的“判定模型”，不改 `MissingPartChanged` 的事实信号职责，不改 `TriggerDisableSync` 的状态写入职责。新增一个仅在 `BodyConstraints` 内部使用的语义解析器，负责两件事：识别缺失部位是否属于可操作肢体链；解析该链对应的左右侧。禁止把 race 特判、UI 文本、显示标签判断继续扩散到 Trigger 业务层。

**Tech Stack:** C#、RimWorld/Verse、Harmony、PowerShell smoke tests、`dotnet msbuild`

---

## 架构红线

- 不改 `Source/BDP/Patches/Patch_HediffSet_AddDirect_BodyConstraintSignal.cs` 与 `Source/BDP/Patches/Patch_Pawn_HealthTracker_RemoveHediff_BodyConstraintSignal.cs` 的职责：它们只发“缺失部位变化”事实，不做业务判断。
- 不改 `Source/BDP/Core/Trigger/Switching/Flow/TriggerDisableSync.cs` 的职责：它只消费评估结果并回写槽位状态，不解析种族 body。
- `TriggerBodyDisableEvaluator` 不再直接读 `LabelShort` / UI 标签做真值判断。
- 不引入任何 race 白名单 / 黑名单分支；Milira、HAR、原版都走同一套语义解析。
- 语义解析器只放在 `Source/BDP/Core/BodyConstraints/`，不把身体结构知识泄漏到 Trigger 运行时、GUI、CombatBody、Trion。

### Task 1: 锁定当前根因与边界回归

**Files:**
- Create: `Source/BDP.Tests/TriggerBodyConstraintSemanticResolutionSmokeTests.ps1`
- Modify: `Source/BDP.Tests/TriggerPureReadBoundarySmokeTests.ps1`
- Read-only reference: `Source/BDP/Core/BodyConstraints/TriggerBodyDisableEvaluator.cs`
- Read-only reference: `Source/BDP/Core/Trigger/Switching/Flow/TriggerDisableSync.cs`

**Step 1: 写失败测试，锁定现状问题**

- 断言 `TriggerBodyDisableEvaluator` 当前仍依赖硬编码部位名：`Hand` / `Arm` / `Shoulder`。
- 断言当前仍依赖 `LabelShort` 或文本包含 `left/right` 做侧别推断。
- 断言 Milira 类问题属于“字符串模型失真”，不是 `TriggerDisableSync` 没有执行。

**Step 2: 写边界测试，防止后续越层**

- 断言 `Patch_HediffSet_AddDirect_BodyConstraintSignal` 和 `Patch_Pawn_HealthTracker_RemoveHediff_BodyConstraintSignal` 只发布 `MissingPartChanged`。
- 断言 `TriggerDisableSync` 继续只调用 evaluator 获取 `TriggerDisableReason`，不直接解析 `BodyPartRecord`。

**Step 3: 运行测试确认当前失败**

Run: `& '.\Source\BDP.Tests\TriggerBodyConstraintSemanticResolutionSmokeTests.ps1'`  
Expected: FAIL，指出当前实现依赖字符串部位名或 `LabelShort`。

Run: `& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'`  
Expected: PASS 或给出仅边界断言缺失的明确信号，但不应暴露架构越层。

### Task 2: 建立 BodyConstraints 内部语义解析器

**Files:**
- Create: `Source/BDP/Core/BodyConstraints/TriggerBodyPartSemanticResolver.cs`
- Create: `Source/BDP/Core/BodyConstraints/TriggerBodyPartSemanticResult.cs`
- Modify: `Source/BDP/Core/BodyConstraints/TriggerBodyDisableEvaluator.cs`

**Step 1: 定义最小语义结果模型**

- 新建 `TriggerBodyPartSemanticResult`，只表达：
  - `IsManipulationLimb`
  - `ResolvedSide`
  - `CanDisableTrigger`
- 不在这里混入 UI、slot、chip、trigger runtime 信息。

**Step 2: 实现最小解析器**

- 新建 `TriggerBodyPartSemanticResolver`，只接收 `BodyPartRecord`。
- 第一层职责：沿父链识别“当前缺失部位是否属于可操作肢体链”。
- 第二层职责：沿链解析左右侧。
- 解析依据优先使用结构语义与稳定锚点；禁止把 `LabelShort` / `customLabel` / 本地化显示文本当真值源。

**Step 3: 收口 evaluator**

- `TriggerBodyDisableEvaluator` 不再自己猜 `defName` 与 `left/right`。
- 它只遍历 `Hediff_MissingPart`，把 `missingPart.Part` 交给 resolver，再把结果折算成 `TriggerDisableReason`。

**Step 4: 跑最小测试**

Run: `& '.\Source\BDP.Tests\TriggerBodyConstraintSemanticResolutionSmokeTests.ps1'`  
Expected: PASS，说明 evaluator 已经退化为纯折算器，字符串猜测被移除。

### Task 3: 用语义样本覆盖原版与自定义 body

**Files:**
- Modify: `Source/BDP.Tests/TriggerBodyConstraintSemanticResolutionSmokeTests.ps1`
- Read-only reference: `参考资源/模组资源/Milira_米莉拉/1.6/Defs/BodyDefs/Bodies_Milira.xml`
- Read-only reference: `参考资源/模组资源/Milira_米莉拉/1.6/Defs/BodyDefs/Bodies_Milian.xml`
- Read-only reference: `参考资源/模组资源/Milira_米莉拉/1.6/Defs/BodyPartDefs/BodyParts_Milian.xml`
- Read-only reference: `参考资源/模组资源/HumanoidAlienRaces_外星人框架`

**Step 1: 加原版样本**

- 断言原版 `Hand` / `Arm` / `Shoulder` 所在链仍能正确映射到 `Main` / `Sub`。

**Step 2: 加 Milira 样本**

- 断言“部位 def 还是原版，但左右信息不在 `LabelShort`”的情况下，解析器仍能得到正确侧别。

**Step 3: 加 Milian 样本**

- 断言“部位 defName 已自定义，不再叫 `Hand`”的情况下，只要仍属于可操作肢体链，也能触发禁用。

**Step 4: 加负样本**

- 非可操作肢体链缺失不禁用 Trigger。
- 无法稳定判定侧别时返回“不禁用”，而不是乱禁用。
- `TriggerSide.Special` 继续永不受该规则影响。

**Step 5: 跑测试**

Run: `& '.\Source\BDP.Tests\TriggerBodyConstraintSemanticResolutionSmokeTests.ps1'`  
Expected: PASS，覆盖原版、Milira、Milian、负样本四类场景。

### Task 4: 做边界回归，确认耦合没有扩散

**Files:**
- Modify: `Source/BDP.Tests/TriggerPureReadBoundarySmokeTests.ps1`
- Modify: `Source/BDP.Tests/TriggerSingleTruthSmokeTests.ps1`
- Read-only reference: `Source/BDP/Core/BodyConstraints/TriggerBodyDisableEvaluator.cs`
- Read-only reference: `Source/BDP/Core/BodyConstraints/TriggerBodyPartSemanticResolver.cs`
- Read-only reference: `Source/BDP/Core/Trigger/Switching/Flow/TriggerDisableSync.cs`

**Step 1: 补纯读边界断言**

- 断言 `TriggerBodyPartSemanticResolver` 只存在于 `BodyConstraints` 目录内。
- 断言 `TriggerDisableSync` 不直接依赖 resolver。
- 断言 Trigger runtime 层没有新增 race/body 解析入口。

**Step 2: 补单一真值断言**

- 断言缺失部位真值仍来自 `Pawn.health.hediffSet`。
- 断言槽位禁用真值仍只由 evaluator 输出，经 `TriggerDisableSync` 写回。

**Step 3: 跑边界测试**

Run: `& '.\Source\BDP.Tests\TriggerPureReadBoundarySmokeTests.ps1'`  
Expected: PASS

Run: `& '.\Source\BDP.Tests\TriggerSingleTruthSmokeTests.ps1'`  
Expected: PASS

### Task 5: 最小编译与验收

**Files:**
- No code changes required unless verification reveals a concrete issue

**Step 1: 最小编译**

Run: `dotnet msbuild '.\Source\BDP\BDP.csproj' -p:Configuration=Debug -t:Build -v:minimal`  
Expected: BUILD SUCCEEDED

**Step 2: 手测清单**

- 原版人类缺失左手：只禁用对应侧 Trigger 槽位。
- 原版人类缺失右臂：只禁用对应侧 Trigger 槽位。
- Milira 缺失对应手部：正确禁用对应侧。
- Milian 缺失自定义手部：正确禁用对应侧。
- 缺失非可操作肢体链部位：不禁用 Trigger。
- `Special` 槽位始终不受影响。

**Step 3: 验收结论**

- 只有当 smoke tests 通过、编译通过、Milira/Milian 手测通过，并且 `Patch -> SignalHub -> Evaluator -> TriggerDisableSync` 这条边界没有新增反向依赖时，本计划才算完成。

## 实施顺序建议

1. 先锁失败测试，证明根因在 evaluator 的字符串模型  
2. 再把语义解析器收进 `BodyConstraints` 内部  
3. 再补原版 / Milira / Milian 样本回归  
4. 最后做边界测试和最小编译  

Plan complete and saved to `docs/plans/2026-04-07-trigger-body-constraint-semantic-resolution-implementation-plan.md`. Two execution options:

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

**Which approach?**
