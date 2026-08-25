# 光魂举盾“注视警戒”实现计划（最终修正版）

## 目标

用一个 XML 可配置的正式 Verb（行为器）统一实现光魂举盾的手动和自动注视警戒；目标暂时失去射程或视线时保留锁定，恢复后继续注视。

## 实施项

1. 用烟雾测试锁定以下边界：
   - 射程唯一来源为 Verb 的 XML `range`，临时值 `15.9`。
   - 手动按钮必须是原版 `Command_VerbTarget`，名称为“注视警戒”。
   - 手动与自动必须读取同一个 Verb。
   - Job 只能注视，不能移动或攻击。
   - `CanHitTarget` 只能决定本 tick 是否转向，不能结束 Job。
   - 旧的 `Facing`、`FaceTarget` 自定义技术标识必须不存在。
2. 将自定义类、DefName、语言键和测试统一迁移为 `GuardWatch`。
3. 修正 `JobDriver_LightSoulGuardWatch`：
   - 目标暂时不可命中时保留 Job 和目标。
   - 目标恢复射程和视线后继续调用原版 `FaceTarget`。
   - 目标真正销毁、离图或姿态结束时才结束。
4. 保留原版自动攻击检查时点、原版目标查找器、禁止暴力门禁及射击提示保护。
5. 编译 `BDP.Content`，运行光魂、攻击门禁和自动攻击相关烟雾测试，检查旧标识零残留，记录工作日志并提交。

## 验证命令

```powershell
dotnet msbuild Source/BDP.Content/BDP.Content.csproj /t:Rebuild /p:Configuration=Release /v:minimal
& Source/BDP.Tests/LightSoulGuardWatchSmokeTests.ps1
& Source/BDP.Tests/LightSoulChipSmokeTests.ps1
& Source/BDP.Tests/ViolenceDisabledAttackGateSmokeTests.ps1
& Source/BDP.Tests/AutoAttackSeparationSmokeTests.ps1
```

## 游戏内验收

- 攻击按钮灰显；“注视警戒”按钮仍可用。
- 手动选中目标后，人物只原地注视，不移动、不攻击。
- 目标离开 15.9 格或被障碍遮挡时暂停注视，但 Job 不结束。
- 原目标回到射程并恢复视线后，人物自动继续注视。
- 目标销毁、离图、退出举盾或收到其它命令后，锁定解除。
