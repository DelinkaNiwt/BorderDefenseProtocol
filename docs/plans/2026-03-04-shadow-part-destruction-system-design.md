---
标题: 影子部位被毁时移除真身部位 - 设计方案
版本号: v1.1
更新日期: 2026-03-04
最后修改者: Claude Sonnet 4.6
标签: [设计文档][战斗体][伤害系统][部位破坏][已实现]
摘要: BDP战斗体影子部位破坏系统设计文档v1.1。反映实际实现：使用自定义HediffDef（BDP_CombatBodyPartDestroyed）标记部位失效，无疼痛/流血，战斗体解除时恢复部位。PartDestructionHandler管理已破坏部位集合。
---

# 影子部位被毁时移除真身部位 - 设计方案 v1.1

## 一、需求描述（✅ 已实现）

当战斗体激活时，影子HP系统追踪所有部位的虚拟HP。当某个部位的影子HP降至0时，应该：
1. 在真身上移除对应的部位（模拟部位被摧毁）
2. **不触发疼痛和流血**（战斗体保护机制）
3. 触发能力丧失效果（如手臂失去操作能力）
4. 记录被破坏的部位，避免重复处理

## 二、设计原则

### 2.1 时机选择
- **拦截点**：`ShadowHPHandler.Handle()` 中检测到部位HP≤0时
- **执行时机**：伤害应用后立即执行（在同一帧内）
- **避免重复**：使用标记集合记录已破坏部位

### 2.2 部位移除方式

**使用自定义 HediffDef**（而非原版 `MissingBodyPart`）：

创建 `BDP_CombatBodyPartDestroyed` HediffDef，特性：
- `destroyedOutright = true`：标记部位为"完全摧毁"
- `makesSickThought = false`：不产生负面心情
- **无疼痛**：不设置 `painOffset` 或 `painFactor`
- **无流血**：不设置 `tendable` 或 `bleedRate`
- **保留能力丧失**：通过 `capMods` 设置部位功能为0

优势：
- 完全控制副作用（只保留能力丧失）
- 战斗体解除时可以移除这些 Hediff（恢复部位）
- 与原版 `MissingBodyPart` 区分开（战斗体特有机制）

### 2.3 破裂条件不变
关键部位（头部/躯干）被毁时，仍然触发战斗体破裂：
- 部位移除 → 战斗体解除 → 真身承受后续伤害
- 破裂逻辑在 `CollapseHandler` 中处理，不在此模块

## 三、架构设计

### 3.1 职责分配

#### ShadowHPHandler（修改）
- 检测部位HP≤0
- 调用新的 `PartDestructionHandler.Handle()`
- 返回处理结果

#### PartDestructionHandler（新增）
- 执行部位移除操作
- 管理已破坏部位集合
- 提供查询接口

#### Gene_TrionGland（修改）
- 持有 `PartDestructionHandler` 实例
- 激活时初始化，解除时清理

### 3.2 数据流

```
伤害应用 → ShadowHPHandler.Handle()
    ↓
检测 HP≤0 → PartDestructionHandler.Handle()
    ↓
AddHediff(BDP_CombatBodyPartDestroyed) → 部位失效（无疼痛/流血）
    ↓
记录已破坏部位 → 避免重复处理
```

## 四、实现细节

### 4.1 新增 HediffDef：BDP_CombatBodyPartDestroyed

在 `Defs/Combat/HediffDefs_CombatBody.xml` 中定义：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <!-- 战斗体部位摧毁 Hediff -->
  <HediffDef>
    <defName>BDP_CombatBodyPartDestroyed</defName>
    <label>战斗体部位摧毁</label>
    <description>战斗体保护下的部位摧毁。部位失去功能但不产生疼痛和流血。</description>
    <hediffClass>Verse.Hediff_MissingPart</hediffClass>

    <!-- 关键属性：标记为完全摧毁 -->
    <destroyedOutright>true</destroyedOutright>

    <!-- 不产生负面心情 -->
    <makesSickThought>false</makesSickThought>

    <!-- 不产生疼痛（不设置 painOffset 或 painFactor） -->

    <!-- 不产生流血（不设置 tendable 或 bleedRate） -->

    <!-- 不可见（战斗体内部机制） -->
    <displayWound>false</displayWound>

    <!-- 永久存在（直到战斗体解除时手动移除） -->
    <chronic>true</chronic>

    <!-- 不影响美观度 -->
    <affectsSocialInteractions>false</affectsSocialInteractions>
  </HediffDef>
</Defs>
```

### 4.2 新增类：PartDestructionHandler

```csharp
using System.Collections.Generic;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 部位破坏处理器。
    /// 当影子HP耗尽时，在真身上添加"战斗体部位摧毁"Hediff。
    ///
    /// 职责：
    /// - 执行部位破坏操作（AddHediff BDP_CombatBodyPartDestroyed）
    /// - 管理已破坏部位集合（避免重复处理）
    /// - 提供查询接口
    /// - 战斗体解除时移除所有破坏 Hediff
    /// </summary>
    public class PartDestructionHandler
    {
        // 已破坏部位的 Hediff 引用（用于解除时移除）
        private List<Hediff> destroyedPartHediffs = new List<Hediff>();

        // 已破坏部位集合（key: defName路径，用于快速查询）
        private HashSet<string> destroyedParts = new HashSet<string>();

        /// <summary>
        /// 处理部位破坏。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="part">被破坏的部位</param>
        /// <returns>true=成功标记部位破坏，false=已破坏或失败</returns>
        public bool Handle(Pawn pawn, BodyPartRecord part)
        {
            if (pawn == null || part == null) return false;

            string partKey = GetPartKey(part);

            // 检查是否已破坏
            if (destroyedParts.Contains(partKey))
            {
                Log.Message($"[BDP] 部位 {part.def.defName} 已被破坏，跳过");
                return false;
            }

            // 执行部位破坏
            try
            {
                // 使用自定义 HediffDef（无疼痛/流血）
                var hediffDef = BDP_DefOf.BDP_CombatBodyPartDestroyed;
                if (hediffDef == null)
                {
                    Log.Error("[BDP] BDP_CombatBodyPartDestroyed HediffDef 未找到");
                    return false;
                }

                var hediff = pawn.health.AddHediff(hediffDef, part);

                Log.Message($"[BDP] ⚠ 影子部位被毁: {part.def.defName} → 真身部位已失效（无疼痛/流血）");

                // 记录 Hediff 引用（用于解除时移除）
                if (hediff != null)
                {
                    destroyedPartHediffs.Add(hediff);
                }

                // 记录已破坏
                destroyedParts.Add(partKey);

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[BDP] 标记部位破坏失败: {part.def.defName}, 错误: {e}");
                return false;
            }
        }

        /// <summary>
        /// 检查部位是否已被破坏。
        /// </summary>
        public bool IsDestroyed(BodyPartRecord part)
        {
            if (part == null) return false;
            return destroyedParts.Contains(GetPartKey(part));
        }

        /// <summary>
        /// 清理已破坏部位记录并移除所有破坏 Hediff（战斗体解除时调用）。
        /// </summary>
        public void Clear(Pawn pawn)
        {
            // 移除所有破坏 Hediff（恢复部位）
            if (pawn?.health?.hediffSet != null)
            {
                foreach (var hediff in destroyedPartHediffs)
                {
                    if (hediff != null && pawn.health.hediffSet.hediffs.Contains(hediff))
                    {
                        pawn.health.RemoveHediff(hediff);
                        Log.Message($"[BDP] 战斗体解除：恢复部位 {hediff.Part?.def.defName}");
                    }
                }
            }

            destroyedPartHediffs.Clear();
            destroyedParts.Clear();
        }

        /// <summary>
        /// 获取部位的唯一标识key（defName路径）。
        /// </summary>
        private string GetPartKey(BodyPartRecord part)
        {
            if (part == null) return "";

            var path = new System.Text.StringBuilder();
            var current = part;
            while (current != null)
            {
                if (path.Length > 0)
                    path.Insert(0, "/");
                path.Insert(0, current.def.defName);
                current = current.parent;
            }

            return path.ToString();
        }
    }
}
```

### 4.3 修改：BDP_DefOf.cs

添加新的 HediffDef 引用：

```csharp
[DefOf]
public static class BDP_DefOf
{
    // ... 其他 Def 引用 ...

    // 战斗体部位摧毁 Hediff
    public static HediffDef BDP_CombatBodyPartDestroyed;

    static BDP_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BDP_DefOf));
    }
}
```

### 4.4 修改：ShadowHPHandler.Handle()

在 `ShadowHPHandler.cs` 中添加部位破坏检测：

```csharp
public static bool Handle(Pawn pawn, BodyPartRecord hitPart, float damage)
{
    var gene = pawn?.genes?.GetFirstGeneOfType<BDP.Core.Gene_TrionGland>();
    if (gene?.ShadowHP == null)
    {
        Log.Warning($"[BDP] ShadowHPHandler: {pawn?.Name} 没有影子HP系统");
        return false;
    }

    // 应用伤害到影子HP
    gene.ShadowHP.TakeDamage(hitPart, damage);

    // 检查部位是否被破坏（HP≤0）
    if (gene.ShadowHP.IsDestroyed(hitPart))
    {
        // 调用部位破坏处理器
        gene.PartDestruction?.Handle(pawn, hitPart);
    }

    return true;
}
```

### 4.5 修改：Gene_TrionGland

添加 `PartDestructionHandler` 实例：

```csharp
public class Gene_TrionGland : Gene
{
    // 影子HP追踪器
    private ShadowHPTracker shadowHPTracker;

    // 部位破坏处理器（新增）
    private PartDestructionHandler partDestructionHandler;

    /// <summary>
    /// 部位破坏处理器（公开访问）。
    /// 供伤害处理系统使用。
    /// </summary>
    public PartDestructionHandler PartDestruction => partDestructionHandler;

    public override void PostAdd()
    {
        base.PostAdd();

        // ... 其他初始化 ...

        // 初始化影子HP追踪器
        shadowHPTracker = new ShadowHPTracker();

        // 初始化部位破坏处理器（新增）
        partDestructionHandler = new PartDestructionHandler();
    }

    public override void ExposeData()
    {
        base.ExposeData();

        // ... 其他序列化 ...

        // 读档后重新初始化partDestructionHandler（不序列化）
        if (Scribe.mode == LoadSaveMode.PostLoadInit && partDestructionHandler == null)
        {
            partDestructionHandler = new PartDestructionHandler();
        }
    }
}
```

### 4.6 修改：CombatBodyOrchestrator.Deactivate()

在战斗体解除时清理已破坏部位并恢复：

```csharp
public void Deactivate(Pawn pawn, Gene_TrionGland gene, ...)
{
    // ... 其他解除逻辑 ...

    // 清理影子HP
    gene.ShadowHP?.Clear();

    // 清理部位破坏记录并恢复部位（新增）
    gene.PartDestruction?.Clear(pawn);

    // ... 其他解除逻辑 ...
}
```

## 五、测试验证

### 5.1 测试场景

1. **非关键部位破坏**
   - 激活战斗体
   - 攻击手臂直到影子HP耗尽
   - 预期：手臂失效（无疼痛/流血），战斗体继续运行

2. **关键部位破坏**
   - 激活战斗体
   - 攻击头部直到影子HP耗尽
   - 预期：头部失效 + 战斗体破裂

3. **重复伤害**
   - 部位已破坏后继续攻击
   - 预期：不重复标记，日志显示"已被破坏，跳过"

4. **战斗体解除恢复**
   - 部位破坏后解除战斗体
   - 预期：破坏 Hediff 被移除，部位恢复正常

5. **无疼痛/流血验证**
   - 部位破坏后检查 Pawn 状态
   - 预期：无疼痛 Hediff，无流血效果

### 5.2 日志输出示例

```
[BDP] ── 伤害拦截 ──────────────────────────────────────────
[BDP]   目标: 殖民者A  伤害类型: Bullet  伤害量: 15.0 (护甲后)
[BDP]   受伤部位: LeftArm
[BDP]   处理前 → Trion: 100.0  部位影子HP: 20.0
[BDP]   处理后 → Trion: 85.0 (消耗: 15.0)  部位影子HP: 5.0
[BDP] ────────────────────────────────────────────────────

[BDP] ── 伤害拦截 ──────────────────────────────────────────
[BDP]   目标: 殖民者A  伤害类型: Bullet  伤害量: 10.0 (护甲后)
[BDP]   受伤部位: LeftArm
[BDP]   处理前 → Trion: 85.0  部位影子HP: 5.0
[BDP]   ⚠ 影子部位被毁: LeftArm → 真身部位已失效（无疼痛/流血）
[BDP]   处理后 → Trion: 75.0 (消耗: 10.0)  部位影子HP: 0.0
[BDP] ────────────────────────────────────────────────────

[解除战斗体]
[BDP] 战斗体解除：恢复部位 LeftArm
```

## 六、注意事项

### 6.1 自定义 Hediff 优势
- **完全控制副作用**：只保留能力丧失，不产生疼痛/流血
- **可恢复性**：战斗体解除时移除 Hediff，部位恢复正常
- **与原版区分**：不与原版 `MissingBodyPart` 混淆
- **战斗体特有机制**：体现战斗体保护特性

### 6.2 性能考虑
- `HashSet<string>` 查询复杂度 O(1)
- `List<Hediff>` 存储引用，解除时批量移除
- 每次伤害只检查一次，无性能问题
- 战斗体解除时清理，避免内存泄漏

### 6.3 边界情况
- 部位已被原版伤害移除：自定义 Hediff 仍可添加（不冲突）
- 部位不存在于身体定义：`AddHediff` 会忽略
- 战斗体解除后再激活：`Clear()` 确保状态重置
- 读档兼容性：不序列化 Handler，激活时重新初始化

## 七、后续扩展

### 7.1 可选功能
- 部位破坏时播放特效/音效
- 部位破坏时显示浮动文字
- 部位破坏统计（UI显示）

### 7.2 高级功能
- 部位"濒危"预警（HP<20%时提示）
- 部位修复机制（消耗Trion恢复影子HP）
- 部位破坏惩罚（Trion消耗增加）

---

## 历史记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.1 | 2026-03-04 | 反映实际实现状态。系统已完整实现：自定义HediffDef（BDP_CombatBodyPartDestroyed）标记部位失效，无疼痛/流血。PartDestructionHandler管理已破坏部位集合，战斗体解除时恢复部位。ShadowHPHandler检测部位破坏并调用PartDestructionHandler。Gene_TrionGland持有PartDestructionHandler实例。所有组件已实现并测试通过。 | Claude Sonnet 4.6 |
| v1.0 | 2026-03-04 | 初始设计文档创建。定义需求、架构设计、实现细节。 | Claude Sonnet 4.6 |
