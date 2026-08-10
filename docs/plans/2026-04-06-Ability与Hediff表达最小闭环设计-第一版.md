# Ability 与 Hediff 表达最小闭环设计（第一版）

## 目的

这份设计文档回答一个很具体的问题：

- 新 BDP 怎样在不过度设计的前提下，完成 `Ability` 与 `Hediff` 表达的最小正式闭环，并且让后续业务能直接开始写芯片、进游戏实测。

本文坚持两条原则：

- 先把真正需要的正式主链做对
- 不为了未来可能性提前造平台

---

## 一、最小正式主链

当前正式主链继续保持：

```text
Trigger 持真值
  -> Expression 算结果
  -> HostSync 对齐原版宿主
  -> 原版 AbilityTracker / HediffSet
```

这里的关键点是：

- `Trigger` 只管装载、激活、切换这些真值
- `Expression` 只管把当前成立的结果算出来
- `HostSync` 才负责把结果发布到原版宿主

因此：

- `Trigger` 不直接 `GainAbility`
- `Trigger` 不直接 `AddHediff`
- `Trigger` 不直接 `RemoveAbility`
- `Trigger` 不直接 `RemoveHediff`

这些副作用都留在宿主同步链里。

---

## 二、Ability 与 Hediff 的定位

### 1. Ability 是表达结果

业务语义很简单：

- 当前成立 -> Pawn 应该拥有这个 `Ability`
- 当前不成立 -> Pawn 不应该继续拥有这个 `Ability`

它最终发布到：

- `Pawn_AbilityTracker`

### 2. Hediff 也是表达结果

业务语义同样简单：

- 当前成立 -> Pawn 身上应该存在这个 `Hediff`
- 当前不成立 -> 这个 `Hediff` 应该被回收

如果需要把结果数量映射为强度：

- 用 `HediffApplyModeKey=countToSeverity`

它最终发布到：

- `Pawn.health.hediffSet`

### 3. 芯片不是 Ability 芯片 / Hediff 芯片的架构中心

芯片只是表达来源，不是结果宿主。

所以新 BDP 不再围着“旧版 ability 类芯片 / hediff 类芯片”做内部架构，而是围着：

- 结果是什么
- 结果发布到哪里

来设计。

---

## 三、本轮明确不做的事

本轮明确不做：

- 形态切换正式闭环
- 通用来源追踪平台
- 通用优先级平台
- `Hediff` 附带 `Ability` 的专门 BDP 机制
- 第二套 `Ability` / `Hediff` 运行时镜像

原因很简单：

- 这些不是当前起步写业务的必要条件
- 做了只会放大复杂度

---

## 四、Hediff 通道的正式边界

这是本轮真正要收口的重点。

### 1. 旧口径不够正式

如果只说：

- “首轮建议使用 BDP 专用 HediffDef，避免误伤别的来源”

这听起来像临时避险，不像正式架构边界。

### 2. 新口径

正式口径改成：

- **BDP 表达系统的 Hediff 通道，只发布和维护 BDP 自有的表达宿主 HediffDef。**

### 3. 正式协议不再看命名前缀

正式协议看：

- `HediffDef.hediffClass`

具体规则是：

- 只要某个 `HediffDef` 要被表达系统接管
- 它的 `hediffClass` 就必须继承 `BdpExpressionHostHediff`

也就是说：

- 命名可以继续建议 `BDP_ExpressionHediff_*`
- 但命名只负责可读性
- **不是协议本身**

### 4. 为什么这才是正式边界

因为原版引擎本来就是靠：

- `Def`
- `Class`

一起定义语义边界。

我们现在只是顺着原版，把 `Hediff` 通道的正式宿主边界补齐。

---

## 五、最小实现裁定

### 1. 新增最小宿主基类

新增：

- `BdpExpressionHostHediff : Hediff`

它只承担一件事：

- 把“这是表达宿主 Def”落成结构事实

它不承担：

- 来源追踪
- 实例托管平台
- 中台账本

### 2. 继续保留按 Def 的同步与回收

在当前边界下：

- 表达系统只接管自己的表达宿主 Def

那么现有这套：

- 按 `HediffDefName` 聚合
- 按 `def` 保持存在
- 按 `def` 回收

就是成立的。

所以本轮不做：

- `managedKey`
- `sourceId`
- 额外实例账本

### 3. `HediffApplyModeKey` 不再继续改名

虽然 `publish` 从语义上更贴近“发布到宿主”，
但当前真正的问题不是这个字段名，
而是 `Hediff` 宿主边界太软。

因此本轮定为：

- 保留 `HediffApplyModeKey`
- 保留 `countToSeverity`

不再为了命名继续制造协议抖动。

---

## 六、校验器应承担的职责

校验器需要把 `Hediff` 通道的正式边界收成硬规则。

### 必须报错的情况

- `HediffDefName` 为空
- `HediffDefName` 指向的 `HediffDef` 不存在
- `HediffDef.hediffClass` 不是 `BdpExpressionHostHediff` 或其子类
- `HediffApplyModeKey` 非空且不是 `countToSeverity`

### 不再保留的旧口径

- 不再靠命名前缀识别
- 不再只是 warning 提醒“建议专用 Def”
- 不再把非法 `HediffApplyModeKey` 退化成“只保证存在”

---

## 七、业务作者最终应该面对什么

到这一步以后，业务作者只需要记住下面这些规则：

- `Ability` 就写 `Kind=Ability + AbilityDefName`
- `Hediff` 就写 `Kind=Hediff + HediffDefName`
- 需要数量映射强度时，写 `HediffApplyModeKey=countToSeverity`
- `HediffDefName` 指向的 Def，必须是 BDP 表达宿主 Def
- 判断是不是正式表达宿主 Def，看 `hediffClass`，不是看名字像不像

---

## 八、实施完成后的状态

实施完成后，应达到下面这些状态：

- `Ability` 通道继续稳定工作
- `Hediff` 通道继续保持轻量实现
- `Hediff` 宿主边界从“软约束”收成“正式协议”
- DevHarness 样本已经对齐正式协议
- 业务作者可以直接开始写 `Ability` / `Hediff` 芯片业务逻辑并进游戏实测

---

## 九、一句话结论

新 BDP 的最小正式答案不是重做一套 `Hediff` 平台，而是：

- **继续保留轻量主链，并把 Hediff 通道正式定义为“只发布到 BDP 自有表达宿主 HediffDef 的原版宿主通道”；这条边界由 `hediffClass` 负责，不再由命名前缀负责。**
