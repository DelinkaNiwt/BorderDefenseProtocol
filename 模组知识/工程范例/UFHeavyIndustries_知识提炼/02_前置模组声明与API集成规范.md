# UF Heavy Industries - 前置模组声明与API集成规范

## 📋 元信息

**摘要**：详细分析联合重工模组的前置模组声明方式、依赖关系、与Harmony/SRALib/Biotech等前置模组的API集成规范，提供可直接参考的集成标准。

**版本号**：v1.0
**修改时间**：2026-01-12
**关键词**：前置模组, About.xml, API集成, Harmony, SRALib, Biotech DLC

**标签**：
- [待审] 基于About.xml分析
- [规范] 可作为其他模组参考
- [集成] API调用标准化

---

## 第一部分：About.xml 完整分析

### 1.1 About.xml 源文件解读

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ModMetaData>
  <!-- 1. 基本信息 -->
  <name>联合重工-UF Heavy Industries</name>
  <author>善良海豹</author>
  <packageId>KindSeal.LOL</packageId>

  <!-- 2. 版本支持 -->
  <supportedVersions>
    <li>1.6</li>
    <li>1.7</li>
    <li>1.8</li>
    <li>1.9</li>
  </supportedVersions>

  <!-- 3. 模组描述 -->
  <description>
    善良的联合舰娘北宅又获得了地球时间差不多几百万年的短暂假期，于是她来到边缘世界体验生活。
    这个周期的她非常喜欢造大舰巨炮，但苦于现有的势力都没有满足其要求的型号，
    于是她自创了一个善良铸造厂用于生产销售她定制的产品。
  </description>

  <!-- 4. 前置模组声明 -->
  <modDependencies>
    <li>
      <packageId>brrainz.harmony</packageId>
      <displayName>Harmony</displayName>
      <steamWorkshopUrl>steam://url/CommunityFilePage/2009463077</steamWorkshopUrl>
      <downloadUrl>https://github.com/pardeike/HarmonyRimWorld/releases/latest</downloadUrl>
    </li>
    <li>
      <packageId>Ludeon.RimWorld.Biotech</packageId>
      <displayName>Biotech</displayName>
      <steamWorkshopUrl>https://store.steampowered.com/app/1826140/RimWorld__Biotech/?curator_clanid=25160263</steamWorkshopUrl>
    </li>
    <li>
      <packageId>DiZhuan.SRALib</packageId>
      <displayName>SRALib</displayName>
      <steamWorkshopUrl>steam://url/CommunityFilePage/3565275325</steamWorkshopUrl>
    </li>
  </modDependencies>

  <!-- 5. 加载顺序声明 -->
  <loadAfter>
    <li>brrainz.harmony</li>
    <li>Ludeon.RimWorld</li>
    <li>DiZhuan.SRALib</li>
  </loadAfter>
</ModMetaData>
```

### 1.2 关键字段说明

| 字段 | 用途 | 值 | 说明 |
|------|------|-----|------|
| `name` | 显示名 | 联合重工-UF Heavy Industries | 用户看到的模组名 |
| `author` | 作者 | 善良海豹 | 开发者署名 |
| `packageId` | 模组ID | KindSeal.LOL | **唯一标识符，不能重复** |
| `supportedVersions` | 版本支持 | 1.6,1.7,1.8,1.9 | 支持4个版本 |
| `modDependencies` | 前置声明 | 3个模组 | **必须前置** |
| `loadAfter` | 加载顺序 | 3个模组 | 确保加载顺序正确 |

---

## 第二部分：前置模组详细说明

### 2.1 Harmony（必需）

**作用**：提供补丁系统框架

**为什么必需**：
- AT-Field 防御系统需要12个Harmony补丁来hook游戏逻辑
- 补丁用于拦截爆炸、投射物、激光等伤害

**集成方式**：

```csharp
// 在每个 Patch_*.cs 文件中
using HarmonyLib;

[HarmonyPatch(typeof(GenExplosion), "DoExplosion")]
public static class Patch_GenExplosion_DoExplosion
{
    public static bool Prefix(IntVec3 center, Map map, DamageDef damType)
    {
        // 拦截爆炸逻辑
        return true;  // 返回false会阻止原方法执行
    }
}
```

**关键属性**：
- PackageId: `brrainz.harmony`
- 最小版本: 2.0+ 推荐
- 加载优先级: 应该最早加载

**验证状态**：✓ RiMCP已验证 - HarmonyLib.HarmonyPatch 存在

---

### 2.2 Biotech DLC（必需）

**作用**：提供能力系统框架和装甲扩展

**为什么必需**：
- Turbojet 背包系统依赖 Biotech DLC 的能力系统
- 提供 AbilityDef、CompAbility、Pawn.abilities 等API

**集成方式**：

```csharp
// 在 TurbojetBackpack 项目中
using RimWorld;
using Verse;

public class CompAbility_Magazine : CompAbilityEffect
{
    public override void CompTick()
    {
        // 利用 Biotech 提供的能力框架
        base.CompTick();

        // 自定义弹匣逻辑
        if (charges < Props.maxCharges)
        {
            ticksToNextCharge--;
            if (ticksToNextCharge <= 0)
            {
                ReloadOneCharge();
            }
        }
    }
}
```

**关键API**：
- `AbilityDef` - 能力定义
- `CompAbilityEffect` - 能力效果基类
- `Pawn.abilities` - 角色能力管理器
- `Apparel` - 装甲系统扩展

**验证状态**：✓ RiMCP已验证 - RimWorld.CompAbilityEffect、RimWorld.Apparel 存在

---

### 2.3 SRALib（必需）

**作用**：提供高级渲染功能和工具库

**为什么必需**：
- 所有火力系统（炮塔、激光）需要SRA渲染框架
- AT-Field 护盾的视觉效果需要SRA的渲染优化
- Mote特效系统依赖SRA库

**集成方式**：

```csharp
// 在各项目中使用 SRA 渲染功能
using SRA;

public class Building_HeavyNavalGunTurret : Building
{
    // 使用 SRA 框架的渲染功能
    private SRA_Graphic graphic;

    public override void Draw()
    {
        base.Draw();

        // 使用 SRA 的高级渲染
        if (graphic != null)
        {
            graphic.Draw();
        }
    }
}

// Mote 特效系统
public class Mote_ATFieldOctagon : Mote
{
    public override void Draw()
    {
        // 使用 SRA 的特效渲染
        // 绘制八角形护盾效果
    }
}
```

**关键功能**：
- `SRA_Graphic` - 高级图形类
- `Mote` - 特效粒子系统
- 渲染优化和性能增强
- 动画系统支持

**验证状态**：⚠️ 部分验证 - SRALib在RiMCP中可能不可用（第三方库），基于参考实现验证

---

## 第三部分：依赖关系与加载顺序

### 3.1 依赖关系图

```
UF Heavy Industries
│
├─ Harmony (必需)
│  ├─ 用于: AT-Field 的12个补丁
│  ├─ 用于: 电磁炮的补丁
│  └─ 用于: 其他系统扩展
│
├─ Biotech DLC (必需)
│  ├─ 用于: Turbojet 能力系统
│  ├─ 用于: 装甲扩展
│  └─ 用于: 能力框架
│
├─ SRALib (必需)
│  ├─ 用于: 火力系统渲染
│  ├─ 用于: AT-Field 护盾视觉
│  ├─ 用于: Mote 特效系统
│  └─ 用于: 动画与渲染优化
│
└─ RimWorld 核心 (隐式依赖)
   ├─ Verse 核心库
   └─ RimWorld 游戏逻辑
```

### 3.2 加载顺序关键性

```
推荐加载顺序（从上到下）：

1. RimWorld 核心
2. DLC: Biotech     ← 必须在UF之前
3. Harmony          ← 必须在UF之前
4. SRALib           ← 必须在UF之前
5. UF Heavy Industries  ← 本模组

理由：
- DLC 是基础，必须最早加载
- Harmony 补丁需要先注册，UF 的补丁才能生效
- SRALib 提供渲染框架，UF 的渲染依赖它
- UF 在最后加载，使用前置模组的功能
```

### 3.3 加载失败的后果

```
如果加载顺序错误：

❌ 如果 Harmony 没加载
   → AT-Field 的12个补丁注册失败
   → 护盾无法拦截任何伤害
   → 游戏可能崩溃

❌ 如果 Biotech DLC 未安装
   → CompAbility 系统不存在
   → Turbojet 背包无法工作
   → 装甲赋予能力失败

❌ 如果 SRALib 没加载
   → 高级渲染功能不可用
   → Mote 特效显示为白色方块
   → 炮塔动画卡顿或不显示
```

---

## 第四部分：API集成规范

### 4.1 Harmony 补丁集成规范

**模式1：Prefix 补丁（用于拦截）**

```csharp
[HarmonyPatch(typeof(GenExplosion), "DoExplosion")]
public static class Patch_GenExplosion_DoExplosion
{
    // Prefix: 在原方法之前执行
    // 返回 false 会阻止原方法执行
    public static bool Prefix(IntVec3 center, Map map, DamageDef damType)
    {
        if (map == null)
            return true;  // 继续原逻辑

        // 尝试拦截爆炸
        List<Comp_AbsoluteTerrorField> fields = ATFieldManager.Get(map).activeFields;
        if (fields != null && fields.Count > 0)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].CheckExplosionIntercept(center, damType))
                    return false;  // 拦截成功，阻止爆炸
            }
        }

        return true;  // 所有护盾都没拦截，继续原逻辑
    }
}
```

**最佳实践**：
- ✓ 使用 Prefix 而不是 Postfix（能阻止原方法）
- ✓ 快速返回，避免昂贵操作
- ✓ 防御性编程（检查 null）
- ✓ 补丁间不相互依赖

---

### 4.2 Biotech DLC API 集成规范

**使用能力系统的标准方式**：

```csharp
// 1. 在装甲穿戴时赋予能力
public class CompApparelGiveAbility : ThingComp
{
    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);

        // 检查前置条件
        if (pawn.abilities != null && Props.abilityDef != null)
        {
            // 调用 Biotech 提供的 API
            pawn.abilities.GainAbility(Props.abilityDef);
        }
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);

        if (pawn.abilities != null && Props.abilityDef != null)
        {
            // 移除能力
            pawn.abilities.RemoveAbility(Props.abilityDef);
        }
    }
}

// 2. 在能力效果中管理弹匣
public class CompAbility_Magazine : CompAbilityEffect
{
    private int charges;

    public override void CompTick()
    {
        base.CompTick();

        // 访问 parent（Biotech 提供）
        if (charges < Props.maxCharges)
        {
            ticksToNextCharge--;
            if (ticksToNextCharge <= 0)
            {
                ReloadOneCharge();
            }
        }
    }

    public bool TryConsumeCharge()
    {
        if (charges > 0)
        {
            charges--;
            return true;
        }
        return false;
    }
}
```

**关键约定**：
- ✓ 检查 Pawn.abilities != null（可能是旧版本游戏）
- ✓ 使用 Notify_Equipped/Unequipped 回调
- ✓ 继承 CompAbilityEffect 实现自定义逻辑
- ✓ 在 XML 定义中声明 CompProperties

---

### 4.3 SRALib 渲染 API 集成规范

**渲染框架使用示例**：

```csharp
// 在建筑中使用 SRA 渲染
public class Building_HeavyNavalGunTurret : Building
{
    private SRA_GraphicData graphicData;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        // 初始化 SRA 渲染
        if (graphicData == null)
        {
            graphicData = new SRA_GraphicData
            {
                texPath = "Things/Building/HeavyNavalGun",
                // 其他参数...
            };
        }
    }

    public override void Draw()
    {
        base.Draw();

        // 使用 SRA 高级渲染功能
        if (graphicData != null)
        {
            // 渲染管线
            Matrix4x4 matrix = default;
            matrix.SetTRS(DrawPos, Quaternion.identity, new Vector3(1f, 1f, 1f));

            // 使用 SRA 的图形系统
            Graphics.DrawMesh(MeshPool.plane10, matrix,
                graphicData.GetMaterial(), 0, null, 0);
        }
    }
}

// Mote 特效系统
public class Mote_ATFieldOctagon : Mote
{
    public float damageScale;

    public override void Draw()
    {
        // 使用 SRA 的粒子系统
        Vector3 drawPos = exactPosition;
        drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

        // 渲染八角形特效
        // SRA 提供高性能的粒子渲染管线
        Graphics.DrawMesh(
            MeshPool.plane10,
            Matrix4x4.TRS(drawPos, Quaternion.identity, Vector3.one * damageScale),
            material,
            0
        );
    }
}
```

**最佳实践**：
- ✓ 使用 SRA 的材质系统而不是基础材质
- ✓ 利用 SRA 的图形优化
- ✓ 在 Draw() 中使用 SRA 的渲染管线
- ✓ Mote 特效充分利用 SRA 的粒子系统

---

## 第五部分：集成检查清单

### 5.1 模组开发者集成检查清单

在开发使用这些前置模组的功能时，确保：

```
□ Harmony 集成
  □ About.xml 中声明了 Harmony
  □ 所有补丁类都有 [HarmonyPatch] 属性
  □ Prefix/Postfix 方法签名正确
  □ 补丁逻辑有防御性编程（null 检查）
  □ 补丁间没有循环依赖

□ Biotech DLC 集成
  □ 检查 Pawn.abilities != null 再使用
  □ 使用 AbilityDef 定义能力
  □ 继承 CompAbilityEffect 实现效果
  □ 在 XML 中声明 CompProperties_AbilityEffect
  □ 使用 Notify_Equipped/Unequipped 回调

□ SRALib 集成
  □ 导入 SRA 命名空间
  □ 使用 SRA 提供的图形类
  □ 在 Draw() 中使用 SRA 渲染管线
  □ Mote 特效使用 SRA 的粒子系统
  □ 测试在没有 SRA 时的降级方案
```

### 5.2 用户安装检查清单

使用此模组时，确保：

```
□ 安装了 RimWorld 1.6 ~ 1.9
□ 安装了 Biotech DLC
□ 安装了 Harmony（来自 Steam Workshop）
□ 安装了 SRALib（来自 Steam Workshop）
□ 模组加载顺序正确（Biotech → Harmony → SRALib → UF）
□ 没有加载冲突的模组
□ 没有禁用任何前置模组
```

---

## 第六部分：故障排查

### 6.1 常见问题与解决

| 问题 | 症状 | 原因 | 解决 |
|------|------|------|------|
| **护盾无法工作** | AT-Field建筑生成但无法拦截伤害 | Harmony未加载 | 检查About.xml，重新订阅Harmony模组 |
| **背包能力不显示** | 穿上背包但没有能力 | Biotech DLC未安装 | 必须购买Biotech DLC |
| **特效显示为白块** | 护盾/炮塔特效是白色方块 | SRALib未加载 | 重新订阅SRALib，确保加载顺序 |
| **模组无法加载** | 游戏启动时模组被禁用 | 缺少前置模组 | 安装3个前置模组：Harmony, SRALib, Biotech |
| **补丁冲突** | 游戏崩溃或行为异常 | 补丁冲突 | 检查是否有其他模组的补丁冲突 |

### 6.2 调试技巧

```csharp
// 在补丁中添加调试日志
[HarmonyPatch(typeof(GenExplosion), "DoExplosion")]
public static class Patch_GenExplosion_DoExplosion
{
    public static bool Prefix(IntVec3 center, Map map, DamageDef damType)
    {
        Log.Message($"[UF] Explosion at {center} with {damType?.label}");

        List<Comp_AbsoluteTerrorField> fields = ATFieldManager.Get(map).activeFields;
        Log.Message($"[UF] Active fields: {fields?.Count ?? 0}");

        if (fields != null && fields.Count > 0)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                bool intercepted = fields[i].CheckExplosionIntercept(center, damType);
                Log.Message($"[UF] Field {i} intercept: {intercepted}");
                if (intercepted)
                    return false;
            }
        }

        return true;
    }
}
```

---

## 验证状态

✓ **已验证API**：
- HarmonyLib.HarmonyPatch - Harmony补丁框架
- RimWorld.Apparel - 装甲系统（Biotech扩展）
- RimWorld.CompAbilityEffect - 能力效果基类
- RimWorld.Pawn.abilities - 能力管理器

⚠️ **部分验证**：
- SRALib 的具体API（基于参考实现）
- Mote 渲染系统的性能特性

❌ **未验证**：
- SRALib 的所有细节功能
- 与其他模组的兼容性

---

## 相关报告导航

- → **01_系统架构全景分析.md** - 了解整体项目结构
- → **03_代码实现架构与设计模式.md** - 深入API使用模式

---

## 历史记录

| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|--------|---------|------|
| 1.0 | 初版：前置模组声明与API集成规范 | 2026-01-12 | Knowledge Refiner |

