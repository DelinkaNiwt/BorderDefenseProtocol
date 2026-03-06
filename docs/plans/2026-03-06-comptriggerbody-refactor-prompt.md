# CompTriggerBody重构任务启动提示词

复制以下内容到新会话开始重构任务：

---

你好！我需要你帮我执行一个代码重构任务。这是一个RimWorld模组（BDP - 边境防卫协议）的CompTriggerBody类的重构工作。

## 任务背景

CompTriggerBody是触发器系统的核心组件，当前代码规模过大（2059行），承担10个不同职责，严重违反单一职责原则。我们需要通过partial class将其拆分为多个文件。

## 关键文档

请先阅读以下两个文档：

1. **重构计划**: `模组工程/BorderDefenseProtocol/docs/plans/2026-03-06-comptriggerbody-refactor-plan.md`
   - 包含完整的重构方案、文件拆分策略、实施步骤、风险控制

2. **任务清单**: `模组工程/BorderDefenseProtocol/docs/plans/2026-03-06-comptriggerbody-refactor-checklist.md`
   - 包含详细的任务清单、验证步骤、回滚策略

## 当前状态

- **当前阶段**: [请查看checklist.md中的"当前阶段"字段]
- **完成进度**: [请查看checklist.md中的完成标记]
- **最后更新**: [请查看checklist.md底部的"最后更新"字段]

## 你需要做什么

1. **读取文档**: 先读取上述两个文档，理解重构方案和当前进度
2. **检查状态**: 查看checklist.md，确认当前处于哪个阶段
3. **执行任务**: 按照checklist.md中未完成的任务逐项执行
4. **更新清单**: 每完成一个任务项，更新checklist.md中的标记（`- [ ]` → `- [x]`）
5. **验证测试**: 每个阶段完成后，执行验证测试
6. **提交代码**: 验证通过后，提交git commit

## 重要约束

1. **存档兼容性**: 所有序列化字段必须保持位置和名称不变
2. **接口实现**: IVerbOwner和ICombatBodySupport必须在主类实现
3. **分阶段执行**: 必须按阶段1→阶段2→阶段3的顺序执行，不可跳过
4. **充分验证**: 每个阶段完成后必须执行完整的验证测试
5. **及时提交**: 每个阶段验证通过后立即提交git commit

## 编译命令

```bash
cd "模组工程/BorderDefenseProtocol/Source/BDP"
dotnet msbuild BDP.csproj -p:Configuration=Debug -t:Build -v:minimal
```

## 关键文件路径

- **主文件**: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/CompTriggerBody.cs`
- **新建文件目录**: `模组工程/BorderDefenseProtocol/Source/BDP/Trigger/Comps/`

## 工作流程

```
1. 读取计划和清单
   ↓
2. 确认当前阶段和进度
   ↓
3. 执行当前阶段的任务
   ↓
4. 编译验证
   ↓
5. 功能测试
   ↓
6. 更新清单（标记完成）
   ↓
7. 提交git commit
   ↓
8. 进入下一阶段（重复2-7）
```

## 遇到问题时

1. **编译失败**: 检查字段引用、方法调用是否正确
2. **测试失败**: 回滚到上一个commit，重新分析问题
3. **存档不兼容**: 立即停止，检查ExposeData是否改变
4. **不确定如何继续**: 询问我，不要盲目修改

## 开始执行

请先读取两个文档，然后告诉我：
1. 当前处于哪个阶段？
2. 该阶段的下一个未完成任务是什么？
3. 你准备如何执行这个任务？

准备好了就开始吧！

---

**注意**: 这是一个长期任务，可能需要多个会话完成。每次会话结束前，请务必：
1. 更新checklist.md中的进度
2. 提交已完成的代码
3. 在checklist.md底部记录当前状态

这样下一个会话可以无缝继续。
