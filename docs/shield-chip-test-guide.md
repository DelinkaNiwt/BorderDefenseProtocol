# 护盾芯片测试指南

## 芯片信息

**芯片名称**: BDP_Chip_Shield（护盾芯片）

**芯片类型**: 防御型芯片（Hediff芯片）

**槽位限制**: 特殊槽位（SpecialOnly）

**Trion成本**:
- 激活消耗: 20 Trion
- 占用成本: 50 Trion
- 持续消耗: 0（护盾抵挡时才消耗）
- 抵挡消耗: 伤害值 × 0.5

**时间配置**:
- 激活预热: 1秒（60 ticks）
- 关闭延迟: 0.5秒（30 ticks）

---

## 功能说明

护盾芯片激活后会在使用者身上添加 `BDP_Shield` hediff，提供全方位能量护盾：

1. **全方位防护**: 可以抵挡来自任意方向的远程攻击
2. **Trion消耗**: 每次抵挡会消耗Trion（伤害值 × 0.5）
3. **自动失效**: 当Trion不足时护盾失效，伤害穿透
4. **视觉特效**: 抵挡成功时播放灵能折跃护盾特效

---

## 游戏内测试步骤

### 1. 生成护盾芯片

```csharp
// Dev Mode控制台
Thing chip = ThingMaker.MakeThing(BDP_DefOf.BDP_Chip_Shield);
GenPlace.TryPlaceThing(chip, UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near);
```

### 2. 装载芯片到触发体

1. 选中一个装备了触发体的pawn
2. 打开触发体界面
3. 将护盾芯片插入特殊槽位

### 3. 激活护盾芯片

1. 在触发体界面中点击护盾芯片
2. 点击"激活"按钮
3. 等待1秒预热
4. 观察pawn的健康标签页，应该看到 `BDP_Shield` hediff

### 4. 测试护盾功能

**测试1: 基础抵挡**
```csharp
// 确保pawn有足够的Trion
var comp = Find.Selector.SingleSelectedThing.GetComp<CompTrion>();
Log.Message($"Trion: {comp.Cur}/{comp.Max}, Available: {comp.Available}");

// 从远处攻击pawn，观察：
// - 伤害被完全吸收
// - 播放护盾特效
// - Trion按公式消耗
```

**测试2: Trion不足**
```csharp
// 将Trion降至很低
var comp = Find.Selector.SingleSelectedThing.GetComp<CompTrion>();
comp.Consume(comp.Available - 5f);

// 攻击pawn，观察：
// - 护盾因Trion不足而失效
// - 伤害穿透
```

**测试3: 关闭芯片**
```csharp
// 在触发体界面中关闭护盾芯片
// 观察：
// - 等待0.5秒关闭延迟
// - BDP_Shield hediff被移除
// - 攻击pawn，伤害正常造成
```

### 5. 验证Trion消耗

```csharp
// 记录初始Trion
var comp = Find.Selector.SingleSelectedThing.GetComp<CompTrion>();
float initialTrion = comp.Available;
Log.Message($"初始Trion: {initialTrion}");

// 造成20点伤害（被护盾抵挡）
// 预期消耗: 20 × 0.5 = 10 Trion

// 检查最终Trion
float finalTrion = comp.Available;
Log.Message($"最终Trion: {finalTrion}");
Log.Message($"消耗Trion: {initialTrion - finalTrion}");
```

---

## 预期结果

✅ 护盾芯片可以正常装载到特殊槽位
✅ 激活后添加BDP_Shield hediff
✅ 护盾可以抵挡远程攻击
✅ 抵挡时播放特效
✅ Trion按公式消耗（damage × 0.5）
✅ Trion不足时护盾失效
✅ 关闭芯片后移除hediff
✅ 护盾不阻止pawn向外射击

---

## 常见问题

**Q: 护盾芯片无法装载？**
A: 检查是否插入了特殊槽位（不是左右手槽位）

**Q: 护盾没有抵挡伤害？**
A: 检查：
1. 芯片是否已激活
2. pawn是否有BDP_Shield hediff
3. pawn是否有足够的Trion
4. 伤害类型是否是远程/爆炸伤害

**Q: 护盾消耗Trion太快？**
A: 可以在XML中调整 `trionCostMultiplier` 参数（默认0.5）

---

## 扩展配置

如果需要创建其他类型的护盾芯片，可以修改XML：

```xml
<!-- 前方护盾芯片示例 -->
<ThingDef ParentName="BDP_ChipBase">
  <defName>BDP_Chip_ShieldFront</defName>
  <label>前方护盾芯片</label>
  <!-- ... 其他配置 ... -->
  <modExtensions>
    <li Class="BDP.Trigger.HediffChipConfig">
      <hediffDef>BDP_ShieldFront</hediffDef> <!-- 使用前方护盾hediff -->
    </li>
  </modExtensions>
</ThingDef>
```

---

**创建时间**: 2026-03-09
**测试状态**: 待测试
