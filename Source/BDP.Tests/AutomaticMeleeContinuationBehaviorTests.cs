using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using BDP.Core.Verbs;
using HarmonyLib;
using Verse;
using Verse.AI;

/// <summary>调用真实有限续段控制；只隔离地图、计划准备和原版起手等引擎依赖。</summary>
internal static class AutomaticMeleeContinuationBehaviorTests
{
    /// <summary>依赖目录，先注册再进入引用游戏类型的检查方法。</summary>
    private static string[] directories;
    /// <summary>实际编译的核心程序集。</summary>
    private static Assembly core;
    /// <summary>当前独立用例。</summary>
    private static Fixture current;
    /// <summary>用例统计。</summary>
    private static int failures, cases;

    /// <summary>先注册依赖加载，避免在游戏外启动程序集时缺少 Unity 引用。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        return Run();
    }

    /// <summary>安装引擎边界后验证真实续段行为。</summary>
    private static int Run()
    {
        core = Assembly.LoadFrom(Path.Combine(directories[2], "BDP.Core.dll"));
        if (AccessTools.Method(typeof(BdpVerb_MeleeAttackDamage), "TickAutomaticContinuation") == null)
        {
            Console.Error.WriteLine("失败：自动近战首段之后没有有限续段推进入口，副手无法得到执行。");
            return 1;
        }
        Harmony harmony = new Harmony("bdp.tests.automatic.melee.continuation");
        Patch(harmony, AccessTools.PropertyGetter(typeof(Thing), "Spawned"), "Spawned");
        Patch(harmony, AccessTools.PropertyGetter(typeof(Pawn), "Dead"), "Dead");
        Patch(harmony, AccessTools.PropertyGetter(typeof(Pawn), "Downed"), "Downed");
        Patch(harmony, AccessTools.PropertyGetter(typeof(Verb), "WarmingUp"), "Warming");
        Patch(harmony, AccessTools.PropertyGetter(typeof(Pawn_StanceTracker), "FullBodyBusy"), "Busy");
        Patch(harmony, AccessTools.Method(typeof(Verb), "CanHitTarget"), "CanHit");
        Patch(harmony, AccessTools.Method(typeof(Verb), "TryStartCastOn", new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }), "StartCast");
        Patch(harmony, AccessTools.Method(core.GetType("BDP.Core.AttackExecution.AttackExecutionPostLoadRecovery"), "IsCurrentAttackSessionValid"), "SessionValid");
        Patch(harmony, AccessTools.Method(core.GetType("BDP.Core.Verbs.MeleeVerbContinuationPlanner"), "TryPrepareNextRun"), "Prepare");
        Patch(harmony, AccessTools.Method(core.GetType("BDP.Core.VerbHosting.TriggerBodyVerbHostManager"), "EnsureFormalBindings"), "Skip");
        Patch(harmony, AccessTools.Method(typeof(Verb), "VerbTick"), "Skip");
        foreach (string typeName in new[] { "BDP.Core.AttackExecution.AttackExecutionDiagnostics", "BDP.Core.AttackExecution.AttackExecutionVisualRuntimeBridge" })
            foreach (MethodInfo method in core.GetType(typeName).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                if (method.Name.StartsWith("Log", StringComparison.Ordinal) || method.Name == "Clear" || method.Name == "Publish") Patch(harmony, method, "Skip");

        Test("副手在原任务下起手且目标不变", delegate { Tick(); Check(current.Casts == 1 && current.StartedVerb == current.Next && current.StartedTarget == current.Target, "副手没有正确起手"); Check(current.Pawn.CurJob == current.Job, "原版任务被替换"); });
        Test("最终冷却未完只等待", delegate { current.Busy = true; Tick(); NoAdvance(); Check(Active(), "冷却期间丢失活跃更新"); });
        Test("预热中不得推进", delegate { current.Warming = true; Tick(); NoAdvance(); });
        Test("同宿主连击中不得推进", delegate { current.Old.state = VerbState.Bursting; Set(current.Old, "burstShotsLeft", 1); Tick(); NoAdvance(); });
        Test("没有真正起手不得推进", delegate { Bind(); Tick(); NoAdvance(); });
        Test("首段起手拒绝不得激活", delegate { Bind(); current.CastAccepted = false; current.Old.TryStartCastOn(current.Target); current.Casts = 0; Tick(); NoAdvance(); });
        Test("有限计划完成不会重复主手", delegate { Tick(); Tick(); Tick(current.Next); Check(current.Casts == 1 && !Active() && !Active(current.Next), "有限计划重新循环"); });
        Test("原任务结束取消剩余段", delegate { current.Pawn.jobs.curJob = null; Tick(); NoAdvance(); Check(!Active(), "任务结束后仍保留续段"); });
        Test("原任务替换取消剩余段", delegate { current.Pawn.jobs.curJob = new Job { loadID = 43 }; Tick(); NoAdvance(); });
        Test("原任务原地换目标也取消", delegate { current.Job.targetA = new LocalTargetInfo(Blank<Thing>()); Tick(); NoAdvance(); });
        Test("原版改攻其他目标不消费旧段", delegate { Verb candidate; Check(!Request(Blank<Thing>(), out candidate), "旧目标计划被转给新目标"); Tick(); NoAdvance(); });
        Test("目标离开地图取消", delegate { current.TargetSpawned = false; Tick(); NoAdvance(); });
        Test("目标移出下一段近战距离不自行追赶", delegate { current.Hittable = false; Tick(); Check(current.Prepares == 1 && current.Casts == 0 && !Active(), "不可达目标被起手或仍留在自动续段"); });
        Test("投影会话失效取消", delegate { current.SessionAccepted = false; Tick(); NoAdvance(); });
        Test("首段硬失败取消而未命中仍可续", delegate { Set(current.Old, "<LastMeleeBurstShotCompleted>k__BackingField", false); Tick(); NoAdvance(); });
        Test("准备失败不再自动重试", delegate { current.PrepareAccepted = false; Tick(); Tick(); Check(current.Prepares == 1 && current.Casts == 0 && !Active(), "失败后反复准备"); });
        Test("准备期间原任务切换不能起手", delegate { current.ReplaceJob = true; Tick(); Check(current.Prepares == 1 && current.Casts == 0, "失效任务仍起手"); });
        Test("副手起手拒绝不继续计划", delegate { current.CastAccepted = false; current.Remaining = 2; Tick(); Tick(current.Next); Check(current.Casts == 1 && !Active(current.Next), "拒绝后继续自动攻击"); });
        Test("原版先再次请求时直接交付副手", delegate { Verb candidate; Check(Request(current.Target, out candidate) && candidate == current.Next, "又重新选择了主手"); candidate.TryStartCastOn(current.Target); Tick(); Check(current.Casts == 1, "更新入口重复消费已交接段"); });
        Test("同一宿主的后续段不会被清理误伤", delegate { current.Next = current.Old; Tick(); Check(current.Casts == 1, "同宿主续段起手失败"); Tick(); Check(current.Casts == 1 && !Active(), "同宿主有限结束失败"); });
        Test("第三段继承同一个原任务边界", delegate { current.Remaining = 2; Tick(); Check(Active(current.Next), "未保留第三段"); current.Pawn.jobs.curJob = null; Tick(current.Next); Check(current.Casts == 1, "第三段越过原任务结束"); });
        Test("手动持续计划不进入自动推进", delegate { Set(current.Old, "planReason", Enum.Parse(core.GetType("BDP.Core.AttackExecution.AttackExecutionReason"), "Manual")); Bind(); Start(); Tick(); NoAdvance(); });
        Test("真实宿主管理器恢复空队列并跨槽交接", delegate {
            object manager = AttachManager(); current.Remaining = 2;
            Call(manager, "Tick");
            Check(current.Casts == 1 && Active(current.Next), "管理器未调用有限续段");
            Check(((IList)Get(manager, "activeVerbsForTick")).Count == 1, "跨槽后旧槽未退出或新槽未入队");
            current.Pawn.jobs.curJob = null; Call(manager, "Tick");
            Check(((IList)Get(manager, "activeVerbsForTick")).Count == 0 && current.Casts == 1, "任务结束未清理新槽");
        });
        Test("管理器原版请求入口直接交付剩余段", delegate {
            object manager = AttachManager(); object[] args = { current.Target, null };
            Check((bool)AccessTools.Method(manager.GetType(), "TryPrepareAutomaticMeleeContinuation").Invoke(manager, args)
                && args[1] == current.Next && current.Prepares == 1, "管理器没有找到原任务剩余段");
        });
        Test("原版战斗等待无任务目标仍可完成同次计划", delegate { current.Job.targetA = LocalTargetInfo.Invalid; Bind(); Start(); Tick(); Check(current.Casts == 1, "无任务目标的原版等待被误取消"); });
        Test("攻击者倒地取消剩余段", delegate { current.CasterDowned = true; Tick(); NoAdvance(); Check(!Active(), "攻击者倒地后仍有续段"); });
        Test("目标倒地遵守原任务的补刀许可", delegate { current.Target = Blank<Pawn>(); current.Job.targetA = current.Target; Bind(); Start(); current.TargetDowned = true; Tick(); NoAdvance(); });
        Test("原任务允许补刀时可以续段", delegate { current.Target = Blank<Pawn>(); current.Job.targetA = current.Target; current.Job.killIncappedTarget = true; Bind(); Start(); current.TargetDowned = true; Tick(); Check(current.Casts == 1, "原版允许的补刀被无条件阻止"); });
        Test("目标死亡取消剩余段", delegate { current.Target = Blank<Pawn>(); current.Job.targetA = current.Target; Bind(); Start(); current.TargetDead = true; Tick(); NoAdvance(); });
        Test("重新绑定上下文会取消旧自动计划", delegate { Call(current.Old, "ApplyExecutionContext", new object[] { null }); Tick(); NoAdvance(); Check(!Active(), "旧自动计划泄漏到新上下文"); });
        Test("宿主重置会取消旧自动计划", delegate { current.Old.Reset(); Tick(); NoAdvance(); Check(!Active(), "重置后仍有旧计划"); });
        Test("新段不能沿用宿主上一轮完成标记", delegate { Bind(); Call(current.Old, "NotifyAutomaticPlanStarted"); Tick(); NoAdvance(); Check(!Active(), "未实际出手的新段错误续接"); });
        harmony.UnpatchAll(harmony.Id);
        Console.WriteLine("自动近战有限续段检查：" + cases + "项，失败" + failures + "项。");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>每项重建最小原版任务与正式宿主。</summary>
    private static void Test(string name, Action action)
    {
        cases++;
        current = new Fixture();
        try { Bind(); Start(); action(); Console.WriteLine("通过：" + name); }
        catch (Exception error) { failures++; Console.Error.WriteLine("失败：" + name + "：" + (error.InnerException ?? error).Message); }
    }
    /// <summary>把待执行计划绑定到原版任务。</summary>
    private static void Bind() { Call(current.Old, "BindAutomaticContinuation", current.Job, current.Target); }
    /// <summary>通过真实正式宿主成功起手分支激活，再模拟首段已正常消耗完毕。</summary>
    private static void Start() { current.Old.TryStartCastOn(current.Target); current.Casts = 0; Set(current.Old, "<LastMeleeBurstShotCompleted>k__BackingField", true); }
    /// <summary>调用真实宿主更新入口。</summary>
    private static void Tick(BdpVerb_MeleeAttackDamage verb = null) { Call(verb ?? current.Old, "TickAutomaticContinuation"); }
    /// <summary>读取实际活跃条件。</summary>
    private static bool Active(BdpVerb_MeleeAttackDamage verb = null) { return (bool)Call(verb ?? current.Old, "RequiresFormalHostRuntimeTick"); }
    /// <summary>使用真实绑定和活跃队列；只跳过表达刷新及原版逐刻引擎调用。</summary>
    private static object AttachManager()
    {
        object owner = FormatterServices.GetUninitializedObject(core.GetType("BDP.Core.Trigger.CompTriggerBody"));
        object manager = Activator.CreateInstance(core.GetType("BDP.Core.VerbHosting.TriggerBodyVerbHostManager"), new[] { owner });
        Set(owner, "verbHostManager", manager);
        AddBinding(manager, owner, current.Old, "MainPrimary");
        AddBinding(manager, owner, current.Next, "SubPrimary");
        return manager;
    }
    /// <summary>准备有效的正式宿主槽位，后续队列管理执行真实代码。</summary>
    private static void AddBinding(object manager, object owner, BdpVerb_FormalHostMelee verb, string slotName)
    {
        object slot = Enum.Parse(core.GetType("BDP.Core.VerbHosting.BdpFormalVerbHostSlot"), slotName);
        object state = FormatterServices.GetUninitializedObject(core.GetType("BDP.Core.VerbHosting.BdpFormalVerbBindingState"));
        Set(state, "<IsAvailable>k__BackingField", true);
        Set(state, "<WeaponMode>k__BackingField", Enum.Parse(core.GetType("BDP.Core.Expressions.WeaponExpressionMode"), "Melee"));
        object binding = FormatterServices.GetUninitializedObject(core.GetType("BDP.Core.VerbHosting.BdpFormalVerbBinding"));
        Set(binding, "<State>k__BackingField", state); Set(binding, "<MeleeVerb>k__BackingField", verb);
        ((IDictionary)Get(manager, "bindings")).Add(slot, binding);
        Set(verb, "owner", owner); Set(verb, "slot", slot);
    }
    /// <summary>模拟原版先于宿主更新到达的同目标请求，续段选择仍由真实方法完成。</summary>
    private static bool Request(Thing target, out Verb verb) { object[] args = { target, null }; bool handled = (bool)AccessTools.Method(current.Old.GetType(), "TryPrepareAutomaticContinuation").Invoke(current.Old, args); verb = (Verb)args[1]; return handled; }
    /// <summary>确认没有续段准备与攻击。</summary>
    private static void NoAdvance() { Check(current.Prepares == 0 && current.Casts == 0, "出现了额外准备或攻击"); }
    /// <summary>只替换不可在游戏外运行的边界。</summary>
    private static void Patch(Harmony harmony, MethodBase method, string prefix) { Check(method != null, "夹具依赖缺失：" + prefix); harmony.Patch(method, prefix: new HarmonyMethod(typeof(AutomaticMeleeContinuationBehaviorTests), prefix)); }
    /// <summary>控制目标是否仍在地图。</summary>
    private static bool Spawned(Thing __instance, ref bool __result) { __result = __instance != current.Target || current.TargetSpawned; return false; }
    /// <summary>控制目标死亡，避免进入未构造的健康系统。</summary>
    private static bool Dead(Pawn __instance, ref bool __result) { __result = __instance == current.Target && current.TargetDead; return false; }
    /// <summary>控制攻击者或目标倒地。</summary>
    private static bool Downed(Pawn __instance, ref bool __result) { __result = __instance == current.Pawn ? current.CasterDowned : current.TargetDowned; return false; }
    /// <summary>控制预热。</summary>
    private static bool Warming(ref bool __result) { __result = current.Warming; return false; }
    /// <summary>控制原版冷却姿态。</summary>
    private static bool Busy(ref bool __result) { __result = current.Busy; return false; }
    /// <summary>控制目标是否仍在近战范围。</summary>
    private static bool CanHit(ref bool __result) { __result = current.Hittable; return false; }
    /// <summary>控制正式会话有效性。</summary>
    private static bool SessionValid(ref bool __result) { __result = current.SessionAccepted; return false; }
    /// <summary>使用真实上下文绑定，隔离表达结果重建，不替代本次待测续段控制。</summary>
    private static bool Prepare(object[] __args, ref bool __result)
    {
        current.Prepares++;
        object context = FormatterServices.GetUninitializedObject(core.GetType("BDP.Core.AttackExecution.MeleeAttackExecutionContext"));
        Set(context, "<Verb>k__BackingField", current.Next);
        Set(context, "<Target>k__BackingField", new LocalTargetInfo(current.Target));
        Set(context, "<NextRuntimeStepIndex>k__BackingField", current.Remaining);
        Set(context, "<PlanReason>k__BackingField", Enum.Parse(core.GetType("BDP.Core.AttackExecution.AttackExecutionReason"), "AutoMelee"));
        if (current.PrepareAccepted) Call(current.Next, "ApplyExecutionContext", context);
        if (current.ReplaceJob) current.Pawn.jobs.curJob = new Job { loadID = 44 };
        __args[2] = context;
        __result = current.PrepareAccepted;
        return false;
    }
    /// <summary>仅记录原版起手；正式宿主成功激活分支照常执行。</summary>
    private static bool StartCast(Verb __instance, LocalTargetInfo castTarg, ref bool __result) { current.Casts++; current.StartedVerb = __instance; current.StartedTarget = castTarg.Thing; __result = current.CastAccepted; return false; }
    /// <summary>显示与诊断不属于本次隔离行为检查。</summary>
    private static bool Skip() { return false; }
    /// <summary>创建对象而不触发引擎构造。</summary>
    private static T Blank<T>() { return (T)FormatterServices.GetUninitializedObject(typeof(T)); }
    /// <summary>设置真实字段。</summary>
    private static void Set(object instance, string name, object value) { AccessTools.Field(instance.GetType(), name).SetValue(instance, value); }
    /// <summary>读取实际状态。</summary>
    private static object Get(object instance, string name) { return AccessTools.Field(instance.GetType(), name).GetValue(instance); }
    /// <summary>调用真实非公开成员。</summary>
    private static object Call(object instance, string name, params object[] args) { return AccessTools.Method(instance.GetType(), name).Invoke(instance, args); }
    /// <summary>断言可观察行为。</summary>
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    /// <summary>解析游戏与核心依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args) { foreach (string directory in directories) { string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); } return null; }

    /// <summary>只保存边界前提和观察值，不模拟待测自动推进器。</summary>
    private sealed class Fixture
    {
        /// <summary>原版人物、原任务和固定攻击目标。</summary>
        public Pawn Pawn = Blank<Pawn>();
        /// <summary>原版任务编号用于存档后继续辨认源任务。</summary>
        public Job Job;
        /// <summary>固定目标。</summary>
        public Thing Target = Blank<Thing>();
        /// <summary>首段正式宿主。</summary>
        public BdpVerb_FormalHostMelee Old = Blank<BdpVerb_FormalHostMelee>();
        /// <summary>副手正式宿主，可改为同宿主验证交接。</summary>
        public BdpVerb_FormalHostMelee Next = Blank<BdpVerb_FormalHostMelee>();
        /// <summary>受控边界前提。</summary>
        public bool Busy, Warming, ReplaceJob, CasterDowned, TargetDowned, TargetDead;
        /// <summary>受控边界默认有效。</summary>
        public bool TargetSpawned = true, Hittable = true, SessionAccepted = true, PrepareAccepted = true, CastAccepted = true;
        /// <summary>下一段后的剩余索引，默认到此结束。</summary>
        public int Remaining = -1;
        /// <summary>真正进入准备和起手的次数。</summary>
        public int Prepares, Casts;
        /// <summary>实际起手动作。</summary>
        public Verb StartedVerb;
        /// <summary>实际起手目标。</summary>
        public Thing StartedTarget;

        /// <summary>原版任务仍由人物持有；不构造 BDP 作业推进器。</summary>
        public Fixture()
        {
            Pawn.jobs = Blank<Pawn_JobTracker>();
            Pawn.stances = Blank<Pawn_StanceTracker>();
            Job = new Job { loadID = 42, targetA = new LocalTargetInfo(Target) };
            Pawn.jobs.curJob = Job;
            Old.caster = Pawn;
            Next.caster = Pawn;
            Set(Old, "nextRuntimeStepIndex", 1);
            Set(Old, "planReason", Enum.Parse(core.GetType("BDP.Core.AttackExecution.AttackExecutionReason"), "AutoMelee"));
        }
    }
}
