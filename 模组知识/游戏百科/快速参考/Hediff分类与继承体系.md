---
标题：Hediff分类与继承体系
版本号: v1.0
更新日期: 2026-02-10
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld Hediff系统的完整分类体系，包含8大功能分类、30个C#子类继承树、HediffDef关键字段表和XML抽象父模板速查
---

# Hediff分类与继承体系

**总览**：Hediff（Health Difference）是RimWorld中附加在Pawn身上的一切健康状态的统称——从刀伤到癌症、从仿生臂到心灵连接、从药物兴奋到怀孕，全部是Hediff。

## 按功能分8大类

| # | 功能分类 | C#类 | XML模板 | 典型实例 | DLC |
|---|---------|------|---------|---------|-----|
| 1 | **物理伤害** | `Hediff_Injury` | `InjuryBase` | 切割伤、瘀伤、烧伤、枪伤 | Core |
| 2 | **缺失部位** | `Hediff_MissingPart` | — | 失去左臂、失去左眼 | Core |
| 3 | **植入体/假肢** | `Hediff_Implant` → `Hediff_AddedPart` | `ImplantHediffBase` | 仿生眼、义肢、心灵连接器 | Core |
| 4 | **疾病/状态** | `HediffWithComps` | `DiseaseBase` / `ChronicDiseaseBase` | 流感、瘟疫、哮喘、白内障 | Core |
| 5 | **药物相关** | `Hediff_High` / `Hediff_Addiction` / `Hediff_Hangover` | `AddictionBase` | 烟瘾、酒精兴奋、宿醉 | Core |
| 6 | **生育相关** | `Hediff_Pregnant` / `Hediff_Labor` / `Hediff_LaborPushing` | — | 怀孕、分娩 | Biotech |
| 7 | **基因/机械相关** | `Hediff_Deathrest` / `Hediff_ChemicalDependency` / `Hediff_BandNode` / `Hediff_VatLearning` | — | 死眠、化学依赖、频段连接、培养缸学习 | Biotech |
| 8 | **异常相关** | `Hediff_Shambler` / `Hediff_MetalhorrorImplant` / `Hediff_DeathRefusal` 等 | `OrganDecayBase` | 蹒跚者转化、金属恐怖寄生、拒绝死亡 | Anomaly |

> **开发者关键认知**：绝大多数游戏中的疾病、Buff、Debuff并不需要自定义C#类——它们直接使用`HediffWithComps`，通过XML配置`HediffComp`组件来实现各种效果。只有需要特殊逻辑的状态才需要继承写新的Hediff子类。

## C#完整继承树（30个Hediff_子类，源码验证）

```
Hediff (根基类)
├── HediffWithComps (支持组件系统，最常用基类)
│   ├── Hediff_Injury (受伤)
│   ├── Hediff_MissingPart (缺失部位)
│   ├── Hediff_Implant (植入体)
│   │   └── Hediff_AddedPart (替换部位/假肢)
│   ├── Hediff_Level (等级制)
│   │   └── Hediff_Psylink (心灵连接, Royalty)
│   ├── Hediff_Addiction (成瘾)
│   ├── Hediff_High (药物兴奋)
│   │   └── Hediff_Alcohol (酒精兴奋)
│   ├── Hediff_Hangover (宿醉)
│   ├── Hediff_ChemicalDependency (基因化学依赖, Biotech)
│   ├── Hediff_HemogenCraving (血源素渴求, Biotech)
│   ├── Hediff_HeartAttack (心脏病发)
│   ├── Hediff_Deathrest (死眠, Biotech)
│   ├── Hediff_DeathRefusal (拒绝死亡, Anomaly)
│   ├── Hediff_VatLearning (培养缸学习, Biotech)
│   ├── Hediff_DuplicateSickness (复制体病变, Anomaly)
│   ├── Hediff_Shambler (蹒跚者转化, Anomaly)
│   ├── Hediff_MetalhorrorImplant (金属恐怖寄生, Anomaly)
│   ├── Hediff_PainField (痛苦力场, Anomaly)
│   ├── Hediff_DisruptorFlash (干扰闪光, Anomaly)
│   ├── HediffWithParents (追踪父母信息)
│   │   ├── Hediff_Pregnant (怀孕, Biotech)
│   │   ├── Hediff_Labor (分娩, Biotech)
│   │   └── Hediff_LaborPushing (分娩推送, Biotech)
│   └── 【大量XML配置的疾病/Buff/Debuff直接使用此类】
├── Hediff_Scaria (狂暴病)
├── Hediff_BloodRage (血怒, Anomaly)
├── Hediff_CubeInterest (方块兴趣, Anomaly)
├── Hediff_CubeWithdrawal (方块戒断, Anomaly)
├── Hediff_DarknessExposure (黑暗暴露, Anomaly)
└── Hediff_BandNode (频段节点连接, Biotech)
```

> **继承树设计要点**：注意有6个类（Scaria、BloodRage、CubeInterest、CubeWithdrawal、DarknessExposure、BandNode）直接继承`Hediff`而非`HediffWithComps`。原因是它们不需要组件系统，有自己独立的简单逻辑。模组开发中，除非有特殊理由，**应优先继承`HediffWithComps`**以获得组件扩展能力。

## HediffDef关键分类字段

| 字段 | 默认值 | 作用 |
|------|--------|------|
| `hediffClass` | `typeof(Hediff)` | 使用的C#类 |
| `isBad` | `true` | 是否为负面状态 |
| `chronic` | `false` | 是否为慢性病 |
| `tendable` | `false` | 是否可治疗/护理 |
| `makesSickThought` | `false` | 是否触发"生病"想法 |
| `countsAsAddedPartOrImplant` | `false` | 是否算作植入体 |
| `isInfection` | `false` | 是否为感染 |
| `lethalSeverity` | `-1f` | 致死严重度阈值（-1=不致死） |
| `everCurableByItem` | `true` | 是否可被物品治愈 |
| `pregnant` | `false` | 是否为怀孕相关 |
| `preventsDeath` | `false` | 是否阻止死亡 |

## XML抽象父模板速查

| 模板名 | hediffClass | 用途 | 来源 |
|--------|-------------|------|------|
| `InjuryBase` | `Hediff_Injury` | 物理伤害 | Core |
| `DiseaseBase` | `HediffWithComps` | 一般疾病/状态 | Core |
| `ChronicDiseaseBase` | `HediffWithComps` | 慢性病（`chronic=true`） | Core |
| `AddictionBase` | `Hediff_Addiction` | 药物成瘾 | Core |
| `ImplantHediffBase` | `Hediff_Implant` | 植入体 | Core |
| `RoleStatBuff` | `HediffWithComps` | 意识形态角色增益 | Ideology |
| `OrganDecayBase` | `HediffWithComps` | 器官衰退 | Anomaly |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-10 | 从3.1笔记第1章第1点抽取为独立知识文档 | Claude Opus 4.6 |
