# BDP 弹道系统 Kernel V2 重构方案（Guided/Tracking 去越权）

> 日期：2026-02-28  
> 面向问题：引导/追踪模块越权、管线契约漂移、补丁连锁导致的命中/穿墙/路径判定不稳定

---

## 1. 目标与边界

### 1.1 目标

1. 让 `Guided`/`Tracking` 只负责“下一步往哪飞”，不再负责命中裁决、生命周期裁决、引擎兼容补丁。
2. 支持新模块以低耦合方式接入宿主，不依赖共享字段和执行顺序暗约定。
3. 将 vanilla 兼容逻辑集中到宿主兼容层，停止在各模块里分散补丁。

### 1.2 非目标

1. 不在本方案中扩展新弹种玩法，只重构架构和职责。
2. 不改现有武器/芯片平衡参数（伤害、射速、消耗）。

---

## 2. 当前实现的结构性问题（代码证据）

1. `PathResolver` 契约是“改 `Destination`”，但追踪实际通过宿主改 `origin/ticksToImpact`。  
   证据：`PathContext` 仅暴露 `Destination` 可写；`TrackingModule.ResolvePath` 调 `RedirectFlightTracking`；`Bullet_BDP.RedirectFlightTracking` 直接写核心飞行字段。

2. `TickObserver` 契约是只读观察，但追踪在该阶段执行 `Destroy()`。  
   证据：`IBDPTickObserver` 注释为只读观察；`TrackingModule.OnTick` 中有超时/丢锁自毁。

3. 引导与追踪通过宿主共享字段隐式耦合。  
   证据：`GuidedModule` 设置 `IsOnFinalSegment/TrackingTarget`，`TrackingModule` 依赖该字段激活。

4. `ImpactSomething` 里内置 `TrackingExpired -> Impact(null)`，导航问题被推到结果层补丁。  
   证据：`Bullet_BDP.ImpactSomething` 的 `TrackingExpired` 分支。

5. 发射时序存在“多源真相”，导致 `postLaunchInitDone` 补丁化初始化。  
   证据：`SpawnSetup` 与首 tick 的 `postLaunchInitDone` 回填。

---

## 3. 目标架构：Ballistics Kernel V2

### 3.1 分层原则

1. **Flight Intent 层（导航）**：模块只能提交“飞行意图”，不能直接改宿主核心飞行字段。  
2. **Kernel 层（宿主）**：唯一有权限写 `origin/destination/ticksToImpact`。  
3. **Outcome 层（结果）**：命中、销毁、爆炸、穿透统一在结果层决策。  
4. **Compatibility 层（vanilla 适配）**：所有引擎语义兼容集中处理。  
5. **Observer 层（只读）**：拖尾/特效/日志，不允许副作用。

### 3.2 新接口分组（建议）

1. `IFlightIntentProvider`：输入只读上下文，输出 `FlightIntent`（方向/目标点/阶段建议）。  
2. `ILifecyclePolicy`：输出 `LifecycleDecision`（继续/失效/销毁原因）。  
3. `IArrivalPolicy`：输出 `ArrivalDecision`（继续飞行/进入命中解析）。  
4. `IImpactPolicy`：输出 `ImpactDecision`（命中对象/地面/爆炸/替代处理）。  
5. `IVisualObserver`：只读观察，不可修改宿主。

### 3.3 权限硬约束

1. 仅宿主 Kernel 可调用 `Destroy()/Impact()/RedirectFlight*`。
2. 模块禁止直接写 `usedTarget`、`TrackingExpired`、`IsOnFinalSegment` 等宿主状态。
3. 模块间通信只走显式事件或只读状态，不走共享可变字段。

---

## 4. 关键决策（每项 2-3 方案 + 建议）

### 决策 A：导航模块如何表达“往哪飞”

| 方案 | 描述 | 优点 | 风险 |
|---|---|---|---|
| A1 | 维持现状：模块直接调用宿主重定向方法 | 改动最小 | 越权和耦合持续存在 |
| A2 | **FlightIntent 命令化**：模块只提交意图，宿主统一应用 | **职责清晰、可审计、易扩展** | 需要重写接口和调度 |
| A3 | 完整自研速度/位置仿真（弱化引擎飞行逻辑） | 控制力最强 | 风险最高，维护成本大 |

**建议：A2**

### 决策 B：Guided 与 Tracking 如何协作

| 方案 | 描述 | 优点 | 风险 |
|---|---|---|---|
| B1 | 继续用 `IsOnFinalSegment` 共享字段 | 改动小 | 隐式耦合与时序脆弱不变 |
| B2 | **宿主阶段机**：`FlightPhase` 显式状态（Guided, TerminalTracking, TerminalBallistic） | **解耦、可视化、便于新增模块** | 需要重构状态流 |
| B3 | 合并成一个 `GuidedTrackingComposite` 模块 | 实现集中 | 组合弹可扩展性变差 |

**建议：B2**

### 决策 C：`usedTarget` 与命中判定冲突如何处理

| 方案 | 描述 | 优点 | 风险 |
|---|---|---|---|
| C1 | 继续在 Tracking 中按条件强制同步 `usedTarget` | 实现快 | 导航层越权结果层 |
| C2 | **宿主级 HitResolver**：`ImpactSomething` 仅对 `Bullet_BDP` 做统一命中解析，导航模块不触碰 `usedTarget` | **边界清晰、补丁集中** | 需一次性改命中流程 |
| C3 | Harmony 改 vanilla `Projectile.ImpactSomething` 通用逻辑 | 统一性高 | 影响面大，版本维护成本高 |

**建议：C2**

### 决策 D：拦截/穿墙（origin 语义冲突）如何处理

| 方案 | 描述 | 优点 | 风险 |
|---|---|---|---|
| D1 | 继续 `ORIGIN_OFFSET`，但上移到 Compatibility 层统一处理 | 成本低、快速止血 | 仍有语义折中 |
| D2 | 针对 `Bullet_BDP` Patch 拦截距离因子（移除近距归零冲突） | 语义更干净 | 引擎版本兼容成本上升 |
| D3 | 自研碰撞扫描替代 vanilla 拦截 | 控制力强 | 成本与风险最高 |

**建议：短期 D1，稳定后评估 D2**

### 决策 E：迁移策略

| 方案 | 描述 | 优点 | 风险 |
|---|---|---|---|
| E1 | 一次性替换（Big Bang） | 架构最干净 | 回归风险高 |
| E2 | **双管线并行 + Feature Flag**（`BallisticsKernelV2Enabled`） | **可灰度、可回滚、便于对照验证** | 过渡期代码增加 |
| E3 | 仅局部替换 Tracking，Guided 后续再改 | 初期快 | 过渡耦合时间长 |

**建议：E2**

---

## 5. 推荐总方案（组合）

**A2 + B2 + C2 + D1(阶段1)/D2(阶段2) + E2**

这组组合可以在不牺牲可回滚性的前提下，达成“低耦合、不越权、模块可扩展”的目标。

---

## 6. 落地执行蓝图（建议 6 阶段）

### 阶段 0：基线固化

1. 固化关键战斗场景回放与日志样本（Hound/Viper/Argus/Hornet）。
2. 记录当前行为基线：命中率、穿墙率、丢锁后行为、护盾/掩体表现。

### 阶段 1：Kernel V2 骨架

1. 新增上下文与决策对象：`FlightContext/FlightIntent/LifecycleDecision/ImpactDecision`。
2. 新增接口组：`IFlightIntentProvider/ILifecyclePolicy/IArrivalPolicy/IImpactPolicy/IVisualObserver`。
3. `Bullet_BDP` 增加 V2 调度分支（Feature Flag 控制）。

### 阶段 2：Guided V2 重写

1. `GuidedModule` 仅输出航向意图和阶段切换建议。
2. 删除对 `TrackingTarget/IsOnFinalSegment` 的直接写入。
3. `GuidedVerbState` 只负责发射前输入，不再承担结果层修补语义。

### 阶段 3：Tracking V2 重写

1. `TrackingModule` 仅输出转向意图与重锁建议。
2. 去除 `OnTick` 自毁与 `usedTarget` 修改。
3. 去除 `HandleArrival` 对命中保证的写入逻辑。

### 阶段 4：Outcome/Compatibility 收口

1. 把 `TrackingExpired`、`usedTarget` 相关逻辑迁移到宿主 `HitResolver`。
2. 拦截兼容策略集中到 `Compatibility` 层（先保留 D1）。
3. 生命周期策略迁到 `ILifecyclePolicy`（统一自毁/过期规则）。

### 阶段 5：清理与文档对齐

1. 删除旧接口或保留空壳兼容层并标记废弃。
2. 修正文档与 XML 注释不一致（例如 Argus `trackingDelay` 注释）。
3. 删除过期字段与跨层补丁逻辑。

---

## 7. 验收标准（必须全部满足）

1. `Guided`/`Tracking` 源码中不再出现 `Destroy`、`Impact`、`usedTarget` 直接写入。
2. `IBDPTickObserver` 实现全部为只读行为（视觉/日志类）。
3. 只有一个位置写核心飞行字段（Kernel 统一应用）。
4. Argus（引导+追踪）在障碍、移动目标、护盾场景下行为稳定且无“修一处坏多处”回归。
5. Feature Flag 可一键切回旧管线。

---

## 8. 风险与缓解

1. **风险**：重构初期行为偏移。  
   **缓解**：双管线并行 + 对照日志 + 可回滚开关。

2. **风险**：命中解析改动影响掩体/护盾语义。  
   **缓解**：先做 `Bullet_BDP` 局部解析，不改全局 vanilla。

3. **风险**：模块迁移期间状态字段并存导致混乱。  
   **缓解**：阶段性禁用旧字段写入，增加运行时断言和诊断日志。

