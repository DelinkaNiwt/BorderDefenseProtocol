# Trion实体框架设计草案

---

## 文档元信息

**摘要**：基于《境界触发者》Trion系统的统一实体框架设计。采用"单一核心组件+策略模式"架构，实现"万物皆Trion实体"的设计理念，统一管理人类、Trion兵、Trion建筑的Trion交互逻辑。

**版本号**：草案 v0.3
**修改时间**：2026-01-10
**关键词**：Trion Framework、统一实体框架、策略模式、CompTrion、Combat Body
**标签**：[草稿]

---

## 一、核心设计理念

### 1.1 万物皆Trion实体

不再区分"人"、"兵器"、"建筑"，所有拥有Trion能力的对象统一视为 **Trion实体**。

**统一性**：
- 都挂载同一个核心组件：`CompTrion`
- 都拥有Trion四要素：Capacity / Reserved / Consumed / Available
- 都有Trigger挂载点（挂载点数量和类型不同）
- 都受Trion消耗规则约束

**差异性**：
- 通过不同的 **生命周期策略（Strategy）** 实现行为差异
- 人类：Strategy_HumanCombatBody（快照/回滚/虚拟伤害）
- Trion兵：Strategy_TrionSoldier（直接销毁/爆炸）
- 建筑：Strategy_TrionBuilding（停机/重启）

### 1.2 架构核心思想

```
单一入口 + 策略分化

CompTrion（唯一核心组件）
  ├─ 数据层：Trion四要素、输出功率
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
          │   └─ 泄漏速率（LeakRate）
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
- 调度策略层生命周期
- 管理Trigger挂载点
- 统一消耗接口
- 定时计算（60 Tick）

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

#### TriggerMount（挂载点）
**职责**：
- 管理单个槽位的Trigger列表
- 激活/切换Trigger
- 计算挂载点消耗
- 处理部位损毁（如左手被切除）

---

## 三、核心组件设计

### 3.1 CompTrion（唯一核心组件）

```
CompTrion
├─ 数据字段
│   ├─ Capacity（总容量）
│   ├─ Reserved（占用量）
│   ├─ Consumed（已消耗量）
│   ├─ Available（可用量，派生）
│   ├─ OutputPower（输出功率）
│   └─ LeakRate（泄漏速率）
│
├─ 策略引擎
│   └─ ILifecycleStrategy（根据宿主类型自动选择）
│
├─ 挂载管理
│   └─ List<TriggerMount>（挂载点列表）
│
└─ 核心方法
    ├─ PostSpawnSetup()：初始化策略
    ├─ CompTick()：定时计算消耗
    ├─ Consume(amount)：统一消耗接口
    ├─ PreApplyDamage()：伤害拦截入口
    └─ OnDepleted()：触发策略的耗尽逻辑
```

**初始化流程**：
```
1. RimWorld生成Thing
2. CompTrion.PostSpawnSetup()
3. 检查宿主类型（Pawn? Building? TrionSoldier?）
4. 选择对应策略（Strategy_HumanCombatBody / Strategy_TrionSoldier / ...）
5. 调用 strategy.OnInitialize()
6. 初始化TriggerMount（根据配置生成挂载点）
```

**Tick流程**：
```
每60 Tick执行一次：
1. 累加基础消耗（战斗体维持消耗）
2. 遍历所有TriggerMount，累加激活Trigger的持续消耗
3. 累加泄漏速率（伤口造成的泄漏）
4. Consume(totalConsumption)
5. 调用 strategy.OnTick()
6. 检查是否耗尽 → 触发OnDepleted()
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
└─ OnDepleted()：Trion耗尽时调用
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
│   ├─ List<VirtualWound>（虚拟伤口列表）
│   └─ bool isActive（战斗体是否激活）
│
└─ 生命周期
    ├─ OnInitialize()
    │   ├─ 创建快照（健康、装备、物品）
    │   ├─ 冻结生理需求（饥饿、睡眠不变化）
    │   ├─ 计算Reserved占用量
    │   └─ Trigger状态：Disconnected → Dormant
    │
    ├─ ShouldInterceptDamage()
    │   └─ return true（拦截所有伤害）
    │
    ├─ OnDamageTaken(amount, hitPart)
    │   ├─ 护盾判定（如果有护盾Trigger激活）
    │   ├─ 伤害转Trion消耗（1:1）
    │   ├─ 注册虚拟伤口（增加LeakRate）
    │   └─ 检查核心部位被毁 → 触发Bail Out
    │
    ├─ OnTick()
    │   └─ 检查是否满足回滚条件
    │
    └─ OnDepleted()
        ├─ 回滚快照（恢复健康、装备、物品）
        ├─ 恢复生理需求
        ├─ Reserved流失（转为Consumed）
        ├─ 施加debuff："Trion枯竭"
        ├─ Trigger状态：Dormant/Active → Disconnected
        └─ 检查Bail Out → 传送到锚点
```

**解除方式**：
| 方式 | 触发条件 | Reserved处理 | Debuff |
|------|---------|-------------|--------|
| **主动解除** | 玩家操作 | 返还 | 无 |
| **被动解除** | Trion≤0 或 核心被毁 | 流失 | 有 |

### 4.3 Strategy_TrionSoldier（Trion兵策略）

**核心职责**：Trion兵的生命周期管理

```
Strategy_TrionSoldier
├─ OnInitialize()
│   └─ 播放生成特效
│
├─ ShouldInterceptDamage()
│   └─ return false（不拦截，走原版血量逻辑）
│       或 return true（拦截，直接扣Trion）
│
├─ OnDamageTaken(amount, hitPart)
│   └─ 直接扣Trion（无快照无回滚）
│
└─ OnDepleted()
    ├─ 播放爆炸特效
    └─ 销毁实体
```

**待定问题**：
- Trion兵受伤是走原版血量，还是也用虚拟伤害？
- 是否有部位损毁影响功能？
- 是否有泄漏机制？

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
└─ OnDepleted()
    ├─ 停机（禁用功能，但不销毁）
    └─ 等待Trion恢复后重启
```

**建筑特点**：
- 有传统建材结构（肉身）
- Trion用于功能运作（如炮台开火、护盾展开）
- 受伤走原版逻辑，不泄漏Trion
- 耗尽后停机，不销毁

---

## 五、挂载管理层设计

### 5.1 TriggerMount（Trigger挂载点）

```
TriggerMount
├─ 数据
│   ├─ string slotTag（槽位标识：LeftHand/RightHand/Sub）
│   ├─ BodyPartDef boundPart（绑定部位：左手/右手）
│   ├─ List<TriggerComponent> equippedList（装备列表）
│   ├─ TriggerComponent activeTrigger（当前激活的Trigger）
│   └─ bool isFunctional（是否功能正常）
│
└─ 方法
    ├─ TryEquip(componentDef)：装备Trigger
    ├─ TryActivate(componentDef)：激活Trigger
    ├─ Deactivate()：关闭当前激活的Trigger
    ├─ OnPartDestroyed()：部位被毁时调用
    └─ TickAndGetConsumption()：计算本挂载点的消耗
```

**挂载点配置**（基于原作8槽位系统）：

| 实体类型 | 挂载点配置 | 说明 |
|---------|-----------|------|
| **人类** | LeftHand(主) + RightHand(主) + Sub(副) | 主槽各装备4个，激活1个；副槽装备4个，激活不限 |
| **Trion兵** | MainWeapon(主) + Sub(副) | 根据兵器类型配置 |
| **建筑** | Turret(炮塔) | 炮台类建筑有武器挂载点 |

### 5.2 TriggerComponent（Trigger组件）

```
TriggerComponent（抽象）
├─ 数据
│   ├─ TriggerComponentDef def（配置）
│   ├─ TriggerState state（Disconnected/Dormant/Active）
│   └─ int activationDelay（激活引导时间）
│
└─ 生命周期
    ├─ OnEquipped()：装备时调用
    ├─ OnActivated()：激活时调用
    ├─ OnTick()：激活状态下每Tick调用
    ├─ OnDeactivated()：关闭时调用
    └─ OnUnequipped()：卸载时调用
```

**状态转换**：
```
Disconnected（未连接）
  ↓ Combat Body生成
Dormant（休眠）
  ↓ 支付激活费用 + 引导时间
Active（激活）
  ↓ 手动关闭 或 切换
Dormant（休眠）
  ↓ Combat Body解除 或 部位被毁
Disconnected（未连接）
```

---

## 六、Trion消耗管理

### 6.1 消耗来源分类

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
- 伤口泄漏：`wound.leakageRate`（伤势严重度相关）

### 6.2 消耗计算流程

```
每60 Tick执行一次：

1. 初始化累加器：totalConsumption = 0

2. 基础消耗
   if (compTrion.Strategy is Strategy_HumanCombatBody)
       totalConsumption += 1.0  // Combat Body维持

3. Trigger维持消耗
   foreach (mount in compTrion.Mounts)
       if (mount.activeTrigger != null)
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

## 七、伤害拦截机制

### 7.1 Harmony拦截流程

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
            ├─ 护盾判定
            ├─ 伤害转Trion消耗
            ├─ 注册虚拟伤口
            └─ return false（拦截原版扣血）
```

### 7.2 护盾判定逻辑

```
护盾判定流程（在Strategy.OnDamageTaken之前）：

1. 检查是否有激活的护盾Trigger
2. 概率判定（基于护盾配置）
3. 如果成功抵挡：
   ├─ 按比例减少伤害
   ├─ 扣除护盾抵挡费用
   ├─ 不注册虚拟伤口
   └─ 播放护盾特效
4. 如果抵挡失败：
   └─ 继续正常伤害流程
```

---

## 八、Bail Out系统

### 8.1 触发方式

| 触发方式 | 条件 | 优先级 |
|---------|------|--------|
| **自动触发1** | Trion Available ≤ 0 | 普通 |
| **自动触发2** | 核心部位被摧毁 | 最高 |
| **手动触发** | 玩家按钮 | 最高 |

### 8.2 Bail Out流程

```
前提条件检查：
  是否装备"Bail Out组件"？
    ├─ 是 → 执行Bail Out
    └─ 否 → 原地强制解除Combat Body

Bail Out执行流程：
1. 找到最近的传送锚
2. 传送（瞬移）
3. 强制解除Combat Body（调用Strategy.OnDepleted()）
   ├─ 回滚快照
   ├─ Reserved流失
   ├─ 施加debuff
   └─ Trigger状态重置
4. 播放传送特效
```

### 8.3 Bail Out组件

```
TriggerComponentDef: BailOut
├─ reserveCost: 400（占用大量Trion）
├─ activationCost: 0
├─ sustainCost: 0
├─ usageCost: 0（自动触发，无使用费用）
└─ workerClass: TriggerWorker_BailOut
```

**设计意图**（基于原作）：
- 装备Bail Out占用大量Trion（如400）
- Trion量低的人装备后可用量极少
- 是"用Trion换生命安全"的保险机制

---

## 九、与Trion系统设定文档的对应关系

### 9.1 概念映射

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
| **Trion天赋** | CapacitySourceDef | 配置层 |

### 9.2 实现覆盖度

**框架已实现**（底层机制）：
- ✅ Trion四要素管理
- ✅ 策略模式（区分人类/兵器/建筑）
- ✅ Trigger挂载点管理
- ✅ 状态机（Disconnected/Dormant/Active）
- ✅ 伤害拦截机制
- ✅ 虚拟伤害系统
- ✅ 快照回滚机制
- ✅ Bail Out系统
- ✅ 60 Tick批量消耗计算

**应用层实现**（ProjectWT）：
- ⏳ 具体Trigger组件Def（弧月、炸裂弹、护盾等）
- ⏳ TriggerWorker实现（射击、防御、能力释放）
- ⏳ Combat Body渲染和外观
- ⏳ UI界面（Gizmo、配置台对话框）
- ⏳ AI行为（JobDriver、ThinkNode）
- ⏳ 输出功率系统（可选扩展）
- ⏳ 副作用系统（可选扩展）

---

## 十、扩展性设计

### 10.1 添加新实体类型

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
4. 在策略工厂中添加分支：
   if (parent is Vehicle) strategy = new Strategy_TrionVehicle()
```

### 10.2 添加新Trigger组件

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
    <usageCost>2</usageCost>

    <requiredSlot>Sub</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_Spider</workerClass>
</TriggerComponentDef>
```

---

## 十一、待决策问题

### 11.1 设计问题

1. **Trion兵的伤害处理方式？**
   - 方案A：拦截伤害，直接扣Trion（无血量概念）
   - 方案B：不拦截，走原版血量，Trion仅用于功能消耗
   - 建议：待确认原作设定或平衡性需求

2. **Trigger激活引导时间如何体现？**
   - 当前设计：activationDelay字段（1-5 Tick）
   - 表现方式：引导期间旧Trigger仍消耗，新Trigger未激活
   - 建议：保留此设计

3. **输出功率系统是否纳入框架？**
   - 当前设计：未包含
   - 作用：影响Trigger表现（伤害、射程等）、限制能力释放
   - 建议：作为扩展模块，框架提供OutputPower字段，应用层实现影响逻辑

4. **Combat Body是否需要过滤某些Hediff？**
   - 当前设计：全部快照/回滚
   - 特殊需求：某些永久性Hediff可能需要保留
   - 建议：添加过滤规则配置

### 11.2 技术选型

1. **是否使用Harmony拦截伤害？**
   - 当前设计：是（Pawn_HealthTracker.PreApplyDamage）
   - 必要性：实现虚拟伤害系统必须拦截
   - 建议：必须使用

2. **是否依赖HAR框架？**
   - 当前设计：不强制依赖
   - 兼容方案：提供HAR集成示例
   - 建议：框架独立，提供HAR适配指南

3. **事件总线是否需要？**
   - 参考文档：有事件总线设计
   - 奥卡姆剃刀：在RimWorld单线程环境中可能过度设计
   - 建议：砍掉独立事件总线，改用C#原生event或直接方法调用

---

## 十二、架构优势总结

### 12.1 相比"多Comp模式"的优势

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

### 12.2 框架核心价值

1. **统一性**：人类、Trion兵、建筑都是Trion实体，共享底层逻辑
2. **扩展性**：添加新实体类型无需改框架代码
3. **忠实原作**：保留Trion、Trigger、Combat Body等原作概念
4. **清晰职责**：CompTrion管数据和调度，Strategy管差异逻辑，Mount管装备
5. **性能优化**：60 Tick批量计算，避免性能问题

---

## 十三、下一步工作

### 13.1 设计阶段

- [ ] 用户审阅架构方向
- [ ] 确认"单Comp+策略"模式是否正确
- [ ] 确认Strategy的职责划分是否清晰
- [ ] 确认是否保留了原作特色
- [ ] 决策待定问题

### 13.2 交付阶段（用户审阅通过后）

生成6份设计文档：
1. Trion Framework核心概念设计
2. 功能详细设计说明书
3. 数据结构设计规范
4. 系统交互流程设计
5. 配置参数定义
6. 技术方案选择与考量

附加：RiMCP验证清单

---

## 十四、总结

### 14.1 框架定位

**Trion Framework** 是一个基于《境界触发者》原作的**统一实体框架**，采用"单一核心组件+策略模式"设计，实现"万物皆Trion实体"的理念。

### 14.2 核心架构

```
CompTrion（唯一入口）
  ├─ 数据层：Trion四要素、输出功率
  ├─ 策略层：ILifecycleStrategy（决定"它是什么"）
  └─ 挂载层：TriggerMount（管理Trigger装备）
```

### 14.3 设计理念

- **统一性**：所有Trion实体共享核心逻辑
- **差异性**：通过策略模式实现行为差异
- **扩展性**：添加新实体类型只需新增策略类
- **忠实原作**：保留Trion、Trigger、Combat Body等核心概念

---

**需求架构师**
*2026-01-10*
