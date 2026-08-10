# Ability 与 Hediff Trion 业务面使用说明（第一版）

> 2026-07-29 修订：旧版“由 Hediff 自行登记持续消耗”的方案已经删除。下面内容已按当前正式边界改写。

## 这份说明只说一件事

- `Ability（技能）`每次使用时怎样扣 `Trion（崔昂）`
- 表达效果成立期间的持续消耗应该怎样按有效来源数计费

---

## 一、原则

- 芯片表达层只负责“把什么结果挂到 Pawn 身上”
- `Ability` 的按次使用消耗写在对应芯片表达条目的 `Trion.UseCost`
- 表达效果成立期间的持续消耗写在该表达条目的 `Trion.SustainCostBySourceCount`
- 芯片本体不再拥有持续消耗字段；持续费用跟随最终有效效果，而不是只跟随“芯片已激活”

---

## 二、Ability 怎么写

放在芯片对应的表达条目：

```xml
<li>
  <Kind>Ability</Kind>
  <AbilityDefName>BDP_ExampleAbility</AbilityDefName>
  <Trion>
    <UseCost>50</UseCost>
    <MinimumRequired>50</MinimumRequired>
  </Trion>
</li>
```

语义：

- 施法前检查可用 Trion 是否达到 `MinimumRequired`
- 不足时不给施放
- 真正施法时正式扣掉这笔 `Trion`

---

## 三、表达持续消耗怎么写

放在对应表达条目的 `Trion` 配置块：

```xml
<li>
  <Kind>Hediff</Kind>
  <HediffDefName>BDP_ExampleHediff</HediffDefName>
  <Trion>
    <SustainCostBySourceCount>
      <li>
        <SourceCount>1</SourceCount>
        <TotalPerSecond>2</TotalPerSecond>
      </li>
      <li>
        <SourceCount>2</SourceCount>
        <TotalPerSecond>5</TotalPerSecond>
      </li>
    </SustainCostBySourceCount>
  </Trion>
</li>
```

语义：

- 同一个最终状态只有 1 个有效来源时，整组状态每秒总计消耗 2 点
- 同一个最终状态有 2 个有效来源时，整组状态每秒总计消耗 5 点，不是每张各扣 5 点
- 实际来源数超过最高已写档位时，继续沿用最后一档
- 表必须从 `SourceCount=1` 开始连续递增，不能跳号、重复，费用不能是负数或无穷值
- 效果失效、被条件压制或芯片停用后，费用会随最终表达发布自动停止；读档后也会自动重建
- Combo（组合结果）的持续费用表必须在组合表达条目里显式写，不能从来源芯片自动继承

---

## 四、当前样例

- `BDP_TestAbility_ExpressionOnly`
  - 施法扣 `50`
- `BDP_TestHediff_ExpressionOnly`
  - 验证 Hediff 表达与严重度聚合
  - 1 个有效来源持续消耗 `2/秒`
  - 2 个及以上有效来源持续消耗总计 `5/秒`

---

## 五、记住一句话

- **芯片激活只扣一次性激活费；效果维持由最终有效表达按来源总数收费。**
