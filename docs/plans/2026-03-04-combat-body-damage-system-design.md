---
标题：战斗体伤害系统设计文档
版本号: v1.0
更新日期: 2026-03-04
最后修改者: Claude Sonnet 4.6
标签：[文档][用户已确认][已完成][未锁定]
摘要: BDP战斗体伤害隔离系统的详细设计文档。基于brainstorming对话确定实现方案：事件驱动破裂检测、HediffComp伤害拦截、影子HP系统、配置化设计、最小化Harmony(仅1个patch)。实现范围为核心伤害隔离功能(选项A)，暂不实现引导时间机制。
---

# 战斗体伤害系统设计文档

## 前言

本文档是BDP模组战斗体系统中**伤害隔离子系统**的详细设计文档。

**设计目标**：实现战斗体的核心价值——完全伤害隔离(NR-001)，战斗体受伤不影响真身，解除后真身100%完好。

**设计范围**：
- ✅ 内层FSM(简化版，无引导时间)
- ✅ 伤害拦截(HediffComp钩子)
- ✅ 影子HP系统
- ✅ 基础伤口Hediff
- ✅ 死亡/倒地拦截
- ✅ 紧急脱离路由

**设计原则**：
1. **事件驱动** - 破裂检测用事件回调，不轮询
2. **配置驱动** - 所有数值可在XML中调整
3. **最小化Harmony** - 只有1个patch(死亡拦截)
4. **原版兼容** - 参考原版流血和伤口机制
5. **简化实现** - 暂不实现引导时间，专注核心功能

**依赖文档**：
- 需求来源：`战斗体系统需求设计文档.md` (v4.1, 43条需求)
- 架构参考：`战斗体系统架构设计文档.md` (v1.3)
- 技术参考：`战斗体系统RimWorld原生技术参考分析.md` (v1.0)

---

## 一、架构概览

### 1.1 核心组件结构

```
Combat/
├── Hediffs/
│   ├── HediffComp_CombatBodyActive.cs      # 核心协调器
│   │   ├── ShadowHPTracker (内部类)        # 影子HP管理
│   │   ├── WoundTracker (内部类)           # 伤口追踪
│   │   ├── PostPreApplyDamage()            # 伤害拦截入口
│   │   ├── ProcessDamage()                 # 伤害处理逻辑
│   │   ├── Notify_Downed()                 # 倒地拦截
│   │   └── HandleTrionDepleted()           # Trion耗尽回调
│   │
│   ├── Hediff_CombatWound.cs               # 战斗体伤口Hediff
│   ├── HediffComp_CombatWound.cs           # 伤口Comp(Trion流失)
│   └── HediffCompProperties_CombatBody.cs  # 配置属性
│
├── Patches/
│   └── Patch_DeathPrevention.cs            # 死亡拦截(唯一Harmony)
│
└── Escape/
    └── EscapeRouter.cs                     # 紧急脱离路由(责任链)
```

### 1.2 关键设计决策

| 决策项 | 选择 | 理由 |
|--------|------|------|
| **实现范围** | 核心伤害隔离功能 | 专注P0优先级，快速验证核心价值 |
| **引导时间** | 暂不实现 | 简化实现，后续版本添加 |
| **伤害拦截** | HediffComp.PostPreApplyDamage | 零Harmony，原版钩子，兼容性最好 |
| **破裂检测** | 事件驱动 | 不轮询，性能更好，代码更清晰 |
| **死亡拦截** | Harmony patch + preventsDeath | 唯一需要Harmony的地方 |
| **倒地拦截** | Hediff.Notify_Downed虚方法 | 零Harmony，原版已验证可行 |
| **配置方式** | XML Def + ModExtension | 易于调整，模组兼容 |
| **架构模式** | 混合式(方案3) | 平衡简单性和可维护性 |

### 1.3 与现有代码的集成

- **Gene_TrionGland** - 外层FSM和快照系统保持不变
- **CombatBodyOrchestrator** - 激活时添加战斗体Hediff，触发HediffComp初始化
- **CompTrion** - 需要扩展：添加OnTrionDepleted事件
- **CombatBodyState** - 外层状态管理保持不变

---

## 二、核心组件详细设计

### 2.1 HediffComp_CombatBodyActive (核心协调器)

**职责**：
- 伤害拦截和处理的单一入口
- 管理影子HP和伤口状态
- 检测破裂条件并触发被动破裂
- 响应倒地事件

**关键字段**：
```csharp
public class HediffComp_CombatBodyActive : HediffComp
{
    // 配置属性
    private HediffCompProperties_CombatBody Props =>
        (HediffCompProperties_CombatBody)props;

    // 内部状态管理
    private ShadowHPTracker shadowHP;
    private WoundTracker wounds;

    // 是否激活
    private bool IsActive => parent != null && !parent.ShouldRemove;
}
```

**关键方法**：

**初始化**：
```csharp
public override void CompPostMake()
{
    base.CompPostMake();

    // 初始化影子HP
    shadowHP = new ShadowHPTracker();
    shadowHP.Initialize(Pawn);

    // 初始化伤口追踪器
    wounds = new WoundTracker(Pawn);

    // 订阅Trion耗尽事件
    var compTrion = Pawn.GetComp<CompTrion>();
    if (compTrion != null)
    {
        compTrion.OnTrionDepleted += HandleTrionDepleted;
    }
}
```

**伤害拦截入口**：
```csharp
public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
{
    absorbed = false;
    if (!IsActive) return;

    // 处理伤害
    ProcessDamage(dinfo);

    // 阻止原版HP系统
    absorbed = true;
}
```

**伤害处理逻辑**：
```csharp
private void ProcessDamage(DamageInfo dinfo)
{
    // 1. 直接消耗Trion: f(x)
    float trionCost = CalculateTrionCost(dinfo.Amount);
    ConsumeTrion(trionCost);

    // 2. 更新影子HP
    shadowHP.TakeDamage(dinfo.HitPart, dinfo.Amount);

    // 3. 检查部位是否被毁
    if (shadowHP.IsPartDestroyed(dinfo.HitPart))
    {
        HandlePartDestroyed(dinfo.HitPart);
    }
    else
    {
        // 4. 创建或更新伤口Hediff
        wounds.CreateOrMergeWound(dinfo.HitPart, dinfo.Amount);
    }
}
```

**破裂条件回调**：
```csharp
// 事件回调1: Trion耗尽
private void HandleTrionDepleted()
{
    TriggerPassiveCollapse("Trion耗尽");
}

// 事件回调2: 部位被毁
private void HandlePartDestroyed(BodyPartRecord part)
{
    // 移除该部位的伤口
    wounds.RemoveWound(part);

    // 检查是否是弱点部位
    if (IsWeakPoint(part))
    {
        TriggerPassiveCollapse($"{part.Label}被毁");
    }
    else
    {
        // 非弱点器官: 注册高额Trion流失
        RegisterHighDrain(part);
    }
}

// 事件回调3: 倒地
public override void Notify_Downed()
{
    base.Notify_Downed();
    if (IsActive)
    {
        TriggerPassiveCollapse("倒地");
    }
}
```

**触发被动破裂**：
```csharp
private void TriggerPassiveCollapse(string reason)
{
    Log.Message($"[BDP] 战斗体破裂: {Pawn.Name} - {reason}");

    // 通知Gene层
    var gene = Pawn.genes?.GetFirstGeneOfType<Gene_TrionGland>();
    if (gene != null)
    {
        // 触发被动破裂流程
        gene.OnCombatBodyEnded(CombatBodyEndReason.Passive);
    }
}
```

### 2.2 ShadowHPTracker (内部类)

**职责**：
- 追踪每个身体部位的当前耐久值
- 判断部位是否被毁
- 序列化存档

**设计**：
```csharp
private class ShadowHPTracker : IExposable
{
    private Dictionary<BodyPartRecord, float> partHP;
    private Pawn pawn;

    // 初始化:每个部位从最大HP开始
    public void Initialize(Pawn pawn)
    {
        this.pawn = pawn;
        partHP = new Dictionary<BodyPartRecord, float>();

        foreach (var part in pawn.health.hediffSet.GetNotMissingParts())
        {
            // 植入物覆盖的缺失部位视为完整(NR-027)
            partHP[part] = part.def.hitPoints;
        }
    }

    // 受伤:扣减影子HP
    public void TakeDamage(BodyPartRecord part, float damage)
    {
        if (partHP.ContainsKey(part))
        {
            partHP[part] = Mathf.Max(0f, partHP[part] - damage);
        }
    }

    // 判断部位是否被毁
    public bool IsPartDestroyed(BodyPartRecord part)
    {
        return partHP.ContainsKey(part) && partHP[part] <= 0f;
    }

    // 获取部位当前HP
    public float GetPartHP(BodyPartRecord part)
    {
        return partHP.GetValueOrDefault(part, 0f);
    }

    // 序列化 (NR-041要求)
    public void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            var keys = partHP.Keys.ToList();
            var values = partHP.Values.ToList();
            Scribe_Collections.Look(ref keys, "parts", LookMode.BodyPart, pawn.RaceProps.body);
            Scribe_Collections.Look(ref values, "hpValues", LookMode.Value);
        }
        else
        {
            List<BodyPartRecord> keys = null;
            List<float> values = null;
            Scribe_Collections.Look(ref keys, "parts", LookMode.BodyPart, pawn.RaceProps.body);
            Scribe_Collections.Look(ref values, "hpValues", LookMode.Value);

            if (keys != null && values != null)
            {
                partHP = new Dictionary<BodyPartRecord, float>();
                for (int i = 0; i < keys.Count; i++)
                {
                    partHP[keys[i]] = values[i];
                }
            }
        }
    }
}
```


### 2.3 WoundTracker (内部类)

**职责**: 管理战斗体伤口Hediff、实现同部位伤口合并机制、注册/注销Trion持续流失

### 2.4 Hediff_CombatWound (独立类)

**关键设计**: 不继承Hediff_Injury,天然屏蔽出血/自愈/疼痛

---

## 三、配置化设计

所有数值配置通过XML Def和ModExtension实现,包括:
- 伤害转换系数 (damageToTrionMultiplier)
- 弱点部位列表 (weakPointParts)
- 部位流失倍数 (partDrainMultipliers)
- 器官被毁流失值 (organDestroyedDrainPerDay)

---

## 四、关键技术要点

### 4.1 死亡拦截 (唯一Harmony patch)
Patch `Pawn_HealthTracker.ShouldBeDead()` 方法,战斗体激活时返回false并触发被动破裂

### 4.2 CompTrion事件扩展
添加 `OnTrionDepleted` 事件,Trion耗尽时触发破裂

### 4.3 序列化要点
影子HP使用 `LookMode.BodyPart` 序列化,Dictionary拆分为两个List处理

---

## 五、实现范围总结

### 本次实现
✅ HediffComp_CombatBodyActive (核心协调器)
✅ ShadowHPTracker (影子HP管理)
✅ WoundTracker (伤口管理)
✅ Hediff_CombatWound (战斗体伤口)
✅ Patch_DeathPrevention (死亡拦截)
✅ 配置系统 (XML驱动)
✅ CompTrion事件扩展

### 暂不实现
❌ 引导时间机制
❌ 完整紧急脱离系统
❌ 枯竭debuff

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-04 | 初版完成。基于brainstorming对话确定设计方案：事件驱动破裂检测、HediffComp伤害拦截、影子HP系统、配置化设计、最小化Harmony(仅1个patch)。实现范围为核心伤害隔离功能(选项A)，暂不实现引导时间机制。 | Claude Sonnet 4.6 |

