$ErrorActionPreference = 'Stop'

# 隔离原版宿主，编译正式地图许可和恢复门控源码；验证状态与 XML 配置，不模拟完整游戏。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$facilityRoot = Join-Path $sourceRoot 'BDP.Content\Trion\Facility'
$sleepConfigPath = Join-Path (Split-Path -Parent $sourceRoot) '1.6\Content\Defs\TrionRecoverySettingsDef\Settings.xml'
$stubPath = Join-Path ([IO.Path]::GetTempPath()) ('bdp-trion-permission-' + [Guid]::NewGuid().ToString('N') + '.cs')
$stubs = @'
using System;
using System.Collections.Generic;
using System.Linq;
namespace RimWorld
{
    /// <summary>隔离原版定义引用绑定标记。</summary>
    public class DefOfAttribute : Attribute { }
    /// <summary>测试定义引用初始化入口；实际定义由 XML 测试宿主赋值。</summary>
    public static class DefOfHelper
    {
        /// <summary>忽略原版全局定义数据库初始化。</summary>
        public static void EnsureInitializedInCtor(Type type) { }
    }
    /// <summary>测试派系身份。</summary>
    public class Faction
    {
        /// <summary>固定玩家派系。</summary>
        public static readonly Faction OfPlayer = new Faction();
    }
}
namespace Verse
{
    /// <summary>测试原版定义基类。</summary>
    public class Def
    {
        /// <summary>XML 定义的唯一名称。</summary>
        public string defName;
    }
    /// <summary>测试地图宿主。</summary>
    public class Map
    {
        /// <summary>诊断使用的地图标识。</summary>
        public int uniqueID;
        /// <summary>测试建筑列表。</summary>
        public ListerBuildings listerBuildings = new ListerBuildings();
        /// <summary>测试组件注册表。</summary>
        public Dictionary<Type, object> components = new Dictionary<Type, object>();
        /// <summary>读取已登记组件。</summary>
        public T GetComponent<T>() where T : class { object value; return components.TryGetValue(typeof(T), out value) ? value as T : null; }
    }
    /// <summary>测试建筑列表。</summary>
    public class ListerBuildings
    {
        /// <summary>地图上的全部测试建筑。</summary>
        public List<Building> buildings = new List<Building>();
        /// <summary>模拟原版玩家建筑枚举。</summary>
        public IEnumerable<T> AllBuildingsColonistOfClass<T>() where T : Building { return buildings.OfType<T>().Where(x => x.Faction == RimWorld.Faction.OfPlayer); }
    }
    /// <summary>测试地图组件生命周期。</summary>
    public class MapComponent
    {
        /// <summary>所属地图。</summary>
        protected Map map;
        /// <summary>关联地图。</summary>
        public MapComponent(Map value) { map = value; }
        /// <summary>初始化回调。</summary>
        public virtual void FinalizeInit() { }
        /// <summary>存读档回调。</summary>
        public virtual void ExposeData() { }
    }
    /// <summary>测试建筑状态。</summary>
    public class Building
    {
        /// <summary>所属地图。</summary>
        public Map Map;
        /// <summary>是否已经生成。</summary>
        public bool Spawned = true;
        /// <summary>是否销毁。</summary>
        public bool Destroyed;
        /// <summary>所属派系。</summary>
        public RimWorld.Faction Faction;
    }
    /// <summary>测试角色身份与原版任务睡眠标记。</summary>
    public class Pawn : Building
    {
        /// <summary>当前角色的任务跟踪器。</summary>
        public Verse.AI.Pawn_JobTracker jobs = new Verse.AI.Pawn_JobTracker();
    }
    /// <summary>测试读档阶段。</summary>
    public enum LoadSaveMode { Inactive, PostLoadInit }
    /// <summary>测试读档状态。</summary>
    public static class Scribe
    {
        /// <summary>当前模拟读档阶段。</summary>
        public static LoadSaveMode mode;
    }
    /// <summary>测试游戏时间。</summary>
    public class TickManager
    {
        /// <summary>当前模拟游戏刻。</summary>
        public int TicksGame;
    }
    /// <summary>测试时间入口。</summary>
    public static class Find
    {
        /// <summary>唯一模拟时间宿主。</summary>
        public static TickManager TickManager = new TickManager();
    }
}
namespace Verse.AI
{
    /// <summary>测试原版当前任务的实际睡眠状态。</summary>
    public class JobDriver
    {
        /// <summary>是否真正睡着，而非仅躺下或无法清醒。</summary>
        public bool asleep;
    }
    /// <summary>测试原版角色任务跟踪器。</summary>
    public class Pawn_JobTracker
    {
        /// <summary>当前任务执行器。</summary>
        public JobDriver curDriver = new JobDriver();
    }
}
namespace BDP.Support.Diagnostics
{
    /// <summary>隔离日志输出。</summary>
    public static class BdpDiagnostics
    {
        /// <summary>忽略一次性诊断。</summary>
        public static void Once(string key, string text) { }
        /// <summary>忽略节流诊断。</summary>
        public static void Throttled(string key, string text, int ticks) { }
    }
}
namespace BDP.Core.Permissions
{
    /// <summary>观察正式地图组件发出的通知。</summary>
    public class MapPermissionMapComponent
    {
        /// <summary>通知次数。</summary>
        public int notifications;
        /// <summary>登记提供器；此测试只验证其状态读取。</summary>
        public void Register(IMapPermissionProvider provider) { }
        /// <summary>记录聚合许可翻转。</summary>
        public void NotifyPermissionStateChanged(string key, RimWorld.Faction faction) { notifications++; }
    }
}
namespace BDP.Content.Trion.Facility
{
    /// <summary>测试资源宿主，捕获不必要的查询结算。</summary>
    public class CompTrionFacilityResource
    {
        /// <summary>建筑当前运作状态。</summary>
        public bool IsOperational;
        /// <summary>查询触发的结算次数。</summary>
        public int settlements;
        /// <summary>观察地图完成初始化后是否重建核心快照。</summary>
        public int initializations;
        /// <summary>隔离绑定业务；地图只调用核心自身初始化，不按查询结算资源。</summary>
        public void InitializeOperation() { initializations++; }
        /// <summary>观察查询是否调用结算。</summary>
        public void SettleResourceToTick(int tick) { settlements++; }
    }
    /// <summary>测试建筑载体。</summary>
    public class Building_TrionFacility : Verse.Building
    {
        /// <summary>独立资源宿主。</summary>
        public CompTrionFacilityResource ResourceComp = new CompTrionFacilityResource();
    }
}
'@

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Multiplier($Gate, $Pawn, [float]$Expected, [string]$Message) {
    $actual = $Gate.GetRecoveryMultiplier($Pawn)
    Assert-True ([Math]::Abs($actual - $Expected) -lt 0.00001) "$Message 实际倍率：$actual，预期：$Expected。"
}

function Notify-CoreState($Component, $Building) {
    $method = $Component.GetType().GetMethod('NotifyOperationalStateChanged')
    Assert-True ($null -ne $method) '地图许可必须提供核心状态变化入口。'
    [void]$method.Invoke($Component, @($Building))
}

try {
    [IO.File]::WriteAllText($stubPath, $stubs, [Text.UTF8Encoding]::new($false))
    Add-Type -Path @(
        $stubPath,
        (Join-Path $sourceRoot 'BDP\Core\Permissions\IMapPermissionProvider.cs'),
        (Join-Path $sourceRoot 'BDP\Core\Trion\ITrionRecoveryGate.cs'),
        (Join-Path $sourceRoot 'BDP\Core\Trion\TrionRecoveryGateRegistry.cs'),
        (Join-Path $facilityRoot 'TrionFacilityMapComponent.cs'),
        (Join-Path $facilityRoot 'TrionRelayCorePermissionProvider.cs'),
        (Join-Path $facilityRoot 'TrionRecoverySettingsDef.cs'),
        (Join-Path $facilityRoot 'TrionRecoveryFacilityGate.cs'))

    # 从正式 XML 读取倍率，避免测试宿主另抄一套默认配置。
    [xml]$sleepConfig = Get-Content -LiteralPath $sleepConfigPath -Raw -Encoding utf8
    $configNodes = $sleepConfig.SelectNodes('/Defs/BDP.Content.Trion.Facility.TrionRecoverySettingsDef')
    Assert-True ($configNodes.Count -eq 1) '独立睡眠配置文件必须提供唯一的正式配置定义。'
    $config = $configNodes[0]
    $settings = [BDP.Content.Trion.Facility.TrionRecoverySettingsDef]::new()
    $settings.defName = $config.defName
    $settings.sleepWithoutCoreMultiplier = [float]::Parse($config.sleepWithoutCoreMultiplier, [Globalization.CultureInfo]::InvariantCulture)
    $settings.sleepWithCoreMultiplier = [float]::Parse($config.sleepWithCoreMultiplier, [Globalization.CultureInfo]::InvariantCulture)
    $defOfField = [BDP.Content.Trion.Facility.TrionRecoverySettingsDefOf].GetField($settings.defName)
    Assert-True ($null -ne $defOfField) '配置名称必须对应正式定义引用字段。'
    $defOfField.SetValue($null, $settings)

    $map = [Verse.Map]::new()
    $component = [BDP.Content.Trion.Facility.TrionFacilityMapComponent]::new($map)
    $permission = [BDP.Core.Permissions.MapPermissionMapComponent]::new()
    $map.components[$component.GetType()] = $component
    $map.components[$permission.GetType()] = $permission
    $first = [BDP.Content.Trion.Facility.Building_TrionFacility]::new()
    $first.Map = $map
    $first.Faction = [RimWorld.Faction]::OfPlayer
    $first.ResourceComp.IsOperational = $true
    $map.listerBuildings.buildings.Add($first)
    $component.Register($first)
    $provider = [BDP.Content.Trion.Facility.TrionRelayCorePermissionProvider]::new($component)
    1..100 | ForEach-Object { Assert-True $provider.IsActive '运行核心必须提供许可。' }
    Assert-True ($first.ResourceComp.settlements -eq 0) '许可读取不能触发资源结算。'

    $second = [BDP.Content.Trion.Facility.Building_TrionFacility]::new()
    $second.Map = $map
    $second.Faction = [RimWorld.Faction]::OfPlayer
    $second.ResourceComp.IsOperational = $true
    $map.listerBuildings.buildings.Add($second)
    $before = $permission.notifications
    $component.Register($second)
    $first.ResourceComp.IsOperational = $false
    Notify-CoreState $component $first
    Assert-True $provider.IsActive '停运一座核心不能关闭另一座核心的许可。'
    Assert-True ($permission.notifications -eq $before) '聚合许可未翻转时不能重复通知下游。'
    $component.Unregister($second)
    Assert-True (-not $provider.IsActive) '移除最后一座运行核心必须关闭许可。'
    Assert-True ($permission.notifications -eq $before + 1) '最后一座核心移除必须通知下游一次。'
    $second.Spawned = $false
    $second.Map = $null
    $map.listerBuildings.buildings.Remove($second) | Out-Null

    $first.ResourceComp.IsOperational = $true
    $first.Faction = [RimWorld.Faction]::new()
    Notify-CoreState $component $first
    Assert-True (-not $provider.IsActive) '非玩家核心不能发布玩家许可。'
    $first.Faction = [RimWorld.Faction]::OfPlayer
    Notify-CoreState $component $first
    Assert-True $provider.IsActive '核心归属玩家后必须恢复许可。'

    $gate = [BDP.Content.Trion.Facility.TrionRecoveryFacilityGate]::new()
    $pawn = [Verse.Pawn]::new()
    $pawn.Map = $map
    $pawn.Faction = [RimWorld.Faction]::OfPlayer
    Assert-Multiplier $gate $pawn 1 '有核心许可且清醒时保持基准恢复率。'
    $pawn.jobs.curDriver.asleep = $true
    Assert-Multiplier $gate $pawn $settings.sleepWithCoreMultiplier '核心许可与睡眠同时存在时使用对应配置。'
    $pawn.Map = [Verse.Map]::new()
    Assert-Multiplier $gate $pawn $settings.sleepWithoutCoreMultiplier '角色换到无许可地图后，睡眠独立提供恢复。'
    $pawn.jobs.curDriver.asleep = $false
    Assert-Multiplier $gate $pawn 0 '无核心且清醒时停止恢复，不能沿用旧地图许可。'

    # 同地图核心停运与唤醒都必须即时影响下一次倍率读取。
    $pawn.Map = $map
    $first.ResourceComp.IsOperational = $false
    Notify-CoreState $component $first
    Assert-Multiplier $gate $pawn 0 '当前地图最后一座核心停运后，清醒角色停止恢复。'
    $pawn.jobs.curDriver.asleep = $true
    Assert-Multiplier $gate $pawn $settings.sleepWithoutCoreMultiplier '停运期间仍可依靠实际睡眠恢复。'
    $first.ResourceComp.IsOperational = $true
    Notify-CoreState $component $first
    Assert-Multiplier $gate $pawn $settings.sleepWithCoreMultiplier '睡眠中核心恢复运行后切换到有许可倍率。'

    # 修改配置后的结果必须同步变化，证明倍率来自配置而非门控代码常量。
    $defaultWithoutCore = $settings.sleepWithoutCoreMultiplier
    $defaultWithCore = $settings.sleepWithCoreMultiplier
    $settings.sleepWithCoreMultiplier = 1.7
    Assert-Multiplier $gate $pawn 1.7 '有许可睡眠倍率必须读取可调整配置。'
    $pawn.Map = [Verse.Map]::new()
    $settings.sleepWithoutCoreMultiplier = 0.35
    Assert-Multiplier $gate $pawn 0.35 '无许可睡眠倍率必须读取可调整配置。'
    $settings.sleepWithoutCoreMultiplier = $defaultWithoutCore
    $settings.sleepWithCoreMultiplier = $defaultWithCore

    $pawn.jobs.curDriver = $null
    Assert-Multiplier $gate $pawn 0 '没有任务执行器不能被视为睡眠。'
    $pawn.jobs = $null
    Assert-Multiplier $gate $pawn 0 '没有任务跟踪器不能被视为睡眠。'
    $pawn.Faction = [RimWorld.Faction]::new()
    Assert-Multiplier $gate $pawn 1 '非玩家派系保持原有放行规则。'
    $pawn.Faction = [RimWorld.Faction]::OfPlayer
    $pawn.Map = $null
    Assert-Multiplier $gate $pawn 1 '无地图角色保持原有放行规则。'
    Assert-Multiplier $gate $null 1 '非角色宿主保持原有放行规则。'

    Assert-True ([BDP.Core.Trion.TrionRecoveryGateRegistry]::GetRecoveryMultiplier($pawn) -eq 1) '未注册内容门控时，Core 保持基准恢复率。'
    [BDP.Core.Trion.TrionRecoveryGateRegistry]::Register($gate)
    $pawn.Map = $map
    Assert-True ([BDP.Core.Trion.TrionRecoveryGateRegistry]::GetRecoveryMultiplier($pawn) -eq 1) '注册表必须读取内容层倍率。'
    [BDP.Core.Trion.TrionRecoveryGateRegistry]::Clear([BDP.Content.Trion.Facility.TrionRecoveryFacilityGate]::new())
    $pawn.Map = [Verse.Map]::new()
    Assert-True ([BDP.Core.Trion.TrionRecoveryGateRegistry]::GetRecoveryMultiplier($pawn) -eq 0) '旧实例清理不能移除当前门控。'
    [BDP.Core.Trion.TrionRecoveryGateRegistry]::Clear($gate)
    Assert-True ([BDP.Core.Trion.TrionRecoveryGateRegistry]::GetRecoveryMultiplier($pawn) -eq 1) '卸载当前门控后必须恢复独立运行的基准速率。'

    [Verse.Scribe]::mode = [Verse.LoadSaveMode]::PostLoadInit
    $component.ExposeData()
    $component.FinalizeInit()
    Assert-True $provider.IsActive '读档初始化必须从当前建筑状态重建许可。'
    Assert-True ($first.ResourceComp.initializations -eq 1) '读档须在地图完成初始化后重建核心运行快照。'
    Assert-True ($first.ResourceComp.settlements -eq 0) '重建许可不能借查询结算资源。'
    Write-Output 'TrionFacilityPermissionState PASS：四种恢复组合、配置调整、状态切换、门控注册、只读许可、多核心、拆除、派系、换图、读档。'
}
finally {
    Remove-Item -LiteralPath $stubPath -ErrorAction SilentlyContinue
}
