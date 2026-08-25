# 光魂抵挡特效前后景 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 让光魂抵挡复合特效按地图南北关系稳定绘制在人物前方或后方。

**Architecture:** 在 `BDP.Content` 中增加一个可选的方向分层配置，未配置的护盾保持原行为。光魂 XML 引用两套复用原版资源的前景/后景特效定义，运行时使用已经解析出的攻击来源方向选择定义。

**Tech Stack:** RimWorld 1.6、C# 7.3、Def XML（定义 XML）、PowerShell 冒烟测试。

---

### Task 1: 建立失败回归测试

**Files:**
- Create: `Source/BDP.Tests/LightSoulImpactDepthSmokeTests.ps1`

**Step 1: 写失败测试**

- 要求确定性策略提供北侧后景判定。
- 要求光魂两个姿态显式启用方向分层。
- 要求前景、后景复合特效中的白闪、火花、烟尘使用统一绘制层。

**Step 2: 运行测试确认失败**

Run: `powershell -ExecutionPolicy Bypass -File Source/BDP.Tests/LightSoulImpactDepthSmokeTests.ps1`

Expected: FAIL，原因是方向分层方法和 XML 定义尚不存在。

### Task 2: 实现最小方向选择

**Files:**
- Modify: `Source/BDP.Content/Shield/EnergyShieldBlockPolicy.cs`
- Modify: `Source/BDP.Content/Shield/HediffCompProperties_EnergyShield.cs`
- Modify: `Source/BDP.Content/Shield/HediffComp_EnergyShield.cs`
- Modify: `Source/BDP.Content/Shield/EnergyShieldEffectPlayer.cs`

**Step 1: 添加纯方向判定**

以攻击来源方向 `z > 0` 判定后景，`z <= 0` 判定前景。

**Step 2: 添加可选 XML 配置**

允许业务定义声明是否启用方向分层、后景白闪，以及前景/后景偏转特效；缺省继续使用原版定义。

**Step 3: 接入现有抵挡时序**

使用当前已经算好的 `direction` 选择一次特效定义，不重新计算方向。

### Task 3: 声明光魂前后景特效

**Files:**
- Create: `1.6/Content/Defs/Effects/LightSoulImpact.xml`
- Create: `Languages/ChineseSimplified (简体中文)/DefInjected/ThingDef/LightSoulImpact.xml`
- Modify: `1.6/Content/Defs/HediffDef/LightSoul.xml`

**Step 1: 声明后景 Fleck（轻量粒子）**

复用原版 `ExplosionFlash`、`SparkFlash`、`AirPuff`、`MicroSparksFast` 的纹理和时长，统一改为 `Projectile` 层。

**Step 2: 声明前景 Mote（动态特效）**

复用原版两种飞散火花，统一改为 `MoteOverhead` 层。

**Step 3: 声明前景和后景 Effecter（组合特效）**

保持原版 `Deflect_Metal_Bullet` 的声效、数量、速度、缩放和角度参数，仅替换绘制层对应的子定义。

**Step 4: 配置两个姿态**

灵活姿态与举盾姿态引用同一组前后景定义。

### Task 4: 验证与提交

**Files:**
- Modify: `C:/NiwtDatas/Projects/RimworldModStudio/日志/Agent工作日志/Agent日志46.md`

**Step 1: 运行新增测试**

预期：`LightSoulImpactDepthSmokeTests PASS`。

**Step 2: 运行相关回归测试**

运行 `LightSoulBlockFeedbackSmokeTests.ps1`、`LightSoulChipSmokeTests.ps1` 与 `ShieldChipSmokeTests.ps1`。

**Step 3: 编译**

运行 Content 项目的 Release 构建，预期零错误。

**Step 4: 校验差异并记录日志**

确认只包含计划内文件，工作日志按时间倒序添加一条记录。

**Step 5: 提交**

提交计划与实现，保留可回退节点。
