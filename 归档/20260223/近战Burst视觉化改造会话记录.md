---
标题：近战Burst视觉化改造会话记录
版本号: v1.5
更新日期: 2026-02-24
最后修改者: Claude Opus 4.6
标签: [日志][进行中][未锁定]
摘要: 记录近战burst从同tick多击改为跨tick连击的分析、设计、实现和Bug修复过程
---

## 会话背景

BDP模组的近战burst系统在单次`TryCastShot()`中通过for循环同步执行所有打击，视觉上只有一次挥砍动画但伤害日志刷出多条。用户希望实现真正的视觉连击感。

## 分析阶段

### 1. 根因确认
通过RimSearcher查询引擎源码，确认：
- `Verb_MeleeAttack.TryCastShot()` 每次调用触发一次 `Notify_MeleeAttackOn()` 前冲动画
- 当前实现在一次调用内循环多次 `base.TryCastShot()`，动画只播放1次

### 2. 方案评估
评估了4个方案：
- **方案A（语义重定义）**：不改代码，改叫"多重打击"——体验无改善
- **方案B（冷却换连击）**：burst转化为cooldown缩减——轮次边界消失，双近战体验崩坏
- **方案C（自定义Tick延迟）**：完全自建状态机——复杂度高
- **方案D（借用引擎burst机制）**：利用`burstShotCount`+`ticksBetweenBurstShots`——最终采用

### 3. 引擎验证（RimSearcher）
查验了以下关键源码，发现两个阻塞点：

**阻塞点1：VerbTick不被调用**
- `Pawn.Tick()` 调用 `verbTracker.VerbsTick()`，但BDP Verb不在VerbTracker中
- 解决：自定义JobDriver手动调用VerbTick()

**阻塞点2：FullBodyBusy时序死锁**
- `Pawn.Tick()` 中 `VerbsTick()`(L1546) 先于 `StanceTrackerTick()`(L1556)
- `TryCastNextBurstShot()` 设置 `Stance_Cooldown(N+1)` 但 `ticksToNextBurstShot=N`
- VerbTick触发时Stance还剩1tick → `FullBodyBusy=true` → `TryCastShot()`返回false → burst取消
- 解决：在TryCastShot()中清除Stance_Cooldown

**额外发现：固定间隔限制**
- `TicksBetweenBurstShots` 非virtual属性，`TryCastNextBurstShot()` 非virtual方法
- 解决：Post-VerbTick覆盖 `ticksToNextBurstShot` 字段（protected，子类可写）

### 4. 按芯片配置间隔的可行性
用户提出按芯片配置不同burst间隔的需求。确认通过 `ApplyPendingInterval()` 在VerbTick之后覆盖引擎设置的固定值即可实现，与双武器系统三种组合模式均兼容。

## 实现阶段

### 改动文件清单

| 文件 | 操作 | 说明 |
|------|------|------|
| `Verb_BDPMelee.cs` | 重写 | 去for循环，加hitIndex状态机+Stance清除+ApplyPendingInterval |
| `WeaponChipConfig`（同文件） | 新增字段 | `meleeBurstInterval`（int, 默认12 ticks） |
| `JobDriver_BDPChipMeleeAttack.cs` | 新建 | 跟随+近战+手动VerbTick+ApplyPendingInterval |
| `DualVerbCompositor.cs` | 修改 | ComposeDualMelee设置burstShotCount和ticksBetweenBurstShots |
| `WeaponChipEffect.cs` | 修改 | SynthesizeMeleeVerbProps接收完整WeaponChipConfig |
| `BDP_DefOf.cs` | 新增 | BDP_ChipMeleeAttack引用 |
| `JobDefs_Trigger.xml` | 新增 | BDP_ChipMeleeAttack JobDef |
| `ThingDefs_Chips.xml` | 修改 | MeleeA: interval=6, MeleeB: interval=18 |

### 编译结果
0 错误 0 警告，编译通过。

## 待验证项

- [x] ~~游戏内单近战芯片A（burst=2, interval=6tick）：2次独立前冲动画~~ → Bug7阻塞，已修复
- [x] 游戏内单近战芯片A（burst=2, interval=6tick）：2次独立前冲动画（Bug9修复后确认工作）
- [x] 游戏内双近战A+B：左右芯片交替出现在伤害日志中
- [ ] 混合模式（近战+远程）：近战侧burst正常，远程侧不受影响
- [x] 伤害日志显示正确芯片名
- [x] 目标死亡时burst中断
- [x] 跟随目标移动（Bug8修复后确认）

## Bug修复阶段

### Bug7：miss/dodge取消整个burst（2026-02-23）

**现象：**
1. 攻击间隔远比配置的0.1s/0.3s长，连击感完全没有
2. 双武器A+B只能看到芯片A的伤害，芯片B从未出现
3. 伤害日志每次攻击多条记录（确认为原版行为，非bug）

**根因分析：**
通过追踪引擎完整调用链发现：

```
TryCastNextBurstShot():
    if (Available() && TryCastShot()):  // 成功
        burstShotsLeft--;
    else:
        burstShotsLeft = 0;  // 失败 → 取消整个burst！
```

`Verb_MeleeAttack.TryCastShot()` 在miss和dodge时返回false。
我们的override直接 `return base.TryCastShot()` 的返回值，导致：
- 任何一击miss/dodge → `TryCastNextBurstShot` 设 `burstShotsLeft=0` → 整个burst取消
- 每次攻击退化为独立的完整周期（warmup + 1击 + 完整cooldown 1.5-2秒）
- 双武器A+B中hitIndex永远到不了右侧(B)

注意：远程burst不受此影响，因为远程的`TryCastShot()`在miss时仍返回true（弹丸已发射）。

**引擎调用链验证：**
```
Pawn.DoTick()
  ├─ Tick() [每tick]
  │   └─ JobTrackerTick() → DriverTick() → tickAction [每tick]
  │       └─ VerbTick() + ApplyPendingInterval()  ← burst计时器驱动
  └─ TickInterval(delta) [按UpdateRateTicks间隔]
      └─ JobTrackerTickInterval() → DriverTickInterval() → tickIntervalAction
          └─ TryMeleeAttack() → TryStartCastOn()  ← 发起新burst
```

`tickIntervalAction` 的调用频率取决于 `GetCameraUpdateRate()`：
- 视野内：`(int)(cameraZoom + 1)` = 1-5 ticks
- 视野外：15 ticks

**修复：**
`Verb_BDPMelee.TryCastShot()` 中，burst期间始终返回true：

```csharp
// 修复前：
bool result = base.TryCastShot();
// ...
return result;  // miss/dodge时false → burst取消

// 修复后：
base.TryCastShot();
// ...
return true;  // miss/dodge已由base处理，burst继续
```

**改动文件：** `Verb_BDPMelee.cs` 第95行
**编译结果：** 0错误 0警告
**测试结果：** 用户反馈无变化，添加调试日志进一步排查

### Bug8：目标走离后小人不跟随（2026-02-23）

**现象：**
小人冲上前贴身近战攻击后，如果目标走离，小人停留在原地不追。

**根因分析：**
`JobDriver_BDPChipMeleeAttack.tickIntervalAction` 的跟随条件逻辑写反了：

```csharp
// 我们的（错误）：用 && → 目标相同时永远不跟
if (thing != destination.Thing && (!moving || !canReach))

// 原版FollowAndMeleeAttack（正确）：用 || → 目标相同但走远也跟
if (target != destination || (!moving && !canReach))
```

场景：目标走离后
- `thing == destination.Thing` → true（同一个目标对象）
- `!moving` → true（攻击后停下了）
- `!canReach` → true（目标走远了）

原版：`false || (true && true)` = true → 跟随 ✓
我们：`false && (true || true)` = false → 不跟随 ✗

另外，原版用 `else if` 分离跟随和攻击（要么跟要么打），我们用独立 `if`（可能同时跟和打）。

**修复：**
1. `&&` 改为 `||`，匹配原版逻辑
2. 攻击分支改为 `else if`，与原版一致

**改动文件：** `JobDriver_BDPChipMeleeAttack.cs`
**编译结果：** 0错误 0警告

### 调试日志（临时，2026-02-23）

在 `Verb_BDPMelee.TryCastShot()` 中添加 `Log.Message` 输出：
- Burst开始时：burstShotsLeft、左右burst数/间隔、verbProps.burstShotCount
- 每击时：hitIndex、side、chipDef、burstShotsLeft、state

用于确认Bug7修复是否生效、burst机制是否正常启动。验证完毕后移除。

### Bug9：ShotsPerBurst硬编码为1（2026-02-23）——burst从未启动的真正根因

**现象：**
Bug7修复后用户反馈无变化。调试日志显示每次burst开始时 `burstShotsLeft=1`，
即使 `verbProps.burstShotCount=2`。burst从未真正多发过。

**根因分析：**
```csharp
// Verb基类（Verse.Verb.cs:143）：
protected virtual int ShotsPerBurst => 1;  // 硬编码1！

// WarmupComplete()（Verse.Verb.cs:377）：
burstShotsLeft = ShotsPerBurst;  // 永远=1
```

只有 `Verb_Shoot` 覆盖了 `ShotsPerBurst => base.BurstShotCount`（读取verbProps）。
近战Verb继承链（`Verb_MeleeAttack` → `Verb_MeleeAttackDamage` → `Verb_BDPMelee`）
从未覆盖此属性，导致无论 `verbProps.burstShotCount` 设多少，`burstShotsLeft` 永远=1。

这解释了为什么Bug7的修复（return true）没有效果——burst本身就只有1发，
不存在"第2发被miss取消"的情况。Bug7的修复仍然是正确的（防止miss取消burst），
但Bug9才是burst不工作的真正原因。

**修复：**
在 `Verb_BDPMelee` 中覆盖 `ShotsPerBurst`：

```csharp
protected override int ShotsPerBurst => verbProps.burstShotCount;
```

**改动文件：** `Verb_BDPMelee.cs`
**编译结果：** 0错误 0警告
**测试结果：** burst基本成功，burstShotsLeft正确显示8（左2+右6），双芯片交替出现

### Bug10：burst异常后verb.Reset()导致无限重启 + tickIntervalAction无保护（2026-02-24）

**现象：**
Bug9修复后burst基本工作，但出现两类异常：
1. 红色 `Exception in JobDriver tick` — 来自tickIntervalAction（无try-catch）
2. 黄色 `tickAction异常，重置verb: Object reference not set to an instance of an object` — 来自tickAction（有try-catch）
3. `verb.Reset()`后burstShotsLeft回到8，burst无限重启

**根因分析：**
三个问题叠加：

1. **tickAction的catch调用`verb.Reset()`**：Reset()清除state/currentTarget/burstShotsLeft，
   但不清除BDP自有的hitIndex字段。下次tickIntervalAction调用TryMeleeAttack时，
   TryStartCastOn检查state=Idle通过 → WarmupComplete重设burstShotsLeft=8 → burst重启，
   但hitIndex残留 → 状态不一致 → 无限循环。

2. **tickIntervalAction无try-catch**：任何异常直接变红色错误，Job崩溃。

3. **tickIntervalAction在burst期间仍尝试攻击**：虽然TryStartCastOn有`state==Bursting`保护，
   但异常可能在到达该检查前发生。

**修复（三处）：**

1. `Verb_BDPMelee` 新增 `SafeAbortBurst()` 方法：清理所有BDP状态字段（hitIndex、
   currentChipDef、pendingInterval、cachedBurst）再调用基类Reset()，确保状态完全一致。

2. `JobDriver_BDPChipMeleeAttack.tickAction` catch：改用`SafeAbortBurst()`替代`verb.Reset()`，
   输出完整堆栈（`ex`而非`ex.Message`）方便定位。

3. `JobDriver_BDPChipMeleeAttack.tickIntervalAction`：
   - 加try-catch防止红色异常
   - 加`verb.state == VerbState.Bursting`保护，burst进行中不尝试新攻击
   - 异常时调用SafeAbortBurst() + EndJobWith(Errored)终止Job

4. `Verb_BDPMelee.ApplyPendingInterval()`：加`pawn?.stances != null && currentTarget.IsValid`
   防御性检查。

**改动文件：** `Verb_BDPMelee.cs`、`JobDriver_BDPChipMeleeAttack.cs`
**编译结果：** 0错误 0警告
**测试结果：** 红色异常消除，黄色异常仍存在（NullRef来源待定位 → Bug11）

### Bug11：Verb.maneuver为null导致NullRef（2026-02-24）——burst异常的真正根因

**现象：**
Bug10修复后红色异常消除，黄色warning提供了完整堆栈：

```
System.NullReferenceException: Object reference not set to an instance of an object
  at RimWorld.Verb_MeleeAttack.TryCastShot () [0x0027e]
  at BDP.Trigger.Verb_BDPMelee.TryCastShot () [0x00185]
```

NullRef发生在`Verb_MeleeAttack.TryCastShot()`内部，偏移0x0027e。

**根因分析：**
`Verb_MeleeAttack.TryCastShot()` 中 `CreateCombatLog` 访问 `maneuver` 字段：

```csharp
// Verb_MeleeAttack.cs:
CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesHit, ...);
// 内部访问 this.maneuver（Verb基类的公共字段）
// 以及 maneuver.logEntryDef
```

`Verb.maneuver` 由 `VerbTracker.InitVerb()` 设置（从Tool的ToolCapacityDef查找ManeuverDef）。
但BDP的Verb不经过VerbTracker创建路径——在 `CompTriggerBody.CreateAndCacheChipVerbs()` 中
手动实例化，代码明确注释 `// verb.tool = null` 和 `// verb.maneuver = null`。

因此 `maneuver` 始终为null → `maneuver.combatLogRulesHit` 抛NullRef。

同理 `verb.tool` 也为null，影响战斗日志的bodyPartGroup和label显示。

**修复：**
在 `Verb_BDPMelee` 中新增 `EnsureToolAndManeuver()` 方法，每击调用（非仅首击，
因为左右芯片可能有不同的tool/maneuver）：

```csharp
private void EnsureToolAndManeuver(CompTriggerBody triggerComp, SlotSide side)
{
    var slot = triggerComp.GetActiveSlot(side);
    var cfg = slot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
    var firstTool = cfg?.tools?.FirstOrDefault();

    tool = firstTool;  // 设置tool（供战斗日志使用）

    if (firstTool?.capacities?.Count > 0)
        maneuver = DefDatabase<ManeuverDef>.AllDefs
            .FirstOrDefault(m => m.requiredCapacity == firstTool.capacities[0]);

    if (maneuver == null)  // 兜底
        maneuver = DefDatabase<ManeuverDef>.AllDefs.FirstOrDefault();
}
```

在 `TryCastShot()` 中 `base.TryCastShot()` 调用前调用此方法。

**改动文件：** `Verb_BDPMelee.cs`（新增using System.Linq + EnsureToolAndManeuver方法）
**编译结果：** 0错误 0警告
**测试结果：** NullRef消除。仅残留一次原版引擎warning `"meleed from out of melee position"`
（burst续击时目标微移导致CanHitTarget返回false，但攻击仍正常执行，无功能影响）。

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-23 | 初始记录：近战burst视觉化改造全过程 | Claude Opus 4.6 |
| v1.1 | 2026-02-23 | Bug7修复：miss/dodge取消整个burst，改为始终返回true | Claude Opus 4.6 |
| v1.2 | 2026-02-23 | Bug8修复：跟随条件&&改||；添加burst调试日志 | Claude Opus 4.6 |
| v1.3 | 2026-02-23 | Bug9修复：覆盖ShotsPerBurst（基类硬编码1） | Claude Opus 4.6 |
| v1.4 | 2026-02-24 | Bug10修复：SafeAbortBurst替代verb.Reset()，tickIntervalAction加try-catch和burst保护 | Claude Opus 4.6 |
| v1.5 | 2026-02-24 | Bug11修复：EnsureToolAndManeuver设置tool/maneuver字段（BDP Verb不经过InitVerb） | Claude Opus 4.6 |
