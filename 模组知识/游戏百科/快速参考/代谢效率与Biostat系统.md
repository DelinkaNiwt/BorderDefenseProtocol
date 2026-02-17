---
标题：代谢效率与Biostat系统
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld基因系统的Biostat三维度体系详解，包含biostatMet/Cpx/Arc字段、代谢-食物消耗曲线、复杂度-组装时间曲线、Archite胶囊机制、平衡设计原理
---

# 代谢效率与Biostat系统

**总览**：Biostat是基因系统的平衡机制，通过三个维度约束基因组合：**Metabolism（代谢效率）** 影响食物消耗速率，**Complexity（复杂度）** 限制基因总量并影响组装时间，**Archite（远古科技）** 要求稀有的Archite胶囊。每个基因在这三个维度上都有成本或收益，玩家必须在强力效果和代谢/复杂度代价之间权衡。

## 三维度对比表

| 维度 | GeneDef字段 | 范围 | 默认值 | 正值含义 | 负值含义 | 影响 |
|------|-----------|------|--------|---------|---------|------|
| **Metabolism（代谢）** | `biostatMet` | -5 ~ +5 | 0 | 代谢效率高→食物消耗少 | 代谢效率低→食物消耗多 | 食物消耗速率倍率 |
| **Complexity（复杂度）** | `biostatCpx` | -5 ~ +5 | 1 | 占用复杂度配额 | 释放复杂度配额（罕见） | 基因总量限制 + 组装时间 |
| **Archite（远古科技）** | `biostatArc` | 0+ | 0 | 需要Archite胶囊 | — | 组装时需投入稀有资源 |

> **范围常量**：`GeneTuning.BiostatRange = new IntRange(-5, 5)`，代谢和复杂度的有效范围均为-5到+5。

## Metabolism（代谢效率）详解

### 代谢-食物消耗曲线

代谢效率通过`GeneTuning.MetabolismToFoodConsumptionFactorCurve`影响食物消耗速率：

| 代谢效率总值 | 食物消耗倍率 | 说明 |
|------------|------------|------|
| -5 | 2.25× | 食物消耗翻倍以上 |
| -4 | 1.94× | — |
| -3 | 1.63× | — |
| -2 | 1.31× | — |
| -1 | 1.0× | 接近正常 |
| **0**（基准） | **1.0×** | 正常食物消耗 |
| +1 | 0.9× | — |
| +2 | 0.8× | — |
| +3 | 0.7× | — |
| +4 | 0.6× | — |
| +5 | 0.5× | 食物消耗减半 |

> **曲线特征**：负代谢的惩罚（最高2.25×）远大于正代谢的收益（最低0.5×），这是有意的不对称设计——强力基因的代谢代价是显著的。

### 代谢效率计算

Pawn的总代谢效率 = Σ 所有活跃基因的`biostatMet`。该值通过曲线映射为食物消耗倍率，应用于`Need_Food.FoodFallPerTickAssumingCategory`。

## Complexity（复杂度）详解

### 复杂度限制

- **基础最大复杂度**：`GeneTuning.BaseMaxComplexity = 6`
- Pawn的基因总复杂度 = Σ 所有Xenogene的`biostatCpx`
- 超过最大复杂度时，基因组装器无法开始组装

### 复杂度-组装时间曲线

`GeneTuning.ComplexityToCreationHoursCurve`决定Xenogerm的组装时间：

| 总复杂度 | 组装时间（小时） |
|---------|---------------|
| 0 | 3h |
| 4 | 5h |
| 8 | 8h |
| 12 | 12h |
| 16 | 17h |
| 20 | 23h |

> **设计意图**：复杂度越高，组装时间非线性增长，高复杂度方案的时间成本显著。

## Archite（远古科技）详解

- `biostatArc > 0`的基因需要Archite胶囊（`ThingDefOf.ArchiteCapsule`）
- 组装Xenogerm时，基因组装器（Building_GeneAssembler）需要投入对应数量的Archite胶囊
- `ArchitesRequiredNow = architesRequired - ArchitesCount`
- Archite胶囊是稀有资源，通常通过贸易或任务获得
- XenotypeDef的`Archite`属性：如果genes列表中任何基因属于`GeneCategoryDefOf.Archite`分类，则标记为Archite异种

## Biostat平衡设计原理

| 设计原则 | 说明 |
|---------|------|
| **强力效果 = 高代谢代价** | 如Robust体质（+伤害抗性）biostatMet=-2 |
| **纯外观基因 = 零代谢** | 如皮肤颜色、头发颜色 biostatMet=0 |
| **负面效果 = 正代谢收益** | 如UV Sensitivity（阳光敏感）biostatMet=+2 |
| **Archite = 超强效果** | 如Deathless（不死）biostatArc=1 |
| **复杂度是总量限制** | 防止堆叠过多基因，基础上限6 |

## 开发者要点

1. **代谢是核心平衡杠杆**：模组新增基因时，biostatMet的设定直接影响游戏平衡——强力效果应有负代谢代价
2. **复杂度限制基因数量**：biostatCpx默认为1，意味着基础6复杂度只能装6个标准基因。模组可通过设置biostatCpx=0创建"免费"基因
3. **Archite是稀缺性控制**：biostatArc>0的基因需要稀有资源，适合设计为"终极"基因
4. **代谢曲线是非线性的**：-5代谢=2.25×食物消耗，+5代谢=0.5×食物消耗，惩罚远大于收益
5. **Pawn_GeneTracker聚合计算**：总代谢/复杂度由Tracker遍历所有活跃基因累加，模组无需手动计算

## 关键源码引用

| 类/字段 | 位置 | 说明 |
|--------|------|------|
| `GeneTuning.MetabolismToFoodConsumptionFactorCurve` | GeneTuning.cs | 代谢→食物消耗曲线 |
| `GeneTuning.ComplexityToCreationHoursCurve` | GeneTuning.cs | 复杂度→组装时间曲线 |
| `GeneTuning.BiostatRange` | GeneTuning.cs | Biostat有效范围(-5~+5) |
| `GeneTuning.BaseMaxComplexity` | GeneTuning.cs | 基础最大复杂度=6 |
| `GeneDef.biostatMet/biostatCpx/biostatArc` | GeneDef.cs | 三维度字段定义 |
| `Building_GeneAssembler.ArchitesRequiredNow` | Building_GeneAssembler.cs | Archite需求计算 |
| `BiostatsTable` | BiostatsTable.cs | Biostat UI显示 |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 初始版本：三维度对比、代谢-食物消耗曲线、复杂度-组装时间曲线、Archite机制、平衡设计原理 | Claude Opus 4.6 |
