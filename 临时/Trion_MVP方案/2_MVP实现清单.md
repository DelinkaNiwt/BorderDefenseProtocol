---
摘要: Trion_MVP代码工程师实现清单，包含所有C#类和XML Def的详细需求
版本号: v0.1
修改时间: 2026-01-10
关键词: 实现清单,代码需求,类定义,Def定义,优先级
标签: [待审]
---

# Trion_MVP 实现清单 v0.1

## 概述

本清单详细列出MVP阶段需要实现的所有代码和配置。总共 **15个C#类** + **8个XML Def文件**。

---

## Part 1: 核心业务逻辑类（C#）

### 必需类 - 第一阶段（关键路径）

#### 1.1 TrionTalentManager.cs
**目标**：统一管理Pawn的天赋存取

```csharp
需求：
- 方法：GetTalent(Pawn) → TalentGrade?
- 方法：SetTalent(Pawn, TalentGrade) → void
- 常量：string TalentKey = "Trion_Talent"
- 存储位置：Pawn.modData
- 错误处理：null检查，序列化异常捕获

验收标准：
✓ 设置后能正确读取
✓ 多次读取返回一致结果
✓ 读档前后数据不变
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 1.2 DefaultTrionStrategy.cs
**目标**：ILifecycleStrategy的默认实现，用于MVP测试

```csharp
需求：
- 实现ILifecycleStrategy接口所有方法
- GetInitialTalent(CompTrion) 实现逻辑：
  * 获取Strategy参数的Pawn
  * 检查是否已有天赋（TrionTalentManager.GetTalent）
  * 如已有则返回该天赋
  * 如无则生成随机天赋并存储
  * 返回TalentGrade
- GetBaseMaintenance() 返回 5f（简单值）
- OnCombatBodyGenerated/Destroyed: Log记录即可
- CanBailOut(CompTrion): 返回true（简单实现）
- GetBailOutTarget(CompTrion): 返回 Pawn.Position（原地脱离）
- 其他回调: 空实现或Log

验收标准：
✓ 首次调用GetInitialTalent后天赋被保存
✓ 后续调用返回相同天赋
✓ Strategy可被正常序列化和反序列化
✓ 所有回调无异常
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 1.3 Hediff_TrionGland.cs
**目标**：Trion腺体植入物，负责CompTrion的初始化

```csharp
需求：
- 继承：HediffWithComps
- PostAdd(DamageInfo?) 重写：
  * 调用base.PostAdd()
  * 获取parent Pawn
  * 检查是否已有CompTrion（pawn.GetComp<CompTrion>()）
  * 如无则创建并添加CompTrion
- AddTrionCompToPawn(Pawn) 方法：
  * 创建CompProperties_Trion实例，设置：
    - compClass = typeof(CompTrion)
    - strategyClassName = "TrionMVP.DefaultTrionStrategy"
    - capacity = 1400f
    - enableSnapshot = true
    - enableBailOut = true
  * 创建CompTrion实例
  * 设置props和parent
  * 添加到pawn.AllComps
  * 调用comp.PostSpawnSetup(false)
  * Log记录

验收标准：
✓ 植入后CompTrion在Pawn上
✓ Strategy已初始化
✓ Capacity为1400
✓ 日志可见"获得Trion能量系统"
✓ 重复植入不会添加多个CompTrion
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 1.4 Building_TrionDetector.cs
**目标**：Trion天赋检测仪，负责天赋初始化

```csharp
需求：
- 继承：Building
- 方法：ScanPawn(Pawn pawn)
  * 检查pawn是否有CompTrion
  * 如无则提示"无Trion腺体"并返回
  * 调用TrionTalentManager.GetTalent(pawn)
  * 如天赋为null：
    - 调用GenerateRandomTalent(pawn)
    - 调用TrionTalentManager.SetTalent()
    - 调用CompTrion.RecalculateCapacityFromTalent()
    - Log记录"首次扫描"
  * 调用ShowDetectionResult(pawn, talent)显示结果

- 方法：GenerateRandomTalent(Pawn) → TalentGrade
  * 从[S, A, B, C, D, E]中随机选一个
  * 可选：加权随机（高等级概率更低）

- 方法：ShowDetectionResult(Pawn, TalentGrade)
  * 创建FloatMenu或Dialog显示扫描结果
  * 显示内容：名字、天赋等级、Capacity新值

验收标准：
✓ 首次扫描生成天赋
✓ 天赋保存到modData
✓ Capacity更新为正确值
✓ 再次扫描显示相同结果
✓ UI交互自然
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 1.5 TrionComponent.cs
**目标**：Trion组件物品基类，定义组件属性

```csharp
需求：
- 继承：Thing
- 字段：
  * string componentName - 组件名称
  * int trionCost - Trion消耗值
  * string componentType - 类型（Melee/Ranged/Auxiliary/Special）
  * string componentEffect - 效果描述
  * float rarity - 稀有度
  * int slotSize = 1 - 占用槽位数（默认1）
  * bool canCoexistWith - 是否能与其他同类共存
- 虚方法：GetDescription() → string
  * 返回"组件名称\n消耗:X\n效果:..."

验收标准：
✓ 组件能被正常生成和摆放
✓ 可被捡起和装备到触发器
✓ 序列化正常（能被存档）
✓ 检查栏显示详细信息
```

**优先级**：⭐⭐⭐⭐☆ 第一阶段

---

### 可选类 - 第二阶段（增强体验）

#### 1.6 Building_TriggerConfigBench.cs
**目标**：触发器配置台，负责组件装备

```csharp
需求：
- 继承：Building
- 字段：Trigger selectedTrigger - 当前配置的触发器
- 方法：GetGizmos() → IEnumerable<Gizmo>
  * 添加"选择触发器"按钮 → 显示地图上所有触发器列表
  * 添加"选择组件"按钮 → 显示库存中所有组件列表
  * 添加"装备组件"按钮 → 执行装备逻辑
- 方法：EquipComponent(TrionComponent comp, Trigger trigger)
  * 检查trigger是否在战斗体激活状态
  * 检查槽位是否足够
  * 调用trigger.AddComponent(comp)
  * 更新Reserved值
  * 日志记录

验收标准：
✓ 能选择触发器
✓ 能选择组件
✓ 装备后组件列表更新
✓ 槽位限制有效
✓ 战斗体激活时提示不能修改
```

**优先级**：⭐⭐⭐⭐☆ 第一阶段

---

#### 1.7 UI_CombatBodyStatus.cs
**目标**：战斗体激活/解除按钮UI

```csharp
需求：
- 继承：ITab或Gizmo
- 按钮：激活战斗体
  * 检查CompTrion是否存在
  * 调用CompTrion.GenerateCombatBody()
  * 更新UI状态
- 按钮：解除战斗体
  * 调用CompTrion.DestroyCombatBody(DestroyReason.Manual)
  * 更新UI状态
- 显示信息：
  * Capacity / Reserved / Consumed / Available
  * 战斗体状态（激活/未激活）
  * 当前装备组件列表

验收标准：
✓ 按钮能激活战斗体
✓ 激活后能看到Capacity更新
✓ 按钮能解除战斗体
✓ 数据显示正确实时更新
```

**优先级**：⭐⭐⭐☆☆ 第二阶段

---

### 框架适配类（需修改ProjectTrion_Framework）

#### 1.8 CompTrion - 新增方法
**在ProjectTrion_Framework/CompTrion.cs中添加**

```csharp
需求：
- 新增公开方法：RecalculateCapacityFromTalent(TalentGrade talent)
  * 逻辑同RecalculateCapacity()
  * 目的：供MVP应用层调用
  * 可直接重命名现有private RecalculateCapacity()为public

- 新增公开方法：GetInspectInfo() → string
  * 返回 CompInspectStringExtra()
  * 目的：方便UI获取格式化信息

验收标准：
✓ 方法可被应用层调用
✓ Capacity正确重算
✓ UI信息完整
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 1.9 ILifecycleStrategy - 文档补充
**在ProjectTrion_Framework/ILifecycleStrategy.cs中补充**

```csharp
需求：
- 补充GetInitialTalent()的参数和返回说明：
  * 参数：CompTrion comp - 当前的Trion组件实例
  * 返回：TalentGrade?
    - 非null：框架会调用RecalculateCapacity()
    - null：框架跳过Capacity计算，应用层自管理
- 补充示例注释：
  * 首次调用时初始化天赋
  * 后续调用时返回已有天赋
  * 可以从comp.parent(Pawn)读取数据做决策

验收标准：
✓ 文档清晰
✓ 开发者能理解预期行为
```

**优先级**：⭐⭐⭐⭐☆ 第一阶段

---

## Part 2: XML Def文件

### 必需Def - 第一阶段

#### 2.1 HediffDefs_TrionGland.xml
**位置**：Defs/HediffDefs/

```xml
需求：
- 定义HediffDef "Trion_Gland_Implant"
  * label: "Trion腺体植入"
  * description: "获得Trion能量系统，可使用Trion触发器"
  * hediffClass: TrionMVP.Hediff_TrionGland
  * defaultLabelColor: (0.2, 0.8, 0.5) [青绿色]
  * isBad: false [是福利]
  * comps: HediffCompProperties_SeverityPerDay (severityPerDay=0)
  * stages: (可选) 显示状态描述

验收标准：
✓ 植入手术后能添加此hediff
✓ hediff显示为蓝色标签（福利）
✓ 不会自动消失（severity=0）
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 2.2 ThingDefs_TrionDetector.xml
**位置**：Defs/ThingDefs_Buildings/

```xml
需求：
- 定义ThingDef "Trion_Detector"
  * label: "Trion天赋检测仪"
  * description: "扫描小人的Trion天赋等级。首次扫描时随机生成天赋。"
  * thingClass: TrionMVP.Building_TrionDetector
  * category: Building
  * thingDefCategory: BuildingBase（可选）
  * size: (2, 2)
  * graphicData:
    - texPath: Things/Building/TrionDetector_top（或占位贴图）
    - graphicClass: Graphic_Single
    - drawSize: (2, 2)
  * costList: Steel 100, ComponentIndustrial 5
  * statBases: MaxHitPoints 100, Flammability 0.5
  * placingDraggableDimensions: 2
  * castEdgeShadows: false

验收标准：
✓ 建筑能被建造
✓ 占位贴图显示正常
✓ 交互菜单可见
✓ 成本合理
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 2.3 ThingDefs_TrionGlandItem.xml
**位置**：Defs/ThingDefs_Items/

```xml
需求：
- 定义ThingDef "Item_TrionGland_Raw"
  * label: "Trion腺体（未精炼）"
  * description: "经过处理的Trion腺体，可用于植入手术"
  * thingClass: Thing
  * category: Item
  * stackLimit: 5
  * graphicData:
    - texPath: Things/Item/TrionGland
    - graphicClass: Graphic_StackCount
  * statBases: Mass 0.5, MarketValue 200
  * intricate: false
  * thingCategories: Medicine, BodyParts（可选）

验收标准：
✓ 物品能被生成
✓ 能被选作植入物进行手术
✓ 堆栈正常
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 2.4 ThingDefs_TrionComponents.xml
**位置**：Defs/ThingDefs_Items/

```xml
需求：
- 定义多个组件物品（至少5个作为MVP演示）：

组件1：弧月（近战武器）
- defName: TrionComponent_ArcusBlade
- label: 弧月
- description: Trion驱动近战刀具，消耗10/激活
- thingClass: TrionMVP.TrionComponent
- cost: Steel 50, ComponentIndustrial 2
- MarketValue: 500
- Mass: 2.5

组件2：护盾生成器（辅助）
- defName: TrionComponent_ShieldGen
- label: 护盾生成器
- description: Trion驱动护盾，消耗10-20/激活
- cost: Steel 60, ComponentIndustrial 3
- MarketValue: 600
- Mass: 1.5

组件3：炸裂弹（远程武器）
- defName: TrionComponent_ExplosiveBullet
- label: 炸裂弹
- description: Trion驱动远程武器，消耗10/激活
- cost: Steel 40, ComponentIndustrial 2
- MarketValue: 400
- Mass: 0.5

组件4：变色龙隐身（辅助）
- defName: TrionComponent_Chameleon
- label: 变色龙隐身
- description: Trion驱动隐身装置，消耗10/激活+1/维持
- cost: Steel 80, ComponentIndustrial 4
- MarketValue: 800
- Mass: 2.0

组件5：紧急脱离系统（特殊）
- defName: TrionComponent_BailOut
- label: 紧急脱离系统
- description: Trion驱动紧急脱离装置，消耗400/激活
- cost: Steel 150, ComponentIndustrial 8, Gold 10
- MarketValue: 2000
- Mass: 5.0

验收标准：
✓ 所有组件能生成
✓ 能从库存中看到
✓ 成本合理（稀有度越高越贵）
✓ 占位贴图显示正常
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

#### 2.5 SurgeryDefs_TrionImplant.xml
**位置**：Defs/SurgeryDefs/

```xml
需求：
- 定义SurgeryDef "Surgery_ImplantTrionGland"
  * label: "植入Trion腺体"
  * description: "在患者体内植入Trion腺体，使其获得Trion能量系统"
  * surgerySuccessfullyRemovedHediffMessage: "植入成功"
  * addHediffOnSuccess: "Trion_Gland_Implant"
  * ingredients: [Item_TrionGland_Raw]
  * requiredMedicalCare: Normal
  * minAlienRace: false
  * successfullyRemovedHediffMessage: "Trion腺体已植入"
  * bodyPartGroups: 可指定位置（如胸部）

验收标准：
✓ 手术可执行
✓ 成功后Hediff自动添加
✓ CompTrion自动初始化
```

**优先级**：⭐⭐⭐⭐⭐ 必需

---

### 可选Def - 第二阶段

#### 2.6 ThingDefs_TriggerConfigBench.xml
**位置**：Defs/ThingDefs_Buildings/

```xml
需求：
- 定义ThingDef "Trion_TriggerConfigBench"
  * label: "触发器配置台"
  * description: "配置触发器，装备和移除组件"
  * thingClass: TrionMVP.Building_TriggerConfigBench
  * category: Building
  * size: (3, 3)
  * graphicData: 占位贴图
  * costList: Steel 200, ComponentIndustrial 10
  * statBases: MaxHitPoints 200, Flammability 0.2

验收标准：
✓ 建筑能建造
✓ UI交互正常
```

**优先级**：⭐⭐⭐☆☆ 第二阶段

---

#### 2.7 ResearchDefs_Trion.xml
**位置**：Defs/ResearchDefs/

```xml
需求：
- 定义ResearchProjectDef "Trion_Technology"
  * label: "Trion能量系统"
  * description: "解锁Trion相关建筑和物品"
  * baseCost: 1000
  * techLevel: Industrial
  * prerequisites: (optional) 如Electricity等
  * unlocks:
    - Trion_Detector
    - Trion_TriggerConfigBench
    - Surgery_ImplantTrionGland

验收标准：
✓ 研究能进行
✓ 完成后相关物品/建筑解锁
```

**优先级**：⭐⭐⭐☆☆ 第二阶段

---

#### 2.8 Defs_TraderKinds.xml （可选）
**位置**：Defs/TraderKinds/

```xml
需求（可选）：
- 定义NPC商人可销售Trion物品
- 或定义起始物品列表

验收标准：
✓ MVP地图生成时有初始物品
```

**优先级**：⭐⭐☆☆☆ 可选

---

## Part 3: 项目文件配置

### 3.1 .csproj 配置
**需求**：
- 项目名称：TrionMVP
- 目标框架：.NET Framework 4.7.2
- 编译输出：TrionMVP.dll
- 引用：
  * ProjectTrion.dll (框架程序集)
  * RimWorld程序集 (来自游戏目录)
  * Verse.dll

**验收标准**：
✓ 编译成功，0错误
✓ DLL生成到 1.6/Assemblies/ 目录

---

### 3.2 LoadFolders.xml
**需求**：
```xml
<loadFolders>
  <v1.6>
    <li>/</li>
  </v1.6>
</loadFolders>
```

**验收标准**：
✓ Mod能被游戏正确加载

---

### 3.3 About.xml
**需求**：
```xml
<ModMetaData>
  <name>Trion Framework MVP</name>
  <author>需求架构师</author>
  <url>...</url>
  <description>Trion框架MVP测试模组，用于验证框架核心功能</description>
  <version>0.1.0</version>
  <gameVersion>1.6</gameVersion>
  <dependencies>
    <li>ludeon.rimworld</li>
  </dependencies>
</ModMetaData>
```

**验收标准**：
✓ Mod信息正确显示
✓ 依赖关系正确

---

## Part 4: 实现步骤和验收

### 步骤顺序（推荐）

**阶段1：核心框架适配（1天）**
1. ✓ CompTrion.RecalculateCapacityFromTalent() 公开
2. ✓ ILifecycleStrategy 文档补充

**阶段2：核心业务逻辑（3天）**
3. ✓ TrionTalentManager.cs
4. ✓ DefaultTrionStrategy.cs
5. ✓ Hediff_TrionGland.cs
6. ✓ Building_TrionDetector.cs
7. ✓ TrionComponent.cs (基础)
8. ✓ HediffDefs_TrionGland.xml
9. ✓ ThingDefs_TrionGlandItem.xml
10. ✓ SurgeryDefs_TrionImplant.xml
11. ✓ ThingDefs_TrionDetector.xml
12. ✓ ThingDefs_TrionComponents.xml

**阶段3：UI和增强（2天）**
13. Building_TriggerConfigBench.cs
14. UI_CombatBodyStatus.cs
15. ThingDefs_TriggerConfigBench.xml
16. ResearchDefs_Trion.xml

**阶段4：测试和调试（2天）**
17. 执行10个测试场景
18. 日志审查和优化
19. 性能基准测试

---

### 验收标准汇总

**编译**：
- [ ] 0编译错误
- [ ] 0运行时异常（关键路径）
- [ ] DLL正确生成

**功能**：
- [ ] 植入→天赋初始化完整流程可执行
- [ ] 10个测试场景全部通过
- [ ] 日志记录完整

**性能**：
- [ ] 战斗体消耗计算每60Tick耗时 <5ms
- [ ] 泄漏缓存有效降低性能开销

**存档**：
- [ ] 读档前后天赋和Capacity一致
- [ ] CompTrion能被正确序列化

---

## Part 5: 占位贴图需求

### 需要的图片资源

| Def | 用途 | 尺寸 | 格式 |
|------|------|------|------|
| Things/Building/TrionDetector_top | 检测仪 | 128×128 (2×2) | PNG |
| Things/Building/TriggerConfigBench | 配置台 | 192×192 (3×3) | PNG |
| Things/Item/TrionGland | 腺体物品 | 64×64 | PNG |
| Things/Item/TrionComponent_ArcusBlade | 弧月组件 | 64×64 | PNG |
| Things/Item/TrionComponent_ShieldGen | 护盾组件 | 64×64 | PNG |
| Things/Item/TrionComponent_ExplosiveBullet | 炸裂弹组件 | 64×64 | PNG |
| Things/Item/TrionComponent_Chameleon | 隐身组件 | 64×64 | PNG |
| Things/Item/TrionComponent_BailOut | 脱离组件 | 64×64 | PNG |

**来源**：C:\NiwtDatas\Projects\RimworldModStudio\参考资源\通用资源\占位贴图\

**说明**：如占位贴图不足，需反馈补充

---

## 历史记录

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|---------|---------|--------|
| v0.1 | 初始清单，15个C#类+8个Def | 2026-01-10 | 需求架构师 |

