# CompTriggerBody完整重构计划

## 背景与目标

### 为什么需要重构

CompTriggerBody是BDP模组触发器系统的核心组件，当前存在严重的架构问题：

- **代码规模过大**: 主文件2059行，总计2215行（含Debug.cs）
- **职责混杂**: 承担10个不同职责（槽位管理、Verb构建、UI生成、生命周期等）
- **可维护性差**: 修改一个功能需要在巨大文件中定位，认知负担极高
- **测试困难**: 无法为单个职责编写独立测试
- **持续膨胀**: 从v1.0到v12.2，代码量增长14%，新增组合技、副攻击、手部联动等功能

### 重构目标

通过partial class拆分，将CompTriggerBody按职责域分离为多个文件：

- **主文件**: 300行，仅保留类声明、接口实现、公开API
- **功能文件**: 8个partial class文件，每个150-600行，职责单一
- **可维护性**: 单文件行数减少66-93%，职责清晰
- **可测试性**: 各功能模块可独立测试
- **可扩展性**: 新增功能时只需修改对应文件

### 核心约束

1. **存档兼容性**: 所有序列化字段必须保持位置和名称不变
2. **接口实现**: IVerbOwner和ICombatBodySupport必须在主类实现
3. **RimWorld生命周期**: PostSpawnSetup等钩子必须正确触发
4. **Verb系统集成**: 芯片Verb脱离VerbTracker的架构必须保持

---

## 文件拆分方案

### 拆分后的文件结构

```
CompTriggerBody/
├── CompTriggerBody.cs                    (主文件，300行)
├── CompTriggerBody.Fields.cs             (字段声明，150行)
├── CompTriggerBody.Lifecycle.cs          (生命周期钩子，200行)
├── CompTriggerBody.SlotManagement.cs     (槽位管理，300行)
├── CompTriggerBody.Activation.cs         (激活/关闭逻辑，400行)
├── CompTriggerBody.VerbSystem.cs         (Verb构建系统，600行)
├── CompTriggerBody.SwitchStateMachine.cs (切换状态机，250行)
├── CompTriggerBody.GizmoGeneration.cs    (Gizmo生成，200行)
├── CompTriggerBody.CombatBodySupport.cs  (战斗体集成，150行)
├── CompTriggerBody.HandDestruction.cs    (手部缺失联动，150行)
└── CompTriggerBody.Debug.cs              (调试工具，156行，已存在)
```

### 各文件职责说明

#### CompTriggerBody.cs (主文件)
- 类声明和XML文档
- IVerbOwner接口实现（委托到VerbSystem）
- ICombatBodySupport接口实现（委托到CombatBodySupport）
- 公开属性（LeftHandSlots、RightHandSlots等）
- 核心公开方法签名

#### CompTriggerBody.Fields.cs
- 所有字段的集中声明
- 槽位数据字段（leftHandSlots、rightHandSlots、specialSlots）
- 切换状态机字段（leftSwitchCtx、rightSwitchCtx）
- Verb缓存字段（9个Verb引用）
- 按侧Verb存储字段（leftHandActiveVerbProps等）
- 序列化字段（savedChipVerbs）

#### CompTriggerBody.Lifecycle.cs
- PostSpawnSetup（初始化和读档恢复）
- Notify_Equipped / Notify_Unequipped
- PostDestroy
- PostExposeData（存档序列化）

#### CompTriggerBody.SlotManagement.cs
- GetSlot、GetSlotsForSide、AllSlots、AllActiveSlots
- LoadChip、UnloadChip、LoadChipInternal
- SetSideVerbs、ClearSideVerbs、GetChipSide
- CalculateTotalAllocationCost

#### CompTriggerBody.Activation.cs
- CanActivateChip（5项前置检查）
- ActivateChip、DeactivateChip、DeactivateAll
- DoActivate（激活成本、持续消耗、effect.Activate）
- DeactivateSlot（注销消耗、effect.Deactivate）
- ActivateAllSpecial、DeactivateAllSpecial
- TryGrantComboAbility、TryRevokeComboAbilities

#### CompTriggerBody.VerbSystem.cs
- RebuildVerbs（VerbTracker重建入口）
- CreateAndCacheChipVerbs（手动创建芯片Verb）
- CreateSecondaryVerbs、CreateComboVerbs
- FindOrCreateVerb、CreateLegacyVolleyVerb
- 组合技匹配逻辑（MatchComboVerb）
- 组合技参数读取辅助（GetFirstRange等）
- GetVerbProperties、GetTools（IVerbOwner接口实现）

#### CompTriggerBody.SwitchStateMachine.cs
- IsSideSwitching、GetSwitchPhase、GetSideSwitchProgress
- TryResolveSideSwitch（懒求值状态结算）
- SetSideCtx
- SwitchContext类定义
- SwitchPhase枚举定义

#### CompTriggerBody.GizmoGeneration.cs
- CompGetEquippedGizmosExtra（装备时Gizmo）
- 芯片攻击Gizmo生成（左手、右手、双手、组合技）
- 射击模式Gizmo生成
- 状态Gizmo生成

#### CompTriggerBody.CombatBodySupport.cs
- TryAllocateTrionForCombatBody
- ReleaseTrionFromCombatBody
- IsCombatBodyActive属性
- ICombatBodySupport接口实现

#### CompTriggerBody.HandDestruction.cs
- IsSideDisabled
- OnHandDestroyed
- ForceDeactivateLeftSlots、ForceDeactivateRightSlots
- OnPartDestroyed（静态事件订阅）

---

## 实施步骤（3阶段）

### 阶段1: 字段和生命周期分离（低风险）

**目标**: 提取字段声明和生命周期钩子，不改变任何逻辑

**步骤**:
1. 创建`CompTriggerBody.Fields.cs`
   - 复制所有字段声明（约20个字段）
   - 保留XML注释
   - 从主文件删除字段声明

2. 创建`CompTriggerBody.Lifecycle.cs`
   - 移动PostSpawnSetup、Notify_Equipped、Notify_Unequipped
   - 移动PostDestroy、PostExposeData
   - 移动InitializeSlots、RestoreActivationState等辅助方法

3. 主文件保留
   - 类声明和XML文档
   - 接口实现
   - 公开属性和方法签名

4. 编译验证

**验证方法**:
- 编译通过
- 读档测试：加载旧存档，检查所有字段正确反序列化
- 装备测试：装备触发体，检查PostSpawnSetup和Notify_Equipped正确触发
- 存档测试：保存新存档，检查XML结构与旧存档一致

**预计时间**: 1小时实施 + 30分钟验证

---

### 阶段2: 功能模块分离（中风险）

**目标**: 按职责拆分功能模块

**步骤**:
1. 创建`CompTriggerBody.SlotManagement.cs`
   - 移动槽位查询方法（GetSlot、AllSlots等）
   - 移动装载/卸载方法（LoadChip、UnloadChip）
   - 移动按侧Verb管理（SetSideVerbs、ClearSideVerbs）

2. 创建`CompTriggerBody.Activation.cs`
   - 移动CanActivateChip（5项检查）
   - 移动ActivateChip、DeactivateChip、DeactivateAll
   - 移动DoActivate、DeactivateSlot
   - 移动ActivateAllSpecial、DeactivateAllSpecial
   - 移动组合能力授予/撤销逻辑

3. 创建`CompTriggerBody.SwitchStateMachine.cs`
   - 移动切换状态查询（IsSideSwitching、GetSwitchPhase）
   - 移动TryResolveSideSwitch
   - 移动SetSideCtx
   - 移动SwitchContext类定义

4. 创建`CompTriggerBody.HandDestruction.cs`
   - 移动IsSideDisabled
   - 移动OnHandDestroyed、OnPartDestroyed
   - 移动ForceDeactivateLeftSlots、ForceDeactivateRightSlots

5. 每移动一个文件后编译验证

**验证方法**:
- 编译通过
- 功能测试：
  - 装载/卸载芯片（左手、右手、特殊槽）
  - 激活/关闭芯片
  - 切换芯片（检查前摇/后摇进度）
  - 手部破坏响应（移除手部，检查槽位禁用）
- 存档兼容性测试

**预计时间**: 2小时实施 + 1小时验证

---

### 阶段3: Verb系统和UI分离（高风险）

**目标**: 拆分最复杂的Verb构建系统和Gizmo生成

**步骤**:
1. 创建`CompTriggerBody.VerbSystem.cs`
   - 移动RebuildVerbs（主入口）
   - 移动CreateAndCacheChipVerbs（芯片Verb创建）
   - 移动CreateSecondaryVerbs、CreateComboVerbs
   - 移动FindOrCreateVerb、CreateLegacyVolleyVerb
   - 移动CreateComboVerb、CreateComboVerbFromClass
   - 移动组合技匹配逻辑（MatchComboVerb）
   - 移动辅助方法（GetFirstRange、GetFirstWarmup等）
   - 移动GetVerbProperties、GetTools（IVerbOwner接口实现）

2. 创建`CompTriggerBody.GizmoGeneration.cs`
   - 移动CompGetEquippedGizmosExtra
   - 移动芯片攻击Gizmo生成逻辑
   - 移动射击模式Gizmo生成
   - 移动状态Gizmo生成

3. 创建`CompTriggerBody.CombatBodySupport.cs`
   - 移动TryAllocateTrionForCombatBody
   - 移动ReleaseTrionFromCombatBody
   - 移动IsCombatBodyActive属性
   - 移动ICombatBodySupport接口实现

4. 主文件保留接口实现（委托到各partial类）

5. 编译验证

**验证方法**:
- 编译通过
- Verb测试：
  - 激活芯片后检查Verb正确创建
  - 检查左手、右手、双手、组合技Verb
  - 检查副攻击Verb（右键）
  - 检查Verb参数（射程、预热、冷却）
- Gizmo测试：
  - 征召后检查攻击按钮显示
  - 检查射击模式按钮
  - 检查状态Gizmo（切换进度）
- 战斗测试：
  - 使用各种Verb攻击敌人
  - 检查组合技触发
  - 检查战斗体激活/解除
- 存档兼容性测试

**预计时间**: 2小时实施 + 1.5小时验证

---

## 关键文件路径

### 主要修改文件
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.cs`（主文件，重构）

### 新建文件
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.Fields.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.Lifecycle.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.SlotManagement.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.Activation.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.VerbSystem.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.SwitchStateMachine.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.GizmoGeneration.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.CombatBodySupport.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.HandDestruction.cs`

### 相关依赖文件（需要了解，不修改）
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Data/ChipSlot.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Data/SwitchContext.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Core/Comps/CompTrion.cs`
- `模组工程/BorderDefenseProtocol/Source/BDP/Core/Interfaces/ICombatBodySupport.cs`

---

## 风险控制

### 存档兼容性保证

**策略**:
1. **字段位置不变**: 所有序列化字段保留在Fields.cs，ExposeData逻辑不变
2. **字段名不变**: 不重命名任何序列化字段
3. **序列化顺序不变**: ExposeData中的Scribe调用顺序保持一致

**测试流程**:
1. 重构前保存存档A（包含触发体装备、芯片装载、激活状态）
2. 重构后加载存档A
3. 检查所有字段值正确（槽位、芯片、激活状态、切换状态）
4. 保存存档B
5. 对比存档A和B的XML结构（应完全一致）

### 功能完整性验证

**测试清单**:
- [ ] 装备触发体到Pawn
- [ ] 卸下触发体
- [ ] 装载芯片（左手、右手、特殊槽）
- [ ] 卸载芯片
- [ ] 激活芯片（单手、双手、特殊槽）
- [ ] 关闭芯片
- [ ] 切换芯片（检查前摇/后摇进度显示）
- [ ] 使用左手攻击
- [ ] 使用右手攻击
- [ ] 使用双手触发
- [ ] 使用组合技
- [ ] 使用副攻击（右键）
- [ ] 射击模式切换
- [ ] 手部破坏响应（移除手部，检查槽位禁用）
- [ ] 战斗体激活/解除
- [ ] 存档/读档（多次循环）

### 回滚策略

**每个阶段的回滚**:
- **阶段1失败**: 删除Fields.cs和Lifecycle.cs，恢复主文件
- **阶段2失败**: 删除所有新文件，恢复主文件
- **阶段3失败**: 删除所有新文件，恢复主文件

**回滚触发条件**:
- 编译失败
- 存档加载失败
- 核心功能测试失败（激活/攻击/存档）
- 出现空引用异常或其他运行时错误

---

## 代码组织原则

### 字段声明位置
- **规则**: 所有字段集中在`CompTriggerBody.Fields.cs`
- **原因**: 便于查找和维护，避免字段分散

### 属性定义位置
- **公开属性**: 在主文件（CompTriggerBody.cs）
- **内部属性**: 在对应功能文件

### 方法实现位置
- **公开方法**: 签名在主文件，实现在对应功能文件
- **私有方法**: 在对应功能文件

### 避免循环依赖
- **单向依赖**: 主文件 → 功能文件，功能文件之间不直接依赖
- **共享字段**: 通过Fields.cs共享数据，避免方法间传参
- **调用链控制**: 主文件公开方法 → 功能文件private方法

---

## 验证方法

### 编译验证
```bash
cd "模组工程/BorderDefenseProtocol/Source/BDP"
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

### 游戏内测试流程

1. **基础功能测试**（每个阶段后执行）
   - 启动游戏，加载测试存档
   - 装备触发体到Pawn
   - 装载芯片到左手、右手、特殊槽
   - 激活芯片，检查Gizmo显示
   - 使用各种攻击Verb
   - 保存存档，退出游戏
   - 重新加载存档，检查状态恢复

2. **存档兼容性测试**（阶段3后执行）
   - 使用重构前的存档
   - 加载存档，检查所有字段正确
   - 执行完整功能测试
   - 保存新存档
   - 对比新旧存档XML结构

3. **压力测试**（阶段3后执行）
   - 多次切换芯片（检查状态机稳定性）
   - 快速激活/关闭芯片（检查Trion消耗）
   - 手部破坏后激活芯片（检查禁用逻辑）
   - 战斗体激活/解除循环（检查特殊槽）

---

## 预期成果

### 代码质量提升

| 指标 | 重构前 | 重构后 | 提升 |
|------|--------|--------|------|
| 主文件行数 | 2059行 | 300行 | -85% |
| 单文件最大行数 | 2059行 | 600行 | -71% |
| 单类职责数 | 10个 | 1-2个 | -80% |
| 方法数/文件 | 70个 | 5-15个 | -79% |

### 可维护性提升
- 修改单个功能时只需关注对应文件（150-600行）
- 新增功能时只需创建新的partial class文件
- 代码审查时可以按文件分配审查任务

### 可测试性提升
- 各功能模块可独立测试
- 可以为单个职责编写单元测试
- 测试代码与实现代码分离

---

## 总结

本次重构将CompTriggerBody从2059行的巨型类拆分为10个职责单一的partial class文件，主文件仅保留300行。通过3个阶段的渐进式重构，在保证存档兼容性和功能完整性的前提下，显著提升代码的可维护性、可测试性和可扩展性。

**预计总时间**: 5小时实施 + 3小时验证 = 8小时

**风险等级**: 中等（通过分阶段实施和充分测试降低风险）

**收益**: 代码质量提升66-85%，长期维护成本大幅降低
