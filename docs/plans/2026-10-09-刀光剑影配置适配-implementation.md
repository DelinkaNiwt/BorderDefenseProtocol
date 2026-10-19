# 刀光剑影配置适配实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:**（目标）在刀光剑影武器配置页配置 BDP 有效近战功能/构型及真实形态姿态，并让实际攻击使用对应设置。

**Architecture:**（架构）使用现有可选兼容宿主，新增一个独立适配器。定义、状态解释与覆盖复用 BDP 现有机制，刀光设置和生成复用外部机制；通过经过检查的反射保持只依赖 Core。详细行为以同目录 `2026-10-09-刀光剑影配置适配-design.md` 为准。

**Tech Stack:**（技术）C# 7.3、.NET Framework 4.8、Harmony（运行时补丁）、RimWorld 1.6、PowerShell 行为夹具、现有语言包。

---

## 实施状态（2026-10-10）

- 任务 1—5 已完成；任务 6 的可插拔开发诊断、语言包与验收说明已完成，游戏内验收待执行。
- 正式逻辑位于 `BDP.Compatibility.dll`（兼容程序集）；观察器位于 `BDP.Development.dll`（开发程序集），默认不安装观察补丁。
- 构建时使用 `/p:BuildProjectReferences=false`（不重编译依赖工程），在临时输出目录放入原有 Core/Content 供编译引用；只发布兼容、开发程序集及其符号，Core/Content 没有重编译或替换。
- 配置行绘制补充原版 `UnityEngine.TextRenderingModule`（文字渲染模块）引用；业务和外部模组依赖边界保持原设计。
- 新增行为夹具 15 组通过，覆盖真实外部分类入口、设置数据库、补丁安装卸载与诊断启停；既有兼容宿主、NIT（背包页模组）和开发程序集边界检查通过。
- 独立夹具仅隔离 Unity 资源/日志和 Windows CLR（独立运行库）与游戏 Mono（游戏运行库）的语言排版差异，不验证实际画面、存盘重启和双持来源。具体实测步骤见同目录 `2026-10-10-刀光剑影适配验收.md`。

---

所有相对路径均以 `C:/NiwtDatas/Projects/RimworldModStudio/模组工程/BorderDefenseProtocol` 为根。直接当前工程、当前分支执行；不建工作树或新分支。每个实现文件逐成员中文注释，新增玩家文字只写语言包。不执行计划外重构。

## 任务 1：锁定行为与接口

创建 `Source/BDP.Tests/FlashingBladesCompatibilityBehaviorTests.cs` 与 `Source/BDP.Tests/FlashingBladesCompatibilityBehaviorTests.ps1`，使用现有独立 C# 行为夹具形式，编译至系统临时目录并清理；不提交临时产物。

先定义能验证下列行为的失败断言，再实现对应任务，不做源码关键字断言代替行为：

1. 同一功能、状态的两个芯片共享配置键；语言变化、左右手与实例标识不改变键。
2. 不同功能的同名形态姿态不共用构型状态键；无状态同构型共享键。
3. 大盾姿态虽只有防御附加条目，继承公共盾击后仍入列；纯远程/纯防御状态排除。
4. 构型替换工具和追加工具按现有覆盖规则成立；构型自动不读功能手动覆盖。
5. 制造包装形态归一化，不与功能基础条目重复。
6. 自动类别按外部工具评分；构型不同来源类别不同时标记动态默认，播放使用各自有效输入。
7. 上下文各自保存来源；重置只清本条目；重新加载仍读到字符串设置。

复查外部程序集模块标识；发现签名变化时重新反编译有关入口，而不是照旧修改或直接加绕过补丁。

运行：`pwsh -NoProfile -File Source/BDP.Tests/FlashingBladesCompatibilityBehaviorTests.ps1`。首次允许预期的缺少适配器或未实现行为失败；不能把缺依赖、夹具编译错误当作有效失败证据。

## 任务 2：目录身份与复用解释

创建：

- `Source/BDP.Compatibility/FlashingBlades/FlashingBladesCatalog.cs`：目录项、状态枚举、身份键、名称与默认类别。
- `Source/BDP.Compatibility/FlashingBlades/FlashingBladesDiscovery.cs`：一次性发现外部目标与 BDP 内容定义/服务，缓存访问器及分类输入。

按设计查询已加载功能定义，遍历实际形态及姿态。构型候选先调用 `ChipCombinationCompatibilityService.Validate`（制造合法性校验），以各已加载可接受职业检查是否存在合法组合；不能以功能默认职业或构型白名单替代完整校验。

复制配置用 `ChipConfigurationMergeService.CloneSingle`（复制功能配置），构型处理用 `ChipArmamentFormApplicationService.Apply`（覆盖配置）；只调用到克隆结果，不修改 Def 原数据。调用现有 `ResolveUncached`（无缓存契约解释）检查结构、选择状态及正规化近战工具。复用已解析 `DeclaredMeleeToolSurfaces`（有效工具表面），避免重复计算工具选择规则。

身份构造算法必须使用固定字段顺序：

```csharp
// 身份段只来自稳定定义/状态键；空串表示不存在的层级。
static string BuildKey(string kind, string definition, string stateOwner,
    string mode, string stance)
{
    return "BDP.FB.v1|" + string.Join("|", new[]
    {
        kind, definition, stateOwner, mode, stance
    }.Select(value => Uri.EscapeDataString(value ?? string.Empty)));
}
```

默认分类使用每个有效来源输入的全部工具，调用外部 `ClassifyWeaponDef`（定义分类）。私有分类输入定义只存在兼容缓存，不注册全局、不创建物品。无状态构型按键合并目录行，同时保留功能来源到分类输入的映射；类别不一致时标注随来源，不能混合所有来源工具求一个虚假类别。

夹具先验证契约继承与覆盖失败，再验证实现通过。提交本任务真实改动，不提交假构型 Def 或测试副作用。

## 任务 3：配置页与语言包

创建 `Source/BDP.Compatibility/FlashingBlades/FlashingBladesWindowBridge.cs`。

创建 `Languages/ChineseSimplified (简体中文)/Keyed/FlashingBladesCompatibility.xml`，仅新增条目格式、形态/姿态/来源功能标记、动态自动类别、距离不适用提示以及本适配诊断的语言键。已有功能和状态名称读取现有语言键。

只在外部配置窗口 `BuildFilteredEntries`（生成筛选列表）之后追加虚拟行，读取同窗口搜索、模组、仅覆盖筛选条件；每次窗口列表重建后追加一次，不修改全局武器目录。复用原菜单的按字符串设置/清除方法，显示本条目的有效自动类别。

仅为虚拟行接管 `DrawWeaponRow`（绘制武器行），其它行完整沿用原方法。距离区域显示不适用，不调用距离菜单，不把触发体范围假称为表达武器范围。定制同窗口模组菜单计数，以原始目录数加本目录项数展示 BDP 计数；不修改其它窗口的全局计数。

验证搜索光魂、举盾、蝎子，以及按 BDP 模组筛选和仅覆盖筛选；每次刷新不得重复行。原有“清空全部”按钮应同时清掉虚拟字符串覆盖记录。长名称沿用游戏界面可用的截断/悬浮说明，保证功能、形态、姿态能辨认。

## 任务 4：实际攻击与限定补丁

创建 `Source/BDP.Compatibility/FlashingBlades/FlashingBladesRuntimeBridge.cs` 与 `Source/BDP.Compatibility/FlashingBlades/FlashingBladesAdapter.cs`。

修改 `Source/BDP.Compatibility/CompatibilityBootstrap.cs`，仅注册新的独立适配器；不改工程引用或现有 NIT 适配器。

播放选择伪代码：

```text
上下文构建完成：
  非 BDP 实际近战 → 不处理
  读取实际宿主 ResultId → 公开来源 → 来源芯片
  读取功能/生效构型 → 归一化功能自身形态姿态
  选择已有的构型状态键，否则功能状态键
  按当前有效来源取得自动类别、读取该键手动覆盖
  冻结到本次播放上下文的弱关联记录

分类入口：
  没有 BDP 记录 → 原方法
  有记录 → 返回新的分类结果，采用记录的自动类别

生成入口：
  没有 BDP 记录 → 原参数
  有记录 → 仅将手动纹理参数替换为记录的覆盖，未覆盖则未知枚举
```

严禁修改外部共享的定义分类结果对象；新建本次分类结果，避免污染触发体或其它角色缓存。身份无法定位时，以当前真实工具自动分类，不读取触发体的手动设置。

外部 `TryGetExtendedRangeData`（查询扩展范围）只对 `BdpVerb_MeleeAttackDamage` 拒绝范围接管，返回未取得扩距；其它攻击原样。该补丁是防止本次纯视觉适配改变 BDP 战斗的边界，不实现另一套距离逻辑。

适配安装和禁用复用现有宿主；必需成员全部确认后才安装，安装途中失败撤销自己的全部补丁。原有效果启用、阵营、概率、排除和去重检查照常运行，不从另一个事件补播效果。

行为夹具验证两个独立上下文不串配置、同定义换状态后的新上下文更新、已有上下文不被新状态覆盖、外部缺失/成员缺失时独立停用；对外部真实设置数据库验证保存、清除和重新初始化。通过后提交。

## 任务 5：编译与边界验证

已确认本机 MSBuild（构建工具）路径如下。实现阶段先将产物构建到任务专用临时目录，验证后再更新正式兼容程序集与符号；Core、Content 不重编译或替换。

```powershell
$taskBuildDirectory = 'C:/NiwtDatas/Projects/RimworldModStudio/tmp/bdp-flashing-blades-build/'
New-Item -ItemType Directory -Path $taskBuildDirectory -Force | Out-Null
# 全局 OutputPath 也影响工程引用解析，先放入原有依赖产物，禁止重编译它们。
Copy-Item -LiteralPath '1.6/Assemblies/BDP.Core.dll' -Destination $taskBuildDirectory
& 'C:/NiwtApplications/VIsualStudio/Community/MSBuild/Current/Bin/MSBuild.exe' 'Source/BDP.Compatibility/BDP.Compatibility.csproj' /t:Build /p:Configuration=Release /p:BuildProjectReferences=false /p:OutputPath=$taskBuildDirectory
Copy-Item -LiteralPath '1.6/Assemblies/BDP.Content.dll' -Destination $taskBuildDirectory
& 'C:/NiwtApplications/VIsualStudio/Community/MSBuild/Current/Bin/MSBuild.exe' 'Source/BDP.Development/BDP.Development.csproj' /t:Build /p:Configuration=Release /p:BuildProjectReferences=false /p:OutputPath=$taskBuildDirectory
# 开发工程会清理临时目录中的 Core/Content 副本；夹具从正式目录解析这些依赖。
pwsh -NoProfile -File 'Source/BDP.Tests/FlashingBladesCompatibilityBehaviorTests.ps1' -AssemblyDirectory $taskBuildDirectory -DevelopmentAssemblyDirectory $taskBuildDirectory
pwsh -NoProfile -File 'Source/BDP.Tests/CompatibilityAssemblyBoundarySmokeTests.ps1'
pwsh -NoProfile -File 'Source/BDP.Tests/CompatibilityHostSafetySmokeTests.ps1'
pwsh -NoProfile -File 'Source/BDP.Tests/NiceInventoryTabCompatibilitySmokeTests.ps1'
pwsh -NoProfile -File 'Source/Build/Verify-BdpCompatibilityBoundary.ps1' -CompatibilityProjectPath 'Source/BDP.Compatibility/BDP.Compatibility.csproj' -CompatibilityAssemblyPath 'C:/NiwtDatas/Projects/RimworldModStudio/tmp/bdp-flashing-blades-build/BDP.Compatibility.dll' -ModSourceRoot 'Source' -CoreAssemblyPath '1.6/Assemblies/BDP.Core.dll'
git diff --check
```

预期：构建零错误，新增行为夹具所有断言通过，既有兼容宿主及依赖边界检查通过，外部程序集未形成硬引用。边界检查对本适配的外部程序集引用还须加入行为夹具验证，不能只依赖旧检查对 NIT 的特定检查。

未来构型验证数据仅放夹具；临时输出不加入版本控制。失败必须追根因，回退无效尝试；不顺手改其它系统。

## 任务 6：可插拔实测诊断与交付

创建 `Source/BDP.Development/Compatibility/FlashingBladesPlaybackDiagnostics.cs`，只在 Development 加载及显式启用时安装观察补丁，正式兼容代码不依赖 Development。观察窗口条目及实际播放上下文，记录结果标识、来源芯片、形态姿态、配置键、自动类别、手动纹理类别与播放次数；有失配则单独标识。不开逐帧日志，不向正式配置页塞实现术语。

新诊断文字放在开发模组对应语言包，沿用既有开发程序集入口与打包方式。诊断应可禁用、易检索、可单独删除，不为它增加永久业务事件接口。

游戏内验证：

1. 当前有效名单包含弧月、蝎子、风刃及光魂三个状态；无姿态不多造姿态标注。
2. 分别修改光魂三个状态配置，切换后准确生效；一个角色的设置作用于另一个同类芯片。
3. 蝎子每轮三次合法挥击对应三次外部播放尝试；未命中也允许挥击，目标非法不额外播放。双持分别核对主副来源。
4. 保存重启、恢复自动、清空全部；搜索和筛选结果同步。
5. 外部效果总开关、阵营、概率和触发体排除仍有效。开启扩距后，BDP 仍按原有接触追敌/合法性判断，其它原版武器沿用外部扩距。
6. 移除刀光剑影后 BDP 可正常加载；诊断关闭后无持续日志，移除开发程序集不影响正式兼容。

更新工作日志并提交正式兼容产物、语言包、有效测试和必要文档。没有实际游戏验收证据时，交付应明确“代码与构建已验证，游戏内待验收”，不得写已完全验证。
