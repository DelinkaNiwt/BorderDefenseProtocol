# Trion 构型枪口闪光增强设计

## 已确认事实

- `BDP_ArmamentForm_TrionCube` 是射手远程武装的隐藏默认构型。
- Trion 的射击逻辑已经由 BDP 通用远程流程播放原版 `ShotFlash`。
- 当前 Trion 构型没有显式设置 `muzzleFlashScale`，因此使用原版默认值 `1.0`。

## 目标

只把 Trion 构型的默认枪口闪光尺寸提高到 `1.8`，让射击瞬间更容易观察；不改变其他武器、射击时序、贴图阶段或通用发射逻辑。

## 方案

在 `BDP_ArmamentForm_TrionCube/overrides` 增加 `muzzleFlashScale=1.8`。该字段会沿现有构型覆盖链进入正式远程发射计划，继续由通用 `ShotFlash` 播放。

## 验证

- 扩展 Trion 视觉烟测，断言构型覆盖值为 `1.8`。
- 运行 Trion 视觉烟测和枪口闪光归属烟测。
- 最终仍需进游戏观察实际显示强度；本次不修改通用代码，因此不需要重新编译程序集。
