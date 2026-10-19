# 自施放能力预热视觉根因修正实施计划

## 1. 固定失败边界

- 更新 `ChameleonSelfCastVisualBoundarySmokeTests.ps1`，要求自施放 Verb 使用专用前摇姿态。
- 要求该姿态继承原版 `Stance_Warmup`、设置 `neverAimWeapon`、覆盖 `StanceDraw()`，并禁止重新调用 `DrawAimPie`。

## 2. 实现通用分支

- 新增 `BdpStance_SelfCastWarmup.cs`。
- 在 `BdpVerb_CastSelfAbility` 中将自施放前摇姿态替换为该通用姿态。
- 用原版公开 `GenDraw.DrawCooldownCircle` 实现中心圆形剩余进度显示；保留读档后的进度上限。

## 3. 验证与交付

- 运行变色龙视觉、定义、攻击路由/关闭及能力生命周期相关冒烟测试。
- 运行 Core、Content、Development Release 构建和 Core 隔离检查。
- 写入工作日志；不纳入既有 PSD、PNG、DLL/PDB 修改以外的无关文件。
