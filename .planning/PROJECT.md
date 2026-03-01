---
标题：CompTriggerBody 架构减负重构
版本号: v1.0
更新日期: 2026-03-01
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 从 CompTriggerBody 提取 ChipVerbBuilder 和 ComboAbilityManager 辅助类，预留芯片扩展事件，实现单一职责
---

# CompTriggerBody 架构减负重构

## What This Is

对 BDP 模组触发器模块的 CompTriggerBody 类进行架构减负重构。当前 CompTriggerBody 承担了槽位管理、Verb重建、Gizmo生成、组合能力等多重职责（~1587行），违反单一职责原则。本次重构将非核心职责提取为独立辅助类，并为 IChipEffect 预留扩展事件。

## Core Value

CompTriggerBody 只做槽位状态机 + IVerbOwner + 事件分发，其余职责委托给专职辅助类。

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] 提取 ChipVerbBuilder：Verb创建/缓存/齐射/组合技逻辑从 CompTriggerBody 搬出
- [ ] 提取 ComboAbilityManager：组合能力授予/撤销逻辑从 CompTriggerBody 搬出
- [ ] IChipEffect 预留扩展事件（OnKilledPawn 等），带默认空实现
- [ ] CompTriggerBody 瘦身后只保留：槽位状态机、IVerbOwner实现、芯片装卸、激活/关闭事件分发、存档序列化
- [ ] 重构后行为不变：所有现有功能（芯片激活/关闭、Verb重建、齐射、组合技、组合能力、Gizmo）正常工作
- [ ] 存档兼容：读档后状态正确恢复

### Out of Scope

- IChipEffect 接口重写 — 现有接口职责边界清晰，不需要改动
- Gizmo 逻辑提取 — Gizmo 生成依赖 Verb 缓存引用，与 CompTriggerBody 耦合紧密，强行拆分收益不大
- 新增芯片类型 — 本次只做架构重构，不加新功能
- BDPBulletTraitDef（P0）/ Stat接入（P1）/ 互斥标签（P2）— 其他优先级的优化建议，不在本次范围

## Context

- 源自架构评估报告建议 P3：`docs/reports/2026-03-01-vanilla-vs-bdp-architecture-evaluation.md`
- 学习原版 WeaponTraitWorker 的事件分发模式，但不照搬（BDP 的 IChipEffect 已有合理的效果委托机制）
- CompTriggerBody 已有 partial class 拆分先例（Debug 方法在 CompTriggerBody.Debug.cs）
- 关键约束：装备后的武器 CompTick() 不被调用，切换冷却采用懒求值
- C# 7.3 语法限制（RimWorld 1.6）

## Constraints

- **语法**: C# 7.3（RimWorld 1.6.4633 限制）
- **存档兼容**: 重构不可破坏现有存档的读取
- **行为不变**: 纯重构，不改变任何外部可观察行为
- **模块边界**: 提取的辅助类只被 CompTriggerBody 调用，不暴露给外部模块

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 提取辅助类而非新建 IChipEffectWorker 接口 | IChipEffect 管芯片类型行为，"环绕逻辑"（Trion/Verb/Combo）是编排层，不是芯片类型特有的，塞进接口不合适 | — Pending |
| Verb重建逻辑一起抽出 | 占 CompTriggerBody 22%（~350行），是最重的非核心职责 | — Pending |
| IChipEffect 预留 OnKilledPawn 等扩展事件 | 为击杀回复/叠层芯片留接口，带默认空实现不增加现有实现类负担 | — Pending |

---
*Last updated: 2026-03-01 after initialization*

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-01 | 初版：项目初始化 | Claude Opus 4.6 |
