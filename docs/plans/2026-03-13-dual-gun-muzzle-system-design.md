---
标题：BDP双枪枪口发射系统设计
版本号: v1.0
更新日期: 2026-03-13
最后修改者: Claude Sonnet 4.6
标签: [文档][用户已确认][已完成][未锁定]
摘要: BDP模组双武器系统的枪口发射功能设计方案，实现子弹从枪口位置精确发射，在视觉真实性和性能之间取得平衡
---

# BDP双枪枪口发射系统设计

## 1. 设计目标

为BDP模组的双武器系统实现枪口发射功能，使子弹从实际的枪口位置发射，而非小人中心位置。

**核心需求：**
- 视觉真实性：子弹必须从枪口位置发射
- 性能优先：避免每帧复杂计算，只在发射时计算
- 平衡考虑：在视觉效果和性能之间找到平衡点
- 向后兼容：不破坏现有芯片配置

**设计约束：**
- 枪口位置在发射时计算（不是每帧）
- 配置方式：基础配置 + 可选的左手覆盖值
- 每个芯片独立配置，不预设类型分类
- 未配置时回退到小人中心发射

## 2. 架构设计

### 2.1 整体架构

采用"配置驱动 + 运行时计算"的架构模式，在现有的武器绘制系统基础上扩展枪口发射功能。

**核心组件：**

1. **配置层（WeaponDrawChipConfig）**
   - 扩展现有配置类，添加枪口位置相关字段
   - 支持基础配置 + 可选左手覆盖值
   - 完全向后兼容（未配置时自动回退）

2. **计算层（Verb_BDPRangedBase）**
   - 在发射时计算枪口世界坐标
   - 使用四元数旋转进行坐标系转换
   - 优先使用枪口位置，失败时回退到小人中心

3. **数据流：**
   ```
   XML配置 → WeaponDrawChipConfig → GetMuzzlePosition() → LaunchProjectile() → 子弹生成
   ```

**设计原则：**
- 最小侵入：只修改必要的类和方法
- 渐进增强：新功能不影响现有芯片
- 性能优先：只在发射时计算，不影响渲染
- 配置灵活：支持对称和非对称武器

### 2.2 方案选择

**选定方案：最小侵入式扩展**

对比了三种方案后，选择最小侵入式扩展方案：
- 方案A：最小侵入式扩展（✅ 选定）
- 方案B：集中式枪口管理器（架构复杂度高）
- 方案C：预计算配置表（配置量大，不支持连续旋转）

**选择理由：**
1. 符合优先级：在视觉真实性和性能之间取得平衡，改动最小
2. 风险可控：只修改发射逻辑，不影响渲染系统
3. 易于实现：利用现有的WeaponDrawChipConfig和BuildEntry逻辑
4. 完全兼容：未配置时自动回退，不破坏现有芯片
5. 性能合理：发射频率低（每秒几次），四元数旋转开销可忽略

## 3. 配置层设计

### 3.1 WeaponDrawChipConfig 扩展

在现有的`WeaponDrawChipConfig`类中添加以下字段：

```csharp
/// <summary>
/// 枪口相对于武器中心的偏移（武器局部坐标系）。
/// 例如：(0, 0, 0.3) 表示枪口在武器前端0.3米处。
/// </summary>
public Vector3 muzzleOffset = Vector3.zero;

/// <summary>
/// 左手枪口偏移覆盖值（可选）。
/// 如果为null，左手枪口位置通过镜像右手自动推导。
/// 用于非对称武器（如左右手持不同型号的枪）。
/// </summary>
public Vector3? leftMuzzleOffsetOverride = null;

/// <summary>
/// 是否为远程武器（枪械）。
/// true: 子弹从枪口发射
/// false: 子弹从小人中心发射（近战武器、未配置的武器）
/// </summary>
public bool isRangedWeapon = false;
```

### 3.2 配置示例

**对称双手枪：**
```xml
<li Class="BDP.Trigger.WeaponDraw.WeaponDrawChipConfig">
  <isRangedWeapon>true</isRangedWeapon>
  <muzzleOffset>(0, 0, 0.25)</muzzleOffset>
  <!-- leftMuzzleOffsetOverride 不配置，自动镜像 -->
  <graphicData>
    <texPath>Things/Weapons/Pistol</texPath>
    <drawSize>(0.8, 0.8)</drawSize>
  </graphicData>
  <defaultOffset>(-0.2, 0, 0.1)</defaultOffset>
  <defaultAngle>-15</defaultAngle>
  <leftHandAngleOffset>30</leftHandAngleOffset>
</li>
```

**非对称武器（左手枪稍长）：**
```xml
<li Class="BDP.Trigger.WeaponDraw.WeaponDrawChipConfig">
  <isRangedWeapon>true</isRangedWeapon>
  <muzzleOffset>(0, 0, 0.25)</muzzleOffset>
  <leftMuzzleOffsetOverride>(0, 0, 0.30)</leftMuzzleOffsetOverride>
</li>
```

### 3.3 设计要点

- 使用武器局部坐标系，与武器旋转无关
- 默认值为`Vector3.zero`，未配置时自动回退
- `leftMuzzleOffsetOverride`为可空类型，支持智能推导
- `isRangedWeapon`标志明确区分枪械和近战武器

## 4. 计算层设计

### 4.1 枪口位置计算算法

在`Verb_BDPRangedBase`中添加`GetMuzzlePosition()`方法：

```csharp
/// <summary>
/// 计算指定侧别芯片的枪口世界坐标。
/// </summary>
protected Vector3? GetMuzzlePosition(WeaponDrawChipConfig config, SlotSide chipSide, float aimAngle)
{
    // 1. 检查是否为远程武器
    if (config == null || !config.isRangedWeapon)
        return null;

    // 2. 获取枪口局部偏移（支持左手覆盖）
    Vector3 localMuzzleOffset = config.muzzleOffset;
    if (chipSide == SlotSide.LeftHand && config.leftMuzzleOffsetOverride.HasValue)
        localMuzzleOffset = config.leftMuzzleOffsetOverride.Value;

    // 3. 计算武器世界位置（复用现有的BuildEntry逻辑）
    bool isLeft = chipSide == SlotSide.LeftHand;
    var weaponEntry = CompTriggerBody.BuildEntry(config, chipThing, CasterPawn.Rotation, isLeft);
    Vector3 weaponWorldPos = CasterPawn.DrawPos + weaponEntry.drawOffset;

    // 4. 计算武器总旋转角度 = 瞄准角度 + 配置角度
    float totalAngle = aimAngle + weaponEntry.angle;

    // 5. 使用四元数旋转将枪口偏移从局部坐标系转换到世界坐标系
    Quaternion weaponRotation = Quaternion.AngleAxis(totalAngle, Vector3.up);
    Vector3 muzzleWorldOffset = weaponRotation * localMuzzleOffset;

    // 6. 枪口世界坐标 = 武器世界坐标 + 枪口世界偏移
    return weaponWorldPos + muzzleWorldOffset;
}
```

### 4.2 设计要点

1. **坐标系转换**：使用四元数旋转（`Quaternion.AngleAxis`）将局部坐标转换为世界坐标
2. **复用现有逻辑**：直接调用`CompTriggerBody.BuildEntry()`获取武器世界位置
3. **智能推导**：优先使用`leftMuzzleOffsetOverride`，未配置时使用`muzzleOffset`
4. **角度叠加**：考虑瞄准角度和配置角度，确保枪口位置随武器旋转
5. **性能优化**：只在发射时调用一次，不缓存中间结果

## 5. 集成层设计

### 5.1 LaunchProjectile() 修改

在`Verb_BDPRangedBase.LaunchProjectile()`中集成枪口位置计算：

```csharp
protected bool LaunchProjectile(Thing chipEquipment, Vector3 originOffset = default)
{
    // 无芯片上下文时回退到原版
    if (chipEquipment == null)
        return base.TryCastShot();

    // ── 现有逻辑：检查目标、投射物定义、LOS等 ──
    // ... (保持不变) ...

    // ── 新增：计算发射位置（优先使用枪口位置） ──
    Vector3 drawPos = caster.DrawPos + originOffset;

    // 尝试从枪口发射（仅远程武器）
    if (chipSide.HasValue)
    {
        var config = GetChipConfig();
        if (config?.isRangedWeapon == true)
        {
            // 计算瞄准角度
            float aimAngle = (currentTarget.CenterVector3 - caster.DrawPos).AngleFlat();

            // 获取枪口位置
            var muzzlePos = GetMuzzlePosition(config, chipSide.Value, aimAngle);
            if (muzzlePos.HasValue)
            {
                drawPos = muzzlePos.Value;
                // 注意：枪口位置已包含武器偏移，不再叠加originOffset
            }
        }
    }

    // ── 现有逻辑：生成投射物、命中判定、发射 ──
    Projectile proj = (Projectile)GenSpawn.Spawn(projectileDef, resultingLine.Source, caster.Map);
    proj.Launch(manningPawn, drawPos, ...);
    // ...
}
```

### 5.2 设计要点

1. **最小侵入**：只修改`drawPos`的计算逻辑，其他逻辑保持不变
2. **优先级策略**：优先使用枪口位置，回退到小人中心 + originOffset
3. **齐射兼容**：枪口位置已包含武器偏移，不再叠加`originOffset`
4. **性能考虑**：只在`chipSide.HasValue`时计算（单侧Verb）
5. **角度计算**：使用`AngleFlat()`获取水平面瞄准角度

## 6. 错误处理和向后兼容

### 6.1 错误处理策略

**配置缺失处理：**
- `config == null`：回退到小人中心发射
- `config.isRangedWeapon == false`：回退到小人中心发射
- `muzzleOffset == Vector3.zero`：视为未配置，回退到小人中心发射

**计算失败处理：**
- `chipSide`为null（双侧Verb）：由子类各自处理，基类返回null
- `GetMuzzlePosition()`返回null：自动回退到小人中心
- 四元数旋转异常：不应发生，但如果发生则回退

**运行时异常处理：**
- 所有枪口位置计算都包裹在null检查中
- 任何步骤失败都优雅降级，不抛出异常
- 使用`?.`和`??`操作符确保安全

### 6.2 向后兼容保证

**现有芯片完全兼容：**
- 未配置`isRangedWeapon`的芯片：默认值为`false`，自动回退
- 未配置`muzzleOffset`的芯片：默认值为`Vector3.zero`，自动回退
- 近战武器芯片：不受影响，继续从小人中心发射

**配置迁移路径：**
1. 第一阶段：添加代码，所有芯片保持现有行为
2. 第二阶段：逐步为枪械芯片添加`isRangedWeapon`和`muzzleOffset`配置
3. 第三阶段：测试和调优

**性能影响：**
- 未配置枪口位置的芯片：零性能开销（直接回退）
- 配置了枪口位置的芯片：每次发射增加约0.1ms计算时间（可忽略）

### 6.3 日志和调试

```csharp
// 开发模式下输出诊断信息
if (Prefs.DevMode && config?.isRangedWeapon == true)
{
    Log.Message($"[BDP.Muzzle] {chipSide}: muzzlePos={muzzlePos}, aimAngle={aimAngle:F1}");
}
```

## 7. 测试策略

### 7.1 测试方法

**单元测试（代码层面）：**
- 测试`GetMuzzlePosition()`的坐标转换逻辑
- 验证不同朝向下的枪口位置计算
- 测试左手覆盖值的优先级逻辑
- 测试向后兼容（未配置时的回退行为）

**集成测试（游戏内测试）：**
- 创建测试用双枪芯片配置
- 在游戏中装备测试芯片
- 观察子弹是否从枪口位置发射
- 测试不同朝向（South/North/East/West）
- 测试齐射模式下的枪口发射

**可视化调试：**
- 开发模式下绘制枪口位置标记（红色方块）
- 在控制台输出枪口坐标和瞄准角度
- 对比武器绘制位置和子弹发射位置

### 7.2 测试用例

```xml
<!-- 测试芯片：对称双手枪 -->
<ThingDef ParentName="BDP_ChipBase">
  <defName>BDP_Chip_TestDualPistol</defName>
  <label>测试双手枪</label>
  <modExtensions>
    <li Class="BDP.Trigger.WeaponDraw.WeaponDrawChipConfig">
      <isRangedWeapon>true</isRangedWeapon>
      <muzzleOffset>(0, 0, 0.25)</muzzleOffset>
      <graphicData>
        <texPath>Things/Weapons/Pistol</texPath>
        <drawSize>(0.8, 0.8)</drawSize>
      </graphicData>
      <defaultOffset>(-0.2, 0, 0.1)</defaultOffset>
      <defaultAngle>-15</defaultAngle>
      <leftHandAngleOffset>30</leftHandAngleOffset>
    </li>
  </modExtensions>
</ThingDef>
```

### 7.3 验收标准

1. ✅ 子弹从枪口位置发射（视觉上对齐）
2. ✅ 不同朝向下枪口位置正确
3. ✅ 左右手枪口位置对称（或按覆盖值）
4. ✅ 未配置的芯片保持原有行为
5. ✅ 性能无明显下降（发射延迟 < 1ms）
6. ✅ 齐射模式下枪口发射正常

### 7.4 调试工具

```csharp
// 在 Patch_DrawEquipmentAiming_Weapon 中添加
if (Prefs.DevMode && DebugViewSettings.drawTargeting)
{
    var triggerComp = eq.TryGetComp<CompTriggerBody>();
    if (triggerComp != null)
    {
        foreach (var slot in triggerComp.AllActiveSlots())
        {
            var config = slot.loadedChip?.def?.GetModExtension<WeaponDrawChipConfig>();
            if (config?.isRangedWeapon == true)
            {
                // 计算并绘制枪口位置
                var muzzlePos = GetMuzzlePosition(config, slot.side, aimAngle);
                if (muzzlePos.HasValue)
                {
                    GenDraw.DrawFieldEdges(
                        new List<IntVec3> { muzzlePos.Value.ToIntVec3() },
                        Color.red
                    );
                }
            }
        }
    }
}
```

## 8. 实施计划

### 8.1 实施步骤

1. **第一步**：扩展`WeaponDrawChipConfig`，添加`muzzleOffset`、`leftMuzzleOffsetOverride`和`isRangedWeapon`字段
2. **第二步**：在`Verb_BDPRangedBase`中实现`GetMuzzlePosition()`方法
3. **第三步**：修改`Verb_BDPRangedBase.LaunchProjectile()`，集成枪口位置
4. **第四步**：创建测试用的双枪芯片XML配置
5. **第五步**：游戏内测试，调整参数直到视觉效果满意
6. **第六步**：添加可视化调试工具（可选）

### 8.2 风险评估

**低风险：**
- 配置层扩展（只添加字段，不修改现有逻辑）
- 向后兼容性（未配置时自动回退）

**中风险：**
- 坐标系转换逻辑（需要仔细测试不同朝向）
- 齐射模式兼容性（需要验证originOffset处理）

**缓解措施：**
- 充分的单元测试和集成测试
- 开发模式下的可视化调试工具
- 渐进式部署（先测试单枪，再测试双枪）

## 9. 总结

本设计方案采用最小侵入式扩展的方式，在现有的武器绘制系统基础上实现枪口发射功能。通过配置驱动和运行时计算的架构，在视觉真实性和性能之间取得了良好的平衡。

**核心优势：**
- 改动最小，风险可控
- 完全向后兼容
- 配置灵活，支持对称和非对称武器
- 性能开销可忽略（只在发射时计算）
- 易于测试和调试

**下一步：**
- 创建详细的实现计划
- 开始编码实现
- 进行充分的测试和调优

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-13 | 初始版本，完成双枪枪口发射系统设计 | Claude Sonnet 4.6 |
