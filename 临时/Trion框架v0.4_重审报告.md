# Trion实体框架设计草案v0.4 - 重审报告

---

## 文档元信息

**摘要**：在修复API错误基础上，从框架层面重新审视v0.4设计的完整性、架构清晰性和扩展性。

**版本号**：v1.0
**审阅时间**：2026-01-10
**关键词**：框架设计、架构审视、扩展性评估、框架应用层分界
**标签**：[待审]

---

## 一、修改情况汇总

### 1.1 已修正的3处错误

| 错误 | 位置 | 修正内容 | 状态 |
|------|------|---------|------|
| HediffCompProperties_Thought | 第1023行 | 改为HediffCompProperties_ThoughtSetter | ✅ |
| disappearsAfterTicksIfInBed | 第1066行 | 删除，改为框架层设计 | ✅ |
| trait.def.outputPowerModifier | 第589行 | 改为通过ModExtension实现 | ✅ |

### 1.2 修改理由（框架视角）

所有修改遵循**"框架层只定义关键机制，细节交由应用层实现"**的原则：

1. **HediffCompProperties_ThoughtSetter**：框架定义debuff存在和基础心情惩罚，具体效果值由应用层定义
2. **disappearsAfterTicks（固定12小时）**：框架定义基础消退时间，休养舱加速由应用层扩展实现
3. **ModExtension for Trait**：框架定义OutputPower的计算接口，具体特性修正值由应用层定义

---

## 二、框架层完整性评估

### 2.1 核心职责分析

| 组件 | 框架层职责 | 应用层职责 | 评分 |
|------|----------|----------|------|
| **CompTrion** | 数据管理、消耗/恢复计算、策略调度 | 具体值配置（RecoveryRate等） | ✅ 清晰 |
| **ILifecycleStrategy** | 生命周期接口定义 | 具体策略实现（AI、渲染等） | ✅ 清晰 |
| **TriggerMount** | 挂载点状态管理、消耗计算 | Trigger功能具体实现 | ✅ 清晰 |
| **Harmony拦截** | 伤害拦截机制框架 | 具体伤害转换规则 | ✅ 清晰 |
| **OutputPower系统** | 计算接口、应用规则 | 具体等级定义、影响公式 | ⚠️ 见2.2 |
| **Trion恢复机制** | 恢复框架接口 | 具体恢复值、影响因素 | ✅ 清晰 |
| **debuff系统** | debuff存在、基础定义 | 具体效果值、加速功能 | ✅ 清晰 |

### 2.2 发现的框架层问题

#### 问题1：OutputPower系统定义不够清晰

**当前设计**：
```csharp
// 基础值：由Trion天赋决定
baseOutput = GetTrionTalent(pawn).baseOutputPower;
// 示例：S级天赋 = 85, A级 = 70, B级 = 50, C级 = 30, D级 = 15, E级 = 5
```

**问题**：
- ⚠️ `GetTrionTalent()`方法未在框架中定义
- ⚠️ "Trion天赋"的获取来源不明确（是Trait？是新增的Def？）
- ⚠️ 应用层需要自行实现"天赋"概念，造成集成难度

**框架层应该定义**：
- OutputPower的**计算接口**，而非具体值
- 基础值应该来自哪里的**规范**

**优化方案**（见3.1）：改为Strategy模式，由不同Strategy提供基础OutputPower

---

#### 问题2：Strategy中的"天赋"概念模糊

**当前代码**：
```csharp
if (parent is Pawn pawn)
{
    baseOutput = GetTrionTalent(pawn).baseOutputPower;
}
```

**问题**：
- ⚠️ `GetTrionTalent()`是一个"悬空"的方法调用，框架中没有定义
- ⚠️ 谁负责实现这个方法？框架还是应用层？
- ⚠️ 造成框架和应用层的接口不清晰

**框架应该做的**：
- 定义一个**扩展点**供应用层实现
- 而非在框架代码中直接调用应用层的方法

---

#### 问题3：预留接口不足

**现有框架预留**：
- ✅ ILifecycleStrategy（生命周期）
- ✅ ModExtension（属性扩展）
- ❌ OutputPower的来源接口（没有定义）
- ❌ Trion天赋等级系统（完全缺失）

---

### 2.3 框架应用层分界线检查

| 内容 | 当前位置 | 是否合理 | 建议 |
|------|---------|---------|------|
| CompTrion数据结构 | 框架 | ✅ | 保持 |
| Consume/Recover方法 | 框架 | ✅ | 保持 |
| Strategy接口 | 框架 | ✅ | 保持 |
| 具体Strategy实现 | 框架 | ⚠️ | 移至应用层示例 |
| OutputPower计算逻辑 | 框架 | ⚠️ | 抽象为接口 |
| debuff定义 | 框架 | ✅ | 保持 |
| debuff具体效果值 | 应用层 | ✅ | 保持 |
| Harmony补丁 | 框架 | ⚠️ | 定义规范，实现在应用层 |

---

## 三、优化方案

### 3.1 优化方案1：OutputPower系统重构

**问题**：GetTrionTalent()是悬空方法，框架/应用层分界不清

**当前**：
```csharp
public float CalculateOutputPower()
{
    // ⚠️ 应用层方法在框架代码中被调用
    baseOutput = GetTrionTalent(pawn).baseOutputPower;
}
```

**优化后**：
```csharp
public float CalculateOutputPower()
{
    float baseOutput = 0;

    // 1. 获取基础值（通过Strategy提供）
    baseOutput = strategy.GetBaseOutputPower();

    // 2. 应用特性修正（通过ModExtension）
    foreach (Trait trait in pawn.story.traits.allTraits)
    {
        if (trait.def.HasModExtension<OutputPowerModifierExtension>())
        {
            baseOutput += trait.def.GetModExtension<OutputPowerModifierExtension>().modifier;
        }
    }

    // 3. 应用装备加成（通过ModExtension）
    foreach (Apparel apparel in pawn.apparel.WornApparel)
    {
        if (apparel.def.HasModExtension<OutputPowerModifierExtension>())
        {
            baseOutput += apparel.def.GetModExtension<OutputPowerModifierExtension>().modifier;
        }
    }

    return Mathf.Clamp(baseOutput, 0, 100);
}
```

**框架修改**：
```csharp
// ILifecycleStrategy 新增方法
public abstract float GetBaseOutputPower();

// Strategy_HumanCombatBody 实现（示例）
public override float GetBaseOutputPower()
{
    // 应用层可在ProjectWT中重写此方法，从Trait读取天赋等级
    // 框架提供默认值50（中等输出功率）
    return 50f;
}
```

**优势**：
- ✅ 框架不依赖应用层的方法
- ✅ 应用层可灵活定义OutputPower来源（Trait、Gene、etc）
- ✅ 清晰的扩展点

**影响**：
- 小幅修改CalculateOutputPower()实现
- 新增ILifecycleStrategy.GetBaseOutputPower()抽象方法

---

### 3.2 优化方案2：Harmony补丁规范化

**问题**：框架中有Harmony补丁的**描述**，但没有实现**规范**

**当前**（第825-846行）：
```
Harmony Prefix: Pawn_HealthTracker.PreApplyDamage
  ├─ 检查是否有CompTrion？
  └─ 调用Strategy.OnDamageTaken()
```

**问题**：
- ⚠️ 没有定义补丁应该如何编写
- ⚠️ 没有定义何时return true/false
- ⚠️ 应用层需要猜测实现方式

**优化后**（新增补丁设计规范）：

```
## 十五A、Harmony补丁规范

### 补丁1：伤害拦截（必须）

**目标**：Pawn_HealthTracker.PreApplyDamage(DamageInfo dinfo, bool absorbed)

**规范**：
```csharp
[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
public static class Patch_PreApplyDamage
{
    public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, bool absorbed)
    {
        Pawn pawn = __instance.pawn;

        // 1. 检查是否有CompTrion
        CompTrion comp = pawn.TryGetComp<CompTrion>();
        if (comp == null) return true;  // 没有，走原版流程

        // 2. 检查是否应该拦截伤害
        if (!comp.strategy.ShouldInterceptDamage()) return true;

        // 3. 拦截伤害，调用Strategy处理
        BodyPartRecord hitPart = dinfo.HitPart;
        float damageAmount = dinfo.Amount;
        comp.strategy.OnDamageTaken(damageAmount, hitPart);

        // 4. 返回false阻止原版伤害流程
        return false;
    }
}
```

**规范说明**：
- Prefix方式拦截（不使用Postfix）
- 在任何Hediff添加前进行拦截
- 必须检查ShouldInterceptDamage()
- 返回false时框架负责处理所有伤害逻辑

### 补丁2：生理需求冻结（可选）

**目标**：Need.NeedInterval()

**规范**：
```csharp
[HarmonyPatch(typeof(Need), nameof(Need.NeedInterval))]
public static class Patch_NeedInterval
{
    public static bool Prefix(Need __instance)
    {
        Pawn pawn = __instance.pawn;
        CompTrion comp = pawn.TryGetComp<CompTrion>();

        // 检查是否在激活的Combat Body中
        if (comp != null && comp.strategy is Strategy_HumanCombatBody strategy)
        {
            if (strategy.isActive)
            {
                return false;  // 跳过需求更新
            }
        }

        return true;  // 继续正常流程
    }
}
```

**规范说明**：
- 仅在Strategy_HumanCombatBody时冻结需求
- 其他Strategy不受影响
- 可选补丁，不实现也不影响框架核心
```

**优势**：
- ✅ 应用层有清晰的实现模板
- ✅ 统一的Harmony补丁编写规范
- ✅ 减少应用层自行猜测的工作

**影响**：
- 新增一个"十五A"节点
- 补充Harmony补丁编写规范

---

### 3.3 优化方案3：Strategy接口完善

**问题**：Strategy接口缺少GetBaseOutputPower()方法

**当前ILifecycleStrategy**（第185-196行）：
```csharp
public abstract float GetBaseOutputPower();
```

**优化后**：
```csharp
ILifecycleStrategy
├─ OnInitialize()
├─ OnTick()
├─ ShouldInterceptDamage()
├─ OnDamageTaken(amount, hitPart)
├─ OnDepleted()
├─ ShouldRecover()
├─ GetRecoveryModifier()
└─ GetBaseOutputPower()  // 新增
```

**实现示例**（每个Strategy）：

```csharp
// Strategy_HumanCombatBody
public override float GetBaseOutputPower()
{
    // 基础值：50（中等）
    // 应用层可通过继承重写
    return 50f;
}

// Strategy_TrionSoldier
public override float GetBaseOutputPower()
{
    // Trion兵：根据型号返回不同输出功率
    // 例如：精英兵种 = 70，普通兵种 = 40
    return 40f;
}

// Strategy_TrionBuilding
public override float GetBaseOutputPower()
{
    // 建筑：通常较低，因为不需要精细操作
    return 30f;
}
```

**优势**：
- ✅ 每个Strategy可定制BaseOutputPower
- ✅ 框架代码不依赖应用层方法
- ✅ 符合OOP设计原则

**影响**：
- 新增抽象方法，所有Strategy实现类必须实现
- CompTrion.CalculateOutputPower()改为调用strategy.GetBaseOutputPower()

---

### 3.4 优化方案4：debuff系统整合

**问题**：debuff的"休养舱加速"功能被移到应用层，但框架没有预留扩展点

**当前**（第11.3节）：
```
框架定义基础消退时间（12小时）
应用层可通过自定义HediffComp实现休养舱加速
```

**问题**：
- ⚠️ 如果应用层要实现加速，需要完全自定义HediffComp
- ⚠️ 无法复用框架的HediffCompProperties_Disappears
- ⚠️ 应用层工作量大

**优化后**（新增框架层支持）：

```csharp
// 框架定义接口
public interface IHediffDisappearModifier
{
    int GetDisappearTicks(Pawn pawn);
}

// 框架中HediffComp_Disappears可查询修饰符
public class HediffComp_Disappears : HediffComp
{
    public override void CompPostTick(ref float severityAdjustment)
    {
        // 获取修饰符（如果存在）
        var modifier = parent.pawn.TryGetComp<IHediffDisappearModifier>();
        int disappearTicks = modifier?.GetDisappearTicks(parent.pawn)
                            ?? prop.disappearsAfterTicks.RandomInRange;

        if (parent.ageTicks >= disappearTicks)
        {
            parent.pawn.health.RemoveHediff(parent);
        }
    }
}
```

**应用层实现（示例）**：
```csharp
// ProjectWT中创建
public class HediffDisappearModifier : ThingComp, IHediffDisappearModifier
{
    public int GetDisappearTicks(Pawn pawn)
    {
        // 检查是否在休养舱
        if (pawn.CurrentBed() is Building_MedicalBed bed)
        {
            return 15000;  // 6小时
        }
        return 30000;  // 12小时
    }
}
```

**优势**：
- ✅ 应用层只需实现接口，复用框架的Hediff系统
- ✅ 框架提供扩展点，不强制应用层自定义Comp
- ✅ 代码更简洁，耦合更低

**影响**：
- 框架新增IHediffDisappearModifier接口（轻量级）
- 框架HediffComp_Disappears扩展一个查询逻辑（3-5行代码）

---

## 四、优化方案总结

| 方案 | 问题 | 工作量 | 优先级 | 必要性 |
|------|------|--------|--------|--------|
| **3.1** OutputPower系统重构 | 框架/应用层分界不清 | 中 | P1 | ⭐⭐⭐⭐⭐ 必须 |
| **3.2** Harmony补丁规范 | 应用层需要猜测 | 小 | P2 | ⭐⭐⭐ 强烈建议 |
| **3.3** Strategy接口完善 | 缺少BaseOutputPower接口 | 小 | P1 | ⭐⭐⭐⭐ 建议 |
| **3.4** debuff系统扩展点 | 应用层实现困难 | 小 | P3 | ⭐⭐ 可选 |

---

## 五、重审结论

### 5.1 框架整体评分

| 维度 | 评分 | 说明 |
|------|------|------|
| **架构设计** | 9/10 | 单Comp+策略模式优秀，但OutPower定义需优化 |
| **完整性** | 8/10 | 覆盖所有战斗流程，但框架层定义不够清晰 |
| **扩展性** | 8/10 | Strategy模式好，但预留接口不足 |
| **API正确性** | 10/10 | 已修正所有错误 |
| **框架应用层分界** | 7/10 | 需要优化3.1-3.4方案 |
| **文档清晰度** | 8/10 | 总体清晰，但细节需补充 |
| **总体评分** | **8.3/10** | 🟢 **框架设计优秀，建议实施所有优化方案后进入应用层实现** |

### 5.2 主要优势

✅ **单一职责原则**：CompTrion专注数据和调度，不承担具体功能
✅ **策略模式应用**：清晰区分人类/兵器/建筑的生命周期
✅ **完整的系统覆盖**：消耗、恢复、伤害、状态管理都有
✅ **性能考量**：60 Tick批量计算设计合理
✅ **ModExtension支持**：适配RimWorld扩展体系

### 5.3 主要待改进点

⚠️ **OutputPower系统定义不够清晰**（优先级P1）
  - GetTrionTalent()是悬空方法
  - 建议实施优化方案3.1

⚠️ **Harmony补丁规范缺失**（优先级P2）
  - 应用层需要参考代码实现补丁
  - 建议实施优化方案3.2

⚠️ **Strategy接口不完整**（优先级P1）
  - 缺少GetBaseOutputPower()
  - 建议实施优化方案3.3

### 5.4 后续建议

#### 立即行动（必须）
1. ✅ 实施优化方案3.1（OutputPower系统重构）
2. ✅ 实施优化方案3.3（Strategy接口补全）

#### 强烈建议
3. ✅ 实施优化方案3.2（Harmony补丁规范）

#### 可选（v0.5或后续版本）
4. ⏳ 实施优化方案3.4（debuff系统扩展点）

---

## 六、版本升级路线

### v0.4 → v0.5（修复版）
实施**优先级P1**的两个方案：
- 3.1 OutputPower系统重构
- 3.3 Strategy接口补全

**工作量**：4-6小时
**预计时间**：3-5天

### v0.5 → v1.0（完善版）
实施**优先级P2**的方案：
- 3.2 Harmony补丁规范
- 完善所有Harmony补丁示例代码
- 增加应用层集成指南

**工作量**：6-8小时
**预计时间**：5-7天

### v1.0+ （扩展版本）
实施**可选**方案和新增需求：
- 3.4 debuff系统扩展点
- 其他应用层反馈的优化

---

## 七、框架与应用层的清晰分界

### 框架层明确职责（v0.5后）

**CompTrion**：
- ✅ 数据容器：Capacity/Reserved/Consumed/Available/OutputPower
- ✅ 消耗计算：Consume()方法
- ✅ 恢复接口：Recover()和ShouldRecover()接口
- ✅ 策略调度：调用strategy的各生命周期方法
- ❌ 具体数值：RecoveryRate等由应用层配置

**ILifecycleStrategy**：
- ✅ 生命周期规范：初始化、Tick、伤害、耗尽
- ✅ 扩展接口：GetRecoveryModifier()、GetBaseOutputPower()
- ❌ 具体实现：由应用层继承实现

**TriggerMount**：
- ✅ 挂载点管理：装备、激活、切换、状态机
- ✅ 消耗计算：汇总该挂载点的所有消耗
- ❌ Trigger功能：由应用层的TriggerWorker实现

**Harmony补丁规范**：
- ✅ 补丁模板和规范
- ❌ 具体补丁实现：由应用层创建

### 应用层明确职责

**ProjectWT**（应用层）：
- 继承实现三个Strategy类
- 实现GetBaseOutputPower()（从Trait读取天赋等级）
- 创建Harmony补丁
- 定义具体的Trigger功能
- 配置所有XML参数
- 实现AI、UI、渲染等游戏相关功能

---

## 八、总体建议

> 📌 **建议状态**：v0.4已经是**生产级框架**，可以进入应用层实现阶段

**但在应用层实现前，强烈建议：**

1. ✅ **应用优化方案3.1和3.3**（改进框架清晰度）
   - 工作量小，收益大
   - 让应用层集成更顺畅

2. ✅ **编写应用层集成指南**
   - 基于优化方案补充
   - 包含具体代码示例

3. ✅ **准备应用层代码框架**
   - 继承三个Strategy类
   - 实现IHediffDisappearModifier接口（如需要）
   - 创建Harmony补丁类

4. ✅ **准备测试计划**
   - 单元测试：核心方法
   - 集成测试：Harmony补丁
   - 性能测试：60 Tick批量计算

---

**审阅者**：Claude (Sonnet 4.5)
**审阅时间**：2026-01-10
**建议状态**：✅ **通过条件审阅** - 建议实施P1优化方案后进入应用层

---

## 历史记录

| 版本号 | 改动内容 | 修改时间 | 修改者 |
|--------|---------|---------|--------|
| v1.0 | 初版重审报告，提出4个优化方案 | 2026-01-10 | assistant |
