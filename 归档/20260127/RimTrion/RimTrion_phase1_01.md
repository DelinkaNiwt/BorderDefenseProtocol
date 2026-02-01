作为**需求架构师**，我已根据 RiMCP 查阅的底层源码（Biotech DLC `Gene_Resource` 及 `StatDef` 系统），为您制定了 **Phase 1: 物理法则验证** 前两步的详细技术落地规划。

### 📋 核心技术摘要
*   **继承策略**：利用 `Gene_Resource` 作为父类是完全正确的。它为您解决了 90% 的 UI 绘制（那个能量条）和基础存读档问题。
*   **最大挑战**：`Gene_Resource` 默认将 `max` 视为一个静态 float 变量，而我们需要它是一个动态的 `StatDef`。这需要重写属性访问器。
*   **资源映射**：
    *   `Available` (可用量) ⮕ 对应父类 `cur`。
    *   `Capacity` (总量) ⮕ 对应 `pawn.GetStatValue(TrionMaxCap)`。
    *   `Reserved` (占用量) ⮕ 我们需新增的字段。

---

### Step 1: 定义物理常数 (StatDefs)

在 RimWorld 中，`StatDef` 是连接 XML 配置和 C# 逻辑的桥梁。我们需要定义两个全新的属性。

#### 1.1 XML 实现 (`Defs/Stats/Stats_Pawns_Trion.xml`)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Defs>

  <!-- 1. Trion Max Capacity: 能量总量上限 -->
  <StatDef>
    <defName>TrionMaxCap</defName>
    <label>trion capacity</label>
    <description>The maximum amount of Trion energy a virtual body can sustain.</description>
    <category>PawnCombat</category> <!-- 显示在小人面板的战斗一栏 -->
    <defaultBaseValue>0</defaultBaseValue> <!-- 默认为0，只有拥有基因/特性的人才有 -->
    <minValue>0</minValue>
    <toStringStyle>Integer</toStringStyle>
    <showOnAnimals>false</showOnAnimals>
    <displayPriorityInCategory>100</displayPriorityInCategory>
  </StatDef>

  <!-- 2. Trion Output: 输出功率 -->
  <StatDef>
    <defName>TrionOutput</defName>
    <label>trion output</label>
    <description>The maximum power output available to drive Trigger Chips.</description>
    <category>PawnCombat</category>
    <defaultBaseValue>0</defaultBaseValue>
    <minValue>0</minValue>
    <toStringStyle>Integer</toStringStyle>
    <showOnAnimals>false</showOnAnimals>
  </StatDef>

</Defs>
```

#### 💡 架构师批注
*   **Category**: 设置为 `PawnCombat` 意味着玩家可以在角色面板的 "Combat" 标签页直接看到这两个数值，方便调试。
*   **defaultBaseValue**: 设为 0 是关键。这意味着普通人类的上限为0，即便误装了 `Gene` 也会因为上限为0而无法积攒能量，这是一个天然的防错机制。

---

### Step 2: 实现能量核心 (C# Implementation)

这是物理层的核心。我们需要创建一个 `Gene_Trion` 类，它不仅是一个资源条，更是一个逻辑管理器。

#### 2.1 类结构设计
*   **继承**: `RimWorld.Gene_Resource`
*   **接口**: `IGeneResourceDrain` (如果未来需要像血原质一样自然流失) 或自定义接口。

#### 2.2 关键代码实现 (`Gene_Trion.cs`)

```csharp
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTrion
{
    // 定义接口以便未来扩展（如建筑/电池也能接入）
    public interface ITrionSource
    {
        float Available { get; }
        bool TryConsume(float amount);
        void Reserve(float amount);
        void Release(float amount);
    }

    public class Gene_Trion : Gene_Resource, ITrionSource
    {
        // === 核心数据 ===
        // "Reserved" 是父类没有的，我们需要自己存
        private float reservedTrion; 
      
        // === 属性重写 (The Override Magic) ===
      
        // 1. 劫持 Max 属性：不再读取 stored field，而是读取 StatDef
        public override float Max => pawn.GetStatValue(TrionDefOf.TrionMaxCap);

        // 2. 将父类的 "Value" 映射为我们的 "Available"
        public float Available => Value; // Value 其实就是 base.cur

        // 3. 初始值
        public override float InitialResourceMax => 0f; // 依赖 StatDef，这里仅作占位
      
        // 4. 只有在战斗体激活时才显示 Gizmo (能量条)
        // Phase 1 暂时常驻显示以便调试
        public override bool Active => base.Active && (pawn.GetStatValue(TrionDefOf.TrionMaxCap) > 0);

        // === 核心公式实现 ===

        // 尝试消耗 (开火/护盾扣除)
        public bool TryConsume(float amount)
        {
            if (Available >= amount)
            {
                Value -= amount; // 父类会自动 Clamp 到 0
                return true;
            }
            return false;
        }

        // 占用逻辑 (装备触发器)
        public void Reserve(float amount)
        {
            // 检查逻辑：占用量不能超过 (总上限 - 已消耗)
            // 但根据文档，占用量是硬性扣除上限。
            reservedTrion += amount;
          
            // 重新计算 Available 的上限
            // 注意：父类的 Value 属性 setter 会自动 Clamp (cur, 0, Max)。
            // 但我们的 "Max" 属性返回的是 StatDef 的全量。
            // 这里有一个逻辑陷阱：Gene_Resource 的 UI 是基于 Cur/Max 画的。
            // 如果我们想让 UI 显示 "灰色占用段"，我们需要自定义 Gizmo。
            // Phase 1 简化方案：直接扣除 cur。
          
            if (Value > (Max - reservedTrion))
            {
                Value = Max - reservedTrion;
            }
        }

        public void Release(float amount)
        {
            reservedTrion = Mathf.Max(0f, reservedTrion - amount);
        }

        // === 必须实现的父类方法 ===
      
        protected override Color BarColor => new Color(0.2f, 0.8f, 1.0f); // Trion 蓝色
        protected override Color BarHighlightColor => new Color(0.4f, 0.9f, 1.0f);

        public override void Tick()
        {
            base.Tick();
            // 可以在这里处理自然恢复，或者 "泄露 (Leaking)" 逻辑
            // Phase 1: 暂时留空
        }

        // === 存读档 ===
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref reservedTrion, "reservedTrion", 0f);
        }
    }
}
```

#### 2.3 自定义 Gizmo (UI显示)
原版 `GeneGizmo_Resource` 只能画一条简单的纯色条。为了体现 "Reserved"（被占用）的概念，我们需要在这个阶段就进行简单的修改。

建议 Phase 1 暂时使用原版 Gizmo，但在 `Label` 上做文章：
覆盖 `Gene_Resource.ResourceLabel`：
```csharp
public override string ResourceLabel 
{
    get 
    {
        return $"Trion (Reserved: {reservedTrion})";
    }
}
```

---

### ⚠️ Phase 1 开发注意事项 (架构师警告)

1.  **StatDef 缓存陷阱**:
    *   RimWorld 的 `GetStatValue` 并不总是每帧重新计算，它有缓存周期。
    *   **风险**: 当玩家穿上一件增加 `TrionMaxCap` 的装备时，能量条上限可能不会立即更新（通常默认 `checkEverySecond`）。
    *   **解决**: 在装备穿戴的回调中调用 `pawn.Notify_UsedVerbChanged()` 或者等待 60 ticks 自动刷新。Phase 1 暂时忽略，但在测试时要注意“延迟”。

2.  **Max 变化时的数值截断**:
    *   当 `Max` 因为 Debuff 突然降低时（例如 1000 -> 500），而当前 `Available` 是 800。
    *   `Gene_Resource` 的 `Value` setter 会自动执行 `Mathf.Clamp(value, 0f, max)`。
    *   这意味着多出的 300 点能量会**瞬间消失**。这符合设定（容器缩小，能量溢出耗散），但你需要知道这是自动发生的。

3.  **DefOf 绑定**:
    *   别忘了创建一个 `TrionDefOf` 类并用 `[DefOf]` 属性标记，把 `TrionMaxCap` 绑定到 C# 变量上，否则你每次都要用字符串查找 Def，性能极差。

```csharp
[DefOf]
public static class TrionDefOf
{
    public static StatDef TrionMaxCap;
    public static StatDef TrionOutput;
}
```

### 🎯 下一步行动建议
完成上述代码后，请直接启动游戏并使用 **DevMode -> Add Gene -> Trion Core**。
*   如果能看到蓝色能量条。
*   如果穿装备/改特性后，能量条的上限（分母）会变。
*   **则 Phase 1 的前置地基已夯实。** 接下来就可以做最刺激的部分：**"快照与自杀测试"**。