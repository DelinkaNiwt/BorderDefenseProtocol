# 战斗体快照系统实现总结

## 实现日期
2026-03-02

## 实现内容

### 1. 核心类（7个文件）

#### Combat/Snapshot/CombatBodySnapshot.cs
- 快照核心类，实现 IThingHolder 接口
- 管理 3 个 ThingOwner 容器（原衣物、战斗体衣物、原物品）
- 实现 Hediff、需求、衣物、物品的快照和恢复
- 支持序列化（ExposeData）

#### Combat/Snapshot/ApparelState.cs
- 记录衣物状态（locked, forced）
- 实现 IExposable 序列化

#### Combat/Snapshot/ItemState.cs
- 记录物品状态（notForSale, unpackedCaravan）
- 实现 IExposable 序列化

#### Combat/Snapshot/Patch_CompRottable.cs
- Harmony patch 阻止容器中物品腐烂
- 检查 holdingOwner.Owner 是否为 CombatBodySnapshot

#### Combat/GeneExtension_CombatBody.cs
- GeneDef 的 ModExtension
- 配置默认战斗体装备列表

#### Core/Genes/Gene_TrionGland.cs（修改）
- 添加 CombatBodySnapshot 字段
- PostAdd 时初始化快照和战斗体装备
- 实现 ExposeData 序列化

#### Defs/Combat/ThingDefs_CombatApparel.xml
- 定义 BDP_CombatBodyArmor（战斗体装甲）
- 无品质、无耐久、不可破坏
- 不可交易、不可制作

### 2. 技术要点

#### ThingOwner 容器
```csharp
private ThingOwner<Apparel> originalApparelContainer;
private ThingOwner<Thing> originalInventoryContainer;
private ThingOwner<Apparel> combatApparelContainer;

// 构造函数
originalApparelContainer = new ThingOwner<Apparel>(this);
```

#### IThingHolder 接口
```csharp
public IThingHolder ParentHolder => pawn;
public ThingOwner GetDirectlyHeldThings() => originalApparelContainer;
public void GetChildHolders(List<IThingHolder> outChildren)
{
    outChildren.Add((IThingHolder)originalApparelContainer);
    outChildren.Add((IThingHolder)originalInventoryContainer);
    outChildren.Add((IThingHolder)combatApparelContainer);
}
```

#### 状态恢复顺序
1. 先调用 Wear/TryAdd 将物品添加到 Pawn
2. 再恢复状态标记（locked, forced, notForSale 等）

#### 反射访问私有字段
```csharp
var unpackedField = typeof(Pawn_InventoryTracker)
    .GetField("unpackedCaravanItems", BindingFlags.NonPublic | BindingFlags.Instance);
var unpackedList = unpackedField?.GetValue(pawn.inventory) as List<Thing>;
```

#### Harmony Patch 阻止腐烂
```csharp
[HarmonyPatch(typeof(CompRottable), "CompTick")]
static bool Prefix(CompRottable __instance)
{
    var holder = __instance.parent.holdingOwner?.Owner;
    if (holder is CombatBodySnapshot)
        return false;  // 阻止腐烂
    return true;
}
```

### 3. 数据流

#### 激活战斗体
```
1. 拍摄 Hediff 快照（字段值）
2. 拍摄需求快照（当前值）
3. 转移衣物：原衣物 → originalApparelContainer
4. 装备战斗体衣物：combatApparelContainer → Pawn.apparel
5. 转移物品：物品 → originalInventoryContainer
```

#### 解除战斗体
```
1. 恢复 Hediff（重建 Hediff 对象）
2. 恢复需求（设置当前值）
3. 卸下战斗体衣物：Pawn.apparel → combatApparelContainer
4. 恢复原衣物：originalApparelContainer → Pawn.apparel
5. 恢复物品：originalInventoryContainer → Pawn.inventory
```

#### 存档数据
```
Pawn
├── Gene_TrionGland
│   └── CombatBodySnapshot (IThingHolder)
│       ├── originalApparelContainer (ThingOwner<Apparel>)
│       │   └── [原衣物列表]
│       ├── combatApparelContainer (ThingOwner<Apparel>)
│       │   └── [战斗体衣物列表]
│       ├── originalInventoryContainer (ThingOwner<Thing>)
│       │   └── [原物品列表]
│       ├── apparelStates (Dictionary<int, ApparelState>)
│       ├── itemStates (Dictionary<int, ItemState>)
│       ├── hediffRecords (List<HediffRecord>)
│       └── needLevels (Dictionary<NeedDef, float>)
└── apparel
    └── [当前穿着的衣物]
```

### 4. 编译状态
✅ 编译成功，无警告，无错误

### 5. 待测试功能
- [ ] 衣物换装（locked, forced 状态）
- [ ] 物品转移（notForSale, unpackedCaravan 状态）
- [ ] 物品腐烂阻止
- [ ] Hediff 快照和恢复
- [ ] 需求快照和恢复
- [ ] 存读档兼容性

### 6. 下一步
1. 实现战斗体激活/解除的触发逻辑（UI 按钮或自动触发）
2. 添加 BDPSnapshotConfigDef 配置系统（排除特定 Hediff）
3. 实现战斗体配置建筑（允许玩家自定义战斗体装备）
4. 添加战斗体状态 HUD 显示
5. 游戏内测试所有场景

## 技术债务
- 需要添加更多的错误处理和边界检查
- 需要添加日志输出以便调试
- 需要优化性能（大量 Hediff 时的快照性能）
- 需要添加单元测试

## 参考文档
- `docs/战斗体快照系统测试指南.md`
- `docs/plans/2026-03-01-combat-body-requirements-v3.md`
