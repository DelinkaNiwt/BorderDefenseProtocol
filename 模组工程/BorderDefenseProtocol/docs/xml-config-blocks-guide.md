# BDP模组XML配置块使用指南

## 一、配置块总览

VerbChipConfig包含以下5个独立配置块：

| 配置块 | 用途 | 适用芯片类型 | 必填？ |
|--------|------|-------------|--------|
| `cost` | Trion成本配置 | 所有芯片 | 是 |
| `melee` | 近战配置 | 仅近战芯片 | 近战必填 |
| `volley` | 齐射配置 | 仅齐射模式 | 齐射必填 |
| `guided` | 引导配置 | 仅引导模式 | 引导必填 |
| `passthrough` | 穿透配置 | 需要穿透的芯片 | 可选 |

---

## 二、配置块详细说明

### 2.1 cost（成本配置）

**用途**：配置芯片的Trion消耗

**适用**：所有芯片（远程、近战、能力）

**字段**：
```xml
<cost>
  <trionPerShot>2</trionPerShot>  <!-- 每次攻击消耗的Trion -->
</cost>
```

**示例**：
- 低成本芯片：`<trionPerShot>1</trionPerShot>`
- 中成本芯片：`<trionPerShot>3</trionPerShot>`
- 高成本芯片：`<trionPerShot>8</trionPerShot>`

---

### 2.2 melee（近战配置）

**用途**：配置近战攻击参数

**适用**：仅近战芯片

**字段**：
```xml
<melee>
  <burstCount>3</burstCount>           <!-- 连击次数（1=单次攻击） -->
  <burstInterval>10</burstInterval>    <!-- 连击间隔（ticks） -->
  <tools>                              <!-- 近战Tool列表 -->
    <li>
      <label>刀刃</label>
      <capacities><li>Cut</li></capacities>
      <power>18</power>
      <cooldownTime>1.5</cooldownTime>
      <armorPenetration>0.25</armorPenetration>
    </li>
  </tools>
</melee>
```

**示例**：
- 单次攻击：`<burstCount>1</burstCount>`
- 3连击：`<burstCount>3</burstCount> <burstInterval>10</burstInterval>`

---

### 2.3 volley（齐射配置）

**用途**：配置齐射模式的散布参数

**适用**：仅使用`Verb_BDPVolley`的芯片

**字段**：
```xml
<volley>
  <spreadRadius>0.3</spreadRadius>  <!-- 齐射散布半径（格） -->
</volley>
```

**示例**：
- 无散布（精准齐射）：`<spreadRadius>0</spreadRadius>`
- 轻微散布：`<spreadRadius>0.3</spreadRadius>`
- 明显散布：`<spreadRadius>0.6</spreadRadius>`

**注意**：
- 只有`verbClass`为`Verb_BDPVolley`时才需要此块
- 单发射击（`Verb_BDPShoot`）不需要此块

---

### 2.4 guided（引导配置）

**用途**：配置引导飞行模式参数

**适用**：仅使用引导弹的芯片

**字段**：
```xml
<guided>
  <enabled>true</enabled>           <!-- 是否启用引导 -->
  <maxAnchors>5</maxAnchors>        <!-- 最大锚点数（不含最终目标） -->
  <anchorSpread>0.3</anchorSpread>  <!-- 锚点散布半径（格） -->
</guided>
```

**示例**：
- 不使用引导：省略整个`<guided>`块，或设置`<enabled>false</enabled>`
- 使用引导：`<enabled>true</enabled> <maxAnchors>5</maxAnchors>`

**注意**：
- 只有投射物为引导弹（如`BDP_Bullet_Guided`）时才需要此块
- 普通弹不需要此块

---

### 2.5 passthrough（穿透配置）

**用途**：配置穿体穿透能力

**适用**：需要穿透多个目标的芯片

**字段**：
```xml
<passthrough>
  <power>3</power>  <!-- 穿透力（0=不穿透，每次穿透递减） -->
</passthrough>
```

**示例**：
- 不穿透：省略整个`<passthrough>`块，或设置`<power>0</power>`
- 穿透1-2个目标：`<power>2</power>`
- 穿透3-4个目标：`<power>3</power>`

**注意**：
- 这是可选配置，大多数芯片不需要
- 穿透力每次穿透后递减，降至0时停止

---

## 三、芯片类型配置矩阵

| 芯片类型 | cost | melee | volley | guided | passthrough |
|---------|------|-------|--------|--------|-------------|
| **远程-单发** | ✅ | ❌ | ❌ | ❌ | 可选 |
| **远程-齐射** | ✅ | ❌ | ✅ | ❌ | 可选 |
| **远程-引导齐射** | ✅ | ❌ | ✅ | ✅ | 可选 |
| **远程-引导单发** | ✅ | ❌ | ❌ | ✅ | 可选 |
| **近战-单击** | ✅ | ✅ | ❌ | ❌ | ❌ |
| **近战-连击** | ✅ | ✅ | ❌ | ❌ | ❌ |
| **混合模式** | ✅ | ✅ | 可选 | 可选 | 可选 |

---

## 四、完整配置示例

### 4.1 远程-单发（最简配置）

```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <primaryVerbProps>
    <verbClass>BDP.Trigger.Verb_BDPShoot</verbClass>
    <!-- ... 其他Verb参数 ... -->
  </primaryVerbProps>

  <cost>
    <trionPerShot>5</trionPerShot>
  </cost>
</li>
```

**说明**：只需要`cost`块，其他块全部省略

---

### 4.2 远程-齐射

```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <primaryVerbProps>
    <verbClass>BDP.Trigger.Verb_BDPVolley</verbClass>
    <!-- ... 其他Verb参数 ... -->
  </primaryVerbProps>

  <cost>
    <trionPerShot>2</trionPerShot>
  </cost>

  <volley>
    <spreadRadius>0.3</spreadRadius>
  </volley>
</li>
```

**说明**：需要`cost` + `volley`

---

### 4.3 远程-引导齐射（完整配置）

```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <primaryVerbProps>
    <verbClass>BDP.Trigger.Verb_BDPVolley</verbClass>
    <defaultProjectile>BDP_Bullet_Guided</defaultProjectile>
    <!-- ... 其他Verb参数 ... -->
  </primaryVerbProps>

  <cost>
    <trionPerShot>5</trionPerShot>
  </cost>

  <volley>
    <spreadRadius>0.3</spreadRadius>
  </volley>

  <guided>
    <enabled>true</enabled>
    <maxAnchors>5</maxAnchors>
    <anchorSpread>0.3</anchorSpread>
  </guided>
</li>
```

**说明**：需要`cost` + `volley` + `guided`

---

### 4.4 近战-单击

```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <!-- 近战芯片没有primaryVerbProps -->

  <cost>
    <trionPerShot>3</trionPerShot>
  </cost>

  <melee>
    <burstCount>1</burstCount>
    <burstInterval>12</burstInterval>
    <tools>
      <li>
        <label>刀刃</label>
        <capacities><li>Cut</li></capacities>
        <power>18</power>
        <cooldownTime>1.5</cooldownTime>
      </li>
    </tools>
  </melee>
</li>
```

**说明**：需要`cost` + `melee`，不需要任何远程配置块

---

### 4.5 近战-连击

```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <cost>
    <trionPerShot>2</trionPerShot>
  </cost>

  <melee>
    <burstCount>3</burstCount>
    <burstInterval>10</burstInterval>
    <tools>
      <li>
        <label>拳头</label>
        <capacities><li>Blunt</li></capacities>
        <power>12</power>
        <cooldownTime>1.2</cooldownTime>
      </li>
    </tools>
  </melee>
</li>
```

**说明**：需要`cost` + `melee`，`burstCount>1`表示连击

---

### 4.6 远程-穿透

```xml
<li Class="BDP.Trigger.VerbChipConfig">
  <primaryVerbProps>
    <verbClass>BDP.Trigger.Verb_BDPShoot</verbClass>
    <!-- ... 其他Verb参数 ... -->
  </primaryVerbProps>

  <cost>
    <trionPerShot>6</trionPerShot>
  </cost>

  <passthrough>
    <power>3</power>
  </passthrough>
</li>
```

**说明**：需要`cost` + `passthrough`

---

## 五、命名改进总结

### 改进前（v1）

```
VerbChipConfig
├── cost
├── melee
└── special  ← 太泛化，不知道包含什么
    ├── volleySpreadRadius
    ├── guided
    └── passthroughPower
```

**问题**：
- `special`名字太泛化
- 所有远程特性混在一起
- 近战芯片也要看到`special`块（虽然不需要填）

---

### 改进后（v2）

```
VerbChipConfig
├── cost          ← 成本配置（所有芯片）
├── melee         ← 近战配置（仅近战）
├── volley        ← 齐射配置（仅齐射）
├── guided        ← 引导配置（仅引导）
└── passthrough   ← 穿透配置（可选）
```

**优势**：
- ✅ 每个块名字清晰表达功能
- ✅ 近战芯片只看到`cost`和`melee`
- ✅ 远程芯片按需选择`volley`/`guided`/`passthrough`
- ✅ 配置块职责单一，易于理解和维护

---

## 六、常见问题

### Q1: 近战芯片需要填`volley`或`guided`块吗？
**A**: 不需要。近战芯片只需要`cost` + `melee`，其他块全部省略。

### Q2: 单发射击需要填`volley`块吗？
**A**: 不需要。只有`verbClass`为`Verb_BDPVolley`时才需要`volley`块。

### Q3: 不使用引导的芯片需要填`guided`块吗？
**A**: 不需要。只有投射物为引导弹时才需要`guided`块。

### Q4: `passthrough`块是必填的吗？
**A**: 不是。这是可选配置，只有需要穿透多个目标的芯片才需要。

### Q5: 如果我想要一个既能齐射又能引导的芯片？
**A**: 同时填写`volley`和`guided`块即可（见示例4.3）。

### Q6: 混合模式芯片（远程+近战）怎么配置？
**A**: 填写`cost` + `melee` + 远程相关块（`volley`/`guided`等）。

---

**签名**: Claude Sonnet 4.6
