$ErrorActionPreference = 'Stop'

# 隔离原版宿主，只编译两台真实建筑源码；制造检查必须走原版账单接口分派。
$sourceRoot = Split-Path -Parent $PSScriptRoot
$buildingRoot = Join-Path $sourceRoot 'BDP.Content\Assembly\Building'
$stubPath = Join-Path ([IO.Path]::GetTempPath()) ('bdp-assembly-permission-' + [Guid]::NewGuid().ToString('N') + '.cs')
$failures = [Collections.Generic.List[string]]::new()
$stubs = @'
using System;
using System.Collections.Generic;
namespace Verse
{
    /// <summary>隔离地图，仅提供可切换的许可状态。</summary>
    public class Map
    {
        /// <summary>本地图的核心是否提供许可。</summary>
        public bool CorePermission;
    }
    /// <summary>隔离建筑定义，满足真实建筑的命令显示依赖。</summary>
    public class ThingDef
    {
        /// <summary>建筑说明。</summary>
        public string description;
        /// <summary>命令图标。</summary>
        public object uiIcon;
    }
    /// <summary>隔离建筑宿主；不替代被测建筑的准入逻辑。</summary>
    public class Building
    {
        /// <summary>建筑所属地图。</summary>
        public Map Map;
        /// <summary>建筑所属派系。</summary>
        public RimWorld.Faction Faction = RimWorld.Faction.OfPlayer;
        /// <summary>建筑是否已经销毁。</summary>
        public bool Destroyed;
        /// <summary>建筑是否已生成。</summary>
        public bool Spawned = true;
        /// <summary>建筑定义。</summary>
        public ThingDef def = new ThingDef();
        /// <summary>隔离电力组件。</summary>
        public RimWorld.CompPowerTrader PowerComp = new RimWorld.CompPowerTrader();
        /// <summary>隔离基础命令枚举。</summary>
        public virtual IEnumerable<Gizmo> GetGizmos() { yield break; }
        /// <summary>隔离基础右键菜单。</summary>
        public virtual IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn) { yield break; }
    }
    /// <summary>隔离角色，保留装配所需的可达与任务入口。</summary>
    public class Pawn : Building
    {
        /// <summary>角色任务宿主。</summary>
        public Verse.AI.Pawn_JobTracker jobs = new Verse.AI.Pawn_JobTracker();
        /// <summary>测试角色可以到达建筑，排除无关拒绝原因。</summary>
        public bool CanReach(Building building, Verse.AI.PathEndMode mode, Danger danger) { return true; }
    }
    /// <summary>隔离可达危险等级。</summary>
    public enum Danger { Deadly }
    /// <summary>隔离命令基类。</summary>
    public class Gizmo { }
    /// <summary>隔离真实建筑编译所需的命令载体。</summary>
    public class Command_Action : Gizmo
    {
        /// <summary>命令名称。</summary>
        public string defaultLabel;
        /// <summary>命令说明。</summary>
        public string defaultDesc;
        /// <summary>命令图标。</summary>
        public object icon;
        /// <summary>命令动作；本测试不调用界面动作。</summary>
        public Action action;
    }
    /// <summary>隔离右键菜单载体。</summary>
    public class FloatMenuOption
    {
        /// <summary>仅满足真实建筑编译，不替代菜单准入。</summary>
        public FloatMenuOption(string label, Action action) { }
    }
    /// <summary>隔离本地化和组件访问。</summary>
    public static class TestExtensions
    {
        /// <summary>保留语言键，方便识别真实拒绝原因。</summary>
        public static string Translate(this string key, params object[] arguments) { return key; }
        /// <summary>沿用原版字符串为空的语义。</summary>
        public static bool NullOrEmpty(this string value) { return string.IsNullOrEmpty(value); }
        /// <summary>只返回隔离电力组件。</summary>
        public static T TryGetComp<T>(this Building building) where T : class { return building.PowerComp as T; }
    }
    /// <summary>隔离窗口栈。</summary>
    public class WindowStack
    {
        /// <summary>真实建筑的界面动作不在本测试执行。</summary>
        public void Add(object window) { }
    }
    /// <summary>隔离界面全局入口。</summary>
    public static class Find
    {
        /// <summary>唯一隔离窗口栈。</summary>
        public static WindowStack WindowStack = new WindowStack();
    }
    /// <summary>隔离开发模式。</summary>
    public static class DebugSettings
    {
        /// <summary>测试关闭开发命令。</summary>
        public static bool godMode;
    }
    /// <summary>隔离原版消息。</summary>
    public static class Messages
    {
        /// <summary>真实建筑的调试动作不在本测试执行。</summary>
        public static void Message(string text, Building target, object type, bool historical) { }
    }
    /// <summary>隔离任务定义。</summary>
    public class JobDef { }
}
namespace RimWorld
{
    /// <summary>隔离派系身份。</summary>
    public class Faction
    {
        /// <summary>固定玩家派系。</summary>
        public static readonly Faction OfPlayer = new Faction();
    }
    /// <summary>保留真实原版制造准入的两项接口签名。</summary>
    public interface IBillGiver
    {
        /// <summary>搬料与加工中的可用性检查。</summary>
        bool CurrentlyUsableForBills();
        /// <summary>分配制造任务前的可用性检查。</summary>
        bool UsableForBillsAfterFueling();
    }
    /// <summary>保留原版非虚方法，暴露可切换的原版拒绝状态。</summary>
    public class Building_WorkTable : Verse.Building, IBillGiver
    {
        /// <summary>原版运行中检查的结果。</summary>
        public bool VanillaCurrentlyUsable = true;
        /// <summary>原版分配任务前检查的结果。</summary>
        public bool VanillaUsableAfterFueling = true;
        /// <summary>刻意保持非虚方法，避免测试掩盖接口重新实现的缺失。</summary>
        public bool CurrentlyUsableForBills() { return VanillaCurrentlyUsable; }
        /// <summary>刻意保持非虚方法，匹配真实原版账单入口。</summary>
        public bool UsableForBillsAfterFueling() { return VanillaUsableAfterFueling; }
    }
    /// <summary>隔离电力状态。</summary>
    public class CompPowerTrader
    {
        /// <summary>默认通电，防止掩盖许可拒绝。</summary>
        public bool PowerOn = true;
    }
    /// <summary>隔离原版任务菜单装饰。</summary>
    public static class FloatMenuUtility
    {
        /// <summary>菜单装饰不改变真实建筑的准入结果。</summary>
        public static Verse.FloatMenuOption DecoratePrioritizedTask(Verse.FloatMenuOption option, Verse.Pawn pawn, Verse.Building building) { return option; }
    }
    /// <summary>隔离正面消息定义。</summary>
    public static class MessageTypeDefOf
    {
        /// <summary>仅满足真实建筑调试命令编译。</summary>
        public static object PositiveEvent;
    }
}
namespace Verse.AI
{
    /// <summary>隔离建筑交互格定位。</summary>
    public enum PathEndMode { InteractionCell }
    /// <summary>隔离玩家任务标签。</summary>
    public enum JobTag { Misc }
    /// <summary>隔离任务载体。</summary>
    public class Job { }
    /// <summary>隔离任务创建入口。</summary>
    public static class JobMaker
    {
        /// <summary>真实建筑的派工动作不在本测试执行。</summary>
        public static Job MakeJob(Verse.JobDef definition, Verse.Building target) { return new Job(); }
    }
    /// <summary>隔离角色任务跟踪器。</summary>
    public class Pawn_JobTracker
    {
        /// <summary>真实建筑的派工动作不在本测试执行。</summary>
        public void TryTakeOrderedJob(Job job, JobTag tag) { }
    }
}
namespace BDP.Core.Permissions
{
    /// <summary>隔离地图许可来源；被测建筑必须传入正确地图、派系及许可键。</summary>
    public static class MapPermissionAccess
    {
        /// <summary>读取动态测试状态，保留正式许可入口签名。</summary>
        public static bool HasPermission(Verse.Map map, RimWorld.Faction faction, string key)
        {
            return map != null && map.CorePermission && faction == RimWorld.Faction.OfPlayer
                && key == BDP.Content.Trion.Facility.TrionRelayCorePermissionProvider.ActivePermissionKey;
        }
    }
}
namespace BDP.Content.Trion.Facility
{
    /// <summary>只提供生产建筑所需许可键，不编译资源或真实提供器。</summary>
    public static class TrionRelayCorePermissionProvider
    {
        /// <summary>与正式许可身份一致的预期常量。</summary>
        public const string ActivePermissionKey = "BDP.TrionRelayCore.Active";
    }
}
namespace BDP.Core.Trigger
{
    /// <summary>隔离触发体配置模式。</summary>
    public enum TriggerLoadoutControlMode { PlayerConfigurable, PlayerNonConfigurable }
    /// <summary>隔离装配准入所需的正式读取接口。</summary>
    public interface ITriggerLoadoutReader
    {
        /// <summary>当前触发体的配置模式。</summary>
        TriggerLoadoutControlMode LoadoutControlMode { get; }
    }
    /// <summary>测试角色始终装备可配置触发体。</summary>
    public class TestLoadoutReader : ITriggerLoadoutReader
    {
        /// <summary>排除固定预装物品造成的无关拒绝。</summary>
        public TriggerLoadoutControlMode LoadoutControlMode { get { return TriggerLoadoutControlMode.PlayerConfigurable; } }
    }
    /// <summary>隔离触发体服务，排除没有装备的无关拒绝。</summary>
    public static class TriggerSurfaceAccess
    {
        /// <summary>返回可配置触发体。</summary>
        public static ITriggerLoadoutReader ResolveLoadoutReader(Verse.Pawn pawn) { return new TestLoadoutReader(); }
        /// <summary>返回可用装配命令身份。</summary>
        public static object ResolveLoadoutCommands(Verse.Pawn pawn) { return new object(); }
    }
}
namespace BDP.Core.CombatBody
{
    /// <summary>隔离真实建筑需要的战斗体阶段。</summary>
    public enum CombatBodyPhase { Active }
    /// <summary>隔离战斗体只读接口。</summary>
    public interface ICombatBodyReader
    {
        /// <summary>当前阶段。</summary>
        CombatBodyPhase Phase { get; }
    }
    /// <summary>测试角色没有开启战斗体，排除无关装配拒绝。</summary>
    public static class CombatBodySurfaceAccess
    {
        /// <summary>返回未开启战斗体的宿主状态。</summary>
        public static ICombatBodyReader ResolveReader(Verse.Pawn pawn) { return null; }
    }
}
namespace BDP.Content.Assembly
{
    /// <summary>隔离装配任务定义，不运行装配事务。</summary>
    public static class AssemblyJobDefs
    {
        /// <summary>满足真实建筑菜单的任务定义依赖。</summary>
        public static Verse.JobDef UseTriggerAssembler = new Verse.JobDef();
    }
}
namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>仅满足真实建筑编译，不编译制造窗口。</summary>
    public class Window_ChipManufacturing
    {
        /// <summary>界面行为不在本测试范围。</summary>
        public Window_ChipManufacturing(BDP.Content.Assembly.Building_ChipFabricator building) { }
    }
}
namespace BDP.Content.Assembly.ChipManufacturing.Debug
{
    /// <summary>隔离真实建筑编译需要的调试汇总载体。</summary>
    public class ChipFabricatorDebugCompletionReport
    {
        /// <summary>处理的账单数。</summary>
        public int EncounteredBillCount;
        /// <summary>产生的芯片数。</summary>
        public int ProducedChipCount;
        /// <summary>完成的账单数。</summary>
        public int CompletedBillCount;
        /// <summary>跳过的账单数。</summary>
        public int SkippedBillCount;
    }
    /// <summary>只满足真实建筑编译，不编译完整调试服务。</summary>
    public static class ChipFabricatorDebugCompletionService
    {
        /// <summary>调试批量完成不在本测试范围。</summary>
        public static ChipFabricatorDebugCompletionReport CompleteAll(BDP.Content.Assembly.Building_ChipFabricator building) { return new ChipFabricatorDebugCompletionReport(); }
    }
}
namespace BDP.Tests
{
    /// <summary>在同一隔离程序集调用真实建筑，避免 PowerShell 绕过接口分派。</summary>
    public static class AssemblyFacilityPermissionProbe
    {
        /// <summary>模拟原版分配账单任务时的接口调用。</summary>
        public static bool CanBeginBill(BDP.Content.Assembly.Building_ChipFabricator building) { return ((RimWorld.IBillGiver)building).UsableForBillsAfterFueling(); }
        /// <summary>模拟原版任务每刻检查时的接口调用。</summary>
        public static bool CanContinueBill(BDP.Content.Assembly.Building_ChipFabricator building) { return ((RimWorld.IBillGiver)building).CurrentlyUsableForBills(); }
        /// <summary>调用真实装配统一拒绝入口，包含可达检查。</summary>
        public static string AssemblyRejection(BDP.Content.Assembly.Building_TriggerAssembler building, Verse.Pawn pawn) { return building.ResolveUseRejection(pawn, true); }
        /// <summary>调用真实装配到达后的最终准入。</summary>
        public static bool CanOpenAssembly(BDP.Content.Assembly.Building_TriggerAssembler building, Verse.Pawn pawn) { return building.CanPawnOpenAssemblyWindow(pawn); }
    }
}
'@

# 收集行为断言，单次失败也保留另一台的独立验证结果。
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $failures.Add($Message) }
}

try {
    [IO.File]::WriteAllText($stubPath, $stubs, [Text.UTF8Encoding]::new($false))
    Add-Type -Path @(
        $stubPath,
        (Join-Path $buildingRoot 'Building_ChipFabricator.cs'),
        (Join-Path $buildingRoot 'Building_TriggerAssembler.cs'))

    $map = [Verse.Map]::new()
    $fabricator = [BDP.Content.Assembly.Building_ChipFabricator]::new()
    $fabricator.Map = $map
    $assembler = [BDP.Content.Assembly.Building_TriggerAssembler]::new()
    $assembler.Map = $map
    $pawn = [Verse.Pawn]::new()
    $pawn.Map = $map

    # 原版条件均可用；无许可必须阻止制造派工、继续加工与两个装配入口。
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanBeginBill($fabricator)) '核心停运：制造接口仍允许分配新账单任务。'
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanContinueBill($fabricator)) '核心停运：制造接口仍允许已有任务继续加工。'
    Assert-True (-not [string]::IsNullOrEmpty([BDP.Tests.AssemblyFacilityPermissionProbe]::AssemblyRejection($assembler, $pawn))) '核心停运：装配统一入口没有拒绝原因。'
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanOpenAssembly($assembler, $pawn)) '核心停运：装配最终确认仍允许打开窗口。'

    # 同一建筑和角色恢复许可后应可用，避免缓存旧拒绝状态。
    $map.CorePermission = $true
    Assert-True ([BDP.Tests.AssemblyFacilityPermissionProbe]::CanBeginBill($fabricator)) '核心运行：正常制造接口不能开始任务。'
    Assert-True ([BDP.Tests.AssemblyFacilityPermissionProbe]::CanContinueBill($fabricator)) '核心运行：正常制造接口不能继续加工。'
    Assert-True ([string]::IsNullOrEmpty([BDP.Tests.AssemblyFacilityPermissionProbe]::AssemblyRejection($assembler, $pawn))) '核心运行：合格角色被装配入口拒绝。'
    Assert-True ([BDP.Tests.AssemblyFacilityPermissionProbe]::CanOpenAssembly($assembler, $pawn)) '核心运行：合格角色不能通过装配最终确认。'

    # 已使用后停运再恢复，两个入口均必须读取当前状态。
    $map.CorePermission = $false
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanBeginBill($fabricator)) '运行后停运：制造仍允许新任务。'
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanContinueBill($fabricator)) '运行后停运：制造仍允许继续加工。'
    Assert-True (-not [string]::IsNullOrEmpty([BDP.Tests.AssemblyFacilityPermissionProbe]::AssemblyRejection($assembler, $pawn))) '运行后停运：装配入口沿用旧许可。'
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanOpenAssembly($assembler, $pawn)) '运行后停运：装配最终确认沿用旧许可。'
    $map.CorePermission = $true
    Assert-True ([BDP.Tests.AssemblyFacilityPermissionProbe]::CanBeginBill($fabricator)) '核心重新运行：制造派工没有恢复。'
    Assert-True ([BDP.Tests.AssemblyFacilityPermissionProbe]::CanContinueBill($fabricator)) '核心重新运行：制造工作没有恢复。'
    Assert-True ([BDP.Tests.AssemblyFacilityPermissionProbe]::CanOpenAssembly($assembler, $pawn)) '核心重新运行：装配最终确认没有恢复。'

    # 许可只是追加条件，不能放行原版本就不能使用的工作台。
    $fabricator.VanillaCurrentlyUsable = $false
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanContinueBill($fabricator)) '核心许可不能绕过原版制造运行条件。'
    $fabricator.VanillaCurrentlyUsable = $true
    $fabricator.VanillaUsableAfterFueling = $false
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanBeginBill($fabricator)) '核心许可不能绕过原版制造派工条件。'
    $fabricator.VanillaUsableAfterFueling = $true
    $assembler.PowerComp.PowerOn = $false
    Assert-True ([BDP.Tests.AssemblyFacilityPermissionProbe]::AssemblyRejection($assembler, $pawn) -eq 'BDP_Message_TriggerAssembly_NoPower') '核心许可不能掩盖装配台停电原因。'
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanOpenAssembly($assembler, $pawn)) '核心许可不能放行未通电装配台。'
    $assembler.PowerComp.PowerOn = $true

    # 两台必须查询自己的地图与派系，不能复用别处许可。
    $fabricator.Map = [Verse.Map]::new()
    $assembler.Map = $fabricator.Map
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanBeginBill($fabricator)) '制造台换到无许可地图后仍获放行。'
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanOpenAssembly($assembler, $pawn)) '装配台换到无许可地图后仍获放行。'
    $fabricator.Map = $map
    $assembler.Map = $map
    $fabricator.Faction = [RimWorld.Faction]::new()
    $assembler.Faction = $fabricator.Faction
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanBeginBill($fabricator)) '制造台不能借用其他派系的核心许可。'
    Assert-True (-not [BDP.Tests.AssemblyFacilityPermissionProbe]::CanOpenAssembly($assembler, $pawn)) '装配台不能借用其他派系的核心许可。'

    if ($failures.Count -gt 0) { throw ('AssemblyFacilityPermissionState FAIL：' + [Environment]::NewLine + ($failures -join [Environment]::NewLine)) }
    Write-Output 'AssemblyFacilityPermissionState PASS：真实制造接口与装配统一准入、核心开关及恢复、原版拒绝、电力、地图与派系。'
}
finally {
    Remove-Item -LiteralPath $stubPath -ErrorAction SilentlyContinue
}
