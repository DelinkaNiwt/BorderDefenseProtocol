# 战斗体快照系统修复记录

## 修复日期
2026-03-03

## 发现的问题

### 1. 重复定义错误
**错误信息**：
```
Mod Niwt.BDP has multiple Verse.ThingDefs named BDP_CombatBodyArmor. Skipping.
```

**原因**：
- `Combat/CombatBodyApparelDefs.xml`（旧文件）
- `Combat/ThingDefs_CombatApparel.xml`（新文件）

两个文件都定义了 `BDP_CombatBodyArmor`。

**修复**：
删除旧文件 `CombatBodyApparelDefs.xml`，保留新文件。

---

### 2. ThingDef 配置错误
**错误信息**：
```
Config error in BDP_CombatBodyArmor: has a recipeMaker but no costList or costStuffCount.
Config error in BDP_CombatBodyArmor: has >0 DeteriorationRate but can't deteriorate.
Config error in BDP_CombatBodyArmor: is smeltable but does not give anything for smelting.
Config error in BDP_CombatBodyArmor: flammable but has no hitpoints (will burn indefinitely)
```

**原因**：
- 使用了 `ApparelMakeableBase` 作为父类，继承了不需要的属性
- 设置了 `recipeMaker` 但没有 `costList`
- 设置了 `Flammability` 但 `useHitPoints=false`
- 继承了 `thingCategories`、`tradeTags` 等不需要的属性

**修复**：
1. 改用 `ApparelBase` 作为父类（更基础，继承更少）
2. 移除 `recipeMaker`（战斗体装备不可制作）
3. 移除 `Flammability`（不可燃烧）
4. 移除 `thingCategories`（不属于任何分类）
5. 移除 `colorGenerator`（不需要颜色变化）
6. 移除 `defaultOutfitTags`（不属于任何服装组）
7. 移除 `tradeTags`（不可交易）
8. 设置 `useHitPoints=false`（无耐久）
9. 设置 `generateCommonality=0`（不自然生成）

---

### 3. 贴图缺失
**错误信息**：
```
Could not load Texture2D at 'Things/Pawn/Humanlike/Apparel/CombatBodyArmor/CombatBodyArmor'
```

**原因**：
指定的贴图路径不存在。

**修复**：
临时使用游戏内置的 ShieldBelt 贴图：
```xml
<texPath>Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt</texPath>
<wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt</wornGraphicPath>
```

**TODO**：
未来需要创建专用的战斗体装甲贴图。

---

### 4. Harmony Patch 错误
**错误信息**：
```
Error in static constructor of BDP.BDPMod: System.TypeInitializationException
Patching exception in method null
Undefined target method for patch method static
```

**原因**：
`Patch_CompRottable` 使用了 `nameof(CompRottable.CompTick)`，但 `CompTick` 是继承自 `ThingComp` 的方法，不是 `CompRottable` 的直接成员，`nameof` 可能无法正确解析。

**修复**：
改用字符串字面量方式指定方法名：
```csharp
[HarmonyPatch(typeof(CompRottable))]
[HarmonyPatch("CompTick")]
public static class Patch_CompRottable_PreventRotInSnapshot
```

同时添加 null 检查：
```csharp
var holder = __instance.parent?.holdingOwner?.Owner;
```

---

## 修复后的文件

### ThingDefs_CombatApparel.xml（简化版）
```xml
<ThingDef ParentName="ApparelBase">
  <defName>BDP_CombatBodyArmor</defName>
  <label>战斗体装甲</label>
  <description>战斗体激活时自动装备的特殊装甲。</description>
  <graphicData>
    <texPath>Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt</texPath>
    <graphicClass>Graphic_Single</graphicClass>
  </graphicData>
  <techLevel>Spacer</techLevel>
  <statBases>
    <MaxHitPoints>99999</MaxHitPoints>
    <Mass>0</Mass>
    <ArmorRating_Sharp>1.20</ArmorRating_Sharp>
    <ArmorRating_Blunt>0.50</ArmorRating_Blunt>
    <ArmorRating_Heat>0.60</ArmorRating_Heat>
    <Insulation_Cold>20</Insulation_Cold>
    <Insulation_Heat>10</Insulation_Heat>
    <EquipDelay>0</EquipDelay>
  </statBases>
  <equippedStatOffsets>
    <MoveSpeed>0.20</MoveSpeed>
  </equippedStatOffsets>
  <generateCommonality>0</generateCommonality>
  <useHitPoints>false</useHitPoints>
  <apparel>
    <bodyPartGroups>
      <li>Torso</li>
      <li>Shoulders</li>
      <li>Arms</li>
      <li>Legs</li>
    </bodyPartGroups>
    <wornGraphicPath>Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt</wornGraphicPath>
    <layers>
      <li>Middle</li>
      <li>Shell</li>
    </layers>
    <tags>
      <li>BDP_CombatBody</li>
    </tags>
  </apparel>
  <comps>
    <li Class="CompProperties_Forbiddable" />
  </comps>
  <tradeability>None</tradeability>
</ThingDef>
```

### Patch_CompRottable.cs（修复版）
```csharp
[HarmonyPatch(typeof(CompRottable))]
[HarmonyPatch("CompTick")]
public static class Patch_CompRottable_PreventRotInSnapshot
{
    [HarmonyPrefix]
    public static bool Prefix(CompRottable __instance)
    {
        var holder = __instance.parent?.holdingOwner?.Owner;
        if (holder is CombatBodySnapshot)
        {
            return false;  // 阻止腐烂 tick
        }
        return true;  // 其他容器正常腐烂
    }
}
```

---

## 验证步骤

1. ✅ 删除重复的 ThingDef 文件
2. ✅ 简化 ThingDef 配置，移除不需要的属性
3. ✅ 使用临时贴图（ShieldBelt）
4. ✅ 修复 Harmony Patch 语法
5. ✅ 编译成功，无警告，无错误
6. ⏳ 游戏内测试（待验证）

---

## 待办事项

1. **创建专用贴图**：
   - 战斗体装甲的图标贴图
   - 战斗体装甲的穿戴贴图
   - 路径：`Textures/Things/Pawn/Humanlike/Apparel/CombatBodyArmor/`

2. **测试 Harmony Patch**：
   - 验证物品腐烂阻止功能是否正常工作
   - 测试存读档后容器中的食物是否保持新鲜

3. **完善配置系统**：
   - 创建 `BDPSnapshotConfigDef` 用于配置排除的 Hediff
   - 添加战斗体装备的可配置性（通过建筑）

4. **实现激活逻辑**：
   - 添加战斗体激活/解除的 UI 按钮
   - 实现自动激活触发条件（如战斗开始时）

---

## 技术笔记

### Harmony Patch 方法名解析
- ✅ 推荐：`[HarmonyPatch("MethodName")]` - 字符串字面量
- ⚠️ 谨慎：`[HarmonyPatch(nameof(Class.Method))]` - 仅当方法是类的直接成员时可用
- ❌ 错误：对继承的方法使用 `nameof` 可能失败

### ThingDef 父类选择
- `ApparelBase` - 最基础的衣物定义，继承最少
- `ApparelMakeableBase` - 可制作的衣物，继承了 recipeMaker 相关属性
- `ApparelArmorBase` - 装甲类衣物，继承了更多防护相关属性

对于战斗体装备这种特殊物品，应该使用 `ApparelBase` 并手动添加需要的属性。

### useHitPoints 的影响
- `useHitPoints=false` 时，物品不会显示耐久度条
- 但是 `MaxHitPoints` 仍然需要设置（用于某些计算）
- 不能设置 `Flammability`（会导致无限燃烧）
- 不能设置 `DeteriorationRate`（会导致配置错误）
