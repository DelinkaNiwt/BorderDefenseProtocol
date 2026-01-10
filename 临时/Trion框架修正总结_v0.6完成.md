# Trion框架修正总结 - v0.6完成

---

## 文档元信息

**摘要**：记录从v0.4→v0.5→v0.6的框架修正过程、问题分析、和最终解决方案

**版本号**：1.0
**修改时间**：2026-01-10
**关键词**：框架修正、问题分析、架构纠正、分层设计
**标签**：[定稿]

---

## 一、问题诊断

### 问题1：具体Strategy实现写在框架层

**表现**：v0.5中存在这些类的完整实现：
```
├─ Strategy_HumanCombatBody
├─ Strategy_TrionSoldier
└─ Strategy_TrionBuilding
```

**根本原因**：混淆了"框架层"和"应用层"的职责

**后果**：
- 框架失去扩展性（新增实体类型需要修改框架代码）
- 框架包含业务逻辑
- 不符合开闭原则

**用户提问**：*"为什么人、Trion兵、炮台的策略实现要写入框架层里？万一我不止这三种呢？"*

**解决**：删除所有具体Strategy实现类，框架只定义ILifecycleStrategy接口

---

### 问题2：硬编码数值参数散落在框架代码中

**表现**：
```csharp
// ❌ 错误示例
private float GetLeakageRate(VirtualWound wound)
{
    if (wound.severity < 10) return 2.0f;      // 硬编码
    if (wound.severity < 30) return 5.0f;      // 硬编码
    return 10.0f;                              // 硬编码
}

private float RecoveryRate = 2.0f;             // 硬编码
```

**根本原因**：框架代码直接决定游戏平衡参数

**后果**：
- 调整游戏平衡需要修改框架代码
- 不同难度/玩法无法使用不同参数
- 应用层无法自主决定数值

**解决**：所有数值改为：
- 来自应用层Def（TriggerComponentDef的属性）
- 来自Strategy实现方法（GetBaseMaintenance、GetLeakageRate等）

---

### 问题3：SelectStrategy是具体实现而非机制

**表现**：
```csharp
// ❌ 错误
private ILifecycleStrategy SelectStrategy(ThingWithComps thing)
{
    if (thing.def.HasModExtension<TrionSoldierMarker>())
        return new Strategy_TrionSoldier(this);  // 框架直接实例化
    // ...
}
```

**根本原因**：框架认为自己应该知道如何选择策略

**后果**：
- 框架与具体实体类型耦合
- 添加新策略类型需要修改框架
- 应用层无法自定义策略选择逻辑

**解决**：改为委托机制：
```csharp
// ✅ 正确
public Action<CompTrion, ThingWithComps> StrategyProvider { get; set; }

// 应用层在初始化时设置
CompTrion.StrategyProvider = (comp, thing) =>
{
    if (thing.def.HasModExtension<TrionSoldierMarker>())
        return new Strategy_TrionSoldier(comp);
    // ...
};
```

---

## 二、设计原则确立

**核心原则**：**框架只定义接口和通用机制**

### 如何判断？

```
问题：这是通用机制还是具体实现？

如果是"只适用于某一种情况" → 删除，改为接口
如果是"所有实体都会经历这个" → 保留为通用机制
```

### 框架的四层职责

| 层级 | 框架职责 | 示例 |
|------|--------|------|
| **接口层** | 定义契约 | ILifecycleStrategy |
| **数据层** | 管理四要素 | Capacity、Reserved、Consumed |
| **机制层** | 通用调度流程 | CompTick的执行顺序 |
| **API层** | 公开调用接口 | Consume、Recover、SetStrategy |

### 应用层职责

| 方面 | 应用层决定 | 示例 |
|------|----------|------|
| **选择** | 使用哪个Strategy | StrategyProvider回调 |
| **参数** | 所有数值 | RecoveryRate、LeakageRate |
| **逻辑** | 业务流程 | 护盾判定、debuff创建 |
| **实现** | 所有具体类 | Strategy_HumanCombatBody |

---

## 三、修正清单

### ✅ 已删除

- [x] Strategy_HumanCombatBody 具体实现
- [x] Strategy_TrionSoldier 具体实现
- [x] Strategy_TrionBuilding 具体实现
- [x] 硬编码的恢复速率（2.0f）
- [x] 硬编码的泄漏速率（2、5、10）
- [x] 硬编码的基础消耗（1.0f）
- [x] SelectStrategy中的具体判定逻辑
- [x] CompTick中的业务逻辑

### ✅ 已改为接口/委托

- [x] Strategy实现 → ILifecycleStrategy接口
- [x] 恢复速率 → strategy.GetRecoveryModifier()
- [x] 泄漏速率 → strategy.GetLeakageRate(wound)
- [x] 基础消耗 → strategy.GetBaseMaintenance()
- [x] 策略选择 → CompTrion.StrategyProvider委托

### ✅ 已清理应用层代码

- [x] 护盾计算逻辑（移至Strategy.OnDamageTaken）
- [x] debuff创建（移至Strategy.OnDamageTaken）
- [x] 部位断连（移至Strategy.OnBodyPartLost）
- [x] 快照回滚（移至Strategy.OnDepleted）

---

## 四、v0.6最终架构概览

### 框架层（CompTrion）- 纯调度

```
PostSpawnSetup
    └─> 注入Strategy (由应用层提供)
    └─> strategy.OnInitialize()

CompTick (每60 Tick)
    ├─> 累加消耗 (strategy.GetBaseMaintenance + mounts.sustainCost + leakage)
    ├─> 执行消耗 (Consume方法)
    ├─> 恢复检查 (strategy.ShouldRecover)
    ├─> 调用恢复 (RecoveryRate * strategy.GetRecoveryModifier)
    ├─> 调用策略Tick (strategy.OnTick)
    └─> 检查耗尽 (strategy.OnDepleted)

PreApplyDamage (伤害拦截)
    └─> strategy.OnDamageTaken(damageAmount, hitPart)

OnBodyPartLost (部位丧失)
    └─> strategy.OnBodyPartLost(part)
```

### 应用层实现

```
Strategy_HumanCombatBody
    ├─> OnInitialize() [注册快照系统]
    ├─> OnDamageTaken() [护盾判定、debuff创建]
    ├─> OnBodyPartLost() [部位断连、泄漏增加]
    ├─> OnDepleted() [快照回滚、施加debuff]
    ├─> GetBaseMaintenance() [返回40]
    ├─> GetLeakageRate(wound) [根据严重度返回泄漏速率]
    └─> ... 其他实现

Strategy_TrionSoldier
    ├─> OnInitialize() [初始化战斗体状态]
    ├─> OnDamageTaken() [能量消耗]
    ├─> OnDepleted() [强制返回]
    └─> ... 其他实现

Strategy_TrionBuilding
    ├─> OnInitialize() [初始化建筑]
    ├─> OnDepleted() [关闭所有Trigger]
    └─> ... 其他实现
```

---

## 五、数值来源路径

```
组件初始化
    ├─> Capacity: 应用层Pawn属性 or Def
    ├─> OutputPower: strategy.GetBaseOutputPower() → strategy.ModifyOutputPower()
    ├─> RecoveryRate: 应用层Def
    └─> LeakRate: strategy.GetLeakageRate(wound)

每Tick消耗
    ├─> BaseMaintenance: strategy.GetBaseMaintenance()
    ├─> TriggerSustain: TriggerComponentDef.sustainCost
    └─> Leakage: LeakRate (由strategy维护)

恢复计算
    └─> RecoveryRate * strategy.GetRecoveryModifier()

激活费用 (TriggerMount.TryActivate)
    └─> TriggerComponentDef.activationCost
```

---

## 六、与设定文档的对应关系

### Trion四要素 ✅ 完整支持

| 要素 | 框架支持 | 应用层配置 |
|------|--------|----------|
| **容量** | Capacity属性 | 应用层Def |
| **保留** | Reserved属性 | 应用层维护 |
| **消耗** | Consumed属性 | 框架自动计算 |
| **可用** | Available属性（只读） | 自动计算 |

### 战斗系统流程 ✅ 完整支持

| 流程 | 框架支持 | 应用层实现 |
|------|--------|----------|
| **伤害拦截** | PreApplyDamage入口 | Strategy.OnDamageTaken |
| **伤害转换** | Consume方法 | 应用层在OnDamageTaken中调用 |
| **部位丧失** | OnBodyPartLost入口 | Strategy.OnBodyPartLost |
| **快照/回滚** | 事件回调 | Strategy实现具体逻辑 |
| **虚拟伤口** | VirtualWound数据结构 | 应用层创建/管理 |
| **Trigger激活** | TriggerMount状态机 | 应用层调用TryActivate |

---

## 七、后续工作建议

### 第一阶段：框架验证 ✅ 已完成

- [x] 删除具体Strategy实现
- [x] 删除硬编码数值
- [x] 改为接口+委托模式
- [x] 清理应用层代码

### 第二阶段：应用层补充（待进行）

- [ ] 根据框架接口实现所有Strategy类
- [ ] 定义所有Def中的数值参数
- [ ] 实现业务逻辑（护盾、debuff等）
- [ ] 编写Harmony补丁

### 第三阶段：用例清单澄清（待进行）

用户之前指出："用例清单里为什么有些是'框架支持-无'？它们既然是Trion建筑就一定在某种角度上与Trion有所关联才对。"

需要逐一审视标注为"框架支持-无"的项目，可能的原因：
- 这些是应用层负责实现的功能，框架不直接支持
- 需要向用户澄清这些项的性质和框架支持方式

---

## 八、关键设计决策回顾

### 为什么用委托而不是工厂模式？

```csharp
// 委托方案 ✅ 更灵活
public Action<CompTrion, ThingWithComps> StrategyProvider { get; set; }

// 工厂方案 (可选)
public IStrategyFactory StrategyFactory { get; set; }
```

**选择委托的原因**：
- 更轻量（无需创建工厂类）
- 应用层可以动态改变策略选择逻辑
- 支持Mod之间的集成

### 为什么TriggerMount不完全通用化？

TriggerMount仍然有具体的业务逻辑（activationDelay计时、sustainCost计费等），为什么？

**原因**：这些是**通用机制**，不是**具体实现**
- 任何能源系统都需要"激活引导"机制
- 任何能源系统都需要"持续消耗"机制
- 这些不是"Trion特有"，是"能源系统通用"

### 为什么strategy.GetLeakageRate需要VirtualWound参数？

```csharp
float GetLeakageRate(VirtualWound wound);
```

**原因**：不同伤口有不同泄漏速率，框架提供入参给Strategy判断

---

## 九、验证清单

### 代码检查 ✅

- [x] CompTrion中没有硬编码数值
- [x] CompTrion中没有Strategy实例化
- [x] ILifecycleStrategy接口完整
- [x] TriggerMount状态机清晰
- [x] 所有业务逻辑都标注为应用层

### 设计检查 ✅

- [x] 框架/应用层边界清晰
- [x] 所有扩展点都公开
- [x] 接口设计完整且最小化
- [x] 没有循环依赖
- [x] 遵循开闭原则

### 文档检查 ✅

- [x] 设计原则明确
- [x] 职责分工清晰
- [x] 实现示例完整
- [x] API文档齐全

---

## 十、总结

v0.6框架完成了从**混合层设计**到**纯净框架设计**的转变：

| 方面 | v0.5（错误） | v0.6（正确） |
|------|----------|---------|
| **Strategy** | 具体实现在框架中 | 仅接口在框架中 |
| **数值** | 硬编码2.0f、5.0f等 | 来自应用层Def和接口 |
| **策略选择** | 框架中的具体判定 | 应用层委托回调 |
| **职责混合** | 框架包含业务逻辑 | 框架仅定义机制 |
| **扩展性** | 添加新类型需修改框架 | 仅需应用层实现 |

**框架现在是**：
- ✅ 纯净的（无业务逻辑）
- ✅ 可扩展的（支持任意Strategy）
- ✅ 灵活的（所有数值由应用层决定）
- ✅ 可维护的（单一职责）
- ✅ 可验证的（接口清晰）

---

**需求架构师**
*2026-01-10*

| 版本 | 内容 | 时间 | 修改者 |
|------|------|------|-------|
| 1.0 | 完成v0.6修正总结 | 2026-01-10 | 需求架构师 |
