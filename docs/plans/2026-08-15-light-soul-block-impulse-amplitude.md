# 光魂护盾受击位移幅度 Implementation Plan（实施计划）

> **For Claude（供执行者）：** REQUIRED SUB-SKILL（必需子技能）：使用 `superpowers:test-driven-development（测试驱动开发）` 逐项执行，并在提交前使用 `superpowers:verification-before-completion（完成前验证）`。

**Goal（目标）：** 将光魂灵活盾与举盾成功抵挡后的盾面受击位移幅度从 `0.08` 格降低为 `0.04` 格。

**Architecture（架构）：** 只调整 Content（内容层）两个正式 Hediff（健康状态）配置，不修改 Core（核心层）通用视觉冲量曲线。现有测试同时锁定配置值、首帧位移、半程 20% 反向回弹和结束归零。

**Tech Stack（技术栈）：** RimWorld Def XML（边缘世界定义配置）、PowerShell（微软命令行脚本）冒烟测试。

---

### Task 1（任务一）：建立减半幅度的失败测试

**Files（文件）：**

- Modify（修改）：`Source/BDP.Tests/LightSoulBlockFeedbackSmokeTests.ps1`

**Step 1：更新期望行为**

把两个姿态的配置断言从 `0.08` 改为 `0.04`。曲线实例使用 `Distance = 0.04`，并断言：

```powershell
Assert-True ([Math]::Abs($initialOffset.x - 0.04) -lt 0.0001)
Assert-True ([Math]::Abs($reboundOffset.x + 0.008) -lt 0.0001)
Assert-True ($finishedOffset -eq [UnityEngine.Vector3]::zero)
```

时长断言继续为 `8 tick`。

**Step 2：确认旧配置失败**

Run（运行）：

```powershell
& 'Source\BDP.Tests\LightSoulBlockFeedbackSmokeTests.ps1'
```

Expected（预期）：FAIL（失败），指出正式配置仍为 `0.08` 格。

### Task 2（任务二）：修改正式光魂配置

**Files（文件）：**

- Modify（修改）：`1.6/Content/Defs/HediffDef/LightSoul.xml`

**Step 1：实施最小 XML 调整**

在灵活盾和举盾两个 `HediffCompProperties_EnergyShield` 条目中分别修改：

```xml
<!-- 盾面最大内缩距离为 0.04 格。 -->
<blockVisualImpulseDistance>0.04</blockVisualImpulseDistance>
```

不修改 `blockVisualImpulseTicks=8` 或任何其它字段。

**Step 2：验证测试转绿**

Run（运行）：

```powershell
& 'Source\BDP.Tests\LightSoulBlockFeedbackSmokeTests.ps1'
```

Expected（预期）：PASS（通过）。

### Task 3（任务三）：验证、日志与提交

**Files（文件）：**

- Modify（修改）：`日志/Agent工作日志/Agent日志47.md`

**Step 1：解析正式 XML 并运行相关光魂测试**

运行光魂抵挡反馈、抵挡表现、光魂芯片测试，并确认 `LightSoul.xml` 可被 XML 解析器读取。

**Step 2：检查限定差异**

```powershell
git diff --check
```

确认没有 Core 源码或程序集变化。

**Step 3：记录并提交**

按时间倒序向 `Agent日志47.md` 增加本次调整，只暂存测试、正式 XML、计划和日志：

```powershell
git commit -m "tune: 降低光魂盾面受击位移"
```
