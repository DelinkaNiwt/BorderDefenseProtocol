# 战斗体伤口系统设计文档 v1.0

**创建时间**: 2026-03-04
**状态**: 设计完成，待实现
**AI模型**: Claude Sonnet 4.6 (1M context)

---

## 1. 系统概述

### 1.1 目标
为战斗体实现专用的伤口系统，满足以下需求：
- 伤口不继承原版伤口类（Hediff_Injury），天然屏蔽出血/疼痛
- 同部位同类型伤口合并（severity累加）
- 伤口持续消耗Trion
- 伤口影响部位效率（Moving/Manipulation/Sight），但不影响维生系统
- 手部缺失时强制关闭对应触发器槽位

### 1.2 架构约束
- 遵循NR-042（伤口Hediff规范）
- 遵循NR-020（战斗体无法自愈）
- 遵循NR-024（战斗体无法使用原版治疗）
- 不继承原版伤口类，使用BDP专用Hediff
- 最简原则，注释丰富

---

## 2. 核心设计决策

### 决策1: 伤口合并策略
**规则**: 同部位 + 同伤害类型 → 合并（severity累加）
**规则**: 同部位 + 不同伤害类型 → 独立显示

**示例**:
```
左臂先被子弹打中（15伤害），再被子弹打中（10伤害）
→ 显示: "战斗体枪伤 x2" (severity=25)

左臂先被子弹打中，再被刀砍中
→ 显示: "战斗体枪伤" + "战斗体切割伤" (两个独立Hediff)
```

### 决策2: Severity与计数关系
**方案**: Severity累加，显示计数

- Severity = 总伤害值累加（如15+10=25）
- 显示格式: "战斗体枪伤 x2"
- Trion流失基于总severity计算
- 需要额外字段记录受伤次数

### 决策3: 伤害类型映射策略
**方案**: 完全映射

为每种原版伤害类型创建对应的Trion伤口HediffDef：
- `BDP_CombatWound_Bullet` (枪伤)
- `BDP_CombatWound_Cut` (切割伤)
- `BDP_CombatWound_Blunt` (钝击伤)
- `BDP_CombatWound_Burn` (烧伤)
- `BDP_CombatWound_Frostbite` (冻伤)
- `BDP_CombatWound_Scratch` (抓伤)
- `BDP_CombatWound_Bite` (咬伤)
- `BDP_CombatWound_Stab` (刺伤)
- `BDP_CombatWound_Crush` (碾压伤)
- 其他根据需要扩展

### 决策4: 伤口影响机制
**方案**: 选择性使用原版能力系统

**影响的能力**（通过HediffDef.stages.capMods配置）:
- `Moving` - 移动能力（腿部伤口）
- `Manipulation` - 操作能力（手臂/手部伤口）
- `Sight` - 视觉能力（眼睛伤口）

**不影响的能力**:
- `Consciousness` - 意识
- `BloodPumping` - 血液循环
- `BloodFiltration` - 血液过滤
- `Breathing` - 呼吸
- `Metabolism` - 新陈代谢
- `Eating` - 进食
- `Talking` - 说话
- `Hearing` - 听力

**特殊处理**:
- 手部缺失时，强制关闭对应触发器槽位（通过事件机制）

### 决策5: 手部缺失检测机制
**方案**: 事件驱动 + 激活时检查（混合方案）

**事件驱动**（立即响应）:
1. `PartDestructionHandler` 检测到手部破坏
2. 触发 `Gene_TrionGland.OnPartDestroyed` 事件
3. `CompTriggerBody` 订阅事件，收到通知后强制关闭对应槽位

**激活时检查**（防御性检查）:
- 玩家尝试激活芯片时，检查对应手部是否缺失
- 如果缺失，拒绝激活并显示提示信息

---

## 3. 数据结构设计

### 3.1 伤口Hediff结构

**HediffDef XML示例**:
```xml
<HediffDef>
  <defName>BDP_CombatWound_Bullet</defName>
  <label>战斗体枪伤</label>
  <labelNoun>枪伤</labelNoun>
  <description>战斗体受到枪械攻击造成的伤口。持续消耗Trion。</description>
  <hediffClass>BDP.Combat.Hediff_CombatWound</hediffClass>
  <defaultLabelColor>(0.8, 0.8, 0.35)</defaultLabelColor>
  <makesSickThought>false</makesSickThought>
  <tendable>false</tendable>
  <isBad>true</isBad>

  <!-- 不继承Hediff_Injury，天然屏蔽出血/疼痛 -->

  <comps>
    <li Class="BDP.Combat.HediffCompProperties_CombatWound">
      <!-- 伤口配置，见3.2节 -->
    </li>
  </comps>

  <stages>
    <li>
      <!-- 根据部位影响能力 -->
      <capMods>
        <li>
          <capacity>Moving</capacity>
          <offset>-0.1</offset>  <!-- 每个伤口-10%移动 -->
        </li>
      </capMods>
    </li>
  </stages>
</HediffDef>
```

### 3.2 HediffComp字段结构

**HediffCompProperties_CombatWound**:
```csharp
public class HediffCompProperties_CombatWound : HediffCompProperties
{
    // Trion流失配置
    public float trionDrainPerSeverityPerDay = 5f;  // 每点severity每天消耗5 Trion

    // 伤害类型标识（用于合并判断）
    public string damageTypeKey = "Bullet";

    public HediffCompProperties_CombatWound()
    {
        compClass = typeof(HediffComp_CombatWound);
    }
}
```

**HediffComp_CombatWound**:
```csharp
public class HediffComp_CombatWound : HediffComp
{
    // 受伤次数（用于显示"x2"、"x3"）
    public int hitCount = 1;

    // 上次Trion流失时间（用于计算流失量）
    private int lastDrainTick = -1;

    // Trion流失注册ID（用于解除时取消注册）
    private int drainRegistrationId = -1;

    public HediffCompProperties_CombatWound Props =>
        (HediffCompProperties_CombatWound)props;

    // CompPostMake: 初始化，注册Trion流失
    // CompPostTick: 定期检查Trion流失
    // CompPostPostRemoved: 取消Trion流失注册
    // GetLabel: 返回显示标签（包含"x2"等）
}
```

### 3.3 伤口适配层数据结构

**WoundAdapter**（静态工具类）:
```csharp
public static class WoundAdapter
{
    // 伤害类型 → HediffDef 映射表
    private static Dictionary<string, HediffDef> damageTypeToHediffDef;

    // 初始化映射表
    static WoundAdapter()
    {
        damageTypeToHediffDef = new Dictionary<string, HediffDef>
        {
            { "Bullet", BDP_DefOf.BDP_CombatWound_Bullet },
            { "Cut", BDP_DefOf.BDP_CombatWound_Cut },
            { "Blunt", BDP_DefOf.BDP_CombatWound_Blunt },
            // ... 其他映射
        };
    }

    // 获取对应的Trion伤口HediffDef
    public static HediffDef GetCombatWoundDef(DamageDef damageDef);

    // 添加或合并伤口
    public static void AddOrMergeWound(Pawn pawn, BodyPartRecord part,
        DamageDef damageDef, float severity);
}
```

---

## 4. 实现流程设计

### 4.1 伤害处理Pipeline（更新）

```
原版伤害 → Patch_FinalizeAndAddInjury拦截
  ↓
CombatBodyDamageHandler.HandleDamage()
  ↓
├─ TrionCostHandler (已实现)
├─ ShadowHPHandler (已实现)
├─ PartDestructionHandler (已实现)
├─ WoundHandler (新增) ← 本次实现重点
│   ├─ 获取伤害类型
│   ├─ 查找或创建伤口Hediff
│   ├─ 合并或新建伤口
│   └─ 注册Trion流失
└─ CollapseHandler (已实现)
```

### 4.2 伤口生命周期

**创建阶段**:
1. `WoundHandler.Handle()` 被调用
2. 通过 `WoundAdapter.GetCombatWoundDef()` 获取对应HediffDef
3. 检查同部位是否已有同类型伤口
4. 如果有 → 合并（severity累加，hitCount+1）
5. 如果没有 → 创建新伤口Hediff
6. `HediffComp_CombatWound.CompPostMake()` 注册Trion流失

**更新阶段**:
1. `HediffComp_CombatWound.CompPostTick()` 定期检查
2. 计算Trion流失量（基于severity和配置）
3. 扣除Trion（通过CompTrion）
4. 如果Trion不足 → 触发战斗体破裂

**移除阶段**:
1. 战斗体解除时，`WoundHandler.Clear()` 被调用
2. 移除所有战斗体伤口Hediff
3. `HediffComp_CombatWound.CompPostPostRemoved()` 取消Trion流失注册

### 4.3 手部缺失联动流程

```
影子手部HP耗尽
  ↓
ShadowHPHandler检测到非关键部位破坏
  ↓
PartDestructionHandler.Handle(pawn, handPart)
  ├─ 添加BDP_CombatBodyPartDestroyed Hediff
  ├─ 检测到是手部
  └─ 触发Gene_TrionGland.OnPartDestroyed事件
      ↓
CompTriggerBody.OnPartDestroyed(args)
  ├─ 判断是左手还是右手
  └─ ForceDeactivateLeftSlots() 或 ForceDeactivateRightSlots()
      ↓
对应槽位所有芯片强制关闭
```

---

## 5. 接口设计

### 5.1 WoundHandler接口

```csharp
public static class WoundHandler
{
    /// <summary>
    /// 处理战斗体伤口。
    /// </summary>
    /// <param name="pawn">受伤的Pawn</param>
    /// <param name="part">受伤部位</param>
    /// <param name="damageDef">伤害类型</param>
    /// <param name="severity">伤害量</param>
    /// <returns>true=处理成功，false=处理失败</returns>
    public static bool Handle(Pawn pawn, BodyPartRecord part,
        DamageDef damageDef, float severity);

    /// <summary>
    /// 清理所有战斗体伤口（战斗体解除时调用）。
    /// </summary>
    public static void Clear(Pawn pawn);
}
```

### 5.2 WoundAdapter接口

```csharp
public static class WoundAdapter
{
    /// <summary>
    /// 获取对应的Trion伤口HediffDef。
    /// </summary>
    public static HediffDef GetCombatWoundDef(DamageDef damageDef);

    /// <summary>
    /// 添加或合并伤口。
    /// </summary>
    public static void AddOrMergeWound(Pawn pawn, BodyPartRecord part,
        DamageDef damageDef, float severity);
}
```

### 5.3 事件接口（扩展Gene_TrionGland）

```csharp
// 在Gene_TrionGland中新增
public class PartDestroyedEventArgs
{
    public Pawn Pawn;
    public BodyPartRecord Part;
    public bool IsHandPart;
    public HandSide HandSide;
}

public static event System.Action<PartDestroyedEventArgs> OnPartDestroyed;

public static void TriggerPartDestroyedEvent(PartDestroyedEventArgs args)
{
    OnPartDestroyed?.Invoke(args);
}
```

---

## 6. 实现步骤

### Phase 1: 基础伤口系统
1. 创建 `Hediff_CombatWound` 类
2. 创建 `HediffComp_CombatWound` 和 `HediffCompProperties_CombatWound`
3. 创建伤口HediffDef XML（至少3种：Bullet/Cut/Blunt）
4. 实现 `WoundAdapter` 静态工具类
5. 实现 `WoundHandler` 静态处理器
6. 在 `CombatBodyDamageHandler` 中集成 `WoundHandler`

### Phase 2: Trion流失机制
1. 在 `HediffComp_CombatWound` 中实现Trion流失逻辑
2. 实现流失注册/取消机制
3. 实现Trion不足时的破裂触发

### Phase 3: 手部缺失联动
1. 在 `Gene_TrionGland` 中添加 `OnPartDestroyed` 事件
2. 在 `PartDestructionHandler` 中添加手部检测和事件触发
3. 在 `CompTriggerBody` 中订阅事件并实现强制关闭逻辑
4. 在芯片激活时添加手部检查

### Phase 4: 测试与优化
1. 游戏内测试伤口创建和合并
2. 测试Trion流失
3. 测试手部缺失联动
4. 性能优化
5. 日志优化

---

## 7. 技术细节

### 7.1 伤口合并逻辑

```csharp
public static void AddOrMergeWound(Pawn pawn, BodyPartRecord part,
    DamageDef damageDef, float severity)
{
    // 获取对应的HediffDef
    HediffDef hediffDef = GetCombatWoundDef(damageDef);
    if (hediffDef == null) return;

    // 查找同部位同类型的现有伤口
    var existingWound = pawn.health.hediffSet.hediffs
        .OfType<Hediff_CombatWound>()
        .FirstOrDefault(h =>
            h.Part == part &&
            h.def == hediffDef);

    if (existingWound != null)
    {
        // 合并：severity累加，hitCount+1
        existingWound.Severity += severity;
        var comp = existingWound.TryGetComp<HediffComp_CombatWound>();
        if (comp != null)
        {
            comp.hitCount++;
        }
        Log.Message($"[BDP] 伤口合并: {hediffDef.label} x{comp.hitCount} (severity={existingWound.Severity:F1})");
    }
    else
    {
        // 新建伤口
        var hediff = (Hediff_CombatWound)HediffMaker.MakeHediff(hediffDef, pawn, part);
        hediff.Severity = severity;
        pawn.health.AddHediff(hediff);
        Log.Message($"[BDP] 新建伤口: {hediffDef.label} (severity={severity:F1})");
    }
}
```

### 7.2 Trion流失计算

```csharp
public override void CompPostTick(ref float severityAdjustment)
{
    base.CompPostTick(ref severityAdjustment);

    // 每60 ticks检查一次
    if (!parent.pawn.IsHashIntervalTick(60)) return;

    // 计算流失量
    float drainPerTick = Props.trionDrainPerSeverityPerDay * parent.Severity / GenDate.TicksPerDay;
    float drainAmount = drainPerTick * 60;

    // 扣除Trion
    var compTrion = parent.pawn.GetComp<CompTrion>();
    if (compTrion != null)
    {
        compTrion.Cur -= drainAmount;

        // 检查Trion是否耗尽
        if (compTrion.Cur <= 0f)
        {
            Log.Warning($"[BDP] 伤口导致Trion耗尽，触发战斗体破裂");
            // 触发破裂...
        }
    }
}
```

### 7.3 手部检测逻辑

```csharp
private bool IsHandPart(BodyPartRecord part)
{
    if (part == null) return false;

    // 检查是否为Hand或Hand的子部位
    if (part.def.defName == "Hand") return true;

    // 检查父部位是否为Hand
    if (part.parent?.def.defName == "Hand") return true;

    return false;
}

private HandSide GetHandSide(BodyPartRecord part)
{
    // 向上遍历找到Shoulder，判断左右
    var current = part;
    while (current != null)
    {
        if (current.def.defName == "Shoulder")
        {
            // 通过customLabel判断（"left shoulder" / "right shoulder"）
            if (current.customLabel != null &&
                current.customLabel.ToLower().Contains("left"))
            {
                return HandSide.Left;
            }
            return HandSide.Right;
        }
        current = current.parent;
    }

    // 默认返回左手（不应该到这里）
    Log.Warning($"[BDP] 无法判断手部侧边: {part.def.defName}");
    return HandSide.Left;
}
```

---

## 8. 配置示例

### 8.1 伤口HediffDef配置

```xml
<!-- 枪伤 -->
<HediffDef>
  <defName>BDP_CombatWound_Bullet</defName>
  <label>战斗体枪伤</label>
  <hediffClass>BDP.Combat.Hediff_CombatWound</hediffClass>
  <comps>
    <li Class="BDP.Combat.HediffCompProperties_CombatWound">
      <trionDrainPerSeverityPerDay>5</trionDrainPerSeverityPerDay>
      <damageTypeKey>Bullet</damageTypeKey>
    </li>
  </comps>
  <stages>
    <li>
      <capMods>
        <li>
          <capacity>Moving</capacity>
          <offset>-0.05</offset>
        </li>
      </capMods>
    </li>
  </stages>
</HediffDef>

<!-- 切割伤 -->
<HediffDef>
  <defName>BDP_CombatWound_Cut</defName>
  <label>战斗体切割伤</label>
  <hediffClass>BDP.Combat.Hediff_CombatWound</hediffClass>
  <comps>
    <li Class="BDP.Combat.HediffCompProperties_CombatWound">
      <trionDrainPerSeverityPerDay>6</trionDrainPerSeverityPerDay>
      <damageTypeKey>Cut</damageTypeKey>
    </li>
  </comps>
  <stages>
    <li>
      <capMods>
        <li>
          <capacity>Manipulation</capacity>
          <offset>-0.08</offset>
        </li>
      </capMods>
    </li>
  </stages>
</HediffDef>
```

---

## 9. 测试计划

### 9.1 单元测试场景
1. 同部位同类型伤口合并测试
2. 同部位不同类型伤口独立测试
3. Trion流失速率测试
4. 手部缺失触发器关闭测试

### 9.2 集成测试场景
1. 完整战斗流程测试（受伤→伤口→Trion流失→破裂）
2. 手部破坏→触发器关闭→尝试激活测试
3. 战斗体解除→伤口移除测试

### 9.3 性能测试
1. 多个Pawn同时受伤的性能测试
2. 长时间运行的Trion流失性能测试

---

## 10. 风险与限制

### 10.1 已知限制
- 伤口不会自愈（符合需求NR-020）
- 伤口无法使用原版治疗（符合需求NR-024）
- 只能通过战斗体解除来移除伤口

### 10.2 潜在风险
- 伤口过多可能导致性能问题（通过合并机制缓解）
- Trion流失可能导致意外破裂（需要平衡配置）
- 手部检测逻辑可能在某些Mod添加的身体结构上失效

### 10.3 兼容性考虑
- 与其他Mod添加的伤害类型兼容（通过映射表扩展）
- 与其他Mod添加的身体部位兼容（通过defName判断）

---

## 11. 后续扩展

### 11.1 可能的扩展方向
1. 伤口严重程度分级（轻伤/重伤）
2. 特殊伤口效果（如烧伤降低移动速度）
3. 伤口视觉效果（贴图/特效）
4. 伤口治疗系统（使用Trion修复）

### 11.2 暂不实现的功能
- 伤口感染
- 伤口疤痕
- 伤口疼痛（已通过不继承Hediff_Injury屏蔽）
- 伤口出血（已通过不继承Hediff_Injury屏蔽）

---

**文档版本**: v1.0
**最后更新**: 2026-03-04
**审核状态**: 待用户审核

---
*AI: Claude Sonnet 4.6 (1M context)*
