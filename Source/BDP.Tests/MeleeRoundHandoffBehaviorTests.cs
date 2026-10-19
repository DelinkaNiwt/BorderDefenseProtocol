using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using BDP.Core.Verbs;
using HarmonyLib;
using Verse;
using Verse.AI;

/// <summary>执行真实任务推进和续段绑定；只控制地图、准备结果及原版起手等游戏依赖。</summary>
internal static class MeleeRoundHandoffBehaviorTests
{
    /// <summary>真实依赖目录。</summary>
    private static string[] directories;
    /// <summary>真实核心程序集。</summary>
    private static Assembly core;
    /// <summary>当前独立用例的状态。</summary>
    private static Fixture current;
    /// <summary>失败用例数量。</summary>
    private static int failures;

    /// <summary>注册依赖后运行回归检查。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        return Run();
    }

    /// <summary>设置隔离边界后调用真实任务逻辑，不复写待修的交接分支。</summary>
    private static int Run()
    {
        core = Assembly.LoadFrom(Path.Combine(directories[2], "BDP.Core.dll"));
        Harmony harmony = new Harmony("bdp.tests.melee.round.handoff");
        Patch(harmony, AccessTools.PropertyGetter(typeof(Thing), "Spawned"), "Spawned");
        Patch(harmony, AccessTools.PropertyGetter(typeof(Verb), "WarmingUp"), "WarmingUp");
        Patch(harmony, AccessTools.PropertyGetter(typeof(Pawn_StanceTracker), "FullBodyBusy"), "Busy");
        Patch(harmony, AccessTools.Method(typeof(ReachabilityImmediate), "CanReachImmediate", new[] { typeof(Pawn), typeof(LocalTargetInfo), typeof(PathEndMode) }), "Reachable");
        Patch(harmony, AccessTools.Method(typeof(Verb), "CanHitTarget"), "CanHit");
        Patch(harmony, AccessTools.Method(typeof(Verb), "CanHitTargetFrom"), "CanHit");
        Patch(harmony, AccessTools.Method(typeof(Verb), "TryStartCastOn", new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }), "StartCast");
        Patch(harmony, AccessTools.Method(typeof(Pawn_PathFollower), "StartPath"), "StartPath");
        Patch(harmony, AccessTools.Method(typeof(JobDriver), "EndJobWith"), "EndJob");
        Patch(harmony, AccessTools.Method(core.GetType("BDP.Core.AttackExecution.AttackExecutionPostLoadRecovery"), "IsCurrentAttackSessionValid"), "SessionValid");
        Patch(harmony, AccessTools.Method(core.GetType("BDP.Core.Verbs.MeleeVerbContinuationPlanner"), "TryPrepareNextRun"), "Prepare");
        Type diagnostics = core.GetType("BDP.Core.AttackExecution.AttackExecutionDiagnostics");
        foreach (MethodInfo method in diagnostics.GetMethods(BindingFlags.Public | BindingFlags.Static))
            if (method.Name.StartsWith("Log", StringComparison.Ordinal)) Patch(harmony, method, "Skip");

        Test("同次更新续接起手并使用新动作、新目标", delegate {
            Check(Tick(), "续接未返回成功");
            Check(current.Casts == 1, "下一轮只完成准备，没有在同次更新起手");
            Check(current.StartedVerb == current.NextVerb && current.StartedTarget == current.NextTarget, "沿用了旧动作或旧目标");
            Check((int)Get(current.Driver, "castCount") == 1, "成功起手没有推进计数");
        });
        Test("原版冷却未完不续接", delegate { current.Busy = true; Check(Tick() && current.Prepares == 0 && current.Casts == 0, "提前续接或起手"); });
        Test("预热中不重起手", delegate { current.Warming = true; Check(Tick() && current.Prepares == 0 && current.Casts == 0, "预热被重起手"); });
        Test("连击中不重起手", delegate { current.OldVerb.state = VerbState.Bursting; Set(current.OldVerb, "burstShotsLeft", 1); Check(Tick() && current.Prepares == 0 && current.Casts == 0, "连击被重起手"); });
        Test("整次计划正常结束不再起手", delegate { Set(current.OldVerb, "nextRuntimeStepIndex", -1); Check(Tick() && current.Ends == 1 && current.Casts == 0, "已结束计划继续起手"); });
        Test("下一段准备失败退出", delegate { current.PrepareAccepted = false; Check(!Tick() && current.Casts == 0, "准备失败仍然起手"); });
        Test("准备期间任务切换不再起手", delegate { current.ReplaceJob = true; Check(Tick() && current.Casts == 0, "旧推进器在任务切换后起手"); });
        Test("下一段目标不可达先追赶", delegate { current.Reachable = false; Check(Tick() && current.Paths == 1 && current.Casts == 0, "追赶检查被跳过"); });
        Test("下一段目标失效正常结束", delegate { current.NextTarget = null; Check(Tick() && current.Ends == 1 && current.Casts == 0, "失效新目标被用于起手"); });
        Test("下一段起手拒绝不推进计数", delegate { current.CastAccepted = false; Check(!Tick() && (int)Get(current.Driver, "castCount") == 0, "起手拒绝仍推进计数"); });
        harmony.UnpatchAll(harmony.Id);
        Console.WriteLine("近战轮次交接隔离检查：" + (failures == 0 ? "10项通过" : failures + "项失败"));
        return failures == 0 ? 0 : 1;
    }

    /// <summary>每项重建状态，避免用例相互污染。</summary>
    private static void Test(string name, Action action)
    {
        current = new Fixture();
        try { action(); Console.WriteLine("通过：" + name); }
        catch (Exception error) { failures++; Console.Error.WriteLine("失败：" + name + "：" + (error.InnerException ?? error).Message); }
    }
    /// <summary>调用真实核心私有推进方法。</summary>
    private static bool Tick() { return (bool)AccessTools.Method(current.Driver.GetType(), "TryTickExecution").Invoke(current.Driver, null); }
    /// <summary>安装限定于隔离进程的受控前置条件。</summary>
    private static void Patch(Harmony harmony, MethodBase method, string prefix)
    {
        Check(method != null, "找不到夹具依赖的真实方法：" + prefix);
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(MeleeRoundHandoffBehaviorTests), prefix));
    }
    /// <summary>最低必要的有效目标条件。</summary>
    private static bool Spawned(ref bool __result) { __result = true; return false; }
    /// <summary>受控预热状态。</summary>
    private static bool WarmingUp(ref bool __result) { __result = current.Warming; return false; }
    /// <summary>受控原版忙碌条件。</summary>
    private static bool Busy(ref bool __result) { __result = current.Busy; return false; }
    /// <summary>受控可达性，确认原有追赶分支仍执行。</summary>
    private static bool Reachable(ref bool __result) { __result = current.Reachable; return false; }
    /// <summary>无需地图的合法命中前置条件。</summary>
    private static bool CanHit(ref bool __result) { __result = true; return false; }
    /// <summary>有效会话前置条件，不模拟投影和读档恢复。</summary>
    private static bool SessionValid(ref bool __result) { __result = true; return false; }
    /// <summary>提供准备结果；后续动作与目标绑定仍由真实续段方法完成。</summary>
    private static bool Prepare(object[] __args, ref bool __result)
    {
        current.Prepares++;
        object context = FormatterServices.GetUninitializedObject(core.GetType("BDP.Core.AttackExecution.MeleeAttackExecutionContext"));
        Set(context, "<Verb>k__BackingField", current.NextVerb);
        Set(context, "<Target>k__BackingField", new LocalTargetInfo(current.NextTarget));
        Set(context, "<RequiredStepCount>k__BackingField", 3);
        __args[2] = context;
        if (current.ReplaceJob) current.Pawn.jobs.curJob = new Job();
        __result = current.PrepareAccepted;
        return false;
    }
    /// <summary>记录真正的起手调用，不模拟原版伤害或姿态建立。</summary>
    private static bool StartCast(Verb __instance, LocalTargetInfo castTarg, ref bool __result)
    {
        current.Casts++;
        current.StartedVerb = __instance;
        current.StartedTarget = castTarg.Thing;
        __result = current.CastAccepted;
        return false;
    }
    /// <summary>记录追赶调用，避免进入游戏寻路。</summary>
    private static bool StartPath() { current.Paths++; return false; }
    /// <summary>清除当前作业，保留结束后不可继续起手的真实边界条件。</summary>
    private static bool EndJob() { current.Ends++; current.Pawn.jobs.curJob = null; current.Pawn.jobs.curDriver = null; return false; }
    /// <summary>诊断格式化与游戏对象显示不属于本次交接验证。</summary>
    private static bool Skip() { return false; }
    /// <summary>创建未初始化对象，避免运行游戏引擎构造。</summary>
    private static T Blank<T>() { return (T)FormatterServices.GetUninitializedObject(typeof(T)); }
    /// <summary>读取真实字段。</summary>
    private static object Get(object target, string name) { return AccessTools.Field(target.GetType(), name).GetValue(target); }
    /// <summary>设置真实字段，兼容父类私有状态。</summary>
    private static void Set(object target, string name, object value) { AccessTools.Field(target.GetType(), name).SetValue(target, value); }
    /// <summary>验证公共行为边界。</summary>
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    /// <summary>加载真实依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in directories) { string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }

    /// <summary>单个用例的最小状态；不模拟完整地图、近战伤害或画面。</summary>
    private sealed class Fixture
    {
        /// <summary>受控人物。</summary>
        public Pawn Pawn = Blank<Pawn>();
        /// <summary>真实核心推进器。</summary>
        public JobDriver Driver;
        /// <summary>上一轮动作。</summary>
        public BdpVerb_MeleeAttackDamage OldVerb = Blank<BdpVerb_MeleeAttackDamage>();
        /// <summary>下一轮动作。</summary>
        public BdpVerb_MeleeAttackDamage NextVerb = Blank<BdpVerb_MeleeAttackDamage>();
        /// <summary>下一轮目标。</summary>
        public Thing NextTarget = Blank<Thing>();
        /// <summary>是否仍在原版冷却。</summary>
        public bool Busy;
        /// <summary>是否正在预热。</summary>
        public bool Warming;
        /// <summary>目标是否可立即触及。</summary>
        public bool Reachable = true;
        /// <summary>准备是否成功。</summary>
        public bool PrepareAccepted = true;
        /// <summary>起手是否接受。</summary>
        public bool CastAccepted = true;
        /// <summary>准备期间是否换任务。</summary>
        public bool ReplaceJob;
        /// <summary>准备次数。</summary>
        public int Prepares;
        /// <summary>起手次数。</summary>
        public int Casts;
        /// <summary>追赶次数。</summary>
        public int Paths;
        /// <summary>结束次数。</summary>
        public int Ends;
        /// <summary>实际起手动作。</summary>
        public Verb StartedVerb;
        /// <summary>实际起手目标。</summary>
        public Thing StartedTarget;

        /// <summary>绑定真实作业与推进器，并设置已完成一轮、等待续接的状态。</summary>
        public Fixture()
        {
            Pawn.jobs = Blank<Pawn_JobTracker>();
            Pawn.pather = Blank<Pawn_PathFollower>();
            Pawn.stances = Blank<Pawn_StanceTracker>();
            Driver = (JobDriver)FormatterServices.GetUninitializedObject(core.GetType("BDP.Core.AttackExecution.JobDriver_BdpMeleeAttackExecution"));
            Driver.pawn = Pawn;
            Driver.job = new Job { targetA = new LocalTargetInfo(Blank<Thing>()), verbToUse = OldVerb };
            Pawn.jobs.curJob = Driver.job;
            Pawn.jobs.curDriver = Driver;
            Set(Driver, "continuationPlanner", FormatterServices.GetUninitializedObject(core.GetType("BDP.Core.Verbs.MeleeVerbContinuationPlanner")));
            Set(Driver, "castCount", 1);
            Set(OldVerb, "nextRuntimeStepIndex", 0);
        }
    }
}
