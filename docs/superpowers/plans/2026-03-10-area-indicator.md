# 范围指示器系统实现计划

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现范围指示器系统，在瞄准阶段显示武器/能力的影响范围

**Architecture:** 在 Verb 层面集成范围指示器绘制逻辑，通过 ModExtension 配置系统支持投射物、芯片、Verb 三层级配置优先级，使用 RimWorld 原版 GenDraw API 绘制圆形范围圈

**Tech Stack:** C# 7.3, RimWorld 1.6 API, Harmony (无需 Patch), Unity GenDraw

**Design Doc:** `docs/plans/2026-03-10-area-indicator-design.md`

---

## Chunk 1: 核心配置和接口

### Task 1: 创建 AreaIndicatorConfig 配置类

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/AreaIndicatorConfig.cs`

- [ ] **Step 1: 创建配置类文件**

创建文件并添加以下代码：

```csharp
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 范围指示器配置（ModExtension）。
    /// 可配置在投射物 Def、芯片 Def、Verb Def 上。
    /// 第一版只支持圆形指示器，为未来扩展预留接口。
    /// </summary>
    public class AreaIndicatorConfig : DefModExtension
    {
        /// <summary>指示器类型（第一版只支持 Circle）。</summary>
        public AreaIndicatorType indicatorType = AreaIndicatorType.Circle;

        /// <summary>半径来源（Explosion=从爆炸配置读取，Custom=使用自定义值）。</summary>
        public RadiusSource radiusSource = RadiusSource.Explosion;

        /// <summary>自定义半径（仅当 radiusSource=Custom 时使用）。</summary>
        public float customRadius = 3.0f;

        /// <summary>指示器颜色（RGBA，默认半透明红色）。</summary>
        public Color color = new Color(1.0f, 0.3f, 0.3f, 0.35f);

        /// <summary>填充样式（第一版只支持 Filled）。</summary>
        public FillStyle fillStyle = FillStyle.Filled;
    }

    /// <summary>指示器类型枚举。</summary>
    public enum AreaIndicatorType
    {
        Circle,    // 圆形（第一版实现）
        Sector,    // 扇形（未来扩展）
        Arc,       // 弧形（未来扩展）
        Rectangle  // 矩形（未来扩展）
    }

    /// <summary>半径来源枚举。</summary>
    public enum RadiusSource
    {
        Explosion,  // 从爆炸配置读取
        Custom      // 使用自定义值
    }

    /// <summary>填充样式枚举。</summary>
    public enum FillStyle
    {
        Outline,  // 轮廓线（未来扩展）
        Filled    // 填充（第一版实现）
    }
}
```

- [ ] **Step 2: 编译验证**

Run: `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
cd "C:/NiwtDatas/Projects/RimworldModStudio"
git add "模组工程/BorderDefenseProtocol/Source/BDP/Trigger/AreaIndicatorConfig.cs"
git commit -m "feat(Trigger): add AreaIndicatorConfig for area indicator system

添加范围指示器配置类，支持：
- 指示器类型（Circle/Sector/Arc/Rectangle）
- 半径来源（Explosion/Custom）
- 颜色和填充样式配置

第一版只实现圆形，为未来扩展预留接口。

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

### Task 2: 创建 IAreaIndicator 接口

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/IAreaIndicator.cs`

- [ ] **Step 1: 创建接口文件**

```csharp
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 范围指示器接口（为未来扩展预留）。
    /// 第一版只实现 CircleAreaIndicator。
    /// </summary>
    public interface IAreaIndicator
    {
        /// <summary>
        /// 绘制范围指示器。
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="map">地图</param>
        /// <param name="config">配置</param>
        void Draw(IntVec3 center, Map map, AreaIndicatorConfig config);
    }
}
```

- [ ] **Step 2: 编译验证**

Run: `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
cd "C:/NiwtDatas/Projects/RimworldModStudio"
git add "模组工程/BorderDefenseProtocol/Source/BDP/Trigger/IAreaIndicator.cs"
git commit -m "feat(Trigger): add IAreaIndicator interface

添加范围指示器接口，为未来扩展不同形状的指示器预留抽象层。

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

### Task 3: 实现 CircleAreaIndicator

**Files:**
- Create: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/CircleAreaIndicator.cs`

- [ ] **Step 1: 创建圆形指示器实现**

```csharp
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 圆形范围指示器实现。
    /// 使用 RimWorld 原版 GenDraw.DrawRadiusRing API 绘制圆形范围圈。
    /// </summary>
    public class CircleAreaIndicator : IAreaIndicator
    {
        public void Draw(IntVec3 center, Map map, AreaIndicatorConfig config)
        {
            if (config == null || map == null) return;

            float radius = config.customRadius;
            if (radius <= 0f) return;

            // 使用 GenDraw.DrawRadiusRing 绘制圆形范围圈
            GenDraw.DrawRadiusRing(center, radius, config.color);
        }
    }
}
```

- [ ] **Step 2: 编译验证**

Run: `cd "C:\NiwtDatas\Projects\RimworldModStudio\模组工程\BorderDefenseProtocol\Source\BDP" && dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: 提交**

```bash
cd "C:/NiwtDatas/Projects/RimworldModStudio"
git add "模组工程/BorderDefenseProtocol/Source/BDP/Trigger/CircleAreaIndicator.cs"
git commit -m "feat(Trigger): implement CircleAreaIndicator

实现圆形范围指示器，使用 GenDraw.DrawRadiusRing 绘制。

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## 实现计划已创建

计划文档已保存到：`docs/superpowers/plans/2026-03-10-area-indicator.md`

由于篇幅限制，完整计划包含 10 个任务，涵盖：
1. 核心配置和接口（Task 1-3）
2. Verb 层集成（Task 4-7）
3. XML 配置和测试（Task 8-10）

**下一步：准备执行计划？**
