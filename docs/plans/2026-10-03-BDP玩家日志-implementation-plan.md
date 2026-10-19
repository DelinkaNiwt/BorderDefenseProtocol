# BDP玩家日志实施计划

> 执行要求：使用 `superpowers:executing-plans`（执行计划）逐项推进；遵守当前工程规则，在当前分支执行，不创建工作树，不委派子代理。

**Goal:** 为BDP提供可复用、符合原版粒度的攻击与直接结果文案，并记录战斗体关键事件。

**Architecture:** 使用原版日志容器与条目基类。Core.dll（基础程序集）只提供必要的中性创建接入口和来源身份传递；Content.dll（正式内容程序集）提供条目扩展、文法和重要事件。直接结果只在本次交互完成点补充，不建立状态追踪或独立历史系统。

**Tech Stack:** RimWorld 1.6（边缘世界1.6）、C# 7.3、.NET Framework 4.8（运行框架）、Harmony（补丁库）、RulePackDef（文法定义）、原版 Scribe（存档接口）。

状态：任务1至7的代码与工程内交付已完成。用户确认“补”后，旋空波体的固定能力来源已从生成点传入并接入共用文法；尚未进行游戏实测，独立游戏模组副本尚未同步。

## 动态芯片来源修正（2026-10-03）

- 用户实测截图显示开火来源为“定制芯片”。根因是制造芯片共用物品定义，实际表达在实例动态配置；此前仅冻结通用物品定义和可空语言键，没有冻结实际模板条目。无语言键的条目无法从物品定义恢复，错误退回容器名称。
- Core从现有实例来源接口复制有序来源键，Content在条目创建时按制造系统同一条目复制规则还原实际功能模板与原始条目，替换为最小固定来源身份；不保存翻译后的名称，不读取之后的芯片选择，不增加历史仓库。
- 制造系统生成和还原共用同一个形态前缀函数，单/双形态来源匹配不在日志中另写制造规则。静态芯片读取统一总配置中的表达块，语言名称继续来自已有配置及语言数据。
- 对旧记录中无法恢复的声明身份，省略来源而保留完整句；不把动态物品容器名称当作攻击名，也不猜测历史实际芯片。
- 发布编译0警告、0错误；工程内基础/内容程序集与调试符号四项和交付产物哈希一致，原文件备份到工作区 `tmp/bdp-player-log-origin-before-deploy`。未运行测试脚本，修正后的新攻击尚需游戏实测。

## 来源补齐记录（2026-10-03）

- 用户授权额外修改旋空弧月生成效果组件。生成波体时传入本次实际能力定义；波体保存该定义，命中条目只复制定义类型与定义名，不读取人物之后的芯片。
- 共用来源解析对其它定义复用原版按类型查询定义的接口。能力来源在原版弹体名称位置取词，保留原版部位和结果文法；不按旋空或其它技能名称分支，不改静态武器缺失判断。
- 本次来源身份只服务日志，没有压入伤害语义作用域，没有改动伤害、扫掠、护盾或反馈机制。名称继续读取已有能力语言数据，没有新增硬编码玩家文案。
- 波体新增的存档字段只保存来源定义；其原有其它飞行状态的存档行为不在本次修改范围内。不能把来源字段已保存等同于波体飞行存读档已验证。
- 发布编译0警告、0错误；更新工程内内容程序集与调试符号，两项与交付目录哈希一致。更新前文件备份在工作区 `tmp/bdp-player-log-source-before-deploy`。未运行测试脚本或游戏验收。

## 主体实施记录（2026-10-03，来源补齐前）

- 共用工厂、来源身份、远程开火/命中、近战构造点、范围逐目标与直接结果、战斗体关键事件分别提交。原版容器、人物关联、存档和实际伤害算法继续使用。
- 直接结果由当前调用确认，再合入对应目标的同一条目；不记录持续状态变化。负重文法由内容配置选择，部位移除在实际成功点补重要后果。
- 固定战斗体宿主与日志组件的注入顺序。强制资格退出使用独立原因，不生成主动解除文案；读档恢复订阅不重播当前阶段。
- 收尾撤回未被调用的组合来源辅助方法上的重复接入。确认能力施放路径没有当前攻击上下文后，撤回波体无效的来源捕获与存档；目前使用完整通用命中句，不虚报正式能力名。
- 当时待批准的最小补充：额外修改 `Source/BDP.Content/SenkuKogetsu/CompAbilityEffect_SenkuKogetsuWave.cs` 的生成点，向波体传入本次能力定义，冻结来源身份并按定义读取名称。现已授权并完成，见上方记录。
- 最新编译：Release（发布配置），0警告、0错误。更新主工程 `1.6/Assemblies` 的基础/内容程序集和调试符号，四个文件与交付目录哈希一致；原文件备份在工作区 `tmp/bdp-player-log-before-deploy`。
- 未运行测试脚本或游戏验收。游戏 `Mods/BorderDefenseProtocol` 是与工作区分离的旧副本，本轮只更新计划规定的工程内交付文件，游戏副本尚未同步。本轮只提供简体中文语言包；其它语言仍需对应翻译。

## 依据与约束

- 设计依据：同目录 `2026-10-03-BDP玩家日志-design.md`。
- 下列路径均相对主模组根目录 `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol`。
- 原版代码依据为当前游戏真实 `Assembly-CSharp.dll`，通过 DecompilerServer（反编译工具）读取。Def 数据需要查询时使用 RimSearcher V3 CLI（运行时定义查询工具）。
- 原版 `PlayLogEntryUtility.RulesForOptionalWeapon` 在武器定义为空时直接结束，连已有弹体定义也不会生成对应武器弹体文法；BDP动态武器目前会因静态动作列表为空而传入空武器。必须补正确文法输入，不能仅捕获解析异常。
- 原版近战 `Verb_MeleeAttack.CreateCombatLog` 是非虚方法，并在返回前添加条目；不能用普通重写接入，也不采用事后删除再添加。
- 原版近战命中、闪避、未命中、伤害关联和偏转判定仍由原版执行，禁止复制整段攻击算法。
- 原版爆炸对每个受影响角色创建独立的爆炸结果条目；BDP取消范围普通伤害时会跳过该方法。因此多目标特殊结果需要接入已存在的逐目标作用域，不能只补直接命中的弹体条目。
- 原版战斗体阶段事件已经存在；当前强制解除与主动解除最终共用 `Release` 原因，日志不能据此宣称都是玩家主动操作。
- 不增加或运行测试脚本。本文末尾提供将来游戏验收项目，不表示已验证。实施编译只检查交付程序集能否生成。

## 任务1：建立最小文法与条目创建入口

新增文件：

- `Source/BDP/Core/CombatLog/IBattleLogEntryFactory.cs`
- `Source/BDP/Core/CombatLog/BattleLogEntryFactoryAccess.cs`
- `Source/BDP.Content/PlayerLog/PlayerBattleLogEntryFactory.cs`
- `Source/BDP.Content/PlayerLog/PlayerBattleLogSource.cs`
- `Source/BDP.Content/PlayerLog/PlayerBattleLogGrammar.cs`
- `Source/BDP.Content/PlayerLog/BattleLogEntry_BdpRangedFire.cs`
- `Source/BDP.Content/PlayerLog/BattleLogEntry_BdpRangedImpact.cs`
- `1.6/Content/Defs/RulePackDef/PlayerLog.xml`
- `Languages/ChineseSimplified (简体中文)/DefInjected/RulePackDef/PlayerLog.xml`
- `Languages/ChineseSimplified (简体中文)/Keyed/PlayerLog.xml`

修改：`Source/BDP.Content/ContentBootstrap.cs`。

步骤：

1. 工厂只负责创建原版攻击条目的BDP扩展，并提供本次承伤结果的补充入口；先接远程，后续任务补近战和爆炸创建。参数使用原版人物/定义以及已有 `ISemanticContext`、`DamageResolution` 等中性事实，不暴露芯片名称、战斗体业务或中文句子。
2. 接入口仅保留一个正式提供器，由Content启动注册。未注册时沿用原版创建方式；不建立多级匹配、优先级框架、历史表或全局条目字典。
3. Content条目分别继承原版远程开火和命中条目。继承人物关联、图标、点击、简略筛选及基础存档；命中条目继续是原版伤害结果条目的子类。
4. 文法辅助类只生成当前条目的必要规则。普通命中优先保留原版伤害文法；新增开火与特殊结果文法必须有缺来源、缺目标及二者皆缺的完整路径。
5. 原版开火会读取静态武器首个动作；保留现有安全判断，不删除判断，不向触发器定义塞入伪造动作，不把弹体定义冒充武器。
6. Def文件声明文法结构；玩家句子、动作词和名称连接词从语言文件读取。新增条目逐成员注释、XML逐条目注释，使用UTF-8。

本任务只建立上述接入口和文法，后续任务接入现有调用点。完成后按本任务文件单独提交。

## 任务2：冻结可本地化的正式来源身份

修改文件：

- `Source/BDP/Core/Expressions/Pipeline/ExpressionSourceCollector.cs`
- `Source/BDP/Core/Expressions/Pipeline/ComboFormalExpressionResultFactory.cs`
- `Source/BDP/Core/Expressions/Pipeline/CompositeExpressionResolver.cs`
- 任务1的 `PlayerBattleLogSource.cs`、`PlayerBattleLogGrammar.cs`

步骤：

1. 在已有语义上下文的 `ExtraData`（附加数据）中补必要来源身份：定义类型与定义名、声明条目标识、可空变体标识。只使用固定少量身份键，不引入新的通用状态协议。
2. 普通来源由来源收集器生成；组合与复合结果在各自正式生成点生成。保留现有 `DisplayLabel` 行为，不借日志任务重构全部显示名称系统。
3. Content按定义身份和条目标识读取现有语言键或本地化定义名称。正式条目名称优先，定义名称其次；两者均不可用时使用缺来源句式，不输出内部ID。
4. 创建日志时复制必要身份到条目自身；投射物继续沿已有发射计划保存语义上下文。不得在日志显示时查询角色当前选中芯片。
5. 双持/多来源发射必须读取该次实际发射记录的来源，不能把复合入口当成每颗弹的唯一来源。只复制语义身份，不持有可变化的完整正式结果对象。
6. 变体仍从既有定义与语言数据解析，不在代码中拼接硬编码中文。历史条目保存身份，句子在展示时生成。

不用给每件现有武器新增配置。完成后仅提交本任务的来源身份改动。

## 任务3：接入远程攻击和已有独立伤害入口

修改文件：

- `Source/BDP/Core/Verbs/BdpVerb_Shoot.cs`：`LogWarmupBattleEntry`。
- `Source/BDP/Core/Projectiles/BdpProjectile.cs`：`Impact`、`ExecuteImpact`及承伤结果关联处。
- `Source/BDP.Content/SenkuKogetsu/BdpSenkuKogetsuWave.cs`：已有命中条目创建、必要来源存档处。
- 任务1的远程条目与工厂。

步骤：

1. 在已有开火创建点调用中性工厂，仍使用最终语义目标，保留连发粒度和原版式记录时刻，不新增每颗弹的开火记录。
2. 在已有命中创建点调用同一工厂，继续传入实际受击对象、原意图目标、弹体和掩体；不改变碰撞、伤害、弹道与销毁顺序。
3. 保留原版 `TakeDamage`（承伤）与 `AssociateWithLog`（关联日志）链。日志不得反向影响伤害裁决。
4. 在本次交互完成时补已确认的承伤分类。区分实际接触、未命中、伤害被挡住和模块取消普通伤害；没有伤害部位不自动判定未命中。
5. 防护来源未知时省略其名字，不推断是哪种护盾。只确认伤害被挡住时，句子明确描述伤害，避免宣称整次攻击无效。
6. 旋空波体沿用自己的逐目标实际命中入口，不建立技能专属格式；缺少来源身份时补最小冻结数据，保留其现有伤害机制。
7. 自定义补充结果后使该条目的原版文字缓存失效；读档时恢复必要身份与分类，不重建或重播条目。

完成后提交远程接线与该次结果文法。

## 任务4：以一个窄补丁接入原版近战条目

新增文件：

- `Source/BDP.Content/PlayerLog/BattleLogEntry_BdpMeleeCombat.cs`
- `Source/BDP/Patches/Patch_Verb_MeleeAttack_CreateCombatLog_BdpEntry.cs`

修改：任务1的中性工厂接口、Content工厂和文法辅助类。

步骤：

1. 在原版 `CreateCombatLog` 方法内，只把条目构造调用替换为静态工厂调用，并传入当前动作对象。原来的判空、添加条目、返回及后续伤害处理保持原样。
2. 工厂判断当前动作是否为 `BdpVerb_MeleeAttackDamage`。非BDP动作构造完全相同的原版条目；BDP动作通过已注册提供器创建原版近战条目的子类。
3. 该补丁保留原版构造参数；不截断整段方法，不事后替换历史条目，不修改全局文字显示方法。
4. BDP子类保留原版命中、闪避、未命中和偏转规则选择，只补正式来源与必要动作文法。近战动作默认由原版工具/招式决定。
5. 补丁匹配必须检查唯一目标构造点；若当前程序集形状不符，停止该任务并说明具体差异，不扩大补丁到其它近战逻辑。
6. 保留 `BdpVerb_MeleeAttackDamage` 的原版 `base.TryCastShot()` 调用与既有节奏处理，避免复制伤害算法。

完成后单独提交近战条目接入。

## 任务5：在既有交互完成点补必要直接结果

新增文件：

- `Source/BDP.Content/PlayerLog/BattleLogEntry_BdpExplosionImpact.cs`
- `Source/BDP/Patches/Patch_DamageWorker_ExplosionDamageThing_BdpLogEntry.cs`
- `Source/BDP/Patches/Patch_DamageResult_AssociateWithLog_BdpEntry.cs`

修改文件：

- `Source/BDP/Core/Projectiles/RangedFlightProtocol/Impact/AttackTargetEvent.cs`
- `Source/BDP/Core/Projectiles/RangedFlightProtocol/Impact/AttackTargetEventDispatcher.cs`
- `Source/BDP/Core/Projectiles/RangedFlightProtocol/Effects/ExtraEffectExecutionContext.cs`
- `Source/BDP/Core/Projectiles/RangedFlightProtocol/Impact/DamageResolutionRuntime.cs`
- `Source/BDP/Core/Projectiles/BdpProjectile.cs`
- `Source/BDP/Patches/Patch_DamageWorker_ExplosionDamageThing_BdpSemantics.cs`
- `Source/BDP.Content/RangedModules/Debuff/RangedDebuffConfig.cs`
- `Source/BDP.Content/RangedModules/Debuff/RangedDebuffModule.cs`
- `Source/BDP.Content/RangedModules/Debuff/HediffExtraEffectExecutor.cs`
- `Source/BDP.Content/RangedModules/Debuff/ThresholdActions/RemoveBodyPartThresholdAction.cs`
- `1.6/Content/Defs/RangedModuleDef/RangedDebuff.xml`
- 任务1的结果条目与语言文件。

步骤：

1. 目标事件及执行上下文只增加可空的当前原版结果条目引用，派发器原样传递；不用攻击ID寻找全局历史条目。
2. 在Content模块配置增加可空的直接结果文法定义引用，并沿既有额外效果计划参数传递其定义名。默认空值不产生附加文案。
3. 执行器只在本次操作确实产生配置所声明的直接结果后补充句子。返回 `true` 不足以确认负重增加或部位移除；需要在对应操作点确认实际事实。
4. 此确认限定在当前调用内部；不订阅效果后续变化，不记录恢复/到期，不输出严重度、层数流水，不新增全量结果分类协议。
5. 普通BDP范围伤害只在原版爆炸条目的构造点接入子类，准入条件是当前爆炸存在BDP正式交互上下文；非BDP爆炸构造原版条目。复用原版添加、承伤和关联链。
6. 在已有承伤捕获中增加可空条目引用。仅当BDP语义作用域存在且当前原版伤害结果对象严格匹配已有捕获对象时，关联补丁将刚关联的条目写回捕获；不改变关联行为，不存全局历史表。逐目标爆炸后置处理从该次捕获取得对应条目，再传入目标事件。
7. 范围普通伤害被BDP取消时，原版不会创建结果条目。在现有已接受目标分支为角色创建一条结果条目，并保存在现有 `ImpactDamageScopeState` 的本次调用状态中；保留现有目标筛选、伤害/护盾裁决和效果派发顺序。
8. 直接结果只补对应目标的结果条目；不能把多个目标结果写进发射记录或另建等待缓冲。对范围伤害使用实际逐目标交互，不把范围筛选成功描述成普通子弹直接碰撞。
9. 普通伤害与直接结果组合一次；非伤害成功结果有独立的完整结果句式。确实无变化时不宣称效果增加。
10. 部位移除只有原版伤害结果尚未表达该事实且实际移除成功时才补充；同一事实不重复列出。
11. 当前附着与部位移除仅作为共用规则的接入样例，配置选文法，代码不按芯片名称判断。爆炸接入只覆盖上述已存在的BDP作用域，不修改全局爆炸玩法。

完成后提交本次交互结果接入。若实现需要改变既有伤害或效果玩法，立即停止并反馈，不能借日志任务调整机制。

## 任务6：接入战斗体关键事件

新增文件：

- `Source/BDP.Content/PlayerLog/CombatBody/CompCombatBodyPlayerLog.cs`
- `Source/BDP.Content/PlayerLog/CombatBody/PawnCombatBodyPlayerLogInjector.cs`
- `Source/BDP.Content/PlayerLog/CombatBody/CombatBodyPlayerLog.cs`

修改文件：

- `Source/BDP/Core/CombatBodySession/CombatBodyExitTransaction.cs`：退出原因。
- `Source/BDP.Content/CombatBody/Escape/CombatBodyEmergencyEscapeService.cs`：成功脱离点。
- `Source/BDP.Content/ContentBootstrap.cs`。
- 任务1的文法及语言文件。

步骤：

1. Content组件只注入已有战斗体宿主的角色定义，通过 `CombatBodySurfaceAccess.ResolveEvents` 获取既有阶段事件。初始化/载入恢复订阅，销毁时解除；不每帧扫描、不因读档重播当前状态。
2. 确认实际进入活动阶段后记录启动，实际进入崩解阶段后记录崩解。未知崩解原因使用通用句，已确认耗尽原因使用语言键。
3. 退出事务的原因解析纳入已有 `forceRelease` 参数，区分强制资格退出与主动解除。只在正常主动解除完成恢复后写主动解除。
4. 实际传送成功后记录紧急脱离，不能在准备方案时记录。芯片消费与屏幕通知保持既有机制。
5. 使用原版 `BattleLogEntry_Event` 与专属文法包。保留原版人物关联和存档，不采用默认非倒地即死亡图标的状态转变条目。
6. 关键事件在简略视图保留；和平启动/解除也记录，继续由原版战斗分组管理。各确认阶段只记一次，无额外“崩解结束”条目。

完成后提交关键事件及其文法。

## 任务7：交付检查与收尾

1. 逐文件检查中文注释与UTF-8；核对新增所有玩家文案均在语言文件，Core未引用Content，正式路径不依赖Development。
2. 检查每个事件只有一个提交入口；保留原版伤害关联、目标关联、顺序和筛选，不存在全局整句覆盖、状态观察器或历史仓库。
3. 在主模组目录执行编译，输出到独立交付目录：

   ```powershell
   dotnet build Source/BDP.Content/BDP.Content.csproj -c Release -p:OutDir=C:/NiwtDatas/Projects/RimworldModStudio/tmp/bdp-player-log-build/
   ```

   预期：生成 `BDP.Core.dll` 与 `BDP.Content.dll`，无新增编译错误。不要将临时目录或无关改动提交；编译成功不代表游戏内行为已验证。

4. 整理确切改动文件与交付程序集。部署沿工程既有方式进行，不删除用户存档或无关文件。
5. 写倒序工作日志，明确已做事项、编译结果和尚未实测内容；每步仅提交本步相关文件，不包含预先暂存的 `AGENTS.md` 或其他用户改动。
6. 对照本计划检查实现只保留最终必要路线，回退无意义的尝试性接入。

## 游戏验收项目（实施后供实测，不自动执行）

| 场景 | 应看到的行为 |
| --- | --- |
| 普通BDP远程攻击、攻击地面 | 原版粒度的开火与结果；缺目标仍有完整句子，无文法报错 |
| 敌人攻击角色 | 同一事实按人物关联显示，敌我规则相同 |
| 连发及多目标 | 无额外逐弹开火条目；真实不同命中不被同源去重 |
| 非BDP枪械与近战 | 原版记录方式与行为保持原样 |
| 近战命中、未命中、闪避和偏转 | 原版结果分类与伤害部位正确，正式来源准确 |
| 非伤害附着或无变化 | 成功时可有简短直接结果；无变化不宣称增加；恢复到期不刷日志 |
| 护盾与附带效果 | 只描述已确认抵挡范围，不由零伤害判断整次失败 |
| 飞行期间换武装、存读档 | 来源保持发起攻击时的身份，读档不新增历史条目 |
| 启动、主动解除、强制解除、崩解及脱离 | 时机正确、不重复，强制解除无主动文案，崩解无人物死亡误报 |
| 简略/全部与语言切换 | 筛选沿用原版，关键事件保留，句式和名称从语言数据生成 |

实施中发现需要增加表外系统改动时，按工程规则先说明必要性与具体范围，再继续。
