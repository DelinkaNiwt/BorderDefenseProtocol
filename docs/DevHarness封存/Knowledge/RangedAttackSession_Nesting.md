# 远程攻击会话层级知识笔记

本文记录 BDP 远程攻击执行链的通用层级名词、顺序关系、嵌套关系与关键字段映射。

## 总名词

`选择会话 + 攻击实例` 合起来称为：

```text
攻击会话
```

含义：

- `选择会话`：玩家从进入瞄准/选点，到确认目标或取消之前的交互阶段。
- `攻击实例`：一次确认后冻结参数，并驱动预热、发射、投射物初始化与后续动作的执行阶段。
- `攻击会话`：一次玩家选择输入与其生成的攻击执行实例的合称。

## 通用层级图

```text
攻击会话
├─ 选择会话
│  ├─ 输入步骤
│  │  ├─ 鼠标/键盘输入帧
│  │  ├─ 当前候选目标
│  │  └─ 模块贡献的选择状态
│  └─ 确认结果
│     ├─ 最终确认目标
│     └─ 冻结前的模块选择参数
└─ 攻击实例
   ├─ 正式请求
   │  ├─ 攻击者
   │  ├─ 表达结果
   │  ├─ 目标
   │  └─ 模块冻结数据
   ├─ 执行计划
   │  ├─ 预热/施放参数
   │  ├─ burst 发射配置
   │  └─ 投射物初始化配置
   ├─ 运行时动作步
   │  ├─ 预热
   │  ├─ 发射窗口
   │  │  ├─ 第 1 发发射载荷
   │  │  │  ├─ 宿主发射计划
   │  │  │  └─ 投射初始化计划
   │  │  ├─ 第 2 发发射载荷
   │  │  │  ├─ 宿主发射计划
   │  │  │  └─ 投射初始化计划
   │  │  └─ 第 3 发发射载荷
   │  │     ├─ 宿主发射计划
   │  │     └─ 投射初始化计划
   │  └─ 后续 continuation
   │     └─ 下一轮 burst 或结束
   └─ 投射物生命周期
      ├─ 初始化
      ├─ 飞行
      └─ 到达
```

## 顺序链

```text
进入选择
→ 玩家输入
→ 确认目标
→ 创建攻击实例
→ 冻结正式请求
→ 生成执行计划
→ 预热
→ 发射 burst
→ 初始化 projectile
→ projectile 飞行
→ projectile 到达
→ continuation 决定下一轮 burst 或结束
```

## 嵌套关系

```text
攻击会话
└─ 攻击实例
   └─ 发射窗口 / burst
      └─ 发射载荷 / emit
         └─ 投射初始化计划
            └─ 投射物 / projectile
```

多轮 burst 时：

```text
攻击实例
├─ burst #1
│  ├─ projectile #1
│  ├─ projectile #2
│  └─ projectile #3
├─ burst #2
│  ├─ projectile #1
│  ├─ projectile #2
│  └─ projectile #3
└─ burst #3
   ├─ projectile #1
   ├─ projectile #2
   └─ projectile #3
```

如果玩家打断动作并重新选择：

```text
攻击会话 #1
├─ 选择会话 #1
└─ 攻击实例 #1
   ├─ burst #1
   └─ burst #2

玩家打断

攻击会话 #2
├─ 选择会话 #2
└─ 攻击实例 #2
   └─ burst #1
```

## 字段映射

```text
选择会话
├─ 输入帧：TargetingInputFrame
├─ 推进判定：TargetingAdvanceDecision / TargetingAdvanceKind
├─ 瞄准参数：TargetingParameters
└─ 确认记录：ConfirmRecord

攻击实例
├─ 表达结果：FormalExpressionResult
├─ 执行入口：DefaultAttackExecutionEntry
├─ 发射装配：RangedBurstEmissionAssembler
├─ 发射载荷：AttackExecutionEmit
├─ 发射记录：FireRecord
├─ 投射初始化计划：ProjectileInitPlan
└─ 投射物：BdpProjectile

续行动作
├─ 续行规划：RangedVerbContinuationPlanner
└─ 下一轮 burst 的继续或结束
```

## 命名约定

- `攻击会话`：选择会话与攻击实例的总称。
- `选择会话`：玩家输入与目标确认阶段。
- `攻击实例`：确认后的一次执行实体。
- `正式请求`：把表达结果、目标、模块冻结数据交给执行链的请求。
- `执行计划`：预热、发射、投射初始化等执行参数的汇总。
- `运行时动作步`：预热、发射窗口、续行等实际动作流程。
- `发射窗口 / burst`：一次 burst 轮次。
- `发射载荷 / emit`：burst 内的一发。
- `投射初始化计划`：一发 projectile 启动前的发射参数。
- `投射物 / projectile`：实际进入地图飞行和到达逻辑的实体。
