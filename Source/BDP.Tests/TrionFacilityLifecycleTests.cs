using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Trion.Facility;

namespace UnityEngine
{
    /// <summary>资源计算所需的原版数学宿主。</summary>
    public static class Mathf
    {
        /// <summary>浮点上界。</summary>
        public static float Max(float a, float b) => Math.Max(a, b);
        /// <summary>整数上界。</summary>
        public static int Max(int a, int b) => Math.Max(a, b);
        /// <summary>浮点下界。</summary>
        public static float Min(float a, float b) => Math.Min(a, b);
        /// <summary>区间限制。</summary>
        public static float Clamp(float value, float min, float max) => Math.Min(max, Math.Max(min, value));
    }
    /// <summary>按钮图标宿主。</summary>
    public class Texture2D { }
}
namespace RimWorld
{
    /// <summary>原版科技档位宿主。</summary>
    public enum TechLevel { Undefined, Animal, Neolithic, Medieval, Industrial, Spacer, Ultra, Archotech }
    /// <summary>隔离原版定义绑定标记。</summary>
    public class DefOfAttribute : Attribute { }
    /// <summary>定义字段由测试配置提供。</summary>
    public static class DefOfHelper
    {
        /// <summary>测试不启动游戏定义数据库。</summary>
        public static void EnsureInitializedInCtor(Type type) { }
    }
    /// <summary>物品分组调用的原版模式宿主。</summary>
    public enum TransferAsOneMode { Normal }
    /// <summary>隔离原版聚合判断；正式代码使用真实引擎的材质、品质及组件规则。</summary>
    public static class TransferableUtility
    {
        /// <summary>只提供分组边界输入，不复制完整原版物品系统。</summary>
        public static bool TransferAsOne(Verse.Thing a, Verse.Thing b, TransferAsOneMode mode) =>
            a.def == b.def && a.Stuff == b.Stuff && a.quality == b.quality;
    }
    /// <summary>隔离原版单向数量状态；正式窗口直接使用真实引擎的数量控件。</summary>
    public class TransferableOneWay
    {
        /// <summary>聚合后的实际堆栈引用。</summary>
        public List<Verse.Thing> things = new List<Verse.Thing>();
        /// <summary>用于显示的代表实物。</summary>
        public Verse.Thing AnyThing => things.FirstOrDefault();
        /// <summary>原版选定数量。</summary>
        public int CountToTransfer { get; private set; }
        /// <summary>原版输入缓存。</summary>
        public string EditBuffer = "0";
        /// <summary>原版默认上限，正式供物组覆写为仍可用数量。</summary>
        public virtual int GetMaximumToTransfer() => things.Sum(thing => thing.stackCount);
        /// <summary>允许测试模拟列表数量变化后留下的旧选择。</summary>
        public void ForceTo(int count) { CountToTransfer = count; EditBuffer = count.ToString(); }
    }
    /// <summary>仅隔离原版容器归属；不复制搬运、分派或存档实现。</summary>
    public class CompTransporter : Verse.ThingComp
    {
        /// <summary>用于确认实际实物只属于本核心的容器。</summary>
        public List<Verse.Thing> innerContainer = new List<Verse.Thing>();
        /// <summary>原版已有装载状态，正式运行由原版组件维护。</summary>
        public bool LoadingInProgressOrReadyToLaunch;
    }
    /// <summary>玩家派系宿主。</summary>
    public class Faction
    {
        /// <summary>唯一玩家派系。</summary>
        public static Faction OfPlayer = new Faction { IsPlayer = true };
        /// <summary>是否玩家。</summary>
        public bool IsPlayer;
    }
    /// <summary>任务借住状态入口。</summary>
    public static class LodgerUtility
    {
        /// <summary>读取借住标记。</summary>
        public static bool IsQuestLodger(this Verse.Pawn pawn) => pawn.lodger;
    }
}
namespace Verse
{
    /// <summary>定义宿主。</summary>
    public class Def
    {
        /// <summary>定义标识。</summary>
        public string defName;
        /// <summary>原版配置校验宿主。</summary>
        public virtual IEnumerable<string> ConfigErrors() { yield break; }
    }
    /// <summary>基因定义宿主。</summary>
    public class GeneDef : Def { }
    /// <summary>健康状态定义宿主。</summary>
    public class HediffDef : Def { }
    /// <summary>任务定义宿主。</summary>
    public class JobDef : Def { }
    /// <summary>建筑图标定义宿主。</summary>
    public class ThingDef : Def
    {
        /// <summary>定义图标。</summary>
        public UnityEngine.Texture2D uiIcon;
        /// <summary>科技档位。</summary>
        public RimWorld.TechLevel techLevel;
        /// <summary>原版是否按耐久修正市值，默认与普通物品一致。</summary>
        public bool healthAffectsPrice = true;
    }
    /// <summary>原版销毁方式宿主。</summary>
    public enum DestroyMode { Vanish }
    /// <summary>真实收益逻辑所需的物品宿主。</summary>
    public class Thing
    {
        /// <summary>物品定义。</summary>
        public ThingDef def = new ThingDef();
        /// <summary>物品固有市值；不提供交易报价。</summary>
        public float MarketValue;
        /// <summary>原版物品自身材质宿主。</summary>
        public ThingDef Stuff;
        /// <summary>原版分组品质输入。</summary>
        public int quality;
        /// <summary>精确耐久分组输入。</summary>
        public int HitPoints = 100;
        /// <summary>不含堆叠件数的名称。</summary>
        public string LabelNoCount = "测试物品";
        /// <summary>实际数量。</summary>
        public int stackCount = 1;
        /// <summary>是否已销毁。</summary>
        public bool Destroyed;
        /// <summary>是否已落地。</summary>
        public bool Spawned = true;
        /// <summary>所属地图。</summary>
        public Map Map = new Map();
        /// <summary>隔离原版拆分动作，保留未选择数量。</summary>
        public Thing SplitOff(int count)
        {
            if (count == stackCount) return this;
            stackCount -= count;
            return new Thing { def = def, MarketValue = MarketValue, stackCount = count, Map = Map };
        }
        /// <summary>观察最终消费，不模拟游戏地图移除。</summary>
        public void Destroy(DestroyMode mode) { Destroyed = true; Spawned = false; }
    }
    /// <summary>缩小建筑的原版外壳宿主。</summary>
    public class MinifiedThing : Thing
    {
        /// <summary>内层实际建筑。</summary>
        public Thing InnerThing;
    }
    /// <summary>任务记录的真实物品及数量。</summary>
    public class ThingCountClass
    {
        /// <summary>实际物品引用。</summary>
        public Thing thing;
        /// <summary>本次交付数量。</summary>
        public int Count;
        /// <summary>初始化批次条目。</summary>
        public ThingCountClass(Thing thing, int count) { this.thing = thing; Count = count; }
    }
    /// <summary>组件参数宿主。</summary>
    public class CompProperties { public Type compClass; }
    /// <summary>组件生命周期宿主。</summary>
    public class ThingComp
    {
        /// <summary>所属建筑。</summary>
        public ThingWithComps parent;
        /// <summary>组件参数。</summary>
        public CompProperties props;
        /// <summary>生成回调。</summary>
        public virtual void PostSpawnSetup(bool loaded) { }
        /// <summary>逐刻入口。</summary>
        public virtual void CompTick() { }
        /// <summary>存档入口。</summary>
        public virtual void PostExposeData() { }
        /// <summary>检查面板入口。</summary>
        public virtual string CompInspectStringExtra() => null;
        /// <summary>按钮入口。</summary>
        public virtual IEnumerable<Gizmo> CompGetGizmosExtra() { yield break; }
    }
    /// <summary>携带组件的建筑宿主。</summary>
    public class ThingWithComps : Thing
    {
        /// <summary>组件列表。</summary>
        public List<ThingComp> comps = new List<ThingComp>();
        /// <summary>错峰所需建筑编号。</summary>
        public int thingIDNumber;
        /// <summary>归属。</summary>
        public RimWorld.Faction Faction = RimWorld.Faction.OfPlayer;
        /// <summary>组件读取。</summary>
        public T TryGetComp<T>() where T : ThingComp => comps.Find(c => c is T) as T;
    }
    /// <summary>建筑宿主。</summary>
    public class Building : ThingWithComps { }
    /// <summary>人物宿主；旅行并不销毁人物。</summary>
    public class Pawn
    {
        /// <summary>真正死亡。</summary>
        public bool Dead;
        /// <summary>任务借住。</summary>
        public bool lodger;
        /// <summary>是否在地图生成。</summary>
        public bool Spawned = true;
        /// <summary>地图引用。</summary>
        public Map Map;
        /// <summary>人物名称。</summary>
        public string LabelShortCap = "人物";
        /// <summary>人物派系。</summary>
        public RimWorld.Faction Faction = RimWorld.Faction.OfPlayer;
        /// <summary>基因宿主。</summary>
        public PawnGenes genes = new PawnGenes();
        /// <summary>健康宿主。</summary>
        public PawnHealth health = new PawnHealth();
        /// <summary>任务宿主。</summary>
        public Verse.AI.Pawn_JobTracker jobs = new Verse.AI.Pawn_JobTracker();
    }
    /// <summary>基因生效状态宿主。</summary>
    public class Gene { public bool Active = true; }
    /// <summary>基因查询计数宿主。</summary>
    public class PawnGenes
    {
        /// <summary>当前身份基因。</summary>
        public Gene gene = new Gene();
        /// <summary>原版按定义取基因。</summary>
        public Gene GetGene(GeneDef def) => def == null ? null : gene;
    }
    /// <summary>意识读取计数宿主。</summary>
    public class Capacities
    {
        /// <summary>实际清醒能力。</summary>
        public bool awake = true;
        /// <summary>意识属性读取次数。</summary>
        public int reads;
        /// <summary>模拟原版清醒能力属性。</summary>
        public bool CanBeAwake { get { reads++; return awake; } }
    }
    /// <summary>健康宿主。</summary>
    public class PawnHealth
    {
        /// <summary>清醒能力。</summary>
        public Capacities capacities = new Capacities();
        /// <summary>人物契约列表。</summary>
        public HediffSet hediffSet = new HediffSet();
    }
    /// <summary>人物健康记录。</summary>
    public class Hediff { public HediffDef def; }
    /// <summary>健康集合宿主。</summary>
    public class HediffSet { public List<Hediff> hediffs = new List<Hediff>(); }
    /// <summary>地图人物列表。</summary>
    public class MapPawns { public List<Pawn> FreeColonistsSpawned = new List<Pawn>(); }
    /// <summary>地图宿主。</summary>
    public class Map { public MapPawns mapPawns = new MapPawns(); }
    /// <summary>游戏时间宿主。</summary>
    public class TickManager { public int TicksGame; }
    /// <summary>窗口集合宿主。</summary>
    public class WindowStack { public void Add(object value) { } }
    /// <summary>原版全局宿主。</summary>
    public static class Find
    {
        /// <summary>时间管理器。</summary>
        public static TickManager TickManager = new TickManager();
        /// <summary>窗口容器。</summary>
        public static WindowStack WindowStack = new WindowStack();
    }
    /// <summary>存档阶段宿主。</summary>
    public enum LoadSaveMode { Inactive, Saving, LoadingVars, ResolvingCrossRefs, PostLoadInit }
    /// <summary>存档入口宿主。</summary>
    public static class Scribe { public static LoadSaveMode mode; }
    /// <summary>值存档宿主。</summary>
    public static class Scribe_Values { public static void Look<T>(ref T value, string key, T fallback) { } }
    /// <summary>人物引用宿主。</summary>
    public static class Scribe_References { public static void Look<T>(ref T value, string key) { } }
    /// <summary>语言键宿主。</summary>
    public static class Translation { public static string Translate(this string key, params object[] args) => key; }
    /// <summary>按钮基类宿主。</summary>
    public class Gizmo { }
    /// <summary>动作按钮宿主。</summary>
    public class Command_Action : Gizmo
    {
        /// <summary>按钮名称。</summary>
        public string defaultLabel;
        /// <summary>按钮说明。</summary>
        public string defaultDesc;
        /// <summary>按钮图标。</summary>
        public UnityEngine.Texture2D icon;
        /// <summary>动作回调。</summary>
        public Action action;
    }
    /// <summary>切换按钮宿主。</summary>
    public class Command_Toggle : Command_Action
    {
        /// <summary>当前值。</summary>
        public Func<bool> isActive;
        /// <summary>切换回调。</summary>
        public Action toggleAction;
    }
    /// <summary>开发按钮标记。</summary>
    public static class DebugSettings { public static bool ShowDevGizmos; }
    /// <summary>原版人物菜单。</summary>
    public class FloatMenu { public FloatMenu(List<FloatMenuOption> options) { } }
    /// <summary>菜单项宿主。</summary>
    public class FloatMenuOption { public FloatMenuOption(string label, Action action) { } }
}
namespace Verse.AI
{
    /// <summary>任务宿主。</summary>
    public class Job { }
    /// <summary>任务创建宿主。</summary>
    public static class JobMaker { public static Job MakeJob(Verse.JobDef def, Verse.ThingWithComps target) => new Job(); }
    /// <summary>人工任务排队宿主。</summary>
    public class Pawn_JobTracker { public void TryTakeOrderedJob(Job job) { } }
}
namespace BDP.Content.Trion.Royalty.Contract
{
    /// <summary>只隔离人物契约的原版健康载体。</summary>
    public class Hediff_RoyalContract : Verse.Hediff
    {
        /// <summary>关系对象。</summary>
        public Verse.Pawn partner;
        /// <summary>皇室签约方。</summary>
        public bool royalSigner;
    }
}
namespace BDP.Content.Trion.Facility.Jobs
{
    /// <summary>只隔离原版任务定义加载。</summary>
    public static class TrionFacilityJobDefOf
    {
        /// <summary>绑定任务。</summary>
        public static Verse.JobDef BDP_BindTrionFacility = new Verse.JobDef();
        /// <summary>解绑任务。</summary>
        public static Verse.JobDef BDP_UnbindTrionFacility = new Verse.JobDef();
        /// <summary>献祭任务。</summary>
        public static Verse.JobDef BDP_SacrificeToTrionFacility = new Verse.JobDef();
    }
}
namespace BDP.Content.Trion.Facility.Sacrifice
{
    /// <summary>仅隔离窗口绘制；正式收益和最终提交仍编译真实源码。</summary>
    public class Dialog_TrionFacilitySacrifice
    {
        /// <summary>测试不创建原版界面。</summary>
        public Dialog_TrionFacilitySacrifice(BDP.Content.Trion.Facility.Building_TrionFacility facility) { }
    }
}
namespace BDP.Content.Trion.Facility
{
    /// <summary>光照和许可通知的可观察宿主，不复制正式资源或绑定逻辑。</summary>
    public class Building_TrionFacility : Verse.Building
    {
        /// <summary>许可通知次数。</summary>
        public int notifications;
        /// <summary>光照刷新次数。</summary>
        public int glowUpdates;
        /// <summary>真实资源组件。</summary>
        public CompTrionFacilityResource ResourceComp => TryGetComp<CompTrionFacilityResource>();
        /// <summary>通知计数。</summary>
        public void NotifyOperationalStateChanged() { notifications++; }
        /// <summary>光照计数。</summary>
        public void RefreshOperationalGlow() { glowUpdates++; }
    }
}
namespace BDP.Tests
{
    /// <summary>测试正式核心组件的公共运行行为，不模拟任务寻路。</summary>
    public static class TrionFacilityLifecycleTests
    {
        /// <summary>数值断言。</summary>
        private static void Equal(float expected, float actual, string reason)
        {
            if (Math.Abs(expected - actual) > 0.002f) throw new Exception(reason + ": " + actual);
        }
        /// <summary>生命周期条件断言。</summary>
        private static void Check(bool condition, string reason)
        {
            if (!condition) throw new Exception(reason);
        }
        /// <summary>挂载真实业务组件，仅原版身份定义由宿主提供。</summary>
        private static CompTrionFacilityControl MakeControl(Building_TrionFacility building)
        {
            var control = new CompTrionFacilityControl
            {
                parent = building,
                props = new CompProperties_TrionFacilityControl
                {
                    royalGene = new Verse.GeneDef(),
                    contractHediff = new Verse.HediffDef()
                }
            };
            building.comps.Add(control);
            return control;
        }
        /// <summary>推进时间并调用真实逐刻入口；生产代码自己判断是否到期。</summary>
        private static void Advance(BDP.Content.Trion.Facility.CompTrionFacilityResource resource, int tick)
        {
            Verse.Find.TickManager.TicksGame = tick;
            resource.CompTick();
        }
        /// <summary>验证真实组件的容量、权限、运行周期和存档回调时序。</summary>
        public static void Run()
        {
            var building = new BDP.Content.Trion.Facility.Building_TrionFacility();
            var resource = new BDP.Content.Trion.Facility.CompTrionFacilityResource();
            resource.parent = building;
            resource.props = new BDP.Content.Trion.Facility.CompProperties_TrionFacilityResource { maxResource = 1000, baseDrainPerDay = 10, settleInterval = 250 };
            building.comps.Add(resource);
            resource.PostSpawnSetup(false);
            Equal(900, resource.TryAddResource(900), "首次入账");
            Equal(300, resource.TryAddResource(300), "容器完整接收经业务核验的最后一件300资源，不能截为100");
            Equal(1200, resource.CurrentResource, "溢出余额必须保留");
            Equal(0, resource.TryAddResource(1), "达到门槛后拒绝新补充");
            Verse.Scribe.mode = Verse.LoadSaveMode.PostLoadInit;
            resource.PostExposeData();
            Verse.Scribe.mode = Verse.LoadSaveMode.Inactive;
            Equal(1200, resource.CurrentResource, "读档不能截掉超额余额");
            Console.WriteLine("通过：完整溢出、门槛拒绝与读档保留。");
            var control = MakeControl(building);
            var royal = new Verse.Pawn();
            Check(!resource.IsOperational, "未绑定时即使正储量也不能运行");
            Check(!control.CompGetGizmosExtra().OfType<Verse.Command_Action>().Any(g => g.defaultLabel == "BDP_TrionFacility_Sacrifice"),
                "未绑定时不能提供普通献祭操作入口");
            Check(control.TryBind(royal), "有效皇室可以独立绑定");
            Check(resource.IsOperational, "绑定后正储量核心开始运行");
            Check(control.CompGetGizmosExtra().OfType<Verse.Command_Action>().Count(g => g.defaultLabel == "BDP_TrionFacility_Sacrifice") == 1,
                "绑定后必须提供一个可点击的正式献祭补充按钮");
            int reads = royal.health.capacities.reads;
            int notifications = building.notifications;
            for (int tick = 1; tick < 250; tick++)
            {
                Advance(resource, tick);
                resource.CompInspectStringExtra();
                Check(resource.IsOperational, "未到期时读取缓存运行状态");
            }
            Equal(reads, royal.health.capacities.reads, "逐刻和信息面板不能检查意识");
            Equal(notifications, building.notifications, "同一状态不能反复通知");
            royal.Spawned = false;
            royal.Map = null;
            Advance(resource, 250);
            Check(resource.IsOperational, "进入商队或离图不影响运行");
            royal.health.capacities.awake = false;
            Advance(resource, 499);
            Check(resource.IsOperational, "允许检查之前保留原运行状态");
            Advance(resource, 500);
            Check(!resource.IsOperational && control.BoundRoyal == royal, "昏迷停运但不解绑");
            float paused = resource.CurrentResource;
            Advance(resource, 750);
            Equal(paused, resource.CurrentResource, "停运期间不扣耗");
            royal.health.capacities.awake = true;
            Advance(resource, 1000);
            Equal(paused, resource.CurrentResource, "恢复时不能补扣暂停区间");
            Check(resource.IsOperational, "意识恢复后自动运行");
            var partner = new Verse.Pawn();
            royal.health.hediffSet.hediffs.Add(new BDP.Content.Trion.Royalty.Contract.Hediff_RoyalContract
            {
                def = ((CompProperties_TrionFacilityControl)control.props).contractHediff,
                partner = partner,
                royalSigner = true
            });
            Check(control.HasCoreAccess(partner), "本核心皇室的契约者获得普通权限");
            Check(!control.TryUnbind(partner), "契约者不能解绑");
            Check(!control.HasCoreAccess(new Verse.Pawn()), "其他皇室没有本核心权限");
            royal.Dead = true;
            Advance(resource, 1250);
            Check(!resource.IsOperational && control.BoundRoyal == null, "真正死亡自动解绑");
            var replacement = new Verse.Pawn();
            Check(control.TryBind(replacement), "死亡后另一皇室可以接管");
            float beforeUnbind = resource.CurrentResource;
            Check(control.TryUnbind(replacement), "绑定皇室可以主动解绑");
            Equal(beforeUnbind, resource.CurrentResource, "解绑不清空储量");
            Check(!resource.IsOperational, "解绑后停运");
            Check(control.TryBind(replacement), "允许重新绑定");
            replacement.genes.gene.Active = false;
            Advance(resource, 1500);
            Check(control.BoundRoyal == null && !resource.IsOperational, "皇室基因失效自动解绑");
            replacement.genes.gene.Active = true;
            Check(control.TryBind(replacement), "资格恢复后可以重新绑定");
            replacement.Faction = new RimWorld.Faction();
            Advance(resource, 1750);
            Check(control.BoundRoyal == null && !resource.IsOperational, "离开玩家派系自动解绑");
            var lodger = new Verse.Pawn { lodger = true };
            Check(!control.TryBind(lodger), "临时借住皇室不能绑定");
            Check(!control.HasCoreAccess(partner), "解绑撤销原契约者对该核心的权限");
            Console.WriteLine("通过：低频读取、外出、昏迷、暂停结算、死亡接管、权限及资格失效。");

            var savedRoyal = new Verse.Pawn();
            Check(control.TryBind(savedRoyal), "保存前建立正常归属");
            float beforeSave = resource.CurrentResource;
            Verse.Find.TickManager.TicksGame = 1800;
            Verse.Scribe.mode = Verse.LoadSaveMode.Saving;
            resource.PostExposeData();
            Equal(beforeSave - 10f * 50f / 60000f, resource.CurrentResource,
                "保存必须结清从1750到1800的运行消耗，不能留到读档猜测");
            float savedBalance = resource.CurrentResource;
            Check(savedBalance > 1000, "保存结算也须保留溢出");
            savedRoyal.health.capacities.awake = false;
            int readsBeforeLoad = savedRoyal.health.capacities.reads;
            Verse.Scribe.mode = Verse.LoadSaveMode.PostLoadInit;
            resource.PostExposeData();
            control.PostExposeData();
            Check(!resource.IsOperational, "读档过程不得提前发布未重建的许可");
            Check(control.BoundRoyal == savedRoyal, "引用解析期间不能清空皇室归属");
            Equal(readsBeforeLoad, savedRoyal.health.capacities.reads,
                "组件读档回调不得提前读取人物健康");
            Verse.Scribe.mode = Verse.LoadSaveMode.Inactive;
            resource.InitializeOperation();
            Check(!resource.IsOperational && control.BoundRoyal == savedRoyal,
                "地图最终初始化须按意识重建暂停状态并保留归属");
            Advance(resource, 2000);
            Equal(savedBalance, resource.CurrentResource, "读档后的暂停区间不补扣保存前消耗");
            savedRoyal.health.capacities.awake = true;
            Advance(resource, 2250);
            Check(resource.IsOperational, "读档后清醒可以恢复许可");
            Equal(savedBalance, resource.CurrentResource, "读档后恢复不补扣暂停区间");
            Console.WriteLine("通过：保存时结清、溢出保留、延后重建与恢复不补扣。");
        }
    }
}
