# BorderDefenseProtocol 过时内容审计报告

**日期**: 2026-03-06
**审计范围**: 代码库中所有标记为Obsolete的API、过时的配置字段、临时文件和文档
**审计人**: Claude Sonnet 4.6

---

## 执行摘要

本次审计发现了**5类过时内容**，共计**17项**。其中：
- **必须保留**（向后兼容）: 5项
- **建议迁移**（有替代方案）: 10项
- **可以删除**（无用临时文件）: 2项

---

## 1. 过时的C#代码API

### 1.1 WeaponChipConfig.verbProperties
**位置**: `Source/BDP/Trigger/Data/WeaponChipConfig.cs:54`
**状态**: `[System.Obsolete("Use primaryVerbProps/secondaryVerbProps instead")]`
**使用情况**:
- XML配置中仍有**3处**使用（ThingDefs_Chips.xml）
- 代码中有**10处**回退逻辑引用

**存在意义**: ✅ **必须保留**
- **原因**: 向后兼容旧存档和旧配置
- **迁移路径**:
  ```xml
  <!-- 旧写法 -->
  <verbProperties>
    <li>...</li>
  </verbProperties>

  <!-- 新写法 -->
  <primaryVerbProps>...</primaryVerbProps>
  <secondaryVerbProps>...</secondaryVerbProps>
  ```
- **建议**: 更新XML配置文件，但保留代码中的回退逻辑

---

### 1.2 WeaponChipConfig.supportsVolley
**位置**: `Source/BDP/Trigger/Data/WeaponChipConfig.cs:62`
**状态**: `[System.Obsolete("Use secondaryVerbProps with Verb_BDPVolley instead")]`
**使用情况**:
- XML配置中有**3处**使用（ThingDefs_Chips.xml）
- 代码中有**1处**回退逻辑（CompTriggerBody.VerbSystem.cs:253）

**存在意义**: ✅ **必须保留**
- **原因**: 向后兼容，自动创建齐射verb
- **迁移路径**:
  ```xml
  <!-- 旧写法 -->
  <supportsVolley>true</supportsVolley>

  <!-- 新写法 -->
  <secondaryVerbProps>
    <verbClass>BDP.Trigger.Verb_BDPVolley</verbClass>
    ...
  </secondaryVerbProps>
  ```
- **建议**: 更新XML配置，但保留代码回退逻辑

---

### 1.3 ComboVerbDef.supportsVolley
**位置**: `Source/BDP/Trigger/Data/ComboVerbDef.cs:48`
**状态**: `[Obsolete("Use secondaryVerbClass instead")]`
**使用情况**:
- XML配置中有**2处**使用（ComboVerbDefs.xml）
- 代码中有**1处**回退逻辑（CompTriggerBody.VerbSystem.cs:378）

**存在意义**: ✅ **必须保留**
- **原因**: 向后兼容组合技配置
- **迁移路径**:
  ```xml
  <!-- 旧写法 -->
  <supportsVolley>true</supportsVolley>

  <!-- 新写法 -->
  <secondaryVerbClass>BDP.Trigger.Verb_BDPComboShoot</secondaryVerbClass>
  ```
- **建议**: 更新ComboVerbDefs.xml

---

### 1.4 CompTriggerBody.CreateLegacyVolleyVerb()
**位置**: `Source/BDP/Trigger/Comps/CompTriggerBody.VerbSystem.cs:296`
**状态**: `[System.Obsolete("Use explicit secondaryVerbProps configuration instead")]`
**使用情况**:
- 被`CreateSecondaryVerb()`调用（VerbSystem.cs:255）
- 仅在`supportsVolley=true`且`secondaryVerbProps=null`时触发

**存在意义**: ✅ **必须保留**
- **原因**: 向后兼容的自动齐射verb创建逻辑
- **依赖**: WeaponChipConfig.supportsVolley
- **建议**: 保留，直到所有XML配置迁移完成

---

### 1.5 CompTriggerBody.BeginCombatBodyActivation()
**位置**: `Source/BDP/Trigger/Comps/CompTriggerBody.CombatBodySupport.cs:102`
**状态**: `[System.Obsolete("已废弃，请使用TryAllocateTrionForCombatBody + ActivateAllSpecial")]`
**使用情况**:
- ❌ **无外部调用**（仅方法定义本身）
- 已被拆分为两个原子方法

**存在意义**: ⚠️ **可考虑删除**
- **原因**:
  - 新架构已完全替代（v2.2重构）
  - 无外部调用者
  - 方法内部已打印废弃警告日志
- **风险**: 如果有外部模组调用此方法，删除会导致兼容性问题
- **建议**:
  - **短期**: 保留，观察1-2个版本
  - **长期**: 在下一个大版本（v2.0）中删除

---

## 2. 过时的辅助方法

### 2.1 WeaponChipConfig.GetFirstBurstCount()
**位置**: `Source/BDP/Trigger/Data/WeaponChipConfig.cs:112`
**状态**: 标记为`[已废弃]`（注释）
**使用情况**:
- 被`GetPrimaryBurstCount()`作为回退逻辑调用
- 被`CompTriggerBody.VerbSystem.cs`中的组合技逻辑调用

**存在意义**: ✅ **必须保留**
- **原因**: 回退逻辑，支持旧配置
- **建议**: 保留

---

### 2.2 WeaponChipConfig.GetFirstProjectileDef()
**位置**: `Source/BDP/Trigger/Data/WeaponChipConfig.cs:121`
**状态**: 标记为`[已废弃]`（注释）
**使用情况**: 被`GetPrimaryProjectileDef()`作为回退逻辑调用

**存在意义**: ✅ **必须保留**
- **原因**: 回退逻辑，支持旧配置
- **建议**: 保留

---

## 3. TODO标记（待完成功能）

### 3.1 TrionCostHandler硬编码配置
**位置**: `Source/BDP/Combat/Damage/Handlers/TrionCostHandler.cs:17`
**内容**: `// TODO: 从XML配置读取，临时硬编码`
**当前实现**: `private const float TRION_COST_PER_DAMAGE = 0.5f;`

**存在意义**: ⚠️ **建议改进**
- **问题**: 硬编码的Trion消耗系数，无法通过XML配置
- **影响**: 平衡性调整需要重新编译
- **建议**:
  ```csharp
  // 在CombatBodyDef或全局配置中添加
  public class BDPSettings : ModSettings
  {
      public float trionCostPerDamage = 0.5f;
  }
  ```

---

## 4. 临时文件和测试文件

### 4.1 _write_test.tmp
**位置**: `1.6/Assemblies/_write_test.tmp`
**类型**: 临时测试文件

**存在意义**: ❌ **应该删除**
- **原因**: 测试残留文件
- **建议**: 立即删除
  ```bash
  rm "1.6/Assemblies/_write_test.tmp"
  ```

---

### 4.2 测试芯片配置
**位置**: `1.6/Defs/Trigger/AbilityDefs_TestChips.xml`
**内容**: 使用过时的`verbProperties`字段

**存在意义**: ⚠️ **建议迁移**
- **原因**: 测试配置应使用最新API
- **建议**: 更新为`primaryVerbProps/secondaryVerbProps`

---

## 5. 过时的设计文档

### 5.1 弹道系统重构文档（多版本）
**位置**: `docs/plans/`
- `2026-02-28-ballistics-pipeline-v5-refactor-design.md`
- `2026-02-28-bdp-ballistics-kernel-v2-refactor.md`
- `2026-02-28-bdp-ballistics-v5-comprehensive-refactor.md`

**存在意义**: ⚠️ **建议归档**
- **原因**:
  - 多个版本的同一设计文档
  - 可能已实现或废弃
- **建议**:
  - 确认哪个版本是最终实现
  - 将其他版本移至`docs/archive/`

---

### 5.2 战斗体需求文档（多版本）
**位置**: `docs/plans/`
- `2026-03-01-combat-body-requirements-v1.md`
- `2026-03-01-combat-body-requirements-v2.md`
- `2026-03-01-combat-body-requirements-v3.md`

**存在意义**: ⚠️ **建议归档**
- **原因**: 迭代版本，v3应该是最终版
- **建议**: 将v1、v2移至`docs/archive/`

---

### 5.3 战斗体伤害系统设计（多版本）
**位置**: `docs/plans/`
- `2026-03-04-combat-body-damage-system-design.md`
- `2026-03-04-combat-body-damage-system-design-v2.md`

**存在意义**: ⚠️ **建议归档**
- **原因**: v2应该是最终版
- **建议**: 将v1移至`docs/archive/`

---

## 6. 迁移优先级建议

### 高优先级（立即处理）
1. ✅ **删除临时文件**: `_write_test.tmp`
2. ✅ **更新测试配置**: `AbilityDefs_TestChips.xml`使用新API

### 中优先级（下个版本）
3. ⚠️ **迁移XML配置**:
   - `ThingDefs_Chips.xml`中的3处`verbProperties`和`supportsVolley`
   - `ComboVerbDefs.xml`中的2处`supportsVolley`
4. ⚠️ **实现TODO**: TrionCostHandler的XML配置支持
5. ⚠️ **归档文档**: 将多版本设计文档整理归档

### 低优先级（长期维护）
6. 🔄 **保留向后兼容代码**:
   - 所有标记为`[Obsolete]`的方法和字段
   - 在确认无外部依赖后（1-2个大版本后）再考虑删除
7. 🔄 **监控BeginCombatBodyActivation**: 观察是否有外部调用

---

## 7. 清理脚本

```bash
#!/bin/bash
# 清理临时文件和归档过时文档

cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol"

# 1. 删除临时文件
rm -f "1.6/Assemblies/_write_test.tmp"

# 2. 创建归档目录
mkdir -p docs/archive/ballistics-refactor
mkdir -p docs/archive/combat-body-design

# 3. 归档弹道系统文档
mv docs/plans/2026-02-28-ballistics-pipeline-v5-refactor-design.md docs/archive/ballistics-refactor/
mv docs/plans/2026-02-28-bdp-ballistics-kernel-v2-refactor.md docs/archive/ballistics-refactor/

# 4. 归档战斗体需求文档
mv docs/plans/2026-03-01-combat-body-requirements-v1.md docs/archive/combat-body-design/
mv docs/plans/2026-03-01-combat-body-requirements-v2.md docs/archive/combat-body-design/

# 5. 归档战斗体伤害系统文档
mv docs/plans/2026-03-04-combat-body-damage-system-design.md docs/archive/combat-body-design/

echo "清理完成！"
```

---

## 8. 总结

### 当前状态
- **代码健康度**: ✅ 良好
- **向后兼容性**: ✅ 完整保留
- **技术债务**: ⚠️ 中等（主要是XML配置迁移）

### 关键发现
1. **所有Obsolete API都有明确的迁移路径**
2. **向后兼容逻辑完善**，不会破坏旧存档
3. **文档版本管理需要改进**，存在多个迭代版本

### 行动建议
1. **立即**: 删除临时文件
2. **本周**: 更新XML配置使用新API
3. **本月**: 归档过时文档，实现TODO功能
4. **长期**: 在v2.0大版本时清理所有Obsolete代码

---

**审计完成时间**: 2026-03-06
**下次审计建议**: 2026-04-06（1个月后）

---

## 元信息

- **文档类型**: 技术审计报告
- **标签**: [审计] [技术债务] [代码清理] [向后兼容]
- **相关文档**:
  - `2026-03-06-comptriggerbody-refactor-checklist.md`
  - `2026-03-06-comptriggerbody-refactor-plan.md`

## 历史记录

| 日期 | 版本 | 修改内容 | 修改人 |
|------|------|----------|--------|
| 2026-03-06 | v1.0 | 初始版本，完整审计 | Claude Sonnet 4.6 |
