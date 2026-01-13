---
摘要: Trion框架MVP测试方案，从植入→检测→战斗全流程验证框架功能
版本号: v0.1
修改时间: 2026-01-10
关键词: MVP,天赋初始化,植入物,检测仪,战斗流程,快照恢复
标签: [待审]
---

# Trion框架MVP测试方案 v0.1

## 核心设计理念

### 问题背景
- 框架已编译成功，但未经实际测试
- 需要验证7大功能块的正确性
- 需要验证与游戏原生系统的兼容性

### 设计约束
1. **不修改原版代码**：使用植入物+建筑作为交互入口
2. **符合世界观**：天赋确实在生成时拥有，但逻辑上延迟初始化
3. **最小化侵入**：避免Harmony全局补丁，只在必要处使用
4. **快速迭代**：MVP阶段优先验证核心，可简化复杂逻辑

### 关键洞察
检测仪是天赋初始化的**天然触发源**：
- 首次扫描→随机生成天赋→Capacity重算→显示结果
- 后续扫描→直接返回已有天赋
- 符合游戏交互习惯，无需特殊逻辑

---

## 架构设计

### 第一层：植入获取系统

**Trion腺体植入物**（Hediff_TrionGland）
```
殖民者 + 植入腺体 → CompTrion自动初始化
                   ├─ Strategy创建
                   ├─ Capacity=1400（临时值）
                   ├─ Reserved=0, Consumed=0
                   └─ Snapshot初始化
```

**触发时机**：
- Hediff.PostAdd() 时自动添加CompTrion到Pawn
- 无需Harmony补丁，无需外部触发
- 符合RimWorld植入物的标准流程

---

### 第二层：天赋初始化系统

**Trion天赋检测仪**（Building_TrionDetector）

流程图：
```
玩家操作 → 检测仪
           ├─ Pawn有CompTrion？
           │  ├─ 否 → 提示"无Trion腺体"
           │  └─ 是 → 继续
           │
           ├─ 天赋是否已初始化？
           │  ├─ 是 → 直接显示结果
           │  └─ 否 → 继续
           │
           └─ 天赋初始化步骤
              ├─ 随机生成天赋等级（S-E）
              ├─ 存入Pawn.modData
              ├─ 调用CompTrion.RecalculateCapacity()
              │  └─ 使用TalentCapacityProvider查表计算新Capacity
              ├─ 更新UI显示
              └─ 记录日志
```

**天赋等级→Capacity查表**（由TalentCapacityProvider提供）：
| 天赋 | S | A | B | C | D | E |
|------|------|------|------|------|------|------|
| Capacity | 2000 | 1800 | 1600 | 1400 | 1200 | 1000 |

---

### 第三层：组件系统

#### 3.1 组件物品定义（新增！）

**组件分类**：
```
组件（Thing）
├─ 近战武器类
│  ├─ 弧月（消耗10）
│  └─ 其他近战武器
├─ 远程武器类
│  ├─ 炸裂弹（消耗10）
│  └─ 其他远程武器
├─ 辅助类
│  ├─ 护盾生成器（消耗10-20）
│  ├─ 变色龙隐身（消耗10）
│  └─ 其他辅助组件
└─ 特殊类
   └─ 紧急脱离系统（消耗400）
```

**组件物品属性**：
```csharp
public class TrionComponent : Thing
{
    public string componentName;        // "弧月"
    public int trionCost;               // 消耗值：10
    public string componentType;        // "Melee", "Ranged", "Auxiliary", "Special"
    public string componentEffect;      // "近战能力增强"
    public float rarity;                // 稀有度：0-1

    // 可选属性
    public int slotSize = 1;            // 占用的槽位数
    public bool canCoexistWith;         // 是否可与其他组件共存
}
```

**Def示例**：
```xml
<!-- Components_Weapons.xml -->
<ThingDefs>
  <ThingDef>
    <defName>TrionComponent_ArcusBlade</defName>
    <label>弧月（近战武器组件）</label>
    <description>高效能的Trion驱动近战刀具。消耗Trion：10/激活。</description>
    <thingClass>TrionMVP.TrionComponent</thingClass>
    <category>Item</category>
    <stackLimit>1</stackLimit>
    <graphicData>
      <texPath>Things/Item/TrionComponent_ArcusBlade</texPath>
    </graphicData>
    <statBases>
      <Mass>2.5</Mass>
      <MarketValue>500</MarketValue>
    </statBases>
  </ThingDef>
</ThingDefs>
```

#### 3.2 触发器配置（装备组件）

**触发器配置台**（Building_TriggerConfigBench）
```
玩家 → 选择触发器 → 选择组件 → 装备到槽位
                               ↓
                    触发器.AddComponent(comp)
                               ↓
                    组件注册到TriggerMount.components列表
                               ↓
                    计算新的Reserved占用值
```

**重要约束**：
- 战斗体激活后，组件锁定（不能更改）
- 组件可热切换（战斗体未激活时）
- 槽位限制：双手各4个，特殊1个

---

### 第四层：战斗与消耗系统

基于参考文档《Trion战斗系统流程.md》的简化版：

#### 4.1 战斗体生成

```
玩家激活战斗体（UI按钮）
    ↓
CompTrion.GenerateCombatBody()
    ├─ 保存快照：Pawn身体状态、伤口、装备
    ├─ _isInCombat = true
    ├─ 计算Reserved = 所有装备组件的消耗总和
    └─ Strategy.OnCombatBodyGenerated() 回调
```

#### 4.2 每60Tick消耗计算

```
CompTick() → TickConsumption()
    ├─ baseMaintenance = Strategy.GetBaseMaintenance()   [默认5/60tick]
    ├─ mountConsumption = Σ(激活组件的消耗)
    ├─ leakRate = CalculateLeakRate()                    [伤口导致]
    ├─ totalConsumption = base + mount + leak
    ├─ Consumed += totalConsumption
    ├─ 检查Available ≤ 0 → TriggerBailOut()
    └─ Strategy.OnTick() 回调
```

**泄漏计算**：
```
leak = 0.5 + injury.Severity * 0.1
if (关键部位受伤)
    leak *= 2
```

#### 4.3 Bail Out紧急脱离

触发条件：
- Available ≤ 0 **或** Trion供给器官被摧毁

执行步骤：
```
TriggerBailOut()
    ├─ Strategy.CanBailOut() 检查能否脱离
    │  └─ 如否 → 战斗体直接破裂（DestroyReason.BailOutFailed）
    ├─ Strategy.GetBailOutTarget() 获取目标位置
    │  └─ 如无效 → 战斗体破裂
    └─ 执行传送 + DestroyCombatBody(BailOutSuccess)
```

---

### 第五层：状态恢复

#### 5.1 主动解除（玩家按钮）
```
Reserved返还 = 组件占用值继续锁定在Capacity中
Consumed保留 = 战斗中的消耗不返还
快照恢复 = Pawn身体状态回滚
```

#### 5.2 被动解除（Available≤0 or 供给器官摧毁）
```
Reserved流失 = Reserved累加到Consumed
快照恢复 = Pawn身体状态回滚（伤口不继承）
Debuff应用 = "Trion枯竭" 减益效果
```

---

## 测试场景设计

### 场景1：植入→初始化
- **目标**：验证植入物能正确附加CompTrion
- **步骤**：
  1. 新建MVP地图
  2. 生成殖民者A
  3. 用控制台或建筑制造"Trion腺体"物品
  4. 给殖民者A进行植入手术
- **验证标准**：
  - CompTrion存在于殖民者A
  - Strategy已初始化
  - Capacity = 1400（查看UI）
  - 日志记录植入成功

### 场景2：首次检测→天赋初始化
- **目标**：验证检测仪首次扫描能生成天赋并重算Capacity
- **步骤**：
  1. 建造Trion天赋检测仪
  2. 让殖民者A走到检测仪前
  3. 点击交互→扫描
- **验证标准**：
  - 天赋被随机生成（可观察modData或日志）
  - Capacity更新为天赋对应值
  - UI显示"天赋等级：B，Capacity：1600"
  - 第二次扫描显示相同结果

### 场景3：装备触发器和组件
- **目标**：验证组件正确注册到TriggerMount
- **步骤**：
  1. 从地图上找到或制造触发器（可用dev工具生成）
  2. 制造若干组件物品（弧月×1，护盾×2，紧急脱离×1）
  3. 殖民者A装备触发器
  4. 在配置台添加组件
- **验证标准**：
  - 触发器.Mounts列表更新
  - UI显示组件列表正确
  - Reserved计算正确（检查CompInspectStringExtra）

### 场景4：生成战斗体
- **目标**：验证快照保存和Reserved计算
- **步骤**：
  1. 确保殖民者A已装备触发器+组件
  2. 点击"激活战斗体"按钮
  3. 观察UI变化
- **验证标准**：
  - Snapshot包含Pawn当前状态
  - Reserved = 组件占用总和
  - _isInCombat = true
  - 日志显示"战斗体已生成"

### 场景5：基础消耗
- **目标**：验证每60Tick消耗计算
- **步骤**：
  1. 战斗体激活
  2. 让时间推进（等待或用时间控制）
  3. 观察Consumed和Available变化
- **验证标准**：
  - Available逐步减少
  - 消耗速率符合 baseMaintenance（应为5/60tick）
  - 无激活组件时只有基础消耗

### 场景6：激活组件消耗
- **目标**：验证激活组件后的消耗增加
- **步骤**：
  1. 战斗体激活
  2. 手动激活一个组件（如护盾）
  3. 等待几个60Tick周期
  4. 观察Consumed增加速度
- **验证标准**：
  - 组件激活后，消耗速率 = baseMaintenance + 组件消耗
  - 如激活两个组件，消耗进一步增加

### 场景7：伤口泄漏
- **目标**：验证伤口导致的泄漏计算
- **步骤**：
  1. 战斗体激活
  2. 用控制台或敌人攻击给殖民者A造成伤口（如手臂伤口，Severity=0.5）
  3. 观察Consumed增加速度
- **验证标准**：
  - 受伤后消耗速率增加
  - 关键部位伤口消耗速率翻倍
  - 泄漏计算 ≈ 0.5 + 0.5*0.1 = 0.55（关键部位则1.1）

### 场景8：Bail Out触发
- **目标**：验证自动脱离机制
- **步骤**：
  1. 战斗体激活，装备紧急脱离组件
  2. 等待消耗至Available ≤ 0
  3. 或摧毁Trion供给器官（可用控制台）
- **验证标准**：
  - Available ≤ 0时自动触发BailOut
  - Pawn传送到指定目标位置（Strategy.GetBailOutTarget决定）
  - 战斗体破裂，状态变为"Trion枯竭"

### 场景9：快照恢复
- **目标**：验证战斗体摧毁后的状态恢复
- **步骤**：
  1. 战斗体激活前，记录Pawn的HP、部位、伤口
  2. 激活战斗体
  3. 给Pawn造成伤害和伤口
  4. Bail Out脱离或主动解除
  5. 检查Pawn状态
- **验证标准**：
  - Pawn状态完全回滚到快照保存时
  - 战斗体伤口**不继承**到肉身
  - 消耗值保留（如主动解除）或累加（如被动解除）
  - 心理状态不回滚（如有Debuff）

### 场景10：读档持久化
- **目标**：验证Capacity和天赋持久化
- **步骤**：
  1. 完成场景1-4，让殖民者A有天赋、CompTrion、Capacity已重算
  2. 保存游戏
  3. 读档
  4. 检查殖民者A的Capacity和天赋
- **验证标准**：
  - Capacity值不变
  - 天赋值不变
  - Strategy重新初始化成功
  - CompTrion继续可用

---

## 实现优先级

### MVP第一阶段（核心框架验证）
1. ✓ 植入物→CompTrion初始化
2. ✓ 检测仪→天赋初始化
3. ✓ 触发器+组件基础装备
4. ✓ 战斗体生成与快照
5. ✓ 基础消耗计算
6. ✓ Bail Out机制

### MVP第二阶段（完整体验）
7. 伤口泄漏精细计算
8. 多组件激活管理
9. 心理状态Debuff
10. 高级Strategy实现

### 后续迭代
- 多Strategy支持（不同种族）
- 组件升级系统
- 天赋突变系统
- 完整UI界面

---

## 历史记录

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|---------|---------|--------|
| v0.1 | 初始方案，包含架构5层、10个测试场景 | 2026-01-10 | 需求架构师 |

