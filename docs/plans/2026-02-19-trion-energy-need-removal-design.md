---
标题：Trion能量系统重构——移除Need_Trion，改用Gizmo资源条
版本号: v1.0
更新日期: 2026-02-19
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 对6.1 Trion能量系统的架构重构设计。核心变更：移除Need_Trion，将其三个职责（恢复驱动、GUI显示、耗尽Hediff管理）全部归入CompTrion；新增Gizmo_TrionBar（继承Gizmo_Slider）在底部Gizmo栏显示三段色资源条（占用→可用→空）；简化惰性激活链路为纯max>0判断。不影响CompTrion公开API和所有外部模块接口。
---

# Trion能量系统重构——移除Need_Trion，改用Gizmo资源条

## 1. 变更动机

### 1.1 架构原则性问题

当前设计中Need_Trion承担三个职责：
1. 驱动日常Trion恢复（每150 ticks调用CompTrion.Recover）
2. 在需求面板（左下角，和食物/休息并列）显示Trion状态条
3. 管理Hediff_TrionDepletion的添加/移除/severity

**问题：Trion不是"需求"，它是"资源"。**

- Need系统的语义是"Pawn的生理/心理需求"——食物、休息、心情
- Trion是战斗资源，由外部系统（触发器、战斗体）消耗，不是Pawn内在需求
- 放在Need面板里误导玩家认为它需要像食物一样"照顾"

### 1.2 RimWorld先例

RimWorld中类似资源的显示方式：
- 血源质(Hemogen)：Gene_Resource持有数据 → GeneGizmo_Resource（继承Gizmo_Slider）在底部Gizmo栏显示
- 灵能精神力(Psyfocus)：Pawn_PsychicEntropyTracker持有数据 → PsychicEntropyGizmo（继承Gizmo）在底部Gizmo栏显示

两者都不走Need系统，都在选中Pawn时的底部Gizmo栏显示资源条。

---

## 2. 变更总览

### 2.1 删除

- Need_Trion 类（C#）
- BDP_Need_Trion NeedDef（XML）
- Gene_TrionGland.enablesNeeds 字段（XML）
- 5.1 D7决策（onlyIfCausedByGene惰性激活策略）
- TrionDefExtension.thresholdPercents（原为Need阈值标记）

### 2.2 修改

- CompTrion.CompTick() — 接管恢复驱动 + 耗尽Hediff管理
- CompTrion — 新增gizmo缓存字段
- CompTrion.CompGetGizmosExtra() — 返回Gizmo_TrionBar
- CompProperties_Trion — 新增recoveryInterval、allocatedBarColor字段

### 2.3 新增

- Gizmo_TrionBar : Gizmo_Slider — 三段色资源条

### 2.4 不变（关键确认）

- CompTrion的公开API（Consume/Recover/Allocate/Release/SetFrozen/ForceDeplete）
- Gene_TrionGland的statOffsets机制
- Hediff_TrionDepletion的HediffDef和分级效果
- Stat聚合层（三个StatDef）
- 所有外部模块的接口（触发器/战斗/设施/角色/世界）
- XPath Patch预挂载CompTrion到Human ThingDef

### 2.5 惰性激活链路简化

```
旧：Gene → enablesNeeds → Need创建 → 恢复开始 + GUI显示
新：Gene → statOffsets → max>0 → CompTrion自动恢复 + Gizmo显示
判断条件统一为 max > 0，不再依赖Need系统
```

---

## 3. CompTrion.CompTick() 新增逻辑

```
CompTick() 完整逻辑（修改后）：

  // ── 1. 定期刷新max（不变） ──
  if TicksGame >= refreshTick:
    RefreshMax()
    refreshTick = TicksGame + Props.statRefreshInterval

  // ── 2. 聚合消耗结算（不变） ──
  if drainRegistry.Count > 0 && TicksGame % Props.drainSettleInterval == 0:
    totalDrain = Sum(drainRegistry.Values)
    Consume(totalDrain * Props.drainSettleInterval / TICKS_PER_DAY)

  // ── 3. 恢复驱动（新增，原Need_Trion职责） ──
  if parent is Pawn pawn && max > 0 && !frozen
     && TicksGame % Props.recoveryInterval == 0:
    recoveryRate = pawn.GetStatValue(BDP_StatDefOf.TrionRecoveryRate)
    amount = recoveryRate * Props.recoveryInterval / TICKS_PER_DAY
    Recover(amount)

  // ── 4. 耗尽Hediff管理（新增，原Need_Trion职责） ──
  if parent is Pawn pawn && max > 0
     && TicksGame % Props.recoveryInterval == 0:
    percent = Percent
    hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TrionDepletionDef)
    if percent < Props.depletionThreshold:
      if hediff == null:
        hediff = pawn.health.AddHediff(TrionDepletionDef)
      hediff.Severity = 1.0 - (percent / Props.depletionThreshold)
    else:
      if hediff != null:
        pawn.health.RemoveHediff(hediff)

  // ── 5. 非Pawn载体自动恢复（不变） ──
  if Props.recoveryPerDay > 0 && parent is not Pawn
     && TicksGame % Props.drainSettleInterval == 0:
    Recover(Props.recoveryPerDay * Props.drainSettleInterval / TICKS_PER_DAY)
```

设计决策：
- 恢复和耗尽检查共用recoveryInterval（默认150 ticks，与原NeedInterval一致）
- 恢复条件：max > 0 && !frozen（替代原Need_Trion.IsFrozen检查）
- 耗尽检查条件：max > 0（无论frozen与否都检查，战斗中Trion被消耗也应触发耗尽效果）

---

## 4. Gizmo_TrionBar 设计

### 4.1 定位

```
Gizmo_TrionBar : Gizmo_Slider

职责：在选中Pawn/Building时，底部Gizmo栏显示Trion三段色资源条
来源：CompTrion.CompGetGizmosExtra()（max > 0 && Props.showGizmo时返回）
```

### 4.2 继承Gizmo_Slider的原因

- 自带条形图绘制框架（背景、填充、标签、阈值线）
- 自带拖拽基础设施（虽然不启用拖拽）
- GeneGizmo_Resource也继承它，模式成熟

### 4.3 重写的属性/方法

```
┌──────────────────────┬──────────────────────────────────────┐
│  成员                  │  行为                                  │
├──────────────────────┼──────────────────────────────────────┤
│  ValuePercent (get)   │  comp.Percent（当前值/最大值）          │
│  BarLabel (get)       │  "{cur:F0} / {max:F0}"                │
│  Title (get)          │  "Trion"                               │
│  BarColor (get)       │  Props.barColor（可用段颜色）           │
│  IsDraggable (get)    │  false（不启用拖拽）                    │
│  Target (get/set)     │  不使用（IsDraggable=false时不显示）    │
│  GetBarThresholds()   │  yield return Props.depletionThreshold │
│  DrawBar() [重写]     │  三段色绘制（见下方）                   │
│  GetTooltip() [重写]  │  详细信息tooltip                       │
└──────────────────────┴──────────────────────────────────────┘
```

### 4.4 三段色绘制逻辑

```
段顺序（从左到右）：
┌──────────────────────────────────────────────┐
│  ▓▓▓▓▓▓▓▓████████████░░░░░░░░░░░░░░░░░░░░  │
│  [占用段]  [可用段]    [空段]                  │
│  左侧      中间        右侧                   │
└──────────────────────────────────────────────┘

视觉情绪：
  占用段（左）= 已锁定、已承诺的能量 → 沉稳、压抑
    → 暗青 (0.25, 0.4, 0.45) — "冻结在那里"的感觉
  可用段（中）= 可自由使用的能量 → 活跃、正面
    → 明亮青绿 (0.3, 0.85, 0.55) — "随时可用"的感觉
  空段（右）= 已消耗/待恢复 → 空缺、暗淡
    → 深色背景 (0.1, 0.1, 0.1) — 标准空条

绘制顺序（从底到顶）：
  1. 背景色 — 整条（空段自然显示）
  2. 占用段+可用段底色 — 从0到 cur/max（totalPercent）
     先画占用段色覆盖整个filled区域
  3. 可用段 — 从 allocated/max 到 cur/max
     覆盖在占用段之上，右侧部分变为可用色
  4. 阈值线 — depletionThreshold位置
  5. 文字标签

日常状态（allocated=0）：
  占用段宽度=0，退化为 [可用段][空段] 双色
  视觉上与标准资源条一致

战斗体状态（allocated>0）：
  [占用:暗青][可用:明绿][空:暗] 三段清晰可辨
```

### 4.5 Tooltip内容

```
"Trion: 70 / 100
 可用: 40  占用: 30
 恢复: +50.0/天
 消耗: -5.2/天
 [冻结]"  ← 仅frozen时显示
```

### 4.6 Gizmo缓存

```
CompTrion新增字段：
  private Gizmo_TrionBar gizmo;  // 缓存，避免每帧创建

CompGetGizmosExtra()：
  if max > 0 && Props.showGizmo:
    if gizmo == null:
      gizmo = new Gizmo_TrionBar(this)
    yield return gizmo
```

---

## 5. CompProperties_Trion 变更

```xml
<!-- 新增字段 -->
<recoveryInterval>150</recoveryInterval>
  <!-- Pawn恢复和耗尽检查的tick间隔 -->
  <!-- 150 ticks = 原NeedInterval间隔，保持行为一致 -->

<allocatedBarColor>(0.25, 0.4, 0.45, 1.0)</allocatedBarColor>
  <!-- 占用段颜色：暗青 — 锁定、沉稳 -->

<!-- 已有字段（含义不变） -->
<barColor>(0.3, 0.85, 0.55, 1.0)</barColor>
  <!-- 可用段颜色：明亮青绿 — 活跃、可用 -->
  <!-- 注：原设计中此字段已存在，颜色值更新 -->
```

---

## 6. 受影响文档更新清单

| 文档 | 变更内容 |
|------|---------|
| 6.1 Trion能量系统详细设计 | §1.1移除Need_Trion新增Gizmo；§1.2/§1.3更新；§3.7新增恢复+耗尽逻辑；删除§4.2 Need_Trion；删除§4.4 TrionDefExtension；新增§4.2 Gizmo_TrionBar；§8新增字段；§11更新决策 |
| 5.1 总体架构设计 | §3/§6微内核行移除Need新增Gizmo；§11 D7标记废弃 |
| 6.2 触发器模块详细设计 | 无需修改 |
| Gene_TrionGland GeneDef XML | 移除enablesNeeds字段 |

---

## 7. 设计决策记录

| # | 决策 | 选择 | 理由 | 否决方案 |
|---|------|------|------|---------|
| R1 | Need_Trion去留 | 完全移除 | Trion是资源不是需求；Need面板语义不匹配；GUI应在Gizmo栏 | 保留但隐藏（概念不干净） |
| R2 | 职责重分配 | 全部归入CompTrion | 单一职责集中，不再需要代理层 | 分散到多个组件（增加复杂度） |
| R3 | GUI风格 | Gizmo_Slider+扩展 | 复用框架减少开发量；三段色展示占用/可用/空 | 自定义Gizmo（开发量大）；纯Gizmo_Slider（无法展示占用段） |
| R4 | 三段色顺序 | 占用→可用→空（左到右） | 占用是"已锁定的底层"，可用是"活跃的中间层" | 可用→占用→空（占用在中间不直观） |
| R5 | Gizmo来源 | CompTrion提供 | CompTrion已预挂载到所有Human；max>0判断统一；Building/Item可复用 | Gene提供（Building无法复用） |
| R6 | 目标值功能 | 不启用 | YAGNI；Trion补给方式未确定 | 启用（预留接口但当前无用） |
| R7 | 惰性激活 | 纯max>0判断 | 移除Need后自然简化；不再依赖enablesNeeds | 保留enablesNeeds（多余） |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-19 | 初版完成：Trion能量系统重构设计。移除Need_Trion，三个职责（恢复驱动、GUI显示、耗尽Hediff管理）全部归入CompTrion。新增Gizmo_TrionBar（继承Gizmo_Slider）三段色资源条（占用→可用→空，暗青→明绿→暗）。简化惰性激活链路为纯max>0判断。7项设计决策记录。 | Claude Opus 4.6 |
