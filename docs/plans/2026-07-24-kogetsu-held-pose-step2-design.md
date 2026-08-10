# 弧月手持姿态第二步设计

## 目标

在第一步“手柄主层与发光刀刃层同时绘制”已经通过游戏内确认的基础上，恢复旧 BDP 弧月的完整常驻手持姿态。

验收覆盖：

- 弧月位于主侧时，人物朝南、北、东、西四种表现。
- 弧月位于副侧时，人物朝南、北、东、西四种表现。
- 共 8 种位置、角度、镜像和前后遮挡组合。

本步不修改旋空弧月招式动画、近战伤害、攻击时序或贴图资源。

## 根因

新 BDP 已有 `VisualPoseResolver`（视觉姿态解析器），其南北、东西、手侧镜像和高度层算法与旧版 `WeaponDrawChipConfig`（武器绘制芯片配置）逐项对应。

当前缺口只有绘制入口：

- 单枚激活武器芯片固定使用 `ReplaceTextureOnly`（只替换贴图）。
- 该路径故意沿用 RimWorld（边缘世界）原版持握姿态，不读取视觉预设的南北或东西姿态。
- 因此即使给弧月补回旧版姿态参数，这些参数也不会生效。

## 方案选择

### 采用：显式姿态配置自动升级为完整替换

把视觉预设是否显式声明 `SouthNorthPose`（南北姿态）或 `EastWestPose`（东西姿态），定义为单武器是否需要完整姿态管线的中性判断：

- 两个姿态块都为空：继续使用 `ReplaceTextureOnly`，保持原版持握。
- 任意姿态块存在：使用 `Replace`（完整替换），进入现有 `DrawResidentEntries`（绘制常驻条目）和 `VisualPoseResolver`。

该规则按照当前关系最终选中的普通或复合视觉预设判断，不按弧月名称判断。

优点：

- 不新增弧月专用代码。
- 不新增需要与姿态块重复维护的布尔开关。
- 不复制姿态算法。
- 现有未配置姿态块的单武器继续沿用原版表现。

### 放弃：增加显式布尔开关

例如 `UseFullSingleWeaponPose`（单武器使用完整姿态）。它表达明确，但作者必须同时维护开关和姿态块，容易出现配置不一致，属于不必要字段。

### 放弃：在只替换贴图路径中复制姿态算法

这会形成第二套南北、东西、镜像和高度计算，后续与完整管线产生漂移，不符合骨架复用和最小介入原则。

### 放弃：全部单武器统一进入完整管线

它会改变现有武器已经确认的原版持握基线，影响范围超过弧月需求。

## 架构设计

### 视觉预设

`ExpressionVisualPresetDef`（表达视觉预设定义）增加一个只读语义：

```text
HasExplicitPose =
    SouthNorthPose 不为空
    或 EastWestPose 不为空
```

该语义不缓存、不改变配置默认值，只回答作者是否显式声明自定义姿态。

### 视觉投影

`DefaultVisualProjectionBuilder`（默认视觉投影构建器）先确定当前视觉关系，再解析宿主装备绘制模式：

```text
单武器
  ├─ 最终视觉预设无显式姿态 → ReplaceTextureOnly
  └─ 最终视觉预设有显式姿态 → Replace

多武器
  └─ 保持现有 Replace / Suppress 规则
```

“最终视觉预设”与绘制补丁使用同一规则：

- 单侧关系使用 `VisualPresetDefName`（普通视觉预设名称）。
- 组合或双武器关系优先使用 `CompositeVisualPresetDefName`（复合视觉预设名称）。
- 没有复合预设时回退普通预设。

### 绘制

弧月进入 `Replace` 后直接复用现有流程：

1. 解析弧月主贴图和发光刀刃附加层。
2. `VisualPoseResolver` 计算主侧/副侧和四朝向姿态。
3. 主贴图与附加层共享位置、角度和镜像。
4. 刀刃层继续保留第一步的极小高度差。
5. 绘制成功后跳过原版宿主装备贴图；解析失败时回退原版绘制。

不修改第一步新增的单武器附加层快捷绘制能力，它仍服务“原版姿态 + 多层贴图”的其他预设。

## 弧月参数映射

### 南北姿态

```xml
<SouthNorthPose>
  <DefaultOffset>(-0.20, 0, 0.1)</DefaultOffset>
  <DefaultAngle>-50</DefaultAngle>
  <DefaultAltitudeOffset>0.05</DefaultAltitudeOffset>
  <SouthZAdjust>-0.05</SouthZAdjust>
  <NorthZAdjust>0.05</NorthZAdjust>
  <SubHandAngleOffset>15</SubHandAngleOffset>
</SouthNorthPose>
```

含义：

- 主侧朝南以 X `-0.20` 为基准，副侧自动镜像到另一边。
- 朝北时 X、Z 和高度按旧版规则反向。
- `-50` 度补偿弧月贴图自身斜角。
- 副侧额外偏转 `15` 度。
- 手侧镜像和朝北规则继承新骨架默认值，不重复写入。

### 东西姿态

```xml
<EastWestPose>
  <SideBaseX>0.08</SideBaseX>
  <SideDeltaX>0.03</SideDeltaX>
  <FrontAltitudeOffset>0.05</FrontAltitudeOffset>
  <BackAltitudeOffset>-0.05</BackAltitudeOffset>
  <DefaultAngle>-50</DefaultAngle>
  <SubHandAngleOffset>15</SubHandAngleOffset>
</EastWestPose>
```

含义：

- 朝东和朝西自动反转侧身 X 基准。
- 前景手靠近人物中心，背景手远离人物中心。
- 前景手绘制在人物前方，背景手绘制在人物后方。
- 旧版 `sideDeltaZ`（侧身 Z 微分）为默认 `0`，继续省略。
- 东西朝向不做额外手侧镜像，继承新骨架默认值。

## 兼容与回退

- 当前 DevHarness（伴生测试模组）其它武器视觉预设均未声明姿态块，继续使用原版持握。
- 弧月与旋空组成组合关系时，旋空是被动来源，激活武器实例数仍为 1；弧月预设声明姿态后进入完整管线。
- 最终预设找不到或主贴图解析失败时，继续回退宿主装备原版绘制，避免人物手中完全空白。
- 本步不改变执行焦点、枪口跟随或近战攻击语义。

## 验证

### 自动验证

1. 先新增失败测试，要求无姿态的单武器继续返回 `ReplaceTextureOnly`。
2. 要求显式姿态的单武器返回 `Replace` 并复用完整姿态管线。
3. 要求普通/复合最终预设选择规则与绘制补丁一致。
4. 要求弧月南北、东西参数逐项等于旧版。
5. 要求弧月手柄与刀刃配置保持第一步结果不变。
6. 解析全部 DevHarness XML（可扩展标记语言）并编译主模组与 DevHarness 的 `Release`（发布）配置。

### 游戏内确认

依次检查：

1. 主侧朝南。
2. 主侧朝北。
3. 主侧朝东。
4. 主侧朝西。
5. 副侧朝南。
6. 副侧朝北。
7. 副侧朝东。
8. 副侧朝西。

每种状态只确认：

- 武器位置。
- 刀身角度。
- 左右镜像。
- 人物前后遮挡。
- 手柄和发光刀刃是否继续重合。

自动验证完成后停止，由用户进行上述 8 种游戏内确认。
