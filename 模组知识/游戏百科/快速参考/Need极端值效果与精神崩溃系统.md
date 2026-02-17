---
标题：Need极端值效果与精神崩溃系统
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: 记录Need达到极端值（0或满）时的游戏效果，重点覆盖精神崩溃系统（MentalBreaker）、营养不良致死链、非自愿睡眠、奴隶叛乱、机械体自动关机等机制
---

## 1. 总览

Need达到极端值时会触发各种后果——从心情惩罚到精神崩溃、从营养不良致死到奴隶叛乱。本文档按危险程度从高到低梳理各Need的极端值效果，重点解析精神崩溃系统和奴隶叛乱系统的源码机制。

---

## 2. 精神崩溃系统（MentalBreaker）

**核心类**：`Verse.AI.MentalBreaker`

### 2.1 三级阈值

阈值基于`MentalBreakThreshold` Stat（默认基础值≈0.25）计算：

| 级别 | 公式 | 默认阈值 | MTB（天） | 含义 |
|------|------|---------|----------|------|
| **Minor** | Stat × 1.0 | ≈0.25 | 4.0 | 小型崩溃（发呆、暴食） |
| **Major** | Stat × 4/7 | ≈0.143 | 0.8 | 中型崩溃（破坏、纵火） |
| **Extreme** | Stat × 1/7 | ≈0.036 | 0.5 | 大型崩溃（杀人、自杀） |

### 2.2 触发机制

```
每150 ticks检查一次：
  1. 心情 < 阈值 → ticksBelow += 150
  2. 心情 ≥ 阈值 → ticksBelow = 0（重置）
  3. ticksBelow > 2000 → 按MTB概率触发崩溃
  4. 冷却期检查：ticksUntilCanDoMentalBreak > 0 → 跳过
```

**关键常量**（源码验证）：
- `MinTicksBelowToBreak = 2000`（约33分钟游戏内时间）
- `MinTicksSinceRecoveryToBreak = 15000`（恢复后冷却期，约6小时游戏内时间）
- `CheckInterval = 150`

### 2.3 最终稻草机制（Final Straw）

`RandomFinalStraw()`从当前所有负面思想中选择一个作为崩溃触发原因：
- 筛选条件：MoodOffset ≤ 最差思想的50%
- 按负面程度加权随机选择
- 显示为"最后一根稻草：XXX"

### 2.4 崩溃类型选择

- 按`MentalBreakIntensity`（Extreme/Major/Minor）筛选可用崩溃类型
- 按`CommonalityFor(pawn)`加权随机选择具体崩溃
- Ideology可覆盖崩溃列表（意识形态专属崩溃）
- Anomaly DLC按`anomalyMentalBreakChance`概率选择异常崩溃

### 2.5 限制条件

崩溃**不会发生**的情况：
- Pawn倒地、睡眠、已在精神状态中
- 变异体且`preventsMentalBreaks=true`
- 非玩家阵营且崩溃强度非Extreme
- 任务禁用随机崩溃
- `MentalBreaksBlocked()`返回true

---

## 3. 各Need极端值效果详表

### 3.1 Food=0：营养不良致死链

**核心类**：`RimWorld.Need_Food` + `HediffDef:Malnutrition`

**致死链**：
```
Food降至0 → Starving状态
  → 每150 ticks增加Malnutrition Severity（约0.453/天 × 0.8~1.2随机因子）
  → Malnutrition 5个阶段逐步恶化
  → Severity ≥ 1.0 → lethalSeverity触发死亡
```

**Malnutrition阶段表**（源码验证）：

| 阶段 | minSeverity | 意识惩罚 | 饥饿速率加成 | 社交冲突倍率 |
|------|------------|---------|------------|------------|
| Trivial | 0 | -5% | +50% | ×1.5 |
| Minor | 0.2 | -10% | +60% | ×2.0 |
| Moderate | 0.4 | -20% | +60% | ×2.5 |
| Severe | 0.6 | -30% | +60% | ×3.0 |
| Extreme | 0.8 | setMax=0.1 | +60% | — |

**致死时间**：1.0 ÷ 0.453 ≈ **2.2天**（受随机因子影响，实际1.8~2.8天）

**恢复机制**：进食后Food>0，Malnutrition以相同速率下降

### 3.2 Rest=0：非自愿睡眠

**核心类**：`RimWorld.Need_Rest`

**触发机制**：
```
Rest降至0 → ticksAtZero开始累计
  → ticksAtZero > 1000 → 按MTB概率触发非自愿睡眠
  → 强制开始LayDown Job（startInvoluntarySleep=true）
```

**MTB递增表**（源码验证）：

| ticksAtZero范围 | MTB（天） | 说明 |
|----------------|----------|------|
| ≤ 1000 | ∞ | 不触发 |
| 1001 ~ 15000 | 0.25 | 基础概率 |
| 15001 ~ 30000 | 0.125 | 概率翻倍 |
| 30001 ~ 45000 | 1/12 ≈ 0.083 | 继续增加 |
| ≥ 45000 | 0.0625 | 几乎必然触发 |

**特殊行为**：
- 如果Pawn在精神状态中且`recoverFromCollapsingExhausted=true`，非自愿睡眠会结束精神状态
- 不致死，但倒地后可能触发倒地即死机制（非玩家阵营）

### 3.3 Mood极端值：精神崩溃

详见第2节。心情本身不直接致死，但通过精神崩溃间接导致：
- **Extreme崩溃**可能包含自杀（Berserk、杀人狂暴等）
- 崩溃期间Pawn不受控制，可能被其他Pawn击杀

**Joy对Mood的影响**：
- Joy极低（Recreation-starved）：约-20心情
- Joy满值：约+10心情
- Joy本身不直接触发任何极端效果，仅通过Mood间接影响

### 3.4 Suppression=0：奴隶叛乱

**核心类**：`RimWorld.SlaveRebellionUtility`（Ideology DLC）

**叛乱MTB计算**：
```
基础MTB = 45天
÷ 压制因子（Suppression 0→5x, 0.333→1.5x, 0.5→1x, 1→0.25x）
÷ 奴隶数量因子（1人→1x, 5人→0.5x, 10人→0.3x, 20人→0.2x）
÷ 心情因子（0→1.5x, 0.5→1x, 1→0.8x）
÷ 移动能力因子（0→0.01x, 0.5→0.5x, 1→1x）
÷ 其他因子（武器附近×4, 无人看管×20, 靠近地图边缘×1.7）
× 极乐脑叶切除术×10（大幅降低叛乱概率）
```

**3种叛乱规模**（源码验证）：

| 类型 | 范围 | 说明 |
|------|------|------|
| **SingleRebellion** | 仅发起者 | 单人叛乱 |
| **LocalRebellion** | 35格范围内奴隶 | 局部叛乱 |
| **GrandRebellion** | 地图上所有奴隶 | 全体叛乱 |

**2种行为模式**：
- **攻击性**（50%概率）：奴隶武装起来攻击殖民者
- **逃跑**（50%概率）：奴隶试图逃离地图

### 3.5 MechEnergy=0：自动关机

**核心类**：`RimWorld.Need_MechEnergy`（Biotech DLC）

**机制**（源码验证）：
```
MechEnergy降至0 → selfShutdown = true
  → 添加SelfShutdown Hediff
  → 强制开始SelfShutdown Job（躺下不动）
  → 自动关机期间以1/天速率缓慢恢复能量
  → 能量恢复至15或开始充电 → selfShutdown = false → 移除Hediff
```

**消耗速率**：
- 活跃状态：10/天 × `MechEnergyUsageFactor`
- 空闲状态：3/天 × `MechEnergyUsageFactor`
- 关机/充电/倒地/远行：0/天

**不致死**，但关机期间完全无法行动。

### 3.6 Authority极端值：权威不满

**核心类**：`RimWorld.Need_Authority`（Royalty DLC）

- Authority极低时产生负面心情思想
- 不直接致死，通过Mood间接影响
- 通过演讲、统治行为恢复

---

## 4. 危险性排序总表

| 排名 | Need | 极端效果 | 致命性 | 时间窗口 |
|------|------|---------|--------|---------|
| 1 | **Food=0** | 营养不良→死亡 | ⚠️ 直接致死 | ~2.2天 |
| 2 | **Mood过低** | 精神崩溃（含自杀/杀人） | ⚠️ 间接致死 | MTB 0.5~4天 |
| 3 | **Suppression=0** | 奴隶叛乱（攻击/逃跑） | ⚠️ 高危险 | MTB可低至~2天 |
| 4 | **Rest=0** | 非自愿睡眠 | 低（倒地即死风险） | MTB 0.25天起 |
| 5 | **MechEnergy=0** | 自动关机 | 不致死 | 即时 |
| 6 | **Joy极低** | -20心情 | 间接（通过Mood） | — |
| 7 | **Authority极低** | 负面心情 | 间接（通过Mood） | — |

---

## 5. 开发者要点

1. **Food是唯一直接致死的Need**：Malnutrition的`lethalSeverity=1`，约2.2天致死。模组设计自定义Need时，如需致死效果应参考此模式（Need→Hediff→lethalSeverity）
2. **精神崩溃有2000 ticks最低持续要求**：心情短暂跌破阈值不会立即触发崩溃，这为玩家提供了反应窗口
3. **冷却期15000 ticks**：崩溃恢复后有约6小时冷却期，防止连续崩溃
4. **奴隶叛乱概率受多因子影响**：压制值、奴隶数量、心情、武器可及性、看守者在场等，模组可通过调整这些因子控制叛乱难度
5. **MechEnergy的自动关机是可恢复的**：关机期间缓慢恢复能量，不需要外部干预（但充电更快）

---

## 6. 关键源码引用表

| 类 | 命名空间 | 关键方法/字段 | 说明 |
|----|---------|-------------|------|
| `MentalBreaker` | `Verse.AI` | `TestMoodMentalBreak()`, `RandomFinalStraw()` | 精神崩溃触发和最终稻草 |
| `Need_Food` | `RimWorld` | `NeedInterval()`, `MalnutritionSeverityPerInterval` | 饥饿→营养不良 |
| `Need_Rest` | `RimWorld` | `ShouldInvoluntarySleepFromMTB()`, `ticksAtZero` | 非自愿睡眠 |
| `Need_MechEnergy` | `RimWorld` | `NeedInterval()`, `selfShutdown` | 机械体自动关机 |
| `SlaveRebellionUtility` | `RimWorld` | `InitiateSlaveRebellionMtbDaysHelper()`, `StartSlaveRebellion()` | 奴隶叛乱 |
| `HediffDef:Malnutrition` | Core XML | `lethalSeverity=1`, 5个stages | 营养不良定义 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 创建文档：精神崩溃系统、各Need极端值效果、奴隶叛乱系统、危险性排序，全部经源码验证 | Claude Opus 4.6 |
