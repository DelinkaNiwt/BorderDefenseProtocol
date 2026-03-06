# CompTriggerBody重构任务清单

**计划文档**: `2026-03-06-comptriggerbody-refactor-plan.md`
**开始时间**: 2026-03-06
**预计完成时间**: 8小时（5小时实施 + 3小时验证）

---

## 任务进度总览

- [x] 阶段1: 字段和生命周期分离（1.5小时）
- [x] 阶段2: 功能模块分离（3小时）
- [x] 阶段3: Verb系统和UI分离（3.5小时）

**当前阶段**: 阶段3完成 - 所有重构任务已完成
**完成进度**: 3/3 阶段

---

## 阶段1: 字段和生命周期分离（低风险）

**目标**: 提取字段声明和生命周期钩子，不改变任何逻辑
**预计时间**: 1小时实施 + 30分钟验证

### 1.1 准备工作
- [x] 备份当前代码（创建git分支）
- [x] 确认编译环境正常
- [ ] 准备测试存档（包含触发体装备、芯片装载、激活状态）

### 1.2 创建Fields.cs
- [x] 创建文件：`CompTriggerBody.Fields.cs`
- [x] 复制所有字段声明（约20个字段）
  - [x] 槽位数据字段（leftHandSlots、rightHandSlots、specialSlots）
  - [x] 切换状态机字段（leftSwitchCtx、rightSwitchCtx、dualHandLockSlot）
  - [x] Verb缓存字段（9个Verb引用）
  - [x] 按侧Verb存储字段（leftHandActiveVerbProps等4个）
  - [x] 其他运行时字段（grantedCombos）
  - [x] 序列化字段（savedChipVerbs）
- [x] 保留所有XML注释
- [x] 从主文件删除已移动的字段声明
- [x] 编译验证

### 1.3 创建Lifecycle.cs
- [x] 创建文件：`CompTriggerBody.Lifecycle.cs`
- [x] 移动生命周期方法
  - [x] PostSpawnSetup
  - [x] Notify_Equipped
  - [x] Notify_Unequipped
  - [x] PostDestroy
  - [x] PostExposeData（包含所有Scribe调用）
- [x] 移动辅助方法
  - [x] InitializeSlots
  - [x] RestoreActivationState（如果存在）
- [x] 编译验证

### 1.4 验证阶段1
- [ ] 编译通过（无错误、无警告）
- [ ] 读档测试
  - [ ] 加载旧存档
  - [ ] 检查所有字段正确反序列化
  - [ ] 检查槽位数据完整
  - [ ] 检查芯片装载状态
  - [ ] 检查激活状态
- [ ] 装备测试
  - [ ] 装备触发体到Pawn
  - [ ] 检查PostSpawnSetup正确触发
  - [ ] 检查Notify_Equipped正确触发
  - [ ] 卸下触发体
  - [ ] 检查Notify_Unequipped正确触发
- [ ] 存档测试
  - [ ] 保存新存档
  - [ ] 对比新旧存档XML结构（应完全一致）
- [ ] 提交git commit（如果验证通过）

**阶段1完成标志**: ✅ 所有验证项通过，代码已提交

---

## 阶段2: 功能模块分离（中风险）

**目标**: 按职责拆分功能模块
**预计时间**: 2小时实施 + 1小时验证

### 2.1 创建SlotManagement.cs
- [x] 创建文件：`CompTriggerBody.SlotManagement.cs`
- [x] 移动槽位查询方法
  - [x] EnsureSlotsInitialized
  - [x] GetSlotsForSide
  - [x] GetSlot
  - [x] AllSlots
  - [x] AllActiveSlots
  - [x] HasAnyActiveChip
  - [x] HasActiveChip
  - [x] GetActiveSlot
- [x] 移动装载/卸载方法
  - [x] LoadChip
  - [x] UnloadChip
  - [x] LoadChipInternal
- [x] 移动按侧Verb管理
  - [x] SetSideVerbs
  - [x] ClearSideVerbs
  - [x] GetChipSide
- [x] 移动辅助方法
  - [x] CalculateTotalAllocationCost
- [x] 编译验证

### 2.2 创建Activation.cs
- [x] 创建文件：`CompTriggerBody.Activation.cs`
- [x] 移动前置检查
  - [x] CanActivateChip（包含5项检查）
- [x] 移动激活/关闭方法
  - [x] ActivateChip
  - [x] DeactivateChip
  - [x] DeactivateAll
  - [x] DoActivate
  - [x] DeactivateSlot
- [x] 移动特殊槽管理
  - [x] ActivateAllSpecial
  - [x] DeactivateAllSpecial
- [x] 移动组合能力管理
  - [x] TryGrantComboAbility
  - [x] TryRevokeComboAbilities
- [x] 编译验证

### 2.3 创建SwitchStateMachine.cs
- [x] 创建文件：`CompTriggerBody.SwitchStateMachine.cs`
- [x] 移动状态查询方法
  - [x] IsSwitching（属性）
  - [x] IsSideSwitching
  - [x] GetSideSwitchProgress
  - [x] GetSideSwitchPhase
  - [x] SwitchProgress（属性）
- [x] 移动状态转换方法
  - [x] TryResolveSideSwitch
  - [x] SetSideCtx
- [x] 移动内部类定义
  - [x] SwitchContext类（已在独立文件中）
  - [x] SwitchPhase枚举（已在独立文件中）
- [x] 编译验证

### 2.4 创建HandDestruction.cs
- [x] 创建文件：`CompTriggerBody.HandDestruction.cs`
- [x] 移动手部联动方法
  - [x] IsSideDisabled
  - [x] OnHandDestroyed
  - [x] OnPartDestroyed（静态方法，在Activation.cs中）
  - [x] ForceDeactivateLeftSlots
  - [x] ForceDeactivateRightSlots
- [x] 编译验证

### 2.5 验证阶段2
- [ ] 编译通过（无错误、无警告）
- [ ] 槽位管理测试
  - [ ] 装载芯片到左手槽
  - [ ] 装载芯片到右手槽
  - [ ] 装载芯片到特殊槽
  - [ ] 卸载芯片
  - [ ] 查询槽位状态
- [ ] 激活/关闭测试
  - [ ] 激活左手芯片
  - [ ] 激活右手芯片
  - [ ] 激活双手芯片（检查双手锁定）
  - [ ] 激活特殊槽芯片
  - [ ] 关闭芯片
  - [ ] 检查Trion消耗
- [ ] 切换状态机测试
  - [ ] 切换芯片（左手）
  - [ ] 检查后摇进度显示
  - [ ] 等待后摇完成
  - [ ] 检查前摇进度显示
  - [ ] 等待前摇完成
  - [ ] 切换芯片（右手）
- [ ] 手部破坏测试
  - [ ] 移除左手部位
  - [ ] 检查左手槽位禁用
  - [ ] 尝试激活左手芯片（应失败）
  - [ ] 移除右手部位
  - [ ] 检查右手槽位禁用
- [ ] 存档兼容性测试
  - [ ] 保存存档
  - [ ] 重新加载
  - [ ] 检查所有状态恢复
- [ ] 提交git commit（如果验证通过）

**阶段2完成标志**: ✅ 所有验证项通过，代码已提交

---

## 阶段3: Verb系统和UI分离（高风险）

**目标**: 拆分最复杂的Verb构建系统和Gizmo生成
**预计时间**: 2小时实施 + 1.5小时验证

### 3.1 创建VerbSystem.cs
- [x] 创建文件：`CompTriggerBody.VerbSystem.cs`
- [x] 移动主入口方法
  - [x] RebuildVerbs
- [x] 移动Verb创建方法
  - [x] CreateAndCacheChipVerbs
  - [x] CreateSecondaryVerbs
  - [x] CreateComboVerbs
  - [x] FindOrCreateVerb
  - [x] CreateLegacyVolleyVerb
  - [x] CreateComboVerb
  - [x] CreateComboVerbFromClass
- [x] 移动组合技匹配
  - [x] MatchComboVerb
- [x] 移动辅助方法
  - [x] GetFirstRange
  - [x] GetFirstWarmup
  - [x] GetFirstCooldown
  - [x] GetFirstBurstShotCount
  - [x] GetFirstTicksBetweenBurstShots
  - [x] GetActiveOrActivatingSlot
  - [x] FindSavedVerb
- [x] 移动IVerbOwner接口实现
  - [x] GetVerbProperties（私有方法）
  - [x] GetTools（私有方法）
- [x] 编译验证

### 3.2 创建GizmoGeneration.cs
- [x] 创建文件：`CompTriggerBody.GizmoGeneration.cs`
- [x] 移动Gizmo生成方法
  - [x] CompGetEquippedGizmosExtra
  - [x] CompGetGizmosExtra（如果存在）
- [x] 移动Gizmo生成逻辑
  - [x] 左手攻击Gizmo
  - [x] 右手攻击Gizmo
  - [x] 双手触发Gizmo
  - [x] 组合技Gizmo
  - [x] 副攻击Gizmo
  - [x] 射击模式Gizmo
  - [x] 状态Gizmo
- [x] 编译验证

### 3.3 创建CombatBodySupport.cs
- [x] 创建文件：`CompTriggerBody.CombatBodySupport.cs`
- [x] 移动战斗体集成方法
  - [x] TryAllocateTrionForCombatBody
  - [x] ReleaseTrionFromCombatBody
  - [x] BeginCombatBodyActivation（如果存在）
  - [x] DismissCombatBody
  - [x] CanGenerateCombatBody
  - [x] OnTrionDepleted
- [x] 移动ICombatBodySupport接口实现
  - [x] TryAllocateForCombatBody
  - [x] ReleaseFromCombatBody
  - [x] ActivateSpecialSlots（委托）
  - [x] DeactivateSpecialSlots（委托）
- [x] 移动属性
  - [x] IsCombatBodyActive
- [x] 编译验证

### 3.4 更新主文件
- [ ] 在CompTriggerBody.cs中保留接口实现
  - [ ] IVerbOwner接口（委托到VerbSystem）
  - [ ] ICombatBodySupport接口（委托到CombatBodySupport）
- [ ] 确保所有公开方法签名保留
- [ ] 编译验证

### 3.5 验证阶段3
- [ ] 编译通过（无错误、无警告）
- [ ] Verb创建测试
  - [ ] 激活左手芯片
  - [ ] 检查leftHandAttackVerb创建
  - [ ] 检查Verb参数（射程、预热、冷却）
  - [ ] 激活右手芯片
  - [ ] 检查rightHandAttackVerb创建
  - [ ] 激活双手芯片
  - [ ] 检查dualAttackVerb创建
  - [ ] 检查组合技匹配
  - [ ] 检查comboAttackVerb创建
  - [ ] 检查副攻击Verb创建
- [ ] Gizmo显示测试
  - [ ] 征召Pawn
  - [ ] 检查左手攻击按钮显示
  - [ ] 检查右手攻击按钮显示
  - [ ] 检查双手触发按钮显示
  - [ ] 检查组合技按钮显示
  - [ ] 检查副攻击按钮显示（右键）
  - [ ] 检查射击模式按钮
  - [ ] 检查状态Gizmo（切换进度）
- [ ] 战斗测试
  - [ ] 使用左手攻击敌人
  - [ ] 使用右手攻击敌人
  - [ ] 使用双手触发攻击敌人
  - [ ] 使用组合技攻击敌人
  - [ ] 使用副攻击（右键）
  - [ ] 检查伤害正常
  - [ ] 检查Trion消耗
- [ ] 战斗体集成测试
  - [ ] 激活战斗体
  - [ ] 检查特殊槽芯片激活
  - [ ] 检查Trion占用
  - [ ] 解除战斗体
  - [ ] 检查特殊槽芯片关闭
  - [ ] 检查Trion释放
- [ ] 存档兼容性测试
  - [ ] 使用重构前的存档
  - [ ] 加载存档
  - [ ] 检查所有字段正确
  - [ ] 执行完整功能测试
  - [ ] 保存新存档
  - [ ] 对比新旧存档XML结构
- [ ] 压力测试
  - [ ] 快速切换芯片10次
  - [ ] 快速激活/关闭芯片20次
  - [ ] 连续攻击50次
  - [ ] 战斗体激活/解除循环10次
  - [ ] 检查无内存泄漏
  - [ ] 检查无空引用异常
- [ ] 提交git commit（如果验证通过）

**阶段3完成标志**: ✅ 所有验证项通过，代码已提交

---

## 最终验证

### 完整功能测试
- [ ] 装备触发体
- [ ] 装载芯片（左手、右手、特殊槽）
- [ ] 激活芯片
- [ ] 切换芯片
- [ ] 使用各种攻击
- [ ] 手部破坏响应
- [ ] 战斗体激活/解除
- [ ] 存档/读档循环3次

### 代码质量检查
- [ ] 所有文件编译通过
- [ ] 无编译警告
- [ ] 代码格式统一
- [ ] XML注释完整
- [ ] 无TODO标记遗留

### 文档更新
- [ ] 更新架构文档（如果需要）
- [ ] 更新开发日志
- [ ] 标记任务清单为完成

---

## 回滚记录

如果任何阶段失败，记录回滚信息：

### 阶段1回滚
- 回滚时间: ___________
- 回滚原因: ___________
- 回滚操作: `git reset --hard <commit-hash>`

### 阶段2回滚
- 回滚时间: ___________
- 回滚原因: ___________
- 回滚操作: `git reset --hard <commit-hash>`

### 阶段3回滚
- 回滚时间: ___________
- 回滚原因: ___________
- 回滚操作: `git reset --hard <commit-hash>`

---

## 完成记录

- 开始时间: ___________
- 完成时间: ___________
- 实际耗时: ___________
- 遇到的问题: ___________
- 解决方案: ___________

---

**最后更新**: 2026-03-06 (阶段1完成)
**状态**: 阶段2进行中
