# Requirements: CompTriggerBody 架构减负重构

**Defined:** 2026-03-01
**Core Value:** CompTriggerBody 只做槽位状态机 + IVerbOwner + 事件分发，其余职责委托给专职辅助类

## v1 Requirements

### Verb管理提取

- [ ] **VERB-01**: ChipVerbBuilder 类承接所有芯片Verb创建逻辑（CreateAndCacheChipVerbs、CreateVolleyVerbs、CreateComboVerbs、CreateSingleComboVerb、CreateSingleVolleyVerb）
- [ ] **VERB-02**: ChipVerbBuilder 承接Verb缓存字段（leftHandAttackVerb、rightHandAttackVerb、dualAttackVerb、leftHandVolleyVerb、rightHandVolleyVerb、dualVolleyVerb、comboAttackVerb、comboVolleyVerb、matchedComboDef）
- [ ] **VERB-03**: ChipVerbBuilder 承接读档Verb复用逻辑（FindSavedVerb、savedChipVerbs）
- [ ] **VERB-04**: ChipVerbBuilder 承接组合技参数辅助方法（GetFirstRange、GetFirstWarmup、GetFirstCooldown、GetFirstTicksBetween、GetFirstSound）
- [ ] **VERB-05**: CompTriggerBody.RebuildVerbs 瘦身为：InitVerbsFromZero + 绑定caster + 调用 builder.Build()
- [ ] **VERB-06**: CompTriggerBody.CompGetEquippedGizmosExtra 通过 builder 属性读取 Verb 缓存
- [ ] **VERB-07**: CompTriggerBody.PostExposeData 通过 builder.CollectSavedVerbs()/SetSavedVerbs() 管理序列化

### 组合能力提取

- [ ] **COMBO-01**: ComboAbilityManager 类承接 TryGrantComboAbility 和 TryRevokeComboAbilities 逻辑
- [ ] **COMBO-02**: ComboAbilityManager 管理 grantedCombos 列表
- [ ] **COMBO-03**: CompTriggerBody.DoActivate 调用 comboMgr.TryGrant()，DeactivateSlot 调用 comboMgr.TryRevoke()

### 扩展事件

- [ ] **EXT-01**: IChipEffect 新增 OnKilledPawn(Pawn pawn, Thing triggerBody, Pawn victim) 方法
- [ ] **EXT-02**: 三个现有实现类（WeaponChipEffect、ShieldChipEffect、UtilityChipEffect）添加空实现
- [ ] **EXT-03**: CompTriggerBody 或 Harmony patch 在击杀时分发 OnKilledPawn 事件给所有激活芯片的 Effect

### 质量保证

- [ ] **QA-01**: 重构后所有现有功能行为不变（芯片激活/关闭、Verb重建、齐射、组合技、组合能力、Gizmo）
- [ ] **QA-02**: 读档后状态正确恢复（savedChipVerbs loadID 格式不变）
- [ ] **QA-03**: 编译通过且无警告

## v2 Requirements

### 进一步减负

- **V2-01**: Gizmo生成逻辑提取到 ChipGizmoBuilder
- **V2-02**: 更多扩展事件（OnDamageDealt、OnDamageTaken）

## Out of Scope

| Feature | Reason |
|---------|--------|
| IChipEffect 接口重写 | 现有接口职责边界清晰，改签名破坏兼容性 |
| IChipEffectWorker 新接口 | 编排逻辑不是芯片类型特有的，用辅助类更合适 |
| BDPBulletTraitDef (P0) | 另一个优化建议，不在本次范围 |
| Stat系统接入 (P1) | 另一个优化建议，不在本次范围 |
| 互斥标签机制 (P2) | 另一个优化建议，不在本次范围 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| VERB-01 | Phase 1 | Pending |
| VERB-02 | Phase 1 | Pending |
| VERB-03 | Phase 1 | Pending |
| VERB-04 | Phase 1 | Pending |
| VERB-05 | Phase 1 | Pending |
| VERB-06 | Phase 1 | Pending |
| VERB-07 | Phase 1 | Pending |
| COMBO-01 | Phase 2 | Pending |
| COMBO-02 | Phase 2 | Pending |
| COMBO-03 | Phase 2 | Pending |
| EXT-01 | Phase 3 | Pending |
| EXT-02 | Phase 3 | Pending |
| EXT-03 | Phase 3 | Pending |
| QA-01 | Phase 3 | Pending |
| QA-02 | Phase 3 | Pending |
| QA-03 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 16 total
- Mapped to phases: 16
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-01*
*Last updated: 2026-03-01 after initial definition*
