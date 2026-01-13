# Milira 米莉拉模组 - 前置模组声明与API集成规范

## 📋 元信息

**摘要**：详细解析 Milira 在 About.xml 和 LoadFolders.xml 中对前置模组的声明写法，以及源代码中对前置模组API的引用和调用方式。提供标准化的集成范例。

**版本号**：v1.0
**修改时间**：2026-01-12
**关键词**：前置模组, 依赖声明, API集成, Harmony补丁, 版本管理

**标签**：
- [待审] 已完成初步分析，未经用户确认
- [参考价值] 可作为自有模组集成范例参考

---

## 第一部分：About.xml 前置模组声明规范

### 1. 完整文件内容解析

**源文件**：`About/About.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
	<name>Milira Race</name>
	<author>Ancot</author>
	<supportedVersions>
		<li>1.5</li>
		<li>1.6</li>
	</supportedVersions>
	<description>The sky elves Milira.</description>
	<packageId>Ancot.MiliraRace</packageId>
	<modIconPath IgnoreIfNoMatchingField="True">Milira/Faction/Faction_Icon</modIconPath>
	<modDependencies>
		<li>
			<packageId>erdelf.HumanoidAlienRaces</packageId>
			<displayName>Humanoid Alien Races 2.0</displayName>
			<steamWorkshopUrl>https://steamcommunity.com/sharedfiles/filedetails/?id=839005762</steamWorkshopUrl>
		</li>
		<li>
			<packageId>Ancot.AncotLibrary</packageId>
			<displayName>Ancot Library</displayName>
			<steamWorkshopUrl>https://steamcommunity.com/sharedfiles/filedetails/?id=2988801276</steamWorkshopUrl>
		</li>
		<li>
			<packageId>Ludeon.RimWorld.Royalty</packageId>
			<displayName>Royalty</displayName>
		  </li>
		  <li>
			<packageId>Ludeon.RimWorld.Biotech</packageId>
			<displayName>Biotech</displayName>
		  </li>
	</modDependencies>
	<loadAfter>
		<li>erdelf.HumanoidAlienRaces</li>
		<li>Ancot.AncotLibrary</li>
	</loadAfter>
	<incompatibleWith>
		<li>ssulunge.KijinRace3</li>
	</incompatibleWith>
</ModMetaData>
```

### 2. 前置模组声明详解

#### 2.1 modDependencies 部分（必需依赖）

**结构**：
```xml
<modDependencies>
	<li>
		<packageId>...</packageId>
		<displayName>...</displayName>
		<steamWorkshopUrl>...</steamWorkshopUrl>  <!-- 可选 -->
	</li>
</modDependencies>
```

**Milira 的前置声明**：

| # | packageId | displayName | 类型 | 说明 |
|----|-----------|-------------|------|------|
| 1 | `erdelf.HumanoidAlienRaces` | Humanoid Alien Races 2.0 | 模组 | 提供种族框架 |
| 2 | `Ancot.AncotLibrary` | Ancot Library | 模组 | 提供工具库 |
| 3 | `Ludeon.RimWorld.Royalty` | Royalty | DLC | 能力系统 |
| 4 | `Ludeon.RimWorld.Biotech` | Biotech | DLC | 机械体框架 |

**关键特征**：

1. **packageId 的命名规律**：
   - 模组：`[作者].[模组名]` (e.g., `erdelf.HumanoidAlienRaces`)
   - DLC：`Ludeon.RimWorld.[DLCName]` (固定前缀)

2. **displayName 的书写**：
   - 用户友好的显示名称
   - 可以包含版本号（如 "2.0"）
   - 用于在 Steam Workshop 中显示

3. **steamWorkshopUrl 的书写**：
   - Steam Workshop 的完整URL
   - DLC通常省略此字段（官方DLC，用户已知）
   - 格式：`https://steamcommunity.com/sharedfiles/filedetails/?id=[ID]`

#### 2.2 loadAfter 部分（加载顺序）

**目的**：确保前置模组在本模组之前加载

```xml
<loadAfter>
	<li>erdelf.HumanoidAlienRaces</li>
	<li>Ancot.AncotLibrary</li>
</loadAfter>
```

**特点**：
- 只列出需要确保加载顺序的前置（通常是非DLC的模组）
- DLC通常不需要在此列出（引擎自动处理）
- 使用 packageId，与 modDependencies 中的 packageId 对应

**Milira 的选择**：
- ✅ 列出了两个模组前置
- ❌ 没有列出DLC（这是正确的，DLC由引擎自动排序）

#### 2.3 incompatibleWith 部分（不兼容列表）

```xml
<incompatibleWith>
	<li>ssulunge.KijinRace3</li>
</incompatibleWith>
```

**说明**：
- 与 KijinRace3 不兼容（可能是种族定义冲突）
- 这是一个其他种族模组

### 3. 标准规范提取

#### 规范1：前置模组声明的最小要求

```xml
<modDependencies>
	<li>
		<packageId>前置模组的ID</packageId>
		<displayName>显示名称</displayName>
	</li>
</modDependencies>
```

#### 规范2：包括Steam链接的完整声明

```xml
<modDependencies>
	<li>
		<packageId>模组ID</packageId>
		<displayName>模组显示名称</displayName>
		<steamWorkshopUrl>https://steamcommunity.com/sharedfiles/filedetails/?id=ID号</steamWorkshopUrl>
	</li>
</modDependencies>
```

#### 规范3：加载顺序声明

```xml
<loadAfter>
	<li>前置模组ID</li>
	<li>另一个前置模组ID</li>
</loadAfter>
```

---

## 第二部分：LoadFolders.xml 版本化加载

### 1. 完整文件内容解析

**源文件**：`LoadFolders.xml`

```xml
<?xml version="1.0" encoding="UTF-8"?>
<loadFolders>
	<v1.5>
		<li>1.5</li>
		<li>Content</li>
		<li IfModActive="Ludeon.RimWorld.Royalty">1.5/Mods/Royalty</li>
		<li IfModActive="Ludeon.RimWorld.Biotech">1.5/Mods/Biotech</li>
		<li IfModActive="Ancot.KiiroRace">1.5/Mods/Ancot.KiiroRace</li>
	</v1.5>
	<v1.6>
		<li>1.6</li>
		<li>Content</li>
		<li IfModActive="Ludeon.RimWorld.Royalty">1.6/Mods/Royalty</li>
		<li IfModActive="Ludeon.RimWorld.Biotech">1.6/Mods/Biotech</li>
		<li IfModActive="Ludeon.RimWorld.Odyssey">1.6/Mods/Odyssey</li>
		<li IfModActive="Ancot.KiiroRace">1.6/Mods/Ancot.KiiroRace</li>
		<li IfModActive="Ancot.KiiroStoryEventsExpanded">1.6/Mods/Ancot.KiiroStoryEventsExpanded</li>
	</v1.6>
</loadFolders>
```

### 2. 加载机制详解

#### 2.1 基本加载列表

```xml
<v1.5>
	<li>1.5</li>           <!-- 必加载：1.5版本的核心文件 -->
	<li>Content</li>       <!-- 必加载：跨版本共享内容 -->
	<li IfModActive="..."> <!-- 条件加载：如果模组激活 -->
</v1.5>
```

**加载规则**：
1. 每个版本标签（`<v1.5>`, `<v1.6>`）对应一个RimWorld版本
2. 无条件的 `<li>` 总是加载
3. 带 `IfModActive` 属性的只在条件满足时加载

#### 2.2 无条件加载项

```xml
<li>1.5</li>      <!-- 版本特定核心内容 -->
<li>Content</li>  <!-- 跨版本共享资源 -->
```

**目的**：
- `1.5/1.6` - 包含该版本特定的 Defs, Source, Languages 等
- `Content` - 包含两个版本都使用的资源（如纹理、音频）

#### 2.3 条件加载：DLC支持

```xml
<li IfModActive="Ludeon.RimWorld.Royalty">1.5/Mods/Royalty</li>
<li IfModActive="Ludeon.RimWorld.Biotech">1.5/Mods/Biotech</li>
```

**设计**：
- 每个DLC一个独立文件夹
- 只有DLC激活时才加载相应的内容
- 避免没有DLC时的错误（如能力定义缺失）

#### 2.4 条件加载：第三方模组支持

```xml
<li IfModActive="Ancot.KiiroRace">1.5/Mods/Ancot.KiiroRace</li>
```

**目的**：
- 与其他Ancot种族的交互内容
- 仅当用户同时安装两个模组时才加载
- 提供种族间的兼容性

#### 2.5 版本差异

**1.5版本**：
```xml
<li IfModActive="Ludeon.RimWorld.Royalty">1.5/Mods/Royalty</li>
<li IfModActive="Ludeon.RimWorld.Biotech">1.5/Mods/Biotech</li>
<li IfModActive="Ancot.KiiroRace">1.5/Mods/Ancot.KiiroRace</li>
```

**1.6版本**：
```xml
<li IfModActive="Ludeon.RimWorld.Royalty">1.6/Mods/Royalty</li>
<li IfModActive="Ludeon.RimWorld.Biotech">1.6/Mods/Biotech</li>
<li IfModActive="Ludeon.RimWorld.Odyssey">1.6/Mods/Odyssey</li>  <!-- 新增 -->
<li IfModActive="Ancot.KiiroRace">1.6/Mods/Ancot.KiiroRace</li>
<li IfModActive="Ancot.KiiroStoryEventsExpanded">1.6/Mods/Ancot.KiiroStoryEventsExpanded</li>  <!-- 新增 -->
```

**区别**：
- 1.6新增了 Odyssey DLC 支持
- 1.6新增了 Ancot.KiiroStoryEventsExpanded 模组支持

### 3. 标准规范提取

#### 规范1：基础版本结构

```xml
<?xml version="1.0" encoding="UTF-8"?>
<loadFolders>
	<v1.5>
		<li>1.5</li>
		<li>Content</li>
	</v1.5>
	<v1.6>
		<li>1.6</li>
		<li>Content</li>
	</v1.6>
</loadFolders>
```

#### 规范2：添加DLC条件加载

```xml
<v1.5>
	<li>1.5</li>
	<li>Content</li>
	<li IfModActive="Ludeon.RimWorld.Royalty">1.5/Mods/Royalty</li>
	<li IfModActive="Ludeon.RimWorld.Biotech">1.5/Mods/Biotech</li>
</v1.5>
```

#### 规范3：添加第三方模组支持

```xml
<li IfModActive="某模组ID">1.5/Mods/某模组ID</li>
```

---

## 第三部分：源代码中的前置模组API集成

### 1. 命名空间引入方式

#### 1.1 AncotLibrary 的引入

**文件示例**：`Source/Milira/Building_TurretGunFortress.cs`

```csharp
using AncotLibrary;

namespace Milira;

public class Building_TurretGunFortress : Building_SpinTurretGun
{
	// ...
}
```

**规范**：
```csharp
using AncotLibrary;  // 声明前置库的命名空间

namespace Milira;    // 自有模组的命名空间
```

**特点**：
- 将 AncotLibrary 作为普通命名空间导入
- 可以直接使用 AncotLibrary 中的类型和方法

#### 1.2 其他标准命名空间

```csharp
using System.Collections.Generic;
using System.Linq;
using RimWorld;       // RimWorld核心API
using UnityEngine;    // 游戏引擎
using Verse;          // RimWorld基础框架
using AncotLibrary;   // 前置库
```

### 2. 前置框架基类继承

#### 2.1 示例1：CompDrone 继承

**文件**：`Source/Milira/CompDrone_Milira.cs`

```csharp
using AncotLibrary;

namespace Milira;

public class CompDrone_Milira : CompDrone
{
	public override bool Draftable => MiliraDefOf.Milira_DroneControl.IsFinished;
}
```

**分析**：
- `CompDrone` 是 AncotLibrary 提供的基类
- 继承该基类实现自有的无人机逻辑
- 覆写 `Draftable` 属性，添加自有的可编队条件

#### 2.2 示例2：CompMechCarrier_Custom 继承

**文件**：`Source/Milira/CompMechCarrier_Consul.cs`

```csharp
using AncotLibrary;
using Verse;

namespace Milira;

public class CompMechCarrier_Consul : CompMechCarrier_Custom
{
	public override PawnKindDef SpawnPawnKind
	{
		get
		{
			// 基类实现
			var baseKind = ((CompMechCarrier_Custom)this).SpawnPawnKind;

			// 自有逻辑
			if (ModsConfig.IsActive("Ancot.MilianModification"))
			{
				// 返回改装后的单位
			}

			return baseKind;
		}
	}
}
```

**模式**：
- 继承 AncotLibrary 的 CompMechCarrier_Custom 基类
- 覆写虚方法，可选调用基类实现（`((CompMechCarrier_Custom)this)`）
- 根据条件添加自有逻辑

### 3. 前置库工具类的使用

#### 3.1 AncotUtility 工具类

**文件**：`Source/Milira/CompAbilityEffect_Excalibur.cs`

```csharp
using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class CompAbilityEffect_Excalibur : CompAbilityEffect
{
	private float damageAmount => AncotUtility.QualityFactor(quality) * damageAmountBase;
	private float armorPenetration => AncotUtility.QualityFactor(quality) * armorPenetrationBase;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		// 使用工具类计算
		float scaledDamage = AncotUtility.QualityFactor(weapon.TryGetComp<CompQuality>().Quality);

		// 应用伤害
		GenExplosion.DoExplosion(
			position,
			map,
			radius,
			damageType,
			instigator,
			scaledDamage: 3 * (int)damageAmount,
			armorPenetration: 2f * armorPenetration
		);
	}
}
```

**规范**：
```csharp
// AncotUtility 提供的静态工具方法
float qualityFactor = AncotUtility.QualityFactor(QualityCategory quality);

// 用于计算品质对伤害/性能的影响
actualValue = baseValue * qualityFactor;
```

### 4. 前置组件的使用

#### 4.1 CompWeaponCharge 组件

**文件**：`Source/Milira/CompAbilityEffect_Excalibur.cs`

```csharp
public CompWeaponCharge compCharge => ((Thing)weapon).TryGetComp<CompWeaponCharge>();

public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
{
	// 检查蓄能状态
	if (compCharge != null && !compCharge.CanBeUsed)
	{
		return;  // 蓄能不足，无法使用
	}

	// 消耗一次蓄能
	CompWeaponCharge obj = compCharge;
	if (obj != null)
	{
		obj.UsedOnce();
	}
}
```

**说明**：
- `CompWeaponCharge` 是 AncotLibrary 提供的武器蓄能组件
- 使用 `TryGetComp<T>()` 获取组件实例
- 通过组件的方法控制蓄能行为

#### 4.2 CompThingContainer_Milian 组件

**文件**：`Source/Milira/Building_TurretGunFortress.cs`

```csharp
protected CompThingContainer_Milian compThingContainer
	=> ((Thing)(object)this).TryGetComp<CompThingContainer_Milian>();

public float damageReductionFactor
{
	get
	{
		if (compThingContainer != null && compThingContainer.innerPawn != null)
		{
			// 通过容器获取内部单位并计算属性
			return ArmorToDamageReductionCurve.Evaluate(
				TryGetOverallArmor(compThingContainer.innerPawn, StatDefOf.ArmorRating_Sharp)
			);
		}
		return 1f;
	}
}
```

**规范**：
```csharp
// 安全地获取自定义组件
var comp = ((Thing)this).TryGetComp<自定义组件>();
if (comp != null)
{
	// 使用组件的属性和方法
}
```

### 5. 可选模组的运行时检查

#### 5.1 检查可选增强模组

**文件**：`Source/Milira/CompMechCarrier_Consul.cs`

```csharp
public override int CostPerPawn
{
	get
	{
		int num = ((CompMechCarrier_Custom)this).CostPerPawn;

		// 检查可选模组
		if (ModsConfig.IsActive("Ancot.MilianModification"))
		{
			// 应用增强版本的数值
			if (((CompMechCarrier_Custom)this).pivot.health.hediffSet
				.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_ImprovedAssemblyProcess) != null)
			{
				num -= 10;  // 降低成本
			}

			if (((CompMechCarrier_Custom)this).pivot.health.hediffSet
				.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_IncrementalAssemblyProcess) != null)
			{
				num += 10;  // 增加成本
			}
		}

		return num;
	}
}
```

**规范**：
```csharp
// 检查可选的相关模组是否激活
if (ModsConfig.IsActive("模组ID"))
{
	// 模组激活，应用相关逻辑
	// 通常是性能增强或新功能
}
```

#### 5.2 典型使用场景

**示例**：`Source/Milira/CompMilianKnightICharge.cs`

```csharp
// 场景1：应用增强版本的改装效果
if (ModsConfig.IsActive("Ancot.MilianModification")
    && PawnOwner.health.hediffSet
       .GetFirstHediffOfDef(MiliraDefOf.MilianFitting_FootCatapult) != null)
{
	// 应用足部弹射装置的增强效果
	applySpecialEffect = true;
}

// 场景2：条件判断中的防御性检查
if (hediff != null && staggering
    && (!ModsConfig.IsActive("Ancot.MilianModification")
        || PawnOwner.health.hediffSet
           .GetFirstHediffOfDef(MiliraDefOf.MilianFitting_ShockAbsorptionModule) == null))
{
	// 当没有改装或没有防震模块时执行
	takeDamage = true;
}
```

### 6. 和谐补丁的使用

#### 6.1 启动和谐补丁

**文件**：`Source/Milira/Ancot_MiliraRace_Patch.cs`

```csharp
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class Ancot_MiliraRace_Patch
{
	static Ancot_MiliraRace_Patch()
	{
		Harmony harmony = new Harmony("Ancot.MiliraRaceHarmonyPatch");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
```

**规范**：
```csharp
[StaticConstructorOnStartup]          // 游戏启动时自动执行
public class PatchInitializer
{
	static PatchInitializer()
	{
		// 创建Harmony实例（ID唯一标识）
		Harmony harmony = new Harmony("作者.模组名HarmonyPatch");

		// 自动扫描并应用该程序集中的所有补丁
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
```

---

## 第四部分：API引用汇总与规范

### 1. 前置模组API速查表

#### AncotLibrary 常用API

| 类名 | 常用成员 | 用途 |
|------|---------|------|
| `AncotUtility` | `QualityFactor(QualityCategory)` | 计算品质因子 |
| `CompDrone` | 基类 | 无人机组件基类 |
| `CompMechCarrier_Custom` | `SpawnPawnKind`, `CostPerPawn`, `CooldownTicks` | 机械运输母体 |
| `CompWeaponCharge` | `CanBeUsed`, `UsedOnce()` | 武器蓄能系统 |
| `CompThingContainer_*` | `innerPawn`, `innerThing` | 物品/生物容器 |

#### RimWorld 核心API（通过 Using）

| 命名空间 | 常用类 | 用途 |
|---------|--------|------|
| `RimWorld` | `CompAbilityEffect`, `StatDefOf` | 能力效果基类 |
| `Verse` | `Pawn`, `Thing`, `Map`, `LocalTargetInfo` | 基础对象 |
| `UnityEngine` | `Vector3`, `Color` | 数学和图形 |

### 2. API集成的最佳实践

#### 实践1：安全的类型转换

```csharp
// ❌ 不安全
CompAbilityEffect ability = (CompAbilityEffect)this;

// ✅ 安全（带类型检查）
CompAbilityEffect ability = this as CompAbilityEffect;

// ✅ 最安全（先转为Thing）
var thing = ((Thing)(object)this);
var comp = thing.TryGetComp<CompAbilityEffect>();
```

#### 实践2：空值检查

```csharp
// ❌ 容易崩溃
float value = weapon.def.tools.First().power;  // 如果weapon为null会崩溃

// ✅ 安全的空值检查
if (weapon != null && weapon.def?.tools?.Any() == true)
{
	float value = weapon.def.tools.First().power;
}
```

#### 实践3：属性访问模式

```csharp
// 模式1：Getter属性（读取）
public CompWeaponCharge compCharge
	=> ((Thing)weapon).TryGetComp<CompWeaponCharge>();

// 使用时
if (compCharge != null && !compCharge.CanBeUsed) { ... }

// 模式2：缓存属性（频繁访问）
private CompQuality _qualityComp;
public CompQuality QualityComp
{
	get
	{
		if (_qualityComp == null)
			_qualityComp = weapon.TryGetComp<CompQuality>();
		return _qualityComp;
	}
}
```

#### 实践4：继承与覆写

```csharp
// 当继承前置库的基类时
public class MyComponent : CompMechCarrier_Custom
{
	// 覆写虚方法
	public override PawnKindDef SpawnPawnKind
	{
		get
		{
			// 可选：获取基类实现
			var baseResult = ((CompMechCarrier_Custom)this).SpawnPawnKind;

			// 应用自有逻辑
			if (customCondition)
				return customKind;

			// 回退到基类
			return baseResult;
		}
	}
}
```

### 3. 常见集成模式

#### 模式1：可选模组增强

```csharp
if (ModsConfig.IsActive("前置模组ID"))
{
	// 应用增强功能
}
```

**适用场景**：与其他模组的交互、可选增强功能

#### 模式2：基类继承与扩展

```csharp
public class CustomComponent : AncotLibraryBase
{
	public override void Initialize()
	{
		base.Initialize();  // 调用基类初始化
		// 自有初始化
	}
}
```

**适用场景**：建立在前置库基础上的自有类型

#### 模式3：工具类使用

```csharp
float result = AncotUtility.QualityFactor(qualityCategory);
```

**适用场景**：使用前置库提供的工具函数

---

## 第五部分：标准化范例

### 范例1：最小可行模组结构（使用AncotLibrary）

**About.xml**
```xml
<ModMetaData>
	<name>My Custom Mod</name>
	<author>Me</author>
	<supportedVersions><li>1.6</li></supportedVersions>
	<packageId>Me.MyCustomMod</packageId>
	<modDependencies>
		<li>
			<packageId>Ancot.AncotLibrary</packageId>
			<displayName>Ancot Library</displayName>
			<steamWorkshopUrl>https://steamcommunity.com/sharedfiles/filedetails/?id=2988801276</steamWorkshopUrl>
		</li>
	</modDependencies>
	<loadAfter>
		<li>Ancot.AncotLibrary</li>
	</loadAfter>
</ModMetaData>
```

**LoadFolders.xml**
```xml
<loadFolders>
	<v1.6>
		<li>1.6</li>
	</v1.6>
</loadFolders>
```

**Source/MyMod/MyComponent.cs**
```csharp
using AncotLibrary;
using Verse;

namespace MyMod;

public class MyDrone : CompDrone
{
	public override bool Draftable => true;  // 简单的覆写
}
```

### 范例2：完整的多DLC支持结构

**LoadFolders.xml**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<loadFolders>
	<v1.6>
		<li>1.6</li>
		<li>Content</li>
		<li IfModActive="Ludeon.RimWorld.Royalty">1.6/Mods/Royalty</li>
		<li IfModActive="Ludeon.RimWorld.Biotech">1.6/Mods/Biotech</li>
		<li IfModActive="SomeOtherMod.Enhanced">1.6/Mods/Enhanced</li>
	</v1.6>
</loadFolders>
```

---

## 第六部分：关键发现与建议

### 6.1 Milira 的集成特色

✅ **清晰的依赖声明**
- About.xml 中明确列出所有前置
- loadAfter 确保加载顺序

✅ **灵活的条件加载**
- 每个DLC一个单独目录
- 可选模组通过运行时检查支持

✅ **充分利用前置库**
- 大量继承 AncotLibrary 的基类
- 大量使用 AncotUtility 工具

### 6.2 开发者应注意的要点

⚠️ **About.xml 中的 displayName 重要**
- 这是玩家在 Steam 中看到的依赖名称
- 应该准确反映实际模组

⚠️ **loadAfter 必须与 modDependencies 对应**
- 不要遗漏任何必要的前置模组
- DLC通常不需要在 loadAfter 中列出

⚠️ **条件加载时提供完整的目录**
- 每个条件模组应有完整的文件结构
- 避免文件不存在的错误

### 6.3 学习价值

这个模组展示了：
- 如何正确声明和管理前置模组
- 如何在代码中集成前置库的API
- 如何支持多个DLC和版本
- 如何处理可选的增强模组

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|----------|----------|--------|
| v1.0 | 初版：前置模组声明与API集成规范 | 2026-01-12 | 知识提炼者 |

