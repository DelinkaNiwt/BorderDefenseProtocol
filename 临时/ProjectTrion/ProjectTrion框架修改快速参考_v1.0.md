# ProjectTrion Framework 修改快速参考

## 🎯 一句话总结

框架缺少 3 个 API（SetReserved、ActivateComponent、DeactivateComponent），且硬编码了应用层的 Snapshot 类。修改这 3 处后，框架的架构将从 **75% 完善**上升到 **90% 完善**。

---

## 📋 三处修改速查表

### 修改 1：ILifecycleStrategy.cs - 添加虚函数

| 项目 | 内容 |
|------|------|
| **文件** | `ILifecycleStrategy.cs` |
| **位置** | 第一个方法前（TalentGrade 枚举之后） |
| **操作** | 添加新虚函数 |
| **代码量** | +10 行 |
| **是什么** | `CombatBodySnapshot CreateSnapshot(CompTrion comp);` |
| **为什么** | 让应用层决定使用哪个 Snapshot 类，框架不硬编码 |
| **应用层动作** | 在 Strategy 中实现此虚函数 |

---

### 修改 2：CompTrion.cs - 三项操作

#### 2.1 删除 TrionCombatBodySnapshot 类

| 项目 | 内容 |
|------|------|
| **文件** | `CompTrion.cs` |
| **位置** | 第 27-63 行 |
| **操作** | 完全删除（37 行） |
| **原因** | 这是应用层的类，不应该在框架中 |
| **迁移** | 将这个类移到应用层 Strategy 实现中 |

#### 2.2 添加三个 API 方法

| 项目 | 内容 |
|------|------|
| **文件** | `CompTrion.cs` |
| **位置** | PostSpawnSetup() 方法之前（第 188 行前） |
| **操作** | 添加三个公开方法 |
| **代码量** | +95 行 |
| **新方法** | `SetReserved(float amount)` |
| **新方法** | `ActivateComponent(TriggerMount mount)` |
| **新方法** | `DeactivateComponent(TriggerMount mount)` |
| **为什么** | 框架提供清晰的 API 让应用层调用 |

#### 2.3 修改 GenerateCombatBody() 方法

| 项目 | 内容 |
|------|------|
| **文件** | `CompTrion.cs` |
| **位置** | GenerateCombatBody() 方法内（第 503 行前后） |
| **操作** | 修改快照创建逻辑 |
| **修改前** | `_snapshot = new TrionCombatBodySnapshot();` ❌ |
| **修改后** | `_snapshot = _strategy?.CreateSnapshot() ?? new CombatBodySnapshot();` ✅ |
| **代码量** | 修改 ~5 行 |
| **为什么** | 让 Strategy 决定，而不是框架硬编码 |

---

## ✅ 修改完成度检查表

### 框架修改

- [ ] ILifecycleStrategy.cs - 添加 CreateSnapshot() 虚函数
- [ ] CompTrion.cs - 删除 TrionCombatBodySnapshot 类（第 27-63 行）
- [ ] CompTrion.cs - 添加 SetReserved() 方法
- [ ] CompTrion.cs - 添加 ActivateComponent() 方法
- [ ] CompTrion.cs - 添加 DeactivateComponent() 方法
- [ ] CompTrion.cs - 修改 GenerateCombatBody() 改为调用 Strategy.CreateSnapshot()
- [ ] 框架代码编译无错误 ✅

### 应用层修改

- [ ] 在 Strategy 实现中新增 CreateSnapshot() 虚函数实现
- [ ] 将 TrionCombatBodySnapshot 类定义移到应用层
- [ ] 在 Strategy.OnCombatBodyGenerated() 中调用 comp.SetReserved()
- [ ] （可选）在组件激活逻辑中使用 comp.ActivateComponent()
- [ ] 应用层代码编译无错误 ✅

---

## 📌 关键代码片段速查

### 框架：SetReserved() 实现

```csharp
public void SetReserved(float amount)
{
    _reserved = Mathf.Clamp(amount, 0, _capacity);
    ValidateDataConsistency();

    if (_reserved + _consumed > _capacity)
    {
        Log.Warning($"Reserved + Consumed > Capacity");
    }
}
```

### 框架：GenerateCombatBody() 修改

```csharp
public void GenerateCombatBody()
{
    // ...

    // 修改这一行：
    _snapshot = _strategy?.CreateSnapshot(this) ?? new CombatBodySnapshot();

    // ...
}
```

### 应用层：Strategy 实现

```csharp
public override CombatBodySnapshot CreateSnapshot(CompTrion comp)
{
    return new TrionCombatBodySnapshot();
}

public override void OnCombatBodyGenerated(CompTrion comp)
{
    float totalReserved = 0;
    foreach (var mount in comp.Mounts)
    {
        totalReserved += mount.GetReservedCost();
    }
    comp.SetReserved(totalReserved);  // 调用新 API
}
```

---

## 🎓 理解修改的意义

### 修改前 ❌

```
框架硬编码应用层的类
↓
只能一个特定应用用
↓
无法复用框架
↓
架构设计评分：75%
```

### 修改后 ✅

```
框架只提供接口和 API
↓
应用层实现具体逻辑
↓
框架可被多个应用复用
↓
架构设计评分：90%
```

---

## ⚡ 最快的修改方式

### 方式 1：逐行复制粘贴（保险）

1. 打开 `ProjectTrion框架代码修改方案_v1.0.md`
2. 按照"修改 1、修改 2.1、修改 2.2、修改 2.3"的顺序
3. 逐个复制代码片段到对应文件位置
4. 编译并修复应用层

### 方式 2：使用 Git Diff（如果有版本控制）

等待完整的修改补丁文件

---

## ⚠️ 常见错误

### 错误 1：忘记删除 TrionCombatBodySnapshot 类

**症状**：编译时有 "class already defined" 或重复定义错误

**修复**：确保第 27-63 行完全删除了

### 错误 2：GenerateCombatBody() 中仍然创建 TrionCombatBodySnapshot

**症状**：编译时 "TrionCombatBodySnapshot 不存在"

**修复**：确保改为 `_strategy?.CreateSnapshot()`

### 错误 3：应用层 Strategy 没有实现 CreateSnapshot()

**症状**：编译警告 "不能实例化抽象成员"

**修复**：在 Strategy 实现中添加 `public override CombatBodySnapshot CreateSnapshot(CompTrion comp)`

### 错误 4：应用层的 TrionCombatBodySnapshot 定义在错误的地方

**症状**：应用层编译错误

**修复**：确保 TrionCombatBodySnapshot 在应用层的 Strategy 文件中，不在框架中

---

## 📊 修改的影响范围

| 维度 | 影响 |
|------|------|
| 编译 | 框架需改，应用层需改 |
| 运行时 | 无破坏性改动，完全向后兼容 |
| 游戏行为 | 无变化，只是重新组织了代码 |
| 性能 | 无变化 |
| 存档兼容性 | ✅ 完全兼容（只是 API 改变） |

---

## 🚀 修改后能做什么

### 修改前

```csharp
// ❌ 无法设置占用值
comp.Reserved = 100;  // 编译错误

// ❌ 无法统一激活组件
mount.IsActive = true;  // 太简陋
comp.Consume(cost);     // 分散的逻辑
```

### 修改后

```csharp
// ✅ 有清晰的 API
comp.SetReserved(100);

// ✅ 有统一的激活 API
comp.ActivateComponent(mount);  // 框架处理所有细节
```

---

## 📞 如果有问题

1. **编译错误** → 检查是否完整删除了 TrionCombatBodySnapshot 类
2. **虚函数未实现** → 应用层 Strategy 需要实现 CreateSnapshot()
3. **逻辑错误** → 检查应用层是否在 OnCombatBodyGenerated() 中调用了 SetReserved()
4. **数据不一致** → 检查 SetReserved() 是否被正确调用

---

**📍 Knowledge-Refiner**
*快速参考 v1.0*
