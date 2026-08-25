# BDP 按实际枪口生成原版射击闪光设计

## 目标

BDP 每个成功发射的投射物计划，都在其来源武器的枪口锚点生成一次原版 `ShotFlash（射击闪光）`；同帧双侧发射时两侧各自闪光，且不再额外生成小人中心闪光。

## 边界

- 继续调用原版 `FleckMaker.Static（静态特效生成接口）` 与 `FleckDefOf.ShotFlash（原版射击闪光定义）`，不自建枪焰渲染器。
- 枪焰位置使用视觉系统已经解析出的 `rootOrigin（枪口根坐标）`，不跟随弹丸随机散布偏移。
- 枪焰尺寸由每个来源结果自己的 `muzzleFlashScale（枪口闪光尺寸）` 冻结到投射物计划，混合双持不共享宿主值。
- 只有投射物实际成功发射后才生成枪焰；失败计划不闪光。
- BDP 宿主暴露给原版的 `muzzleFlashScale` 固定为零，避免原版随后在 `caster.Position（射手所在格中心）` 再生成一次闪光。

## 数据流

```text
来源 FormalExpressionResult（正式表达结果）
  -> ResolvedVerbSpec.MuzzleFlashScale（正式动作规格中的枪焰尺寸）
  -> ProjectileInitPlan.MuzzleFlashScale（每发冻结值）
  -> TryEmitPlan 成功
  -> FleckMaker.Static(rootOrigin, map, ShotFlash, scale)
```

## 兼容策略

BDP 复用原版特效定义与生成接口，因此替换 `ShotFlash` 贴图、材质或定义，以及拦截原版特效生成接口的模组仍有机会生效。BDP 不全局修改 `FleckMaker`，也不压制非 BDP 武器的原版枪焰。

## 验证

- 静态冒烟测试确认枪焰尺寸沿正式规格和每发计划传递。
- 静态冒烟测试确认宿主中心枪焰被压制。
- 静态冒烟测试确认只有成功发射后，才在枪口根坐标调用原版 `ShotFlash`。
- 构建主模组，随后由游戏内分别观察单武器、同型双持和混合双持。
