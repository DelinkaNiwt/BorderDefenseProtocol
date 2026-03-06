# 衣物转移状态丢失分析报告

**日期**: 2026-03-02
**状态**: ✅ 已完成实现并验证
**问题**: 使用 `Remove()` + `TryAdd()` 转移衣物时，"强制"标记丢失

---

## 1. 调用链分析

### 1.1 移除衣物时的调用链

```
pawn.apparel.Remove(apparel)
  ↓
wornApparel.Remove(apparel)  // ThingOwner<Apparel>.Remove()
  ↓
NotifyRemoved(apparel)  // ThingOwner 基类方法
  ↓
pawn_ApparelTracker.Notify_ApparelRemoved(apparel)
  ↓
  ├─ Notify_ApparelChanged()  // 触发图形/能力更新
  ├─ pawn.outfits.forcedHandler.SetForced(apparel, false)  // ← 清除"强制"标记
  ├─ if (IsLocked(apparel)) Unlock(apparel)  // ← 清除"锁定"状态
  ├─ pawn.health.capacities.Notify_CapacityLevelsDirty()  // 标记能力值需重算
  └─ apparel.Notify_Unequipped(pawn)  // 通知衣物自身
       ↓
       ThingWithComps.Notify_Unequipped(pawn)
         ↓
         遍历所有 comps，调用 comp.Notify_Unequipped(pawn)
```

### 1.2 穿上衣物时的调用链

```
pawn.apparel.Wear(apparel, dropReplacedApparel, locked)
  ↓
wornApparel.TryAdd(apparel)  // ThingOwner<Apparel>.TryAdd()
  ↓
NotifyAdded(apparel)  // ThingOwner 基类方法
  ↓
pawn_ApparelTracker.Notify_ApparelAdded(apparel)
  ↓
  ├─ SortWornApparelIntoDrawOrder()  // 按绘制顺序排序
  ├─ Notify_ApparelChanged()  // 触发图形/能力更新
  ├─ 处理 CompApparelVerbOwner_Charged 的 Verb
  ├─ pawn.health.capacities.Notify_CapacityLevelsDirty()  // 标记能力值需重算
  └─ apparel.Notify_Equipped(pawn)  // 通知衣物自身
       ↓
       设置 Ability.pawn 和 Verb.caster
  ↓
if (locked) Lock(apparel)  // ← 仅恢复"锁定"，不恢复"强制"
```

---

## 2. 状态系统识别

### 2.1 两个独立的装备控制系统

| 系统 | 位置 | 字段 | 作用 | UI显示 |
|------|------|------|------|--------|
| **锁定系统** | `Pawn_ApparelTracker` | `lockedApparel: List<Apparel>` | 防止被自动脱下（如换装、掉落） | 锁图标 |
| **强制系统** | `OutfitForcedHandler` | `forcedAps: List<Apparel>` | 装备策略强制保留此衣物 | "强制"文字标记 |

### 2.2 被清除的状态清单

**衣物状态**：

| 状态 | 清除位置 | 恢复方法 | 实现状态 |
|------|----------|----------|----------|
| `lockedApparel` | `Notify_ApparelRemoved` → `Unlock()` | `Wear(locked: true)` | ✅ 已实现 |
| `forcedAps` | `Notify_ApparelRemoved` → `SetForced(false)` | `forcedHandler.SetForced(true)` | ✅ 已实现 |
| Verb.caster | `Notify_Unequipped` → comps | `Notify_Equipped` 自动设置 | ✅ 自动恢复 |
| Ability.pawn | `Notify_Unequipped` → comps | `Notify_Equipped` 自动设置 | ✅ 自动恢复 |

**物品状态**：

| 状态 | 清除位置 | 恢复方法 | 实现状态 |
|------|----------|----------|----------|
| `itemsNotForSale` | `Notify_ItemRemoved` | `TryAddItemNotForSale()` | ✅ 已实现 |
| `unpackedCaravanItems` | `Notify_ItemRemoved` | 反射添加到列表 | ✅ 已实现 |

---

## 3. 根本原因

**RimWorld API 设计假设**：
`Remove()` 是"真正的脱下"行为，会清理所有相关状态（强制、锁定、装备关系等）。

**我们的需求**：
"临时转移到快照容器"，不应触发这些清理。

**矛盾点**：
没有官方 API 支持"无副作用的容器转移"。

---

## 4. 可行方案对比

### 方案A：手动恢复所有状态（当前方案）

**实现**：
```csharp
// 转移前记录
bool wasLocked = pawn.apparel.IsLocked(apparel);
bool wasForced = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false;

// 转移
pawn.apparel.Remove(apparel);
apparelContainer.TryAdd(apparel);

// 恢复
apparelContainer.Remove(apparel);
pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: wasLocked);
if (wasForced) pawn.outfits.forcedHandler.SetForced(apparel, true);
```

**优点**：
- 使用公开 API，兼容性好
- 逻辑清晰，易于维护

**缺点**：
- 需要手动追踪所有状态
- 可能遗漏未知状态（如 Mod 添加的状态）
- 触发不必要的通知（性能开销）

**风险评估**：⭐⭐⭐ 中等（需确保覆盖所有状态）

---

### 方案B：反射绕过通知系统

**实现**：
```csharp
// 使用反射访问私有字段
var wornApparelField = typeof(Pawn_ApparelTracker)
    .GetField("wornApparel", BindingFlags.NonPublic | BindingFlags.Instance);
var wornApparel = (ThingOwner<Apparel>)wornApparelField.GetValue(pawn.apparel);

// 直接操作内部列表，绕过通知
var innerList = wornApparel.InnerListForReading;
innerList.Remove(apparel);
apparel.holdingOwner = null;
apparelContainer.TryAdd(apparel);

// 恢复时同样绕过
apparelContainer.Remove(apparel);
innerList.Add(apparel);
apparel.holdingOwner = wornApparel;
```

**优点**：
- 完全避免状态丢失
- 无性能开销（不触发通知）

**缺点**：
- 绕过通知可能导致其他状态不一致（如图形未更新）
- 依赖内部实现，版本升级可能失效
- 需要手动管理 `holdingOwner` 等底层字段

**风险评估**：⭐⭐⭐⭐ 高（可能引入隐蔽 bug）

---

### 方案C：使用 TryTransferToContainer（已验证不可行）

**问题**：
`TryTransferToContainer` 内部仍调用 `Remove()`，无法避免状态丢失。

---

## 5. 最终实现方案（已验证）

### 5.1 衣物状态恢复

```csharp
// 转移前记录所有状态
var lockedFlags = new Dictionary<Apparel, bool>();
var forcedFlags = new Dictionary<Apparel, bool>();

foreach (var apparel in wornApparel)
{
    lockedFlags[apparel] = pawn.apparel.IsLocked(apparel);
    forcedFlags[apparel] = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false;

    pawn.apparel.Remove(apparel);
    apparelContainer.TryAdd(apparel);
}

// 恢复时
foreach (var apparel in toRestoreApparel)
{
    apparelContainer.Remove(apparel);

    bool wasLocked = lockedFlags[apparel];
    bool wasForced = forcedFlags[apparel];

    // 穿上衣物并恢复锁定状态
    pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: wasLocked);

    // 恢复强制状态
    if (wasForced && pawn.outfits?.forcedHandler != null)
    {
        pawn.outfits.forcedHandler.SetForced(apparel, forced: true);
    }
}
```

### 5.2 物品状态恢复

```csharp
// 转移前记录所有状态
var notForSaleFlags = new Dictionary<Thing, bool>();
var unpackedCaravanFlags = new Dictionary<Thing, bool>();

foreach (var item in items)
{
    notForSaleFlags[item] = pawn.inventory.NotForSale(item);

    // 通过反射访问私有字段
    var unpackedField = typeof(Pawn_InventoryTracker)
        .GetField("unpackedCaravanItems", BindingFlags.NonPublic | BindingFlags.Instance);
    var unpackedList = unpackedField?.GetValue(pawn.inventory) as IList;
    unpackedCaravanFlags[item] = unpackedList?.Contains(item) ?? false;

    pawn.inventory.innerContainer.TryTransferToContainer(item, inventoryContainer);
}

// 恢复时
foreach (var item in toRestoreItems)
{
    bool wasNotForSale = notForSaleFlags[item];
    bool wasUnpacked = unpackedCaravanFlags[item];

    inventoryContainer.TryTransferToContainer(item, pawn.inventory.innerContainer);

    // 恢复"不出售"标记
    if (wasNotForSale)
    {
        pawn.inventory.TryAddItemNotForSale(item);
    }

    // 恢复"商队解包"标记
    if (wasUnpacked)
    {
        var unpackedField = typeof(Pawn_InventoryTracker)
            .GetField("unpackedCaravanItems", BindingFlags.NonPublic | BindingFlags.Instance);
        var unpackedList = unpackedField?.GetValue(pawn.inventory) as IList;
        unpackedList?.Add(item);
    }
}
```

### 5.3 验证结果

**测试环境**：RimWorld 1.6
**测试日期**：2026-03-02

| 状态项 | 转移前 | 转移后 | 恢复后 | 结果 |
|--------|--------|--------|--------|------|
| 衣物锁定 | ✓ | ✗ | ✓ | ✅ 通过 |
| 衣物强制 | ✓ | ✗ | ✓ | ✅ 通过 |
| 物品不出售 | ✓ | ✗ | ✓ | ✅ 通过 |
| 商队解包 | ✓ | ✗ | ✓ | ✅ 通过 |

**结论**：所有已知状态均成功恢复，UI 显示正确。

---

## 6. 待验证问题（后续优化）

## 6. 待验证问题（后续优化）

### 6.1 是否还有其他状态丢失？

需要检查的潜在状态：
- [ ] `Apparel.Wearer` 字段（通过 `Notify_Equipped` 自动设置，应该已恢复）
- [ ] `CompBiocodable` 的绑定状态（不应丢失，绑定在 Comp 内部）
- [ ] `CompQuality` 的品质信息（不应丢失，存储在 Comp 内部）
- [ ] `CompColorable` 的颜色信息（不应丢失，存储在 Comp 内部）
- [ ] Mod 添加的自定义 Comp 状态（需要具体测试）

### 6.2 性能优化方向

当前方案触发完整的 `Notify_ApparelRemoved` / `Notify_ApparelAdded` 链，包括：
- 图形更新（`Notify_ApparelChanged`）
- 能力值重算（`Notify_CapacityLevelsDirty`）
- Comp 通知链

**优化方向**：
- 如果快照/恢复在同一帧内完成，可以考虑批量操作后统一触发通知
- 或使用反射绕过通知系统（风险较高，需充分测试）

---

## 7. 总结

### 7.1 核心发现

RimWorld 的容器转移 API（`Remove` / `TryTransferToContainer`）设计为"真正的移除"，会清理所有所有权相关的状态标记。这是设计行为，不是 bug。

### 7.2 解决方案

采用**方案A：手动恢复所有状态**，通过以下步骤实现完整的状态保留：

1. **转移前记录**：遍历所有衣物/物品，记录所有状态标记
2. **执行转移**：使用标准 API 进行容器转移（触发状态清理）
3. **恢复状态**：使用公开 API 或反射恢复所有记录的状态
4. **验证结果**：对比转移前后的状态，确保一致性

### 7.3 实现位置

- **代码**：`CombatBodyDebugActions.cs` → `VerifyContainerTransfer()`
- **文档**：`docs/reports/apparel-transfer-state-analysis.md`

### 7.4 后续工作

- [ ] 将验证代码迁移到正式的 `CombatBodySnapshot` 类（阶段1）
- [ ] 编写单元测试覆盖所有状态恢复逻辑
- [ ] 测试与常见 Mod 的兼容性（如 CE、VE 系列）
- [ ] 性能测试：大量衣物/物品时的转移开销
