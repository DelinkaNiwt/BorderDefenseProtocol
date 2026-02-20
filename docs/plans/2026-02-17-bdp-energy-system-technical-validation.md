# Trion能量系统技术校验实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 对6.1_Trion能量系统详细设计文档中涉及的所有RimWorld框架API调用进行技术校验，确认API存在性、用法正确性、参数合法性

**架构:** 采用分层校验策略——先验证基础类（ThingComp/Gene/Need/Hediff），再验证Stat系统，最后验证生命周期方法和存档系统。每个校验点包含：API定位、签名验证、用法示例查找

**技术栈:** RimWorld 1.6.4633源码、RimSearcher MCP工具、rimworld-code-rag-mods MCP工具

---

## 校验范围总览

设计文档中涉及的关键API调用点：

| 层级 | 组件 | 关键API | 优先级 |
|------|------|---------|--------|
| 核心层 | ThingComp | PostSpawnSetup, CompTick, PostExposeData | P0 |
| Pawn适配层 | Gene | PostAdd, PostRemove, statOffsets机制 | P0 |
| Pawn适配层 | Need | CurLevel, IsFrozen, NeedInterval | P0 |
| Pawn适配层 | Hediff | Severity, stages, capMods | P0 |
| Stat聚合层 | StatDef | GetStatValue, StatWorker聚合 | P0 |
| 存档系统 | Scribe | Scribe_Values.Look, LoadSaveMode | P1 |
| 工具类 | GenTicks | TicksGame, TICKS_PER_DAY | P1 |

---

## Task 1: 校验ThingComp基础类API

**目标:** 验证CompTrion继承的ThingComp基类及其生命周期方法

**Files:**
- Reference: `临时\RimWT\第六阶段：深度系统设计\6.1_Trion能量系统详细设计.md:386-432`
- Output: `docs\validation\trion-thingcomp-validation.md`

**Step 1: 定位ThingComp类定义**

使用RimSearcher定位ThingComp类：
```bash
# 预期找到Verse.ThingComp类
```

**Step 2: 验证PostSpawnSetup方法签名**

查找PostSpawnSetup方法：
- 预期签名: `public virtual void PostSpawnSetup(bool respawningAfterLoad)`
- 验证参数: respawningAfterLoad用于区分首次生成和读档

**Step 3: 验证CompTick方法**

查找CompTick方法：
- 预期签名: `public virtual void CompTick()`
- 验证调用频率: 每游戏tick调用一次

**Step 4: 验证PostExposeData方法**

查找PostExposeData方法：
- 预期签名: `public virtual void PostExposeData()`
- 验证用途: 存档序列化

**Step 5: 验证parent属性**

查找parent属性：
- 预期类型: `Thing parent`
- 验证用法: 可转换为Pawn、Building等子类

**Step 6: 记录校验结果**

创建校验报告文档，记录：
- API存在性 ✓/✗
- 签名匹配度
- 用法示例引用
- 潜在问题

---

## Task 2: 校验Gene类API和statOffsets机制

**目标:** 验证Gene_TrionGland的继承基类和Stat贡献机制

**Files:**
- Reference: `临时\RimWT\第六阶段：深度系统设计\6.1_Trion能量系统详细设计.md:438-491`
- Output: `docs\validation\trion-gene-validation.md`

**Step 1: 定位Gene基类**

使用RimSearcher定位Gene类：
- 预期命名空间: `RimWorld.Gene`
- 验证是否有PostAdd/PostRemove虚方法

**Step 2: 验证Gene_Resource类（确认不继承）**

查找Gene_Resource类：
- 确认其持有cur/max字段
- 验证设计决策D5的理由（避免两份数据源）

**Step 3: 验证PostAdd方法**

查找Gene.PostAdd方法：
- 预期签名: `public virtual void PostAdd()`
- 查找社区模组中的使用示例

**Step 4: 验证PostRemove方法**

查找Gene.PostRemove方法：
- 预期签名: `public virtual void PostRemove()`
- 验证调用时机

**Step 5: 验证statOffsets机制**

查找GeneDef中的statOffsets字段：
- 预期类型: `List<StatModifier>`
- 验证Stat系统如何读取Gene的statOffsets
- 查找社区模组示例

**Step 6: 验证pawn属性访问**

确认Gene类有pawn属性：
- 预期类型: `Pawn pawn`
- 验证用法: `pawn.GetComp<CompTrion>()`

**Step 7: 记录校验结果**

更新校验报告，记录Gene相关API验证结果

---

## Task 3: 校验Need类API

**目标:** 验证Need_Trion的继承基类和需求系统集成

**Files:**
- Reference: `临时\RimWT\第六阶段：深度系统设计\6.1_Trion能量系统详细设计.md:493-597`
- Output: `docs\validation\trion-need-validation.md`

**Step 1: 定位Need基类**

使用RimSearcher定位Need类：
- 预期命名空间: `RimWorld.Need`
- 验证是否为抽象类

**Step 2: 验证CurLevel属性**

查找CurLevel属性：
- 预期签名: `public abstract float CurLevel { get; set; }`
- 验证是否可重写get/set

**Step 3: 验证MaxLevel属性**

查找MaxLevel属性：
- 预期签名: `public virtual float MaxLevel { get; }`
- 验证默认值

**Step 4: 验证IsFrozen属性**

查找IsFrozen属性：
- 预期签名: `public virtual bool IsFrozen { get; }`
- 验证冻结机制如何工作

**Step 5: 验证NeedInterval方法**

查找NeedInterval方法：
- 预期签名: `public virtual void NeedInterval()`
- 验证调用频率: 150 ticks
- 查找RimWorld源码中的调用点

**Step 6: 验证NeedDef配置**

查找NeedDef类：
- 验证showOnNeedList字段
- 验证threshPercents字段
- 查找社区模组的NeedDef XML示例

**Step 7: 记录校验结果**

更新校验报告，记录Need相关API验证结果

---

## Task 4: 校验Hediff类API和stages机制

**目标:** 验证Hediff_TrionDepletion的分级效果系统

**Files:**
- Reference: `临时\RimWT\第六阶段：深度系统设计\6.1_Trion能量系统详细设计.md:599-666`
- Output: `docs\validation\trion-hediff-validation.md`

**Step 1: 定位HediffWithComps类**

使用RimSearcher定位HediffWithComps：
- 预期命名空间: `Verse.HediffWithComps`
- 验证继承关系: Hediff -> HediffWithComps

**Step 2: 验证Severity属性**

查找Severity属性：
- 预期类型: `float Severity { get; set; }`
- 验证范围: 通常0.0-1.0

**Step 3: 验证HediffDef.stages配置**

查找HediffDef类：
- 验证stages字段类型: `List<HediffStage>`
- 验证minSeverity字段

**Step 4: 验证capMods机制**

查找HediffStage.capMods：
- 预期类型: `List<PawnCapacityModifier>`
- 验证capacity枚举值: Moving, Manipulation, Consciousness
- 验证offset字段

**Step 5: 验证statOffsets机制**

查找HediffStage.statOffsets：
- 预期类型: `List<StatModifier>`
- 验证如何影响Stat系统

**Step 6: 验证Hediff添加/移除API**

查找Pawn.health.hediffSet相关方法：
- `AddHediff(HediffDef def)` 方法
- `GetFirstHediffOfDef(HediffDef def)` 方法
- `RemoveHediff(Hediff hediff)` 方法

**Step 7: 记录校验结果**

更新校验报告，记录Hediff相关API验证结果

---

## Task 5: 校验Stat系统API

**目标:** 验证自定义StatDef和多源聚合机制

**Files:**
- Reference: `临时\RimWT\第六阶段：深度系统设计\6.1_Trion能量系统详细设计.md:670-752`
- Output: `docs\validation\trion-stat-validation.md`

**Step 1: 定位StatDef类**

使用RimSearcher定位StatDef：
- 预期命名空间: `RimWorld.StatDef`
- 验证XML配置字段

**Step 2: 验证GetStatValue方法**

查找Pawn.GetStatValue方法：
- 预期签名: `float GetStatValue(StatDef stat, bool applyPostProcess = true)`
- 验证缓存机制

**Step 3: 验证StatWorker聚合流程**

查找StatWorker类：
- 定位GetValueUnfinalized方法
- 验证statOffsets聚合逻辑
- 验证statFactors聚合逻辑

**Step 4: 验证defaultBaseValue字段**

查找StatDef.defaultBaseValue：
- 验证类型: float
- 验证作用: 聚合起点

**Step 5: 验证category字段**

查找StatCategoryDef：
- 验证BasicsPawn类别
- 查找其他可用类别

**Step 6: 验证toStringStyle字段**

查找ToStringStyle枚举：
- 验证FloatOne选项
- 查找其他格式选项

**Step 7: 查找社区模组自定义Stat示例**

使用rimworld-code-rag-mods搜索：
- 搜索关键词: "StatDef" "statOffsets"
- 找到3-5个实际使用示例
- 验证设计文档中的用法是否符合惯例

**Step 8: 记录校验结果**

更新校验报告，记录Stat系统验证结果

---

## Task 6: 校验存档系统API

**目标:** 验证Scribe存档序列化机制

**Files:**
- Reference: `临时\RimWT\第六阶段：深度系统设计\6.1_Trion能量系统详细设计.md:986-1026`
- Output: `docs\validation\trion-scribe-validation.md`

**Step 1: 定位Scribe_Values类**

使用RimSearcher定位Scribe_Values：
- 预期命名空间: `Verse.Scribe_Values`
- 验证静态类

**Step 2: 验证Look方法签名**

查找Scribe_Values.Look方法：
- 预期签名: `Look<T>(ref T value, string label, T defaultValue = default)`
- 验证泛型支持: float, bool

**Step 3: 验证Scribe.mode枚举**

查找LoadSaveMode枚举：
- 验证PostLoadInit值
- 验证Saving值
- 验证LoadingVars值

**Step 4: 查找社区模组存档示例**

搜索PostExposeData实现示例：
- 找到3-5个ThingComp的存档实现
- 验证读档后校验模式

**Step 5: 记录校验结果**

更新校验报告，记录存档系统验证结果

---

## Task 7: 校验工具类API

**目标:** 验证GenTicks等工具类

**Files:**
- Reference: `临时\RimWT\第六阶段：深度系统设计\6.1_Trion能量系统详细设计.md:407-414`
- Output: `docs\validation\trion-utils-validation.md`

**Step 1: 定位GenTicks类**

使用RimSearcher定位GenTicks：
- 验证TicksGame属性
- 验证类型: int

**Step 2: 验证TICKS_PER_DAY常量**

查找时间常量定义：
- 预期值: 60000
- 查找定义位置（可能在GenDate或其他类）

**Step 3: 验证Mathf工具类**

确认Unity Mathf类可用：
- Min方法
- Max方法
- Clamp方法

**Step 4: 验证GetComp泛型方法**

查找Thing.GetComp方法：
- 预期签名: `T GetComp<T>() where T : ThingComp`
- 验证返回null的情况

**Step 5: 记录校验结果**

更新校验报告，记录工具类验证结果

---

## Task 8: 汇总校验报告

**目标:** 生成完整的技术校验报告

**Files:**
- Input: `docs\validation\trion-*-validation.md`
- Output: `docs\validation\trion-system-validation-summary.md`

**Step 1: 汇总所有校验结果**

整合各子报告：
- API存在性统计
- 签名匹配度统计
- 发现的问题列表

**Step 2: 识别设计文档中的错误**

列出需要修正的设计点：
- API签名不匹配
- 参数类型错误
- 调用方式不符合框架惯例

**Step 3: 提出设计改进建议**

基于源码分析提出优化：
- 更符合RimWorld惯例的实现方式
- 性能优化建议
- 社区最佳实践参考

**Step 4: 生成修订清单**

为设计文档生成修订TODO：
- 需要修改的章节
- 需要补充的API说明
- 需要调整的代码示例

**Step 5: 提交校验报告**

```bash
git add docs/validation/
git commit -m "docs: complete technical validation for Trion energy system design"
```

---

## 预期产出

1. **分项校验报告** (7个文件)
   - trion-thingcomp-validation.md
   - trion-gene-validation.md
   - trion-need-validation.md
   - trion-hediff-validation.md
   - trion-stat-validation.md
   - trion-scribe-validation.md
   - trion-utils-validation.md

2. **汇总报告** (1个文件)
   - trion-system-validation-summary.md

3. **设计文档修订清单**
   - 待修正的API调用
   - 待补充的说明
   - 待调整的代码示例

---

## 校验工具使用指南

### RimSearcher MCP工具

```bash
# 定位类/方法
mcp__RimSearcher__rimworld-searcher__locate "ClassName"

# 查看类详情和继承关系
mcp__RimSearcher__rimworld-searcher__inspect "ClassName"

# 查找方法实现
mcp__RimSearcher__rimworld-searcher__read_code --path="..." --methodName="MethodName"

# 查找子类
mcp__RimSearcher__rimworld-searcher__trace --symbol="BaseClass" --mode="inheritors"

# 查找引用
mcp__RimSearcher__rimworld-searcher__trace --symbol="ClassName" --mode="usages"
```

### rimworld-code-rag-mods MCP工具

```bash
# 搜索社区模组代码示例
mcp__rimworld-code-rag-mods__rough_search --query="statOffsets Gene" --max_results=20

# 查看具体符号实现
mcp__rimworld-code-rag-mods__get_item --symbol="ClassName"

# 查找依赖关系
mcp__rimworld-code-rag-mods__get_uses --symbol="ClassName"
```

---

## 注意事项

1. **优先使用RimSearcher查找官方源码**，确保API来自官方框架
2. **使用rimworld-code-rag-mods查找社区实践**，验证用法惯例
3. **记录所有不匹配项**，即使是小的签名差异
4. **保留源码引用**，在报告中标注文件路径和行号
5. **区分"不存在"和"签名不匹配"**，前者是严重问题，后者可能只需调整

---
