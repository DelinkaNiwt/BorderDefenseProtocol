---
标题：武器身份代理系统 - 新会话快速启动提示词
版本号: v1.0
更新日期: 2026-03-07
最后修改者: Claude Opus 4.6
标签: [文档][用户未确认][已完成][未锁定]
摘要: 用于开启新Claude会话时快速恢复上下文的提示词模板
---

# 新会话快速启动提示词

## 使用方法
复制下方提示词，粘贴到新的Claude会话中即可。根据当前进度修改"当前进度"部分。

---

## 提示词模板

```
我正在为RimWorld模组 BorderDefenseProtocol (BDP) 实施"武器身份代理系统"（Weapon Identity Facade）。

## 背景
BDP的触发器系统有一个"武器信息孤岛"问题：触发体（TriggerBody）是装备槽中的武器Thing，但它只是空壳容器。真正的武器能力来自动态激活的芯片（Chip），芯片不在equipment槽位，导致原版的Info Card、检视面板、Tooltip、Stat系统全都拿不到真实武器信息。

## 解决方案
通过ThingComp虚方法override（TransformLabel、CompInspectStringExtra、CompTipStringExtra、SpecialDisplayStats、GetStatOffset、GetStatFactor），让触发体成为"动态镜子"，投射激活芯片的武器信息。同时增强芯片自身的Info Card展示。

## 关键文件
- 计划文档：docs/plans/2026-03-07-weapon-identity-facade-plan.md
- 任务清单：docs/plans/2026-03-07-weapon-identity-facade-checklist.md
- 触发体核心：Source/BDP/Trigger/Comps/CompTriggerBody.cs（主类+IVerbOwner）
- 触发体分部类：Source/BDP/Trigger/Comps/CompTriggerBody.*.cs（11个分部类）
- 芯片配置：Source/BDP/Trigger/Comps/CompProperties_TriggerChip.cs
- 武器芯片数据：Source/BDP/Trigger/Data/VerbChipConfig.cs
- Hediff芯片数据：Source/BDP/Trigger/Effects/HediffChipEffect.cs（内含HediffChipConfig）
- Ability芯片数据：Source/BDP/Trigger/Effects/AbilityChipEffect.cs（内含AbilityChipConfig）
- FireMode：Source/BDP/FireMode/CompFireMode.cs
- 现有UI：Source/BDP/Trigger/UI/（3个文件）

## 关键现有API（在CompTriggerBody各分部类中）
- AllActiveSlots() → IEnumerable<ChipSlot>（SlotManagement.cs）
- GetActiveSlot(SlotSide) → ChipSlot（SlotManagement.cs）
- HasAnyActiveChip() → bool（SlotManagement.cs）
- GetChipExtension<T>() → T（CompTriggerBody.cs，但依赖ActivatingSlot上下文）
- 芯片Thing → slot.loadedChip
- 芯片配置 → slot.loadedChip.def.GetModExtension<VerbChipConfig>()
- FireMode → slot.loadedChip.TryGetComp<CompFireMode>()
- SlotSide枚举：LeftHand, RightHand, Special
- ChipSlot字段：index, side, loadedChip, isActive, isDisabled

## 当前进度
【在此标注当前阶段和具体进度】
- 阶段1: 未开始 / 进行中（具体到哪一步）/ 已完成
- 阶段2: 未开始 / 进行中 / 已完成
- 阶段3: 未开始 / 进行中 / 已完成
- 阶段4: 未开始 / 进行中 / 已完成

## 本次任务
请先阅读计划文档和任务清单，然后从当前进度继续实施。
每完成一个阶段后请更新任务清单中的checkbox。
```

---

## 阶段专用提示词（可选补充）

### 阶段1专用补充

```
本次专注阶段1：新建 CompTriggerBody.Display.cs 分部类。

要实现的虚方法（CompTriggerBody当前无任何override）：
1. TransformLabel(string label) → 动态名称
2. CompInspectStringExtra() → 检视面板
3. CompTipStringExtra() → 悬停提示
4. SpecialDisplayStats() → Info Card stat条目

注意事项：
- C# 7.3语法
- 中文注释
- 无芯片激活时返回null/原值（向后兼容）
- 从slot.loadedChip.def.GetModExtension<VerbChipConfig>()读数据，不走ActivatingSlot
- 伤害从VerbProperties.defaultProjectile.projectile.GetDamageAmount(1f)读取
- 暂时可以用硬编码中文字符串，阶段4统一替换为Translate()
- 编译命令见CLAUDE.md
```

### 阶段2专用补充

```
本次专注阶段2：芯片Info Card增强。

需要确认CompProperties_TriggerChip的compClass指向什么类。
如果是直接ThingComp → 新建CompTriggerChipRuntime。
如果已有具名类 → 在其中添加SpecialDisplayStats()。

按芯片类型分支展示：
- 所有芯片：基础信息（成本、冷却等）
- VerbChipConfig → 武器参数（伤害、射程等）
- HediffChipConfig → Hediff描述
- AbilityChipConfig → Ability描述
```

### 阶段3专用补充

```
本次专注阶段3：Stat管线桥接。

1. 新建 ChipStatConfig : DefModExtension（equippedStatOffsets + equippedStatFactors）
2. 在CompTriggerBody.Display.cs中添加：
   - GetStatOffset(StatDef) — 遍历AllActiveSlots聚合
   - GetStatFactor(StatDef) — 遍历AllActiveSlots聚合
   - GetStatsExplanation(StatDef, StringBuilder, string) — 明细

原版ThingComp的GetStatOffset/GetStatFactor已由StatWorker.GetValueUnfinalized自动调用。
原版CompUniqueWeapon也使用同样的模式，可参考其实现。
```

### 阶段4专用补充

```
本次专注阶段4：第三方兼容 + 翻译 + StatCategory。

1. 公开API：GetActiveWeaponVerbProperties(), GetActiveWeaponDPS(), GetActiveWeaponRange()
2. StatCategoryDef XML + DefOf类
3. 翻译键XML
4. 回到阶段1/2代码，把硬编码字符串替换为Translate()
```

---

## 历史修改记录
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-03-07 | 初始提示词模板 | Claude Opus 4.6 |
