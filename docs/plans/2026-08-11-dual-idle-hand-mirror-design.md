# 双武器静默手侧镜像设计

## 目标

让朝南静默双持中的角色右手／画面左侧武器通过贴图镜像自然地把枪口指向左下，与另一把指向右下的武器形成倒 V。镜像必须保持枪托、握把和枪口的结构方向正确，不能用大角度旋转伪造。

## 根因

现有南北姿态默认 `HandMirror=true`，但解析器只在 `aimAngle` 接近正南或正北的 ±5 度时应用手侧镜像。角色虽然面朝南，静默时原版绘制入口传入的瞄准角仍可能是斜角，因此没有命中该门槛，两张贴图继续使用相同朝向。

上一轮 `DefaultAngle=-8`、副手最终 `+12` 只旋转了未镜像贴图。继续扩大角度会让枪口看似朝外，却把枪托和握把旋到错误方向，因此该试验数据无效。

## 方案比较

- 继续扩大装饰角：不解决贴图前后方向，放弃。
- 删除所有手侧镜像的瞄准角门槛：会影响全部南北姿态预设和斜向瞄准，放弃。
- 新增默认关闭的“非执行态强制手侧镜像”：只越过静默状态的角度门槛，保留现有主副侧裁决和攻击状态规则。采用此方案。

## 配置设计

在 `ExpressionVisualSouthNorthPoseConfig` 增加：

```csharp
public bool ForceHandMirrorWhenInactive = false;
```

该字段只解除非执行态的 `aimAngle` 门槛，仍受以下规则约束：

- `HandMirror` 必须为 `true`。
- `MirrorOnNorth` 必须为 `false`。
- 仍由现有南北朝向和主副侧关系决定具体翻转哪一侧。
- `IsExecutionActive=true` 时不强制镜像，继续使用现有瞄准角规则。

## 解算流程

`ResolveSouthNorthOffset` 根据配置和运行态生成 `ForceHandMirror`：

```text
ForceHandMirror = ForceHandMirrorWhenInactive && !IsExecutionActive
```

`ResolveDrawAngle` 的手侧镜像条件改为：

```text
HandMirrorAllowed
&& HandMirror
&& (ForceHandMirror || IsNearSouthNorthAim)
```

翻转仍复用现有 `MeshKind` 切换和角度取反。握持点作为姿态原点的解算发生在最终网格与角度确定后，因此镜像不会把握持点带离手位。

## 双武器参考预设

保留已经确认的握持目标：

```xml
<DefaultOffset>(0.20, 0, 0.12)</DefaultOffset>
```

删除上一轮无效的 `DefaultAngle` 和 `SubHandAngleOffset`，并只在双武器预设启用：

```xml
<ForceHandMirrorWhenInactive>true</ForceHandMirrorWhenInactive>
```

单武器基础预设继续没有南北姿态配置，所有旧预设因字段默认关闭而保持原行为。

## 边界

- 本次只解决静默双持贴图方向，不调整东西朝向。
- 不改变原版瞄准镜像区间、枪口发射方向或投射物行为。
- 静默镜像时，枪口诊断点仍按瞄准角解算，可能暂时不贴合镜像后的贴图尖端；执行态不强制镜像。
- 不新增贴图、不复制视觉预设、不修改 Content（内容程序集）业务代码。

## 验证

- 新测试锁定字段默认关闭、非执行态门槛、执行态不强制和东西姿态不启用。
- 参考预设测试锁定只有双武器启用该字段，并确认两个装饰角节点已删除。
- 构建 Core（核心程序集）并部署游戏 DLL（动态链接库）。
- 游戏内用朝南静默状态确认画面左侧枪贴图已翻转，枪口分别朝左下和右下。
