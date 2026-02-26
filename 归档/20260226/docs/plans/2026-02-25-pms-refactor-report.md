---
标题：BDP投射物模块系统（PMS）重构实施报告
版本号: v1.0
更新日期: 2026-02-25
最后修改者: Claude Opus 4.6
标签：[重构] [弹道系统] [架构] [报告]
摘要: 记录PMS重构的完整实施过程、变更清单、已知风险和后续调试要点
---

# BDP投射物模块系统（PMS）重构实施报告

## 1. 重构目标

将BDP弹道层从"双基类重复代码"架构迁移到"统一Bullet基类 + 模块组合"架构，消除 `Bullet_BDP` 与 `Projectile_ExplosiveBDP` 的逻辑重复，同时将Verb层的引导弹专用子类（`Verb_BDPGuided`/`Verb_BDPGuidedVolley`）上提到基类。

## 2. 变更清单

### 2.1 新建文件（7个）

| 文件 | 用途 |
|------|------|
| `Source/BDP/Trigger/Projectiles/IBDPProjectileModule.cs` | 模块接口，定义OnSpawn/OnTick/OnPreImpact/OnImpact/OnPostImpact生命周期 |
| `Source/BDP/Trigger/Projectiles/BDPModuleFactory.cs` | 静态工厂，根据ThingDef.modExtensions自动创建模块实例 |
| `Source/BDP/Trigger/Projectiles/BDPGuidedConfig.cs` | 引导飞行标记配置（DefModExtension，无字段） |
| `Source/BDP/Trigger/Projectiles/BDPExplosionConfig.cs` | 爆炸模块配置（explosionRadius, explosionDamageDef） |
| `Source/BDP/Trigger/Projectiles/TrailModule.cs` | 拖尾模块（Priority=100），从旧Bullet_BDP提取 |
| `Source/BDP/Trigger/Projectiles/GuidedModule.cs` | 引导飞行模块（Priority=10），含BuildWaypoints()和SetWaypoints() |
| `Source/BDP/Trigger/Projectiles/ExplosionModule.cs` | 爆炸模块（Priority=50），替代Projectile_ExplosiveBDP |

### 2.2 重写文件（1个）

| 文件 | 变更 |
|------|------|
| `Source/BDP/Trigger/Projectiles/Bullet_BDP.cs` | 重写为模块宿主薄壳：modules列表 + GetModule<T>() + RedirectFlight() + 生命周期分发 |

### 2.3 删除文件（3个）

| 文件 | 替代方案 |
|------|---------|
| `Source/BDP/Trigger/Projectiles/Projectile_ExplosiveBDP.cs` | ExplosionModule |
| `Source/BDP/Trigger/Projectiles/Verb_BDPGuided.cs` | Verb_BDPRangedBase.SupportsGuided + StartGuidedTargeting() |
| `Source/BDP/Trigger/Projectiles/Verb_BDPGuidedVolley.cs` | 同上，齐射走Verb_BDPVolley（基类已有引导支持） |

### 2.4 修改文件（8个）

| 文件 | 变更摘要 |
|------|---------|
| `Source/BDP/BDPMod.cs` | +3行工厂注册 + `using BDP.Trigger` |
| `Source/BDP/Trigger/DualWeapon/Verb_BDPRangedBase.cs` | +gs字段 +SupportsGuided +StartGuidedTargeting() +OrderForceTargetCore() +BaseOrderForceTarget() +TryStartCastOn重写 +GetLosCheckTarget改用gs +OnProjectileLaunched默认附加引导 |
| `Source/BDP/Trigger/Projectiles/GuidedVerbState.cs` | AttachGuidedFlight改用GetModule<GuidedModule>().SetWaypoints()，删除对旧类的引用 |
| `Source/BDP/Trigger/DualWeapon/Verb_BDPDualRanged.cs` | 删除自身gs字段（改用基类的），StartGuidedTargeting改为override，OrderForceTarget调用OrderForceTargetCore |
| `Source/BDP/Trigger/DualWeapon/Verb_BDPDualVolley.cs` | 同DualRanged的简化 |
| `Source/BDP/Trigger/UI/Command_BDPChipAttack.cs` | 类型检查从Verb_BDPGuided改为Verb_BDPRangedBase.SupportsGuided |
| `Source/BDP/Trigger/Comps/CompTriggerBody.cs` | 齐射verb类型从Verb_BDPGuidedVolley统一为Verb_BDPVolley |
| `1.6/Defs/Trigger/ThingDefs_Projectiles.xml` | 爆炸弹：thingClass→Bullet_BDP，+BDPExplosionConfig，移除projectile.explosionRadius；狙击弹：+BDPGuidedConfig |
| `1.6/Defs/Trigger/ThingDefs_Chips.xml` | 远程芯片B：verbClass从Verb_BDPGuided改为Verb_BDPShoot |

## 3. 架构变化图

```
重构前：
  Bullet_BDP (Bullet) ─── 拖尾+引导逻辑内嵌
  Projectile_ExplosiveBDP (Projectile_Explosive) ─── 拖尾+引导逻辑复制
  Verb_BDPGuided (Verb_BDPShoot) ─── 引导瞄准+BuildWaypoints
  Verb_BDPGuidedVolley (Verb_BDPVolley) ─── 引导瞄准复制

重构后：
  Bullet_BDP (Bullet) ─── 薄壳宿主，分发生命周期到模块
    ├── TrailModule ─── 拖尾（BeamTrailConfig驱动）
    ├── GuidedModule ─── 引导飞行（BDPGuidedConfig驱动）
    └── ExplosionModule ─── 爆炸（BDPExplosionConfig驱动）
  Verb_BDPRangedBase ─── 统一引导弹支持（gs + SupportsGuided）
    ├── Verb_BDPShoot ─── 单发（含引导能力）
    ├── Verb_BDPVolley ─── 齐射（含引导能力）
    ├── Verb_BDPDualRanged ─── 双侧连射（重写双侧特有逻辑）
    └── Verb_BDPDualVolley ─── 双侧齐射（重写双侧特有逻辑）
```

## 4. 已知风险与待验证项

### 4.1 高优先级（必须游戏内验证）

1. **ExplosionModule的Impact流程**：爆炸模块在OnImpact中调用`host.Destroy()`后返回true，宿主的Impact方法中`Spawned`检查可能影响OnPostImpact分发。需验证爆炸弹Impact后不报错。

2. **Scribe序列化**：`Scribe_Collections.Look(ref modules, "bdpModules", LookMode.Deep)` 要求所有模块类有无参构造函数。TrailModule/ExplosionModule的无参构造中config=null，OnTick/OnImpact中有`config ?? host.def.GetModExtension<>()`回退。需验证存档/读档后模块行为正常。

3. **GuidedModule.OnPreImpact中的RedirectFlight**：使用`host.DrawPos`作为新origin（而非旧代码的`destination`字段）。DrawPos在ticksToImpact=0时应≈destination，但可能有微小浮点偏差。需验证折线弹道视觉连续性。

4. **基类TryStartCastOn与子类重写的调用链**：`Verb_BDPRangedBase`新增了`TryStartCastOn`重写（单侧引导），`Verb_BDPDualRanged`也重写了`TryStartCastOn`（双侧引导）。`base.TryStartCastOn`调用链需确认不会双重拦截。

### 4.2 中优先级

5. **Verb_BDPShoot不再有专用引导子类**：基类的`OrderForceTarget`现在会检查`SupportsGuided`并启动引导瞄准。但`Verb_BDPShoot`没有重写`OrderForceTarget`，所以会走基类逻辑。需验证单侧引导射击流程完整。

6. **CompTriggerBody齐射verb创建**：不再区分`Verb_BDPGuidedVolley`和`Verb_BDPVolley`，统一创建`Verb_BDPVolley`。需验证齐射模式下引导飞行仍然生效。

### 4.3 低优先级

7. **无modExtensions的Bullet_BDP**：modules列表为空时应等同原版Bullet行为。工厂返回空列表，所有分发循环不执行。理论上无影响。

## 5. 验证检查清单

- [x] 远程芯片A（爆炸弹）：发射后有拖尾+爆炸效果
- [x] 远程芯片B（狙击弹）：Shift+左键放置锚点，弹道折线飞行+拖尾
- [x] 双手触发（A+B）：交替连射，B侧引导飞行正常
- [x] 齐射模式：右键齐射，引导侧折线飞行
- [X] 存档/读档：飞行中弹道恢复正常 
- [X] 近战芯片不受影响
- [ ] 护盾/辅助芯片不受影响

---

## 历史修改记录
| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-25 | PMS重构实施报告初版 | Claude Opus 4.6 |
