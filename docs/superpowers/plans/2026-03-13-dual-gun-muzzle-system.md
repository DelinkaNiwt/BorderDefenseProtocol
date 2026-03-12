# 双枪枪口发射系统实施计划

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现BDP模组双武器系统的枪口发射功能，使子弹从实际的枪口位置发射，而非小人中心位置

**Architecture:** 采用"配置驱动 + 运行时计算"的架构模式，在现有的武器绘制系统基础上扩展枪口发射功能。配置层扩展WeaponDrawChipConfig添加枪口位置字段，计算层在Verb_BDPRangedBase中实现枪口位置计算，集成层修改LaunchProjectile()方法优先使用枪口位置发射。

**Tech Stack:** C# 7.3, RimWorld 1.6 API, Unity Quaternion, Harmony Patching

---

## 文件结构

**修改的文件：**
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/WeaponDraw/WeaponDrawChipConfig.cs` - 添加枪口位置配置字段
- `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/DualWeapon/Verb_BDPRangedBase.cs` - 添加枪口位置计算方法和集成发射逻辑

**创建的文件：**
- `模组工程/BorderDefenseProtocol/1.6/Defs/Trigger/ThingDefs_TestChips.xml` - 测试用双枪芯片配置

---

## Chunk 1: 配置层扩展

### Task 1: 扩展 WeaponDrawChipConfig 添加枪口位置字段

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/WeaponDraw/WeaponDrawChipConfig.cs:85-105`

- [ ] **Step 1: 在 WeaponDrawChipConfig 类中添加枪口位置相关字段**

在`WeaponDrawChipConfig.cs`文件的字段定义区域（`sideDeltaZ`字段之后，缓存字段之前）添加以下代码：

```csharp
// ── 枪口位置配置 ──

/// <summary>
/// 枪口相对于武器中心的偏移（武器局部坐标系）。
/// 例如：(0, 0, 0.3) 表示枪口在武器前端0.3米处。
/// X: 左右偏移，Y: 上下偏移，Z: 前后偏移（正值=向前）
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

- [ ] **Step 2: 编译项目验证语法正确**

Run: `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`

Expected: `Build succeeded.` 无编译错误

- [ ] **Step 3: 提交配置层扩展**

```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio"
git add "模组工程/BorderDefenseProtocol/Source/BDP/Trigger/WeaponDraw/WeaponDrawChipConfig.cs"
git commit -m "feat(config): add muzzle position fields to WeaponDrawChipConfig

- Add muzzleOffset field for weapon-local muzzle position
- Add leftMuzzleOffsetOverride for asymmetric weapons
- Add isRangedWeapon flag to enable muzzle firing

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Chunk 2: 计算层实现

### Task 2: 实现 GetMuzzlePosition 方法

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/DualWeapon/Verb_BDPRangedBase.cs:850-950`

- [ ] **Step 1: 在 Verb_BDPRangedBase 类中添加 GetMuzzlePosition 方法**

在`Verb_BDPRangedBase.cs`文件中，在`GetChipConfig()`方法之后（约850行附近）添加以下方法：

```csharp
/// <summary>
/// 计算指定侧别芯片的枪口世界坐标。
/// 使用四元数旋转将枪口从武器局部坐标系转换到世界坐标系。
/// </summary>
/// <param name="config">芯片的武器绘制配置</param>
/// <param name="chipSide">芯片侧别（左手/右手）</param>
/// <param name="aimAngle">瞄准角度（度）</param>
/// <returns>枪口世界坐标，如果不是远程武器则返回null</returns>
protected Vector3? GetMuzzlePosition(WeaponDrawChipConfig config, SlotSide chipSide, float aimAngle)
{
    // 1. 检查是否为远程武器
    if (config == null || !config.isRangedWeapon)
        return null;

    // 2. 获取枪口局部偏移（支持左手覆盖）
    Vector3 localMuzzleOffset = config.muzzleOffset;
    if (chipSide == SlotSide.LeftHand && config.leftMuzzleOffsetOverride.HasValue)
        localMuzzleOffset = config.leftMuzzleOffsetOverride.Value;

    // 3. 获取芯片Thing（用于BuildEntry）
    var triggerComp = GetTriggerComp();
    if (triggerComp == null) return null;
    var chipThing = triggerComp.GetActiveSlot(chipSide)?.loadedChip;
    if (chipThing == null) return null;

    // 4. 计算武器世界位置（复用现有的BuildEntry逻辑）
    bool isLeft = chipSide == SlotSide.LeftHand;
    var weaponEntry = CompTriggerBody.BuildEntry(config, chipThing, CasterPawn.Rotation, isLeft);
    Vector3 weaponWorldPos = CasterPawn.DrawPos + weaponEntry.drawOffset;

    // 5. 计算武器总旋转角度 = 瞄准角度 + 配置角度
    float totalAngle = aimAngle + weaponEntry.angle;

    // 6. 使用四元数旋转将枪口偏移从局部坐标系转换到世界坐标系
    Quaternion weaponRotation = Quaternion.AngleAxis(totalAngle, Vector3.up);
    Vector3 muzzleWorldOffset = weaponRotation * localMuzzleOffset;

    // 7. 枪口世界坐标 = 武器世界坐标 + 枪口世界偏移
    Vector3 muzzlePos = weaponWorldPos + muzzleWorldOffset;

    // 8. 开发模式下输出诊断信息
    if (Prefs.DevMode)
    {
        Log.Message($"[BDP.Muzzle] {chipSide}: weaponPos={weaponWorldPos}, " +
            $"muzzleOffset={localMuzzleOffset}, aimAngle={aimAngle:F1}, " +
            $"totalAngle={totalAngle:F1}, muzzlePos={muzzlePos}");
    }

    return muzzlePos;
}
```

- [ ] **Step 2: 添加必要的 using 语句**

确保文件顶部包含以下 using 语句（如果缺失则添加）：

```csharp
using BDP.Trigger.WeaponDraw;
```

- [ ] **Step 3: 编译项目验证语法正确**

Run: `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`

Expected: `Build succeeded.` 无编译错误

- [ ] **Step 4: 提交计算层实现**

```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio"
git add "模组工程/BorderDefenseProtocol/Source/BDP/Trigger/DualWeapon/Verb_BDPRangedBase.cs"
git commit -m "feat(verb): implement GetMuzzlePosition method

- Calculate muzzle world position from weapon-local offset
- Support left-hand override for asymmetric weapons
- Use quaternion rotation for coordinate transformation
- Add dev mode diagnostic logging

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Chunk 3: 集成层实现

### Task 3: 修改 LaunchProjectile 方法集成枪口位置

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/DualWeapon/Verb_BDPRangedBase.cs:174-305`

- [ ] **Step 1: 在 LaunchProjectile 方法中集成枪口位置计算**

找到`LaunchProjectile()`方法中的`Vector3 drawPos = caster.DrawPos + originOffset;`这一行（约215行），将其替换为以下代码：

```csharp
// ── 计算发射位置（优先使用枪口位置） ──
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
```

- [ ] **Step 2: 编译项目验证语法正确**

Run: `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`

Expected: `Build succeeded.` 无编译错误

- [ ] **Step 3: 提交集成层实现**

```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio"
git add "模组工程/BorderDefenseProtocol/Source/BDP/Trigger/DualWeapon/Verb_BDPRangedBase.cs"
git commit -m "feat(verb): integrate muzzle position into LaunchProjectile

- Prioritize muzzle position over pawn center for ranged weapons
- Fallback to pawn center if muzzle position unavailable
- Maintain backward compatibility with unconfigured chips

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Chunk 4: 测试配置

### Task 4: 创建测试用双枪芯片配置

**Files:**
- Create: `模组工程/BorderDefenseProtocol/1.6/Defs/Trigger/ThingDefs_TestChips.xml`

- [ ] **Step 1: 创建测试芯片配置文件**

创建新文件`ThingDefs_TestChips.xml`，内容如下：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- 测试芯片：对称双手枪 -->
  <ThingDef ParentName="BDP_ChipBase">
    <defName>BDP_Chip_TestDualPistol</defName>
    <label>测试双手枪芯片</label>
    <description>用于测试枪口发射系统的双手枪芯片。子弹应从枪口位置发射。</description>

    <graphicData>
      <texPath>Things/Item/Equipment/WeaponMelee/Knife</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>

    <statBases>
      <MarketValue>100</MarketValue>
      <Mass>0.5</Mass>
    </statBases>

    <modExtensions>
      <!-- 武器绘制配置 -->
      <li Class="BDP.Trigger.WeaponDraw.WeaponDrawChipConfig">
        <!-- 枪口发射配置 -->
        <isRangedWeapon>true</isRangedWeapon>
        <muzzleOffset>(0, 0, 0.25)</muzzleOffset>

        <!-- 武器贴图配置 -->
        <graphicData>
          <texPath>Things/Item/Equipment/WeaponMelee/Knife</texPath>
          <graphicClass>Graphic_Single</graphicClass>
          <drawSize>(0.8, 0.8)</drawSize>
        </graphicData>

        <!-- South/North 位置配置 -->
        <defaultOffset>(-0.2, 0, 0.1)</defaultOffset>
        <defaultAngle>-15</defaultAngle>
        <leftHandAngleOffset>30</leftHandAngleOffset>

        <!-- East/West 位置配置 -->
        <sideBaseX>0.15</sideBaseX>
        <sideDeltaX>0.05</sideDeltaX>
        <sideDeltaZ>0.05</sideDeltaZ>
      </li>

      <!-- 芯片效果配置 -->
      <li Class="BDP.Trigger.VerbChipConfig">
        <ranged>
          <verbClass>BDP.Trigger.Verb_BDPSingle</verbClass>
          <projectileDef>Bullet_Revolver</projectileDef>
          <warmupTime>0.3</warmupTime>
          <range>25</range>
          <burstShotCount>1</burstShotCount>
          <ticksBetweenBurstShots>10</ticksBetweenBurstShots>
          <soundCast>Shot_Revolver</soundCast>
        </ranged>
      </li>
    </modExtensions>
  </ThingDef>

</Defs>
```

- [ ] **Step 2: 验证 XML 格式正确**

Run: `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\1.6\Defs\Trigger" && cat ThingDefs_TestChips.xml | head -20`

Expected: 显示文件前20行，确认XML格式正确

- [ ] **Step 3: 提交测试配置**

```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio"
git add "模组工程/BorderDefenseProtocol/1.6/Defs/Trigger/ThingDefs_TestChips.xml"
git commit -m "test: add test chip for muzzle firing system

- Create BDP_Chip_TestDualPistol for testing
- Configure muzzle offset at 0.25m forward
- Use symmetric dual-pistol configuration

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Chunk 5: 游戏内测试

### Task 5: 游戏内功能测试

**Files:**
- Test: 游戏内测试

- [ ] **Step 1: 启动游戏并加载测试场景**

1. 启动 RimWorld
2. 加载开发模式存档或创建新游戏
3. 按 `F12` 启用开发模式
4. 按 `~` 打开调试控制台

- [ ] **Step 2: 生成测试芯片和触发体**

在调试控制台中执行：
```
Thing.Spawn BDP_TriggerBody_Kogetsu 1
Thing.Spawn BDP_Chip_TestDualPistol 2
```

- [ ] **Step 3: 装备测试芯片**

1. 选择一个小人
2. 让小人装备触发体
3. 打开触发体界面，将测试芯片装载到左右手槽位

- [ ] **Step 4: 测试不同朝向的枪口发射**

1. 命令小人攻击目标
2. 观察子弹是否从枪口位置发射（而非小人中心）
3. 测试四个朝向（South/North/East/West）
4. 检查控制台日志中的`[BDP.Muzzle]`诊断信息

Expected:
- 子弹从枪口位置发射，视觉上与武器对齐
- 控制台输出枪口位置坐标
- 不同朝向下枪口位置正确

- [ ] **Step 5: 测试向后兼容性**

1. 卸载测试芯片
2. 装载现有的非枪械芯片（如近战芯片）
3. 观察是否正常工作，无错误

Expected: 现有芯片正常工作，子弹从小人中心发射

- [ ] **Step 6: 记录测试结果**

在控制台或日志文件中查找`[BDP.Muzzle]`相关日志，确认：
- 枪口位置计算正确
- 瞄准角度合理
- 无异常或错误

---

## Chunk 6: 可视化调试工具（可选）

### Task 6: 添加枪口位置可视化调试

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/WeaponDraw/Patch_Pawn_DrawAt_Weapon.cs:60-122`

- [ ] **Step 1: 在 Patch_DrawEquipmentAiming_Weapon 中添加枪口位置可视化**

在`Patch_DrawEquipmentAiming_Weapon.Prefix()`方法的末尾（`return false;`之前）添加以下代码：

```csharp
// ── 开发模式：绘制枪口位置标记 ──
if (Prefs.DevMode && DebugViewSettings.drawTargeting)
{
    var pawn = eq.TryGetComp<CompEquippable>()?.PrimaryVerb?.CasterPawn;
    if (pawn != null)
    {
        foreach (var entry in entries)
        {
            // 注意：这里需要访问 Verb 来计算枪口位置
            // 由于 Patch 中无法直接访问 Verb，这里只是示意性代码
            // 实际实现可能需要在 Verb 中添加静态方法或通过其他方式访问

            // 简化版本：仅绘制武器位置
            Vector3 weaponPos = drawLoc + entry.drawOffset;
            GenDraw.DrawFieldEdges(
                new List<IntVec3> { weaponPos.ToIntVec3() },
                Color.yellow
            );
        }
    }
}
```

**注意：** 由于 Harmony Patch 的限制，完整的枪口位置可视化可能需要在 Verb 类中实现。这里提供的是简化版本，仅绘制武器位置作为参考。

- [ ] **Step 2: 编译项目验证语法正确**

Run: `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`

Expected: `Build succeeded.` 无编译错误

- [ ] **Step 3: 提交可视化调试工具**

```bash
cd "C:\NiwtDatas\Projects\RimworldModStudio"
git add "模组工程/BorderDefenseProtocol/Source/BDP/Trigger/WeaponDraw/Patch_Pawn_DrawAt_Weapon.cs"
git commit -m "feat(debug): add weapon position visualization in dev mode

- Draw weapon position markers when targeting debug view enabled
- Help verify weapon placement and muzzle position calculation

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## 验收标准

完成所有任务后，系统应满足以下标准：

1. ✅ **配置层**：WeaponDrawChipConfig 包含 muzzleOffset、leftMuzzleOffsetOverride 和 isRangedWeapon 字段
2. ✅ **计算层**：Verb_BDPRangedBase 实现 GetMuzzlePosition() 方法，正确计算枪口世界坐标
3. ✅ **集成层**：LaunchProjectile() 优先使用枪口位置，未配置时回退到小人中心
4. ✅ **测试配置**：存在测试用双枪芯片配置，可在游戏中验证功能
5. ✅ **游戏内测试**：子弹从枪口位置发射，视觉上与武器对齐
6. ✅ **向后兼容**：现有芯片正常工作，无破坏性变更
7. ✅ **性能**：发射延迟无明显增加（< 1ms）
8. ✅ **调试**：开发模式下输出枪口位置诊断信息

---

## 风险和缓解措施

**风险1：坐标系转换错误**
- 症状：子弹从错误的位置发射，或不同朝向下位置不一致
- 缓解：充分测试四个朝向，检查控制台日志中的坐标值

**风险2：齐射模式兼容性问题**
- 症状：齐射时子弹位置异常
- 缓解：测试齐射模式，确认 originOffset 处理正确

**风险3：性能下降**
- 症状：发射时出现卡顿
- 缓解：使用性能分析工具测量，确认计算时间 < 1ms

**风险4：向后兼容性破坏**
- 症状：现有芯片无法正常工作
- 缓解：测试多个现有芯片，确认默认值正确

---

## 下一步

完成本实施计划后，可以考虑以下扩展：

1. **枪口火焰特效**：在枪口位置生成火焰特效
2. **枪口烟雾**：在枪口位置生成烟雾粒子
3. **后坐力动画**：根据枪口位置计算后坐力方向
4. **声音定位**：将枪声定位到枪口位置
5. **多枪口支持**：支持霰弹枪等多枪口武器

---

**计划创建时间：** 2026-03-13
**预计实施时间：** 2-3 小时
**复杂度：** 中等
**风险等级：** 低-中
