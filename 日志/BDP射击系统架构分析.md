---
标题：BDP射击系统架构分析
版本号: v1.0
更新日期: 2026-03-13
最后修改者: Claude Sonnet 4.6
标签：#架构分析 #射击系统 #BDP #技术文档
摘要: 深入分析 BDP 模组的射击系统架构,包括整体设计、发射路径、管线系统、配置机制等核心组件
---

# BDP 射击系统架构分析

## 1. 架构概览

### 1.1 核心设计理念

BDP 射击系统采用**管线化架构**（Pipeline Architecture），将射击过程分解为多个独立的阶段和模块，实现了高度的模块化和可扩展性。

**核心特点：**
- **阶段分离**：瞄准（Aim）和射击（Fire）两个独立阶段
- **模块化**：每个功能封装为独立模块，支持插拔式扩展
- **配置驱动**：通过 XML 配置控制射击行为，无需修改代码
- **多模式支持**：统一架构支持单侧/双侧/组合技等多种攻击模式

### 1.2 系统分层

```
┌─────────────────────────────────────────────────────────┐
│                    用户交互层                              │
│  Command_BDPChipAttack (Gizmo) → 用户点击攻击按钮         │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                    Verb 调度层                            │
│  Verb_BDPSingle / Verb_BDPDual / Verb_BDPCombo          │
│  - TryStartCastOn: 开始施法                               │
│  - TryCastShot: 射击入口（引擎回调）                       │
│  - ExecuteFire: 子类实现的射击逻辑                         │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                    管线系统层                              │
│  ShotPipeline: 管线编排器                                 │
│  - ExecuteAim: 瞄准阶段管线                               │
│  - ExecuteFire: 射击阶段管线                              │
│  ShotSession: 会话状态管理                                │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                    模块执行层                              │
│  Aim 模块: LosCheckModule, AnchorAimModule, ...         │
│  Fire 模块: VolleySpreadModule, FlightDataModule, ...   │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                    弹道发射层                              │
│  LaunchProjectile / TryCastShotCore                      │
│  - 命中判定、掩体计算、弹道生成                             │
│  - 枪口位置计算、投射物生成                                │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│                    投射物层                                │
│  Bullet_BDP: 自定义投射物                                 │
│  - 引导飞行、爆炸效果、穿透逻辑                             │
└─────────────────────────────────────────────────────────┘
```

### 1.3 核心类关系

**继承链：**
```
Verb (RimWorld)
  └─ Verb_LaunchProjectile
      └─ Verb_Shoot
          └─ Verb_BDPRangedBase (BDP 基类)
              ├─ Verb_BDPSingle (单侧攻击)
              ├─ Verb_BDPDual (双侧攻击)
              ├─ Verb_BDPCombo (组合技)
              ├─ Verb_BDPMelee (近战)
              └─ Verb_BDPProxy (自动攻击代理)
```

**核心类职责：**

1. **Verb_BDPRangedBase**（抽象基类）
   - 管线系统集成（ShotPipeline）
   - 会话管理（ShotSession）
   - 引导弹支持（锚点瞄准）
   - 自动绕行路径计算
   - 枪口位置计算
   - 齐射循环发射（FireVolleyLoop）

2. **Verb_BDPSingle**（单侧攻击）
   - 单手芯片攻击
   - 逐发/齐射模式切换
   - Trion 消耗管理

3. **Verb_BDPDual**（双侧攻击）
   - 双手芯片同时攻击
   - 左右侧独立发射模式
   - 双侧交替调度
   - 引导路径分侧管理

4. **Verb_BDPCombo**（组合技）
   - 特定芯片组合触发
   - 参数平均值计算
   - FireMode 倍率应用

5. **CompTriggerBody**（触发体组件）
   - Verb 实例创建和缓存
   - 芯片槽位管理
   - VerbProperties 合成
   - FiringPattern 注入

## 2. 发射路径分析

### 2.1 发射模式分类

BDP 支持两种基本发射模式，通过 `FiringPattern` 枚举区分：

#### 2.1.1 Sequential（逐发模式）
- **特点**：由引擎 burst 机制驱动，弹间有间隔
- **配置**：`burstShotCount > 1`, `ticksBetweenBurstShots > 0`
- **流程**：引擎每隔 N tick 调用一次 `TryCastShot()`
- **适用场景**：常规连射武器（如步枪、机枪）

#### 2.1.2 Simultaneous（齐射模式）
- **特点**：单次 `TryCastShot()` 内循环瞬发所有子弹
- **配置**：`burstShotCount = 1`, 实际发射数由 `VerbChipConfig.GetPrimaryBurstCount()` 决定
- **流程**：一次性发射所有子弹，无间隔
- **适用场景**：霰弹枪、齐射武器


### 2.2 完整调用链示例

#### 2.2.1 单侧逐发攻击流程

```
用户点击 Gizmo → Command_BDPChipAttack.ProcessInput()
    ↓
verb.BeginTargetingSession() - 创建 ShotSession
    ↓
Find.Targeter.BeginTargeting(verb) - 进入瞄准模式
    ↓
用户选择目标
    ↓
verb.OrderForceTarget(target)
    ↓
verb.TryStartCastOn(target) - 开始施法
    ├─ TryPrepareAutoRouteForCast() - 自动绕行
    └─ 锁定 currentTarget 为 Cell
    ↓
引擎创建 Job (BDP_ChipRangedAttack)
    ↓
引擎调用 verb.TryCastShot() - 第 1 发
    ├─ ExecuteFire(session)
    │   └─ DoSequentialShot()
    │       ├─ Trion 检查
    │       ├─ TryCastShotCore(chipThing)
    │       │   ├─ GetLosCheckTarget()
    │       │   ├─ GetMuzzlePosition()
    │       │   ├─ GenSpawn.Spawn(projectileDef)
    │       │   ├─ ShotReport.HitReportFor()
    │       │   └─ proj.Launch()
    │       └─ 消耗 Trion
    ↓
等待 ticksBetweenBurstShots ticks
    ↓
引擎调用 verb.TryCastShot() - 第 2 发
    └─ ExecuteFire(session)
        └─ DoSequentialShot()
            └─ TryCastShotCore(chipThing)
```

## 3. 配置系统

### 3.1 VerbChipConfig（芯片配置）

**核心字段：**

```csharp
public class VerbChipConfig : DefModExtension
{
    // 核心 Verb 配置
    public VerbProperties primaryVerbProps;           // 主攻击配置
    public FiringPattern primaryFiringPattern;        // 主攻击发射模式
    public VerbProperties secondaryVerbProps;         // 副攻击配置
    public FiringPattern secondaryFiringPattern;      // 副攻击发射模式

    // 功能域配置
    public RangedConfig ranged;                       // 远程配置
    public AreaIndicatorConfig areaIndicator;         // 范围指示器
}
```

**XML 配置示例：**

```xml
<ThingDef ParentName="BDP_ChipBase">
  <defName>BDP_Chip_Rifle</defName>
  <modExtensions>
    <li Class="BDP.Trigger.VerbChipConfig">
      <primaryVerbProps>
        <verbClass>BDP.Trigger.Verb_BDPSingle</verbClass>
        <range>30</range>
        <burstShotCount>3</burstShotCount>
        <ticksBetweenBurstShots>8</ticksBetweenBurstShots>
        <defaultProjectile>BDP_Bullet_Rifle</defaultProjectile>
      </primaryVerbProps>
      <primaryFiringPattern>Sequential</primaryFiringPattern>

      <ranged>
        <volleySpreadRadius>0.5</volleySpreadRadius>
      </ranged>
    </li>
  </modExtensions>
</ThingDef>
```

## 4. 关键发现

### 4.1 架构设计模式

1. **管线模式（Pipeline Pattern）**
   - 将复杂流程分解为多个独立阶段
   - 每个阶段由多个模块组成
   - 意图合并模式：各模块产出 Intent，管线合并为 Result

2. **策略模式（Strategy Pattern）**
   - FiringPattern 枚举定义发射策略
   - 运行时根据配置选择不同的发射逻辑

3. **模板方法模式（Template Method Pattern）**
   - Verb_BDPRangedBase 定义射击流程骨架
   - 子类重写 ExecuteFire() 实现具体逻辑

4. **会话模式（Session Pattern）**
   - ShotSession 封装单次射击的完整生命周期
   - 跨阶段传递数据

5. **配置驱动（Configuration-Driven）**
   - XML 配置控制射击行为
   - 无需修改代码即可调整参数

### 4.2 关键技术决策

1. **芯片 Verb 不进入 VerbTracker.AllVerbs**
   - 原因：避免被引擎近战选择池错误拾取
   - 实现：手动创建 Verb 实例，缓存到 CompTriggerBody

2. **FiringPattern 从类级区分改为配置级属性**
   - 原因：避免类爆炸
   - 影响：简化继承层次，提高可维护性

3. **双侧 LOS 降级逻辑**
   - 原因：无 LOS 且不支持引导的侧别无法射击
   - 实现：InitDualBurst() 中检查每一侧的 LOS

4. **枪口位置计算**
   - 原因：双枪芯片需要从不同位置发射
   - 实现：GetMuzzlePosition() 使用四元数旋转计算世界坐标

5. **管线系统延迟初始化**
   - 原因：避免在 Verb 创建时执行耗时操作
   - 实现：首次使用时调用 InitShotPipeline()

### 4.3 性能优化

1. **CompTriggerBody 缓存** - 避免重复 TryGetComp 查找
2. **投射物定义缓存** - InitDualBurst() 中缓存
3. **管线配置复用** - ShotPipeline.Build() 只调用一次
4. **会话状态复用** - activeSession 在 burst 期间持久化

### 4.4 扩展性设计

1. **模块插拔式扩展** - 实现 IShotAimModule / IShotFireModule
2. **XML 配置注入** - VerbChipConfig.aimModules / fireModules
3. **自定义 Verb 类型** - 继承 Verb_BDPRangedBase
4. **组合技系统** - ComboVerbDef 定义组合规则

## 5. 代码路径对比

### 5.1 逐发 vs 齐射

| 特性 | Sequential（逐发） | Simultaneous（齐射） |
|------|-------------------|---------------------|
| 引擎调用次数 | N 次 | 1 次 |
| burstShotCount | > 1 | = 1 |
| 发射方法 | TryCastShotCore() | FireVolleyLoop() |
| Trion 消耗时机 | 每发消耗 | 一次性消耗 |
| 散布方式 | 无 | 局部坐标系散布 |

### 5.2 单侧 vs 双侧

| 特性 | Verb_BDPSingle | Verb_BDPDual |
|------|----------------|--------------|
| 芯片数量 | 1 | 2 |
| chipSide | 有值 | null |
| 发射模式 | 单一 FiringPattern | 左右独立 FiringPattern |
| 调度逻辑 | 无 | GetCurrentShotSide() 交替 |
| 引导路径 | 单一路径 | 分侧管理 |

---

## 历史修改记录
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-13 | 创建 BDP 射击系统架构分析文档 | Claude Sonnet 4.6 |
