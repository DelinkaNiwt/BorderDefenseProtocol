# VerbAttackLogger 使用说明

## 功能概述

`VerbAttackLogger` 是一个调试日志工具类，用于在每次执行攻击动作时输出详细信息到游戏日志。

## 输出信息

每次攻击时会在游戏日志中输出以下信息：

### 单侧攻击（Verb_BDPSingle）
```
[BDP攻击] 单侧攻击 | 芯片: BDP_Chip_Scorpion | 槽位: 左手 | 模式: 逐发 | 类型: 主攻击 | 子弹数: 3 | Verb: Verb_BDPSingle
```

**字段说明：**
- **芯片**：当前使用的芯片DefName
- **槽位**：左手/右手（用于区分相同芯片时的左右槽判定）
- **模式**：逐发（Sequential）或齐射（Simultaneous）
- **类型**：主攻击或副攻击
- **子弹数**：本次burst将发射的子弹总数
- **Verb**：使用的Verb类名

### 双侧攻击（Verb_BDPDual）

**Burst开始时的总览日志：**
```
[BDP攻击] 双侧攻击 | 左手: BDP_Chip_Scorpion(逐发, 3发) | 右手: BDP_Chip_Cobra(齐射, 5发) | 类型: 主攻击 | 总子弹数: 8 | Verb: Verb_BDPDual
```

**每发射击时的详细日志：**
```
[BDP攻击] 双侧射击 | 当前射击侧: 左手 | 芯片: BDP_Chip_Scorpion | burst进度: 1/8
[BDP攻击] 双侧射击 | 当前射击侧: 右手 | 芯片: BDP_Chip_Cobra | burst进度: 2/8
```

**字段说明：**
- **左手/右手**：两侧芯片的DefName、发射模式和子弹数
- **当前射击侧**：本次射击使用的是哪一侧的芯片判定（重要：用于区分相同芯片）
- **burst进度**：当前是第几发/总共几发

### 组合技攻击（Verb_BDPCombo）
```
[BDP攻击] 组合技 | 组合技: BDP_ComboVerb_ArcMoon | 芯片: BDP_Chip_ArcMoon + BDP_Chip_Scorpion | 模式: 逐发 | 类型: 主攻击 | 子弹数: 4 | Verb: Verb_BDPCombo
```

**字段说明：**
- **组合技**：ComboVerbDef的DefName
- **芯片**：参与组合的芯片列表（用"+"连接）
- 其他字段同单侧攻击

## 使用场景

1. **调试攻击系统**：验证发射模式、子弹数量是否正确
2. **区分相同芯片**：当左右手装载相同芯片时，通过"槽位"和"当前射击侧"字段区分
3. **验证双侧交替**：观察双侧攻击时的射击顺序（L→R→L→R）
4. **检查FireMode效果**：验证连射模式是否正确影响子弹数量
5. **测试组合技**：确认组合技使用的芯片和参数

## 日志位置

- **游戏内**：按 `~` 键打开开发者控制台，查看日志输出
- **日志文件**：`C:\Users\<用户名>\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`

## 注意事项

1. 日志仅在Debug模式下有意义，Release版本可能需要开启开发者模式
2. 每次burst只在开始时记录一次总览日志（避免刷屏）
3. 双侧攻击会额外记录每发的射击侧信息（用于精确调试）
4. 日志输出使用 `Log.Message()`，不会影响游戏性能

## 实现位置

- **工具类**：`BDP/Core/VerbAttackLogger.cs`
- **调用位置**：
  - `Verb_BDPSingle.TryCastShot()` - 第33-44行
  - `Verb_BDPDual.TryCastShot()` - 第193-211行（总览）+ 第226-230行（每发）
  - `Verb_BDPCombo.TryCastShot()` - 第88-100行
