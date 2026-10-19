# 装配台槽位悬停预览实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标：** 拖动芯片悬停于有效槽位时，以半透明卡片预览放置后的结果；成对芯片同步预览目标槽和镜像槽。

**实现：** 在装配台槽位面板绘制完基础卡片后，用本帧鼠标位置查询现有可见槽位命中列表。复用现有兼容性与成对伙伴校验，只绘制视觉覆盖层；不新增状态、不调用事务。保持 1.6、1.7 共享源码。

**技术：** C# 7.3、RimWorld 原版 Verse/Unity 即时模式绘制、PowerShell 冒烟检查。

---

### 任务 1：增加悬停预览回归检查

**文件：**
- 新建：`Source/BDP.Tests/AssemblySlotHoverPreviewSmokeTests.ps1`
- 检查：`Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs`
- 检查：`Source/BDP.Content/Assembly/Window/Window_TriggerAssembly.cs`

**步骤 1：先写失败检查**

检查脚本应验证：

- 悬停命中使用现有可见槽位命中列表和本帧鼠标位置。
- 预览仅在拖拽且目标兼容、成对伙伴可用时绘制。
- 成对芯片会在目标侧与相同编号的对侧槽绘制结果卡。
- 预览绘制发生在两侧基础槽位卡片之后。
- 预览绘制代码不依赖装配事务；鼠标释放仍由窗口事务路径提交操作。
- 绘制覆盖层会恢复 GUI 颜色状态。
- 主副槽区与特殊槽区的合法目标均显示预览；成对预览只适用于主副槽区。
- 普通芯片替换成对芯片时，原伙伴槽显示空槽预览，避免旧芯片仍可见。

**步骤 2：确认检查因缺少预览实现而失败**

运行：`pwsh -File .\Source\BDP.Tests\AssemblySlotHoverPreviewSmokeTests.ps1`

预期：失败，提示缺少悬停预览绘制逻辑。

### 任务 2：实现最小视觉预览

**文件：**
- 修改：`Source/BDP.Content/Assembly/Window/Panel_SlotLayout.cs`

**步骤 1：捕获绘制入口鼠标位置**

在滚动视图建立前捕获本轮鼠标坐标；基础槽位卡片登记可见命中区域后，通过 `HitAt` 查找悬停槽位。主副槽卡片在舞台绘制结束时叠加预览，特殊槽卡片在特殊槽区绘制结束时叠加预览。

**步骤 2：添加局部预览绘制方法**

复用现有的芯片类别、名称、图标尺寸和文字度量方法，绘制半透明底色、细边框、左侧状态色条及芯片图文。通过 `try/finally` 恢复 `GUI.color`。

**步骤 3：绘制目标及成对镜像预览**

复用现有的芯片兼容性和伙伴可用性判断。目标有效时叠加预览；芯片为成对占槽类型时，在对侧相同索引卡片叠加相同芯片预览；普通芯片替换已成对槽位时，镜像伙伴卡片预览为空。跳过从槽位拖回同一绑定组的无操作情况。

**步骤 4：通过回归检查**

运行：`pwsh -File .\Source\BDP.Tests\AssemblySlotHoverPreviewSmokeTests.ps1`

预期：输出 `AssemblySlotHoverPreview PASS`。

### 任务 3：验证两个游戏版本并提交

**文件：**
- 构建输出：`1.6/Assemblies/BDP.Content.dll`、`1.7/Assemblies/BDP.Content.dll`
- 工作日志：`日志/Agent工作日志/` 中的当日日志文件

**步骤 1：运行相关冒烟检查**

运行新检查、`PairedSlotLinkVisualSmokeTests.ps1`、`PairedSlotInteractionSymmetrySmokeTests.ps1` 和 `TriggerAssemblyWindowSmokeTests.ps1`。

**步骤 2：分别构建 1.6 和 1.7**

从 `模组工程/BorderDefenseProtocol` 目录运行：

- `dotnet msbuild Source/BDP.Content/BDP.Content.csproj -p:RimWorldVersion=1.6 -p:Configuration=Release`
- `dotnet msbuild Source/BDP.Content/BDP.Content.csproj -p:RimWorldVersion=1.7 -p:Configuration=Release`

预期：两个命令均成功，输出分别留在各自版本目录。

**步骤 3：复核差异并提交**

只暂存本功能源码、测试、两个版本构建产物与本次工作日志；运行 `git diff --check` 后提交。不要暂存工作区中已有的 `tmp/` 及 ChartNative17 临时输出。
