---
标题：存档系统与ExposeData机制
版本号: v1.0
更新日期: 2026-02-16
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][未完成][未锁定]
摘要: RimWorld存档系统的完整技术参考。涵盖IExposable接口、Scribe系列API、LoadSaveMode生命周期、LookMode选择、存档XML格式、复杂场景示例和常见陷阱。
---

# 存档系统与ExposeData机制

## 1. 架构总览

RimWorld的存档系统基于XML序列化，核心设计是**同一个ExposeData()方法同时处理保存和加载**，通过`Scribe.mode`区分当前操作。

```
保存流程：
Game.ExposeData() → 递归调用所有子对象的ExposeData()
    → Scribe_Values/References/Deep/Collections 写入XML节点
        → ScribeSaver 输出到.rws文件

加载流程（三阶段）：
阶段1 LoadingVars：
    ScribeLoader 解析XML → 调用所有ExposeData()
    → Scribe_Values 直接赋值
    → Scribe_References 记录ID字符串（不解析）
    → Scribe_Deep 递归创建对象

阶段2 ResolvingCrossRefs：
    CrossRefHandler 将ID字符串解析为实际对象引用
    → Scribe_References 的ref参数被赋值为实际对象

阶段3 PostLoadInit：
    再次调用ExposeData()（mode=PostLoadInit）
    → 模组在此阶段做数据修正和清理
```

## 2. 核心接口与类

### IExposable

```csharp
public interface IExposable
{
    void ExposeData();
}
```

所有需要存档的对象都实现此接口。包括：Thing、Hediff、ThingComp、HediffComp、MapComponent、GameComponent、各种Tracker等。

### ILoadReferenceable

```csharp
public interface ILoadReferenceable
{
    string GetUniqueLoadID();
}
```

可被引用保存的对象实现此接口。Thing的UniqueLoadID格式为`"Thing_" + thingIDNumber`，Pawn为`"Pawn_" + thingIDNumber`。

### Scribe静态类

```csharp
public static class Scribe
{
    public static ScribeSaver saver;      // 保存器
    public static ScribeLoader loader;    // 加载器
    public static LoadSaveMode mode;      // 当前模式
}
```

### LoadSaveMode枚举

```csharp
public enum LoadSaveMode : byte
{
    Inactive,           // 默认，未进行存档操作
    Saving,             // 正在保存
    LoadingVars,        // 加载阶段1：读取基本值和ID
    ResolvingCrossRefs, // 加载阶段2：解析对象引用
    PostLoadInit        // 加载阶段3：后处理
}
```

### LookMode枚举

```csharp
public enum LookMode : byte
{
    Undefined,        // 未指定（Scribe_Collections自动推断）
    Value,            // 基本值类型
    Deep,             // IExposable深度序列化
    Reference,        // ILoadReferenceable引用
    Def,              // Def引用（通过defName）
    LocalTargetInfo,  // 本地目标
    TargetInfo,       // 目标信息
    GlobalTargetInfo, // 全局目标
    BodyPart          // 身体部位
}
```

## 3. Scribe系列API详解

### 3.1 Scribe_Values — 基本值类型

```csharp
public static void Look<T>(ref T value, string label, T defaultValue = default, bool forceSave = false)
```

**支持类型**：int, float, bool, string, enum, IntVec3, Vector2, Vector3, Quaternion, Color, Rot4, CellRect等。

**行为**：
- **Saving**：值≠defaultValue时写入XML元素；值=defaultValue时跳过（节省空间）
- **LoadingVars**：从XML读取值；节点不存在时返回defaultValue
- **forceSave=true**：即使值=defaultValue也强制写入

**类型安全检查**（Saving时自动报错）：
- Thing类型 → 应用Scribe_References或Scribe_Deep
- IExposable类型 → 应用Scribe_References或Scribe_Deep
- Def类型 → 应用Scribe_Defs
- TargetInfo → 应用Scribe_TargetInfo

**float精度**：float使用"G9"格式字符串，保证足够精度。

### 3.2 Scribe_Defs — Def引用

```csharp
public static void Look<T>(ref T value, string label) where T : Def, new()
```

**行为**：
- **Saving**：写入`value.defName`字符串（null时写入"null"）
- **LoadingVars**：通过defName从DefDatabase查询Def对象

**注意**：Def是全局唯一的静态数据，不需要引用解析阶段。

### 3.3 Scribe_References — 对象引用

```csharp
public static void Look<T>(ref T refee, string label, bool saveDestroyedThings = false)
    where T : ILoadReferenceable
```

**三阶段行为**：
1. **Saving**：写入`refee.GetUniqueLoadID()`
2. **LoadingVars**：读取ID字符串，注册到`CrossRefHandler`
3. **ResolvingCrossRefs**：通过ID查找实际对象，赋值给ref参数

**已销毁对象**：
- 默认不保存已销毁Thing的引用（写入"null"）
- `saveDestroyedThings=true`：允许保存已销毁但未Discard的Thing引用
- 已Discard的Thing始终不保存（报Warning）

**WeakReference支持**：有专门的重载处理`WeakReference<T>`。

### 3.4 Scribe_Deep — 深度序列化

```csharp
public static void Look<T>(ref T target, string label, params object[] ctorArgs)
```

**行为**：
- **Saving**：进入XML子节点 → 调用`target.ExposeData()` → 退出节点
  - 多态：运行时类型≠声明类型时，自动写入`Class`属性
  - null：写入`IsNull="True"`属性
- **LoadingVars**：从XML子节点创建对象 → 调用`ExposeData()`
  - `ctorArgs`传递给构造函数（如Tracker需要owner Pawn）
  - 通过`Class`属性支持多态反序列化

### 3.5 Scribe_Collections — 集合

```csharp
// List
public static void Look<T>(ref List<T> list, string label, LookMode lookMode, params object[] ctorArgs)

// Dictionary
public static void Look<K,V>(ref Dictionary<K,V> dict, string label,
    LookMode keyLookMode, LookMode valueLookMode, ref List<K> keysWorkingList, ref List<V> valuesWorkingList)
```

**LookMode选择**：
- 元素是基本类型 → `LookMode.Value`
- 元素是Def → `LookMode.Def`
- 元素是ILoadReferenceable → `LookMode.Reference`
- 元素是IExposable → `LookMode.Deep`

**自动推断**：LookMode.Undefined时，Scribe_Universal.TryResolveLookMode()尝试自动推断。

**Dictionary序列化**：需要提供keysWorkingList和valuesWorkingList作为工作缓冲区：

```csharp
private List<Pawn> tmpKeys;
private List<float> tmpValues;

public void ExposeData()
{
    Scribe_Collections.Look(ref myDict, "myDict",
        LookMode.Reference, LookMode.Value,
        ref tmpKeys, ref tmpValues);
}
```

### 3.6 Scribe_TargetInfo — 目标信息

```csharp
public static void Look(ref LocalTargetInfo value, string label)
public static void Look(ref TargetInfo value, string label)
public static void Look(ref GlobalTargetInfo value, string label)
```

LocalTargetInfo可以是Thing引用或IntVec3坐标，需要特殊处理。

## 4. 存档XML格式示例

```xml
<!-- Thing的存档格式 -->
<thing Class="ThingWithComps">
  <def>MeleeWeapon_Longsword</def>
  <id>Thing_12345</id>
  <map>0</map>
  <pos>(15, 0, 20)</pos>
  <stuff>Steel</stuff>
  <hitPoints>95</hitPoints>

  <!-- ThingComp数据 -->
  <comps>
    <li Class="CompQuality">
      <quality>Excellent</quality>
    </li>
    <li Class="CompShield">
      <energy>0.85</energy>
      <ticksToReset>-1</ticksToReset>
    </li>
  </comps>
</thing>

<!-- Hediff的存档格式 -->
<hediff Class="HediffWithComps">
  <def>MyCustomHediff</def>
  <severity>0.5</severity>
  <ageTicks>15000</ageTicks>
  <comps>
    <li Class="HediffComp_SeverityPerDay">
      <!-- HediffComp数据 -->
    </li>
  </comps>
</hediff>
```

## 5. 完整ExposeData示例

### 简单ThingComp

```csharp
public class CompTrionShield : ThingComp
{
    private float energy;
    private int cooldownTicks;
    private bool isActive;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref energy, "energy", 0f);
        Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 0);
        Scribe_Values.Look(ref isActive, "isActive", false);
    }
}
```

### 复杂场景：引用+集合+后处理

```csharp
public class CompSquadLeader : ThingComp
{
    private Pawn targetPawn;                    // 对象引用
    private ThingDef preferredWeapon;           // Def引用
    private List<Pawn> squadMembers;            // 引用列表
    private Dictionary<Pawn, float> loyaltyMap; // 引用-值字典
    private TrionState innerState;              // 深度嵌套对象

    // 字典工作缓冲区
    private List<Pawn> tmpKeys;
    private List<float> tmpValues;

    public override void PostExposeData()
    {
        base.PostExposeData();

        // 基本值
        // （无基本值字段示例）

        // Def引用
        Scribe_Defs.Look(ref preferredWeapon, "preferredWeapon");

        // 对象引用
        Scribe_References.Look(ref targetPawn, "targetPawn");

        // 深度嵌套
        Scribe_Deep.Look(ref innerState, "innerState", parent);

        // 引用列表
        Scribe_Collections.Look(ref squadMembers, "squadMembers", LookMode.Reference);

        // 引用-值字典
        Scribe_Collections.Look(ref loyaltyMap, "loyaltyMap",
            LookMode.Reference, LookMode.Value,
            ref tmpKeys, ref tmpValues);

        // 后处理：清理无效引用
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            squadMembers?.RemoveAll(p => p == null || p.Dead);
            if (loyaltyMap != null)
            {
                var deadKeys = loyaltyMap.Keys.Where(p => p == null).ToList();
                foreach (var key in deadKeys) loyaltyMap.Remove(key);
            }
        }
    }
}
```

### GameComponent（全局数据）

```csharp
public class TrionGameComponent : GameComponent
{
    private int globalTrionLevel;
    private List<Thing> registeredDevices;

    public TrionGameComponent(Game game) : base() { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref globalTrionLevel, "globalTrionLevel", 0);
        Scribe_Collections.Look(ref registeredDevices, "registeredDevices", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            registeredDevices ??= new List<Thing>();
            registeredDevices.RemoveAll(t => t == null);
        }
    }
}
```

## 6. 常见陷阱详解

### 陷阱1：默认值不一致

```csharp
// ❌ 错误：字段初始值=-1，但Scribe默认值=0
private int cooldown = -1;
Scribe_Values.Look(ref cooldown, "cooldown");  // defaultValue=0

// 保存时cooldown=-1，≠默认值0，写入XML
// 但如果cooldown=0，=默认值0，不写入XML
// 加载旧存档（无此字段）时，返回0而非-1

// ✅ 正确：保持一致
Scribe_Values.Look(ref cooldown, "cooldown", -1);
```

### 陷阱2：Reference在LoadingVars阶段为null

```csharp
// ❌ 错误：LoadingVars阶段引用尚未解析
public override void PostExposeData()
{
    Scribe_References.Look(ref myPawn, "myPawn");
    if (myPawn != null)  // LoadingVars时始终为null！
    {
        DoSomething(myPawn);
    }
}

// ✅ 正确：在PostLoadInit阶段访问
public override void PostExposeData()
{
    Scribe_References.Look(ref myPawn, "myPawn");
    if (Scribe.mode == LoadSaveMode.PostLoadInit && myPawn != null)
    {
        DoSomething(myPawn);
    }
}
```

### 陷阱3：集合加载后为null

```csharp
// ❌ 存档中无此节点时，list加载后为null
Scribe_Collections.Look(ref myList, "myList", LookMode.Deep);
myList.Add(item);  // NullReferenceException!

// ✅ PostLoadInit中初始化
if (Scribe.mode == LoadSaveMode.PostLoadInit)
{
    myList ??= new List<MyItem>();
}
```

### 陷阱4：忘记调用base

```csharp
// ❌ ThingComp不调base，父类数据丢失
public override void PostExposeData()
{
    Scribe_Values.Look(ref myField, "myField");
}

// ✅ 始终调用base
public override void PostExposeData()
{
    base.PostExposeData();
    Scribe_Values.Look(ref myField, "myField");
}
```

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-16 | 创建文档。基于Scribe/Scribe_Values/Scribe_References/Scribe_Deep/Scribe_Collections/Scribe_Defs源码研究，结合原版CompShield/CompRefuelable/Pawn_HealthTracker/HediffSet/Pawn_AbilityTracker的ExposeData实现 | Claude Opus 4.6 |
