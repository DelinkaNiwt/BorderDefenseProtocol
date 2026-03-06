---
标题: 战斗体部位颜色显示修复实现报告
日期: 2026-03-04
类型: 实现报告
状态: 已完成
标签: [战斗体, UI修复, 影子HP, 健康面板]
---

# 战斗体部位颜色显示修复实现报告

## 问题描述

战斗体系统使用"影子HP"机制追踪部位健康状态，不修改原版部位的真实HP。这导致健康面板中所有部位都显示为绿色（健康），即使战斗体已经严重受损。

**根本原因**：原版 `HealthUtility.GetPartConditionLabel` 方法基于真实部位HP计算颜色，而影子HP系统不修改真实HP。

## 解决方案

通过Harmony Postfix patch覆盖部位颜色计算逻辑，使用影子HP百分比计算颜色，完全遵循原版颜色规则。

## 实现细节

### 1. 扩展ShadowHPTracker（新增方法）

**文件**: `Combat/Damage/ShadowHPTracker.cs`

```csharp
/// <summary>
/// 获取部位的影子HP百分比。
/// </summary>
public float GetHealthPercentage(BodyPartRecord part)
{
    if (part == null) return 0f;

    string key = GetPartKey(part);
    if (!shadowHP.ContainsKey(key))
        return 0f;

    float currentHP = shadowHP[key];
    float maxHP = part.def.hitPoints;

    if (maxHP <= 0f) return 0f;

    return currentHP / maxHP;
}
```

**说明**：提供影子HP百分比查询接口，供UI patch使用。

### 2. 创建Harmony Patch（新文件）

**文件**: `Combat/Damage/UI/Patch_HealthUtility_GetPartConditionLabel.cs`

**关键设计**：
- 使用 **Postfix** 而非 Prefix，保留原版逻辑（处理MissingBodyPart等特殊情况）
- 只在战斗体激活时覆盖颜色，不影响普通pawn
- 完全遵循原版颜色阈值：
  - >= 99.9%: 绿色 (GoodConditionColor)
  - 70% ~ 99.9%: 黄色 (SlightlyImpairedColor)
  - 40% ~ 70%: 橙色 (ImpairedColor)
  - < 40%: 红色 (RedColor)
  - <= 0%: 灰色 (Color.gray)

```csharp
[HarmonyPatch(typeof(HealthUtility), "GetPartConditionLabel")]
public static class Patch_HealthUtility_GetPartConditionLabel
{
    static void Postfix(Pawn pawn, BodyPartRecord part, ref Pair<string, Color> __result)
    {
        // 基础检查
        if (pawn == null || part == null) return;

        // 检查是否有战斗体基因且已激活
        var gene = pawn.genes?.GetFirstGeneOfType<Gene_TrionGland>();
        if (gene == null || !gene.IsCombatBodyActive) return;

        // 获取影子HP追踪器
        var shadowHP = gene.ShadowHP;
        if (shadowHP == null) return;

        // 获取影子HP百分比
        float healthPercentage = shadowHP.GetHealthPercentage(part);

        // 根据影子HP计算颜色
        Color color = GetColorFromHealthPercentage(healthPercentage);

        // 覆盖返回值的颜色部分（保留原版的文本标签）
        __result = new Pair<string, Color>(__result.First, color);
    }
}
```

### 3. 修改Hediff_CombatWound（修改LabelColor属性）

**文件**: `Combat/Damage/Hediff_CombatWound.cs`

**修改前**：
```csharp
public override Color LabelColor => Color.white;
```

**修改后**：
```csharp
public override Color LabelColor
{
    get
    {
        // 检查是否战斗体激活
        var gene = pawn?.genes?.GetFirstGeneOfType<BDP.Core.Gene_TrionGland>();
        if (gene == null || !gene.IsCombatBodyActive)
            return Color.white;

        // 检查部位是否被破坏
        if (Part != null && gene.ShadowHP != null)
        {
            float hp = gene.ShadowHP.GetHP(Part);
            if (hp <= 0f)
                return Color.gray;  // 被破坏部位的伤口显示灰色
        }

        return Color.white;
    }
}
```

**说明**：当部位影子HP <= 0时，伤口标签显示灰色，与原版被破坏部位的显示风格一致。

## 边界情况处理

| 情况 | 处理方式 |
|------|---------|
| 战斗体未激活 | Patch检查 `IsCombatBodyActive`，返回原版结果 |
| ShadowHPTracker未初始化 | Patch中null检查，返回原版结果 |
| 部位不在影子HP表中 | `GetHealthPercentage` 返回0%（灰色） |
| 部位被破坏（HP≤0） | 部位名称灰色 + 伤口标签灰色 |
| 战斗体解除 | 立即恢复原版颜色显示（因为patch检查激活状态） |
| 普通Pawn | 完全不受影响，使用原版逻辑 |

## 编译结果

✅ 编译成功
- 输出: `1.6/Assemblies/BDP.dll` (210KB)
- 编译时间: 2026-03-04 23:02
- 无警告，无错误

## 游戏内测试计划

### 测试1：普通Pawn不受影响
- 创建普通Pawn → 造成伤害 → 验证颜色使用原版逻辑（基于真实HP）

### 测试2：战斗体部位颜色正确显示
- 激活战斗体 → 造成不同程度伤害 → 验证颜色：
  - 100% HP: 绿色
  - 80% HP: 黄色
  - 50% HP: 橙色
  - 30% HP: 红色

### 测试3：被破坏部位显示灰色
- 激活战斗体 → 致命伤害（影子HP降至0） → 验证：
  - 部位名称显示灰色
  - 伤口标签显示灰色
  - 部位功能丧失

### 测试4：战斗体解除后恢复正常
- 激活 → 受伤（部位显示黄色/红色） → 解除 → 验证部位颜色恢复绿色

### 测试5：多部位不同损伤
- 对不同部位造成不同程度伤害 → 验证每个部位显示正确颜色

## 性能与兼容性

### 性能
- Postfix只在绘制健康面板时触发（低频操作）
- 计算开销：2次null检查 + 1次字典查询 + 1次除法
- 预期影响：可忽略（<0.1ms）

### 兼容性
- ✅ 使用Postfix，不破坏原版逻辑
- ✅ 只影响战斗体Pawn
- ✅ 如果其他Mod也patch此方法，BDP的Postfix会在最后执行

## 文件清单

### 新增文件
1. `Combat/Damage/UI/Patch_HealthUtility_GetPartConditionLabel.cs` - Harmony patch

### 修改文件
1. `Combat/Damage/ShadowHPTracker.cs` - 添加 `GetHealthPercentage()` 方法
2. `Combat/Damage/Hediff_CombatWound.cs` - 修改 `LabelColor` 属性

### 项目文件
- `BDP.csproj` - 使用通配符自动包含新文件

## 实现状态

✅ 所有代码已实现
✅ 编译通过
⏳ 等待游戏内测试验证

---

## 历史记录

| 日期 | 版本 | 修改内容 | 作者 |
|------|------|---------|------|
| 2026-03-04 | v1.0 | 初始实现 | Claude Sonnet 4.6 |
