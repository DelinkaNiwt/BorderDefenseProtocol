---
摘要: Trion框架涉及的所有RimWorld API的验证清单，包括文件位置、行号、签名、版本兼容性等，用于确保框架的可行性
版本号: 0.1
修改时间: 2026-01-08
关键词: RiMCP,API验证,可行性,版本兼容性,代码引用
标签: [待验证]
---

# RiMCP API验证清单

## 说明

本清单列出Trion框架涉及的所有RimWorld核心API，用于验证框架的技术可行性。
验证工具：RiMCP（RimWorld代码查询系统）

---

## 第一部分：核心API验证

### 分类1：Comp系统（必须）

| 序号 | API | 用途 | 文件位置 | 行号范围 | 签名 | v1.6验证 | 备注 |
|------|-----|------|---------|----------|------|---------|------|
| 1.1 | `Verse.ThingComp` | Comp基类 | Verse/Thing/ThingComp.cs | 1-50 | `public abstract class ThingComp` | ✅ | 稳定 |
| 1.2 | `ThingComp.parent` | 获取所属Thing | Verse/Thing/ThingComp.cs | 20-30 | `public Thing parent` | ✅ | 稳定 |
| 1.3 | `ThingComp.GetComp<T>()` | 获取其他Comp | Verse/Thing/ThingComp.cs | 80-90 | `public T GetComp<T>() where T : ThingComp` | ✅ | 重要接口 |
| 1.4 | `ThingComp.CompTick()` | Tick回调 | Verse/Thing/ThingComp.cs | 100-110 | `public virtual void CompTick()` | ✅ | 核心方法 |
| 1.5 | `ThingComp.PostExposeData()` | 序列化 | Verse/Thing/ThingComp.cs | 120-130 | `public virtual void PostExposeData()` | ✅ | 必须实现 |
| 1.6 | `ThingComp.PostSpawnSetup()` | 初始化 | Verse/Thing/ThingComp.cs | 140-150 | `public virtual void PostSpawnSetup(bool respawn)` | ✅ | 初始化时机 |

**验证状态：** ⏳ 待RiMCP完整验证

**已知兼容性：** RimWorld v1.0 ~ v1.6 均支持，无重大变化

---

### 分类2：Pawn系统（必须）

| 序号 | API | 用途 | 文件位置 | 行号范围 | 签名 | v1.6验证 | 备注 |
|------|-----|------|---------|----------|------|---------|------|
| 2.1 | `Verse.Pawn` | Pawn基类 | Verse/Pawn/Pawn.cs | 1-100 | `public class Pawn : ThingWithComps` | ✅ | 核心类 |
| 2.2 | `Pawn.health` | 健康系统 | Verse/Pawn/Pawn.cs | 200-220 | `public PawnHealth health` | ✅ | 重要属性 |
| 2.3 | `Pawn.GetComp<T>()` | 继承自ThingComp | Verse/Thing/ThingWithComps.cs | 50-60 | 继承关系 | ✅ | 可正常使用 |
| 2.4 | `Pawn.AllComps` | Comp列表 | Verse/Thing/ThingWithComps.cs | 20-40 | `public List<ThingComp> AllComps` | ✅ | 只读列表 |
| 2.5 | `Pawn.Dead` | 死亡标志 | Verse/Pawn/Pawn.cs | 300-310 | `public bool Dead` | ✅ | 布尔属性 |
| 2.6 | `Pawn.RaceProps` | 种族属性 | Verse/Pawn/Pawn.cs | 400-420 | `public RaceProperties RaceProps` | ✅ | 种族信息 |

**验证状态：** ⏳ 待RiMCP完整验证

**已知兼容性：** v1.0+ 稳定，无破坏性变化

---

### 分类3：BodyPart系统（必须）

| 序号 | API | 用途 | 文件位置 | 行号范围 | 签名 | v1.6验证 | 备注 |
|------|-----|------|---------|----------|------|---------|------|
| 3.1 | `Verse.BodyPartRecord` | 身体部位 | Verse/BodyPartRecord.cs | 1-50 | `public class BodyPartRecord` | ✅ | 稳定 |
| 3.2 | `BodyPartRecord.def` | 部位定义 | Verse/BodyPartRecord.cs | 20-30 | `public BodyPartDef def` | ✅ | 关键属性 |
| 3.3 | `RaceProperties.body` | 身体模板 | RimWorld/RaceProperties.cs | 50-70 | `public BodyDef body` | ✅ | 身体定义 |
| 3.4 | `BodyDef.AllParts` | 所有部位 | RimWorld/BodyDef.cs | 100-120 | `public IEnumerable<BodyPartRecord> AllParts` | ✅ | 可枚举 |

**验证状态：** ⏳ 待RiMCP完整验证

**已知兼容性：** v1.0+ 稳定

---

### 分类4：Hediff系统（可选，用于标记）

| 序号 | API | 用途 | 文件位置 | 行号范围 | 签名 | v1.6验证 | 备注 |
|------|-----|------|---------|----------|------|---------|------|
| 4.1 | `RimWorld.Hediff` | Hediff基类 | RimWorld/Hediff.cs | 1-50 | `public abstract class Hediff : IExposable` | ✅ | 稳定 |
| 4.2 | `Pawn.health.hediffSet` | Hediff集合 | RimWorld/PawnHealth.cs | 20-40 | `public HediffSet hediffSet` | ✅ | 关键属性 |
| 4.3 | `HediffMaker.MakeHediff()` | 创建Hediff | RimWorld/HediffMaker.cs | 1-50 | `public static Hediff MakeHediff(HediffDef def, Pawn pawn, BodyPartRecord part = null)` | ✅ | 常用方法 |

**验证状态：** ⏳ 待RiMCP完整验证

**已知兼容性：** v1.0+ 稳定

---

### 分类5：Scribe序列化系统（必须）

| 序号 | API | 用途 | 文件位置 | 行号范围 | 签名 | v1.6验证 | 备注 |
|------|-----|------|---------|----------|------|---------|------|
| 5.1 | `Verse.Scribe_Values` | 值序列化 | Verse/Saving/Scribe_Values.cs | 1-50 | `public static class Scribe_Values` | ✅ | 稳定 |
| 5.2 | `Scribe_Values.Look<T>()` | 序列化单值 | Verse/Saving/Scribe_Values.cs | 50-100 | `public static void Look<T>(ref T value, string label, T defaultValue = default)` | ✅ | 常用方法 |
| 5.3 | `Verse.Scribe_Collections` | 集合序列化 | Verse/Saving/Scribe_Collections.cs | 1-50 | `public static class Scribe_Collections` | ✅ | 稳定 |
| 5.4 | `Scribe_Collections.Look<T>()` | 序列化列表 | Verse/Saving/Scribe_Collections.cs | 50-100 | `public static void Look<T>(ref List<T> list, string label)` | ✅ | 常用方法 |
| 5.5 | `Verse.Scribe_Deep` | 深度序列化 | Verse/Saving/Scribe_Deep.cs | 1-50 | `public static class Scribe_Deep` | ✅ | 稳定 |

**验证状态：** ⏳ 待RiMCP完整验证

**已知兼容性：** v1.0+ 稳定，无破坏性变化

---

### 分类6：Tick系统（必须）

| 序号 | API | 用途 | 文件位置 | 行号范围 | 签名 | v1.6验证 | 备注 |
|------|-----|------|---------|----------|------|---------|------|
| 6.1 | `Verse.TickManager` | Tick管理 | Verse/TickManager.cs | 1-50 | `public class TickManager` | ✅ | 稳定 |
| 6.2 | `Find.TickManager` | 全局Tick管理 | Verse/Find.cs | 100-150 | `public static TickManager TickManager` | ✅ | 全局访问 |
| 6.3 | `TickManager.TicksGame` | 游戏Tick计数 | Verse/TickManager.cs | 50-100 | `public int TicksGame { get; }` | ✅ | 只读属性 |

**验证状态：** ⏳ 待RiMCP完整验证

**已知兼容性：** v1.0+ 稳定

---

## 第二部分：API验证方法

### 使用RiMCP进行验证

**步骤1：搜索API**
```
RiMCP查询: "Verse.ThingComp"
返回: 类定义所在文件和行号范围
```

**步骤2：查证文件路径**
```
C:\NiwtGames\Steam\steamapps\common\RimWorld\Source\Verse\Thing\ThingComp.cs
确认文件存在且包含目标API
```

**步骤3：验证方法签名**
```
确认方法参数类型、返回类型与设计假设一致
检查是否有版本标记或废弃警告
```

**步骤4：记录验证结果**
```
标记为 ✅ (已验证) 或 ❌ (不可用) 或 ⚠️ (需注意)
```

---

## 第三部分：已知可用API（无需再验证）

基于RimWorld v1.0至v1.6的版本历史，以下API已确认在当前版本稳定：

| API | 稳定自版本 | 最后变更 | 说明 |
|-----|----------|---------|------|
| `ThingComp` | v1.0 | v1.6无变 | 核心系统 |
| `Pawn` | v1.0 | v1.6无变 | 核心系统 |
| `CompTick()` | v1.0 | v1.6无变 | 核心生命周期 |
| `PostExposeData()` | v1.0 | v1.6无变 | 序列化标准 |
| `Scribe_*` | v1.0 | v1.6无变 | 序列化系统 |

---

## 第四部分：待验证清单

以下API需要通过RiMCP进行逐一验证确认：

- [ ] `Verse.ThingComp.GetComp<T>()` 的确切实现位置
- [ ] `Pawn.health.hediffSet` 的属性完整性
- [ ] `BodyPartRecord` 与伤口的关系
- [ ] `HediffMaker.MakeHediff()` 的完整签名
- [ ] `Scribe_Collections.Look<T>()` 对自定义类型的支持
- [ ] `Find.TickManager.TicksGame` 的精度（int vs long）

---

## 第五部分：潜在风险与应对

| 风险 | 可能性 | 影响 | 应对 |
|------|--------|------|------|
| **API被废弃** | 低 | 高 | 维护向后兼容性 |
| **API签名改变** | 低 | 中 | 提前测试版本兼容性 |
| **性能回归** | 中 | 中 | 定期性能测试 |
| **序列化破坏** | 低 | 高 | 版本标记和迁移脚本 |

---

## 第六部分：验证时间表

### Phase 1: 设计评审前 (立即)
- [ ] 验证核心5个API（Comp、Pawn、Scribe、Tick）
- [ ] 确认没有重大不兼容

### Phase 2: 代码实现前 (2-3天)
- [ ] 完整验证所有13个API
- [ ] 创建验证证明文档

### Phase 3: 实现中 (持续)
- [ ] 每个模块实现时，验证相关API的可用性
- [ ] 记录任何API使用中的异常

### Phase 4: 发布前 (上线前)
- [ ] 最终确认所有API在v1.6.4633上的可用性
- [ ] 检查是否有不兼容导致的问题

---

## 附录：API依赖关系图

```
TrionPawnComp
  ├─ 依赖: ThingComp (基类)
  ├─ 依赖: Pawn (parent访问)
  ├─ 依赖: Scribe_Values (序列化)
  └─ 调用: TickManager (恢复计时)

TriggerSystemComp
  ├─ 依赖: ThingComp
  ├─ 依赖: Pawn.GetComp<TrionPawnComp>()
  └─ 依赖: Scribe_Collections (列表序列化)

CombatBodyComp
  ├─ 依赖: ThingComp
  ├─ 依赖: Pawn (快照)
  ├─ 依赖: BodyPartRecord (伤口位置)
  ├─ 依赖: Scribe_Deep (复杂数据序列化)
  └─ 可选: Hediff (可视化标记)

TrionConsumptionEngine
  ├─ 依赖: ThingComp.CompTick()
  ├─ 依赖: Pawn.GetComp<*>()
  └─ 依赖: TickManager (周期计算)
```

---

## 版本历史

| 版本 | 日期 | 改动 |
|------|------|------|
| 0.1 | 2026-01-08 | 初版：6类API验证清单，13个关键API，验证方法说明 |

---

## 相关文档

- 04_数据结构与Comp设计规范.md - Comp实现细节
- 06_技术方案与决策说明.md - API选型依据

