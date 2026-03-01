# BDP弹道系统架构交叉验证与审计报告（2026-02-28）

## 0. 报告目标与范围
- 目标：对“BDP弹道系统中管线架构、`Tracking` 与 `Guided` 关系、命中/穿墙/路径碰撞异常”进行静态架构审计。
- 方法：以源码与引擎行为为证据，交叉验证两份分析（本轮审计 + Claude Opus 报告），输出统一结论与客观自审。
- 范围：仅分析，不提出代码变更实现，不改行为。

## 1. 执行摘要
- 结论1：当前系统已经偏离“导航模块只决定下一飞行方向”的边界，出现导航职责与结果职责（命中、生命周期、拦截语义适配）混杂。
- 结论2：`Guided` 与 `Tracking` 通过宿主共享可变状态形成隐式协作协议，时序依赖强，导致补丁链增长。
- 结论3：命中判定、穿墙/拦截、路径碰撞三个痛点并非独立缺陷，而是同一架构耦合问题的不同外显。
- 结论4：Claude 报告 6 个核心问题总体成立，其中 1 处需修正表述强度：`usedTarget` 强制同步机制“存在且可触发”，但默认配置为 `false`。

## 2. 交叉验证矩阵（Claude 报告逐条验真）

| 编号 | Claude结论 | 交叉验证结论 | 证据 |
|---|---|---|---|
| 1 | Tracking 越权管理命中判定（改 `usedTarget`） | **成立（需修正强度）**：机制存在，默认配置为关闭，但架构层越界已形成 | `TrackingModule.cs:199-202`, `TrackingModule.cs:342-345`, `BDPTrackingConfig.cs:69`, `BDPTrackingConfig.cs:72` |
| 2 | Tracking 越权管理生命周期（自毁） | **成立**：`IBDPTickObserver` 文档是观察者，但实现中执行 `Destroy()` | `IBDPTickObserver.cs:4-10`, `TrackingModule.cs:261-268`, `TrackingModule.cs:287-299` |
| 3 | `origin` 后退 `6f` 是拦截适配 hack | **成立**：与原版 `InterceptChanceFactorFromDistance` 强耦合，改变 `origin` 语义 | `Bullet_BDP.cs:135-138`, `Bullet_BDP.cs:171-173`, `VerbUtility.cs:147-158`, `Projectile.cs:271-326` |
| 4 | `IsOnFinalSegment` 造成 Guided/Tracking 隐式耦合 | **成立**：无显式接口契约，全靠共享字段和顺序假设 | `TrackingModule.cs:103-108`, `GuidedModule.cs:50`, `GuidedModule.cs:84`, `Bullet_BDP.cs:289-304` |
| 5 | `GuidedVerbState` 胶水层过厚 | **成立**：单类承担 LOS、目标翻译、自动绕行、单双侧状态等跨层职责 | `GuidedVerbState.cs:61-69`, `GuidedVerbState.cs:86-104`, `GuidedVerbState.cs:127-149`, `GuidedVerbState.cs:182-199`, `GuidedVerbState.cs:246-265` |
| 6 | `RedirectFlightTracking` 远近双策略形成阈值状态机 | **成立**：`3x` 与 `1.5x` 阈值为经验参数，易产生边界连锁 | `Bullet_BDP.cs:176-197`, `TrackingModule.cs:196-209`, `TrackingModule.cs:356-360` |

### 对第1条的客观修正
- Claude 报告将 `usedTarget` 操作描述为常态行为。实际代码是**条件开关**：`forceUsedTargetOnFinalApproach/forceUsedTargetOnArrival` 默认 `false`。
- 但该机制已经进入架构，意味着模块边界被突破，后续维护仍会围绕该越界机制堆补丁。

## 3. 当前“管线”真实架构（实现级）

### 3.1 启动与模块装配
- 模块由 Def 扩展驱动并在 `BDPMod` 注册：`Guided`、`Tracking`、`Explosion`、`Trail`。
- `Bullet_BDP.SpawnSetup` 创建模块、按 `Priority` 排序、构建接口缓存。

证据：
- `BDPMod.cs:26-29`
- `Bullet_BDP.cs:212-214`
- `Bullet_BDP.cs:242-260`

### 3.2 Tick阶段执行顺序
1. `PathResolver`（可改 `Destination`）
2. `base.TickInterval`（原版飞行+拦截+到达判定）
3. `PositionModifier`
4. `TickObserver`

证据：
- `Bullet_BDP.cs:306-313`
- `Bullet_BDP.cs:319`
- `Bullet_BDP.cs:331-350`
- 原版：`Projectile.cs:238-267`

### 3.3 Arrival阶段
- `ImpactSomething` 先分发 `arrivalHandlers`，若 `ctx.Continue=true` 则跳过本次 Impact。
- 默认顺序下 `Guided(P10)` 先于 `Tracking(P15)`，二者共享宿主状态进行阶段切换。

证据：
- `Bullet_BDP.cs:362-372`
- `GuidedModule.cs:39-63`
- `TrackingModule.cs:320-361`

## 4. 关键架构问题（整合后）

### A. 导航层与结果层边界被打穿（最高优先级）
- 导航模块/宿主开始处理命中保障、过期落地、生命周期结束等结果语义。
- 典型表现：
  - `TrackingModule` 存在“强制命中对象同步”能力。
  - `Bullet_BDP.ImpactSomething` 因 `TrackingExpired` 直接 `Impact(null)`，绕开原版 `usedTarget` 直击分支。
  - `TrackingModule.OnTick` 管理超时/丢锁自毁。

证据：
- `TrackingModule.cs:199-202`, `TrackingModule.cs:342-345`
- `Bullet_BDP.cs:377-387`
- `TrackingModule.cs:261-268`, `TrackingModule.cs:287-299`
- 原版对比：`Projectile.cs:490-534`

影响：
- 模块不再“只告诉子弹往哪飞”，而是在争夺“飞到了算不算中、什么时候死亡”的最终裁决权。
- 问题定位复杂化，修复点从几何层外溢到命中流程层。

### B. Guided 与 Tracking 通过共享状态形成隐式协议
- 关键共享字段：`FinalTarget`、`TrackingTarget`、`IsOnFinalSegment`、`TrackingExpired`。
- `GuidedModule.SetWaypoints` 必须回写 `TrackingTarget`，否则追踪激活链路断裂（代码注释已承认该时序依赖）。

证据：
- `GuidedModule.cs:79-84`
- `TrackingModule.cs:85`, `TrackingModule.cs:103-108`
- `Bullet_BDP.cs:289-304`

影响：
- 任一模块改字段语义，会在另一个模块出现“非本地故障”。
- Priority/生命周期微调容易触发“追踪不启动”“提前启动”等连锁问题。

### C. `origin` 语义漂移：从物理起点变成拦截概率适配变量
- 每次重定向都后退 `origin`（`ORIGIN_OFFSET=6f`），用于绕过原版近距离拦截概率归零。
- 该行为与原版拦截模型强绑定，不再是纯几何重定向。

证据：
- `Bullet_BDP.cs:135-145`, `Bullet_BDP.cs:171-181`
- 原版：`VerbUtility.cs:147-158`, `Projectile.cs:333-406`

影响：
- 几何计算与规则语义耦合，后续任何依赖 `origin` 的逻辑都可能被动受影响。
- “修穿墙/拦截”与“修命中/插值时机”互相牵连。

### D. 生命周期时序补丁化
- `SpawnSetup` 与 `Launch` 时机不一致，新增 `postLaunchInitDone` 在首 tick 二次修正目标。

证据：
- `Bullet_BDP.cs:289-304`
- `Projectile.cs:178-227`（`Launch` 才写入关键飞行状态）

影响：
- 目标真值来源不单一（`intendedTarget`/`FinalTarget`/`TrackingTarget`）。
- 增加“偶发有效、偶发失效”的调试难度。

### E. 管线契约与实现/文档漂移
- `IBDPTickObserver` 文档“只读观察”，但 `TrackingModule` 在其中执行销毁。
- `IBDPInterceptModifier` 文件已标注“移除”，但历史设计文档仍在描述相关阶段。

证据：
- `IBDPTickObserver.cs:4-10` vs `TrackingModule.cs:261-299`
- `Pipeline/IBDPInterceptModifier.cs:1-4`
- `docs/plans/2026-02-27-tracking-module-design.md:32-41`

影响：
- 团队认知模型和真实执行模型不一致，导致后续设计评审误判。

## 5. 功能痛点与架构问题映射

### 5.1 命中如何判定
- 引导态把 `currentTarget` 锁成 Cell 后，原版 `ImpactSomething` 可能回退到概率扫描分支。
- 随后系统引入“可选强制同步 `usedTarget`”与“`TrackingExpired` 落地”作为补丁对冲。

证据：
- `GuidedVerbState.cs:130-134`
- `Verb_BDPRangedBase.cs:243-247`
- `Projectile.cs:512-572`
- `Bullet_BDP.cs:377-387`

### 5.2 会不会穿墙
- 冲突根本不是“墙判定写错”，而是“高频重定向 + 原版拦截距离因子”的语义冲突。
- 结果是必须引入 `ORIGIN_OFFSET` 维持拦截概率。

证据：
- `Bullet_BDP.cs:171-181`
- `VerbUtility.cs:147-158`
- `Projectile.cs:333-406`

### 5.3 路径上碰撞判定
- `Arrival` 阶段不断 `Continue` 重定向会改变进入原版 `Impact` 的节奏。
- 丢锁自毁进一步把“应交给原版碰撞结算”的路径直接截断。

证据：
- `TrackingModule.cs:356-361`
- `Bullet_BDP.cs:368-372`
- `TrackingModule.cs:287-299`

## 6. 连锁影响图（补丁网）

1. 引导/追踪改 `currentTarget` 与方向  
2. 触发原版 `usedTarget` / 命中分支行为差异  
3. 加入 `usedTarget` 同步能力（可选）  
4. 出现“过期后命中语义冲突”  
5. 加 `TrackingExpired -> Impact(null)`  
6. 为防残留副作用，再加丢锁/超时自毁  
7. 高频重定向触发拦截距离问题，再加 `origin` 后退 hack

结果：每次补丁都在补前一层补丁的副作用，形成“修1处坏多处”的结构性循环。

## 7. 需求基线一致性检查
- 需求文档强调：子弹不可穿墙、除非明确说明不应有“必中”结果。
- 当前实现中的 `origin` 适配与命中补丁链，说明系统正在为满足基线而承担额外结构复杂度。

证据：
- `docs/各类投射物需求描述.md:2`
- `docs/各类投射物需求描述.md:6`

附带发现：
- `Argus` 注释写 `trackingDelay=0`，但配置值为 `3`，会增加行为理解偏差。

证据：
- `ThingDefs_Projectiles.xml:154`
- `ThingDefs_Projectiles.xml:185`

## 8. 客观自审（架构诊断质量）

### 8.1 已完成的客观性控制
- 使用“外部结论逐条验真”而非整单接受。
- 对每一条高优先问题给出实现级证据（类/方法/行）。
- 将“架构问题”与“参数默认值”分开陈述（如 `forceUsedTarget*` 默认关闭）。

### 8.2 本报告的可信度边界
- 本次为静态审计，未执行运行时回放与战斗日志对照。
- 因此“因果链强度”分为两层：
  - 强证据：代码中直接越界职责、隐式耦合、契约漂移。
  - 中证据：某些具体症状出现频率与触发条件（需运行时统计强化）。

### 8.3 可能误差点（已显式保留）
- 由于部分历史注释编码异常，注释语义未作为主要证据，核心依据是可执行代码路径。
- 未在本轮复测所有弹种组合（单侧、双侧、齐射、组合技）的概率分布差异。

## 9. 最终结论
- 用户提出的架构原则成立：`Guided/Tracking` 应聚焦导航意图，不应承担命中裁决、生命周期终止、引擎拦截语义修正。
- 当前实现中，问题不是“某个 if 写错”，而是“职责跨层 + 状态耦合 + 时序补丁化”导致的系统性不稳定。
- Claude 报告整体判断方向正确；本报告完成了证据化校核、强度修正与架构层整合。

---

## 证据索引（路径）
- `Source/BDP/Trigger/Projectiles/Bullet_BDP.cs`
- `Source/BDP/Trigger/Projectiles/TrackingModule.cs`
- `Source/BDP/Trigger/Projectiles/GuidedModule.cs`
- `Source/BDP/Trigger/Projectiles/GuidedVerbState.cs`
- `Source/BDP/Trigger/DualWeapon/Verb_BDPRangedBase.cs`
- `Source/BDP/Trigger/Projectiles/Pipeline/IBDPTickObserver.cs`
- `Source/BDP/Trigger/Projectiles/Pipeline/IBDPInterceptModifier.cs`
- `Source/BDP/Trigger/Data/BDPTrackingConfig.cs`
- `1.6/Defs/Trigger/ThingDefs_Projectiles.xml`
- `docs/各类投射物需求描述.md`
- RimWorld 引擎参考：
  - `Verse/Projectile.cs`
  - `Verse/VerbUtility.cs`
