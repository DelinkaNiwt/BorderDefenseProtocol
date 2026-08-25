# 双武器静默姿态状态修复实施计划

> 实施要求：按测试驱动开发（Test-Driven Development，测试驱动开发）逐项执行。

**目标：** 让双武器只在整体静默时使用倒 V 镜像和 6° 装饰角，任意一侧攻击时两把武器都按原版目标角绘制。

**架构：** 视觉姿态解析器直接读取现有运行时整轮执行态，不新增服务或缓存。配置层用两个默认关闭的中性选项控制静默镜像与静默装饰角，中型枪械双武器基准显式启用。

**技术栈：** C#（C#语言）、XML（可扩展标记语言）、PowerShell（脚本语言）冒烟测试、.NET（微软开发平台）编译。

---

### 任务一：建立失败回归测试

**文件：**

- 重命名并修改：`Source/BDP.Tests/VisualInactiveHandMirrorSmokeTests.ps1` → `Source/BDP.Tests/VisualIdlePoseStateSmokeTests.ps1`
- 修改：`Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1`
- 修改：`Source/BDP.Tests/RangedWeaponReferenceVisualSmokeTests.ps1`

**步骤：**

1. 将测试期望改为“整体静默”语义。
2. 断言解析器读取 `RuntimeState.HasExecutionState（运行时整轮执行态）`。
3. 断言中型枪械双武器基准启用静默镜像和静默装饰角。
4. 运行三个测试，确认它们因旧实现仍使用单条目状态而失败。

### 任务二：实现最小根因修复

**文件：**

- 修改：`Source/BDP/Core/Expressions/Config/ExpressionVisualSouthNorthPoseConfig.cs`
- 修改：`Source/BDP/Core/Trigger/Visual/VisualPoseResolver.cs`
- 修改：`1.6/Content/Defs/ExpressionDef/Visual.xml`

**步骤：**

1. 用 `HandMirrorOnlyWhenIdle` 替换旧字段。
2. 新增 `DecorativeAngleOnlyWhenIdle`，默认关闭。
3. 南北姿态使用整轮执行态，同时裁定镜像和装饰角。
4. 中型枪械双武器基准开启两个选项。
5. 运行失败测试，确认转为通过。

### 任务三：验证与交付

**步骤：**

1. 运行视觉姿态、继承、枪口锚点和动作阶段相关测试。
2. 运行全部 PowerShell（脚本语言）测试。
3. 编译 BDP 工程并核对输出。
4. 检查差异，只保留本次根因修复。
5. 写入工作日志并提交到当前分支。
