# 阶段0前置验证报告

**日期**: 2026-03-02
**测试对象**: Pawn "Tauno"
**验证工具**: `CombatBodyDebugActions.cs`

---

## 一、验证任务1：Hediff快照回滚

### ✅ 基本功能验证通过

**测试场景**：
- 测试1：3个Hediff（AmbrosiaTolerance, BionicStomach, PsychicAmplifier）
- 测试2：5个Hediff（上述3个 + Crack, Bruise）

**验证结果**：
- ✅ Hediff数量完全一致（移除→恢复后无丢失）
- ✅ `RemoveHediff`/`AddHediff`未触发额外游戏消息
- ✅ `DirtyCache()`正常工作
- ✅ BodyPart索引查找逻辑正确

### ⚠️ 发现的关键问题

#### 问题1：`Hediff_Level`的Severity被重置

**现象**：
- PsychicAmplifier的severity从2.0/4.0重置为1.0
- 灵能等级在游戏UI中显示为1级

**根本原因**：
```csharp
// Verse.Hediff_Level.cs
public class Hediff_Level : HediffWithComps
{
    public int level = 1;  // ← 默认值是1

    public override void TickInterval(int delta)
    {
        Severity = level;  // ← Severity会被level覆盖！
    }
}
```

**影响范围**：
- `Hediff_Level`及其子类（PsychicAmplifier等）
- 所有使用`level`字段而非`Severity`的Hediff

**解决方案**：
1. 检测Hediff类型，对`Hediff_Level`使用反射读取/设置`level`字段
2. 或者在快照时记录`level`字段，恢复时设置`level`而非`Severity`

---

## 二、验证任务2：容器转移顺序

### ✅ 基本功能验证通过

**测试场景**：
- 衣物：3件（包含护盾背包）
- 物品：2件（Leather_Human, CochlearImplant）

**验证结果**：
- ✅ 数量完全一致（转移→恢复后无丢失）
- ✅ `holdingOwner`状态转换正确
  - Remove后：`null`
  - TryAdd/Wear后：`ThingOwner`
- ✅ 转移顺序正确：
  - 衣物：`Remove()` → `TryAdd()` → `Remove()` → `Wear()`
  - 物品：`TryTransferToContainer()`

### ⚠️ 发现的关键问题

#### 问题2：装备的"强制穿戴"标记丢失

**现象**：
- 护盾背包的"强制穿戴"状态被取消
- 恢复后可以手动脱下（原本被锁定）

**可能原因**：
- `Apparel`有额外的状态字段（如`wornByCorpseInt`或强制标记）
- `Remove()`/`Wear()`过程中，这些状态未被保留

**需要调查的字段**：
```csharp
// 可能的Apparel状态字段
- wornByCorpseInt
- forcedHandler（如果存在）
- 其他标记字段
```

---

## 三、副作用清单

### 已确认的副作用

| 副作用类型 | 严重程度 | 影响范围 | 解决方案 |
|-----------|---------|---------|---------|
| **`MissingBodyPart`循环依赖** | 🔴🔴 致命 | 所有截肢/缺失部位 | 只恢复顶层缺失部位 |
| `Hediff_Level.level`字段未恢复 | 🔴 高 | 所有Level类Hediff | 反射读写`level`字段 |
| 装备"强制穿戴"标记丢失 | 🟡 中 | 特殊装备状态 | 记录并恢复额外状态 |

### 详细说明

#### 🔴🔴 致命问题：MissingBodyPart循环依赖

**测试场景**（Pawn: Elekio）：
- 右腿截肢，安装超凡腿
- 原始Hediff：Leg、Femur、Tibia、Foot、Toe(×5)都有`MissingBodyPart`

**错误现象**：
```
Tried to add health diff to missing part BodyPartRecord(Femur parts.Count=0)
Tried to add health diff to missing part BodyPartRecord(Tibia parts.Count=0)
Tried to add health diff to missing part BodyPartRecord(Foot parts.Count=5)
Tried to add health diff to missing part BodyPartRecord(Toe parts.Count=0) (×4次)
```

**根本原因**：
1. 当部位被截肢时，该部位及其所有子部位都会被标记为`MissingBodyPart`
2. 子部位实际上已经"不存在"（`parts.Count=0`）
3. 不能在"不存在的部位"上添加Hediff

**解决方案**：
- 快照时：只记录**顶层缺失部位**（parent不缺失的部位）
- 恢复时：只恢复顶层缺失部位，子部位会自动缺失
- 实现逻辑：
  ```csharp
  // 检查是否为顶层缺失部位
  bool IsTopLevelMissing(Hediff hediff) {
      if (hediff.def != HediffDefOf.MissingBodyPart) return true;
      if (hediff.Part?.parent == null) return true;

      // 检查父部位是否也缺失
      var parentMissing = pawn.health.hediffSet.hediffs
          .Any(h => h.def == HediffDefOf.MissingBodyPart && h.Part == hediff.Part.parent);
      return !parentMissing;
  }
  ```

### 未观察到的副作用（需持续监控）

- ❓ Hediff的HediffComp状态（如免疫进度、倾向性等）
- ❓ 装备的品质、耐久度、颜色等
- ❓ 思想变化（Thought）
- ❓ 技能经验值变化
- ❓ 社交关系变化

---

## 四、改进建议

### 建议1：扩展Hediff快照字段

**当前快照**：
```csharp
class HediffRecord {
    string defName;
    float severity;
    string bodyPartDefName;
    int bodyPartIndex;
}
```

**建议扩展**：
```csharp
class HediffRecord {
    string defName;
    float severity;
    string bodyPartDefName;
    int bodyPartIndex;

    // 新增字段
    int? level;              // 用于Hediff_Level
    bool isPermanent;        // 永久性标记
    Dictionary<string, object> compData;  // HediffComp状态
}
```

### 建议2：扩展装备快照字段

**建议记录**：
```csharp
class ApparelRecord {
    Thing thing;

    // 新增字段
    bool isForcedWear;       // 强制穿戴标记
    int hitPoints;           // 耐久度
    QualityCategory quality; // 品质
    // ... 其他状态
}
```

### 建议3：增加更多测试场景

**推荐测试Pawn配置**：
1. **疾病测试**：感染、瘟疫、疟疾等
2. **成瘾测试**：酒精成瘾、烟草成瘾等
3. **永久伤残**：失明、截肢、疤痕等
4. **装备测试**：
   - 不同品质（劣质、普通、优秀、传奇）
   - 不同耐久度（全新、磨损、破损）
   - 特殊装备（护盾、动力装甲等）

---

## 五、测试记录

### 测试1：Tauno（基础测试）
- **Hediff**: 3个（AmbrosiaTolerance, BionicStomach, PsychicAmplifier）
- **装备**: 3衣物 + 2物品
- **结果**: ✅ 数量一致，⚠️ 灵能等级重置

### 测试2：Tauno（扩展测试）
- **Hediff**: 5个（上述3个 + Crack, Bruise）
- **装备**: 3衣物 + 2物品
- **结果**: ✅ 数量一致，⚠️ 灵能等级重置，⚠️ 强制穿戴标记丢失

### 测试3：Elekio（复杂测试）
- **Hediff**: 16个（枪伤、瘟疫、酒精成瘾、生物心脏、超凡腿、多个缺失部位、灵能放大器等）
- **装备**: 4衣物 + 3物品
- **结果**:
  - ❌ **5个`MissingBodyPart`恢复失败**（致命错误）
  - ⚠️ 灵能等级重置
  - ✅ 装备数量一致

---

## 六、结论

### 阶段0验证结果：❌ 发现致命问题

**通过项**：
- ✅ 基本Hediff数量恢复正确（简单场景）
- ✅ 容器转移无数据丢失
- ✅ 基本流程可行

**致命问题**：
- 🔴🔴 **`MissingBodyPart`循环依赖**：无法恢复截肢部位
- 🔴 `Hediff_Level.level`字段重置：灵能等级丢失
- 🟡 装备额外状态丢失：强制穿戴标记

### 下一步行动

**必须修复（阻塞性问题）**：
1. ✅ 修复`MissingBodyPart`循环依赖（只记录顶层缺失部位）
2. ✅ 修复`Hediff_Level.level`字段恢复（反射读写）

**建议修复（非阻塞）**：
3. 调查装备的"强制穿戴"机制
4. 扩展Hediff快照字段（HediffComp状态）

**验证完成后**：
5. 重新运行所有测试用例
6. 进入阶段1：实现完整的`CombatBodySnapshot`类

---

**报告更新时间**: 2026-03-02 20:30
**验证工具版本**: v0.1
**测试环境**: RimWorld 1.6.4633 + BDP Dev Build
**测试用例**: 3个Pawn，共24个Hediff，10件装备
