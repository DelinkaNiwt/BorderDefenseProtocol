# ProjectTrion Framework 评审与改进指南

## 📚 文档导航

本目录包含对 ProjectTrion Framework 的完整评审和改进方案。请按以下顺序阅读：

### 1. **快速了解** (5 分钟)
→ 阅读：`ProjectTrion框架修改快速参考_v1.0.md`
- 三处修改的速查表
- 关键代码片段
- 常见错误列表

### 2. **深入理解** (20 分钟)
→ 阅读：`ProjectTrion框架架构评审报告_v1.0.md`
- 框架设计的优点分析
- 三个问题的详细分析
- 修改方案和目标说明
- 完整的实现步骤指南

### 3. **代码实现** (30 分钟)
→ 参考：`ProjectTrion框架代码修改方案_v1.0.md`
- 每处修改的完整代码
- 精确的修改位置
- 修改前后的对比
- 应用层需要做的调整

---

## 🎯 核心发现

### 框架整体评分

| 评分 | 描述 |
|------|------|
| **修改前** | 75% ✅ 架构清晰，设计良好 |
| **修改后** | 90% ✅ 边界明确，API 完整 |

### 主要问题

| # | 问题 | 严重程度 | 修复难度 |
|---|------|--------|--------|
| 1 | 缺少 SetReserved() API | 🔴 高 | 🟢 简单 |
| 2 | TrionCombatBodySnapshot 硬编码 | 🟡 中 | 🟡 中等 |
| 3 | 缺少组件激活 API | 🟡 中 | 🟢 简单 |

---

## 💡 为什么要做这些修改

### 问题 1：缺少 SetReserved() API

**现象**：应用层无法设置占用值

```csharp
// 应用层想这样做，但不行：
comp.Reserved = 100;  // ❌ 编译错误，setter 是 private
```

**修复**：添加公开的 SetReserved() 方法

```csharp
comp.SetReserved(100);  // ✅ 现在可以了
```

**目标**：应用层能正确管理 Reserved 占用值

---

### 问题 2：TrionCombatBodySnapshot 硬编码在框架中

**现象**：框架依赖应用层的类

```csharp
// CompTrion.cs 中定义应用层的类
private class TrionCombatBodySnapshot : CombatBodySnapshot  // ❌ 不应该在框架中
{
    // 应用层的规则
}

_snapshot = new TrionCombatBodySnapshot();  // ❌ 硬编码
```

**问题**：
- 框架代码中含有应用层的逻辑
- 不同应用需要不同的过滤规则，无法复用框架
- 框架-应用层的边界混淆

**修复**：通过 Strategy 让应用层决定

```csharp
// Framework
_snapshot = _strategy?.CreateSnapshot() ?? new CombatBodySnapshot();

// Application
public override CombatBodySnapshot CreateSnapshot(CompTrion comp)
{
    return new TrionCombatBodySnapshot();  // 应用层决定
}
```

**目标**：框架保持纯净，不知道应用层的具体类

---

### 问题 3：缺少组件激活 API

**现象**：没有统一的组件激活接口

```csharp
// 应用层的"绕过"方式（不好）
if (comp.Available >= mount.GetActivationCost())
{
    comp.Consume(mount.GetActivationCost());
    mount.IsActive = true;
}
```

**问题**：
- 每个地方都要重复这个逻辑
- 验证分散在应用层
- 框架无法保证激活的一致性

**修复**：添加统一的 API

```csharp
comp.ActivateComponent(mount);  // ✅ 框架处理所有细节
```

**目标**：应用层有清晰的组件激活接口

---

## 📊 修改的影响

### 代码改动

```
框架文件修改：2 个
  - ILifecycleStrategy.cs: +10 行
  - CompTrion.cs: +95 行, -37 行, ~5 行改

应用层工作：
  - 迁移 TrionCombatBodySnapshot 类
  - 实现 CreateSnapshot() 虚函数
  - 调用 SetReserved() API
  - （可选）使用 ActivateComponent() API
```

### 影响范围

| 方面 | 影响 | 说明 |
|------|------|------|
| 编译 | ⚠️ 需要修改 | 框架和应用层都需要改 |
| 运行时 | ✅ 无影响 | 纯粹的代码重组织 |
| 游戏行为 | ✅ 无影响 | 逻辑不变 |
| 性能 | ✅ 无影响 | 无额外开销 |
| 存档兼容 | ✅ 完全兼容 | 只是 API 改变 |

---

## 🚀 快速开始

### 第 1 步：理解修改
阅读 `ProjectTrion框架修改快速参考_v1.0.md`（5 分钟）

### 第 2 步：理解原因
阅读 `ProjectTrion框架架构评审报告_v1.0.md` 中的"问题分析"部分（10 分钟）

### 第 3 步：执行修改
参照 `ProjectTrion框架代码修改方案_v1.0.md`：
1. 修改 ILifecycleStrategy.cs
2. 删除 CompTrion.cs 中的 TrionCombatBodySnapshot 类
3. 添加 SetReserved()、ActivateComponent()、DeactivateComponent() 方法
4. 修改 GenerateCombatBody() 方法

### 第 4 步：应用层调整
在应用层 Strategy 实现中：
1. 实现 CreateSnapshot() 虚函数
2. 迁移 TrionCombatBodySnapshot 类
3. 在 OnCombatBodyGenerated() 中调用 SetReserved()

### 第 5 步：编译和测试
1. 框架编译无错误
2. 应用层编译无错误
3. 运行游戏验证功能正常

---

## ✅ 修改清单

### Framework 修改
- [ ] ILifecycleStrategy.cs - 添加 CreateSnapshot() 虚函数
- [ ] CompTrion.cs - 删除 TrionCombatBodySnapshot 类（第 27-63 行）
- [ ] CompTrion.cs - 添加 SetReserved() 方法
- [ ] CompTrion.cs - 添加 ActivateComponent() 方法
- [ ] CompTrion.cs - 添加 DeactivateComponent() 方法
- [ ] CompTrion.cs - 修改 GenerateCombatBody() 方法
- [ ] **框架编译通过**

### Application 修改
- [ ] Strategy 实现 CreateSnapshot() 虚函数
- [ ] 迁移 TrionCombatBodySnapshot 类到应用层
- [ ] OnCombatBodyGenerated() 中调用 SetReserved()
- [ ] 根据需要使用 ActivateComponent()
- [ ] **应用编译通过**

### 验证
- [ ] 战斗体生成时占用值正确设置
- [ ] 快照和恢复流程工作正常
- [ ] 组件激活和停用工作正常
- [ ] 数据一致性验证有效

---

## 📖 文档说明

### ProjectTrion框架架构评审报告_v1.0.md
**长度**：~600 行 | **阅读时间**：20-30 分钟

内容：
- 框架设计的优点（6 个）
- 框架的问题（3 个，详细分析）
- 修改方案和目标
- 完整的实现步骤
- 修改影响分析
- 反向兼容性评估

**适合**：深入理解框架架构，了解为什么要做这些修改

---

### ProjectTrion框架代码修改方案_v1.0.md
**长度**：~400 行 | **阅读时间**：15-20 分钟

内容：
- 每处修改的完整代码片段
- 精确的修改位置（行号）
- 修改前后的对比
- 应用层需要做什么
- 验证清单

**适合**：执行具体修改，逐行复制粘贴

---

### ProjectTrion框架修改快速参考_v1.0.md
**长度**：~300 行 | **阅读时间**：5-10 分钟

内容：
- 三处修改的速查表
- 完成度检查表
- 关键代码片段
- 常见错误和修复
- 快速修改指南

**适合**：快速查阅，修改时参考

---

## 🎓 学习路径

### 如果你是框架开发者

1. 阅读：架构评审报告（完整理解）
2. 参考：代码修改方案（具体实现）
3. 执行：修改框架代码
4. 验证：编译和单元测试

### 如果你是应用层开发者

1. 阅读：快速参考（了解应用层任务）
2. 参考：代码修改方案中的"应用层需要做的修改"
3. 执行：实现新虚函数，迁移类，调用新 API
4. 验证：集成测试

### 如果你只想快速了解

1. 阅读：快速参考（5 分钟）
2. 了解：三处修改是什么、为什么、怎么改

---

## 🔗 相关文件

框架源代码位置：
```
C:\NiwtDatas\Projects\RimworldModStudio\模组工程\ProjectTrion_Framework\Source\ProjectTrion\
├── Core\
│   ├── ILifecycleStrategy.cs      ← 修改 1：添加虚函数
│   ├── CombatBodySnapshot.cs
│   └── HediffSnapshot.cs
├── Components\
│   ├── CompTrion.cs               ← 修改 2：主要修改
│   ├── CompProperties_Trion.cs
│   ├── TriggerMount.cs
│   └── TriggerMountDef.cs
└── ...
```

---

## 📝 版本历史

| 版本 | 时间 | 说明 |
|------|------|------|
| 1.0 | 2026-01-16 | 初版：完整的评审、方案、参考 |

---

## ❓ 常见问题

### Q: 为什么要修改框架，应用层不能直接用吗？

A: 可以，但是：
1. Reserved setter 是 private，应用层无法调用
2. 框架硬编码了应用层的类，违反了设计原则
3. 没有统一的组件激活 API，应用层要重复代码

修改后，框架的 API 更清晰、更易用、可复用性更高。

---

### Q: 修改会破坏兼容性吗？

A: 不会。这些都是：
- 添加新 API（向后兼容）
- 删除私有类（框架内部）
- 修改内部实现（无外部影响）

现有的应用代码可以继续工作，只需在新调用点使用新 API。

---

### Q: 应用层要改哪些地方？

A: 三处：
1. Strategy 实现 CreateSnapshot() 虚函数
2. 迁移 TrionCombatBodySnapshot 类
3. 在 OnCombatBodyGenerated() 中调用 SetReserved()

通常总工作量不超过 50 行代码。

---

### Q: 修改后游戏行为会改变吗？

A: 不会。这只是：
- 代码的重新组织
- API 的优化
- 框架-应用层的边界调整

游戏逻辑、数据处理、效果表现都不变。

---

## 📞 反馈与建议

如有任何问题或建议，请：
1. 检查快速参考中的"常见错误"部分
2. 重新阅读相关分析部分
3. 按步骤验证修改是否完整

---

**📍 Framework Architecture Refiner**
*ProjectTrion Framework Quality Improvement Guide v1.0*
*2026-01-16*
