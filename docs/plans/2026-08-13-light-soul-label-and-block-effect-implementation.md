# 光魂制造名称与格挡表现修复 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让制造台所有动作预设入口显示语言包名称，并让光魂格挡只保留贴近盾面的闪光、火花与声音。

**Architecture:** Content.dll（内容程序集）的制造 UI（界面）统一解析动作预设标签；现有能量护盾组件增加两个向后兼容的表现参数。光魂只通过 XML（配置文件）选择关闭六边形格挡贴图和 0.4 格命中距离。

**Tech Stack:** C#、RimWorld Verse API（原版模组接口）、XML、PowerShell 回归检查。

---

### Task 1: 建立失败回归检查

**Files:**
- Modify: `Source/BDP.Tests/LightSoulChipSmokeTests.ps1`
- Modify: `Source/BDP.Tests/ChipManufacturingLocalizationSmokeTests.ps1`

**Steps:**
1. 断言制造列表和信息弹窗调用统一动作标签解析器，不再直接显示动作 `LabelCap`。
2. 断言护盾配置包含默认开启的 `showBlockGraphic` 和回退 `shieldRadius` 的 `impactEffectRadius`。
3. 断言光魂两种姿态设置 `showBlockGraphic=false`、`impactEffectRadius=0.4`。
4. 运行两项测试，确认因设施尚不存在而失败。

### Task 2: 修复制造台动作名称

**Files:**
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/Window_ChipManufacturing.cs`
- Modify: `Source/BDP.Content/Assembly/ChipManufacturing/UI/Window_ChipPresetInfo.cs`
- Create: `Source/BDP.Content/Assembly/ChipManufacturing/UI/ChipPresetLabelResolver.cs`

**Steps:**
1. 建立动作预设使用 `ResolvedLabel`、其他定义使用 `LabelCap` 的统一解析器。
2. 列表和信息弹窗标题改用该解析器。
3. 运行制造本地化测试，确认通过。

### Task 3: 调整格挡表现配置

**Files:**
- Modify: `Source/BDP.Content/Shield/HediffCompProperties_EnergyShield.cs`
- Modify: `Source/BDP.Content/Shield/HediffComp_EnergyShield.cs`
- Modify: `Source/BDP.Content/Shield/EnergyShieldEffectPlayer.cs`
- Modify: `1.6/Content/Defs/HediffDef/LightSoul.xml`

**Steps:**
1. 增加默认开启的 `showBlockGraphic` 与默认回退的 `impactEffectRadius`。
2. 格挡命中点改用解析后的独立特效距离。
3. 特效播放器按配置选择是否生成六边形 Fleck，但始终保留闪光和声音；原版偏转火花继续由既有调用播放。
4. 光魂两种姿态关闭六边形并设为 0.4 格。
5. 运行光魂测试，确认通过。

### Task 4: 验证、同步与提交

**Files:**
- Add: `日志/Agent工作日志/Agent日志46.md` 或下一个未满日志文件

**Steps:**
1. 编译 Core、Content、Development 三程序集，要求 0 错误。
2. 运行光魂、制造本地化、护盾关联回归检查。
3. 同步运行模组目录；若 RimWorld 锁定 DLL，明确报告并等待游戏退出。
4. 写工作日志并提交本次实现。
