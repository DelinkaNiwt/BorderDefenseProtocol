# BDP 武器朝北高度修复实施计划

> **For Codex（供 Codex 执行）：** 按 `test-driven-development（测试驱动开发）` 先红后绿，并在提交前使用 `verification-before-completion（完成前验证）`。

**目标：** 让所有 BDP 显式南北姿态在保留自身造型的同时，恢复 RimWorld（边缘世界）原版“朝北比朝南高”的高度差。

**实现方式：** 不修改 C#（C井语言）解析器。只在正式视觉 XML（可扩展标记语言）中令南北方向获得相同的 BDP 额外纵向偏移，使最终高度差只来自原版持械点；仅替换贴图、继续沿用原版姿态的预设保持不变。

**技术范围：** PowerShell（微软命令行脚本）冒烟测试、RimWorld Def XML（定义配置）。依照项目约束直接在当前工程执行，不创建 worktree（工作树）。

---

### 任务一：建立朝北高度等价规则的回归测试

**文件：**

- 新建：`Source/BDP.Tests/VisualNorthElevationParitySmokeTests.ps1`

**步骤：**

1. 读取 `1.6/Content/Defs/ExpressionDef/Visual.xml`。
2. 枚举所有直接声明 `SouthNorthPose（南北姿态）` 的正式预设。
3. 解析 `DefaultOffset.z（基础纵向偏移）`，并按缺省值零读取南北补偿。
4. 断言每个预设满足 `NorthZAdjust = 2 × DefaultOffset.z + SouthZAdjust`。
5. 用原版成年人物南向 `-0.22`、北向 `-0.11` 持械点复算，断言朝北最终比朝南高 `0.11` 格。
6. 断言只替换贴图的单武器预设没有新增显式姿态。
7. 运行：`pwsh -NoProfile -File Source/BDP.Tests/VisualNorthElevationParitySmokeTests.ps1`。
8. 预期：旧配置因朝北补偿不满足等价关系而失败。

### 任务二：修正正式视觉配置与旧断言

**文件：**

- 修改：`1.6/Content/Defs/ExpressionDef/Visual.xml`
- 修改：`Source/BDP.Tests/LightSoulChipSmokeTests.ps1`
- 修改：`Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1`

**步骤：**

1. 将弧月朝北补偿从 `0.05` 改为 `0.15`。
2. 为光魂灵活盾单／双预设写入 `0.20`。
3. 将光魂举盾单／双预设从 `0.25` 改为 `0.36`。
4. 为光魂重刃写入 `0.24`。
5. 为中型远程双武器基准写入 `0.06`。
6. 每条补偿旁写明其用途是抵消 BDP 南北反转、保留原版高度差。
7. 将光魂旧测试由“南北等高”改为“朝北高 `0.11` 格”，并同步中型双武器基准断言。

### 任务三：验证与交付

**文件：**

- 修改：`C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/` 下当前日志文件

**步骤：**

1. 运行新回归测试及所有受影响的现有视觉测试。
2. 解析正式 XML，确认不存在格式错误。
3. 检查 `git diff --check` 与限定范围差异。
4. 按时间倒序记录工作日志。
5. 只暂存本次计划、测试、视觉配置与工作日志，提交一个可回退版本。
