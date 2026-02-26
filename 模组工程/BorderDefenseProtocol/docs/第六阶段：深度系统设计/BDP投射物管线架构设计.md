---
标题：BDP投射物管线架构设计
版本号: v1.0
更新日期: 2026-02-25
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 描述BDP投射物模块系统（PMS）从通知型hook升级为管线（Pipeline）架构的设计方案，包含管线阶段清单、数据包规范、执行顺序和扩展规则。
---

# BDP投射物管线架构设计

## 1. 背景

PMS v3采用5个通知型hook（OnSpawn/OnTick/OnPreImpact/OnImpact/OnPostImpact），模块只能"观察"或"拦截"，无法修改彼此的行为输出。当多个机制需要融合时（如引导+追踪），模块间没有协商/修改中间结果的机制。

## 2. 设计决策

| 决策 | 选择 | 原因 |
|------|------|------|
| 协作模式 | 管线（Pipeline） | 模块需要"修改行为"而非仅"读取状态" |
| 接口粒度 | 每阶段一个小接口（方案B） | 模块只实现关心的阶段，新增阶段不影响现有模块 |
| 数据包类型 | struct | 避免GC压力，大多数数据包<6字段 |
| 空管线优化 | SpawnSetup时建立参与者缓存列表 | 无参与者的阶段零开销 |
| 迁移策略 | 一步替换 | 不做兼容层，直接替换旧hook |
| Verb层 | 暂不管线化 | 本次只做Projectile层 |

## 3. 架构总览

```
IBDPProjectileModule (基础接口)
├── Priority + OnSpawn + ExposeData
│
├── IBDPPathResolver        (管线型) → PathContext
├── IBDPSpeedModifier       (管线型) → SpeedContext
├── IBDPInterceptModifier   (管线型) → InterceptContext
├── IBDPPositionModifier    (管线型) → PositionContext
├── IBDPTickObserver        (通知型) → 无数据包
├── IBDPArrivalHandler      (管线型) → ArrivalContext
└── IBDPImpactHandler       (管线型) → ImpactContext
```

## 4. 管线阶段清单

### 4.1 基础接口

`IBDPProjectileModule : IExposable`
- `int Priority { get; }` — 执行优先级（越小越先）
- `void OnSpawn(Bullet_BDP host)` — SpawnSetup时初始化
- `void ExposeData()` — 序列化

### 4.2 通知型接口

| 接口 | 方法 | 用途 |
|------|------|------|
| `IBDPTickObserver` | `OnTick(host)` | 只读观察，拖尾/视觉/音效 |

### 4.3 管线型接口

| 接口 | 方法 | 数据包 | 用途 |
|------|------|--------|------|
| `IBDPPathResolver` | `ResolvePath(host, ref ctx)` | `PathContext` | 修改destination（追踪） |
| `IBDPSpeedModifier` | `ModifySpeed(host, ref ctx)` | `SpeedContext` | 修改飞行速度 |
| `IBDPInterceptModifier` | `ModifyIntercept(host, ref ctx)` | `InterceptContext` | 修改拦截行为（穿透） |
| `IBDPPositionModifier` | `ModifyPosition(host, ref ctx)` | `PositionContext` | 修改显示坐标（抛物线） |
| `IBDPArrivalHandler` | `HandleArrival(host, ref ctx)` | `ArrivalContext` | 到达决策（继续飞/命中） |
| `IBDPImpactHandler` | `HandleImpact(host, ref ctx)` | `ImpactContext` | 命中效果（爆炸/分裂） |

## 5. 数据包规范

每个管线型接口有专属struct数据包，规则：
- 字段分"输入"（readonly）和"可修改"（读写）
- 值类型，用完即弃，不序列化
- 模块通过ref参数修改数据包

示例：
```csharp
public struct ArrivalContext
{
    public readonly Thing HitTarget;  // 输入
    public bool Continue;             // 可修改
}
```

## 6. 每tick执行顺序

```
1. IBDPPathResolver      → 决定"飞向哪"（修改destination）
2. IBDPSpeedModifier     → 决定"飞多快"（预留）
3. base.TickInterval     → 引擎位置计算 + 拦截检查 + 到达判定
4. IBDPInterceptModifier → 修饰拦截判定（预留）
5. IBDPPositionModifier  → 修饰显示位置（缓存到modifiedDrawPos）
6. IBDPTickObserver      → 通知观察者（拖尾画线等）
7. ticksToImpact<=0?     → 是 → ImpactSomething:
   7a. IBDPArrivalHandler → Continue=true则跳过Impact
   7b. IBDPImpactHandler  → 依次执行命中效果
```

## 7. 模块迁移对照

| 模块 | 旧hook | 新接口 |
|------|--------|--------|
| TrailModule | OnSpawn + OnTick | IBDPProjectileModule + IBDPTickObserver |
| GuidedModule | OnPreImpact | IBDPProjectileModule + IBDPArrivalHandler |
| ExplosionModule | OnImpact | IBDPProjectileModule + IBDPImpactHandler |

## 8. 扩展规则

1. 新增管线阶段：创建新接口+数据包文件，在Bullet_BDP中添加缓存列表和分发逻辑
2. 新增模块：实现IBDPProjectileModule + 需要的管线接口，在BDPModuleFactory注册
3. 模块间协作：通过数据包的可修改字段传递信息，后执行的模块可以看到前面模块的修改
4. GetCapability<T>()：预留的能力查询入口，供外部（如Verb层）查询模块是否实现某接口

## 9. 不在本次范围

- Verb层管线化
- 新模块实现（追踪/分裂/抛物线等）
- Provider接口定义

---

## 历史修改记录
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-25 | 管线架构设计文档初版 | Claude Opus 4.6 |
