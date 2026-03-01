# Research Summary: CompTriggerBody Refactoring

## Stack

**模式选择：辅助类提取（Helper Extraction）**
- RimWorld ThingComp 不支持运行时动态组合，但支持 partial class 和辅助类委托
- 已有先例：CompTriggerBody.Debug.cs（partial class）
- 辅助类不继承 ThingComp，只作为纯逻辑委托对象
- 序列化仍由 CompTriggerBody.PostExposeData 统一管理（辅助类不参与 Scribe）

**关键约束：C# 7.3（RimWorld 1.6）**
- 无默认接口方法 → IChipEffect 新增方法必须在所有实现类中添加
- 无 nullable reference types
- 无 switch expression

## Features

### Table Stakes
1. ChipVerbBuilder 承接所有 Verb 创建/缓存逻辑（CreateAndCacheChipVerbs, CreateVolleyVerbs, CreateComboVerbs, CreateSingleComboVerb, CreateSingleVolleyVerb, FindSavedVerb, 参数辅助方法）
2. ComboAbilityManager 承接组合能力授予/撤销（TryGrantComboAbility, TryRevokeComboAbilities, grantedCombos 列表）
3. 行为完全不变：重构前后外部可观察行为一致
4. 存档兼容：读档后状态正确恢复

### Differentiators
1. IChipEffect 扩展事件（OnKilledPawn 等）— 带默认空实现

### Anti-features
- 不改 Gizmo 逻辑（与 Verb 缓存耦合紧密）
- 不引入新接口层（IChipEffectWorker）
- 不改 IChipEffect 现有方法签名

## Architecture

```
重构前：
  CompTriggerBody (1587行)
    ├── 槽位管理 + 状态机 (~400行) ← 核心，保留
    ├── IVerbOwner (~50行) ← 核心，保留
    ├── Verb重建+缓存 (~350行) ← 提取→ ChipVerbBuilder
    ├── 组合能力 (~50行) ← 提取→ ComboAbilityManager
    ├── 激活/关闭 (~200行) ← 核心，保留（调用辅助类）
    ├── 存档 (~100行) ← 核心，保留（委托辅助类提供数据）
    ├── Gizmo (~100行) ← 保留（读辅助类缓存）
    ├── 战斗体管理 (~50行) ← 核心，保留
    └── 生命周期 (~100行) ← 核心，保留

重构后：
  CompTriggerBody (~850行, -47%)
    ├── 持有 ChipVerbBuilder 实例
    ├── 持有 ComboAbilityManager 实例
    ├── DoActivate → builder.Rebuild() + comboMgr.TryGrant()
    ├── DeactivateSlot → comboMgr.TryRevoke()
    └── Gizmo → 读 builder 缓存

  ChipVerbBuilder (~400行, 新建)
    ├── Rebuild(pawn, verbTracker, sideVerbProps, activeSlots)
    ├── Verb缓存属性（left/right/dual/volley/combo）
    ├── 序列化辅助：CollectSavedVerbs() / SetSavedVerbs()
    └── 依赖：DualVerbCompositor, ComboVerbDef, WeaponChipConfig

  ComboAbilityManager (~60行, 新建)
    ├── TryGrant(pawn, leftSlot, rightSlot)
    ├── TryRevoke(pawn, leftSlot, rightSlot)
    └── grantedCombos 列表
```

**数据流：**
- CompTriggerBody → ChipVerbBuilder：传入 pawn, verbTracker, 按侧VerbProps, 激活槽位
- ChipVerbBuilder → CompTriggerBody：返回 Verb 缓存引用（供 Gizmo 读取）
- CompTriggerBody → ComboAbilityManager：传入 pawn, 左右激活槽位
- 序列化：CompTriggerBody.PostExposeData 调用 builder.CollectSavedVerbs() / builder.SetSavedVerbs()

**构建顺序：** ChipVerbBuilder 先（最大块），ComboAbilityManager 后（依赖简单），IChipEffect 扩展最后

## Pitfalls

1. **序列化断裂**：savedChipVerbs 的收集/恢复必须在 CompTriggerBody.PostExposeData 中保持，ChipVerbBuilder 只提供 Collect/Set 方法，不直接参与 Scribe
2. **Verb loadID 一致性**：loadID 格式 `BDP_Chip_{thingID}_{index}` 必须与重构前完全一致，否则读档时 Job/Stance 解析失败
3. **激活上下文时序**：RebuildVerbs 在 effect.Activate() 内部被调用，此时 slot.isActive 尚未设为 true → GetActiveOrActivatingSlot 逻辑必须保持
4. **C# 7.3 无默认接口方法**：IChipEffect 新增方法（如 OnKilledPawn）必须在 WeaponChipEffect/ShieldChipEffect/UtilityChipEffect 中都加空实现
5. **Gizmo 读缓存引用**：CompGetEquippedGizmosExtra 直接读 leftHandAttackVerb 等字段，提取后需通过 builder 属性访问
