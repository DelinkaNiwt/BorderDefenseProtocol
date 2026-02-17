---
标题：穿梭机系统与TransportShip架构
版本号: v1.0
更新日期: 2026-02-15
最后修改者: Claude Opus 4.6
标签：[文档][用户未确认][已完成][未锁定]
摘要: RimWorld穿梭机(Shuttle)系统完整技术参考——三层架构（TransportShipDef定义→TransportShip运行时实例+ShipJob状态机→CompShuttle+CompTransporter组件协作）、ShipJob四子类生命周期（Arrive着陆→Wait等待装载→FlyAway起飞→Unload卸载）、帝国穿梭机（Royalty许可召唤+permitShuttle+任务系统QuestPart深度集成）vs玩家穿梭机（Odyssey Building_PassengerShuttle+CompLaunchable+CompRefuelable燃料系统+playerShuttle标记）、运输舱vs穿梭机核心差异（单向消耗vs可往返可重用）、CompShuttle装载约束系统（requiredItems/requiredPawns/acceptColonists/autoload自动装载）
---

# 穿梭机系统与TransportShip架构

**总览**：穿梭机（Shuttle）是RimWorld中可往返的世界地图运输工具，与运输舱（Transport Pod）的单向消耗模式形成对比。系统采用三层架构：`TransportShipDef`（定义层，6字段）→ `TransportShip`（运行时实例，ShipJob状态机驱动）→ `CompShuttle` + `CompTransporter`（组件层，装载约束+物品容器）。穿梭机有两种来源：**帝国穿梭机**（Royalty DLC，皇室许可召唤，任务系统深度集成）和**玩家穿梭机**（Odyssey DLC，`Building_PassengerShuttle`，可建造+燃料驱动+可重复使用）。

## 1. 核心类架构

```
TransportShipDef（定义层，6字段）
  └── TransportShip（运行时实例，IExposable）
      ├── shipThing: Thing          ← 地图上的穿梭机建筑实体
      ├── curJob: ShipJob           ← 当前ShipJob
      ├── shipJobs: List<ShipJob>   ← ShipJob队列
      └── questTags: List<string>   ← 任务信号标签

Thing（穿梭机建筑实体）
  ├── CompShuttle                   ← 穿梭机核心逻辑（装载约束、自动装载）
  ├── CompTransporter               ← 物品/Pawn容器（innerContainer）
  ├── CompLaunchable（仅Odyssey）    ← 发射逻辑（燃料消耗、距离计算）
  └── CompRefuelable（仅Odyssey）    ← 燃料系统（Chemfuel）
```

**TransportShipDef字段表**：

| 字段 | 类型 | 说明 | Ship_Shuttle值 | Ship_PassengerShuttle值 |
|------|------|------|---------------|------------------------|
| `shipThing` | ThingDef | 地图上的建筑实体 | Shuttle | PassengerShuttle |
| `arrivingSkyfaller` | ThingDef | 着陆天降物 | ShuttleIncoming | PassengerShuttleIncoming |
| `leavingSkyfaller` | ThingDef | 起飞天降物 | ShuttleLeaving | PassengerShuttleLeaving |
| `worldObject` | WorldObjectDef | 世界地图飞行物 | TravelingShuttle | PassengerShuttle |
| `maxLaunchDistance` | int | 最大发射距离（格） | 70 | —（由CompLaunchable控制） |
| `playerShuttle` | bool | 是否玩家穿梭机 | false | true |

## 2. ShipJob状态机（穿梭机行为驱动）

TransportShip通过ShipJob队列驱动行为，类似Pawn的Job系统。每tick执行`curJob.TickInterval()`，Job结束后自动取下一个。

### 2.1 ShipJob继承体系

```
ShipJob（基类，IExposable）
├── ShipJob_Arrive        ← 着陆：生成Skyfaller天降物，穿梭机从天而降
├── ShipJob_Wait（抽象）   ← 等待：地面停留，提供Gizmo按钮
│   ├── ShipJob_WaitTime      ← 定时等待：duration ticks后自动结束
│   ├── ShipJob_WaitForever   ← 永久等待：直到穿梭机被摧毁
│   └── ShipJob_WaitSendable  ← 等待发送：满足条件后自动飞往目的地
├── ShipJob_FlyAway       ← 起飞：生成Skyfaller离开，转为世界地图飞行物
└── ShipJob_Unload        ← 卸载：每60 ticks卸载一个物品/Pawn
```

### 2.2 ShipJob核心字段

| 字段/属性 | 类型 | 说明 |
|----------|------|------|
| `def` | ShipJobDef | Job定义 |
| `transportShip` | TransportShip | 所属TransportShip |
| `jobState` | ShipJobState | 当前状态 |
| `ShouldEnd` | bool（虚属性） | 是否应结束（子类重写） |
| `Interruptible` | bool（虚属性） | 是否可被打断（FlyAway/Arrive=false） |
| `HasDestination` | bool（虚属性） | 是否有预定目的地 |

### 2.3 7个ShipJobDef

| defName | jobClass | 说明 | DLC |
|---------|----------|------|-----|
| `Arrive` | ShipJob_Arrive | 着陆到地图 | Core |
| `WaitTime` | ShipJob_WaitTime | 定时等待 | Core |
| `WaitForever` | ShipJob_WaitForever | 永久等待（许可穿梭机） | Royalty |
| `WaitSendable` | ShipJob_WaitSendable | 等待发送到目的地 | Royalty |
| `FlyAway` | ShipJob_FlyAway | 起飞离开 | Royalty |
| `Unload` | ShipJob_Unload | 卸载内容物 | Core |
| `Unload_Destination` | ShipJob_Unload | 到达目的地后卸载 | Royalty |

## 3. 穿梭机完整生命周期

### 3.1 帝国穿梭机（许可召唤）

```
玩家使用皇室许可 CallTransportShuttle
  → RoyalTitlePermitWorker_CallShuttle.CallShuttle(landingCell)
    ├── ThingMaker.MakeThing(ThingDefOf.Shuttle)     ← 创建穿梭机实体
    ├── compShuttle.permitShuttle = true              ← 标记为许可穿梭机
    ├── TransportShipMaker.MakeTransportShip()        ← 创建TransportShip包装
    ├── transportShip.ArriveAt(cell, mapParent)       ← 触发着陆
    └── transportShip.AddJobs(                        ← 排队ShipJob序列
          WaitForever,        ← 永久等待玩家装载
          Unload_Destination, ← 到达目的地后卸载
          FlyAway             ← 最后飞走
        )
```

**许可穿梭机Gizmo**（ShipJob_Wait提供）：
- `permitShuttle=true`时：**发射按钮**（选择世界地图目标）+ **遣返按钮**（Unload→FlyAway）
- `permitShuttle=false`时：**发送按钮**（仅当AllRequiredThingsLoaded时可用）

### 3.2 任务穿梭机（QuestScriptDef驱动）

任务系统通过`QuestGen_Shuttle`生成穿梭机，设置`requiredPawns`/`requiredItems`约束：

```
QuestScriptDef生成穿梭机
  → QuestGen_Shuttle.GenerateShuttle()
    ├── 创建Shuttle + TransportShip
    ├── compShuttle.requiredPawns = [指定Pawn列表]
    ├── compShuttle.requiredItems = [指定物品列表]
    ├── compShuttle.acceptColonists = true/false
    └── AddJobs(WaitSendable/WaitTime, FlyAway)
        └── WaitSendable.destination = 任务目标地点
```

**任务相关QuestPart**（6个）：
- `QuestPart_ShuttleLeaveDelay`：延迟后强制穿梭机离开
- `QuestPart_ShuttleDelay`：延迟后触发穿梭机到达
- `QuestPart_RequiredShuttleThings`：设置必需装载物
- `QuestPart_SendShuttleAway`：信号触发穿梭机飞走
- `QuestPart_ExitOnShuttle`：Pawn通过穿梭机离开
- `QuestPart_RequirePawnsCurrentlyOnShuttle`：检查Pawn是否在穿梭机上

### 3.3 玩家穿梭机（Odyssey DLC）

```
玩家建造 PassengerShuttle（需研究Shuttles）
  → Building_PassengerShuttle.SpawnSetup()
    └── ShuttleComp.shipParent.Start()    ← 启动TransportShip
  → 玩家装载Pawn/物品到CompTransporter
  → CompLaunchable.CompGetGizmosExtra()   ← 提供发射Gizmo
    └── 选择世界地图目标 → TryLaunch()
      ├── 消耗燃料（fuelPerTile × 距离）
      ├── 生成PassengerShuttleLeaving Skyfaller
      ├── 创建TravellingTransporters WorldObject
      └── 到达后 → TransportersArrivalAction_TransportShip
          └── 穿梭机在目标地图着陆（保留实体，可再次发射）
```

**玩家穿梭机特有机制**：
- **燃料系统**：CompRefuelable（Chemfuel，容量400，fuelPerTile=3，minFuelCost=50）
- **从货舱加油**：Building_PassengerShuttle提供专用Gizmo，从CompTransporter.innerContainer中取出Chemfuel加油
- **发射冷却**：cooldownTicks=3750（约1.5小时）
- **固定最大距离**：fixedLaunchDistanceMax=62格
- **可命名**：实现IRenameable接口

## 4. CompShuttle装载约束系统

CompShuttle管理穿梭机的装载规则，决定什么可以装、什么必须装。

### 4.1 核心约束字段

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `requiredItems` | List&lt;ThingDefCount&gt; | 空 | 必须装载的物品列表 |
| `requiredPawns` | List&lt;Pawn&gt; | 空 | 必须装载的Pawn列表 |
| `requiredColonistCount` | int | 0 | 必须装载的殖民者数量 |
| `maxColonistCount` | int | 0 | 最大殖民者数量（0=无限） |
| `acceptColonists` | bool | false | 是否接受殖民者 |
| `acceptChildren` | bool | false | 是否接受儿童 |
| `acceptColonyPrisoners` | bool | false | 是否接受囚犯 |
| `onlyAcceptColonists` | bool | false | 是否只接受殖民者 |
| `onlyAcceptHealthy` | bool | false | 是否只接受健康Pawn |
| `permitShuttle` | bool | false | 是否为许可穿梭机（接受所有玩家Pawn/物品） |
| `allowSlaves` | bool | false | 是否接受奴隶 |
| `minAge` | float | 0 | 最小年龄限制 |
| `autoload` | bool | false | 是否自动装载 |

### 4.2 IsAllowed判定逻辑（简化）

```
IsAllowed(Thing t):
  1. playerShuttle=true → 全部允许（玩家穿梭机无限制）
  2. IsRequired(t) → 允许（必需物品/Pawn）
  3. acceptColonists + Pawn检查链：
     IsColonist || (IsPrisoner && acceptColonyPrisoners) || (IsAnimal && PlayerFaction)
     && (!IsSlave || allowSlaves)
     && (!onlyAcceptColonists || !IsQuestLodger)
     && (!onlyAcceptHealthy || PawnIsHealthyEnoughForShuttle)
     && age >= minAge
  4. !acceptChildren + 人形儿童 → 拒绝
  5. permitShuttle → 接受玩家Pawn和物品
  6. 非玩家据点+无敌对威胁 → 接受非Pawn物品
```

### 4.3 自动装载机制（CheckAutoload）

CompShuttle每120 ticks检查自动装载：
1. 计算还缺什么（requiredItems/requiredPawns - 已装载）
2. 从地图上搜索可发送的Pawn和物品（`TransporterUtility.AllSendablePawns/Items`）
3. 将缺少的物品添加到`Transporter.leftToLoad`列表
4. 创建Lord让Pawn自动前往穿梭机装载

**超时卸载**：CompTick每600 ticks检查，Pawn在穿梭机内超过60000 ticks（约1天）自动卸载。

## 5. 帝国穿梭机 vs 玩家穿梭机 vs 运输舱 对比

| 维度 | 帝国穿梭机（Royalty） | 玩家穿梭机（Odyssey） | 运输舱（Core） |
|------|---------------------|---------------------|--------------|
| **DLC** | Royalty | Odyssey | Core |
| **获取方式** | 皇室许可召唤 | 建造（Steel 250+Plasteel 150+ShuttleEngine） | 建造（Steel 60+Component 1） |
| **核心Comp** | CompShuttle + CompTransporter | CompShuttle + CompTransporter + CompLaunchable + CompRefuelable | CompTransporter + CompLaunchable |
| **thingClass** | Building | Building_PassengerShuttle | Building |
| **可重用** | 否（任务结束飞走） | 是（着陆后保留，可再次发射） | 否（发射后消耗） |
| **燃料** | 无（无限） | Chemfuel（容量400，3/格） | Chemfuel（150，2.25/格） |
| **最大距离** | 70格（TransportShipDef） | 62格（CompLaunchable） | 由燃料决定（~66格满油） |
| **装载约束** | 任务系统设置required | playerShuttle=true（无限制） | 无约束（自由装载） |
| **行为驱动** | ShipJob状态机 | ShipJob + CompLaunchable | CompLaunchable直接发射 |
| **质量容量** | 2000 | 500 | 150（每舱） |
| **尺寸** | 5×3 | 3×5 | 1×1 |
| **着陆方式** | Skyfaller天降 | Skyfaller天降 | Skyfaller天降 |
| **世界地图飞行** | TravelingShuttle | PassengerShuttle | TravellingTransporters |
| **任务集成** | 深度（6个QuestPart） | 无 | 无 |

## 6. ShipJob各子类核心逻辑

### 6.1 ShipJob_Arrive（着陆）

```csharp
TryStart():
  mapParent = mapOfPawn?.MapHeld?.Parent ?? this.mapParent ?? AnyPlayerHomeMap
  if (!cell.IsValid)
    cell = DropCellFinder.GetBestShuttleLandingSpot(map, faction)
  // 将世界Pawn从WorldPawns移除
  GenSpawn.Spawn(SkyfallerMaker.MakeSkyfaller(arrivingSkyfaller, shipThing), cell, map)
  QuestUtility.SendQuestTargetSignals(questTags, "Arrived")
```

- `ShouldEnd`：穿梭机实体已Spawned时结束（Skyfaller着陆完成）
- `Interruptible = false`：着陆过程不可打断

### 6.2 ShipJob_FlyAway（起飞）

```csharp
TryStart():
  // 未满足装载要求时，先插入Unload Job卸载多余物品
  if (!AllRequiredThingsLoaded && dropMode != None && hasContents)
    transportShip.SetNextJob(Unload)  // 插入卸载Job
    return false
  // 发送信号
  ShuttleComp.SendLaunchedSignals()  // "SentSatisfied"/"SentUnsatisfied"
  QuestUtility.SendQuestTargetSignals(questTags, "FlewAway")
  // 创建ActiveTransporter，转移内容物
  // DeSpawn穿梭机实体
  // 生成FlyShipLeaving Skyfaller
  if (destinationTile.Valid)
    flyShipLeaving.createWorldObject = true  // 创建世界地图飞行物
  else
    flyShipLeaving.createWorldObject = false // 无目的地，直接消失
  // 保留穿梭机实体引用（SetShuttle），到达后重新Spawn
```

- `Interruptible = false`：起飞过程不可打断
- **关键设计**：`activeTransporter.Contents.SetShuttle(shipThing)` 保存穿梭机实体引用，使穿梭机到达目的地后可以重新生成（而非像运输舱那样消耗）

### 6.3 ShipJob_Unload（卸载）

每60 ticks卸载一个物品/Pawn（按优先级：Pawn优先）：
- 卸载到穿梭机的InteractionCell附近
- 殖民者在非玩家据点卸载后自动征召
- 囚犯卸载后设置等待逃跑延迟
- `unforbidAll`控制卸载物品是否自动解除禁止

### 6.4 ShipJob_WaitSendable（等待发送）

任务穿梭机的核心等待模式——满足装载条件后自动飞往目的地：
```csharp
SendAway():
  // 如果targetPlayerSettlement且无目的地，自动选择殖民者最多的玩家据点
  ShipJob_FlyAway flyAway = MakeShipJob(FlyAway)
  flyAway.destinationTile = destination.Tile
  flyAway.arrivalAction = TransportersArrivalAction_TransportShip(destination, this)
  flyAway.dropMode = None  // 不卸载（保留内容物到目的地）
```

## 7. Shuttle ThingDef XML配置实例

### 7.1 帝国穿梭机（Core定义，Royalty使用）

```xml
<ThingDef ParentName="ShuttleBase">
  <defName>Shuttle</defName>
  <label>imperial shuttle</label>
  <thingClass>Building</thingClass>
  <size>(5,3)</size>
  <statBases>
    <MaxHitPoints>1000</MaxHitPoints>
    <Comfort>0.65</Comfort>
  </statBases>
  <comps>
    <li Class="CompProperties_Shuttle" />  <!-- 无shipDef → 非玩家穿梭机 -->
    <li Class="CompProperties_Transporter">
      <massCapacity>2000</massCapacity>
      <max1PerGroup>true</max1PerGroup>
      <canChangeAssignedThingsAfterStarting>true</canChangeAssignedThingsAfterStarting>
    </li>
    <li Class="CompProperties_AmbientSound">
      <sound>ShuttleIdle_Ambience</sound>
    </li>
  </comps>
</ThingDef>
```

### 7.2 玩家穿梭机（Odyssey）

```xml
<ThingDef ParentName="BuildingBase">
  <defName>PassengerShuttle</defName>
  <label>passenger shuttle</label>
  <thingClass>Building_PassengerShuttle</thingClass>
  <size>(3,5)</size>
  <designationCategory>Odyssey</designationCategory>
  <researchPrerequisites><li>Shuttles</li></researchPrerequisites>
  <costList>
    <Steel>250</Steel><Plasteel>150</Plasteel>
    <ComponentIndustrial>6</ComponentIndustrial><ShuttleEngine>1</ShuttleEngine>
  </costList>
  <comps>
    <li Class="CompProperties_Shuttle">
      <shipDef>Ship_PassengerShuttle</shipDef>  <!-- playerShuttle=true -->
    </li>
    <li Class="CompProperties_Launchable">
      <fuelPerTile>3</fuelPerTile>
      <minFuelCost>50</minFuelCost>
      <fixedLaunchDistanceMax>62</fixedLaunchDistanceMax>
      <skyfallerLeaving>PassengerShuttleLeaving</skyfallerLeaving>
      <worldObjectDef>PassengerShuttle</worldObjectDef>
      <cooldownTicks>3750</cooldownTicks>
    </li>
    <li Class="CompProperties_Transporter">
      <massCapacity>500</massCapacity>
    </li>
    <li Class="CompProperties_Refuelable">
      <fuelCapacity>400</fuelCapacity>
      <fuelFilter><thingDefs><li>Chemfuel</li></thingDefs></fuelFilter>
      <consumeFuelOnlyWhenUsed>true</consumeFuelOnlyWhenUsed>
    </li>
  </comps>
</ThingDef>
```

## 8. 关键源码引用表

| 类 | 命名空间 | 职责 |
|----|---------|------|
| `TransportShip` | RimWorld | 穿梭机运行时实例（ShipJob队列+Tick驱动） |
| `TransportShipDef` | RimWorld | 穿梭机定义（6字段：shipThing/skyfaller/worldObject/maxDist/playerShuttle） |
| `TransportShipMaker` | RimWorld | 工厂方法（创建TransportShip+绑定CompShuttle.shipParent） |
| `CompShuttle` | RimWorld | 穿梭机核心Comp（装载约束/自动装载/Gizmo/信号发送） |
| `CompProperties_Shuttle` | RimWorld | 穿梭机Comp配置（仅1字段：shipDef） |
| `CompTransporter` | RimWorld | 物品容器Comp（innerContainer/leftToLoad/装载Lord） |
| `CompLaunchable` | RimWorld | 发射Comp（燃料计算/距离限制/世界地图目标选择） |
| `ShipJob` | RimWorld | ShipJob基类（TryStart/End/TickInterval/GetJobGizmos） |
| `ShipJob_Arrive` | RimWorld | 着陆Job（Skyfaller生成+位置选择） |
| `ShipJob_FlyAway` | RimWorld | 起飞Job（内容物转移+Skyfaller+世界地图飞行物） |
| `ShipJob_Wait` | RimWorld | 等待Job抽象基类（Gizmo提供：发射/遣返/发送） |
| `ShipJob_WaitForever` | RimWorld | 永久等待（许可穿梭机，直到被摧毁） |
| `ShipJob_WaitTime` | RimWorld | 定时等待（duration ticks后结束） |
| `ShipJob_WaitSendable` | RimWorld | 等待发送（满足条件后自动飞往destination） |
| `ShipJob_Unload` | RimWorld | 卸载Job（每60 ticks卸载一个，按优先级） |
| `Building_PassengerShuttle` | RimWorld | Odyssey玩家穿梭机建筑（IRenameable+从货舱加油Gizmo） |
| `RoyalTitlePermitWorker_CallShuttle` | RimWorld | 皇室许可召唤穿梭机（创建+着陆+排队ShipJob） |
| `QuestGen_Shuttle` | RimWorld.QuestGen | 任务系统穿梭机生成工具 |
| `TransportersArrivalAction_TransportShip` | RimWorld | 穿梭机到达行为（重新Spawn穿梭机实体） |

---

## 历史修改记录

| 版本 | 日期 | 修改摘要 | 签名 |
|------|------|---------|------|
| v1.0 | 2026-02-15 | 初始版本：三层架构TransportShipDef→TransportShip→CompShuttle+CompTransporter、ShipJob状态机4子类7个ShipJobDef、帝国穿梭机许可召唤流程+任务系统6个QuestPart、玩家穿梭机Odyssey Building_PassengerShuttle+CompLaunchable+CompRefuelable、CompShuttle装载约束系统13字段+IsAllowed判定链+自动装载CheckAutoload、帝国vs玩家vs运输舱12维度对比、ShipJob各子类核心逻辑（Arrive着陆+FlyAway起飞SetShuttle保留实体+Unload卸载+WaitSendable自动发送）、XML配置实例2个 | Claude Opus 4.6 |
