# 衣物/物品快照序列化方案分析

**日期**: 2026-03-02
**问题**: 当前 ThingOwner 物理转移方案无法满足存读档需求

---

## 1. 当前方案问题分析

### 1.1 架构设计要求

```
衣物/物品 → 物理转移至快照容器（ThingOwner）
CombatBodySnapshot → 实现 IThingHolder 接口
Gene_TrionGland → 持有 CombatBodySnapshot 引用
```

### 1.2 存档场景问题

**场景1：激活状态下存档**
```
战斗体激活 → 衣物转移到快照容器 → 玩家存档
  ↓
Pawn.apparel.wornApparel = 空（衣物在快照容器中）
CombatBodySnapshot.apparelContainer = 有衣物
  ↓
读档后：衣物在快照容器中，Pawn 裸体 ✗
```

**场景2：快照容器的所有权**
```
CombatBodySnapshot 实现 IThingHolder
  ↓
ThingOwner<Apparel> apparelContainer = new ThingOwner<Apparel>(this)
  ↓
存档时：Scribe_Deep.Look(ref apparelContainer, "apparelContainer", this)
  ↓
读档时：需要正确重建 owner 引用
```

**问题**：
- Thing 对象在快照容器中时，不在 Pawn 身上，存档会丢失 Pawn 的装备状态
- IThingHolder 实现复杂，需要处理 GetChildHolders、GetDirectlyHeldThings
- 快照容器的生命周期管理复杂

---

## 2. 可行方案对比

### 方案A：字段值快照（类似 Hediff）

**设计**：
```csharp
public class ApparelRecord : IExposable
{
    public string defName;
    public string stuffDefName;
    public int hitPoints;
    public int maxHitPoints;
    public QualityCategory? quality;
    public Color? color;

    // 状态标记
    public bool wasLocked;
    public bool wasForced;

    // 唯一标识（用于查找原物）
    public int thingIDNumber;  // Thing.thingIDNumber

    public void ExposeData() { /* ... */ }
}
```

**流程**：
```
激活时：
  1. 遍历 pawn.apparel.WornApparel，记录每件衣物的字段值
  2. 衣物留在 Pawn 身上不动
  3. 标记"这些是快照时的衣物"

恢复时：
  1. 移除激活期间新增的衣物（通过 thingIDNumber 对比）
  2. 恢复快照时衣物的状态标记（locked、forced）
  3. 验证衣物完整性（HP、质量等是否被修改）
```

**优点**：
- ✅ 衣物始终在 Pawn 身上，存档无问题
- ✅ 只序列化字段值，无 IThingHolder 复杂度
- ✅ 可以通过 thingIDNumber 精确匹配原物

**缺点**：
- ⚠️ 无法"真正回滚"衣物状态（如 HP 被打掉无法恢复）
- ⚠️ 激活期间衣物可能被脱下/丢弃，恢复时找不到原物

**风险评估**：⭐⭐ 低（适合"状态标记恢复"场景）

---

### 方案B：Thing 引用列表 + 临时脱下

**设计**：
```csharp
public class CombatBodySnapshot : IExposable
{
    // 只记录引用，不持有实例
    private List<Apparel> snapshotApparels;
    private List<Thing> snapshotItems;

    // 状态标记
    private Dictionary<int, ApparelState> apparelStates;  // key = thingIDNumber

    public void ExposeData()
    {
        Scribe_Collections.Look(ref snapshotApparels, "snapshotApparels", LookMode.Reference);
        Scribe_Collections.Look(ref snapshotItems, "snapshotItems", LookMode.Reference);
        // apparelStates 用 Deep 序列化
    }
}
```

**流程**：
```
激活时：
  1. 记录 pawn.apparel.WornApparel 的引用列表
  2. 记录每件衣物的状态标记
  3. 衣物留在 Pawn 身上不动

恢复时：
  1. 对比当前 WornApparel 和快照引用列表
  2. 移除不在快照中的衣物（激活期间新增的）
  3. 恢复快照中衣物的状态标记
```

**优点**：
- ✅ 衣物始终在 Pawn 身上，存档无问题
- ✅ 通过引用直接匹配，无需 thingIDNumber 查找
- ✅ 可以检测衣物是否被销毁/丢失

**缺点**：
- ⚠️ 引用可能失效（衣物被销毁）
- ⚠️ 无法"真正回滚"衣物状态

**风险评估**：⭐⭐ 低（适合"状态标记恢复"场景）

---

### 方案C：ThingOwner + IThingHolder（当前架构）

**设计**：
```csharp
public class CombatBodySnapshot : IExposable, IThingHolder
{
    private ThingOwner<Apparel> apparelContainer;
    private ThingOwner<Thing> inventoryContainer;

    public CombatBodySnapshot(Pawn pawn)
    {
        apparelContainer = new ThingOwner<Apparel>(this);
        inventoryContainer = new ThingOwner<Thing>(this);
    }

    public void ExposeData()
    {
        Scribe_Deep.Look(ref apparelContainer, "apparelContainer", this);
        Scribe_Deep.Look(ref inventoryContainer, "inventoryContainer", this);
    }

    public ThingOwner GetDirectlyHeldThings() => apparelContainer;
    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        outChildren.Add(apparelContainer);
        outChildren.Add(inventoryContainer);
    }
}
```

**流程**：
```
激活时：
  1. 物理转移衣物到 apparelContainer
  2. 物理转移物品到 inventoryContainer
  3. Pawn 身上为空

恢复时：
  1. 从容器转移回 Pawn
  2. 恢复状态标记
```

**优点**：
- ✅ 真正的"物理隔离"，激活期间无法访问快照衣物
- ✅ 可以"真正回滚"衣物状态（HP、质量等）

**缺点**：
- ✗ **存档问题**：激活状态下存档，Pawn 裸体
- ✗ IThingHolder 实现复杂
- ✗ 容器生命周期管理复杂

**风险评估**：⭐⭐⭐⭐ 高（存档场景有严重问题）

---

## 3. 推荐方案

### 3.1 方案选择：方案B（Thing 引用列表）

**理由**：
1. **存档兼容**：衣物始终在 Pawn 身上，存档无问题
2. **实现简单**：无需 IThingHolder，只需序列化引用
3. **功能足够**：战斗体场景下，只需恢复状态标记，不需要"真正回滚"衣物 HP

**权衡**：
- 放弃"真正回滚衣物状态"的能力
- 战斗体激活期间，衣物仍可被脱下/丢弃（需要额外限制）

### 3.2 具体实现

```csharp
public class CombatBodySnapshot : IExposable
{
    // ── 衣物快照 ──
    private List<Apparel> snapshotApparels;  // 引用列表
    private Dictionary<int, ApparelState> apparelStates;  // key = thingIDNumber

    // ── 物品快照 ──
    private List<Thing> snapshotItems;  // 引用列表
    private Dictionary<int, ItemState> itemStates;  // key = thingIDNumber

    // ── 状态记录结构 ──
    private class ApparelState : IExposable
    {
        public bool wasLocked;
        public bool wasForced;

        public void ExposeData()
        {
            Scribe_Values.Look(ref wasLocked, "wasLocked");
            Scribe_Values.Look(ref wasForced, "wasForced");
        }
    }

    private class ItemState : IExposable
    {
        public bool wasNotForSale;
        public bool wasUnpacked;

        public void ExposeData()
        {
            Scribe_Values.Look(ref wasNotForSale, "wasNotForSale");
            Scribe_Values.Look(ref wasUnpacked, "wasUnpacked");
        }
    }

    // ── 拍摄快照 ──
    public void TakeSnapshot(Pawn pawn)
    {
        // 记录衣物引用和状态
        snapshotApparels = pawn.apparel.WornApparel.ToList();
        apparelStates = new Dictionary<int, ApparelState>();

        foreach (var apparel in snapshotApparels)
        {
            apparelStates[apparel.thingIDNumber] = new ApparelState
            {
                wasLocked = pawn.apparel.IsLocked(apparel),
                wasForced = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false
            };
        }

        // 记录物品引用和状态
        snapshotItems = pawn.inventory.innerContainer.ToList();
        itemStates = new Dictionary<int, ItemState>();

        foreach (var item in snapshotItems)
        {
            itemStates[item.thingIDNumber] = new ItemState
            {
                wasNotForSale = pawn.inventory.NotForSale(item),
                wasUnpacked = /* 反射获取 */
            };
        }
    }

    // ── 恢复快照 ──
    public void RestoreSnapshot(Pawn pawn)
    {
        // 1. 移除激活期间新增的衣物
        var currentApparels = pawn.apparel.WornApparel.ToList();
        var snapshotIDs = new HashSet<int>(snapshotApparels.Select(a => a.thingIDNumber));

        foreach (var apparel in currentApparels)
        {
            if (!snapshotIDs.Contains(apparel.thingIDNumber))
            {
                pawn.apparel.Remove(apparel);
                // 衣物去哪？掉地上或进背包
            }
        }

        // 2. 恢复快照时衣物的状态标记
        foreach (var apparel in snapshotApparels)
        {
            if (apparel.Destroyed) continue;  // 衣物被销毁，跳过

            var state = apparelStates[apparel.thingIDNumber];

            // 恢复锁定
            if (state.wasLocked && !pawn.apparel.IsLocked(apparel))
                pawn.apparel.Lock(apparel);
            else if (!state.wasLocked && pawn.apparel.IsLocked(apparel))
                pawn.apparel.Unlock(apparel);

            // 恢复强制
            if (state.wasForced && !(pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false))
                pawn.outfits.forcedHandler.SetForced(apparel, true);
            else if (!state.wasForced && (pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false))
                pawn.outfits.forcedHandler.SetForced(apparel, false);
        }

        // 3. 物品同理
        // ...
    }

    // ── 序列化 ──
    public void ExposeData()
    {
        // 引用列表
        Scribe_Collections.Look(ref snapshotApparels, "snapshotApparels", LookMode.Reference);
        Scribe_Collections.Look(ref snapshotItems, "snapshotItems", LookMode.Reference);

        // 状态字典（需要自定义序列化）
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            var apparelStateKeys = apparelStates.Keys.ToList();
            var apparelStateValues = apparelStates.Values.ToList();
            Scribe_Collections.Look(ref apparelStateKeys, "apparelStateKeys", LookMode.Value);
            Scribe_Collections.Look(ref apparelStateValues, "apparelStateValues", LookMode.Deep);
        }
        else if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            List<int> apparelStateKeys = null;
            List<ApparelState> apparelStateValues = null;
            Scribe_Collections.Look(ref apparelStateKeys, "apparelStateKeys", LookMode.Value);
            Scribe_Collections.Look(ref apparelStateValues, "apparelStateValues", LookMode.Deep);

            if (apparelStateKeys != null && apparelStateValues != null)
            {
                apparelStates = new Dictionary<int, ApparelState>();
                for (int i = 0; i < apparelStateKeys.Count; i++)
                    apparelStates[apparelStateKeys[i]] = apparelStateValues[i];
            }
        }

        // itemStates 同理
    }
}
```

### 3.3 额外约束

为了防止激活期间衣物被脱下/丢弃，需要：

1. **锁定所有快照衣物**：激活时自动锁定，防止被脱下
2. **Gizmo 禁用**：激活期间禁用"脱下衣物"Gizmo
3. **AI 限制**：通过 Harmony patch 拦截 AI 脱衣行为

---

## 4. 方案对比总结

| 维度 | 方案A（字段值） | 方案B（引用列表）⭐ | 方案C（ThingOwner） |
|------|----------------|-------------------|-------------------|
| 存档兼容 | ✅ 完美 | ✅ 完美 | ✗ 有问题 |
| 实现复杂度 | ⭐⭐ 中等 | ⭐ 简单 | ⭐⭐⭐⭐ 高 |
| 状态恢复 | ✅ 支持 | ✅ 支持 | ✅ 支持 |
| 真正回滚 | ✗ 不支持 | ✗ 不支持 | ✅ 支持 |
| 引用失效处理 | N/A | ⚠️ 需要 | N/A |
| 序列化开销 | 小 | 极小 | 大 |

**推荐**：方案B（Thing 引用列表）

---

## 5. 架构文档更新建议

需要修改 `战斗体系统架构设计文档.md` 的快照章节：

**修改前**：
```
| 衣物 | 物理转移至快照容器（ThingOwner） | ... |
| 物品 | 物理转移至快照容器（ThingOwner） | ... |
```

**修改后**：
```
| 衣物 | 记录引用列表+状态标记（List<Apparel> + Dictionary<int, State>），衣物留在pawn上 | 移除新增衣物，恢复状态标记 |
| 物品 | 记录引用列表+状态标记（List<Thing> + Dictionary<int, State>），物品留在pawn上 | 移除新增物品，恢复状态标记 |
```

**关键技术要点补充**：
- 衣物/物品快照采用**引用列表+状态标记**而非物理转移：激活时记录引用和状态，Thing 留在 pawn 上不动；恢复时移除新增的，恢复状态标记
- 通过 `thingIDNumber` 精确匹配 Thing 对象，处理引用失效情况
- 激活期间自动锁定快照衣物，防止被脱下/丢弃
