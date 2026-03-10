---
标题：武器身份代理系统 - 任务路线清单
版本号: v1.0
更新日期: 2026-03-07
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][未完成][未锁定]
摘要: 武器身份代理系统的分阶段任务清单，实时跟踪进度
---

# 武器身份代理系统 - 任务路线清单

**计划文档**: `2026-03-07-weapon-identity-facade-plan.md`
**提示词文档**: `2026-03-07-weapon-identity-facade-prompt.md`
**开始时间**: 待定
**分支**: `feature/weapon-identity-facade`（建议从当前分支新建）

---

## 任务进度总览

- [x] 阶段1: CompTriggerBody.Display.cs — 触发体信息投射（P0 核心）
- [x] 阶段2: 芯片Info Card增强（P1）
- [x] 阶段3: Stat管线桥接（P2）
- [x] 阶段4: 第三方兼容层 + 翻译键（P3）

**当前阶段**: 全部完成（待游戏内验证）
**完成进度**: 4/4 阶段

---

## 阶段1: CompTriggerBody.Display.cs（P0 核心）

**目标**: 让触发体通过ThingComp虚方法投射激活芯片的武器信息
**新建文件**: `Source/BDP/Trigger/Comps/CompTriggerBody.Display.cs`

### 1.0 准备工作
- [x] 确认编译环境正常
- [ ] 从当前分支创建feature分支
- [x] 阅读CompTriggerBody.SlotManagement.cs确认可用API（AllActiveSlots, GetActiveSlot, HasAnyActiveChip）

### 1.1 TransformLabel — 动态标签
- [x] 实现TransformLabel(string label)
  - [x] 无激活芯片 → 返回原label
  - [x] 单侧激活 → `label + " [" + chipLabel + "]"`
  - [x] 双侧同芯片 → `label + " [" + chipLabel + "×2]"`
  - [x] 双侧异芯片 → `label + " [" + leftLabel + "/" + rightLabel + "]"`
- [x] 编译验证

### 1.2 CompInspectStringExtra — 检视面板
- [x] 实现CompInspectStringExtra()
  - [x] 无战斗体激活 → 返回null（或仅显示基础信息）
  - [x] 私有辅助方法AppendSideStatus(sb, side, sideLabel)
    - [x] 读取VerbChipConfig.primaryVerbProps获取武器参数
    - [x] 格式：`左手: 星尘 12dmg×5 射程40`
    - [x] 读取CompFireMode应用倍率（如果芯片有该Comp）
  - [x] 私有辅助方法AppendSpecialStatus(sb)
    - [x] 简要列出激活的特殊槽芯片
- [x] 编译验证

### 1.3 CompTipStringExtra — 悬停提示
- [x] 实现CompTipStringExtra()
  - [x] 无激活芯片 → 返回null
  - [x] 格式：`左手: 星尘 | 右手: 陨石`
  - [x] 跳过Special侧
- [x] 编译验证

### 1.4 SpecialDisplayStats — Info Card武器参数
- [x] 实现SpecialDisplayStats()
  - [x] 当前配置概览条目（ConfigOverviewEntry）
    - [x] Category: BDP_TriggerConfig（暂用StatCategoryDefOf.Weapon_Ranged，阶段4替换）
    - [x] 显示"左手: 芯片A / 右手: 芯片B"或"无激活芯片"
  - [x] 已装载芯片列表条目（LoadedChipsEntry）
    - [x] 列出所有槽位状态（已装载/空/禁用）
  - [x] 每个激活武器芯片的参数条目组（ActiveChipWeaponStats）
    - [x] 遍历AllActiveSlots()，跳过无VerbChipConfig的芯片
    - [x] 带侧别前缀："左手 伤害"/"右手 伤害"
    - [x] 伤害（从projectile.GetDamageAmount读取，含伤害类型名）
    - [x] 射程
    - [x] 预热时间
    - [x] 连射数（burstShotCount > 1时）
    - [x] 连射速率（60/ticksBetweenBurstShots rpm，burstShotCount > 1时）
    - [x] Trion消耗/发（trionCostPerShot > 0时）
    - [x] 齐射散布（volleySpreadRadius > 0时）
    - [x] 引导支持（supportsGuided == true时）
    - [x] 穿透力（passthroughPower > 0时）
  - [x] 近战芯片参数（tools不为空时）
    - [x] 每个Tool的名称+伤害+冷却
  - [x] 非武器芯片的简要信息
    - [x] Hediff芯片：展示hediffDef.LabelCap
    - [x] Ability芯片：展示abilityDef.LabelCap
- [x] 编译验证

### 1.5 私有辅助方法
- [x] GetSideLabel(SlotSide side) — 返回本地化的侧别名称
- [ ] GetChipTypeLabel(Thing chip) — 返回芯片类型标签（武器/防御/能力/被动）（暂未实现，阶段2需要时添加）
- [x] FormatDamageString(VerbProperties vp) — 格式化伤害字符串
- [x] 编译验证

### 1.6 阶段1验证
- [x] 编译通过（无错误、无警告）
- [ ] 游戏内：装备触发体查看标签变化
- [ ] 游戏内：激活芯片查看检视面板
- [ ] 游戏内：悬停查看tooltip
- [ ] 游戏内：点击i按钮查看Info Card武器参数
- [ ] 游戏内：切换芯片查看信息更新
- [ ] 游戏内：停用所有芯片查看信息回退
- [ ] 提交git commit

**阶段1完成标志**: 触发体的原版信息通道全部能展示激活芯片信息

---

## 阶段2: 芯片Info Card增强（P1）

**目标**: 让芯片自身的Info Card按类型展示完整参数
**修改文件**: `Source/BDP/Trigger/Comps/CompProperties_TriggerChip.cs`（或对应的运行时Comp）

### 2.0 准备工作
- [x] 确认CompProperties_TriggerChip的compClass指向哪个运行时类
  - [x] 已有TriggerChipComp类 → 直接在其中添加

### 2.1 芯片基础信息展示
- [x] SpecialDisplayStats中添加基础信息区块
  - [x] 类别标签（categories列表）
  - [x] 激活成本（activationCost > 0时）
  - [x] 占用成本（allocationCost > 0时）
  - [x] 预热时间（activationWarmup > 0时，转换为秒显示）
  - [x] 后摇时长（deactivationDelay > 0时，转换为秒显示）
  - [x] 每日消耗（drainPerDay > 0时）
  - [x] 最低功率（minOutputPower > 0时）
  - [x] 槽位限制（slotRestriction != None时）
  - [x] 双手占用（isDualHand == true时）
  - [x] 互斥标签（exclusionTags不为空时）
- [x] 编译验证

### 2.2 武器芯片专属展示
- [x] 检测VerbChipConfig DefModExtension
  - [x] 伤害（含伤害类型名称）
  - [x] 射程
  - [x] 预热时间
  - [x] 连射数
  - [x] 连射速率（rpm）
  - [x] Trion消耗/发
  - [x] 齐射散布（volleySpreadRadius > 0时）
  - [x] 引导支持（supportsGuided == true时，含最大锚点数）
  - [x] 穿透力（passthroughPower > 0时）
  - [x] 近战Tool列表（tools不为空时）
    - [x] 每个Tool的名称、伤害、冷却
- [x] 编译验证

### 2.3 Hediff/Ability芯片展示
- [x] 检测HediffChipConfig → hediffDef名称+描述
- [x] 检测AbilityChipConfig → abilityDef名称+描述
- [x] 编译验证

### 2.4 阶段2验证
- [x] 编译通过
- [ ] 游戏内：武器芯片Info Card显示完整参数
- [ ] 游戏内：Hediff芯片Info Card显示效果描述
- [ ] 游戏内：Ability芯片Info Card显示技能描述
- [ ] 游戏内：无DefModExtension的芯片只显示基础信息
- [ ] 提交git commit

**阶段2完成标志**: 所有类型的芯片Info Card都能展示其完整功能信息

---

## 阶段3: Stat管线桥接（P2）

**目标**: 让芯片能通过XML声明式地影响装备者属性
**新建文件**: `Source/BDP/Trigger/Data/ChipStatConfig.cs`

### 3.1 新建ChipStatConfig
- [x] 创建DefModExtension类
  - [x] equippedStatOffsets: List<StatModifier>
  - [x] equippedStatFactors: List<StatModifier>
- [x] 编译验证

### 3.2 CompTriggerBody.Display.cs添加stat方法
- [x] GetStatOffset(StatDef stat) — 遍历AllActiveSlots，聚合ChipStatConfig.equippedStatOffsets
- [x] GetStatFactor(StatDef stat) — 遍历AllActiveSlots，聚合ChipStatConfig.equippedStatFactors
- [x] GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace) — 列出各芯片贡献明细
- [x] 编译验证

### 3.3 验证XML声明
- [ ] 为测试芯片添加ChipStatConfig示例（游戏内验证时进行）

### 3.4 阶段3验证
- [x] 编译通过
- [ ] 游戏内：装备触发体 → 激活带stat修正的芯片 → 检查Pawn的stat是否变化
- [ ] 游戏内：stat面板的解释文本是否列出芯片贡献
- [ ] 游戏内：停用芯片 → stat恢复
- [ ] 提交git commit

**阶段3完成标志**: 芯片可以通过XML声明影响装备者属性

---

## 阶段4: 第三方兼容层 + 翻译键（P3）

**目标**: 为第三方模组提供查询接口，完善翻译

### 4.1 公开查询API
- [x] 在CompTriggerBody中添加（Display.cs或主文件）：
  - [x] `GetActiveWeaponVerbProperties()` → List<VerbProperties>（展示/查询用）
  - [x] `GetActiveWeaponDPS()` → float（估算DPS）
  - [x] `GetActiveWeaponRange()` → float（有效射程）
- [x] 编译验证

### 4.2 自定义StatCategory
- [x] 新建 `1.6/Defs/Core/StatCategoryDefs_BDP.xml`
  - [x] BDP_ChipInfo（芯片信息，displayOrder 2050）
  - [x] BDP_TriggerConfig（触发器配置，displayOrder 2060）
- [x] 新建 `Source/BDP/Core/Defs/BDP_StatCategoryDefOf.cs`
  - [x] 对应的DefOf引用
- [x] 回到阶段1/2的SpecialDisplayStats，替换硬编码的StatCategory
- [x] 编译验证

### 4.3 翻译键
- [x] 新建 `1.6/Languages/ChineseSimplified/Keyed/BDP_Display.xml`
  - [x] BDP_LeftHand / BDP_RightHand / BDP_Special
  - [x] BDP_Range / BDP_TrionPerShot / BDP_VolleySpread
  - [x] BDP_GuidedSupport / BDP_PassthroughPower / BDP_Yes / BDP_No
  - [x] BDP_ChipCategory / BDP_ActivationCost / BDP_AllocationCost
  - [x] BDP_NoActiveChips / BDP_CurrentConfig / BDP_LoadedChips
  - [x] BDP_ChipDamageDesc / BDP_GrantedHediff / BDP_GrantedAbility
  - [x] BDP_SlotEmpty / BDP_SlotDisabled
  - [x] StatCategory标签翻译（注：当前代码仍使用硬编码中文，翻译键已创建供后续替换）

### 4.4 阶段4验证
- [x] 编译通过
- [ ] 游戏内：所有文本正确显示中文
- [ ] Info Card的StatCategory标题正确
- [ ] 如有条件：安装第三方DPS模组测试兼容
- [ ] 提交git commit

**阶段4完成标志**: 完整的翻译、自定义StatCategory、公开API

---

## 最终验证

### 完整功能测试
- [ ] 触发体无芯片 → 信息回退到默认
- [ ] 触发体有芯片但未激活 → TransformLabel不变，SpecialDisplayStats显示装载列表
- [ ] 触发体单侧激活 → 全部信息正确
- [ ] 触发体双侧激活 → 全部信息正确
- [ ] 切换芯片 → 信息实时更新
- [ ] 停用所有芯片 → 信息回退
- [ ] 芯片Info Card → 按类型展示
- [ ] Stat修正 → 正确聚合和回退
- [ ] 存档/读档 → 信息恢复正确

### 代码质量检查
- [ ] 所有文件编译通过，无警告
- [ ] 中文注释完整
- [ ] 无硬编码字符串（全部使用Translate()）
- [ ] 无TODO遗留
- [ ] 遵循最简原则

---

## 历史修改记录
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-07 | 初始任务清单 | Claude Opus 4.6 |
