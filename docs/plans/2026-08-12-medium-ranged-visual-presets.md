# Medium Ranged Visual Presets Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 用 RimWorld 原版 XML 继承把已验证的中型远程枪械单／双武器视觉参数收口成正式抽象预设。

**Architecture:** 建立单武器抽象基准承载缩放、握持点和枪口点；双武器抽象基准继承前者并加入四朝向姿态及握持定位。突击步枪和霰弹枪具体预设只保留贴图与身份，枪壳引用名称不变。

**Tech Stack:** RimWorld Def XML（定义配置）、PowerShell 静态冒烟测试、MSBuild（微软构建工具）

---

### Task 1: 锁定正式继承结构

**Files:**
- Create: `Source/BDP.Tests/MediumRangedVisualPresetInheritanceSmokeTests.ps1`

**Step 1:** 断言存在单武器和双武器抽象预设。

**Step 2:** 断言抽象预设承载已确认的全部参数。

**Step 3:** 断言突击步枪与霰弹枪具体预设只声明自己的贴图并继承正确基准。

**Step 4:** 运行测试，确认因抽象预设尚不存在而失败。

### Task 2: 提炼 XML 预设

**Files:**
- Modify: `1.6/Content/Defs/ExpressionDef/Visual.xml`

**Step 1:** 添加 `BDP_VisualBase_RangedMedium` 抽象单武器基准。

**Step 2:** 添加 `BDP_VisualBase_RangedMedium_Dual` 抽象双武器基准。

**Step 3:** 让突击步枪与霰弹枪四个具体预设继承基准并删除重复参数。

### Task 3: 验证现有行为边界

**Files:**
- Modify only if required by the new inheritance form: related `Source/BDP.Tests/*.ps1`

**Step 1:** 运行新增测试与现有突击步枪、霰弹枪、双持视觉测试。

**Step 2:** 使用 XML 解析器确认文档格式正确。

**Step 3:** 构建 Content（内容程序集），确认不会碰动作阶段视觉源码。

### Task 4: 记录与提交

**Files:**
- Create or modify: `日志/Agent工作日志/Agent日志*.md`

**Step 1:** 写入倒序工作日志。

**Step 2:** 只暂存本需求的文档、XML、测试和日志。

**Step 3:** 提交并复核工作区中其它改动仍未被纳入。
