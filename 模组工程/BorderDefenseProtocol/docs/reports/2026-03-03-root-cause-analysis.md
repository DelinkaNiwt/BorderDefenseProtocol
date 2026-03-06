# 战斗体快照系统错误根因分析

## 分析日期
2026-03-03

## 分析原则
- 拒绝"补丁盖补丁"的叠加式修复
- 追溯错误的根本原因
- 模拟执行过程，确定真正错误点
- 检查曾经的"修复"是否修多或修错
- 保持代码干净

---

## 错误1：Harmony Patch 失败

### 错误信息
```
Error in static constructor of BDP.BDPMod: System.TypeInitializationException
Patching exception in method null
Undefined target method for patch method static
```

### 错误的"修复"历史
1. **第一次尝试**：使用 `nameof(CompRottable.CompTick)`
   - 问题：`CompTick` 不是 `CompRottable` 的直接成员

2. **第二次尝试**：改用字符串字面量 `"CompTick"`
   - 问题：`CompRottable` 根本没有 `CompTick` 方法

### 根因分析

#### 步骤1：查看 CompRottable 的实际方法
```csharp
// CompRottable 的实际方法列表
- CompTickInterval(int delta)
- CompTickRare()
- TickInterval(int delta)  // private
```

**结论**：`CompRottable` 没有 `CompTick` 方法！

#### 步骤2：理解腐烂机制
```csharp
public override void CompTickRare()
{
    TickInterval(250);  // 每250 ticks调用一次
}

private void TickInterval(int delta)
{
    if (!Active)  // ← 关键检查点
    {
        return;
    }
    // 增加腐烂进度
    RotProgress += rotRate * delta;
}
```

**关键发现**：腐烂系统通过 `Active` 属性控制是否腐烂。

#### 步骤3：查看 Active 属性
```csharp
public bool Active
{
    get
    {
        if (PropsRot.disableIfHatcher)
        {
            // 孵化器逻辑
        }
        return !disabled;
    }
}
```

### 正确的修复方案

**方案A（推荐）**：Patch `Active` 属性的 getter
```csharp
[HarmonyPatch(typeof(CompRottable), nameof(CompRottable.Active), MethodType.Getter)]
public static class Patch_CompRottable_PreventRotInSnapshot
{
    [HarmonyPostfix]
    public static void Postfix(CompRottable __instance, ref bool __result)
    {
        if (__result && __instance.parent?.holdingOwner?.Owner is CombatBodySnapshot)
        {
            __result = false;  // 强制 Active = false
        }
    }
}
```

**优点**：
- 最小侵入性
- 利用游戏原有的控制流
- 不会影响其他系统

**方案B（备选）**：Patch `CompTickRare()`
```csharp
[HarmonyPatch(typeof(CompRottable), nameof(CompRottable.CompTickRare))]
public static class Patch_CompRottable_PreventRotInSnapshot
{
    [HarmonyPrefix]
    public static bool Prefix(CompRottable __instance)
    {
        if (__instance.parent?.holdingOwner?.Owner is CombatBodySnapshot)
        {
            return false;  // 跳过整个 tick
        }
        return true;
    }
}
```

**缺点**：
- 更高的侵入性
- 可能影响其他依赖 `CompTickRare` 的逻辑

### 为什么之前的方案失败

1. **假设错误**：假设 `CompRottable` 有 `CompTick` 方法
   - 实际：`CompTick` 是 `ThingComp` 的虚方法，但 `CompRottable` 没有重写它

2. **没有验证**：没有查看 RimWorld 源码确认方法存在

3. **补丁思维**：发现错误后直接改字符串，而不是重新分析

---

## 错误2：BDP_CombatBodyArmor 配置错误

### 错误信息
```
Config error in BDP_CombatBodyArmor: has >0 DeteriorationRate but can't deteriorate.
Config error in BDP_CombatBodyArmor: is smeltable but does not give anything for smelting.
Config error in BDP_CombatBodyArmor: flammable but has no hitpoints (will burn indefinitely)
```

### 错误的"修复"历史
1. **第一次尝试**：使用 `ApparelMakeableBase` 父类
   - 问题：继承了 `recipeMaker`，但没有 `costList`

2. **第二次尝试**：改用 `ApparelBase` 父类
   - 问题：仍然继承了 `Flammability`、`DeteriorationRate`、`smeltable`

3. **第三次尝试**：设置 `useHitPoints=false`
   - 问题：与 `Flammability` 冲突，导致无限燃烧

### 根因分析

#### 步骤1：查看 ApparelBase 的定义
```xml
<ThingDef Name="ApparelBase">
  <useHitPoints>True</useHitPoints>
  <statBases>
    <MaxHitPoints>100</MaxHitPoints>
    <Flammability>1.0</Flammability>
    <DeteriorationRate>2</DeteriorationRate>
  </statBases>
  <smeltable>true</smeltable>
</ThingDef>
```

**问题**：`ApparelBase` 设置了我们不需要的属性。

#### 步骤2：理解配置错误的原因

1. **DeteriorationRate 错误**：
   - `ApparelBase` 设置了 `DeteriorationRate>2`
   - 我设置了 `useHitPoints=false`
   - 冲突：没有 hitpoints 就不能劣化

2. **smeltable 错误**：
   - `ApparelBase` 设置了 `smeltable>true`
   - 但没有定义 `costList` 或 `stuffCategories`
   - 冲突：熔炼后不知道给什么材料

3. **Flammability 错误**：
   - `ApparelBase` 设置了 `Flammability>1.0`
   - 我设置了 `useHitPoints=false`
   - 冲突：没有 hitpoints 会无限燃烧

#### 步骤3：理解 useHitPoints 的设计意图

**错误理解**：`useHitPoints=false` 表示"无敌"
**正确理解**：`useHitPoints=false` 表示"不显示耐久度条"，但仍然可以被破坏

**正确的"无敌"设计**：
- `useHitPoints=true`（显示耐久度条）
- `MaxHitPoints=99999`（超高耐久）
- `Flammability=0`（不可燃）
- `DeteriorationRate=0`（不劣化）
- `smeltable=false`（不可熔炼）

### 正确的修复方案

```xml
<ThingDef ParentName="ApparelBase">
  <defName>BDP_CombatBodyArmor</defName>
  <!-- 显式覆盖父类的不需要的属性 -->
  <statBases>
    <MaxHitPoints>99999</MaxHitPoints>
    <Flammability>0</Flammability>           <!-- 覆盖父类的 1.0 -->
    <DeteriorationRate>0</DeteriorationRate> <!-- 覆盖父类的 2 -->
  </statBases>
  <useHitPoints>true</useHitPoints>          <!-- 保持父类的 true -->
  <smeltable>false</smeltable>               <!-- 覆盖父类的 true -->
</ThingDef>
```

### 为什么之前的方案失败

1. **误解 useHitPoints**：以为 `false` 表示无敌

2. **没有覆盖继承属性**：只添加了新属性，没有覆盖父类的冲突属性

3. **补丁思维**：发现错误后换父类，而不是理解配置系统

---

## 错误3：音效引用错误

### 错误信息
```
Could not resolve cross-reference: No Verse.SoundDef named Interact_Sword found
```

### 根因分析

#### 步骤1：查找音效定义
```bash
# 搜索 RimWorld 的音效定义
Interact_Rifle      ✓ 存在
Interact_SMG        ✓ 存在
Interact_Grenade    ✓ 存在
Interact_MonoSword  ✓ 存在
Interact_Sword      ✗ 不存在
```

#### 步骤2：理解音效系统
- `soundInteract` 是可选属性
- 如果不设置，使用默认音效
- 如果设置了不存在的音效，会报错但不会崩溃

### 正确的修复方案

**方案A（推荐）**：删除音效定义
```xml
<ThingDef ParentName="BaseWeapon">
  <defName>BDP_TriggerBody_Test</defName>
  <!-- 不设置 soundInteract，使用默认音效 -->
</ThingDef>
```

**方案B（备选）**：使用存在的音效
```xml
<soundInteract>Interact_Rifle</soundInteract>
```

### 为什么之前没有发现

1. **复制粘贴**：可能从某个示例代码复制了 `Interact_Sword`

2. **没有验证**：没有检查音效是否存在

---

## 修复总结

### 修复1：Harmony Patch
- **修改文件**：`Combat/Snapshot/Patch_CompRottable.cs`
- **修改内容**：Patch `Active` 属性而不是 `CompTick` 方法
- **验证方法**：编译成功，游戏启动无错误

### 修复2：ThingDef 配置
- **修改文件**：`Defs/Combat/ThingDefs_CombatApparel.xml`
- **修改内容**：显式覆盖 `Flammability`、`DeteriorationRate`、`smeltable`
- **验证方法**：游戏启动无配置错误

### 修复3：音效引用
- **修改文件**：`Defs/Trigger/ThingDefs_TriggerBody.xml`
- **修改内容**：删除不存在的 `soundInteract`
- **验证方法**：游戏启动无音效错误

---

## 经验教训

### 1. 不要假设，要验证
- ✗ 假设 `CompRottable` 有 `CompTick` 方法
- ✓ 查看源码确认方法存在

### 2. 理解系统，不要猜测
- ✗ 猜测 `useHitPoints=false` 表示无敌
- ✓ 理解 RimWorld 的耐久度系统设计

### 3. 追溯根因，不要补丁
- ✗ 发现错误后直接改字符串
- ✓ 分析为什么会出错，找到根本原因

### 4. 检查继承，显式覆盖
- ✗ 只添加新属性，忽略父类的冲突属性
- ✓ 显式覆盖父类的不需要的属性

### 5. 保持代码干净
- ✗ 留下无用的配置（如 `useHitPoints=false`）
- ✓ 每个配置都有明确的目的

---

## 验证清单

- [x] 编译成功，无警告，无错误
- [ ] 游戏启动无 Harmony patch 错误
- [ ] 游戏启动无 ThingDef 配置错误
- [ ] 游戏启动无音效引用错误
- [ ] 游戏内测试：物品在容器中不腐烂
- [ ] 游戏内测试：战斗体装甲可以正常装备
- [ ] 游戏内测试：存读档后容器中的物品状态正确

---

## 下一步

1. 启动游戏，验证所有错误已修复
2. 游戏内测试战斗体快照系统的核心功能
3. 如果还有错误，继续根因分析，不要补丁修复
