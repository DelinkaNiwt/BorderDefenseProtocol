# 2026-04-23 远程随机发射点散布设计

## 背景

当前远程齐射的发射点散布不是随机散布。

现有链路把每发 projectile 记录为一个固定序号，再用序号计算左右对称、均值为 0 的偏移：

- `OriginSpreadSequenceIndex`：当前发在散布序列中的序号。
- `OriginSpreadSequenceCount`：当前散布序列总数。
- `SpreadRadius`：被解释为固定队形展开半径。

因此同样目标、同样发数、同样朝向下，每轮紫点位置都会稳定复现。

## 目标

把“发射点散布”从固定队形设计改成随机区间设计。

作者可以配置一个发射点偏移区间。每发 projectile 真正发射时，都在这个区间里独立随机取一次偏移。

## 非目标

- 不改命中率随机。
- 不改原版 projectile 终点随机抖动。
- 不改毒蛇路径分段逻辑。
- 不继续保留固定序列散布作为默认兼容路径。

## 新配置口径

推荐把旧的单值 `SpreadRadius` 改成局部坐标区间：

```xml
<OriginSpread>
  <LateralMin>-0.3</LateralMin>
  <LateralMax>0.3</LateralMax>
  <ForwardMin>0</ForwardMin>
  <ForwardMax>0.105</ForwardMax>
</OriginSpread>
```

含义：

- `LateralMin / LateralMax`：横向区间。负值偏左，正值偏右。
- `ForwardMin / ForwardMax`：前后区间。负值靠后，正值靠前；当前测试配置把 `ForwardMin` 置 0，只允许向前随机。
- 未声明 `OriginSpread` 时，不做发射点随机散布。

这里用“局部坐标”而不是世界坐标，是为了让配置跟随射击方向自然旋转。

## 新计算口径

每发 projectile 的真实发射点：

```text
理论中心点
+ 横向方向 * Rand.Range(LateralMin, LateralMax)
+ 射击方向 * Rand.Range(ForwardMin, ForwardMax)
```

其中：

- 横向方向从“射击方向”和 `Vector3.up` 叉乘得到。
- 射击方向使用当前真实发射时的 source -> target。
- 每发 projectile 都独立调用 `Rand.Range`。

## 与当前实现的区别

当前实现：

```text
第 N 发 -> 固定比例 -> 固定偏移
```

新实现：

```text
第 N 发 -> 发射瞬间随机抽样 -> 随机偏移
```

当前实现保证整批均匀、对称、可复现。

新实现只保证每发落在配置区间内，不保证整批均匀，也不保证下一轮重复。

## 落地点

需要从根源改掉以下层级：

- Def 配置层：`ChipAttackExecutionConfig` 改为持有 `OriginSpread` 区间配置。
- 表达翻译层：`DefaultChipExpressionContractInterpreter` 不再翻译 `SpreadRadius`。
- 正式执行风格：`SingleAttackExecutionStyle` 不再保存 `volleySpreadRadius`，改保存随机散布区间。
- 发射计划层：`AttackExecutionEmit`、`FireEmitRecord`、`ProjectileInitPlan` 不再携带序列号。
- 发射落地层：`BdpVerb_Shoot` 删除序列比例计算，改成 `Rand.Range`。
- 测试样本层：测试齐射芯片与毒蛇齐射芯片改成 `OriginSpread` 配置。
- 烟测层：新增文本烟测，防止固定序列散布回流。

## 验收标准

- 代码中不再存在远程发射点散布序列算法。
- `BdpVerb_Shoot` 的发射点散布使用 `Rand.Range`。
- 测试 XML 不再使用 `<SpreadRadius>` 表示发射点散布。
- 紫点仍表示真实发射点，但同样条件下多轮齐射不再固定重合。
- 主模组和测试模组能通过 msbuild。
