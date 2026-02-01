# Altered Carbon 2 - 快照与数据恢复系统分析

**分析日期**: 2026-01-24
**来源**: Altered Carbon 2 Re-Sleeved (副本2重生) 模组
**核心类文件**:
- `NeuralData.cs` (2019行) - 神经数据存储
- `Recipe_InstallNeuralStack.cs` (297行) - 神经堆栈应用逻辑
- `Hediff_NeuralStack.cs` - 神经堆栈Hediff实现

**对RimTrion Step 4的参考价值**: ⭐⭐⭐⭐⭐ (高度相关)

---

## 第1部分：Altered Carbon的架构设计

### 1.1 核心概念对标

| 概念 | Altered Carbon | RimTrion | 参考意义 |
|------|-----------------|----------|---------|
| **意识/数据容器** | NeuralStack (物品) | TrionSnapshot (数据结构) | 物品vs数据，各有优劣 |
| **数据保存结构** | NeuralData (2000行大类) | ? (待设计) | 完整性参考 |
| **应用载体** | Hediff_NeuralStack | Hediff_TrionBody | 一致的设计路线 |
| **回滚方式** | OverwritePawn() | ? | 核心实现方法 |
| **虚拟体表示** | 同一Pawn修改属性 | 同一Pawn修改属性 | 一致 |

---

## 第2部分：Altered Carbon的NeuralData结构 (关键发现)

### 2.1 NeuralData包含的数据项 (共140+个字段)

**A. 身份与基础信息** (14项)
```csharp
public Name name;                          // 姓名
public PawnKindDef kindDef;               // pawn类型定义
public Pawn hostPawn;                     // 宿主pawn引用
public long ageBiologicalTicks;           // 生物年龄(ticks)
public long ageChronologicalTicks;        // 历法年龄(ticks)
public Gender originalGender;             // 原始性别
public ThingDef originalRace;             // 原始种族
public XenotypeDef originalXenotypeDef;   // 原始仙族型
public int pawnID;                        // Pawn ID
public bool isFactionLeader;              // 是否是派系领导
public Faction faction;                   // 派系归属
public bool everSeenByPlayer;             // 是否被玩家看过
```

**B. 技能与背景** (7项)
```csharp
public List<SkillRecord> skills;          // 所有技能列表
public BackstoryDef childhood;            // 儿童背景故事
public BackstoryDef adulthood;            // 成人背景故事
public List<Trait> traits;                // 所有特性列表
public List<Thought_Memory> thoughts;     // 所有记忆思想
public DefMap<RecordDef, float> records;  // 所有记录(kill count等)
```

**C. 社交与关系** (9项)
```csharp
public List<DirectPawnRelation> relations;           // 直接关系列表
public Dictionary<DirectPawnRelation, Pawn> otherPawnRelations;  // 关系映射
public List<Pawn> relatedPawns;           // 相关pawn列表
public List<RoyalTitle> royalTitles;      // 皇族头衔列表
public Dictionary<Faction, int> favor;    // 各派系好感值
public Dictionary<Faction, Pawn> heirs;   // 各派系继承人
public List<Thing> bondedThings;          // 绑定物品列表
```

**D. 管理与工作** (10项)
```csharp
public Dictionary<WorkTypeDef, int> priorities;     // 工作优先级字典
public HostilityResponseMode hostilityMode;         // 敌意响应模式
public MedicalCareCategory medicalCareCategory;     // 医疗护理类别
public bool selfTend;                               // 自我救治
public List<TimeAssignmentDef> times;              // 时间分配列表
public FoodPolicy foodPolicy;              // 饮食策略
public ApparelPolicy apparelPolicy;        // 服装策略
public DrugPolicy drugPolicy;              // 毒品策略
public GuestStatus guestStatusInt;         // 客人状态
public PrisonerInteractionModeDef interactionMode;  // 囚犯交互模式
```

**E. 健康与Hediff** (14项)
```csharp
public List<Hediff> savedHediffs;         // 保存的病症列表
public float stackDegradation;            // 堆栈退化程度 [0-1]
public float stackDegradationToAdd;       // 待增加的退化
public bool? diedFromCombat;              // 是否死于战斗
public bool isCopied;                     // 是否为副本
public int? lastTimeBackedUp;             // 上次备份时间
public int editTime;                      // 编辑时间
public bool limitEntropyAmount;           // 限制熵值
public bool canGetRescuedThought;         // 能否获得救援思想
public Pawn relativeInvolvedInRescueQuest;// 参与救援任务的亲戚
public MarriageNameChange nextMarriageNameChange;  // 下次婚名变更
public bool hidePawnRelations;            // 隐藏pawn关系
public Battle battleActive;               // 当前活跃战斗
```

**F. 意识形态与信仰** (7项)
```csharp
public Ideo ideo;                         // 意识形态
public ColorDef favoriteColor;            // 最喜爱颜色
public float certainty;                   // 确定性 [0-1]
public Precept_RoleMulti precept_RoleMulti;      // 多角色教义
public Precept_RoleSingle precept_RoleSingle;    // 单角色教义
public List<Ideo> previousIdeos;          // 之前的意识形态列表
```

**G. 特殊能力与增强** (6项)
```csharp
public List<AbilityDef> abilities;        // 能力列表
public List<AbilityDef> VEAbilities;      // 原版灵能扩展能力
public Hediff VPE_PsycastAbilityImplant;  // 灵能能力植入物
public int? psylinkLevel;                 // 心灵链接等级
public float currentPsyfocus;             // 当前心灵焦点
public float targetPsyfocus;              // 目标心灵焦点
```

**H. 高级状态** (8项)
```csharp
public bool recruitable;                  // 可招募
public float resistance;                  // 抵抗力 [-1=自动]
public float will;                        // 意志 [-1=自动]
public JoinStatus joinStatus;             // 加入状态
public SlaveInteractionModeDef slaveInteractionMode;  // 奴隶交互模式
public Faction hostFactionInt;            // 宿主派系
public Faction slaveFactionInt;           // 奴隶派系
public bool everParticipatedInPrisonBreak;// 曾参与越狱
```

**I. 模组兼容性** (3项)
```csharp
public List<ModCompatibilityEntry> modCompatibilityEntries;  // 模组兼容性条目
public ThingDef sourceStack;              // 来源堆栈定义
public NeuralData neuralDataRewritten;    // 重写的神经数据
```

### 2.2 关键设计特点

#### ✅ 优势分析

1. **数据完整性** (140+字段)
   - 覆盖了RimWorld pawn的几乎所有信息
   - 包括关系、思想、能力等隐藏属性
   - 支持模组兼容性扩展

2. **分离保存策略**
   - 数据保存为独立对象（不依赖Pawn活跃）
   - 可作为物品（NeuralStack）在游戏中流通
   - 支持多个副本存在

3. **备份与版本控制**
   - `lastTimeBackedUp` 字段跟踪备份时间
   - `stackDegradation` 模拟数据衰退
   - `isCopied` 标记数据是否为副本

4. **灵活的应用流程**
   - `OverwritePawn(pawn)` 方法完全覆盖目标pawn的属性
   - `CopyFromPawn(pawn)` 方法从pawn提取全量数据
   - 支持空白躯体和有人躯体的两种情况

#### ⚠️ 复杂性问题

1. **代码规模** (2019行)
   - 单一文件极度庞大
   - 难以维护和扩展
   - 模块边界不清

2. **存读档复杂度**
   - 140+字段的ExposeData()逻辑
   - 版本升级时容易出现不兼容
   - 需要详细的字段映射

3. **性能开销**
   - 创建新NeuralData时需要遍历pawn的所有数据
   - OverwritePawn时需要修改大量属性

---

## 第3部分：核心应用流程分析

### 3.1 安装神经堆栈的完整流程 (ApplyNeuralStack方法)

```
[Step 1] 创建新的Hediff_NeuralStack
    HediffMaker.MakeHediff(stackHediff, pawn) → Hediff_NeuralStack

[Step 2] 检查神经数据有效性
    if (neuralStack.NeuralData.ContainsData) → 是否有数据

[Step 3] 处理非空躯体情况 (关键！)
    if (!pawn.IsEmptySleeve()) {
        ① 创建虚拟Pawn：new NeuralData().CopyFromPawn(pawn, ...)
        ② 生成虚拟Pawn实体：dummyPawn = neuralData4.DummyPawn
        ③ 生成虚拟Pawn尸体：GenSpawn.Spawn(dummyPawn)
        ④ 杀死虚拟Pawn并清理：dummyPawn.Kill() + dummyPawn.Corpse.DeSpawn()
        ⑤ [重要] 完全覆盖原Pawn属性：neuralData3.OverwritePawn(pawn)
    } else {
        ① 恢复空白躯体：pawn.UndoEmptySleeve()
    }

[Step 4] 应用心理效应
    ApplyMindEffects(pawn, hediff)
        ├─ ApplyStackDegradation(...) → 应用堆栈退化Hediff
        ├─ ApplyThoughts(...) → 添加记忆和思想
        ├─ Hediff("SleeveShock") → 脱壳冲击
        └─ needs.AddOrRemoveNeedsAsAppropriate() → 更新需求

[Step 5] 添加Hediff到Pawn
    pawn.health.AddHediff(hediff_NeuralStack, part)

[Step 6] 清理与记录
    neuralStack.DestroyNoKill() → 销毁来源物品
    HistoryEventRecorder → 记录历史事件
```

### 3.2 关键方法说明

#### A. OverwritePawn(pawn) - 核心回滚逻辑

```csharp
// 伪代码概览
public void OverwritePawn(Pawn pawn) {
    // 更新基础属性
    pawn.Name = name;
    pawn.kindDef = kindDef;

    // 更新年龄
    pawn.ageTracker.AgeBiologicalTicks = ageBiologicalTicks;
    pawn.ageTracker.AgeChronologicalTicks = ageChronologicalTicks;

    // 更新技能
    foreach (var skill in skills) {
        pawn.skills.Learn(skill.def, skill.xpSinceLastLevel);
    }

    // 更新特性
    pawn.story.traits.Clear();
    foreach (var trait in traits) {
        pawn.story.traits.GainTrait(trait);
    }

    // 更新关系
    foreach (var relation in relations) {
        pawn.relations.AddDirectRelation(relation);
    }

    // 更新工作优先级
    foreach (var priority in priorities) {
        pawn.workSettings.SetPriority(priority.Key, priority.Value);
    }

    // 更新Hediff
    SetHediffs(pawn);

    // ... 更新140+个其他字段
}
```

#### B. SetHediffs(pawn) - Hediff恢复方法

```csharp
private void SetHediffs(Pawn pawn) {
    // 清空所有Hediff
    pawn.health.RemoveAllHediffs();

    // 恢复保存的Hediff列表
    foreach (var hediff in savedHediffs) {
        Hediff newHediff = MakeCopy(hediff, pawn);
        pawn.health.AddHediff(newHediff);
    }
}

private Hediff MakeCopy(Hediff hediff, Pawn pawn) {
    // 关键技巧：不复制引用，只复制数据
    Hediff newHediff = HediffMaker.MakeHediff(hediff.def, pawn);
    newHediff.severity = hediff.severity;
    newHediff.ageTicks = hediff.ageTicks;
    // ... 复制其他属性
    return newHediff;
}
```

#### C. CopyFromPawn(pawn) - 数据提取方法

```csharp
public void CopyFromPawn(Pawn pawn) {
    // 提取身份信息
    name = pawn.Name;
    kindDef = pawn.kindDef;

    // 提取属性列表
    traits = new List<Trait>(pawn.story.traits.allTraits);
    skills = pawn.skills.skills.ListFullCopy();

    // 提取关系
    relations = pawn.relations.DirectRelations.ToList();

    // 提取工作设置
    priorities = new Dictionary<WorkTypeDef, int>();
    foreach (var workType in DefDatabase<WorkTypeDef>.AllDefs) {
        priorities[workType] = pawn.workSettings.GetPriority(workType);
    }

    // 提取Hediff
    savedHediffs = pawn.health.hediffSet.hediffs.ToList();

    // ... 提取140+个其他字段
}
```

### 3.3 思想与记忆应用 (ApplyThoughts方法)

```csharp
public static void ApplyThoughts(Pawn pawn, NeuralData neuralData) {
    // 检查是否允许跨躯体相关思想
    Ideo ideo = pawn.Ideo;
    if (ideo == null || !ideo.HasPrecept(AC_DefOf.AC_CrossSleeving_DontCare)) {

        // [1] 性别不匹配
        if (pawn.gender != neuralData.OriginalGender) {
            pawn.needs.mood.thoughts.memories.TryGainMemory(
                isAndroid ? AC_DefOf.AC_WrongShellGender
                         : AC_DefOf.AC_WrongGender
            );
        }

        // [2] 种族不匹配（外星人模组）
        if (ModCompatibility.AlienRacesIsActive &&
            neuralData.OriginalRace != null &&
            pawn.kindDef.race != neuralData.OriginalRace) {
            pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.AC_WrongRace);
        }

        // [3] 仙族型不匹配
        if (!pawn.SleeveMatchesOriginalXenotype(neuralData)) {
            pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.AC_WrongXenotype);
        }
    }

    // [4] 基础思想：新躯体/新外壳
    pawn.needs.mood.thoughts.memories.TryGainMemory(
        isAndroid ? AC_DefOf.AC_NewShell : AC_DefOf.AC_NewSleeve
    );

    // [5] 特殊情况：Shellwalker想回到机械躯体
    if (ModCompatibility.VanillaRacesExpandedAndroidIsActive &&
        pawn.story.traits.HasTrait(AC_DefOf.AC_Shellwalker) &&
        !isAndroid) {
        pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.AC_WantsShell);
    }
}
```

---

## 第4部分：对RimTrion Step 4的参考建议

### 4.1 TrionSnapshot数据结构设计建议

#### ❌ 不推荐完全复制Altered Carbon (太复杂)
- 140+字段维护成本过高
- 大多数字段对Trion系统不需要
- 会拖累性能

#### ✅ 推荐采用"最小化必要集"策略

```csharp
public class TrionSnapshot : IExposable {
    // [必要] 身体状态
    public List<Hediff> savedHediffs;        // 伤病列表（关键）
    public List<BodyPartRecord> missingParts;  // 缺失部位（关键）

    // [必要] 需求状态
    public float hungerLevel;                // 饥饿值
    public float restLevel;                  // 休息值
    public float moodOffset;                 // 心情修正（可选）

    // [可选] 属性状态
    public Dictionary<StatDef, float> statModifiers;  // 属性修正

    // [可选] 心理状态
    public List<Thought_Memory> savedThoughts;  // 相关记忆（有选择性）

    // [管理] 元数据
    public int snapshotTick;                 // 创建时间
    public string sourceStackDef;            // 来源堆栈定义
    public float dataIntegrity;              // 数据完整性 [0-1]
}
```

### 4.2 推荐的实现路线

#### 方案A: 简化路线 (推荐用于Phase 1)
- 仅保存 Hediff 列表
- 仅保存 Needs 值
- 清晰、可维护、快速可实现

#### 方案B: 平衡路线 (Phase 2改进)
- 保存 Hediff + Needs + 关键属性
- 保留关键社交关系（婚姻等）
- 支持模组兼容性扩展

#### 方案C: 完整路线 (Phase 3+)
- 参考Altered Carbon的完整框架
- 但使用清晰的模块化设计
- 分为多个小的Snapshot类而非单一大类

### 4.3 关键Hediff处理建议

**Altered Carbon的Hediff处理策略**（值得参考）：
```
✓ 保存的Hediff类型：所有外伤、病症、增强物
✗ 不保存的Hediff类型：临时Buff（如酒精中毒）

✓ 保存方式：仅保存Def和数值（severity, ageTicks等）
✗ 不保存：对象引用（避免GC问题）

✓ 恢复方式：重新创建Hediff对象并应用数值
✗ 直接复用：被GC销毁的对象
```

---

## 第5部分：风险与限制分析

### 5.1 Altered Carbon设计中的已知问题

| 问题 | 表现 | RimTrion如何避免 |
|------|------|-----------------|
| **文件规模** | NeuralData 2019行 | 拆分为5-8个小类 |
| **字段冗余** | 140+字段含无用项 | 精选20-30个关键字段 |
| **版本升级** | 字段添加导致存档不兼容 | 设计版本控制机制 |
| **性能开销** | 创建snapshot需遍历全pawn | 仅提取必要数据 |
| **模组污染** | 保存第三方模组数据风险 | 黑名单+异常处理 |

### 5.2 建议的安全措施

```csharp
public class TrionSnapshot : IExposable {
    // [版本控制] 防止存档破裂
    public int snapshotVersion = 1;

    // [错误恢复] Hediff恢复失败时的回退
    public bool safeMode = false;  // 启用安全模式时跳过有问题的Hediff

    // [模组检测] 识别来源
    public string sourceMod = "RimTrion";

    // [数据校验] 完整性验证
    public string dataHash;

    // [调试] 追踪
    public string creationLocation;  // 在哪个pawn身上创建的快照
}
```

---

## 第6部分：关键学习收获

### ✅ 推荐采纳

1. **分离保存策略** - 数据与Pawn分离
2. **虚拟Pawn方案** - 处理"原躯体丢失"的优雅方式
3. **思想修正系统** - 根据躯体变化调整心理状态
4. **模组兼容性框架** - 扩展性设计

### ⚠️ 谨慎采纳

1. **大型单一类** - 应该拆分设计
2. **140+字段** - 选择性保留关键字段
3. **完全覆盖策略** - 可考虑增量更新

### ❌ 不推荐

1. **物品流通** - RimTrion的快照不需要是物品
2. **多副本系统** - Phase 1不需要支持副本
3. **完整社交关系** - 超出Phase 1范围

---

## 第7部分：关键代码片段参考

### 快照创建
```csharp
public void CreateFromPawn(Pawn pawn) {
    // 保存状态
    this.savedHediffs = new List<Hediff>();
    foreach (var hediff in pawn.health.hediffSet.hediffs) {
        // 只保存关键类型
        if (ShouldSaveHediff(hediff)) {
            this.savedHediffs.Add(hediff);
        }
    }

    // 保存需求
    this.hungerLevel = pawn.needs.food?.CurLevel ?? 0.5f;
    this.restLevel = pawn.needs.rest?.CurLevel ?? 0.5f;
}
```

### 快照应用
```csharp
public void ApplyToPawn(Pawn pawn) {
    // 清空旧状态
    pawn.health.RemoveAllHediffs();

    // 恢复Hediff
    foreach (var savedHediff in savedHediffs) {
        Hediff newHediff = HediffMaker.MakeHediff(
            savedHediff.def,
            pawn,
            savedHediff.Part
        );
        newHediff.Severity = savedHediff.Severity;
        pawn.health.AddHediff(newHediff);
    }

    // 恢复需求
    pawn.needs.food.CurLevel = hungerLevel;
    pawn.needs.rest.CurLevel = restLevel;
}
```

---

## 总结与建议

### 对Step 4设计的最终建议

**采用"简化平衡"方案**：
1. ✅ 保存Hediff列表（最关键）
2. ✅ 保存Needs值（基础需求）
3. ✅ 保存关键属性修正（性别、种族等）
4. ✅ 保存必要思想（跨躯体相关）
5. ✅ 实现模组兼容性扩展点（但Phase 1不实现）

**预期代码规模**：
- TrionSnapshot 类：150-250行
- TrionBody Hediff：150-200行
- 应用逻辑：200-300行
- **总计**：500-750行（相比AC的2000+ 少66%）

**实现难度**：⭐⭐⭐☆☆ (中等偏易)

---

**文档版本**: v1.0
**完成度**: 100% (可用于Phase 1设计讨论)
**下一步**: 需求架构师基于此分析进行Step 4详细设计
