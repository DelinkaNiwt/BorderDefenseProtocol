---
标题：战斗体伤害系统重构方案 — 回归原版系统
版本号: v1.0
更新日期: 2026-03-06
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 将战斗体伤害系统从"影子HP+自定义伤口+全拦截"架构重构为"原版伤害+Hediff效果覆写+最小拦截"架构，借鉴Biotech基因效果模式，最大化复用原版系统。
---

# 战斗体伤害系统重构方案

## 一、现状诊断

### 1.1 当前架构的核心问题

当前战斗体伤害系统采用**全拦截+影子系统**架构：

```
原版伤害流程                          当前BDP做法
─────────────────                    ─────────────────
DamageWorker.FinalizeAndAddInjury    Prefix返回false（全拦截）
  → pawn.health.AddHediff(injury)      → 伤害从不进入原版系统
                                       → 转入影子HP系统
                                       → 创建自定义BDP_CombatWound
```

**问题清单**：

| 问题 | 原因 | 影响 |
|------|------|------|
| 与其他模组的伤害系统完全不兼容 | Prefix返回false截断整个伤害链 | 依赖PostApplyDamage的模组全部失效 |
| 维护了一套平行HP系统（影子HP） | 不信任原版HP追踪 | 220行额外代码，需要独立序列化 |
| 9种自定义伤口HediffDef | 不使用原版Hediff_Injury | 原版治疗、医疗AI全部失效 |
| 自定义伤口→Trion流失的映射表 | 重新发明出血计算 | WoundAdapter 60行映射代码 |
| 部位破坏使用自定义Hediff | 不使用原版Hediff_MissingPart的正常流程 | 健康面板显示异常、原版AI不识别 |
| UI颜色Patch | 影子HP与真实HP不同步 | 额外Patch维护成本 |

**本质问题**：不信任原版系统，于是在原版之上构建了一套平行系统。维护成本高，兼容性差。

### 1.2 原版已提供但未被利用的能力

通过深入研究原版代码（含Biotech DLC），发现以下关键机制完全可以满足需求：

| 需求 | 原版机制 | 代码位置 |
|------|---------|---------|
| 不死 | `HediffDef.preventsDeath = true` | `Pawn_HealthTracker.ShouldBeDead()` 第629行 |
| 不流血 | `HediffStage.totalBleedFactor = 0` | `HediffSet.CalculateBleedRate()` 第1321行 |
| 不痛 | `HediffStage.painFactor = 0` | 已在使用 |
| 阻止精神崩溃 | `HediffStage.blocksMentalBreaks = true` | 已在使用 |
| 伤口不影响部位效率 | `HediffStage.partIgnoreMissingHP = true` | `PawnCapacityUtility.CalculatePartEfficiency()` 第211行 |
| 能力值覆写 | `HediffStage.capMods[].offset` | `PawnCapacityUtility.CalculateCapacityLevel()` 第103行 |
| 阻止饥饿/疲劳 | `HediffStage.hungerRateFactor/restFallFactor = 0` | 已在使用 |
| 疾病免疫 | `HediffStage.makeImmuneTo` | 已在使用 |

**关键发现**：`preventsDeath = true` 会在 `ShouldBeDead()` 的最早期返回 `false`，比任何致死能力检查都优先。这意味着哪怕脑部缺失导致意识为0、心脏缺失导致血液循环为0，Pawn都不会死亡。原版就支持"不死"。

## 二、重构核心思路

### 2.1 设计原则

**从"拦截原版"到"骑乘原版"**

```
旧思路: 拦截伤害 → 自己算 → 自己存 → 自己显示
新思路: 让原版算 → 让原版存 → 让原版显示 → 只覆写"影响"
```

**借鉴对象：Biotech DLC 的基因效果模式**

基因系统的设计哲学：
1. 不修改原版代码
2. 通过 Hediff + HediffStage 的 capMods/statOffsets 改变行为
3. 通过 `preventsDeath` 阻止死亡
4. 通过 HediffComp 实现自定义逻辑
5. 通过 Gene.PostAdd/PostRemove 管理生命周期

战斗体应完全复刻这种模式：**战斗体 = 一个"临时基因效果包"**。

### 2.2 核心设计决策

| 决策 | 选择 | 原因 |
|------|------|------|
| 伤害是否拦截 | **不拦截**，让原版Hediff_Injury正常添加 | 保持伤害链完整，兼容其他模组 |
| HP追踪方式 | **使用原版HP**，删除影子HP | 减少200+行代码，消除同步问题 |
| 伤口表示 | **使用原版Hediff_Injury** | 原版治疗/医疗AI自动生效 |
| 出血处理 | **totalBleedFactor = 0** 抑制出血 | 一行XML配置解决 |
| 死亡阻止 | **preventsDeath = true** | 已在使用，无需额外代码 |
| 部位缺失 | **让原版自然处理MissingPart** | 删除自定义PartDestroyed hediff |
| Trion消耗 | **PostApplyDamage回调中按伤害量扣减** | 在原版流程之后执行，不干扰原版 |
| 伤口→Trion流失 | **HediffComp遍历原版伤口计算drain** | 利用原版injury追踪，无需自建 |
| 关键部位监控 | **HediffComp检测MissingPart** | 在原版处理之后检测结果 |
| 武器不掉落 | **保留TryDropEquipment Patch** | 这是原版不提供的特殊需求 |

## 三、新架构详细设计

### 3.1 核心 Hediff：BDP_CombatBodyActive（增强版）

**设计模式**：一个 HediffDef 承载所有战斗体效果，通过 HediffStage 配置行为覆写，通过 HediffComp 实现自定义逻辑。

**XML 定义（预期）**：
```xml
<HediffDef>
    <defName>BDP_CombatBodyActive</defName>
    <hediffClass>BDP.Combat.Hediff_CombatBodyActive</hediffClass>
    <preventsDeath>true</preventsDeath>

    <stages>
        <li>
            <!-- 效果覆写：全部通过原版HediffStage字段实现 -->
            <painFactor>0</painFactor>              <!-- 无痛 -->
            <totalBleedFactor>0</totalBleedFactor>   <!-- 无出血（NEW!） -->
            <hungerRateFactor>0</hungerRateFactor>   <!-- 无饥饿 -->
            <restFallFactor>0</restFallFactor>       <!-- 无疲劳 -->
            <blocksMentalBreaks>true</blocksMentalBreaks>

            <!-- 环境免疫 -->
            <statOffsets>
                <ComfyTemperatureMin>-200</ComfyTemperatureMin>
                <ComfyTemperatureMax>200</ComfyTemperatureMax>
                <ToxicResistance>1</ToxicResistance>
            </statOffsets>

            <!-- 疾病免疫 -->
            <makeImmuneTo>
                <li>Flu</li>
                <li>Plague</li>
                <!-- ...更多... -->
            </makeImmuneTo>
        </li>
    </stages>

    <comps>
        <!-- Comp 1: Trion伤害消耗 - 受伤时消耗Trion -->
        <li Class="BDP.Combat.HediffCompProperties_TrionDamageCost">
            <costPerDamage>0.5</costPerDamage>
        </li>

        <!-- Comp 2: Trion伤口流失 - 基于伤口总严重度的持续消耗 -->
        <li Class="BDP.Combat.HediffCompProperties_TrionWoundDrain">
            <drainPerSeverityPerDay>5.0</drainPerSeverityPerDay>
        </li>

        <!-- Comp 3: 破裂监控 - 监控Trion耗尽和关键部位破坏 -->
        <li Class="BDP.Combat.HediffCompProperties_RuptureMonitor">
            <criticalParts>
                <li>Head</li>
                <li>Brain</li>
                <li>Heart</li>
                <li>Neck</li>
                <li>Torso</li>
            </criticalParts>
        </li>
    </comps>
</HediffDef>
```

### 3.2 HediffComp 设计（三个核心组件）

#### HediffComp_TrionDamageCost

**职责**：受到伤害时消耗等比例的Trion。

**触发时机**：通过 Harmony Postfix 在伤害应用后调用。

```
原版伤害流程:
  DamageWorker → FinalizeAndAddInjury → AddHediff(injury) → PostApplyDamage
                                                                    ↓
                                                            [我们的Postfix]
                                                              读取 totalDamageDealt
                                                              消耗 Trion = damage × costPerDamage
                                                              若Trion不足 → 通知RuptureMonitor
```

**关键特征**：
- `costPerDamage` 在 XML 中配置
- 通过 CompProperties 暴露配置，无需硬编码
- 消耗失败时不阻止伤害（伤害已经发生），而是触发破裂

#### HediffComp_TrionWoundDrain

**职责**：基于Pawn当前原版伤口的总严重度，持续消耗Trion（替代"流血→失血"）。

**触发时机**：CompPostTick（低频，每250 ticks检查一次）。

```
每250 ticks:
  遍历 pawn.health.hediffSet.hediffs
    → 筛选 Hediff_Injury 和 Hediff_MissingPart（新鲜的）
    → 累计总 Severity
  totalDrain = totalSeverity × drainPerSeverityPerDay
  CompTrion.RegisterDrain("CombatWounds", totalDrain)  // 更新drain量
```

**替代了**：9个自定义伤口HediffDef + HediffComp_CombatWound + WoundAdapter。
**优势**：直接读取原版伤口数据，零维护成本。

#### HediffComp_RuptureMonitor

**职责**：监控战斗体破裂条件，统一触发破裂流程。

**两个破裂条件**：
1. **Trion耗尽**：由 HediffComp_TrionDamageCost 通知
2. **关键部位被摧毁**：检测原版 Hediff_MissingPart 出现在关键部位上

**触发时机**：
- 收到Trion耗尽通知时（即时）
- PostApplyDamage 回调中检查关键部位（即时）

**破裂流程**（复用现有逻辑）：
```
检测到破裂条件
  → 转换 CombatBodyState 到 Collapsing
  → 打断当前动作
  → 添加 BDP_CombatBodyCollapsing Hediff
  → (90 ticks后) Hediff_CombatBodyCollapsing 触发解除
```

### 3.3 Harmony Patch 变化

| Patch | 变化 | 说明 |
|-------|------|------|
| `Patch_DamageWorker_FinalizeAndAddInjury` | **删除** | 不再拦截原版伤害 |
| `Patch_HealthUtility_GetPartConditionLabel` | **删除** | 原版HP就是真实HP，无需自定义颜色 |
| `Patch_EquipmentTracker_TryDropEquipment` | **保留** | 武器不掉落是原版不提供的特殊需求 |
| `Patch_CompRottable` | **保留** | 快照容器防腐烂，独立于伤害系统 |
| `Patch_Pawn_PostApplyDamage` | **新增** | 伤害后消耗Trion + 检查破裂条件 |
| `Patch_Pawn_PreApplyDamage` | **新增** | Collapsing期间吸收所有伤害（无敌） |

**新Patch的实现思路**：

```csharp
// Patch 1: 伤害后处理（替代全拦截Prefix）
[HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
public static class Patch_Pawn_PostApplyDamage
{
    static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
    {
        // 检查战斗体是否激活
        var hediff = __instance.health.hediffSet
            .GetFirstHediffOfDef(BDP_DefOf.BDP_CombatBodyActive);
        if (hediff == null) return;

        // 委托给HediffComp处理
        // HediffComp_TrionDamageCost.OnDamageReceived(totalDamageDealt)
        // HediffComp_RuptureMonitor.CheckRuptureConditions()
    }
}

// Patch 2: 破裂期间无敌
[HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
public static class Patch_Pawn_PreApplyDamage_Collapsing
{
    static void Postfix(Pawn __instance, ref bool absorbed)
    {
        if (absorbed) return;
        // 检查是否处于Collapsing状态
        if (__instance.health.hediffSet
            .HasHediff(BDP_DefOf.BDP_CombatBodyCollapsing))
        {
            absorbed = true; // 吸收所有伤害
        }
    }
}
```

### 3.4 快照系统适配

**现有快照系统完全兼容新方案**：

```
激活时:
  SnapshotAll()             → 记录当前Hediff（含已有伤口）
  RemoveAllHediffsExcept()  → 清理到"干净"状态
  AddHediff(CombatBodyActive) → 添加战斗体Hediff

战斗中:
  原版自然添加 Hediff_Injury   → 伤口在Pawn真实健康列表中
  原版自然添加 Hediff_MissingPart → 缺失部位也在真实列表中
  totalBleedFactor = 0         → 但不出血
  preventsDeath = true         → 但不死

解除时:
  RemoveAllHediffsExcept()  → 清理战斗中的所有伤口和缺失部位
  RestoreAll()              → 恢复到激活前的健康状态
```

**关键点**：战斗中累积的原版伤口在解除时被快照恢复机制自然清理，无需额外代码。

## 四、文件变更清单

### 4.1 删除的文件（C# 源码）

| 文件 | 行数 | 原因 |
|------|------|------|
| `Combat/Damage/ShadowHPTracker.cs` | ~190 | 原版HP替代 |
| `Combat/Damage/Hediff_CombatWound.cs` | ~60 | 原版Hediff_Injury替代 |
| `Combat/Damage/HediffComp_CombatWound.cs` | ~80 | HediffComp_TrionWoundDrain替代 |
| `Combat/Damage/WoundAdapter.cs` | ~60 | 无需伤害类型映射 |
| `Combat/Damage/Handlers/WoundHandler.cs` | ~100 | 原版自动处理伤口 |
| `Combat/Damage/Handlers/ShadowHPHandler.cs` | ~50 | 影子HP删除 |
| `Combat/Damage/Handlers/PartDestructionHandler.cs` | ~150 | 原版自动处理MissingPart |
| `Combat/Damage/Patch_DamageWorker_FinalizeAndAddInjury.cs` | ~44 | 不再拦截伤害 |
| `Combat/Damage/UI/Patch_HealthUtility_GetPartConditionLabel.cs` | ~50 | 原版HP = 显示HP |

**预计删除：~784行代码**

### 4.2 大幅简化的文件

| 文件 | 变化 |
|------|------|
| `Combat/Damage/CombatBodyDamageHandler.cs` | 删除整个类（Pipeline模式不再需要） |
| `Combat/Damage/CombatBodyContext.cs` | 删除（Context模式不再需要） |
| `Combat/Damage/Handlers/TrionCostHandler.cs` | 逻辑移入HediffComp_TrionDamageCost |
| `Combat/Damage/Handlers/CollapseHandler.cs` | 逻辑移入HediffComp_RuptureMonitor |
| `Combat/CombatBodyOrchestrator.cs` | 删除InitializeShadowHP、简化CleanupCombatBodyState |
| `Combat/CombatBodyRuntime.cs` | 删除ShadowHP和PartDestruction字段 |

### 4.3 新增的文件

| 文件 | 行数(估) | 职责 |
|------|----------|------|
| `Combat/Hediff_CombatBodyActive.cs` | ~30 | 战斗体激活Hediff自定义类 |
| `Combat/Comps/HediffComp_TrionDamageCost.cs` | ~50 | Trion伤害消耗 |
| `Combat/Comps/HediffComp_TrionWoundDrain.cs` | ~70 | Trion伤口流失 |
| `Combat/Comps/HediffComp_RuptureMonitor.cs` | ~80 | 破裂条件监控 |
| `Combat/Patches/Patch_Pawn_PostApplyDamage.cs` | ~30 | 伤害后Trion消耗 |
| `Combat/Patches/Patch_Pawn_PreApplyDamage.cs` | ~20 | Collapsing无敌 |

**预计新增：~280行代码**

### 4.4 删除的XML定义

| 文件/定义 | 原因 |
|-----------|------|
| `HediffDefs_CombatWounds.xml` 整个文件 | 9个自定义伤口Def全部删除 |
| `BDP_CombatBodyPartDestroyed` | 使用原版MissingPart |
| `BDP_CombatBodyPartPending` | 可删除或保留为视觉标记 |

### 4.5 修改的XML定义

| 文件/定义 | 变化 |
|-----------|------|
| `BDP_CombatBodyActive` | 添加 `totalBleedFactor=0`、新HediffComp引用 |

### 4.6 代码量对比

| 维度 | 重构前 | 重构后 | 变化 |
|------|--------|--------|------|
| 伤害相关C#代码 | ~1400行 | ~400行 | **-70%** |
| 自定义HediffDef | 12个 | 3个 | **-75%** |
| Harmony Patch | 4个 | 4个 | 0（但2个被替换为更轻量的） |
| 独立子系统 | 5个(ShadowHP/Wound/Part/Pipeline/Context) | 0个 | **-100%** |
| 序列化字段 | ~15个 | ~3个 | **-80%** |

## 五、实施阶段

### 阶段1：基础切换（最高优先级）

**目标**：让原版伤害正常通过，用原版机制实现不死/不流血/不痛。

1. 修改 `BDP_CombatBodyActive` XML：添加 `totalBleedFactor=0`
2. 删除 `Patch_DamageWorker_FinalizeAndAddInjury`（让伤害通过）
3. 实现 `Patch_Pawn_PreApplyDamage`（Collapsing无敌）
4. 验证：战斗体受伤 → 原版伤口出现 → 不流血 → 不死亡

### 阶段2：Trion消耗机制（核心游戏机制）

**目标**：恢复Trion消耗和破裂触发。

1. 实现 `HediffComp_TrionDamageCost`
2. 实现 `Patch_Pawn_PostApplyDamage`
3. 实现 `HediffComp_RuptureMonitor`（Trion耗尽破裂）
4. 验证：受伤 → Trion减少 → Trion耗尽 → 破裂

### 阶段3：伤口Trion流失（持续消耗）

**目标**：伤口持续流失Trion（替代出血）。

1. 实现 `HediffComp_TrionWoundDrain`
2. 验证：累积伤口 → Trion持续流失 → 最终可能耗尽触发破裂

### 阶段4：关键部位监控（破裂条件）

**目标**：关键部位被摧毁时触发破裂。

1. 扩展 `HediffComp_RuptureMonitor` 检测 `Hediff_MissingPart`
2. 验证：头部/心脏被摧毁 → 立即触发破裂

### 阶段5：清理遗留代码

**目标**：删除所有不再需要的代码。

1. 删除影子HP系统全部文件
2. 删除自定义伤口系统全部文件
3. 删除Pipeline/Handler/Context
4. 简化Orchestrator
5. 简化CombatBodyRuntime
6. 删除伤口XML定义文件
7. 删除UI Patch

### 阶段6：快照系统验证

**目标**：确保快照系统正确清理战斗中的原版伤口。

1. 测试：激活 → 受伤 → 解除 → 验证伤口被清理
2. 测试：激活 → 部位缺失 → 解除 → 验证部位恢复
3. 测试：存档/读档 → 验证状态一致性

## 六、风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 原版伤害在单帧内导致死亡/倒地（preventsDeath生效前） | 极低。preventsDeath在ShouldBeDead最早期检查 | 已验证代码执行顺序 |
| 快照恢复时遗漏战斗中新增的Hediff | 中。可能有残留伤口 | RemoveAllHediffsExcept已全覆盖 |
| 破裂期间Collapsing Hediff被意外移除 | 低 | Collapsing Hediff自管理生命周期 |
| 原版部位破坏导致意外能力值变化 | 中。非关键部位缺失可能导致额外debuff | 可接受：这是"真实"的战斗体损伤 |
| 与修改伤害流程的其他模组冲突 | 低。新方案不再拦截伤害，冲突面大幅减小 | 这正是重构的目的之一 |

## 七、兼容性改善

| 场景 | 重构前 | 重构后 |
|------|--------|--------|
| Combat Extended等伤害重做模组 | 完全不兼容（Prefix返回false） | 兼容（不拦截伤害） |
| 医疗类模组（智能医疗等） | 不兼容（自定义伤口不可治疗） | 兼容（原版Hediff_Injury） |
| 血液/出血类模组 | 冲突（自定义出血系统） | 兼容（原版totalBleedFactor） |
| Hediff相关UI模组 | 部分不兼容（自定义Hediff类） | 兼容（原版Hediff类） |
| 存档兼容性 | 复杂（影子HP需要自定义序列化） | 简单（原版Hediff自动序列化） |

---

## 历史修改记录
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-06 | 初版：战斗体伤害系统回归原版的完整重构方案 | Claude Opus 4.6 |
