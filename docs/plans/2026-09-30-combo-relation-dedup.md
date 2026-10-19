# 组合关系详情去重 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 同一详情层级中，相同 ComboDef 的组合关系只显示一条。

**Architecture:** 仅在 `ChipInformationDetailRenderer.BuildRelationLines` 去重，键为解析后的 `combo.defName`。每次调用有自己的已见集合，所以根、形态和姿态层级互不影响；信息模型和关系投影保持原样。

**Tech Stack:** C#、BDP.Content、RimWorld Verse UI。

---

### Task 1：详情关系行去重

**Files:**
- Modify: `模组工程/BorderDefenseProtocol/Source/BDP.Content/Assembly/Window/ChipInformationDetailRenderer.cs`
- Modify: `日志/Agent工作日志/Agent工作日志-20260930-BDP组合功能语义插入与翻译审查.md`

**Step 1：在 `BuildRelationLines` 建立调用局部的 ComboDef 键集合**

使用 `HashSet<string>(StringComparer.Ordinal)`，放在处理关系节点的循环之前。

**Step 2：每个关系解析出有效 ComboDef 后按 `combo.defName` 去重**

名称或说明无效的行继续跳过；有效定义若已在本次调用出现则跳过，否则加入集合并创建一条 `RelationLine`。保留第一个关系节点的展开键。

**Step 3：更新工作日志**

记录显示层去重规则、信息模型保持原样和验证结果。

**Step 4：验证**

运行 BDP.Content Release 构建和 `git diff --check`。按仓库要求不新增或运行测试。

**Step 5：提交**

仅暂存本计划、渲染器和本次工作日志，提交关系详情去重改动。
