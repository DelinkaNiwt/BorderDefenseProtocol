# Trion框架实现指南：用例清单

---

## 文档元信息

**摘要**：基于Trion框架v0.5和ProjectTrion代表性用例列表，详细说明如何实现所有需求的系统、物品、建筑、单位等。清晰列出框架支持项、应用层实现项、依赖关系。

**版本号**：v1.0
**修改时间**：2026-01-10
**标签**：[待审]

---

## 一、物品系统

### 1.1 核心物品

| 物品名 | 类型 | 框架支持 | 应用层任务 | 优先级 |
|--------|------|---------|-----------|--------|
| **Trion驱动核心** | 关键物品 | ThingDef + Comp系统 | 建筑充能动力源，定义价值和效果 | P0 |
| **Trion子核** | 模块 | ThingDef | 用于建筑配置，小型充能元件 | P1 |
| **触发器** | 装备 | CompTrion自动识别 | 定义ThingDef，装备后自动激活框架 | P0 |
| **制式组件芯片** | 配置 | TriggerComponentDef | 定义组件属性（占用、消耗、效果） | P0 |
| **Trion基因** | 消耗品 | 可选 | 永久或临时修改天赋 | P2 |
| **Trion腺体** | 植入体 | Hediff系统 | 通过特殊Hediff实现效果 | P2 |

### 1.2 物品实现示例

```csharp
// 触发器ThingDef（装备后自动激活框架）
<thingDef>
    <defName>Item_TrionTrigger_Standard</defName>
    <label>制式触发器</label>
    <thingClass>ThingWithComps</thingClass>
    <comps>
        <li Class="CompTrion" />  <!-- 自动添加核心组件 -->
    </comps>
    <equippedStatOffsets>
        <li>
            <stat>CarryCapacity</stat>
            <value>-2</value>  <!-- 装备后影响属性 -->
        </li>
    </equippedStatOffsets>
</thingDef>

// 组件芯片定义（可装备到触发器）
<TriggerComponentDef>
    <defName>Trigger_ShotgunBlast</defName>
    <label>炸裂弹</label>
    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>1</usageCost>          <!-- 每发1 Trion -->
    <activationDelay>1</activationDelay>
    <requiredSlot>RightHand</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_ShotgunBlast</workerClass>
</TriggerComponentDef>
```

---

## 二、建筑系统

### 2.1 建筑类型与框架支持

| 建筑名 | 功能 | 框架支持 | 应用层实现 | 优先级 |
|--------|------|---------|-----------|--------|
| **Trion驱动塔** | 提供Trion动力 | 无 | 管理资源分配，支持建筑联动 | P1 |
| **Trion天赋检测仪** | 检测殖民者天赋 | 无 | 调用天赋系统，显示输出功率 | P1 |
| **Trion休养舱** | 加速debuff消退 | CompTrion回滚逻辑 | 检测Pawn是否在舱内，调整debuff时间 | P1 |
| **Trion生产装配台** | 制造组件芯片 | 无 | 工作台UI，配方系统 | P2 |
| **触发器配置台** | 装配组件到触发器 | TriggerMount系统 | UI实现组件选择和装备 | P1 |
| **操作员控制台** | 全局Trion管理 | CompTrion数据查询 | 实时显示所有Trion实体状态 | P2 |
| **Trion兵培育仓** | 制造Trion兵 | Strategy_TrionSoldier | 定义兵器类型，输出功率等 | P1 |
| **Trion兵维护站** | 休眠/充能/维修 | CompTrion恢复机制 | 自动恢复被动Trion兵的能量 | P2 |
| **紧急脱离传送锚** | Bail Out目标 | Strategy.TriggerBailOut() | 标记为传送目标，可见性标记 | P0 |
| **Trion护盾发生器** | 防守建筑 | 无 | 独立的护盾系统（非Trigger护盾） | P3 |
| **Trion炮台** | 自动防守 | Strategy_TrionBuilding + 子弹系统 | 定义炮台类型，伤害等 | P1 |
| **传送站台** | 远程传送 | 无 | 传送逻辑，能量消耗 | P3 |
| **Trion远征艇** | 远征装备 | 可选 | 超出框架范围的高级功能 | P4 |
| **模拟战斗房间** | 训练设施 | 可选 | 超出框架范围的高级功能 | P4 |

### 2.2 关键建筑实现示例

```csharp
// 1. 紧急脱离传送锚（P0优先级）
<thingDef>
    <defName>Building_TransferAnchor</defName>
    <label>紧急脱离传送锚</label>
    <thingClass>Building</thingClass>
    <placingDraggableDimensions>1</placingDraggableDimensions>
    <castEdgeShadows>false</castEdgeShadows>
    <staticSunShadowHeight>0.0</staticSunShadowHeight>
    <passability>Impassable</passability>
    <comps>
        <li Class="CompFlickable" />
    </comps>
    <graphicData>
        <!-- 定义外观 -->
    </graphicData>
</thingDef>

// 2. Trion天赋检测仪（P1优先级）
<thingDef>
    <defName>Building_TrionTalentDetector</defName>
    <label>Trion天赋检测仪</label>
    <thingClass>ProjectWT.Building_TrionDetector</thingClass>
    <comps>
        <li Class="CompPower">
            <compClass>CompPowerTrader</compClass>
            <basePowerConsumption>100</basePowerConsumption>
        </li>
    </comps>
</thingDef>

// 应用层C#实现
public class Building_TrionDetector : Building
{
    public void DetectPawnTalent(Pawn pawn)
    {
        var comp = pawn.TryGetComp<CompTrion>();
        var talentComp = pawn.GetComponent<TrionTalentComp>();

        if (talentComp != null)
        {
            // 显示天赋等级和输出功率
            Messages.Message(
                $"{pawn.Name} - Trion Capacity: {talentComp.TalentCapacity}, Output Power: {comp?.OutputPower}",
                pawn,
                MessageTypeDefOf.NeutralEvent);
        }
    }
}

// 3. Trion休养舱（P1优先级）
<thingDef>
    <defName>Building_TrionRestChamber</defName>
    <label>Trion休养舱</label>
    <thingClass>ProjectWT.Building_TrionRestChamber</thingClass>
    <comps>
        <li Class="CompPower">
            <basePowerConsumption>200</basePowerConsumption>
        </li>
    </comps>
</thingDef>

public class Building_TrionRestChamber : Building
{
    public override void Tick()
    {
        base.Tick();

        // 检查舱内的Pawn
        var innerCells = GenAdj.CellsOccupiedBy(this);
        foreach (var cell in innerCells)
        {
            var pawn = cell.GetFirstPawn(Map);
            if (pawn != null)
            {
                // 加速debuff"Trion枯竭"的消退
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hediff_TrionDepleted);
                if (hediff != null && hediff.ticksToDisappear.HasValue)
                {
                    hediff.ticksToDisappear -= 2;  // 加速2倍
                }
            }
        }
    }
}

// 4. 触发器配置台（P1优先级）
<thingDef>
    <defName>Building_TriggerConfigurator</defName>
    <label>触发器配置台</label>
    <thingClass>ProjectWT.Building_TriggerConfigurator</thingClass>
    <workSpeedStat>CraftingSpeed</workSpeedStat>
    <fillPercent>0.5</fillPercent>
</thingDef>

public class Building_TriggerConfigurator : Building_WorkTable
{
    public void ConfigureTrigger(Pawn worker, Item_TrionTrigger trigger, TriggerComponentDef componentDef)
    {
        // 调用TriggerMount.TryEquip()
        var comp = trigger.TryGetComp<CompTrion>();

        // 查找合适的挂载点
        var mount = comp.Mounts.FirstOrDefault(m => m.slotTag == componentDef.requiredSlot);

        if (mount != null && mount.equippedList.Count < 4)
        {
            // 添加组件到挂载点
            var triggerComponent = new TriggerComponent { def = componentDef };
            mount.equippedList.Add(triggerComponent);

            // 显示反馈
            Messages.Message($"Component {componentDef.label} equipped to {componentDef.requiredSlot}");
        }
    }
}
```

---

## 三、单位系统

### 3.1 Trion兵

| 需求 | 框架支持 | 应用层实现 | 优先级 |
|------|---------|-----------|--------|
| **Trion兵生成** | Strategy_TrionSoldier + CompTrion | 定义PawnKindDef，添加CompTrion | P1 |
| **多种兵器类型** | Strategy工厂方法支持扩展 | 新增Strategy子类或配置Def | P1 |
| **直接销毁机制** | Strategy.OnDepleted() → parent.Destroy() | 确保Trion兵配置正确 | P1 |
| **无快照/回滚** | Strategy_TrionSoldier不实现快照 | 无需额外实现 | 自动 |
| **无Bail Out** | Strategy_TrionSoldier不支持 | 无需额外实现 | 自动 |
| **Trion耗尽后爆炸** | Strategy.OnDepleted()中实现 | 配置爆炸特效Def | P1 |

### 3.2 Trion兵实现示例

```csharp
// 定义Trion兵的PawnKind
<pawnKindDef>
    <defName>TrionSoldier_Basic</defName>
    <label>基础Trion兵</label>
    <race>TrionSoldier</race>  <!-- 自定义种族 -->
    <biocodeWeapons>false</biocodeWeapons>
    <apparelAllowHeadgear>false</apparelAllowHeadgear>
</pawnKindDef>

// 定义Trion兵种族
<thingDef>
    <defName>TrionSoldier</defName>
    <thingClass>Pawn</thingClass>
    <race>
        <defName>TrionSoldier</defName>
        <label>Trion兵</label>
        <baseBodySize>0.9</baseBodySize>
        <fleshColor>(100, 100, 100)</fleshColor>
        <lifeExpectancy>10</lifeExpectancy>
        <leatherDef>Leather_Fake</leatherDef>
    </race>
    <comps>
        <li Class="CompTrion">          <!-- 核心组件 -->
            <capacity>500</capacity>    <!-- Trion兵容量更小 -->
        </li>
    </comps>
</thingDef>

// 标记为TrionSoldier的扩展
<DefModExtension>
    <defName>TrionSoldierMarker</defName>
    <class>ProjectWT.TrionSoldierMarker</class>
</DefModExtension>

// 在PawnKindDef中使用
<pawnKindDef>
    <defName>TrionSoldier_Basic</defName>
    <modExtensions>
        <li Class="ProjectWT.TrionSoldierMarker" />
    </modExtensions>
</pawnKindDef>

// 应用层：Trion兵生成时配置
public static void GenerateTrionSoldier(PawnGenerationRequest request)
{
    var pawn = PawnGenerator.GeneratePawn(request);
    var comp = pawn.TryGetComp<CompTrion>();

    if (comp != null)
    {
        // 检查是否标记为TrionSoldier
        if (pawn.kindDef.HasModExtension<TrionSoldierMarker>())
        {
            // Strategy_TrionSoldier会自动选择
            comp.Capacity = 500;  // Trion兵容量
            comp.OutputPower = 50f;  // 固定输出功率
        }
    }

    return pawn;
}

// 框架Strategy_TrionSoldier的爆炸效果
public override void OnDepleted()
{
    // 播放爆炸特效
    FleckMaker.Static(comp.parent.Position, comp.parent.Map, FleckDefOf.ExplosionFlash);

    // 直接销毁
    comp.parent.Destroy(DestroyMode.Vanish);
}
```

---

## 四、能力系统

### 4.1 能力分类

| 能力类型 | 例子 | 框架支持 | 应用层实现 | 优先级 |
|---------|------|---------|-----------|--------|
| **Trigger基础能力** | 弧月（切割）、炸裂弹（射击） | TriggerWorker基类 | 具体Worker实现 | P0 |
| **Trigger能力组件** | 弧月旋空（范围斩击） | 输出功率判定 + usageCost | 定义TriggerComponentDef | P1 |
| **特殊能力** | Bail Out（紧急脱离） | Framework内置机制 | UI按钮触发 | P0 |
| **被动效果** | 护盾、变色龙 | TriggerComponent.OnActivated/OnDeactivated | Effect实现 | P0 |

### 4.2 能力实现示例

```csharp
// 弧月（近战武器）- 基础能力
<TriggerComponentDef>
    <defName>Trigger_ArcBlade</defName>
    <label>弧月</label>
    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>0</usageCost>           <!-- 近战无持续消耗 -->
    <activationDelay>1</activationDelay>
    <requiredOutputPower>0</requiredOutputPower>
    <requiredSlot>LeftHand</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_ArcBlade</workerClass>
</TriggerComponentDef>

public class TriggerWorker_ArcBlade : TriggerWorker
{
    public override void OnActivated()
    {
        // 激活时的效果（例如武器光效）
    }

    public override void OnAttack(Pawn user, LocalTargetInfo target)
    {
        // 近战攻击逻辑（框架不消耗Trion）
        // 仅调用原生近战系统
    }
}

// 炸裂弹（远程武器）- 使用型能力
<TriggerComponentDef>
    <defName>Trigger_ShotgunBlast</defName>
    <label>炸裂弹</label>
    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>1</usageCost>            <!-- 每发1 Trion -->
    <activationDelay>1</activationDelay>
    <requiredOutputPower>0</requiredOutputPower>
    <requiredSlot>RightHand</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_ShotgunBlast</workerClass>
</TriggerComponentDef>

public class TriggerWorker_ShotgunBlast : TriggerWorker
{
    public override void Fire(Pawn user, LocalTargetInfo target)
    {
        var comp = user.TryGetComp<CompTrion>();

        // 支付使用费用
        comp.Consume(def.usageCost);

        // 执行射击
        GenExplosion.DoExplosion(
            target.Cell,
            user.Map,
            def.range,
            DamageDefOf.Bullet,
            user,
            def.damage);
    }
}

// 弧月旋空（范围能力）- 高阶能力
<TriggerComponentDef>
    <defName>Trigger_ArcSlash</defName>
    <label>弧月旋空</label>
    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>50</usageCost>            <!-- 释放一次消耗50 Trion -->
    <activationDelay>2</activationDelay>
    <requiredOutputPower>50</requiredOutputPower>  <!-- 需要输出功率≥50 -->
    <requiredSlot>RightHand</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_ArcSlash</workerClass>
</TriggerComponentDef>

public class TriggerWorker_ArcSlash : TriggerWorker
{
    public override bool CanUse(Pawn user)
    {
        var comp = user.TryGetComp<CompTrion>();

        // 检查输出功率
        if (comp.OutputPower < def.requiredOutputPower)
        {
            Messages.Message("Output Power not sufficient!");
            return false;
        }

        return true;
    }

    public override void Fire(Pawn user, LocalTargetInfo target)
    {
        var comp = user.TryGetComp<CompTrion>();

        // 支付使用费用
        comp.Consume(def.usageCost);

        // 执行范围斩击
        var cells = GenRadial.RadialCellsAround(target.Cell, def.range);
        foreach (var cell in cells)
        {
            var things = cell.GetThingList(user.Map);
            foreach (var thing in things)
            {
                if (thing is Pawn pawn && pawn.Faction != user.Faction)
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Slash, 30f));
                }
            }
        }

        // 特效
        FleckMaker.Static(target.Cell, user.Map, FleckDefOf.ExplosionFlash);
    }
}

// 护盾（被动能力）
<TriggerComponentDef>
    <defName>Trigger_Shield</defName>
    <label>护盾</label>
    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>0</usageCost>
    <isShield>true</isShield>
    <blockChance>0.5</blockChance>
    <damageReduction>0.5</damageReduction>
    <blockCost>5</blockCost>
    <requiredSlot>LeftHand</requiredSlot>
</TriggerComponentDef>

public class TriggerWorker_Shield : TriggerWorker
{
    public override void OnActivated()
    {
        // 护盾激活特效
    }

    // 护盾抵挡逻辑由框架Strategy_HumanCombatBody实现
}

// 变色龙（持续消耗能力）
<TriggerComponentDef>
    <defName>Trigger_Chameleon</defName>
    <label>变色龙</label>
    <reserveCost>10</reserveCost>
    <activationCost>5</activationCost>
    <sustainCost>1</sustainCost>        <!-- 每60 Tick消耗1 -->
    <usageCost>0</usageCost>
    <activationDelay>1</activationDelay>
    <requiredSlot>Sub</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_Chameleon</workerClass>
</TriggerComponentDef>

public class TriggerWorker_Chameleon : TriggerWorker
{
    public override void OnActivated()
    {
        // 启用隐身效果
        var pawn = (Pawn)comp.parent;
        pawn.Drawer.renderer.graphics.ClearCache();  // 刷新渲染
    }

    public override void OnTick()
    {
        // 每Tick更新隐身状态
        var pawn = (Pawn)comp.parent;

        // 设置透明度
        // 注：RimWorld中通过自定义Graphic实现隐身
    }

    public override void OnDeactivated()
    {
        // 关闭隐身效果
    }
}

// Bail Out（紧急脱离）- 特殊能力
<TriggerComponentDef>
    <defName>Trigger_BailOut</defName>
    <label>紧急脱离系统</label>
    <reserveCost>400</reserveCost>      <!-- 占用大量Trion -->
    <activationCost>0</activationCost>
    <sustainCost>0</sustainCost>
    <usageCost>0</usageCost>
    <requiredSlot>Sub</requiredSlot>
    <workerClass>ProjectWT.TriggerWorker_BailOut</workerClass>
</TriggerComponentDef>

public class TriggerWorker_BailOut : TriggerWorker
{
    // Bail Out逻辑完全由框架Strategy_HumanCombatBody.TriggerBailOut()实现
    // 应用层只需定义占用值和UI按钮
}
```

---

## 五、特性系统

### 5.1 特性分类

| 特性类型 | 例子 | 框架支持 | 应用层实现 | 优先级 |
|---------|------|---------|-----------|--------|
| **天赋特性** | Trion天赋等级 | 不直接支持，通过天赋系统 | TrionTalentComp | P1 |
| **修正特性** | Trion高恢复、精通 | ModExtension系统 | 定义TraitDef和扩展 | P1 |
| **副作用特性** | 需求增加、效率低 | 可选 | 在Strategy中应用 | P2 |

### 5.2 特性实现示例

```csharp
// Trion精通特性（提升输出功率+10）
<traitDef>
    <defName>Trait_TrionMastery</defName>
    <label>Trion精通</label>
    <description>这个人对Trion的理解更深刻，能够更高效地操控它。</description>
    <skipChance>0.5</skipChance>
    <commonality>0.1</commonality>
    <modExtensions>
        <li Class="ProjectWT.OutputPowerModifier">
            <modifier>10</modifier>  <!-- +10输出功率 -->
        </li>
    </modExtensions>
</traitDef>

// Trion高恢复特性（恢复速率+50%）
<traitDef>
    <defName>Trait_TrionFastRecovery</defName>
    <label>Trion快恢复</label>
    <description>这个人的Trion恢复速度异常快。</description>
    <modExtensions>
        <li Class="ProjectWT.TrionRecoveryModifier">
            <modifier>0.5</modifier>  <!-- +50% -->
        </li>
    </modExtensions>
</traitDef>

// 定义ModExtension类
public class OutputPowerModifier : DefModExtension
{
    public float modifier;  // 直接加到输出功率
}

public class TrionRecoveryModifier : DefModExtension
{
    public float modifier;  // 乘以恢复速率
}

// 在Strategy中应用
public override float ModifyOutputPower(float baseOutput)
{
    float modified = baseOutput;

    foreach (var trait in pawn.story.traits.allTraits)
    {
        if (trait.def.HasModExtension<OutputPowerModifier>())
        {
            modified += trait.def.GetModExtension<OutputPowerModifier>().modifier;
        }
    }

    return modified;
}

public override float GetRecoveryModifier()
{
    float modifier = 1.0f;

    foreach (var trait in pawn.story.traits.allTraits)
    {
        if (trait.def.HasModExtension<TrionRecoveryModifier>())
        {
            modifier += trait.def.GetModExtension<TrionRecoveryModifier>().modifier;
        }
    }

    return modifier;
}
```

---

## 六、事件系统

| 事件 | 框架触发点 | 应用层响应 | 优先级 |
|------|-----------|-----------|--------|
| **战斗体生成** | Strategy.OnInitialize() | 播放特效、UI更新 | P2 |
| **战斗体破裂** | Strategy.OnDepleted() | 播放爆炸特效、debuff施加 | P0 |
| **组件激活** | TriggerMount.TickActivation()完成 | 特效反馈 | P2 |
| **护盾抵挡** | Strategy_HumanCombatBody.TryBlockDamage() | 特效和声音 | P1 |
| **部位损毁** | OnBodyPartLost() | 调用mount.OnPartDestroyed() | P0 |
| **Bail Out触发** | Strategy.TriggerBailOut() | 传送特效 | P0 |
| **Trion耗尽debuff施加** | AddTrionDepletedDebuff() | debuff效果应用 | P0 |

---

## 七、实现优先级清单

### P0级（阻塞性，必须先完成）
- [ ] CompTrion核心组件
- [ ] Strategy三个实现类
- [ ] TriggerMount和TriggerComponent
- [ ] Harmony拦截补丁（伤害、截肢）
- [ ] 天赋系统（TrionTalentComp）
- [ ] 快照机制（PawnSnapshot）
- [ ] 触发器和制式触发器（ThingDef）
- [ ] 组件芯片定义（TriggerComponentDef）
- [ ] 传送锚建筑
- [ ] debuff定义

### P1级（核心功能，周期1内完成）
- [ ] 基础组件实现（弧月、炸裂弹、护盾、变色龙）
- [ ] Trion兵生成系统
- [ ] 天赋检测仪建筑
- [ ] 触发器配置台
- [ ] 休养舱加速debuff消退
- [ ] 炮台建筑
- [ ] 兵维护站
- [ ] 特性系统（精通、高恢复）
- [ ] Bail Out按钮/Gizmo

### P2级（增强功能，周期2内完成）
- [ ] 能力型组件（弧月旋空）
- [ ] 建筑间联动系统
- [ ] 操作员控制台
- [ ] Trion基因系统
- [ ] 高阶组件

### P3级（可选功能）
- [ ] Trion护盾发生器
- [ ] 传送站台
- [ ] Trion远征艇
- [ ] 模拟战斗房间

---

## 八、框架覆盖度验证

### 功能完整性检查

```
✅ 物品系统
  ├─ 触发器装备 - 自动激活CompTrion
  ├─ 组件芯片 - 通过TriggerComponentDef定义
  └─ 其他消耗品 - 可选扩展

✅ 建筑系统
  ├─ 传送锚 - Bail Out目标
  ├─ 天赋检测 - 查询CompTrion数据
  ├─ 休养舱 - 修改debuff持续时间
  ├─ 配置台 - 调用TriggerMount.TryEquip()
  ├─ 炮台 - Strategy_TrionBuilding支持
  └─ 维护站 - CompTrion恢复机制

✅ 单位系统
  ├─ 人类 - Strategy_HumanCombatBody
  ├─ Trion兵 - Strategy_TrionSoldier
  └─ 建筑 - Strategy_TrionBuilding

✅ 能力系统
  ├─ 基础能力 - TriggerWorker实现
  ├─ 范围能力 - 输出功率检查
  ├─ 被动效果 - OnActivated/OnDeactivated
  ├─ 特殊能力（Bail Out）- 框架内置
  └─ 持续消耗 - sustainCost机制

✅ 特性系统
  ├─ 天赋特性 - TrionTalentComp
  └─ 修正特性 - ModExtension + Strategy

✅ 事件系统 - 框架提供所有触发点
```

---

## 九、跨系统依赖关系

```
战斗核心（必须实现）
  ├─ CompTrion
  ├─ Strategy三个实现
  └─ Harmony补丁（3个）
      ↓
天赋系统（依赖战斗核心）
  ├─ TrionTalentComp
  └─ OutputPower计算
      ↓
组件系统（依赖战斗核心）
  ├─ TriggerComponentDef
  ├─ TriggerWorker实现
  └─ 每个组件的ActivationDelay/Cost
      ↓
建筑系统（依赖组件系统）
  ├─ 天赋检测仪 → 查询OutputPower
  ├─ 配置台 → TriggerMount.TryEquip()
  ├─ 休养舱 → 修改debuff
  ├─ 炮台 → Strategy_TrionBuilding + 组件
  └─ 维护站 → CompTrion恢复
      ↓
高阶功能（依赖基础系统）
  ├─ 特性系统
  ├─ Bail Out完整逻辑
  └─ 事件反馈
```

---

**需求架构师**
*2026-01-10*
