---
元信息:
  标题: BDP模组工具类提取重构计划
  标签: [重构, 工具类, 代码质量]
  创建日期: 2026-03-10
  状态: 执行中
  优先级: 高
---

# BDP模组工具类提取重构计划

## 一、目标

通过深入分析132个C#源文件,识别并提取重复代码为工具类,提高代码可维护性和可复用性。

## 二、实施策略

### 阶段划分

**阶段1: 高收益低风险项 (P0-P1)**
- 新建核心工具类
- 提取完全重复的代码
- 每个工具类独立提交

**阶段2: 架构优化项 (P1-P2)**
- 引入中间抽象层
- 扩展已有类
- 分批提交测试

**阶段3: 跨模块解耦 (P2-P3)**
- 增加接口层
- 重构耦合点
- 谨慎推进

### 版本管理策略

- 分支: `refactor/utility-extraction-2026-03-10`
- 提交粒度: 每个工具类一次提交
- 提交信息格式: `refactor(module): add UtilityClassName - brief description`
- 关键节点打tag: `v-refactor-phase1-complete`

## 三、详细任务清单

### P0: 最高优先级 (立即执行)

#### Task 1: CombatBodyQuery 工具类
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Combat/Utils/CombatBodyQuery.cs`

**方法列表**:
```csharp
public static class CombatBodyQuery
{
    // 消除6处重复
    public static bool IsCombatBodyActive(Pawn pawn);

    // 消除2处重复
    public static bool IsCollapsing(Pawn pawn);

    // 消除3处重复
    public static ICombatBodySupport FindCombatBodySupport(Pawn pawn);

    // 消除8处重复
    public static bool HasValidHediffSet(Pawn pawn);

    // 消除2处100%重复代码
    public static void InterruptCurrentAction(Pawn pawn, string reason);
}
```

**影响文件**:
- `CombatBodyOrchestrator.cs`
- `Hediff_CombatBodyCollapsing.cs`
- `Patch_Pawn_PreApplyDamage.cs`
- `Patch_Pawn_PostApplyDamage.cs`
- `Patch_HealthUtility_GetHediffDefFromDamage.cs`
- `Patch_HealthCardUtility_DrawHediffRow.cs`
- `HediffComp_TrionDamageCost.cs`
- `HediffComp_TrionWoundDrain.cs`
- `CombatBodyActivationChecker.cs`
- `Patch_EquipmentTracker_TryDropEquipment.cs`

**预计收益**: 消除20+处重复

---

#### Task 2: CompTrion.TryConsume 扩展
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Comps/CompTrion.cs`

**新增方法**:
```csharp
/// <summary>
/// 尝试消耗Trion。如果Available不足则返回false且不消耗。
/// </summary>
public bool TryConsume(float amount)
{
    if (amount <= 0f) return true;
    if (Available < amount) return false;
    Consume(amount);
    return true;
}
```

**影响文件**:
- `Verb_BDPShoot.cs`
- `Verb_BDPVolley.cs`
- `Verb_BDPDualRanged.cs`
- `Verb_BDPDualVolley.cs`
- `Verb_BDPDualMixed.cs`
- `Verb_BDPComboShoot.cs`

**预计收益**: 统一5+处Trion消耗逻辑

---

#### Task 3: TriggerBodyQueries 工具类
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Utils/TriggerBodyQueries.cs`

**方法列表**:
```csharp
public static class TriggerBodyQueries
{
    // 消除13+处重复
    public static TriggerChipComp GetChipComp(this ChipSlot slot);
    public static CompProperties_TriggerChip GetChipProps(this ChipSlot slot);

    // 消除12+处重复
    public static VerbChipConfig GetVerbChipConfig(CompTriggerBody triggerComp, SlotSide side);

    // 消除9处重复
    public static (VerbChipConfig left, VerbChipConfig right) GetDualChipConfigs(CompTriggerBody triggerComp);

    // 消除8+处重复
    public static (ChipSlot left, ChipSlot right) GetActiveHandSlots(CompTriggerBody triggerComp);

    // 消除2处重复(继承链分叉导致)
    public static CompTriggerBody GetTriggerComp(Pawn pawn);
}
```

**影响文件**:
- 所有 `CompTriggerBody.*.cs` partial文件
- 所有 `Verb_BDP*.cs` 文件
- `DualVerbCompositor.cs`

**预计收益**: 消除40+处重复查询链

---

### P1: 高优先级

#### Task 4: HediffCleanupHelper 工具类
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Combat/Utils/HediffCleanupHelper.cs`

**方法列表**:
```csharp
public static class HediffCleanupHelper
{
    // 消除4处"收集-移除"二阶段模式
    public static void RemoveHediffsWhere(Pawn pawn, Predicate<Hediff> predicate);

    // 消除2处重复
    public static bool AnyAncestorIsUnavailable(Pawn pawn, BodyPartRecord part);
}
```

**影响文件**:
- `CombatBodyOrchestrator.cs` (ExtinguishFire, FinalCleanupResidualHediffs)
- `CombatBodySnapshot.cs` (RemoveAllHediffsExceptExcluded, RestoreHediffs)
- `CombatBodyDebugActions.cs`

---

#### Task 5: BDPTargetHelper 工具类
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Projectiles/Utils/BDPTargetHelper.cs`

**方法列表**:
```csharp
public static class BDPTargetHelper
{
    // 消除2处完全相同代码
    public static bool IsTargetValid(LocalTargetInfo target);

    // 消除2处等价代码
    public static bool IsTargetAligned(LocalTargetInfo current, LocalTargetInfo locked);
}
```

**影响文件**:
- `TrackingModule.cs`
- `VanillaAdapter.cs`
- `Bullet_BDP.cs` (DeriveAndSetPhase内联代码)

---

#### Task 6: BDPAngleHelper 工具类
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Projectiles/Utils/BDPAngleHelper.cs`

**方法列表**:
```csharp
public static class BDPAngleHelper
{
    /// <summary>角度(度)→XZ平面单位方向向量(0度=北/+Z方向)</summary>
    public static Vector3 AngleToDirection(float angleDeg);

    /// <summary>XZ平面方向向量→角度(度)</summary>
    public static float DirectionToAngle(Vector3 dirXZ);
}
```

**影响文件**:
- `TrackingModule.cs` (7处角度转换)

---

### P2: 中优先级 (架构优化)

#### Task 7: Verb_BDPDualBase 中间抽象层
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/DualWeapon/Verb_BDPDualBase.cs`

**继承关系**:
```
Verb_LaunchProjectile
  └─ Verb_BDPRangedBase
      └─ Verb_BDPDualBase (新增)
          ├─ Verb_BDPDualRanged
          ├─ Verb_BDPDualVolley
          └─ Verb_BDPDualMixed
```

**上提方法** (30+处重复):
- `GetGuidedConfig()` override (3处完全相同)
- `GetAutoRouteProjectileDef()` override (3处完全相同)
- `OnProjectileLaunched()` override (3处完全相同)
- `GetLosCheckTarget()` override (3处完全相同)
- `TryStartCastOn()` 公共部分
- `WithTargetRestore(Action)` 辅助方法
- `WithProjectileSwap(ThingDef, Func<bool>)` 辅助方法
- `FireVolleyLoop(count, spread, chip)` 辅助方法
- `InitDualBurstLOS(...)` 辅助方法
- `IsSideGuided(SlotSide)` 辅助方法

**影响文件**:
- `Verb_BDPDualRanged.cs`
- `Verb_BDPDualVolley.cs`
- `Verb_BDPDualMixed.cs`

**预计收益**: 消除30+处重复,三个子类代码量减少40%

---

#### Task 8: CompTriggerBody 内部简化
**涉及文件**: 多个 `CompTriggerBody.*.cs` partial文件

**子任务**:
1. 增加 `WithActivatingContext(ChipSlot, Action)` (消除3处重复)
2. 合并 `ForceDeactivateLeftSlots/RightSlots` 为 `ForceDeactivateSideSlots(SlotSide, string)` (消除对称重复)
3. 删除 `ClearSideVerbs`,用 `SetSideVerbs(side, null, null)` 替代
4. 统一 `DeactivateAllSpecial` 和 `ICombatBodySupport.DeactivateSpecialSlots`
5. 提升 `GetSideLabel(SlotSide)` 可见性为internal

---

### P3: 低优先级 (代码卫生)

#### Task 9: 删除重复的Debug HediffRecord类
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Combat/Debug/CombatBodyDebugActions.cs`

删除私有 `HediffRecord` 类,改用 `CombatBodySnapshot.HediffRecord`。

---

#### Task 10: 事件参数类独立
**文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Core/Events/BDPEventArgs.cs` (新建)

将 `CanActivateCombatBodyEventArgs` 等从 `Gene_TrionGland.cs` 移出。

---

## 四、风险控制

### 编译验证
每完成一个Task立即编译:
```bash
cd "模组工程/BorderDefenseProtocol/Source/BDP"
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

### 回滚策略
- 每个Task独立提交
- 如遇问题可单独回滚: `git revert <commit-hash>`
- 保留原分支: `refactor/projectiles-firemode-independence`

### 测试检查点
- Phase 1完成后: 游戏内基础功能测试
- Phase 2完成后: 双武器系统完整测试
- Phase 3完成后: 跨模块交互测试

## 五、执行时间线

- **Day 1 (今天)**: P0任务 (Task 1-3)
- **Day 2**: P1任务 (Task 4-6)
- **Day 3**: P2任务 (Task 7-8)
- **Day 4**: P3任务 + 全面测试

---

历史记录:
- 2026-03-10: 创建计划文档,开始执行 - Claude Sonnet 4.6
