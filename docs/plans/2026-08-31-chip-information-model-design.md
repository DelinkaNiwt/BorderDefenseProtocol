# BDP 芯片信息解析模型重构设计

## 文档状态

- 状态：已确认设计
- 日期：2026-08-31
- 适用范围：BDP 芯片定义、成型芯片、激活表达结果及其信息消费者
- 目标：建立独立、通用、中性的芯片信息解析模型

## 一、设计目标

建立一套不属于任何具体界面的芯片信息解析设施，使以下三类系统共同消费同一个信息模型：

- 表达系统；
- 芯片制造台；
- 触发体装配台。

信息模型负责回答两类问题：

1. 当前芯片对外公开哪些信息块和字段；
2. 这些字段当前的最终值、状态和来源是什么。

旧的 `ExpressionInfoProjection` 信息层属于项目早期预留结构，不做兼容迁移。旧模型、旧投影器、旧读取口和旧测试边界在实施阶段按实际引用关系清理或替换。

## 二、总体数据流

```text
芯片定义 / 有效成型配置 / 激活运行事实
                ↓
       ChipInformationParser
         芯片信息解析器
                ↓
       ChipInformationModel
         芯片信息模型
          ↙         ↓         ↘
       表达系统   制造台预览   装配台预览
```

制造台、装配台和表达系统都是消费者。信息层不读取窗口，不知道消费者名称，也不为消费者提供专用解析入口。

## 三、不可破坏的中性约束

### 3.1 禁止芯片身份特判

解析器和规则不得使用以下数据决定信息结构：

- 芯片 `DefName`；
- 芯片名称；
- 芯片 `ThingID`；
- 芯片样本列表；
- 单一芯片专属标签；
- 中央“芯片 ID → 详情模板”表。

禁止出现按具体芯片选择结果的分支，例如：

```csharp
if (chip.defName == "某芯片")
{
    // 生成专属信息块
}
```

### 3.2 只按通用事实和能力适配

规则只能针对中性语义：

- 存在远程 `Verb`；
- 存在近战 `Tool`；
- 存在 `Ability`；
- 存在 `Hediff`；
- 存在 `Passive`；
- 存在防护结果；
- 存在 Trion、装载或激活条件；
- 存在形态或姿态上下文。

同一规则必须适用于所有满足相同结构的芯片。

### 3.3 信息模型不包含界面行为

模型可以包含稳定语义键、语言包键、顺序、值状态和来源，但不得包含：

- 窗口或面板类型；
- 控件和按钮；
- 绘制代码；
- 布局坐标；
- 制造台或装配台专属字段；
- 已排版的界面段落。

## 四、解析输入

统一输入为 `ChipInformationParseInput`，包含：

```text
Identity              芯片身份，仅用于输出追踪
EffectiveDefinition   当前有效定义
ExpressionFacts       表达事实或正式表达结果
RuntimeState          激活、条件、形态、姿态等状态
ProvenanceSources     基础定义、构型、运行结果等来源
```

三个消费者只负责把自己的数据转换成这一份中性输入：

- 制造台提供有效成型配置；
- 装配台提供选中成品芯片的有效定义和必要状态；
- 表达系统提供激活芯片的有效定义、正式表达结果和运行状态。

解析器不直接依赖制造台的组合解析类型，也不直接读取界面对象。

## 五、输出模型

顶层模型为 `ChipInformationModel`：

```text
Identity
Schema
Blocks
Contexts
Diagnostics
```

### 5.1 Schema

`Schema` 描述当前芯片对外公开的信息能力：

```text
BlockKey
Kind
LabelKey
Order
Fields
```

每个字段描述包含：

```text
FieldKey
LabelKey
ValueKind
UnitKey
VisibilityPolicy
```

### 5.2 Blocks

`Blocks` 保存当前实际解析出的信息块和字段值：

```text
BlockKey
Kind
LabelKey
Order
Fields
ChildBlocks
```

字段值包含：

```text
FieldKey
LabelKey
ValueKind
ValueState
Value
UnitKey
Provenance
```

推荐的 `ValueKind`：

```text
Text
Boolean
Integer
Number
Percentage
Duration
Enum
List
```

推荐的 `ValueState`：

```text
Available
Inactive
Unavailable
Unknown
Invalid
Conflict
```

### 5.3 上下文

形态、姿态和 Ability 子结果使用嵌套上下文，不作为芯片专属界面模板：

```text
Contexts
├─ ContextKey
├─ LabelKey
├─ State
└─ Blocks
```

消费者可以切换查看上下文，但模型本身不执行实际形态切换。

## 六、来源链与构型溯源

字段默认输出最终值，同时保留必要的来源链：

```text
基础定义 → 构型修正 → 有效定义 → 运行结果 → 最终值
```

每条 `Provenance` 记录包含：

```text
SourceKey
SourceKind
ContributionKind
Value
Operation
Applied
```

例如：

```text
基础射程：30
构型修正：× 0.8
最终射程：24
```

普通消费者只读取 `FinalValue`；需要分析时读取 `Provenance`。该机制适用于所有通用替换、加法、倍率和派生关系，不只服务武装构型。

## 七、第一版通用信息块

信息块只在存在有效定义或事实时生成：

| 信息块 | 生成条件 | 字段范围 |
| --- | --- | --- |
| `identity` | 存在基本身份 | 名称、简介、类别、标签 |
| `loadout` | 存在装载定义 | 槽区、占用方式、互斥组、激活延迟 |
| `trion` | 存在 Trion 定义 | 常驻占用、激活消耗、使用消耗、持续消耗 |
| `activation` | 存在激活条件 | 条件类型、要求值、当前值、满足状态、未满足原因 |
| `variant` | 存在成型构型 | 构型标签、最终结果来源、覆盖字段标记 |
| `rangedAttack` | 存在远程 `Verb` | 目标类型、伤害、穿甲、射程、数量、速度、预热、冷却、四项精度、攻击节奏 |
| `meleeAttack` | 存在近战 `Verb` 或 `Tool` | 攻击距离、伤害类型、伤害、穿甲、冷却 |
| `ability` | 存在 `Ability` | 能力冷却、实际子结果 |
| `hediff` | 存在 `Hediff` | 持续时间、效果汇总 |
| `passive` | 存在 `Passive` | 触发时机、作用对象、影响通道、作用侧、匹配条件、修改结果、叠加规则 |
| `contexts` | 存在形态或姿态 | 上下文名称、状态和内部信息块 |
| `extension` | 存在已注册中性扩展 | 扩展契约声明的字段 |

不公开投射物 `DefName`、内部模块链、原版私有字段、视线细节或未经注册的自定义对象。

## 八、规则与解析流程

规则接口为 `IChipInformationRule`，负责：

```text
CanApply(input)
DescribeSchema(input)
Resolve(input, schema)
```

规则只能贡献信息块定义、字段定义、字段值、来源记录或诊断。

解析固定经过以下阶段：

```text
接收输入
  → 提取中性能力
  → 建立上下文
  → 匹配通用规则
  → 生成 Schema
  → 解析字段与来源链
  → 合并重复块和字段
  → 校验冲突与未知数据
  → 输出只读模型
```

规则必须具备稳定排序、递归深度限制、循环引用检测和失败隔离。

## 九、字段来源优先级

字段的最终值采用固定优先级：

```text
RuntimeResult
    > EffectiveDefinition
    > DeclaredDefinition
    > DerivedFact
```

相同字段的处理规则：

- 相同值：合并来源；
- 不同值且有优先级：采用高优先级值并记录被覆盖来源；
- 不同值且无法裁定：标记 `Conflict`，不向玩家模型提供不确定值；
- 未知扩展：进入诊断，不自动显示。

解析器不得使用“最后写入覆盖前面”的隐式规则。

## 十、Core / Content 边界

### Core.dll

负责模型、解析器、规则契约、规则注册表、通用规则、来源链和诊断模型。

### Content.dll

只能注册针对中性扩展契约的规则，并负责将 Content 自己的成型解析结果转换为中性输入。

Core 不引用 Content，不认识 `ChipArmamentFormDef` 等具体业务类型。

### Development.dll

只用于诊断、验证和原型规则，不成为正式消费者依赖，也不污染正式信息模型。

## 十一、消费者契约

所有消费者使用同一个通用读取入口：

```text
Read(ChipInformationParseInput)
    → ChipInformationModel
```

不提供按窗口或系统命名的解析入口。

消费者可以枚举或按键读取公开信息块和字段，但不能：

- 读取原始 `VerbProperties`；
- 访问规则内部实现；
- 修改模型或芯片状态；
- 重复计算最终值；
- 把信息模型当作战斗真值源。

信息模型是正式事实的公开解释快照，不参与攻击判定、能力执行或状态同步。

## 十二、缓存与生命周期

解析器保持无状态：

- 定义输入变化时重新解析定义模型；
- 运行时投影版本变化时重新解析运行模型；
- 相同输入指纹可以复用只读模型；
- 消费者不得各自复制解析逻辑。

制造台、装配台和表达系统的缓存失效由各自的输入变化负责，信息层不维护窗口状态。

## 十三、验收标准

- 不存在按芯片名称、DefName 或 ThingID 定制信息结果的代码；
- 同一通用定义结构可以被不同芯片自动复用；
- 消费者可以枚举芯片公开的信息块和字段；
- 字段值具有明确类型、状态、单位和来源；
- 武装构型默认只显示最终值，同时保留基础值与构型值来源链；
- 未知字段不被伪造或静默暴露；
- Core 不依赖 Content；
- 制造台、装配台和表达系统使用同一模型契约；
- 信息模型异常不会破坏正式表达和战斗逻辑；
- 旧 `ExpressionInfoProjection` 信息层不再作为新模型的兼容入口。
