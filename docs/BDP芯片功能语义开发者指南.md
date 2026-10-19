# BDP 芯片功能语义开发者指南

本文说明如何声明芯片对玩家提供的功能，以及如何把具体实现路线的数据投影到这些语义上。目标是让芯片制造台、触发器装配台和第三方扩展使用同一份有归属、有来源、可诊断的信息树；不是另建一套游戏执行框架。

## 先记住这几条

1. `Feature`（功能语义）回答“玩家能做什么/会得到什么”，不回答它由 `Verb`、`Ability`、`Hediff` 还是自定义组件实现。
2. `Sources`（值来源）只声明去哪里读事实。实际数值必须从游戏执行定义、BDP 业务定义或扩展自己的权威业务定义读取；禁止在 `Features`（功能声明）中手填或复制实例值。
3. `Groups`（信息组）用于把同一功能中可以重复、且彼此数据必须对应的一组信息保持在一起，例如多把近战 Tool 或多种机动方式。
4. `Subgroups`（子组）表达依附于其所属功能或信息组的次要结果。当前所有“额外效果”都用它；子组不能再嵌套子组。
5. `Children`（子功能）只用于“结果本身还是一个完整、独立可理解的玩家功能”，不能用来表示技术对象、来源、攻击方式或额外效果。
6. UI（用户界面）消费解析后的树。面板可以决定隐藏什么、怎样排版，但不能各自再猜一次语义或读取 `Verb`、`Ability`、`Hediff` 技术对象。

## 1. 信息树与归属

芯片的信息至少有芯片本身、功能语义、形态/姿态上下文以及组合关系几个层次。功能树仍是有限深度的功能结构；上下文决定哪棵声明在当前范围内有效；组合关系是扩展引用，不冒充芯片的独立功能。

```text
芯片信息模型
├─ 芯片自身的一般信息
├─ 根级功能树
│  └─ Feature → Groups / Subgroups / Children
├─ 根级扩展引用（如可能参与的组合定义）
└─ 形态上下文
   ├─ 该形态下生效的功能树与扩展引用
   └─ 姿态上下文
      └─ 该姿态下生效的功能树与扩展引用

组合结果（单独成型的芯片）
├─ 组合结果自身的芯片信息
├─ 组合结果自己的功能树
└─ 它自己的根级/上下文扩展引用
```

芯片声明中的 `Features`（功能声明）仍然是一份配置。来源条目通过 `EntryId`（表达条目编号）/`EntryIds` 关联形态或姿态的活动条目后，解析器才把相应声明投影到根、形态或姿态上下文。不要为“某形态”或“某姿态”另外伪造一种功能语义。

形态/姿态上下文键按 `形态键/姿态键` 组合；因此 `ModeKey`（形态键）和 `StanceKey`（姿态键）都不得包含 `/`。来源范围、功能上下文和组合引用共用这一稳定键规则，避免两个不同层级意外合并。

组合引用有两种不同身份：`definition` 指向可从静态定义关系发现的组合定义；`runtime-result` 指向已有表达系统正式发布的组合结果。两者都进入通用 `ExtensionReferences`（扩展引用）出口，不并入 `Features`。静态预览没有运行时结果时仍可展示静态关联；运行时入口只读取已发布快照，不会为了面板展示重新计算组合。

## 2. 五种声明成员各自负责什么

| 成员 | 要回答的问题 | 正确用途 | 不要这样用 |
| --- | --- | --- | --- |
| `Feature` | 玩家感知到的是什么功能？ | 远程攻击、护盾、机动、能力等语义 Def | 不要填写 `Verb` 类名或 `HediffDef` 名来代替语义 |
| `Sources` | 属性事实从哪里读取？ | 一个或多个来源配置及 Worker（读取器） | 不要把射程、伤害、比例等当前数值写在这里 |
| `Groups` | 哪几组信息必须各自对应，不能互相串值？ | 攻击方式、机动方式等可重复实例 | 不要用多个相同属性值平铺来隐去实例对应关系 |
| `Subgroups` | 哪个父功能/父信息组产生了哪些附带结果？ | 所有功能都可用的“额外效果”结构 | 不要把附带效果提升成独立 `Children` |
| `Children` | 主功能产生了哪些完整子功能？ | 例如“能力 → 远程攻击”或“能力 → 机动” | 不要把技术实现对象或组合来源当子功能 |

### 功能级与信息组级子组

额外效果必须归属于实际产生它的最窄完整所有者：

```text
整个功能产生的效果：Feature.Subgroups
某个近战攻击方式产生的效果：Feature.Groups[n].Subgroups
```

两处都引用语义信息组 `BDP_FeatureGroup_ExtraEffect`，但实例和来源仍各自独立。它们不是两套互斥的“额外效果功能”，也不需要人为加一个空父信息组。

## 3. 常见声明场景

以下都是结构示意；省略了芯片本身、表达定义及其它无关字段。示例中的属性值不应被复制进 `Features`。

### 3.1 直接远程攻击

`RangedAttackFeatureSourceConfig`（远程攻击来源配置）读取绑定的表达条目和当前有效攻击事实。射程、伤害、精度、投射物飞行特征等属性由 Worker（读取器）从权威定义投影；声明只确定语义和来源绑定：

```xml
<Features>
  <li>
    <Id>example_ranged_attack</Id>
    <Feature>BDP_Feature_RangedAttack</Feature>
    <Sources>
      <li Class="BDP.Content.Information.Features.Sources.RangedAttackFeatureSourceConfig">
        <Id>example_ranged_source</Id>
        <EntryId>example_primary_attack</EntryId>
      </li>
    </Sources>
    <Subgroups>
      <!-- 仅在该远程攻击整体产生额外效果时添加实例。 -->
    </Subgroups>
  </li>
</Features>
```

没有额外效果时可以完全省略 `Subgroups`。缺少某一项属性值就表示缺值，不要为填满面板猜数值。

### 3.2 多种近战攻击方式

一项近战功能可以包含多条攻击方式。每条方式有自己稳定的组实例 Id、显示标签和来源；不同 `ToolIndex`（Tool 零基序号）对应不同的原版近战 Tool。不得把两把武器的数据合并到一个“近战平均值”。

```xml
<li>
  <Id>example_melee</Id>
  <Feature>BDP_Feature_MeleeAttack</Feature>
  <Groups>
    <li>
      <Id>example_blade</Id>
      <Group>BDP_FeatureGroup_MeleeAttackMethod</Group>
      <LabelKey>BDP_Tool_ExampleBlade</LabelKey>
      <Sources>
        <li Class="BDP.Content.Information.Features.Sources.MeleeAttackMethodFeatureSourceConfig">
          <Id>example_blade_source</Id>
          <EntryId>example_melee_entry</EntryId>
          <ToolIndex>0</ToolIndex>
        </li>
      </Sources>
    </li>
    <li>
      <Id>example_hilt</Id>
      <Group>BDP_FeatureGroup_MeleeAttackMethod</Group>
      <LabelKey>BDP_Tool_ExampleHilt</LabelKey>
      <Sources>
        <li Class="BDP.Content.Information.Features.Sources.MeleeAttackMethodFeatureSourceConfig">
          <Id>example_hilt_source</Id>
          <EntryId>example_melee_entry</EntryId>
          <ToolIndex>1</ToolIndex>
        </li>
      </Sources>
    </li>
  </Groups>
</li>
```

### 3.3 护盾属性与来源

护盾组件提供的是护盾自身的抵挡能力，不是按某一类来袭攻击分别拥有的规则。因此抵挡类型、抵挡角度、抵挡概率、伤害吸收比例和抵挡消耗都由 `BDP_Feature_Shield`（护盾功能）直接接收；来源配置直接挂在该功能实例下。护盾使用什么组件或 Hediff（状态对象）不属于面板契约。

```xml
<li>
  <Id>example_shield</Id>
  <Feature>BDP_Feature_Shield</Feature>
  <Sources>
    <li Class="BDP.Content.Information.Features.Sources.ShieldFeatureSourceConfig">
      <Id>example_shield_source</Id>
      <EntryId>example_shield_entry</EntryId>
    </li>
  </Sources>
  <Subgroups>
    <!-- 由整个护盾共同产生的额外效果放在这里。 -->
  </Subgroups>
</li>
```

未使用的属性不用声明数据；语义 Def 已允许的槽位只是接收范围，不会凭空制造值。

### 3.4 能力：单一结果与多结果能力

`BDP_Feature_GenericAbility`（通用能力语义）表达有独立发动/冷却等生命周期、但暂无专用玩家语义的能力。它的公共属性由 `AbilityLifecycleFeatureSourceConfig`（能力生命周期来源配置）读取。

如果能力只提供通用生命周期信息、没有值得单独命名的完整结果（包括只有一个单一结果但无单独玩家语义的能力），则可以只有能力父节点。若能力产生了可独立理解的功能，把它作为 `Children`：

```xml
<li>
  <Id>example_ability</Id>
  <Feature>BDP_Feature_GenericAbility</Feature>
  <Sources>
    <li Class="BDP.Content.Information.Features.Sources.AbilityLifecycleFeatureSourceConfig">
      <Id>example_ability_lifecycle</Id>
      <EntryId>example_ability_entry</EntryId>
    </li>
  </Sources>
  <Children>
    <!-- 只有实际产生远程攻击时才声明此子功能。 -->
    <li>
      <Id>example_ability_ranged_result</Id>
      <Feature>BDP_Feature_RangedAttack</Feature>
      <Sources>
        <li Class="BDP.Content.Information.Features.Sources.RangedAttackFeatureSourceConfig">
          <Id>example_ability_ranged_source</Id>
          <EntryId>example_ability_entry</EntryId>
        </li>
      </Sources>
    </li>
    <!-- 多结果能力还可有机动、护盾等其它完整子功能。 -->
  </Children>
</li>
```

不要因为底层有一个 `AbilityDef` 就把它称为能力语义；判断依据是玩家是否有一个独立能力生命周期。也不要把同一 Ability 内的每个代码步骤都拆成子功能。

### 3.5 机动与额外效果

`BDP_Feature_Movement`（机动语义）下的多个移动路线用 `BDP_FeatureGroup_MovementMethod`（机动方式信息组）分别表示。有效距离、最小距离、效果半径、目标类型、预热和行动冷却由相应来源读取。实际最终移动了多远属于结果，不是通用设计属性；如某路线附带攻击或状态等次要结果，应声明成相应额外效果子组，而非添加“移动距离”。

### 3.6 形态与姿态

形态/姿态通过表达上下文活动条目筛选现有 `Features` 声明。不同姿态的护盾功能和近战方式必须分别关联自己的 `EntryId`，不要把多个姿态的属性汇总成一个根级数值。常见树形如下：

```text
芯片
├─ 根级功能（所有形态都成立）
├─ 形态 A
│  ├─ 形态 A 的功能
│  ├─ 姿态 A1 → 仅 A1 生效的功能/信息组实例
│  └─ 姿态 A2 → 仅 A2 生效的功能/信息组实例
└─ 形态 B
   └─ 形态 B 的功能
```

具体 XML 应复用芯片表达配置中的 `Modes` / `Stances`（形态/姿态）和 `ActiveEntryIds`（活动条目列表），再由来源的 `EntryId` / `EntryIds` 对应；不要在 `Feature` 内另造上下文分支字段。

### 3.7 组合来源与组合结果

单枚来源芯片可以没有自己的主动功能，但仍通过 `ComboDef`（组合技定义）的来源键声明其组合关联。该关联被作为静态 `ExtensionReference` 放在芯片根级或对应形态/姿态，不把芯片伪装成“组合技能”。真正成型的组合芯片有自己的信息树：它由已求值的组合业务配置声明 `Features`，并按该组合结果自己的上下文读取值。

组合结果信息按正式组合表达的稳定来源条目 Id（`FormalExpressionResult.SourceEntryId`）绑定到 `ComboDef.Features`。正式表达发布时，Core 将组合定义里的通用功能声明与该条最终已裁定的表达字段交给同一 `ChipFeatureResolver`，按正式结果 Id 保存解析树；公开组合结果把它放在独立的 `FeatureInformation` 字段。触发器装配台只读已发布投影，不能用组合两侧来源芯片的初始数值重算，也不能在 UI 绘制时现场解析。

作者仍需按玩家语义选来源 Worker：例如组合远程攻击声明用 `RangedAttackFeatureSourceConfig`，组合能力生命周期用 `AbilityLifecycleFeatureSourceConfig`，其机动或近战子功能再分别使用对应方式组来源。不要用通用表达条目来源替代明确语义 Worker，也不要在 `Features` 手填组合结果数值。

```text
来源芯片：组合引用（静态定义关系）
组合成品：自己的功能树
  能力
  ├─ 机动
  └─ 近战攻击
正式发布时：该 ResultId → 对应 ComboDef.Features 的解析结果
```

已有运行时组合结果的芯片详情可以额外带上 `runtime-result` 引用。装配台只读已发布结果，不从两枚来源芯片拼凑出一个“看起来像组合”的功能。

### 3.8 第三方增加同义实现或新语义

第三方采用不同技术路线实现护盾时，应声明同一个 `BDP_Feature_Shield`，并提供自己的来源 Worker（读取器）。可用 XML Patch（XML 补丁）给 `BDP_Feature_Shield.Properties` 或其规则信息组的属性/子组接收范围追加 Def；前提是新增属性的含义仍然是护盾玩家需要了解的信息。

若新增的是 BDP 还没有的玩家功能，则可以用 XML 新建 `ChipFeatureDef`（功能定义）、属性 Def 和组 Def；在芯片声明中引用它。未知语义仍会保留本地化名称、稳定路径和通用属性，不要求 Core 增加针对该业务的新分支。

XML 可以追加 Def 和槽位，但 XML 不能凭空知道某个模组私有组件的业务含义。对新的数据路线，需要由该扩展自己的程序集提供派生 `ChipFeatureSourceConfig` 和 `IChipFeatureSourceWorker`，从其权威业务 Def/对象读取事实。BDP Development 中的完整示例位于：

- `1.6/Development/Defs/ChipFeatureDef/ExtensionSample.xml`
- `1.6/Development/Defs/ChipFeaturePropertyDef/ExtensionSample.xml`
- `1.6/Development/Patches/ChipFeatureExtensionSample.xml`
- `Source/BDP.Development/Information/Samples/CustomBarrierRuleDef.cs`
- `Source/BDP.Development/Information/Samples/CustomBarrierFeatureSourceWorker.cs`

Development 示例把同一个自定义来源 Worker 接到了两个不同的语义实例：一个是新建的 `BDP_Feature_ExperimentalBarrier`，另一个是既有的 `BDP_Feature_Shield`。后者通过 Patch 向护盾属性接收范围追加了反射比例属性槽位，直接演示“实现路线不同、玩家语义相同”时如何沿用既有语义；前者演示如何新增语义。两条声明仍是独立功能实例，不会因为使用同一 Worker 或属性而自动合并。

示例的 `CustomBarrierFeatureSourceConfig` 只写 `RuleDefName`（规则定义名）；反射比例只存在 `CustomBarrierRuleDef` 权威业务定义中。这个例子演示数据来源边界，不意味着反射一定是所有护盾都应展示的标准属性。

## 4. 当前 BDP 来源读取入口

| 来源配置 | 通常服务的结构 | 读取职责（不代表强制展示全部） |
| --- | --- | --- |
| `RangedAttackFeatureSourceConfig` | 远程攻击 | 当前有效射程、最小距离、目标/视线、预热、行动冷却、伤害、伤害类型、有效穿甲、射击节奏、精度、投射物飞行特征等 |
| `MeleeAttackMethodFeatureSourceConfig` | 一条近战攻击方式组 | 指定 `ToolIndex` 对应的原版有效距离、预热、行动冷却、单次伤害、伤害类型、穿甲、攻击次数和间隔 |
| `AbilityMeleeAttackFeatureSourceConfig` | 能力产生的一条近战攻击方式组 | 读取该 Ability（能力）效果组件显式实现的近战结果只读接口；不会把所有 Ability 自动判断为近战 |
| `ShieldFeatureSourceConfig` | 一项护盾功能 | 该护盾的抵挡角度、抵挡概率、抵挡类型、吸收比例和消耗；支持单枚或聚合护盾状态 |
| `MovementMethodFeatureSourceConfig` | 一条机动方式组 | 有效/最小距离、范围、目标限制、等待/冷却及其它已确定的机动事实 |
| `AbilityLifecycleFeatureSourceConfig` | 能力父功能 | 能力的通用目标和生命周期属性；实际远程攻击、护盾、机动等效果要声明为子功能或额外效果 |
| `ExtraEffectFeatureSourceConfig` | 额外效果子组来源基类 | 只提供扩展基类；每一种具体效果都需自己的来源配置/Worker 和权威数据读取 |

以上是能读取的事实范围，不是 UI 的必显清单。值不存在、定义没有、来源不适用与值冲突是不同状态，不能用默认的零值把这些状态抹平。

## 5. 属性、复制与合并规则

- 属性稳定身份由 `ChipFeaturePropertyDef`（功能属性定义）的 DefName 决定，玩家名称/说明进入语言包。
- 功能和组 Def 的 `Properties` 是“允许接收哪些属性”的槽位，不是属性实例，也不存放芯片当前数值。
- Worker 返回 `ChipFeatureValueContribution`（属性值贡献），附带值状态、来源层级和来源链；解析器按现有优先级合并，不因面板排序或布局改变合并规则。
- 多 Tool、多种机动方式即使使用同一个组 Def，也必须有不同实例 `Id` /路径。不同姿态的护盾则声明为独立护盾功能，并各自关联活动 `EntryId`。消费端应按对应实例展示，避免属性串组。
- `Subgroups` 仅一层深度。若效果本身可以独立选择、发动或理解，重新评估为 `Children`；若它只是一次功能带来的次要结果，保留为额外效果。
- 制造合并通过来源配置统一复制入口；扩展配置的额外字段由复制机制保留，`EntryId` 映射按制造关系更新。第三方仍需保证自定义配置字段可安全复制。

## 6. 错误诊断怎么看

开发者诊断由 Development（开发辅助）提供“诊断芯片功能语义”入口和原始事实报告。报告按功能路径、信息组/子组路径输出属性身份、状态与来源链；被拒绝/无效的结构会同时出现在解析诊断中。常见情况：

| 诊断/状态 | 常见原因 | 修正方向 |
| --- | --- | --- |
| `FeatureDefMissing`、`GroupDefMissing`、`SubgroupDefMissing` | XML 引用了不存在或未加载的 Def | 检查 DefName、加载顺序和 XML 根类型 |
| `FeatureSourceIdMissing`、`FeatureWorkerMissing` | 来源实例缺稳定 Id，或 Worker 类型未绑定 | 补稳定 Id；使用有效 WorkerClass/构造绑定 |
| `GroupNotAccepted`、`FeatureSubgroupNotAccepted`、`SubgroupNotAccepted` | 父功能/父组的槽位未允许该结构 | 给确切所有者的 Def 槽位增加合法引用，或修正错误归属 |
| `FeatureContributionPropertyMissing`、`FeatureWorkerResolveFailed` | Worker 未返回属性 Def，或读取过程异常 | 检查自定义来源、权威数据是否存在和属性 DefName |
| `FeatureDepthExceeded`、`SubgroupNestingNotSupported` | 子功能超过上限，或子组尝试继续嵌套 | 减少层级；子组不是递归功能对象 |
| `Missing` / `Unavailable` | 没有事实，或该来源在当前上下文不适用 | 先确认来源与上下文，不要补默认值伪装存在 |
| `Conflict` / `Invalid` | 同一属性贡献发生冲突或值无效 | 检查来源优先级、单位、属性类型和 Worker 输出 |
| `extension_context_missing` / `extension_context_ambiguous` | 扩展引用指向缺失或重复的上下文键 | 修复引用上下文；错误引用不会自动提升到根级 |

原始事实报告会将被采用的来源标为 `applied`（已采用），其它参与裁定的来源标为 `ignored`（未采用）。日志仅为 Development 诊断设施，不应把原始技术细节显示到正式玩家面板。

## 7. 规模边界和面板职责

- 子功能最大深度是 `ChipFeatureDefinitionValidator.MaximumDepth = 2`，根节点深度从 0 开始；即允许根功能、子功能、孙级功能，拒绝再深一层。
- 信息组可以拥有一层子组；子组本身不能再有子组。当前实际信息骨架就是“功能 → 信息组 → 子组”，功能级子组是“功能 → 子组”。
- Core（中性基础程序集）负责通用契约、解析、校验、复制和稳定结果，不依赖 Content 的远程攻击、护盾或组合业务。
- Content（正式业务程序集）负责 BDP 现有标准语义、业务来源 Worker、组合引用以及面板的实际布局。
- Development（开发辅助程序集）负责样例、诊断和原型；正式程序集不得反向引用它。
- 两个面板消费相同的已解析功能/组/子组结果。它们可以自行选择显示项、折叠和排版；当前功能组渲染已走通用投影，不再依赖实现类型猜测。雷达图、精度图等图表工具是与具体功能数据解耦的中性工具，是否接回具体面板由后续 UI 设计决定。

修改语义契约时，先决定“玩家语义/信息所有者/事实来源”三件事，再改相应 Def 和 Worker，最后验证两面板是否仍能消费同一结果。除非需求明确需要，避免为了新语义改动攻击、护盾、机动或组合的正式执行链。
