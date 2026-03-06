---
标题: 战斗体受伤流程完整推演与问题分析
日期: 2026-03-04
类型: 流程分析报告
状态: 分析中
标签: [战斗体, 伤害系统, 部位破坏, 芯片管理, Bug分析]
---

# 战斗体受伤流程完整推演与问题分析

## 问题概述

用户报告了4个问题:
1. **"不存在"的文本是白色的** - 健康面板显示问题
2. **左食指受伤被破坏后,又检测到右手被破坏** - 部位破坏传播逻辑问题
3. **右手被破坏后,芯片状态异常** - 输出日志要关闭右手槽芯片,但芯片状态中右手槽位芯片没有注销且数量少了一个但又还剩一个
4. **左手被破坏后,没有输出日志也没有关闭左手槽位** - 不一致的行为

## 实际发生的事件序列(根据日志)

### 事件1: 左手食指(Finger)受伤并被破坏

**日志行1820-1946:**

```
[BDP]   受伤部位: Finger
[BDP]   ShadowHP [Finger]: 8.0 → 0.0 ★破坏★
[BDP]   非关键部位破坏，标记部位缺失
[BDP] ⚠ 影子部位被毁: Finger → 真身部位已失效（无疼痛/流血）
[BDP]   部位路径: Torso/Shoulder/Arm/Hand/Finger
```

**流程推演:**

1. **伤害拦截** (`CombatBodyDamageHandler.HandleDamage`)
   - 原版伤害系统计算出护甲后伤害
   - BDP在 `FinalizeAndAddInjury` 前拦截
   - 获取受伤部位: `Finger` (手指)

2. **Trion消耗** (`TrionCostHandler.Handle`)
   - 计算Trion消耗: `damage * 0.5`
   - 从CompTrion扣除Trion

3. **影子HP处理** (`ShadowHPHandler.Handle`)
   - 获取部位影子HP: `8.0`
   - 应用伤害后: `0.0` ★破坏★
   - 检测到部位HP≤0,触发部位破坏逻辑
   - 判断: `Finger` 不是关键部位(非Head/Brain/Heart/Neck/Torso)
   - 调用 `PartDestructionHandler.Handle`

4. **部位破坏处理** (`PartDestructionHandler.Handle`)
   - 添加 `BDP_CombatBodyPartDestroyed` Hediff到 `Finger` 部位
   - 记录部位路径: `Torso/Shoulder/Arm/Hand/Finger`
   - 检查是否为手部部位: `IsHandPart(part)`
     ```csharp
     private bool IsHandPart(BodyPartRecord part)
     {
         if (part == null) return false;
         return part.def.defName == "Hand" || part.parent?.def.defName == "Hand";
     }
     ```
   - **判断结果**: `part.def.defName = "Finger"` (不是"Hand")
   - **判断结果**: `part.parent?.def.defName = "Hand"` ✅ **TRUE**
   - **触发手部破坏事件**: 调用 `GetHandSide(part)` 判断左右手

5. **手部侧边判断** (`GetHandSide`)
   ```csharp
   private BDP.Core.Gene_TrionGland.HandSide GetHandSide(BodyPartRecord part)
   {
       var current = part;
       while (current != null)
       {
           if (current.def.defName == "Shoulder")
           {
               // 检查customLabel是否包含"left"
               if (current.customLabel != null && current.customLabel.ToLower().Contains("left"))
               {
                   return BDP.Core.Gene_TrionGland.HandSide.Left;
               }
               // 默认为右手
               return BDP.Core.Gene_TrionGland.HandSide.Right;
           }
           current = current.parent;
       }
       // 如果找不到Shoulder，默认返回左手
       return BDP.Core.Gene_TrionGland.HandSide.Left;
   }
   ```

   **部位树遍历:**
   - `Finger` → parent: `Hand`
   - `Hand` → parent: `Arm`
   - `Arm` → parent: `Shoulder`
   - `Shoulder.customLabel` = ??? (需要检查)

   **问题点1**: 如果Shoulder的customLabel不包含"left",会默认返回`Right`!

### 事件2: 检测到右手被破坏

**日志行2111:**

```
[BDP] 检测到右手被破坏，强制关闭对应槽位
[BDP] 强制关闭右手槽位芯片: 右手缺失
```

**流程推演:**

6. **触发部位破坏事件** (`Gene_TrionGland.TriggerPartDestroyedEvent`)
   - 事件参数:
     ```csharp
     {
         Pawn = pawn,
         Part = Finger部位,
         IsHandPart = true,
         HandSide = Right  // ← 这里判断错了!
     }
     ```

7. **CompTriggerBody接收事件** (`CompTriggerBody.OnPartDestroyed`)
   - 检测到 `IsHandPart = true`
   - 调用 `OnHandDestroyed(HandSide.Right)`  // ← 错误的侧边

8. **强制关闭右手槽位** (`ForceDeactivateRightSlots`)
   - 输出日志: "强制关闭右手槽位芯片: 右手缺失"
   - 调用 `RebuildVerbs(pawn)`
   - **结果**: 右手芯片被错误地关闭了!

**日志行2184-2190:**
```
[BDP] RebuildVerbs完成 [测试触发体] pawn=Tran
  VerbTracker.AllVerbs (1) — 芯片Verb不在此列表中:
    [0] Verb_MeleeAttackDamage primary=True stdCmd=False melee=True label= caster=Tran
  芯片Verb缓存（手动创建，不在AllVerbs中）:
    left=Verb_BDPShoot right= dual=
    volleyLeft=Verb_BDPVolley volleyRight= volleyDual=
    comboAttack= comboVolley= comboDef=
```

**观察**:
- `left=Verb_BDPShoot` - 左手芯片还在
- `right=` - 右手芯片被清空了 (但实际应该是左手被破坏!)

## 问题根源分析

### 问题1: "不存在"文本是白色的

**原因**: 我们刚实现的 `Patch_HealthUtility_GetPartConditionLabel` 只覆盖了**部位名称**的颜色,但没有处理**被破坏部位的文本标签**。

**原版逻辑**:
- 当部位有 `MissingBodyPart` Hediff时,原版会返回特殊的文本标签 "不存在" (或类似文本)
- 这个文本的颜色由 `HealthUtility.GetPartConditionLabel` 返回

**BDP当前逻辑**:
- 使用自定义 `BDP_CombatBodyPartDestroyed` Hediff (不是 `MissingBodyPart`)
- Patch只检查影子HP,但当部位被破坏后,影子HP=0,返回灰色
- **但是**: 原版可能通过其他路径渲染"不存在"文本,没有经过我们的patch

**需要验证**:
- `BDP_CombatBodyPartDestroyed` 的HediffDef配置
- 原版如何渲染被破坏部位的标签

### 问题2: 左食指被破坏,却检测到右手被破坏

**根本原因**: `GetHandSide()` 方法的判断逻辑有缺陷

**错误逻辑**:
```csharp
if (current.customLabel != null && current.customLabel.ToLower().Contains("left"))
{
    return BDP.Core.Gene_TrionGland.HandSide.Left;
}
// 默认为右手  ← 这里有问题!
return BDP.Core.Gene_TrionGland.HandSide.Right;
```

**问题**:
1. 依赖 `customLabel` 而不是 `label` 或其他更可靠的属性
2. 只检查"left",如果不包含就默认为右手
3. 没有检查"right"关键字
4. RimWorld的部位可能使用不同的命名方式

**正确的判断方式应该是**:
- 检查部位的 `label` 或 `LabelShort`
- 同时检查"left"和"right"关键字
- 或者使用部位索引 (`part.Index`) 来判断左右

### 问题3: 右手芯片状态异常

**原因**: 由于问题2,左手食指被破坏时错误地触发了右手破坏事件

**流程**:
1. 左手食指被破坏
2. `GetHandSide()` 错误返回 `Right`
3. `ForceDeactivateRightSlots()` 被调用
4. 右手芯片被清空: `right=`
5. 但实际右手部位完好,所以芯片状态不一致

**"数量少了一个但又还剩一个"的原因**:
- 可能是UI显示缓存问题
- 或者芯片管理器内部状态不一致

### 问题4: 左手被破坏后,没有输出日志

**需要查看日志**: 用户提到"后面左手被破坏了",但我在当前日志中没有看到左手被破坏的记录。

**可能原因**:
1. 左手实际没有被破坏(只是食指被破坏)
2. 由于右手芯片被错误关闭,导致系统认为已经处理过手部破坏
3. `PartDestructionHandler` 的 `destroyedParts` 集合中已经记录了错误的部位

## 代码流程图

```
攻击命中左手食指
    ↓
CombatBodyDamageHandler.HandleDamage
    ├─ TrionCostHandler: 扣除Trion
    ├─ ShadowHPHandler: 影子HP 8.0 → 0.0
    │   └─ 检测到破坏 → PartDestructionHandler.Handle
    │       ├─ 添加 BDP_CombatBodyPartDestroyed Hediff
    │       ├─ IsHandPart(Finger) = true (因为parent是Hand)
    │       └─ GetHandSide(Finger)
    │           └─ 遍历到Shoulder
    │               └─ customLabel不包含"left"
    │                   └─ 返回 Right ❌ 错误!
    │
    └─ TriggerPartDestroyedEvent(HandSide=Right)
        └─ CompTriggerBody.OnHandDestroyed(Right)
            └─ ForceDeactivateRightSlots()
                └─ 清空右手芯片 ❌ 错误!
```

## 修复建议

### 修复1: GetHandSide() 方法

**方案A: 使用label而不是customLabel**
```csharp
private BDP.Core.Gene_TrionGland.HandSide GetHandSide(BodyPartRecord part)
{
    var current = part;
    while (current != null)
    {
        string label = current.LabelShort.ToLower();

        // 检查是否包含左右关键字
        if (label.Contains("left") || label.Contains("左"))
            return BDP.Core.Gene_TrionGland.HandSide.Left;
        if (label.Contains("right") || label.Contains("右"))
            return BDP.Core.Gene_TrionGland.HandSide.Right;

        current = current.parent;
    }

    // 如果都找不到,记录警告
    Log.Warning($"[BDP] 无法判断手部侧边: {part.def.defName}");
    return BDP.Core.Gene_TrionGland.HandSide.Left;
}
```

**方案B: 使用部位索引**
```csharp
// RimWorld中,左右对称部位通常有不同的Index
// 需要研究RimWorld的部位系统
```

### 修复2: "不存在"文本颜色

需要检查 `BDP_CombatBodyPartDestroyed` HediffDef的配置,确保它正确地标记部位为"缺失"状态。

或者在Patch中添加额外的检查:
```csharp
// 检查部位是否有破坏Hediff
var destroyedHediff = pawn.health.hediffSet.hediffs
    .FirstOrDefault(h => h.Part == part && h.def == BDP_DefOf.BDP_CombatBodyPartDestroyed);
if (destroyedHediff != null)
{
    return new Pair<string, Color>("不存在", Color.gray);
}
```

### 修复3: 芯片状态同步

在 `ForceDeactivateRightSlots/LeftSlots` 中添加验证:
```csharp
// 验证部位确实被破坏
if (!gene.PartDestruction.IsDestroyed(rightHandPart))
{
    Log.Warning($"[BDP] 右手部位未被破坏,跳过关闭芯片");
    return;
}
```

## 下一步行动

1. ✅ 分析完成 - 已识别根本原因
2. ⏳ 修复 `GetHandSide()` 方法
3. ⏳ 修复"不存在"文本颜色显示
4. ⏳ 添加芯片状态验证
5. ⏳ 游戏内测试验证

---

## 历史记录

| 日期 | 版本 | 修改内容 | 作者 |
|------|------|---------|------|
| 2026-03-04 | v1.0 | 初始分析报告 | Claude Sonnet 4.6 |
