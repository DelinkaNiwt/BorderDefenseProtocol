# 新会话任务提示词

请执行以下RimWorld模组重构任务。

---

## 任务概述

**项目**：BDP (Border Defense Protocol) RimWorld模组
**任务**：XML配置统一架构重构
**目标**：统一所有芯片的攻击行为定义方式，消除架构不一致
**约束**：不需要向后兼容，不需要过渡设计，彻底重构

---

## 核心变更

**当前问题**：
- 远程芯片通过`primaryVerbProps`定义攻击行为
- 近战芯片可以不提供`primaryVerbProps`，系统会从`tools`自动合成
- 配置字段扁平化，缺乏功能分组

**目标架构**：
- **所有芯片**（远程/近战）都必须通过`primaryVerbProps`定义攻击行为
- 配置字段按功能域分组：`cost`（成本）、`melee`（近战）、`ranged`（远程）
- 移除自动合成VerbProperties的fallback逻辑

---

## 详细计划

请阅读以下计划文件：
```
C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\docs\plans\2026-03-08-xml-config-unified-architecture-plan.md
```

该计划包含：
- 完整的架构变更说明
- 三阶段实施步骤
- 详细的文件清单
- XML迁移示例
- 测试清单

---

## 实施要求

### 阶段1：创建子配置类（1-2小时）

**创建4个新文件**：
```
Source/BDP/Trigger/Data/CostConfig.cs
Source/BDP/Trigger/Data/MeleeConfig.cs
Source/BDP/Trigger/Data/RangedConfig.cs
Source/BDP/Trigger/Data/GuidedConfig.cs
```

**修改文件**：
- `Source/BDP/Trigger/Data/VerbChipConfig.cs`

**要求**：
- 所有子配置类使用public字段（RimWorld XML序列化要求）
- 提供默认值
- 添加丰富的XML注释

**验证**：编译测试通过

---

### 阶段2：修改代码逻辑（3-4小时）

**修改文件**：
1. `Source/BDP/Trigger/Effects/VerbChipEffect.cs`
   - 移除`SynthesizeMeleeVerbProps`方法
   - 移除fallback逻辑（第37-38行）
   - 添加验证：primaryVerbProps必须存在

2. `Source/BDP/Trigger/DualWeapon/Verb_BDPVolley.cs`
   - 修改配置读取路径：`cfg.trionCostPerShot` → `cfg.cost?.trionPerShot ?? 0f`

3. `Source/BDP/Trigger/DualWeapon/Verb_BDPRangedBase.cs`
   - 修改引导判断：`cfg.supportsGuided` → `cfg.ranged?.guided != null`

4. `Source/BDP/Projectiles/Modules/GuidedModule.cs`
   - 修改引导配置读取路径

5. `Source/BDP/Projectiles/Bullet_BDP.cs`
   - 修改投射物创建时的参数读取

**要求**：
- 使用null条件运算符（`?.`）和null合并运算符（`??`）
- 提供合理的默认值
- 保持代码简洁

**验证**：编译测试通过，创建测试XML验证新格式

---

### 阶段3：迁移XML定义（2-3小时）

**修改文件**：
- `1.6/Defs/Trigger/ThingDefs_Chips.xml`

**迁移规则**：

**近战芯片**：
```xml
<!-- 旧格式 -->
<li Class="BDP.Trigger.VerbChipConfig">
  <meleeBurstCount>3</meleeBurstCount>
  <meleeBurstInterval>12</meleeBurstInterval>
  <trionCostPerShot>5</trionCostPerShot>
  <tools>...</tools>
</li>

<!-- 新格式 -->
<li Class="BDP.Trigger.VerbChipConfig">
  <primaryVerbProps>
    <verbClass>BDP.Trigger.Verb_BDPMelee</verbClass>
    <hasStandardCommand>true</hasStandardCommand>
    <meleeDamageDef>Cut</meleeDamageDef>
    <meleeDamageBaseAmount>15</meleeDamageBaseAmount>
    <defaultCooldownTime>2.5</defaultCooldownTime>
    <burstShotCount>3</burstShotCount>
    <ticksBetweenBurstShots>12</ticksBetweenBurstShots>
  </primaryVerbProps>
  <cost><trionPerShot>5</trionPerShot></cost>
  <melee><tools>...</tools></melee>
</li>
```

**远程芯片**：
```xml
<!-- 旧格式 -->
<li Class="BDP.Trigger.VerbChipConfig">
  <primaryVerbProps>...</primaryVerbProps>
  <trionCostPerShot>5</trionCostPerShot>
  <volleySpreadRadius>0.3</volleySpreadRadius>
  <supportsGuided>true</supportsGuided>
  <maxAnchors>5</maxAnchors>
  <anchorSpread>0.3</anchorSpread>
</li>

<!-- 新格式 -->
<li Class="BDP.Trigger.VerbChipConfig">
  <primaryVerbProps>...</primaryVerbProps>
  <cost><trionPerShot>5</trionPerShot></cost>
  <ranged>
    <volleySpreadRadius>0.3</volleySpreadRadius>
    <guided>
      <maxAnchors>5</maxAnchors>
      <anchorSpread>0.3</anchorSpread>
    </guided>
  </ranged>
</li>
```

**清理工作**：
- 从VerbChipConfig.cs移除所有旧字段
- 删除未使用的代码

**验证**：游戏内全面测试所有芯片

---

## 测试清单

完成后必须验证：
- [ ] 编译无错误无警告
- [ ] 近战单击芯片正常攻击
- [ ] 近战连击芯片正确执行连击
- [ ] 远程单发芯片正常射击
- [ ] 远程齐射芯片正确发射多发子弹
- [ ] 引导齐射芯片路径正确
- [ ] Trion消耗正确扣除
- [ ] 战斗日志显示正确的芯片名称
- [ ] 双武器系统正常工作

---

## Git提交规范

每个阶段完成后提交：

**阶段1**：
```
refactor(config): add sub-config classes for VerbChipConfig

- Add CostConfig, MeleeConfig, RangedConfig, GuidedConfig
- Prepare for unified architecture refactoring
```

**阶段2**：
```
refactor(config): migrate to unified architecture

- Remove SynthesizeMeleeVerbProps fallback logic
- All chips must now provide primaryVerbProps
- Update all config access paths to use sub-configs
- Breaking change: old XML format no longer supported
```

**阶段3**：
```
refactor(xml): migrate all chips to unified architecture

- Migrate all chip definitions to new config structure
- Remove obsolete fields from VerbChipConfig
- All chips now use primaryVerbProps consistently
```

---

## 项目上下文

**项目路径**：`C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol`

**关键文件**：
- 配置类：`Source/BDP/Trigger/Data/VerbChipConfig.cs`
- 效果类：`Source/BDP/Trigger/Effects/VerbChipEffect.cs`
- Verb类：`Source/BDP/Trigger/DualWeapon/Verb_*.cs`
- 模块类：`Source/BDP/Projectiles/Modules/*.cs`
- XML定义：`1.6/Defs/Trigger/ThingDefs_Chips.xml`

**编译命令**：
```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP"
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

**当前分支**：`refactor/projectiles-firemode-independence`

---

## 注意事项

1. **不需要向后兼容**：可以直接删除旧字段，不需要保留fallback
2. **不需要过渡设计**：直接切换到新架构
3. **遵循最简原则**：只做必要的修改，不过度设计
4. **注释丰富**：所有新增代码都要有清晰的注释
5. **中文回答**：所有回复使用简体中文

---

## 开始执行

请按照以下步骤执行：

1. 阅读计划文件，理解完整的重构方案
2. 从阶段1开始，逐步实施
3. 每个阶段完成后编译测试
4. 每个阶段完成后提交代码
5. 全部完成后进行游戏内全面测试

**现在开始阶段1：创建子配置类**

请确认你已理解任务，然后开始执行。
