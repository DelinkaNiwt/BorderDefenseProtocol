# BDP模组XML配置统一架构重构计划

## 元信息
- **创建日期**: 2026-03-08
- **任务类型**: 架构重构
- **优先级**: 高
- **预计工作量**: 6-9小时
- **标签**: [架构], [重构], [XML配置], [统一设计]

---

## 一、重构目标

### 核心目标
**统一所有芯片的攻击行为定义方式**：所有芯片（远程/近战）都必须通过`primaryVerbProps`定义攻击行为。

### 具体目标
1. **消除架构不一致**：移除近战芯片的fallback路径（自动合成VerbProperties）
2. **提高配置可读性**：按功能域分组配置字段（cost, melee, ranged）
3. **简化维护成本**：统一的配置结构，易于理解和扩展

### 非目标
- ❌ 不保留向后兼容性（彻底重构）
- ❌ 不需要过渡设计（直接切换到新架构）
- ❌ 不需要迁移工具（手动迁移XML）

---

## 二、架构变更

### 2.1 VerbChipConfig结构变更

**重构前（扁平结构）**：
```csharp
public class VerbChipConfig : DefModExtension
{
    public VerbProperties primaryVerbProps;
    public VerbProperties secondaryVerbProps;
    public int meleeBurstCount = 1;
    public int meleeBurstInterval = 12;
    public List<Tool> tools;
    public float trionCostPerShot = 0f;
    public float volleySpreadRadius = 0f;
    public bool supportsGuided = false;
    public int maxAnchors = 3;
    public float anchorSpread = 0.3f;
    public float passthroughPower = 0f;
}
```

**重构后（分层结构）**：
```csharp
public class VerbChipConfig : DefModExtension
{
    // 核心Verb配置（保持不变）
    public VerbProperties primaryVerbProps;
    public VerbProperties secondaryVerbProps;

    // 成本配置（分组）
    public CostConfig cost;

    // 近战配置（分组）
    public MeleeConfig melee;

    // 远程配置（分组）
    public RangedConfig ranged;
}

public class CostConfig
{
    public float trionPerShot = 0f;
}

public class MeleeConfig
{
    public List<Tool> tools;
}

public class RangedConfig
{
    public float volleySpreadRadius = 0f;
    public GuidedConfig guided;
    public float passthroughPower = 0f;
}

public class GuidedConfig
{
    public int maxAnchors = 3;
    public float anchorSpread = 0.3f;
}
```

### 2.2 关键变更点

| 变更项 | 旧设计 | 新设计 | 原因 |
|--------|--------|--------|------|
| 近战芯片定义 | 可以不提供primaryVerbProps | 必须提供primaryVerbProps | 统一架构 |
| burst参数位置 | meleeBurstCount/meleeBurstInterval | primaryVerbProps.burstShotCount/ticksBetweenBurstShots | 符合RimWorld标准 |
| tools位置 | 顶层字段 | melee.tools | 功能分组 |
| 引导判断 | supportsGuided布尔值 | guided对象存在性 | 更优雅 |
| 成本配置 | trionCostPerShot | cost.trionPerShot | 功能分组 |

---

## 三、实施计划

### 阶段1：创建子配置类（1-2小时）

**目标**：创建4个新配置类，修改VerbChipConfig添加子配置字段

**新增文件**：
```
Source/BDP/Trigger/Data/
├── CostConfig.cs       (成本配置)
├── MeleeConfig.cs      (近战配置)
├── RangedConfig.cs     (远程配置)
└── GuidedConfig.cs     (引导配置)
```

**修改文件**：
- `VerbChipConfig.cs` - 添加子配置字段，保留旧字段但标记为Obsolete

**验证步骤**：
1. 编译测试通过
2. 确认新配置类可以正常序列化/反序列化

**提交信息**：
```
refactor(config): add sub-config classes for VerbChipConfig

- Add CostConfig, MeleeConfig, RangedConfig, GuidedConfig
- Prepare for unified architecture refactoring
- Old fields marked as Obsolete but still functional
```

---

### 阶段2：修改代码逻辑（3-4小时）

**目标**：修改VerbChipEffect和相关Verb类，移除fallback逻辑

**修改文件**：
1. `VerbChipEffect.cs`
   - 移除`SynthesizeMeleeVerbProps`方法
   - 移除fallback逻辑（第37-38行）
   - 添加验证：所有芯片必须提供primaryVerbProps

2. `Verb_BDPVolley.cs`
   - 修改Trion消耗读取：`cfg.trionCostPerShot` → `cfg.cost?.trionPerShot ?? 0f`
   - 修改散布半径读取：`cfg.volleySpreadRadius` → `cfg.ranged?.volleySpreadRadius ?? 0f`

3. `Verb_BDPRangedBase.cs`
   - 修改引导参数读取：`cfg.supportsGuided` → `cfg.ranged?.guided != null`
   - 修改锚点参数读取：`cfg.maxAnchors` → `cfg.ranged?.guided?.maxAnchors ?? 3`

4. `GuidedModule.cs`
   - 修改引导配置读取路径

5. `Bullet_BDP.cs`
   - 修改投射物创建时的参数读取

**验证步骤**：
1. 编译测试通过
2. 创建测试XML验证新配置格式
3. 游戏内测试：激活芯片，确认攻击行为正常

**提交信息**：
```
refactor(config): migrate to unified architecture

- Remove SynthesizeMeleeVerbProps fallback logic
- All chips must now provide primaryVerbProps
- Update all config access paths to use sub-configs
- Breaking change: old XML format no longer supported
```

---

### 阶段3：迁移XML定义（2-3小时）

**目标**：迁移所有芯片XML到新格式，移除旧字段

**修改文件**：
- `ThingDefs_Chips.xml` - 迁移所有芯片定义

**迁移示例**：

**旧格式（近战芯片）**：
```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <meleeBurstCount>3</meleeBurstCount>
  <meleeBurstInterval>12</meleeBurstInterval>
  <trionCostPerShot>5</trionCostPerShot>
  <tools>
    <li>
      <label>刀刃</label>
      <capacities><li>Cut</li></capacities>
      <power>15</power>
      <cooldownTime>2.5</cooldownTime>
    </li>
  </tools>
</li>
```

**新格式（近战芯片）**：
```xml
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

  <cost>
    <trionPerShot>5</trionPerShot>
  </cost>

  <melee>
    <tools>
      <li>
        <label>刀刃</label>
        <capacities><li>Cut</li></capacities>
        <power>15</power>
        <cooldownTime>2.5</cooldownTime>
      </li>
    </tools>
  </melee>
</li>
```

**旧格式（远程芯片）**：
```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <volleySpreadRadius>0.3</volleySpreadRadius>
  <trionCostPerShot>5</trionCostPerShot>
  <supportsGuided>true</supportsGuided>
  <maxAnchors>5</maxAnchors>
  <anchorSpread>0.3</anchorSpread>
  <primaryVerbProps>
    <verbClass>BDP.Trigger.Verb_BDPVolley</verbClass>
    <!-- ... -->
  </primaryVerbProps>
</li>
```

**新格式（远程芯片）**：
```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <primaryVerbProps>
    <verbClass>BDP.Trigger.Verb_BDPVolley</verbClass>
    <!-- ... -->
  </primaryVerbProps>

  <cost>
    <trionPerShot>5</trionPerShot>
  </cost>

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
1. 从VerbChipConfig.cs移除所有Obsolete字段
2. 删除未使用的代码

**验证步骤**：
1. 编译测试通过
2. 游戏内全面测试所有芯片
3. 确认战斗日志显示正确

**提交信息**：
```
refactor(xml): migrate all chips to unified architecture

- Migrate all chip definitions to new config structure
- Remove obsolete fields from VerbChipConfig
- All chips now use primaryVerbProps consistently
```

---

## 四、测试清单

### 编译测试
- [ ] 阶段1编译通过
- [ ] 阶段2编译通过
- [ ] 阶段3编译通过

### 功能测试
- [ ] 近战单击芯片正常攻击
- [ ] 近战连击芯片正确执行连击
- [ ] 远程单发芯片正常射击
- [ ] 远程齐射芯片正确发射多发子弹
- [ ] 引导齐射芯片路径正确
- [ ] Trion消耗正确扣除
- [ ] 战斗日志显示正确的芯片名称
- [ ] 双武器系统正常工作

### 回归测试
- [ ] 所有现有芯片功能正常
- [ ] 没有引入新的bug
- [ ] 性能没有明显下降

---

## 五、风险与注意事项

### 高风险项
1. **破坏性变更**：旧XML格式完全不兼容
   - 缓解措施：分阶段实施，每个阶段独立验证

2. **近战芯片配置复杂化**：需要显式提供primaryVerbProps
   - 缓解措施：提供清晰的XML模板和文档

3. **代码访问路径变更**：所有读取配置的代码都需要修改
   - 缓解措施：使用null条件运算符提供默认值

### 中风险项
1. **测试覆盖不足**：可能遗漏某些边缘情况
   - 缓解措施：详细的测试清单，游戏内全面测试

2. **文档更新滞后**：重构后文档可能过时
   - 缓解措施：同步更新所有相关文档

---

## 六、成功标准

### 必须达成
- ✅ 所有芯片都通过primaryVerbProps定义攻击行为
- ✅ 配置结构清晰分层（cost, melee, ranged）
- ✅ 所有现有芯片功能正常
- ✅ 编译无错误无警告

### 期望达成
- ✅ 代码可读性显著提升
- ✅ 配置维护成本降低
- ✅ 架构一致性提升

---

## 七、后续工作

重构完成后需要：
1. 更新芯片设计清单文档
2. 更新XML配置示例
3. 创建配置迁移指南（供其他开发者参考）
4. 考虑是否需要对其他配置类进行类似重构（如BDPTrackingConfig）

---

## 历史记录

| 日期 | 版本 | 变更说明 | 作者 |
|------|------|---------|------|
| 2026-03-08 | v1.0 | 初始版本，基于方案B统一架构设计 | Claude Sonnet 4.6 |

