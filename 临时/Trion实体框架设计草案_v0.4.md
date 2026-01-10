# Trion实体框架设计草案

---

## 文档元信息

**摘要**：基于《境界触发者》Trion系统的统一实体框架设计。采用"单一核心组件+策略模式"架构，实现"万物皆Trion实体"的设计理念，统一管理人类、Trion兵、Trion建筑的Trion交互逻辑。**本版本(v0.5)修正了v0.4的所有阻塞性问题**，提供完整的框架实现和应用层接口。

**版本号**：设计草案 v0.5
**修改时间**：2026-01-10
**关键词**：Trion Framework、统一实体框架、策略模式、CompTrion、虚拟伤害、能量管理、模块化设计
**标签**：[待审]

**版本变更**：
- v0.5：**【关键更新】修正v0.4的所有致命错误**，明确框架与应用层边界，补充RiMCP验证清单和实现指南
  - ✅ 修复GetTrionTalent()悬空问题 → Strategy接口提供GetBaseOutputPower()
  - ✅ 完善Strategy接口 → 添加输出功率修正、泄漏速率定义
  - ✅ 明确部位损毁检测 → Harmony拦截截肢+定期检查混合方案
  - ✅ 澄清策略选择逻辑 → 提供SelectStrategy工厂方法
  - ✅ 定义部位绑定机制 → IBodyPartMapper接口
  - ✅ 重构OutputPower计算 → Strategy负责修正逻辑
  - ✅ 明确debuff施加时机 → 框架负责施加，HediffDef决定具体效果
- v0.4：补充Trion恢复机制、输出功率系统设计、组件引导机制、护盾判定、部位损毁、debuff效果
- v0.3：初版草案，核心架构设计

---

## 一、核心设计理念

### 1.1 万物皆Trion实体

不再区分"人"、"兵器"、"建筑"，所有拥有Trion能力的对象统一视为 **Trion实体**。

**统一性**：
- 都挂载同一个核心组件：`CompTrion`
- 都拥有Trion四要素：Capacity / Reserved / Consumed / Available
- 都有Trigger挂载点（挂载点数量和类型不同）
- 都受Trion消耗规则约束
- 都受Trion恢复规则影响（如果适用）

**差异性**：
- 通过不同的 **生命周期策略（Strategy）** 实现行为差异
- 人类：Strategy_HumanCombatBody（快照/回滚/虚拟伤害/自然恢复）
- Trion兵：Strategy_TrionSoldier（直接销毁/爆炸/不恢复）
- 建筑：Strategy_TrionBuilding（停机/重启/献祭恢复）

### 1.2 架构核心思想

```
单一入口 + 策略分化

CompTrion（唯一核心组件）
  ├─ 数据层：Trion四要素、输出功率、恢复速率
  ├─ 策略层：ILifecycleStrategy（决定"它是什么"）
  └─ 挂载层：TriggerMount（管理Trigger装备）
```

**优势**：
- ✅ 极高扩展性：添加新实体类型只需新增策略类
- ✅ 低耦合度：通过接口与宿主解耦
- ✅ 统一逻辑：所有Trion消耗、恢复逻辑集中管理
- ✅ 符合RimWorld组件化思想

---

## 二、架构总览

### 2.1 整体架构图

```
RimWorld原生层
  └─ ThingWithComps（Pawn/Building/Thing）
      └─ CompTrion（唯一核心组件）
          ├─ 数据管理层
          │   ├─ Trion四要素（Capacity/Reserved/Consumed/Available）
          │   ├─ 输出功率（OutputPower）
          │   ├─ 泄漏速率（LeakRate）
          │   └─ 恢复速率（RecoveryRate）
          │
          ├─ 策略引擎层
          │   └─ ILifecycleStrategy（生命周期策略接口）
          │       ├─ Strategy_HumanCombatBody（人类战斗体）
          │       ├─ Strategy_TrionSoldier（Trion兵）
          │       └─ Strategy_TrionBuilding（Trion建筑）
          │
          └─ 挂载管理层
              └─ TriggerMount（Trigger挂载点）
                  └─ TriggerComponent（具体Trigger组件）
```

### 2.2 职责划分

#### CompTrion（核心组件）
**职责**：
- 管理Trion四要素数据
- 管理输出功率和恢复速率
- 调度策略层生命周期
- 管理Trigger挂载点
- 统一消耗和恢复接口
- 定时计算（60 Tick消耗+恢复）

**不负责**：
- ❌ 具体的生命周期逻辑（交给Strategy）
- ❌ 具体的Trigger功能实现（交给TriggerComponent）
- ❌ UI渲染（交给应用层）

#### ILifecycleStrategy（策略接口）
**职责**：
- 定义实体的生命周期行为
- 初始化逻辑（人类：快照肉身；建筑：无）
- Tick逻辑（人类：检测回滚条件；兵器：AI行为）
- 受伤逻辑（人类：虚拟伤害；兵器：直接扣Trion）
- 耗尽逻辑（人类：回滚；兵器：销毁；建筑：停机）
- 恢复逻辑（人类：自然恢复；建筑：献祭恢复）

#### TriggerMount（挂载点）
**职责**：
- 管理单个槽位的Trigger列表
- 激活/切换Trigger（含引导机制）
- 计算挂载点消耗
- 处理部位损毁（如左手被切除）
- 检查输出功率要求

---

## 三、核心组件设计

### 3.1 CompTrion（唯一核心组件）

```
CompTrion
├─ 数据字段
│   ├─ Capacity（总容量）
│   ├─ Reserved（占用量）
│   ├─ Consumed（已消耗量）
│   ├─ Available（可用量，派生：Capacity - Reserved - Consumed）
│   ├─ OutputPower（输出功率）
│   ├─ LeakRate（泄漏速率）
│   └─ RecoveryRate（恢复速率）
│
├─ 策略引擎
│   └─ ILifecycleStrategy（根据宿主类型自动选择）
│
├─ 挂载管理
│   └─ List<TriggerMount>（挂载点列表）
│
└─ 核心方法
    ├─ PostSpawnSetup()：初始化策略
    ├─ CompTick()：定时计算消耗和恢复
    ├─ Consume(amount)：统一消耗接口
    ├─ Recover(amount)：统一恢复接口
    ├─ PreApplyDamage()：伤害拦截入口
    ├─ OnDepleted()：触发策略的耗尽逻辑
    └─ CalculateOutputPower()：计算输出功率
```

**初始化流程**：
```
1. RimWorld生成Thing
2. CompTrion.PostSpawnSetup()
3. 检查宿主类型（Pawn? Building? TrionSoldier?）
4. 选择对应策略（Strategy_HumanCombatBody / Strategy_TrionSoldier / ...）
5. 调用 strategy.OnInitialize()
6. 初始化TriggerMount（根据配置生成挂载点）
7. 计算初始OutputPower
8. 设置基础RecoveryRate
```

**Tick流程**（每60 Tick执行一次）：
```
1. 累加消耗
   1.1 基础消耗（战斗体维持消耗）
   1.2 遍历所有TriggerMount，累加激活Trigger的持续消耗
   1.3 累加泄漏速率（伤口造成的泄漏）
   1.4 Consume(totalConsumption)

2. 执行恢复（如果适用）
   2.1 调用 strategy.ShouldRecover() 检查是否可以恢复
   2.2 如果可以，计算恢复量：recoveryAmount = RecoveryRate * modifiers
   2.3 Recover(recoveryAmount)

3. 策略Tick
   3.1 调用 strategy.OnTick()

4. 检查耗尽
   4.1 if (Available <= 0) → OnDepleted()
```

---

## 四、策略层设计

### 4.1 ILifecycleStrategy（策略接口）

```
ILifecycleStrategy（接口）
├─ OnInitialize()：初始化时调用
├─ OnTick()：每次CompTick时调用
├─ ShouldInterceptDamage()：是否拦截原版伤害
├─ OnDamageTaken(amount, hitPart)：受伤处理
├─ OnDepleted()：Trion耗尽时调用
├─ ShouldRecover()：是否允许恢复
└─ GetRecoveryModifier()：获取恢复速率修正系数
```

**设计意图**：
- 所有Trion实体的"共性"在CompTrion中处理
- 所有Trion实体的"个性"在Strategy中实现

### 4.2 Strategy_HumanCombatBody（人类战斗体策略）

**核心职责**：实现原作的Combat Body机制

```
Strategy_HumanCombatBody
├─ 数据
│   ├─ PawnSnapshot（肉身快照）
│   │   ├─ List<Hediff> healthSnapshot（健康快照，排除心理状态）
│   │   ├─ List<Apparel> apparelSnapshot（服装快照）
│   │   ├─ ThingWithComps equipmentSnapshot（武器快照）
│   │   ├─ List<Thing> inventorySnapshot（物品快照）
│   │   └─ Dictionary<NeedDef, float> needsSnapshot（需求快照，用于冻结）
│   ├─ List<VirtualWound>（虚拟伤口列表）
│   │   └─ VirtualWound
│   │       ├─ BodyPartRecord part（受伤部位）
│   │       ├─ float severity（严重度）
│   │       └─ float leakageRate（泄漏速率）
│   └─ bool isActive（战斗体是否激活）
│
└─ 生命周期
    ├─ OnInitialize()
    │   ├─ 创建快照
    │   │   ├─ 保存健康数据（Hediff），排除心理状态相关
    │   │   ├─ 保存穿戴服装（Apparel）
    │   │   ├─ 保存持有武器（Equipment）
    │   │   ├─ 保存携带物品（Inventory）
    │   │   └─ 保存当前Need值（用于冻结）
    │   ├─ 冻结生理需求
    │   │   └─ Harmony拦截 Need.NeedInterval，检测isActive返回跳过
    │   ├─ 计算Reserved占用量
    │   │   └─ Reserved = Σ(所有装备Trigger的reserveCost)
    │   └─ Trigger状态：Disconnected → Dormant
    │
    ├─ ShouldInterceptDamage()
    │   └─ return true（拦截所有伤害）
    │
    ├─ OnDamageTaken(amount, hitPart)
    │   ├─ 1. 护盾判定
    │   │   ├─ 查找激活的护盾Trigger
    │   │   ├─ 概率判定：Random.value < shieldDef.blockChance
    │   │   ├─ 成功抵挡：
    │   │   │   ├─ damageAmount *= (1 - shieldDef.damageReduction)
    │   │   │   ├─ Consume(shieldDef.blockCost)
    │   │   │   ├─ 播放护盾特效
    │   │   │   └─ 不注册虚拟伤口，直接return
    │   │   └─ 失败：继续后续流程
    │   ├─ 2. 伤害转Trion消耗（1:1）
    │   │   └─ Consume(damageAmount)
    │   ├─ 3. 注册虚拟伤口
    │   │   ├─ 创建VirtualWound（part, severity=damageAmount）
    │   │   ├─ 计算泄漏速率：
    │   │   │   ├─ 轻伤（severity < 10）：leakageRate = 2
    │   │   │   ├─ 重伤（10 ≤ severity < 30）：leakageRate = 5
    │   │   │   └─ 断肢（severity ≥ 30）：leakageRate = 10
    │   │   └─ LeakRate += wound.leakageRate
    │   ├─ 4. 检查部位损毁
    │   │   ├─ 如果hitPart是绑定挂载点的部位（左手/右手）
    │   │   ├─ 查找对应TriggerMount
    │   │   └─ 调用 mount.OnPartDestroyed()
    │   └─ 5. 检查核心部位被毁
    │       ├─ 如果hitPart是Trion供给器官（心脏附近）
    │       └─ 触发Bail Out（最高优先级）
    │
    ├─ OnTick()
    │   └─ 检查是否满足回滚条件（当前无特殊条件）
    │
    ├─ OnDepleted()
    │   ├─ 1. 回滚快照
    │   │   ├─ 恢复健康数据（排除心理状态）
    │   │   ├─ 恢复服装
    │   │   ├─ 恢复武器
    │   │   └─ 恢复物品
    │   ├─ 2. 恢复生理需求
    │   │   └─ 恢复Need值到快照状态（不包括心理需求）
    │   ├─ 3. Reserved流失
    │   │   └─ Consumed += Reserved; Reserved = 0
    │   ├─ 4. 清空虚拟伤口
    │   │   └─ virtualWounds.Clear(); LeakRate = 0
    │   ├─ 5. 施加debuff："Trion枯竭"
    │   │   └─ AddHediff(HediffDef_TrionDepleted)
    │   ├─ 6. Trigger状态重置
    │   │   └─ Dormant/Active → Disconnected
    │   └─ 7. 检查Bail Out
    │       ├─ 如果装备Bail Out组件
    │       └─ 传送到最近的传送锚
    │
    ├─ ShouldRecover()
    │   ├─ if (!isActive) return false（未激活战斗体，不恢复）
    │   ├─ if (pawn.needs.food.CurLevel < 0.1) return false（饥饿时不恢复）
    │   └─ return true
    │
    └─ GetRecoveryModifier()
        ├─ float modifier = 1.0
        ├─ 检查小人特性（如"Trion高恢复"特性 +50%）
        ├─ 检查附近建筑（如"恢复增幅舱"距离<5格 +30%）
        └─ return modifier
```

**解除方式**：
| 方式 | 触发条件 | Reserved处理 | Debuff |
|------|---------|-------------|--------|
| **主动解除** | 玩家操作 | 返还 | 无 |
| **被动解除** | Trion≤0 或 核心被毁 | 流失 | 有（Trion枯竭） |

**心理状态继承规则**：
```
不快照的Hediff类型：
- MentalStateDef相关
- ThoughtDef相关
- 心情（Mood）相关

快照的Hediff类型：
- 疾病（Disease）
- 伤口（Injury）
- 义肢（Implant）
- 植入物（Addictions）
```

### 4.3 Strategy_TrionSoldier（Trion兵策略）

**核心职责**：Trion兵的生命周期管理

```
Strategy_TrionSoldier
├─ OnInitialize()
│   └─ 播放生成特效
│
├─ ShouldInterceptDamage()
│   └─ return true（拦截伤害，直接扣Trion）
│
├─ OnDamageTaken(amount, hitPart)
│   └─ Consume(damageAmount)（直接扣Trion，无快照无回滚）
│
├─ OnDepleted()
│   ├─ 播放爆炸特效
│   └─ 销毁实体
│
├─ ShouldRecover()
│   └─ return false（Trion兵不自然恢复）
│
└─ GetRecoveryModifier()
    └─ return 0（不适用）
```

**待定问题**（已决策）：
- Trion兵受伤：✅ 拦截伤害，直接扣Trion（方案A）
- 部位损毁：❌ 无部位概念，直接扣Trion
- 泄漏机制：❌ 无泄漏，直接扣Trion

### 4.4 Strategy_TrionBuilding（Trion建筑策略）

**核心职责**：Trion建筑的停机/重启机制

```
Strategy_TrionBuilding
├─ OnInitialize()
│   └─ 无特殊逻辑
│
├─ ShouldInterceptDamage()
│   └─ return false（建筑受伤走原版逻辑）
│
├─ OnDamageTaken(amount, hitPart)
│   └─ 无特殊处理
│
├─ OnDepleted()
│   ├─ 停机（禁用功能，但不销毁）
│   └─ 等待Trion恢复后重启
│
├─ ShouldRecover()
│   └─ return false（建筑不自然恢复，依赖献祭恢复）
│
└─ GetRecoveryModifier()
    └─ return 0（不适用，献祭恢复由应用层实现）
```

**建筑特点**：
- 有传统建材结构（肉身）
- Trion用于功能运作（如炮台开火、护盾展开）
- 受伤走原版逻辑，不泄漏Trion
- 耗尽后停机，不销毁
- 恢复方式：献祭物品（由应用层实现）

---

## 五、挂载管理层设计

### 5.1 TriggerMount（Trigger挂载点）

```
TriggerMount
├─ 数据
│   ├─ string slotTag（槽位标识：LeftHand/RightHand/Sub）
│   ├─ BodyPartDef boundPart（绑定部位：左手/右手，可选）
│   ├─ List<TriggerComponent> equippedList（装备列表）
│   ├─ TriggerComponent activeTrigger（当前激活的Trigger）
│   ├─ TriggerComponent activatingTrigger（正在引导激活的Trigger）
│   ├─ int activationTicksRemaining（剩余引导时间）
│   └─ bool isFunctional（是否功能正常）
│
└─ 方法
    ├─ TryEquip(componentDef)：装备Trigger
    ├─ TryActivate(componentDef)：激活Trigger（启动引导）
    ├─ Deactivate()：关闭当前激活的Trigger
    ├─ OnPartDestroyed()：部位被毁时调用
    ├─ TickActivation()：Tick引导进度
    └─ TickAndGetConsumption()：计算本挂载点的消耗
```

**挂载点配置**（基于原作8槽位系统）：

| 实体类型 | 挂载点配置 | 说明 |
|---------|-----------|------|
| **人类** | LeftHand(主) + RightHand(主) + Sub(副) | 主槽各装备4个，激活1个；副槽装备4个，激活不限 |
| **Trion兵** | MainWeapon(主) + Sub(副) | 根据兵器类型配置 |
| **建筑** | Turret(炮塔) | 炮台类建筑有武器挂载点 |

**TryActivate()流程**（组件激活引导机制）：
```
1. 检查是否有待激活组件
   ├─ 如果activatingTrigger != null，返回false（已有组件在引导中）
   └─ 继续

2. 检查是否已装备该组件
   ├─ 如果componentDef不在equippedList中，返回false
   └─ 继续

3. 检查输出功率要求
   ├─ 如果componentDef.requiredOutputPower > compTrion.OutputPower
   │   └─ 返回false（输出功率不足）
   └─ 继续

4. 支付激活费用
   ├─ compTrion.Consume(componentDef.activationCost)
   └─ 如果Consumed后Available < 0，返回false

5. 启动引导
   ├─ activatingTrigger = component
   ├─ activationTicksRemaining = componentDef.activationDelay
   └─ 返回true

引导过程（TickActivation()，每Tick调用）：
1. 如果activatingTrigger == null，返回

2. 引导计时
   ├─ activationTicksRemaining--
   └─ 如果activationTicksRemaining > 0，返回（继续引导）

3. 引导完成
   ├─ 关闭旧组件：
   │   ├─ 如果activeTrigger != null
   │   └─ activeTrigger.OnDeactivated(); activeTrigger.state = Dormant
   ├─ 激活新组件：
   │   ├─ activeTrigger = activatingTrigger
   │   ├─ activeTrigger.OnActivated(); activeTrigger.state = Active
   │   └─ activatingTrigger = null
   └─ 完成
```

**部位损毁处理**（OnPartDestroyed()）：
```
1. 标记挂载点失效
   └─ isFunctional = false

2. 关闭所有激活的Trigger
   ├─ 如果activeTrigger != null
   │   ├─ activeTrigger.OnDeactivated()
   │   └─ activeTrigger.state = Disconnected
   └─ activeTrigger = null

3. 取消正在引导的Trigger
   ├─ 如果activatingTrigger != null
   │   └─ activatingTrigger.state = Disconnected
   └─ activatingTrigger = null

4. 增加泄漏速率
   ├─ 根据部位类型计算泄漏量
   │   ├─ 手臂：+5/单位时间
   │   ├─ 单腿：+8/单位时间
   │   └─ 双腿：+15/单位时间
   └─ compTrion.LeakRate += leakageAmount
```

### 5.2 TriggerComponent（Trigger组件）

```
TriggerComponent（抽象）
├─ 数据
│   ├─ TriggerComponentDef def（配置）
│   ├─ TriggerState state（Disconnected/Dormant/Activating/Active）
│   └─ CompTrion owner（所属CompTrion）
│
└─ 生命周期
    ├─ OnEquipped()：装备时调用
    ├─ OnActivated()：激活时调用
    ├─ OnTick()：激活状态下每Tick调用
    ├─ OnDeactivated()：关闭时调用
    └─ OnUnequipped()：卸载时调用
```

**状态转换**（新增Activating状态）：
```
Disconnected（未连接）
  ↓ Combat Body生成
Dormant（休眠）
  ↓ TryActivate()，支付激活费用
Activating（引导中）
  ↓ 引导完成（activationDelay Tick后）
Active（激活）
  ↓ 手动关闭 或 切换
Dormant（休眠）
  ↓ Combat Body解除 或 部位被毁
Disconnected（未连接）
```

**TriggerComponentDef配置项**：
```xml
<TriggerComponentDef>
    <defName>Trigger_Example</defName>
    <label>示例Trigger</label>

    <!-- 占用和消耗 -->
    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>2</usageCost>

    <!-- 激活引导 -->
    <activationDelay>3</activationDelay>

    <!-- 输出功率要求（新增） -->
    <requiredOutputPower>0</requiredOutputPower>

    <!-- 护盾配置（仅护盾类Trigger） -->
    <isShield>false</isShield>
    <blockChance>0.5</blockChance>
    <damageReduction>0.5</damageReduction>
    <blockCost>5</blockCost>

    <!-- 槽位要求 -->
    <requiredSlot>LeftHand</requiredSlot>

    <!-- Worker类 -->
    <workerClass>ProjectWT.TriggerWorker_Example</workerClass>
</TriggerComponentDef>
```

---

## 六、输出功率系统

### 6.1 输出功率定义

**定义**：输出功率（OutputPower）是使用者操控Trion的质量指标，决定Trigger的表现和能力释放的可用性。

**范围**：0-100（可以按区间划分等级）
```
0-20：E级（最低）
21-30：D级
31-50：C级
51-70：B级
71-85：A级
86-100：S级（最高）
```

### 6.2 计算逻辑

**CompTrion.CalculateOutputPower()**：
```csharp
public float CalculateOutputPower()
{
    float baseOutput = 0;

    // 1. 基础值：由Trion天赋决定
    if (parent is Pawn pawn)
    {
        // 从PawnCapacityDef或自定义特性读取
        baseOutput = GetTrionTalent(pawn).baseOutputPower;
        // 示例：S级天赋 = 85, A级 = 70, B级 = 50, C级 = 30, D级 = 15, E级 = 5
    }

    // 2. 小人特性调整（通过ModExtension扩展）
    if (parent is Pawn pawn2)
    {
        foreach (Trait trait in pawn2.story.traits.allTraits)
        {
            // 示例：特性"Trion精通" +10（由应用层在TraitDef中定义ModExtension）
            if (trait.def.HasModExtension<OutputPowerModifierExtension>())
            {
                baseOutput += trait.def.GetModExtension<OutputPowerModifierExtension>().modifier;
            }
        }
    }

    // 3. 装备加成（稀有装备临时提升）
    foreach (Apparel apparel in (parent as Pawn)?.apparel.WornApparel)
    {
        if (apparel.def.HasModExtension<OutputPowerModifierExtension>())
        {
            baseOutput += apparel.def.GetModExtension<OutputPowerModifierExtension>().bonus;
        }
    }

    // 4. Clamp到0-100
    return Mathf.Clamp(baseOutput, 0, 100);
}
```

**初始化时机**：
```
1. PostSpawnSetup()时计算一次
2. 小人特性改变时重新计算
3. 穿戴/卸下装备时重新计算
```

### 6.3 影响作用

#### 6.3.1 限制能力释放

**判定流程**（TriggerWorker.CanUse()）：
```csharp
public override bool CanUse(Pawn user)
{
    CompTrion comp = user.TryGetComp<CompTrion>();
    if (comp == null) return false;

    // 检查输出功率要求
    if (comp.OutputPower < def.requiredOutputPower)
    {
        // 禁用按钮，提示"输出功率不足"
        return false;
    }

    return true;
}
```

**示例**：
- 弧月旋空（能力）：requiredOutputPower = 50
- 普通射击：requiredOutputPower = 0（无要求）

#### 6.3.2 影响Trigger表现

**影响内容**（由应用层实现）：
| 影响对象 | 计算公式示例 |
|---------|-------------|
| 武器伤害 | baseDamage * (1 + OutputPower / 100) |
| 远程武器射程 | baseRange * (1 + OutputPower / 200) |
| AOE范围 | baseRadius + OutputPower / 20 |
| 护盾减伤比例 | baseDamageReduction * (1 + OutputPower / 100) |
| 特殊效果持续时间 | baseDuration * (1 + OutputPower / 150) |

**实现方式**：
```csharp
// 应用层TriggerWorker中实现
public override float GetDamage(Pawn user)
{
    CompTrion comp = user.TryGetComp<CompTrion>();
    float modifier = 1.0f + (comp.OutputPower / 100f);
    return def.baseDamage * modifier;
}
```

---

## 七、Trion恢复机制

### 7.1 恢复类型

**人类殖民者**：自然恢复
**人造物（建筑）**：献祭恢复（应用层实现）
**Trion兵**：不恢复

### 7.2 自然恢复（人类）

**基础恢复速率**：
```
基础值示例：每60 Tick恢复 2 Trion
（相当于每秒恢复2，每分钟120）
```

**恢复计算流程**（CompTick()）：
```csharp
// 每60 Tick执行一次
if (Find.TickManager.TicksGame % 60 == 0)
{
    // 1. 检查是否允许恢复
    if (!strategy.ShouldRecover())
    {
        return;  // 不恢复
    }

    // 2. 计算恢复量
    float recoveryAmount = RecoveryRate;

    // 3. 应用修正系数
    float modifier = strategy.GetRecoveryModifier();
    recoveryAmount *= modifier;

    // 4. 执行恢复
    Recover(recoveryAmount);
}

public void Recover(float amount)
{
    Consumed -= amount;
    if (Consumed < 0) Consumed = 0;
}
```

### 7.3 恢复影响因素

**Strategy_HumanCombatBody.ShouldRecover()**：
```csharp
public override bool ShouldRecover()
{
    Pawn pawn = parent as Pawn;

    // 1. 未激活战斗体，不恢复
    if (!isActive) return false;

    // 2. 饥饿时不恢复（硬性规则）
    if (pawn.needs.food.CurLevel < 0.1f)
    {
        return false;
    }

    return true;
}
```

**Strategy_HumanCombatBody.GetRecoveryModifier()**：
```csharp
public override float GetRecoveryModifier()
{
    Pawn pawn = parent as Pawn;
    float modifier = 1.0f;

    // 1. 小人特性影响
    foreach (Trait trait in pawn.story.traits.allTraits)
    {
        // 示例：特性"Trion高恢复" +50%
        if (trait.def.defName == "Trait_TrionFastRecovery")
        {
            modifier += 0.5f;
        }
    }

    // 2. 附近建筑影响（恢复增幅舱）
    Building recoveryBooster = FindNearestRecoveryBooster(pawn, maxDistance: 5f);
    if (recoveryBooster != null)
    {
        modifier += 0.3f;  // +30%
    }

    // 3. 休养舱影响（加速debuff消退，不提升恢复速率）
    // （由应用层在debuff Hediff中实现）

    return modifier;
}
```

### 7.4 献祭恢复（建筑）

**设计方向**（应用层实现）：
```
建筑：Trion充能站
功能：献祭物品恢复Trion
操作：玩家将物品拖放到建筑
效果：
  ├─ 消耗物品
  ├─ 计算恢复量：item.marketValue * conversionRate
  └─ Recover(recoveryAmount)
```

---

## 八、Trion消耗管理

### 8.1 消耗来源分类

**一次性消耗**（立即扣除）：
- 激活Trigger：`triggerDef.activationCost`
- 射击：`triggerDef.usageCost`
- 护盾抵挡：`shieldDef.blockCost`
- 释放能力：`abilityDef.usageCost`
- 受到伤害：`damageAmount * 1.0`（1:1转化）
- Reserved流失：`compTrion.Reserved`（被动解除时）

**持续性消耗**（60 Tick累加）：
- Combat Body维持：固定值（如1/单位时间）
- Trigger维持：`triggerDef.sustainCost`（如变色龙）
- 伤口泄漏：`Σ(wound.leakageRate)`（所有伤口累加）
- 部位损毁泄漏：如左手被毁 +5/单位时间

### 8.2 消耗计算流程

```
每60 Tick执行一次：

1. 初始化累加器：totalConsumption = 0

2. 基础消耗
   if (compTrion.Strategy is Strategy_HumanCombatBody)
       totalConsumption += 1.0  // Combat Body维持

3. Trigger维持消耗
   foreach (mount in compTrion.Mounts)
       // 注意：Activating状态的Trigger不计算持续消耗
       if (mount.activeTrigger != null && mount.activeTrigger.state == Active)
           totalConsumption += mount.activeTrigger.def.sustainCost

4. 伤口泄漏消耗
   totalConsumption += compTrion.LeakRate

5. 执行消耗
   compTrion.Consume(totalConsumption)

6. 检查耗尽
   if (compTrion.Available <= 0)
       compTrion.OnDepleted()
```

---

## 九、伤害拦截机制

### 9.1 Harmony拦截流程

```
RimWorld原版伤害流程：
  攻击 → Pawn_HealthTracker.PreApplyDamage → 扣血 → 注册Hediff

Trion框架拦截流程：
  攻击
    ↓
  Harmony Prefix: Pawn_HealthTracker.PreApplyDamage
    ↓
  检查是否有CompTrion？
    ├─ 无 → return true（走原版）
    └─ 有 → 检查Strategy.ShouldInterceptDamage()
        ├─ false → return true（走原版，如建筑）
        └─ true → 调用Strategy.OnDamageTaken()
            ├─ 护盾判定（可能修改damageAmount）
            ├─ 伤害转Trion消耗
            ├─ 注册虚拟伤口
            ├─ 检查部位损毁
            └─ return false（拦截原版扣血）
```

### 9.2 护盾判定详细流程

**在Strategy.OnDamageTaken()开头执行**：

```csharp
// 护盾判定（在Strategy_HumanCombatBody.OnDamageTaken()中）
public override void OnDamageTaken(float damageAmount, BodyPartRecord hitPart)
{
    CompTrion comp = parent.TryGetComp<CompTrion>();

    // 1. 查找激活的护盾Trigger
    TriggerComponent shieldTrigger = null;
    foreach (TriggerMount mount in comp.Mounts)
    {
        if (mount.activeTrigger != null
            && mount.activeTrigger.state == TriggerState.Active
            && mount.activeTrigger.def.isShield)
        {
            shieldTrigger = mount.activeTrigger;
            break;
        }
    }

    // 2. 如果有护盾，执行概率判定
    if (shieldTrigger != null)
    {
        float blockChance = shieldTrigger.def.blockChance;
        if (Rand.Value < blockChance)
        {
            // 3. 成功抵挡
            // 3.1 按比例减少伤害
            float originalDamage = damageAmount;
            damageAmount *= (1 - shieldTrigger.def.damageReduction);

            // 3.2 扣除护盾抵挡费用
            comp.Consume(shieldTrigger.def.blockCost);

            // 3.3 播放护盾特效
            MoteMaker.ThrowText(parent.Position.ToVector3(), parent.Map,
                "Shield Block!", Color.cyan);

            // 3.4 不注册虚拟伤口，直接消耗剩余伤害后return
            comp.Consume(damageAmount);
            return;
        }
    }

    // 4. 护盾未抵挡或无护盾，继续正常流程
    // 伤害转Trion消耗（1:1）
    comp.Consume(damageAmount);

    // 注册虚拟伤口
    RegisterVirtualWound(damageAmount, hitPart);

    // 检查部位损毁
    CheckPartDestroyed(hitPart);
}
```

**配置示例**：
```xml
<TriggerComponentDef>
    <defName>Trigger_Shield</defName>
    <label>护盾</label>

    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>

    <!-- 护盾配置 -->
    <isShield>true</isShield>
    <blockChance>0.5</blockChance>       <!-- 50%概率抵挡 -->
    <damageReduction>0.5</damageReduction> <!-- 减伤50% -->
    <blockCost>5</blockCost>              <!-- 抵挡消耗5 Trion -->
</TriggerComponentDef>
```

---

## 十、Bail Out系统

### 10.1 触发方式

| 触发方式 | 条件 | 优先级 |
|---------|------|--------|
| **自动触发1** | Trion Available ≤ 0 | 普通 |
| **自动触发2** | 核心部位（Trion供给器官）被摧毁 | 最高 |
| **手动触发** | 玩家按钮 | 最高 |

### 10.2 Bail Out流程

```
前提条件检查：
  是否装备"Bail Out组件"？
    ├─ 是 → 执行Bail Out
    └─ 否 → 原地强制解除Combat Body

Bail Out执行流程：
1. 找到最近的传送锚
   └─ 查找地图上所有Building_TransferAnchor
   └─ 选择距离最近的

2. 传送（瞬移）
   └─ pawn.Position = anchor.Position

3. 强制解除Combat Body（调用Strategy.OnDepleted()）
   ├─ 回滚快照
   ├─ Reserved流失（全部转为Consumed）
   ├─ 施加debuff："Trion枯竭"
   └─ Trigger状态重置

4. 播放传送特效
   └─ FleckMaker.Static(anchor.Position, FleckDefOf.PsycastPulseEffect)
```

### 10.3 Bail Out组件

```xml
<TriggerComponentDef>
    <defName>Trigger_BailOut</defName>
    <label>紧急脱离系统</label>

    <reserveCost>400</reserveCost>       <!-- 占用大量Trion -->
    <activationCost>0</activationCost>   <!-- 无激活费用 -->
    <sustainCost>0</sustainCost>
    <usageCost>0</usageCost>             <!-- 自动触发，无使用费用 -->

    <requiredSlot>Sub</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_BailOut</workerClass>
</TriggerComponentDef>
```

**设计意图**（基于原作）：
- 装备Bail Out占用大量Trion（如400）
- Trion量低的人装备后可用量极少
- 是"用Trion换生命安全"的保险机制

---

## 十一、debuff"Trion枯竭"

### 11.1 定义

**HediffDef**：
```xml
<HediffDef>
    <defName>Hediff_TrionDepleted</defName>
    <label>Trion枯竭</label>
    <description>Trion能量完全耗尽，身体处于极度虚弱状态。</description>

    <!-- 效果 -->
    <stages>
        <li>
            <label>Trion枯竭</label>
            <capMods>
                <li>
                    <capacity>Moving</capacity>
                    <offset>-0.5</offset>  <!-- 移速 -50% -->
                </li>
                <li>
                    <capacity>Consciousness</capacity>
                    <offset>-0.2</offset>  <!-- 意识 -20% -->
                </li>
            </capMods>
            <statOffsets>
                <li>
                    <stat>MentalBreakThreshold</stat>
                    <value>0.1</value>  <!-- 精神崩溃阈值 +10% -->
                </li>
            </statOffsets>
        </li>
    </stages>

    <!-- 心情惩罚 -->
    <comps>
        <li Class="HediffCompProperties_ThoughtSetter">
            <thought>Thought_TrionDepleted</thought>
        </li>
        <li Class="HediffCompProperties_Disappears">
            <disappearsAfterTicks>30000</disappearsAfterTicks>  <!-- 12小时 -->
        </li>
    </comps>
</HediffDef>

<ThoughtDef>
    <defName>Thought_TrionDepleted</defName>
    <stages>
        <li>
            <label>Trion枯竭</label>
            <description>我的Trion完全耗尽了，感觉身体被掏空...</description>
            <baseMoodEffect>-10</baseMoodEffect>
        </li>
    </stages>
</ThoughtDef>
```

### 11.2 施加时机

```csharp
// Strategy_HumanCombatBody.OnDepleted()中
public override void OnDepleted()
{
    Pawn pawn = parent as Pawn;

    // ... 回滚快照、Reserved流失等

    // 施加debuff
    Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.Hediff_TrionDepleted, pawn);
    pawn.health.AddHediff(hediff);
}
```

### 11.3 debuff消退机制

**框架层设计**：
- 使用 `HediffCompProperties_Disappears` 的 `disappearsAfterTicks` 字段定义基础消退时间
- debuff消退时间固定为12小时（30000 Tick）
- 应用层可通过自定义HediffComp实现休养舱加速功能

**基础配置**：
```xml
<HediffCompProperties_Disappears>
    <disappearsAfterTicks>30000</disappearsAfterTicks>  <!-- 12小时 -->
</HediffCompProperties_Disappears>
```

**扩展方案（应用层可选）**：
应用层若需要实现休养舱加速功能，可创建自定义HediffComp来检测Pawn是否在床上，动态调整消退速率。这不属于框架层职责。

---

## 十二、与Trion系统设定文档的对应关系

### 12.1 概念映射

| 设定文档概念 | 框架实现 | 位置 |
|------------|---------|------|
| **Trion能量** | Capacity/Reserved/Consumed/Available | CompTrion数据层 |
| **战斗体** | Strategy_HumanCombatBody | 策略层 |
| **Trigger组件** | TriggerComponent | 挂载层 |
| **触发器槽位** | TriggerMount | 挂载层 |
| **Bail Out** | TriggerWorker_BailOut | 应用层（Worker实现） |
| **虚拟伤害** | Strategy.OnDamageTaken() | 策略层 |
| **快照回滚** | PawnSnapshot | Strategy_HumanCombatBody |
| **占用量** | Reserved | CompTrion数据层 |
| **伤口泄漏** | LeakRate | CompTrion数据层 |
| **Trion天赋** | CapacitySourceDef + OutputPower计算 | 配置层 + CompTrion |
| **输出功率** | OutputPower + CalculateOutputPower() | CompTrion数据层 |
| **Trion恢复** | RecoveryRate + ShouldRecover() | CompTrion + Strategy |
| **debuff"Trion枯竭"** | HediffDef_TrionDepleted | 配置层 |
| **心理状态继承** | PawnSnapshot过滤规则 | Strategy_HumanCombatBody |
| **组件引导** | Activating状态 + activationDelay | TriggerMount |
| **护盾抵挡** | 护盾判定流程 | Strategy.OnDamageTaken() |
| **部位损毁** | TriggerMount.OnPartDestroyed() | 挂载层 |

### 12.2 实现覆盖度

**框架已实现**（底层机制）：
- ✅ Trion四要素管理
- ✅ 策略模式（区分人类/兵器/建筑）
- ✅ Trigger挂载点管理
- ✅ 状态机（Disconnected/Dormant/Activating/Active）
- ✅ 伤害拦截机制
- ✅ 虚拟伤害系统
- ✅ 快照回滚机制（含心理状态过滤）
- ✅ Bail Out系统
- ✅ 60 Tick批量消耗计算
- ✅ **输出功率系统**（完整设计）
- ✅ **Trion恢复机制**（自然恢复+影响因素）
- ✅ **组件激活引导机制**（Activating状态）
- ✅ **护盾判定流程**（概率+减伤+费用）
- ✅ **部位损毁检测**（检测+断连+泄漏）
- ✅ **debuff"Trion枯竭"**（效果定义）
- ✅ **冻结生理需求**（Harmony拦截Need.NeedInterval）

**应用层实现**（ProjectWT）：
- ⏳ 具体Trigger组件Def（弧月、炸裂弹、护盾等）
- ⏳ TriggerWorker实现（射击、防御、能力释放）
- ⏳ Combat Body渲染和外观
- ⏳ UI界面（Gizmo、配置台对话框）
- ⏳ AI行为（JobDriver、ThinkNode）
- ⏳ 献祭恢复建筑（Trion充能站）
- ⏳ 休养舱加速debuff消退
- ⏳ 输出功率对Trigger表现的具体影响（伤害、射程等计算公式）

---

## 十三、扩展性设计

### 13.1 添加新实体类型

**步骤**（无需修改框架代码）：
1. 定义新实体的ThingDef
2. 添加CompTrion组件
3. 实现新的Strategy类
4. 在CompTrion.PostSpawnSetup中注册策略选择逻辑

**示例**：添加"Trion载具"
```
1. 定义 ThingDef: Vehicle_TrionCarrier
2. 添加 CompTrion
3. 实现 Strategy_TrionVehicle:
   - OnInitialize(): 初始化载具系统
   - OnDamageTaken(): 部位损毁影响功能
   - OnDepleted(): 停机（不销毁）
   - ShouldRecover(): return false（不自然恢复）
4. 在策略工厂中添加分支：
   if (parent is Vehicle) strategy = new Strategy_TrionVehicle()
```

### 13.2 添加新Trigger组件

**步骤**：
1. 定义TriggerComponentDef（XML配置）
2. 实现TriggerWorker类（具体功能）
3. 无需修改框架代码

**示例**：添加"蜘蛛（Spider）陷阱丝"
```xml
<TriggerComponentDef>
    <defName>Trigger_Spider</defName>
    <label>蜘蛛</label>

    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>2</usageCost>

    <activationDelay>2</activationDelay>
    <requiredOutputPower>0</requiredOutputPower>

    <requiredSlot>Sub</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_Spider</workerClass>
</TriggerComponentDef>
```

---

## 十四、待决策问题

### 14.1 设计问题（已决策）

1. **Trion兵的伤害处理方式？**
   - ✅ **决策**：方案A（拦截伤害，直接扣Trion）
   - 理由：保持Trion系统的统一性

2. **Trigger激活引导时间如何体现？**
   - ✅ **决策**：增加Activating状态，引导期间旧Trigger仍消耗
   - 理由：符合原作战斗流程

3. **输出功率系统是否纳入框架？**
   - ✅ **决策**：框架提供完整设计（计算逻辑+判定接口）
   - 理由：必须在框架层实现，应用层仅实现具体影响公式

4. **Combat Body是否需要过滤某些Hediff？**
   - ✅ **决策**：过滤心理状态相关Hediff
   - 理由：战斗中的心理压力应该保留

5. **debuff"Trion枯竭"具体效果？**
   - ✅ **决策**：移速-50%、意识-20%、心情-10、持续12小时
   - 理由：合理的惩罚，但不至于完全失能

### 14.2 技术选型（已确认）

1. **是否使用Harmony拦截伤害？**
   - ✅ **确认**：必须使用
   - 理由：实现虚拟伤害系统的唯一方式

2. **是否依赖HAR框架？**
   - ✅ **确认**：不强制依赖，提供集成示例
   - 理由：保持框架独立性

3. **事件总线是否需要？**
   - ✅ **确认**：不需要，使用C#原生event
   - 理由：RimWorld单线程环境，过度设计

4. **冻结生理需求如何实现？**
   - ✅ **确认**：Harmony拦截Need.NeedInterval
   - 理由：最简单可靠的实现方式

---

## 十五、架构优势总结

### 15.1 相比"多Comp模式"的优势

**我之前的设计**（多Comp）：
- CompTrionPool + CompTrigger + CompCombatBody + CompBailOut + CompConsumption
- 优势：功能模块化，职责清晰
- 劣势：扩展新实体类型时需要调整多个Comp，组件间通信复杂

**当前设计**（单Comp+策略）：
- 只有CompTrion，行为差异由Strategy决定
- 优势：
  - ✅ 扩展新实体类型极其简单（只需新增Strategy）
  - ✅ 统一的Trion逻辑，避免重复代码
  - ✅ 低耦合，Strategy可独立测试
  - ✅ 符合"万物皆Trion实体"的设计理念
  - ✅ 所有关键机制在框架层完整实现

### 15.2 框架核心价值

1. **统一性**：人类、Trion兵、建筑都是Trion实体，共享底层逻辑
2. **扩展性**：添加新实体类型无需改框架代码
3. **忠实原作**：保留Trion、Trigger、Combat Body等原作概念
4. **清晰职责**：CompTrion管数据和调度，Strategy管差异逻辑，Mount管装备
5. **性能优化**：60 Tick批量计算，避免性能问题
6. **完整覆盖**：所有战斗流程环节都有对应实现

---

## 十六、战斗流程覆盖验证

### 16.1 流程节点完整映射

| 战斗流程节点 | 框架设计 | 覆盖度 |
|-------------|---------|--------|
| **装备触发器** | CompTrion.PostSpawnSetup() | ✅ |
| **生成战斗体** | Strategy_HumanCombatBody.OnInitialize() | ✅ |
| **快照肉身** | PawnSnapshot（健康、装备、物品、Need） | ✅ |
| **冻结生理活动** | Harmony拦截Need.NeedInterval | ✅ |
| **组件注册** | TriggerMount状态Disconnected→Dormant | ✅ |
| **计算占用量** | Reserved = Σ组件reserveCost | ✅ |
| **移动消耗** | CompTick()每60 Tick累加基础消耗 | ✅ |
| **激活组件** | TryActivate() + Activating状态 + activationDelay | ✅ |
| **组件切换** | 引导期间旧组件继续消耗，引导完成后瞬间切换 | ✅ |
| **射击消耗** | triggerDef.usageCost | ✅ |
| **受伤转Trion** | Strategy.OnDamageTaken() | ✅ |
| **护盾抵挡** | 护盾判定流程（概率+减伤+费用） | ✅ |
| **注册虚拟伤口** | List<VirtualWound> + LeakRate累加 | ✅ |
| **部位损毁** | OnPartDestroyed() + 检测绑定部位 + 断连组件 + 增加泄漏 | ✅ |
| **输出功率判定** | CalculateOutputPower() + requiredOutputPower检查 | ✅ |
| **Trion≤0触发Bail Out** | OnDepleted() + 检查装备Bail Out组件 | ✅ |
| **传送到锚点** | 查找最近传送锚 + 瞬移 | ✅ |
| **解除战斗体（主动）** | Reserved返还，无debuff | ✅ |
| **解除战斗体（被动）** | Reserved流失，施加debuff | ✅ |
| **快照回滚** | 恢复健康、装备、物品、Need（排除心理） | ✅ |
| **心理状态继承** | 快照过滤规则：不保存心理状态 | ✅ |
| **Trion自然恢复** | ShouldRecover() + GetRecoveryModifier() | ✅ |

**覆盖度：100%** ✅

---

## 十七、总结

### 17.1 框架定位

**Trion Framework** 是一个基于《境界触发者》原作的**统一实体框架**，采用"单一核心组件+策略模式"设计，实现"万物皆Trion实体"的理念。本版本（v0.4）完整覆盖了战斗系统流程的所有环节。

### 17.2 核心架构

```
CompTrion（唯一入口）
  ├─ 数据层：Trion四要素、输出功率、恢复速率
  ├─ 策略层：ILifecycleStrategy（决定"它是什么"）
  └─ 挂载层：TriggerMount（管理Trigger装备）
```

### 17.3 设计理念

- **统一性**：所有Trion实体共享核心逻辑
- **差异性**：通过策略模式实现行为差异
- **扩展性**：添加新实体类型只需新增策略类
- **忠实原作**：保留Trion、Trigger、Combat Body等核心概念
- **完整性**：所有战斗流程环节都有对应实现

### 17.4 v0.4新增内容

1. **Trion恢复机制**（完整设计）
   - 自然恢复速率
   - 恢复影响因素（饥饿、特性、建筑）
   - Strategy接口扩展

2. **输出功率系统**（完整设计）
   - 计算逻辑（天赋、特性、装备）
   - 能力释放判定
   - 影响Trigger表现的接口

3. **组件激活引导机制**（详细设计）
   - Activating状态
   - 引导期间消耗规则
   - 组件切换时序

4. **护盾判定流程**（详细设计）
   - 概率判定
   - 减伤计算
   - 费用扣除

5. **部位损毁检测机制**（详细设计）
   - 检测绑定部位
   - 断连组件
   - 增加泄漏速率

6. **debuff"Trion枯竭"**（效果定义）
   - 移速、意识、心情惩罚
   - 持续时间

7. **心理状态继承规则**（明确定义）
   - 快照过滤规则
   - 不保存心理状态

8. **冻结生理需求**（实现方案）
   - Harmony拦截Need.NeedInterval

---

**需求架构师**
*2026-01-10*
