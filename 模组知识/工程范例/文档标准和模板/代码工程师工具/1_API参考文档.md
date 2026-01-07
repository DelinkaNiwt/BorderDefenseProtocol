# API 参考文档

**摘要**：集中记录该模组涉及的所有RimWorld API接口，避免代码工程师重复验证API真实性。
**版本**：v1.0
**修改时间**：2026-01-04
**关键词**：API, RimWorld, 类方法, 参数定义, 返回值
**标签**：[定稿]

---

## 设计理由
- **为什么需要**：作为代码工程师，我需要快速查阅API的准确信息，包括类名、方法、参数类型和返回值，无需自己验证其真实性
- **如何使用**：按用途分类整理，每个API条目包含：类路径、方法签名、参数说明、返回值、使用例子、所属模块

---

## 1. 核心类 (Core Classes)

### 1.1 Thing
| 属性 | 类型 | 说明 |
|------|------|------|
| def | ThingDef | 该物品的定义 |
| Map | Map | 所在地图 |
| Position | IntVec3 | 位置坐标 |
| Rotation | Rot4 | 旋转方向 |

| 方法 | 签名 | 说明 |
|------|------|------|
| GetComps | `List<ThingComp> GetComps<T>() where T : ThingComp` | 获取指定类型的Comp |
| GetComp | `T GetComp<T>() where T : ThingComp` | 获取单个Comp |
| Tick | `virtual void Tick()` | 每帧调用（Tick周期为60帧=1秒） |

**使用例子**：
```csharp
Pawn pawn = this.parent as Pawn;
Comp_MyCustom comp = pawn.GetComp<Comp_MyCustom>();
```

---

### 1.2 ThingComp
| 方法 | 签名 | 说明 |
|------|------|------|
| Initialize | `void Initialize(Thing parent)` | 初始化Comp，parent是宿主物品 |
| PostSpawnSetup | `virtual void PostSpawnSetup(bool respawningAfterLoad)` | 生成后初始化 |
| PostDeSpawn | `virtual void PostDeSpawn(Map map)` | 移除前调用 |
| CompTick | `virtual void CompTick()` | 每Tick调用一次 |
| CompGetGizmosExtra | `virtual IEnumerable<Gizmo> CompGetGizmosExtra()` | 返回Gizmo列表 |

**使用例子**：
```csharp
public class Comp_MyMod : ThingComp
{
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // 初始化逻辑
    }
}
```

---

### 1.3 Pawn
| 属性 | 类型 | 说明 |
|------|------|------|
| needs | Pawn_NeedsTracker | 需求追踪器 |
| health | Pawn_HealthTracker | 健康追踪器 |
| skills | Pawn_SkillTracker | 技能追踪器 |

**使用例子**：
```csharp
Pawn p = thing as Pawn;
float hungerLevel = p.needs.food.CurLevel;
```

---

## 2. UI Gizmo 相关

### 2.1 Gizmo
| 属性 | 说明 |
|------|------|
| Order | 在UI中的绘制顺序 |
| Key | 快捷键 |

| 方法 | 说明 |
|------|------|
| ProcessInput | 处理输入 |
| GizmoOnGUI | 绘制Gizmo |

---

### 2.2 Command_Action (常用Gizmo子类)
| 构造参数 | 说明 |
|---------|------|
| defaultLabel | 按钮显示文字 |
| defaultDesc | 鼠标悬停提示 |
| icon | 按钮图标纹理 |
| action | 点击回调 |

**使用例子**：
```csharp
yield return new Command_Action()
{
    defaultLabel = "我的功能",
    defaultDesc = "功能描述",
    icon = ContentFinder<Texture2D>.Get("UI/Commands/MyIcon"),
    action = () => DoSomething()
};
```

---

## 3. XML数据定义相关

### 3.1 ThingDef
| 字段 | 类型 | 说明 |
|------|------|------|
| defName | string | 定义的唯一ID |
| label | string | 显示名称 |
| description | string | 描述文本 |
| thingClass | string | 对应的C#类 |
| comps | List<CompProperties> | 挂载的Comp列表 |

---

## 4. 数据保存相关

### 4.1 Scribe (序列化)
| 方法 | 说明 |
|------|------|
| Scribe_Values.Look | 保存基础类型（int, float, bool等） |
| Scribe_References.Look | 保存引用类型（Pawn, Thing等） |
| Scribe_Collections.Look | 保存集合类型（List等） |

**使用例子**：
```csharp
public override void ExposeData()
{
    base.ExposeData();
    Scribe_Values.Look(ref myInt, "myInt", 0);
    Scribe_References.Look(ref myPawn, "myPawn");
}
```

---

## 已验证的API真实性
- ✓ ThingComp 基类及主要方法（RimWorld v1.6+）
- ✓ Gizmo 及 Command_Action（已测试）
- ✓ Scribe 序列化接口（稳定）

## 待补充
- [ ] 特殊效果API (Effecter相关)
- [ ] 声音系统API
- [ ] 动画系统API

---

### 历史记录
| 版本 | 改动内容 | 修改时间 | 修改者 |
|------|---------|---------|--------|
| v1.0 | 初版发布：RimWorld API参考模板，包含核心类、Comp系统、Gizmo等关键API | 2026-01-04 | knowledge-refiner |
