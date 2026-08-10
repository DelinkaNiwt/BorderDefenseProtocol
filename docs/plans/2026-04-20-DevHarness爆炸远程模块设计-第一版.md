# DevHarness（开发测试模组）爆炸远程模块设计-第一版

## 1. 目标

给 `BorderDefenseProtocol.DevHarness（BDP 开发测试模组）` 增加一个远程射击模块：

- 名称：爆炸模块。
- 效果：投射物终结命中时，在实际落点发生爆炸。
- 爆炸：可配置半径的 `AOE（范围）` 伤害。
- 预览：玩家瞄准时显示爆炸覆盖范围。

一句话：

```text
把“爆炸”做成远程模块效果，
不把它做成新弹体系统，也不扩主模组架构。
```

---

## 2. 需求辨析

这是业务功能，不是架构设施。

因此本次设计只新增测试模组业务模块：

- 不改主模组远程协议主干。
- 不新写爆炸结算系统。
- 不绕过原版爆炸机制。
- 不把测试业务塞进教学骨架模块。

---

## 3. 现有可复用能力

### 3.1 模块挂载

主模组已有 `BdpRangedAttackModuleDef（BDP 远程攻击模块定义）`。

它允许测试模组通过 `runtimeClass（运行时类型）` 和 `defaultConfig（默认配置）` 挂上业务模块。

### 3.2 瞄准预览

主模组已有 `Preview（预览）` 阶段。

模块可以向 `PreviewRecord.DrawItems（预览绘制项列表）` 追加：

- `Ring（圆环）`
- `CellGroup（格子组）`
- `Line（线段）`
- `Label（文字）`

爆炸预览应使用 `CellGroup（格子组）`，因为原版爆炸会受墙体和视线影响，只画圆环会误导玩家。

### 3.3 命中结算

主模组已有 `Impact（终结结算）` 阶段。

模块可以提交 `AreaEffectPlan（范围效果计划）`。

`BdpProjectile（BDP 投射物宿主）` 已经把 `AreaEffectPlan（范围效果计划）` 落回原版 `GenExplosion.DoExplosion（原版爆炸执行接口）`。

所以爆炸模块只需要提交计划，不需要自己执行爆炸。

---

## 4. 选定方案

采用独立模块方案：

```text
ExplosiveModule（爆炸模块）
    ├─ Preview（预览阶段）：画爆炸覆盖格
    └─ Impact（终结结算阶段）：提交 AreaEffectPlan（范围效果计划）
```

### 4.1 预览逻辑

玩家瞄准时：

```text
当前鼠标目标格
    ↓
用原版 DamageWorker.ExplosionCellsToHit（爆炸覆盖格计算）
    ↓
写入 PreviewDrawItemKind.CellGroup（格子组预览）
    ↓
BDP 目标选择宿主统一绘制
```

预览颜色固定使用灰白色。

这属于模块代码里的默认表现，不进入 `XML（配置）`。

同时关闭原版 `FieldRadius（目标周边范围）`，避免同一范围显示两遍。

### 4.2 结算逻辑

投射物终结命中时：

```text
BdpProjectile（BDP 投射物宿主）
    ↓
Impact（终结结算）
    ↓
ExplosiveModule（爆炸模块）提交 AreaEffectPlan（范围效果计划）
    ↓
GenExplosion.DoExplosion（原版爆炸执行接口）
```

爆炸中心取 `context.Projectile.Position（当前投射物所在格）`。

默认抑制基线单体命中：

```text
SuppressBaselineImpact（抑制基线命中） = true
```

这样一发子弹不会同时造成“单体子弹伤害 + 爆炸伤害”两份效果。

---

## 5. 配置设计

新增 `ExplosiveModuleConfig（爆炸模块配置）`。

建议字段：

- `ExplosionRadius（爆炸半径）`：必须大于 0。
- `DamageDef（伤害类型）`：为空时回退投射物伤害类型。
- `DamageAmount（伤害量）`：小于等于 0 时回退投射物伤害量。
- `ArmorPenetration（护甲穿透）`：小于 0 时回退投射物护甲穿透。
- `SuppressBaselineImpact（抑制基线命中）`：默认 `true（是）`。

不建议第一版加入：

- 多段爆炸。
- 延迟爆炸。
- 友军过滤。
- 自定义伤害衰减。
- 自定义爆炸格计算。

这些都会偏离“测试模组业务样例”的体量。

---

## 6. XML（配置文件）落点

新增模块定义：

```text
BDP_TestRangedExplosiveModule（测试远程爆炸模块）
```

位置：

```text
BorderDefenseProtocol.DevHarness/1.6/Defs/Pawn/Expressions/Test/RangedAttackModuleDefs_Test.xml
```

新增测试芯片：

```text
BDP_TestChipExplosiveRanged（测试爆炸远程芯片）
```

位置：

```text
BorderDefenseProtocol.DevHarness/1.6/Defs/Things/Items/Chips/Test/ThingDefs_TestChips_Combat.xml
```

投射物不新增定义。

测试爆炸芯片继续复用当前其它远程类测试芯片使用的共享投射物。

真实爆炸半径仍由模块配置提供，不由投射物 `Def（定义）` 决定。

---

## 7. 不采用方案

### 7.1 直接使用原版爆炸投射物

不采用，原因：

- 半径会偏向绑定在投射物 `Def（定义）` 上。
- 不符合“爆炸是远程模块能力”的需求。
- 后续同一把枪换模块不方便。

### 7.2 扩展主模组预览基础设施

不采用，原因：

- 现有 `CellGroup（格子组）` 足够表达爆炸预览。
- 当前需求不需要新增预览图元。
- 扩主模组会扩大影响范围。

### 7.3 自己写爆炸伤害扫描

不采用，原因：

- 原版已经有成熟爆炸结算。
- 自写扫描容易绕开墙体、视线、声音、火焰、污渍等原版规则。
- 不符合 BDP 优先复用原版的原则。

---

## 8. 验收标准

- 测试模组出现独立爆炸远程芯片。
- 瞄准时能看到爆炸覆盖格。
- 子弹终结命中时发生原版爆炸。
- 爆炸半径来自模块配置。
- 默认不叠加基线单体命中。
- 主模组公共协议不因本业务新增字段或分支。
