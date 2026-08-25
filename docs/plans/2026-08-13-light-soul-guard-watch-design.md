# 光魂举盾“注视警戒”与禁止攻击交互设计

## 目标

举盾姿态彻底禁止攻击，同时保留攻击式目标选择和自动索敌；最终动作仅为注视警戒，不移动、不施放、不造成伤害。

## 根因

- 举盾 Hediff（健康状态）已经通过 `disabledWorkTags/Violent` 禁用暴力，原版攻击执行入口也会拒绝攻击。
- BDP 手动攻击按钮此前没有先读取暴力禁用状态，所以仍可进入目标选择。
- 原版射击提示只看人物是否持有远程武器，没有排除射击属性已随暴力能力禁用的状态，因而错误读取 `ShootingAccuracyPawn`（人物射击精度）。
- 初版警戒 Job 错把 `Verb.CanHitTarget`（当前可命中）当成目标有效性；目标暂时离开射程或视线便结束 Job，丢失锁定，偏离原版强制攻击的持续重试语义。

## 最终结构

```text
举盾 HediffComp（健康状态组件）
└─ Verb_LightSoulGuardWatch（唯一行为与目标状态）
   ├─ XML range：手动/自动共用射程
   ├─ 手动：Command_VerbTarget → OrderForceTarget → 注视警戒 Job
   └─ 自动：原版 CheckForAutoAttack 时点 → AttackTargetFinder → 保存目标
                                                        └→ 原版 FaceTarget
```

### 正式 Verb

- `HediffCompProperties_LightSoulGuardWatch` 继承原版 `HediffCompProperties_VerbGiver`。
- XML 内声明一个 `Verb_LightSoulGuardWatch`，临时 `range=15.9`，要求视线，目标类型对齐 `ForAttackAny`（任意攻击目标）。
- 该 Verb 明确 `violent=false`、`ai_IsWeapon=false`，并以 `TryCastShot() => false` 作为最后防线。
- 手动选择和自动索敌都由同一个 Verb 处理；不存在第二套距离字段、临时搜索 Verb、线程作用域或代理搜索者。

### 手动注视警戒

- HediffComp 投影原版 `Command_VerbTarget`（Verb 目标命令），按钮显示为“注视警戒”。
- 首次选择时，目标选择器调用该 Verb 的射程、视线和目标类型校验。
- `OrderForceTarget` 创建 `BDP_LightSoulGuardWatch` 作业，并把自身写入 `job.verbToUse`。
- Job 每 tick（游戏刻）检查目标：
  - 当前在射程和视线内：调用原版 `Pawn_RotationTracker.FaceTarget` 注视目标。
  - 暂时离开射程或视线：暂停注视，但保留 Job 和原目标锁定。
  - 重新进入射程和视线：自动恢复注视。
- 只有目标销毁或离图、举盾姿态结束、玩家下达其它命令时，Job 才结束。

### 自动注视警戒

- 举盾时，人物的 `TryGetAttackVerb`（取得当前攻击行为）最终返回正式警戒 Verb。它只服务原版目标查找器读取射程和视线；`Pawn.TryStartAttack` 仍会先被 `Violent` 禁用拦截。
- 在原版 `JobDriver_Wait.CheckForAutoAttack`（等待作业自动攻击检查）完成后刷新自动警戒目标，沿用战斗等待、自动攻击开关、倒地/忙碌/搬运等原版条件。
- 搜索继续调用原版 `AttackTargetFinder.BestShootTargetFromCurrentPosition`，搜索者仍是真实 Pawn。
- 朝向更新后置点只读取 Verb 保存的目标并调用原版 `FaceTarget`；不创建自动 Job，不移动，不攻击。

### 禁止攻击与提示保护

- BDP 手动攻击按钮先检查 `WorkTags.Violent`，使用原版“无法从事暴力”原因灰显。
- BDP 自动远程补充入口也在暴力禁用时拒绝提供攻击 Verb。
- 进入举盾姿态时取消本人物正在进行的瞄准、攻击忙碌姿态、当前攻击 Job 和排队攻击 Job；不触碰装备，因此不会掉落武器。
- 暴力禁用的人物不再进入原版射击命中率提示计算，避免读取已禁用属性。

## 边界

- 光魂具体业务全部位于 `BDP.Content.dll`。
- `BDP.Core.dll` 只补齐自身攻击按钮和自动攻击入口对原版暴力禁用规则的遵守，保持中性。
- 不修改盾牌角度、格挡概率、减速、Trion 消耗和贴图表现。
