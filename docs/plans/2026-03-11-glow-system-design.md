# BDP发光效果系统设计文档

**创建日期**: 2026-03-11
**作者**: Claude Sonnet 4.6
**状态**: 设计完成，待实现

---

## 1. 概述

### 1.1 目标

设计并实现一个通用的、可复用的发光效果系统，可应用于BorderDefenseProtocol模组中的各种对象（芯片、武器、建筑、子弹等）。

### 1.2 核心需求

- **通用性**: 适用于所有Thing对象
- **灵活性**: 支持静态、动态、条件触发等多种发光模式
- **可扩展性**: 易于添加新的动画效果和触发条件
- **易用性**: 大部分场景通过XML配置即可使用
- **性能**: 优化渲染性能，避免卡顿

### 1.3 设计原则

- **渲染与控制分离**: CompGlow负责渲染，IGlowController负责逻辑
- **开闭原则**: 通过扩展而非修改来添加新功能
- **符合BDP架构**: 与现有的接口设计风格保持一致
- **最简原则**: 先实现单层发光，预留多层扩展接口

---

## 2. 架构设计

### 2.1 核心组件

```
Thing (任何对象)
    └── CompProperties_BDPGlow (XML配置)
            ├── graphicData (发光贴图配置)
            ├── controllerClass (控制器类型)
            └── controllerParams (控制器参数)

    └── CompGlow (ThingComp, 渲染层)
            ├── glowGraphic (缓存的Graphic对象)
            ├── controller (IGlowController实例)
            └── PostDraw() (绘制发光层)
```

### 2.2 控制器接口

```csharp
public interface IGlowController
{
    void Initialize(Thing parent, GlowControllerParams parameters);
    float GetGlowIntensity();  // 返回 [0, 1]
    Color? GetGlowColor();     // null = 使用默认颜色
    void Tick();
    void ExposeData();
}
```

### 2.3 数据流

```
初始化流程:
Thing.Spawn → CompGlow.PostSpawnSetup()
    ├─→ 加载graphicData → 创建glowGraphic
    └─→ 反射创建controller → controller.Initialize()

渲染流程:
Thing.Draw() → CompGlow.PostDraw()
    ├─→ controller.GetGlowIntensity() → 获取强度
    ├─→ controller.GetGlowColor() → 获取颜色
    └─→ Graphics.DrawMesh() → 绘制发光层

更新流程:
CompGlow.CompTick() → controller.Tick() → 更新动画状态
```

---

## 3. 内置Controller实现

### 3.1 StaticGlowController

**用途**: 静态发光，固定强度和颜色
**参数**: intensity (基础强度)
**适用场景**: 芯片物品、始终发光的建筑

### 3.2 PulseGlowController

**用途**: 呼吸灯/脉冲效果
**参数**:
- minIntensity: 最小强度
- maxIntensity: 最大强度
- period: 周期(ticks)
- curve: 动画曲线(Linear/SmoothInOut/Sine)

**适用场景**: 能量建筑、充能武器

### 3.3 FadeGlowController

**用途**: 淡入淡出动画
**参数**:
- fadeInTicks: 淡入时长
- sustainTicks: 持续时长
- fadeOutTicks: 淡出时长
- loop: 是否循环

**适用场景**: 激活/关闭动画、临时效果

### 3.4 EventDrivenGlowController (基类)

**用途**: 为需要响应事件的Controller提供基类
**特性**: 提供事件注册机制和强度过渡辅助方法
**适用场景**: 装备触发、战斗触发等条件发光

---

## 4. XML配置示例

### 4.1 静态发光芯片

```xml
<ThingDef ParentName="BDP_ChipBase">
  <defName>BDP_Chip_EnergyCore</defName>
  <label>能量核心芯片</label>

  <comps>
    <li Class="BDP.Glow.CompProperties_BDPGlow">
      <graphicData>
        <texPath>Things/Item/Chip/EnergyCore_Glow</texPath>
        <shaderType>MoteGlow</shaderType>
        <drawSize>1.0</drawSize>
        <color>(0.3, 0.8, 1.0)</color>
      </graphicData>
      <controllerClass>BDP.Glow.StaticGlowController</controllerClass>
      <controllerParams Class="BDP.Glow.StaticGlowParams">
        <intensity>0.9</intensity>
      </controllerParams>
    </li>
  </comps>
</ThingDef>
```

### 4.2 呼吸灯建筑

```xml
<ThingDef ParentName="BuildingBase">
  <defName>BDP_TrionReactor</defName>
  <label>Trion反应堆</label>

  <comps>
    <li Class="BDP.Glow.CompProperties_BDPGlow">
      <graphicData>
        <texPath>Things/Building/Reactor_Glow</texPath>
        <shaderType>MoteGlow</shaderType>
        <drawSize>3.0</drawSize>
      </graphicData>
      <controllerClass>BDP.Glow.PulseGlowController</controllerClass>
      <controllerParams Class="BDP.Glow.PulseGlowParams">
        <minIntensity>0.4</minIntensity>
        <maxIntensity>1.0</maxIntensity>
        <period>180</period>
        <curve>Sine</curve>
      </controllerParams>
    </li>
  </comps>
</ThingDef>
```

---

## 5. 扩展机制

### 5.1 自定义Controller

用户可以通过实现IGlowController接口创建自定义发光逻辑：

```csharp
public class TrionGlowController : IGlowController
{
    private Thing parent;
    private TrionGlowParams parameters;

    public void Initialize(Thing parent, GlowControllerParams parameters)
    {
        this.parent = parent;
        this.parameters = (TrionGlowParams)parameters;
    }

    public float GetGlowIntensity()
    {
        var compTrion = parent.TryGetComp<CompTrion>();
        if (compTrion == null) return 0f;

        float trion = compTrion.CurrentTrion;
        float t = Mathf.InverseLerp(parameters.minTrion, parameters.maxTrion, trion);
        return t * parameters.intensity;
    }

    // ... 其他方法实现
}
```

### 5.2 多层发光预留

当前实现单层发光，但在CompProperties_BDPGlow中预留了多层配置接口：

```csharp
// 预留的多层配置结构(注释掉，未来启用)
/*
public class GlowLayerConfig
{
    public GraphicData graphicData;
    public string controllerClass;
    public GlowControllerParams controllerParams;
    public BlendMode blendMode = BlendMode.Additive;
}
*/
```

---

## 6. 性能优化

### 6.1 缓存策略

- 缓存Graphic对象，避免重复创建
- 缓存每tick的强度计算结果
- 使用Material缓存避免重复创建

### 6.2 距离剔除

```csharp
private bool ShouldDraw()
{
    if (!parent.Spawned) return false;

    Vector3 cameraPos = Find.CameraDriver.MapPosition;
    float distSq = (parent.DrawPos - cameraPos).sqrMagnitude;
    return distSq < 900f;  // 30格距离
}
```

### 6.3 强度阈值

当发光强度低于0.01时跳过绘制，避免无意义的渲染调用。

---

## 7. 错误处理

### 7.1 初始化失败处理

- 捕获所有初始化异常
- 记录详细错误日志
- 设置initFailed标志，防止后续错误传播

### 7.2 运行时错误处理

- 使用Log.ErrorOnce避免重复报错
- 绘制失败时设置initFailed标志
- 提供详细的错误上下文信息

---

## 8. 目录结构

```
Source/BDP/
  └── Glow/
      ├── Core/
      │   ├── CompGlow.cs
      │   ├── CompProperties_BDPGlow.cs
      │   ├── IGlowController.cs
      │   └── GlowControllerParams.cs
      │
      ├── Controllers/
      │   ├── StaticGlowController.cs
      │   ├── PulseGlowController.cs
      │   ├── FadeGlowController.cs
      │   ├── EventDrivenGlowController.cs
      │   └── TrionGlowController.cs
      │
      └── Utils/
          └── GlowDebugTools.cs
```

---

## 9. 测试计划

### 9.1 单元测试

- Controller逻辑测试（强度计算、状态转换）
- 参数解析测试
- 错误处理测试

### 9.2 集成测试

- 不同对象类型的发光效果
- 存档/读档功能
- 性能压力测试（大量发光对象）

### 9.3 游戏内测试

- 芯片物品发光
- 装备武器发光
- 建筑发光
- 子弹飞行发光

---

## 10. 实现优先级

### Phase 1: 核心框架
1. CompGlow基础实现
2. IGlowController接口
3. StaticGlowController

### Phase 2: 动画效果
4. PulseGlowController
5. FadeGlowController

### Phase 3: 高级特性
6. EventDrivenGlowController基类
7. 性能优化
8. 调试工具

### Phase 4: 扩展示例
9. TrionGlowController
10. 文档和示例

---

## 11. 风险与限制

### 11.1 已知限制

- 当前只支持单层发光
- 不支持复杂的混合模式
- 事件系统需要与BDP现有事件集成

### 11.2 潜在风险

- 大量发光对象可能影响性能
- 反射创建Controller有轻微性能开销
- 不同着色器的兼容性问题

### 11.3 缓解措施

- 实现距离剔除和强度阈值优化
- 缓存Controller实例
- 提供详细的错误日志和调试工具

---

## 12. 参考资料

- 成长型武器模组: CompPropertiesDrawExtra_gw实现
- EccentricTechDefenseGrid: WeaponAttachment_Glow实现
- RimWorld官方: CompGlower组件
- BDP现有架构: IChipEffect、IBDPProjectileModule等接口设计

---

## 历史记录

- 2026-03-11: 初始设计完成
