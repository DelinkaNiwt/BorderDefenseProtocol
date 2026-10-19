# BDP 分版本构建

BDP 维护游戏版本 1.6、1.7；共享同一套源码，每次构建必须显式指定版本。版本不明确时先向用户确认，不能从本机安装版本、当前目录或近期工作推断。

## 公共资源与版本目录

```text
BorderDefenseProtocol/
├─ Common/                 相同资源只存一份
│  ├─ Defs/                中性定义
│  ├─ Textures/、Sounds/   贴图与音效
│  ├─ Content/             正式业务定义与补丁
│  └─ Development/         公共开发定义、补丁和语言文件
├─ 1.6/                    1.6 程序集与实际版本差异
├─ 1.7/                    1.7 程序集与实际版本差异
├─ Source/                 共享源码
└─ Languages/              共享语言文件
```

`LoadFolders.xml`（加载目录配置）在两版本中均显式加载公共目录及本版本目录，版本专属内容排在对应公共内容之后。相同定义不重复复制；定义发生版本差异时，使用本版本的原版 XML（可扩展标记语言）补丁，或将有差异的条目移出公共目录后分别维护，不能依赖重复的同名定义覆盖。公共目录不放版本程序集。

## 引用配置

将 `Bdp.Local.props.example` 复制为本目录的 `Bdp.Local.props`，填入核实后的本机路径。该文件不入库。

- 1.6：真实原版 `RimWorldWin64_Data/Managed` 目录（或经核实的该版本独立引用备份），以及对应 1.6 的 `0Harmony.dll`。
- 1.7：含 `Assembly-CSharp.dll`、`UnityStubs.dll`、`Waylith.UnityCompat.dll` 和 `Waylith.dll` 的游戏根目录，以及对应 1.7 的 `0Harmony.dll`。
- 不存在的版本留空；构建会明确停止。不得只修改目录名、混装两个版本的 DLL（程序集）或将 1.7 引用当成 1.6。
- 游戏升级会替换安装目录；升级前保留仍维护版本的真实游戏引用与版本信息。原版、扩展包和第三方源码查询、定义数据库也必须对应明确的版本与模组组合。

## 构建命令

从模组根目录运行，目标由用户明确选择。Development（开发辅助）工程会连同 Core（核心）与 Content（正式内容）一起构建；Compatibility（外部兼容）单独构建。

```powershell
# 1.6：保留原有 .NET Framework 4.8 编译方式。
dotnet msbuild Source/BDP.Development/BDP.Development.csproj -p:RimWorldVersion=1.6 -p:Configuration=Release
dotnet msbuild Source/BDP.Compatibility/BDP.Compatibility.csproj -p:RimWorldVersion=1.6 -p:Configuration=Release

# 1.7：条件使用 .NET 10 SDK（开发工具包），先还原构建信息。
dotnet msbuild Source/BDP.Development/BDP.Development.csproj -restore -p:RimWorldVersion=1.7 -p:Configuration=Release
dotnet msbuild Source/BDP.Compatibility/BDP.Compatibility.csproj -restore -p:RimWorldVersion=1.7 -p:Configuration=Release
```

输出位于 `<版本>/Assemblies`；Development 输出位于 `<版本>/Development/Assemblies`；每个工程的中间文件位于 `obj/<版本>/<配置>`。NuGet（构建还原工具）信息位于 `obj/<版本>`。源码可使用 `BDP_RIMWORLD_1_6` 或 `BDP_RIMWORLD_1_7` 编译条件，仅在已确认的版本差异位置使用。

不要通过命令行覆盖 `OutputPath`、`IntermediateOutputPath` 或 `MSBuildProjectExtensionsPath`；构建检查会拒绝偏离当前版本的路径，防止旧脚本误覆盖另一版本。

两版本的程序集依赖方向保持一致：Core 不引用其他 BDP 程序集，Content 和 Compatibility 各自引用 Core，Development 可引用 Core 与 Content，正式层不得反向引用 Development。

## 发布与验证

- `LoadFolders.xml`（加载目录配置）明确区分两个版本的资源；根目录语言文件用于共同文案，版本专属文案进入对应版本语言目录。
- `Verify-BdpDevelopmentBoundary.ps1` 和 `Verify-BdpReleaseReady.ps1` 检查脚本必须显式提供 `-RimWorldVersion 1.6` 或 `1.7`。正式发布检查要求移除 `Common/Development` 和所选版本的 Development 运行内容；制作发布包时同步调整开发加载条目，避免引用不存在的目录。工作区开发状态不等于正式发布包。
- 先确认对应版本的必需程序集已编译成功，再制作或部署该版本发布包；资源基线复制不能视为兼容性验证通过。
- 本机已核实 1.6 独立安装位于 `C:/NiwtGames/RimWorld-1.6`（1.6.4871），Steam 安装为 1.7.4977。各版本编译和实测发现的问题进入兼容性排查清单，统一分析后修复，不向另一版本直接覆盖。
- 历史 `Source/BDP.Tests` 脚本中仍有旧的 `1.6/Defs`、`1.6/Content` 等资源路径；按后续实际验证需要迁移到公共/目标版本加载布局，不将旧路径断言失败误认成游戏业务错误。
