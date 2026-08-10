# CombatBody 快照/回滚系统旧版事实到新版实现点映射表

**日期：** 2026-04-06

## 1. 文档目的

这份文档不是设计稿，也不是计划稿。

它只做一件事：

> 把旧 BDP 已经确认的真实事实，一条条翻译成“新 BDP 必须实现什么”，并标记这些点在当前实施计划里是否已经被正式覆盖。

这样后面讨论“能不能和旧版表现一致”，就不再靠感觉。

---

## 2. 快照/回滚主流程事实映射

## 2.1 激活主流程

### 旧版事实 1

- 旧版激活主流程顺序是：
  - 前置检查
  - Trion 占用
  - 状态转换
  - 激活芯片
  - 注册维持消耗
  - 进入 Active

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/CombatBodyOrchestrator.cs`

**新版必须实现点：**
- 新版仍应让快照/回滚只承接“状态转换里的宿主变换”
- 不应把 Trion / Trigger / Cooldown / Collapse 重新吃回快照系统

**当前计划覆盖情况：**
- 已覆盖

---

### 旧版事实 2

- 旧版真正和快照/回滚直接相关的激活内部顺序是：
  - `SnapshotHediffs`
  - `SnapshotNeeds`
  - 收原始衣物
  - 收原始背包
  - 建立战斗体前台层
  - 清非排除 `Hediff`
  - 添加 `BDP_CombatBodyActive`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/CombatBodyOrchestrator.cs`
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版必须显式保留这条激活时序
- 不能只写成“激活时做快照”

**当前计划覆盖情况：**
- 已覆盖
- 严格等价红线已补充

---

## 2.2 退出主流程

### 旧版事实 3

- 旧版退出主流程关键顺序是：
  - 紧急退出附带处理（如果有）
  - `ExtinguishFire`
  - 清战斗期非排除 `Hediff`
  - 单独移除 `BDP_CombatBodyActive`
  - 解除 Trigger
  - 注销维持消耗
  - 恢复快照
  - 最终残留 `Hediff` 清理
  - 紧急退出额外惩罚（如果有）
  - 转入 `Cooldown`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/CombatBodyOrchestrator.cs`

**边界说明：**
- 这是一条“旧版总退出流程事实”，不是“本子系统独占负责的完整流程”。
- 其中真正属于快照/回滚子系统或宿主前台变换子系统的，只是这几个子段：
  - `ExtinguishFire`
  - 清战斗期非排除 `Hediff`
  - 单独移除 `BDP_CombatBodyActive`
  - 恢复快照
  - 最终残留 `Hediff` 清理
- 明确不属于本子系统的子段：
  - 紧急退出附带处理
  - `ReleaseTriggerSystem`
  - `UnregisterMaintenance`
  - 紧急退出额外惩罚
  - 转入 `Cooldown`

**新版必须实现点：**
- 对快照/回滚子系统来说，真正要吸收的不是整条总退出流程，而只是“恢复快照”这一个子段。
- 如果外部宿主流程仍需要：
  - 灭火
  - 单独移除运行标记
  - 最终残留清理
  这些应放在外部流程里，而不是写进快照/回滚子系统本体。

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的，不是最初就完整覆盖

---

### 旧版事实 4

- 旧版 `RestoreAll()` 的真实顺序是：
  - 恢复衣物
  - 恢复背包
  - 恢复 Need
  - 恢复 Hediff

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版恢复顺序必须写死，不允许抽象成“恢复原始基线”这种模糊表述

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的

---

## 3. 原始衣物 / 原始背包事实映射

### 旧版事实 5

- 旧版原始衣物托管时记录：
  - `wasLocked`
  - `wasForced`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/ApparelState.cs`
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版恢复衣物时也必须恢复这两个标记

**当前计划覆盖情况：**
- 已覆盖

---

### 旧版事实 6

- 旧版原始背包托管时记录：
  - `wasNotForSale`
  - `wasUnpackedCaravan`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/ItemState.cs`
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版恢复背包时也必须恢复这两个标记

**当前计划覆盖情况：**
- 已覆盖

---

## 4. Hediff / Need 事实映射

### 旧版事实 7

- 旧版 `HediffRecord` 显式记录 13 个字段：
  - `defName`
  - `severity`
  - `bodyPartDefName`
  - `bodyPartIndex`
  - `ageTicks`
  - `level`
  - `isPermanent`
  - `painCategory`
  - `sourceLabel`
  - `sourceDefName`
  - `sourceToolLabel`
  - `isFresh`
  - `lastInjuryDefName`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版只允许按这 13 项迁移
- 不允许擅自新增“看起来应该有”的字段

**当前计划覆盖情况：**
- 已覆盖

---

### 旧版事实 8

- 旧版 Need 快照只记录：
  - `NeedDef -> CurLevel`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版 Need 恢复只能做到这个粒度
- 不要擅自扩成更大的生活系统镜像

**当前计划覆盖情况：**
- 已覆盖

---

## 5. Hediff 排除规则事实映射

### 旧版事实 9

- 旧版排除规则不是硬编码列表
- 它是 `Def` 驱动的动态配置系统

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/BDPSnapshotConfigDef.cs`

**旧版支持的维度：**
- `excludedHediffs`
- `excludedHediffClasses`

**旧版匹配方式：**
- 具体 `HediffDef` 精确匹配
- 类名通过 `IsAssignableFrom` 覆盖所有子类
- 遍历 `DefDatabase<BDPSnapshotConfigDef>.AllDefs` 合并全部配置

**新版必须实现点：**
- 新版也必须是动态配置
- 不能退化成 C# 写死的一张默认表

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的

---

### 旧版事实 10

- 旧版默认配置值是：
  - `PsychicAmplifier`
  - `MechlinkImplant`
  - `BDP_CombatBodyActive`
  - `Verse.Hediff_Psylink`
  - `Verse.Hediff_Mechlink`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/1.6/Defs/Combat/BDPSnapshotConfig.xml`

**新版必须实现点：**
- 新版默认配置必须迁同一组值
- 但只作为默认 Def，不是代码常量

**当前计划覆盖情况：**
- 已覆盖

---

### 旧版事实 10.5

- `BDP_CombatBodyActive` 不只是快照排除项
- 它还被旧版其他模块直接依赖，至少包括：
  - `CombatBodyQuery.IsCombatBodyActive()`
  - 伤害后 `Trion` 消耗链
  - 健康卡 UI 图标切换
  - 自身 `HediffDef` 上的 `preventsDeath` / stage 效果 / wound drain comp

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/1.6/Defs/Combat/HediffDefs_CombatBody.xml`
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Utils/CombatBodyQuery.cs`
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Comps/HediffComp_TrionDamageCost.cs`
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Patches/Patch_Pawn_PostApplyDamage.cs`
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Patches/Patch_HealthCardUtility_DrawHediffRow.cs`

**新版必须实现点：**
- 这条事实的作用是提醒：`BDP_CombatBodyActive` 不是快照/回滚子系统内部的责任对象。
- 快照子系统至多只需要知道：
  - 它不应被当成“原始基线”记录/恢复
- 至于它是否继续存在、由谁挂上、由谁移除，属于战斗体运行态和外部流程设计，不属于快照/回滚子系统本体。

**当前计划覆盖情况：**
- 已补充为严格等价红线

---

## 6. 前台装备 / 战斗体装备事实映射

这一块就是你刚才抓出来的重点，也是我前面理解不够细的地方。

## 6.1 预设模式 `Preset`

### 旧版事实 11

- 旧版预设模式的前台装备来源，不是运行时拼出来的
- 它是一件正式 `ThingDef`：`BDP_CombatBodyArmor`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/1.6/Defs/Combat/ThingDefs_CombatApparel.xml`

**新版必须实现点：**
- 新版不能只保留“前台预设概念”
- 必须把这件装备本体也正式迁过去

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的

---

### 旧版事实 12

- 这件预设装甲显式声明了这些字段：
  - `ParentName=ApparelNoQualityBase`
  - `graphicData.texPath`
  - `graphicClass=Graphic_Single`
  - `MaxHitPoints=99999`
  - `Mass=0`
  - `Flammability=0`
  - `DeteriorationRate=0`
  - 三种护甲值
  - 冷热保温
  - `EquipDelay=0`
  - `MoveSpeed +0.20`
  - `generateCommonality=0`
  - `useHitPoints=false`
  - `smeltable=false`
  - `bodyPartGroups`
  - `wornGraphicPath`
  - `layers`
  - `tags`
  - `CompProperties_Forbiddable`
  - `tradeability=None`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/1.6/Defs/Combat/ThingDefs_CombatApparel.xml`

**新版必须实现点：**
- 新版预设装甲本体 Def 至少要迁移这组显式字段
- 不然“看起来还是有个前台装甲”，但细节表现已经和旧版不是一回事

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的

---

### 旧版事实 13

- 这件预设装甲通过 `ParentName=ApparelNoQualityBase` 进入“无品质”体系
- 我已确认这个父类里有：
  - `CompColorable`
- 但在这次已核代码范围里，我**没有看到**它为这件装备单独声明 `Stuff` 配置

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/1.6/Defs/Combat/ThingDefs_CombatApparel.xml`
- `ApparelNoQualityBase` 解析结果

**新版必须实现点：**
- 新版预设装甲应继续走“无品质”路径
- `CompColorable` 也应继续存在
- 对“预设装甲是否有专门材质配置”这件事，当前证据下不能乱补

**当前计划覆盖情况：**
- 已覆盖“无品质”和 `CompColorable`
- 未新增任何无依据的材质系统

---

## 6.2 镜像模式 `MirrorOriginal`

### 旧版事实 14

- 旧版镜像模式会用：
  - `original.def`
  - `original.Stuff`
 重新 `MakeThing`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版镜像模式必须继续复制 `Stuff`

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的

---

### 旧版事实 15

- 旧版镜像模式会复制：
  - `CompColorable` 的当前颜色
  - `StyleDef`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版镜像模式必须继续复制颜色与风格

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的

---

### 旧版事实 16

- 旧版镜像模式会显式去掉 `CompQuality`
- 也会把 `copy.compQuality = null`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版镜像模式也必须保证前台副本无品质

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的

---

### 旧版事实 17

- 旧版镜像模式代码里没有把原件当前耐久值拷给副本
- 注释还明确写了“耐久满”

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版镜像模式不应擅自复制原件当前耐久

**当前计划覆盖情况：**
- 已覆盖
- 是后补进去的

---

## 7. 托管防腐事实映射

### 旧版事实 18

- 旧版防腐补丁触发条件非常窄：
  - `__instance.parent?.holdingOwner?.Owner is CombatBodySnapshot`

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/Patch_CompRottable.cs`

**新版必须实现点：**
- 新版也只应对“快照托管中的东西”生效
- 不能扩成通用仓储防腐系统

**当前计划覆盖情况：**
- 已覆盖

---

## 8. 存档事实映射

### 旧版事实 19

- 旧版快照会序列化：
  - 原始衣物容器
  - 原始背包容器
  - 战斗体衣物容器
  - 衣物状态字典
  - 物品状态字典
  - `hediffSnapshots`
  - `needValues`
  - `pawn` 引用

**代码依据：**
- `模组工程/BorderDefenseProtocol.Legacy/Source/BDP/Combat/Snapshot/CombatBodySnapshot.cs`

**新版必须实现点：**
- 新版至少要把：
  - 原始快照状态
  - 前台状态
跟宿主位一起存档

**当前计划覆盖情况：**
- 已覆盖

---

## 9. 当前校正后的结论

## 9.1 我前面的错误在哪里

错误不在“架构方向判断”，而在：

- 过早把“方向可行”
- 说成了“计划已经足够保证等价”

现在回看，旧版里确实有一批细节，当时还没全收进计划。

## 9.2 现在的状态

现在这份映射表成立后，可以把结论改成：

```text
原始方向没错，
但最初计划不够细。

经过这轮补全后，
计划已经开始从“方向性方案”
收口成“旧版事实对齐方案”。
```

## 9.3 还能不能继续宣称“实施后与旧版表现一致”

现在更准确的说法应当是：

- 不是“因为我觉得差不多，所以会一致”
- 而是“只有当这份映射表里的旧版事实都落实后，才有资格说会一致”

这才是负责任的口径。
