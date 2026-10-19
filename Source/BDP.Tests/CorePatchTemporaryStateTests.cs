using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using BDP.Core.AttackExecution;
using BDP.Patches;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

/// <summary>调用真实 Core 类型与真实原版连射入口，检验补丁临时状态边界。</summary>
internal static class CorePatchTemporaryStateTests
{
    /// <summary>运行时依赖搜索目录。</summary>
    private static string[] directories;
    /// <summary>当前 Core 程序集。</summary>
    private static Assembly core;
    /// <summary>失败项目数。</summary>
    private static int failures;
    /// <summary>真实攻击事件计数。</summary>
    private static int attackCount;

    /// <summary>运行确定性的状态与补丁行为测试；游戏绘制仍需实测。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        core = Assembly.LoadFrom(Path.Combine(args[2], "BDP.Core.dll"));
        InitializeGame();
        Run("失败发射不通知，成功发射只通知一次", TestShotResult);
        Run("发射锚点失配原样保留真实原版指令", TestShotAnchorFallback);
        Run("嵌套和异常发射不串用成功事实", TestShotNestedAndException);
        Run("菜单嵌套正常退出恢复外层提示", TestMenuNested);
        Run("菜单换装备退出只恢复原组件", TestMenuEquipmentChange);
        Run("菜单异常退出恢复原提示", TestMenuException);
        Run("命中色不会强持有已离开的角色", TestColorWeakOwnership);
        Run("命中色保留期限并到期失效", TestColorExpiry);
        Run("身体版本不串用相同物品编号的不同角色", TestBodyVersionIdentity);
        Run("身体版本不会强持有已释放角色", TestBodyVersionWeakOwnership);
        Run("无 BDP 状态的资格检查不分配清理快照", TestOrdinaryEligibility);
        Console.WriteLine("CorePatchTemporaryStateTests: " + (failures == 0 ? "PASS" : "FAIL=" + failures));
        return failures == 0 ? 0 : 1;
    }

    /// <summary>每项独立报告，保证旧实现的多个失败均可被观察。</summary>
    private static void Run(string name, Action test)
    {
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception error) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + error); }
    }

    /// <summary>装载游戏与 Harmony 的真实依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in directories)
        {
            string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>提供真实游戏所有者壳，避免离线调用 Find.TickManager 时没有 Game。</summary>
    private static void InitializeGame() { Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game)); }

    /// <summary>只补原版相等比较必需的定义，不执行地图/材质构造。</summary>
    private static Pawn NewPawn()
    {
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "BDP_TestPawn";
        return pawn;
    }

    /// <summary>真实原版 TryCastNextBurstShot 失败会清零，不能据此推测成功。</summary>
    private static void TestShotResult()
    {
        Harmony harmony = new Harmony("bdp.tests.core.temporary-state");
        harmony.CreateClassProcessor(typeof(Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess)).Patch();
        AttackActionSuccessDispatcher.AttackActionSucceeded += OnAttack;
        try
        {
            TestLaunchVerb verb = new TestLaunchVerb();
            verb.caster = new Thing();
            verb.verbProps = new VerbProperties();
            MethodInfo burst = AccessTools.Method(typeof(Verb), "TryCastNextBurstShot");
            FieldInfo remaining = AccessTools.Field(typeof(Verb), "burstShotsLeft");
            attackCount = 0;
            remaining.SetValue(verb, 1);
            verb.Succeeds = false;
            burst.Invoke(verb, null);
            Check((int)remaining.GetValue(verb) == 0, "原版失败应清零，夹具必须走真实失败分支");
            Check(attackCount == 0, "未实际发射却收到 " + attackCount + " 次成功通知");
            remaining.SetValue(verb, 1);
            verb.Succeeds = true;
            burst.Invoke(verb, null);
            Check(attackCount == 1, "实际成功应通知一次");
        }
        finally { AttackActionSuccessDispatcher.AttackActionSucceeded -= OnAttack; harmony.UnpatchAll(harmony.Id); }
    }

    /// <summary>仅统计该夹具产生的事件，不依赖游戏隐身消费者。</summary>
    private static void OnAttack(AttackActionSuccess attack) { attackCount++; }

    /// <summary>检查真实游戏 IL 唯一锚点，并验证第三方改写后缺失/重复锚点不破坏原指令。</summary>
    private static void TestShotAnchorFallback()
    {
        Harmony diagnostic = new Harmony("bdp.tests.core.anchor-diagnostics");
        Type diagnostics = core.GetType("BDP.Support.Diagnostics.BdpDiagnostics", true);
        diagnostic.Patch(AccessTools.PropertyGetter(diagnostics, "Enabled"),
            prefix: new HarmonyMethod(typeof(CorePatchTemporaryStateTests), "SkipDiagnosticOutput"));
        try { CheckShotAnchorFallback(); }
        finally { diagnostic.UnpatchAll(diagnostic.Id); }
    }

    /// <summary>无 Unity 宿主时仅关闭此夹具诊断输出，不替换生产翻译键或原版方法。</summary>
    private static bool SkipDiagnosticOutput(ref bool __result) { __result = false; return false; }

    /// <summary>直接对真实原版指令检查有效、缺失和重复锚点。</summary>
    private static void CheckShotAnchorFallback()
    {
        MethodInfo burst = AccessTools.Method(typeof(Verb), "TryCastNextBurstShot");
        MethodInfo cast = AccessTools.Method(typeof(Verb), "TryCastShot");
        List<CodeInstruction> original = PatchProcessor.GetOriginalInstructions(burst).ToList();
        Check(original.Count(item => item.Calls(cast)) == 1, "当前真实原版发射锚点应唯一");
        MethodInfo transpiler = AccessTools.Method(typeof(Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess), "Transpiler");
        List<CodeInstruction> edited = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { original })).ToList();
        Check(edited.Count == original.Count + 3, "有效锚点仅插入三条只读观察指令");
        Check(edited.Count(item => item.Calls(cast)) == 1, "必须保留原版真实虚调用");
        List<CodeInstruction> missing = original.Where(item => !item.Calls(cast)).ToList();
        List<CodeInstruction> repeated = new List<CodeInstruction>(original);
        repeated.Add(original.First(item => item.Calls(cast)));
        foreach (List<CodeInstruction> input in new[] { missing, repeated })
        {
            List<CodeInstruction> result = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { input })).ToList();
            Check(result.Count == input.Count, "失配时不能新增/删除指令");
            for (int index = 0; index < result.Count; index++)
                Check(ReferenceEquals(input[index], result[index]), "失配时必须保留原指令与标签");
        }
    }

    /// <summary>真实连射入口中的同步嵌套与异常退出应各持自己的结果。</summary>
    private static void TestShotNestedAndException()
    {
        Harmony harmony = new Harmony("bdp.tests.core.nested-shots");
        harmony.CreateClassProcessor(typeof(Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess)).Patch();
        AttackActionSuccessDispatcher.AttackActionSucceeded += OnAttack;
        try
        {
            MethodInfo burst = AccessTools.Method(typeof(Verb), "TryCastNextBurstShot");
            FieldInfo remaining = AccessTools.Field(typeof(Verb), "burstShotsLeft");
            TestLaunchVerb inner = NewLaunchVerb(true);
            TestLaunchVerb outer = NewLaunchVerb(false);
            remaining.SetValue(inner, 1);
            remaining.SetValue(outer, 1);
            outer.BeforeResult = delegate { burst.Invoke(inner, null); };
            attackCount = 0;
            burst.Invoke(outer, null);
            Check(attackCount == 1, "只应通知成功内层，失败外层不能继承成功");
            FieldInfo observation = AccessTools.Field(typeof(Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess), "current");
            Check(observation.GetValue(null) == null, "嵌套正常退出未清观察状态");
            TestLaunchVerb exceptional = NewLaunchVerb(false);
            exceptional.BeforeResult = delegate { throw new InvalidOperationException("夹具原版回调异常"); };
            remaining.SetValue(exceptional, 1);
            try { burst.Invoke(exceptional, null); throw new Exception("异常应原样传出"); }
            catch (TargetInvocationException error) { Check(error.InnerException is InvalidOperationException, "异常类型被改变"); }
            Check(observation.GetValue(null) == null, "异常退出未清观察状态");
            Check(attackCount == 1, "异常起手不得发布成功");
            TestLaunchVerb next = NewLaunchVerb(true);
            remaining.SetValue(next, 1);
            burst.Invoke(next, null);
            Check(attackCount == 2, "异常后下一轮应正常成功一次");
        }
        finally { AttackActionSuccessDispatcher.AttackActionSucceeded -= OnAttack; harmony.UnpatchAll(harmony.Id); }
    }

    /// <summary>构造不依赖地图的真实原版武器动词。</summary>
    private static TestLaunchVerb NewLaunchVerb(bool succeeds)
    {
        return new TestLaunchVerb { caster = new Thing(), verbProps = new VerbProperties(), Succeeds = succeeds };
    }

    /// <summary>正常退出的嵌套菜单必须恢复外层既有提示。</summary>
    private static void TestMenuNested()
    {
        CompTriggerFixture fixture = NewTriggerFixture();
        object original = NewResult();
        SetHint(fixture.Comp, original);
        object outer = BeginMenu(fixture.Pawn);
        object outerResult = NewResult();
        SetHint(fixture.Comp, outerResult);
        object inner = BeginMenu(fixture.Pawn);
        EndMenu(fixture.Pawn, inner, false);
        Check(ReferenceEquals(Hint(fixture.Comp), outerResult), "内层结束没有恢复外层提示");
        EndMenu(fixture.Pawn, outer, false);
        Check(ReferenceEquals(Hint(fixture.Comp), original), "外层结束没有恢复原提示");
    }

    /// <summary>菜单打开后换装备不能清理新装备的提示。</summary>
    private static void TestMenuEquipmentChange()
    {
        CompTriggerFixture first = NewTriggerFixture();
        CompTriggerFixture second = NewTriggerFixture();
        object firstHint = NewResult();
        object secondHint = NewResult();
        SetHint(first.Comp, firstHint);
        SetHint(second.Comp, secondHint);
        object scope = BeginMenu(first.Pawn);
        first.Pawn.equipment.AllEquipmentListForReading.Clear();
        first.Pawn.equipment.AllEquipmentListForReading.Add(second.Equipment);
        EndMenu(first.Pawn, scope, false);
        Check(ReferenceEquals(Hint(first.Comp), firstHint), "没有恢复实际写入的原组件");
        Check(ReferenceEquals(Hint(second.Comp), secondHint), "错误清空了后来装备组件的提示");
    }

    /// <summary>异常退出也必须恢复真正写入的组件与原提示。</summary>
    private static void TestMenuException()
    {
        CompTriggerFixture fixture = NewTriggerFixture();
        object original = NewResult();
        SetHint(fixture.Comp, original);
        object scope = BeginMenu(fixture.Pawn);
        EndMenu(fixture.Pawn, scope, true);
        Check(ReferenceEquals(Hint(fixture.Comp), original), "异常退出遗留菜单提示");
    }

    /// <summary>构造真实 Pawn/装备追踪器/触发体，只绕开游戏生成与装备通知。</summary>
    private static CompTriggerFixture NewTriggerFixture()
    {
        Pawn pawn = NewPawn();
        pawn.equipment = new Pawn_EquipmentTracker(pawn);
        ThingWithComps equipment = new ThingWithComps();
        equipment.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        equipment.def.equipmentType = EquipmentType.Primary;
        Type type = core.GetType("BDP.Core.Trigger.CompTriggerBody", true);
        ThingComp comp = (ThingComp)FormatterServices.GetUninitializedObject(type);
        comp.parent = equipment;
        AccessTools.Field(typeof(ThingWithComps), "comps").SetValue(equipment, new List<ThingComp> { comp });
        pawn.equipment.AllEquipmentListForReading.Add(equipment);
        return new CompTriggerFixture { Pawn = pawn, Equipment = equipment, Comp = comp };
    }

    /// <summary>调用真实补丁 Prefix，取得本轮查询作用域。</summary>
    private static object BeginMenu(Pawn pawn)
    {
        MethodInfo method = AccessTools.Method(typeof(Patch_FloatMenuUtility_GetRangedAttackAction), "Prefix");
        object[] args = { pawn, new LocalTargetInfo(new IntVec3(1, 0, 1)), null };
        method.Invoke(null, args);
        return args[2];
    }

    /// <summary>调用真实正常/异常退出函数，不复制其清理逻辑。</summary>
    private static void EndMenu(Pawn pawn, object state, bool exceptional)
    {
        Type patch = typeof(Patch_FloatMenuUtility_GetRangedAttackAction);
        if (exceptional)
        {
            MethodInfo finalizer = AccessTools.Method(patch, "Finalizer");
            finalizer.Invoke(null, new object[] { state, new InvalidOperationException("夹具异常") });
            return;
        }
        MethodInfo postfix = AccessTools.Method(patch, "Postfix");
        postfix.Invoke(null, new object[] { state });
        MethodInfo cleanup = AccessTools.Method(patch, "Finalizer");
        cleanup.Invoke(null, new object[] { state, null });
    }

    /// <summary>创建只作身份标记的真实表达结果。</summary>
    private static object NewResult() { return FormatterServices.GetUninitializedObject(core.GetType("BDP.Core.Expressions.FormalExpressionResult", true)); }
    /// <summary>读取真实组件提示字段。</summary>
    private static object Hint(object comp) { return AccessTools.Field(comp.GetType(), "pendingRangedTargetResultHint").GetValue(comp); }
    /// <summary>放入外层已存在的真实结果身份，检验作用域恢复。</summary>
    private static void SetHint(object comp, object value) { AccessTools.Field(comp.GetType(), "pendingRangedTargetResultHint").SetValue(comp, value); }

    /// <summary>退出后的角色不应被短暂视觉记录长期持有。</summary>
    private static void TestColorWeakOwnership()
    {
        WeakReference pawn = RegisterColorAndRelease();
        for (int i = 0; i < 4 && pawn.IsAlive; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Check(!pawn.IsAlive, "静态命中色表仍强持有角色");
    }

    /// <summary>隔离 JIT 局部变量寿命，登记真实颜色后只留下弱引用。</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterColorAndRelease()
    {
        Pawn pawn = NewPawn();
        Type runtime = core.GetType("BDP.Core.Projectiles.RangedFlightProtocol.Impact.HitFeedbackColorRuntime", true);
        AccessTools.Method(runtime, "Register").Invoke(null, new object[] { pawn, Color.green });
        return new WeakReference(pawn);
    }

    /// <summary>弱键改造不应改变原有命中色窗口或到期清理。</summary>
    private static void TestColorExpiry()
    {
        TickManager previous = Current.Game.tickManager;
        Current.Game.tickManager = (TickManager)FormatterServices.GetUninitializedObject(typeof(TickManager));
        try
        {
            FieldInfo ticks = AccessTools.Field(typeof(TickManager), "ticksGameInt");
            ticks.SetValue(Current.Game.tickManager, 500);
            Pawn pawn = NewPawn();
            Type runtime = core.GetType("BDP.Core.Projectiles.RangedFlightProtocol.Impact.HitFeedbackColorRuntime", true);
            AccessTools.Method(runtime, "Register").Invoke(null, new object[] { pawn, Color.green });
            MethodInfo get = AccessTools.Method(runtime, "TryGetColor");
            object[] values = { pawn, Color.white };
            Check((bool)get.Invoke(null, values) && ((Color)values[1]).Equals(Color.green), "注册颜色应能读取");
            ticks.SetValue(Current.Game.tickManager, 516);
            Check((bool)get.Invoke(null, values), "保持原有 16 刻截止边界");
            ticks.SetValue(Current.Game.tickManager, 517);
            Check(!(bool)get.Invoke(null, values), "到期颜色应失效");
        }
        finally { Current.Game.tickManager = previous; }
    }

    /// <summary>版本应属于真实角色实例，而非不同存档可重用的编号。</summary>
    private static void TestBodyVersionIdentity()
    {
        Type hub = core.GetType("BDP.Core.BodyConstraints.PawnBodyConstraintSignalHub", true);
        MethodInfo publish = AccessTools.Method(hub, "Publish");
        MethodInfo get = AccessTools.Method(hub, "GetVersion");
        Pawn first = NewPawn();
        Pawn second = NewPawn();
        first.thingIDNumber = second.thingIDNumber = 945318;
        object kind = Enum.ToObject(publish.GetParameters()[1].ParameterType, 0);
        publish.Invoke(null, new object[] { first, kind });
        Check((int)get.Invoke(null, new object[] { first }) == 1, "发布者版本应递增");
        Check((int)get.Invoke(null, new object[] { second }) == 0, "不同角色共用了旧编号版本");
    }

    /// <summary>版本弱键不应反向持有人物对象。</summary>
    private static void TestBodyVersionWeakOwnership()
    {
        WeakReference pawn = PublishVersionAndRelease();
        for (int i = 0; i < 4 && pawn.IsAlive; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Check(!pawn.IsAlive, "身体版本仍强持有角色");
    }

    /// <summary>隔离 JIT 局部变量寿命，仅留下已发布者弱引用。</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference PublishVersionAndRelease()
    {
        Pawn pawn = NewPawn();
        Type hub = core.GetType("BDP.Core.BodyConstraints.PawnBodyConstraintSignalHub", true);
        MethodInfo publish = AccessTools.Method(hub, "Publish");
        publish.Invoke(null, new object[] { pawn, Enum.ToObject(publish.GetParameters()[1].ParameterType, 0) });
        return new WeakReference(pawn);
    }

    /// <summary>真实普通角色热路径不应反复分配失效清理快照。</summary>
    private static void TestOrdinaryEligibility()
    {
        MethodInfo allocation = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread");
        if (allocation == null) { Console.WriteLine("SKIP 当前运行时缺少逐线程分配计数，资格性能需游戏内验证"); return; }
        Func<long> allocated = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), allocation);
        Type eligibility = core.GetType("BDP.Core.Trigger.TriggerBodyEquipmentEligibility", true);
        Action<Pawn> validate = (Action<Pawn>)Delegate.CreateDelegate(typeof(Action<Pawn>), AccessTools.Method(eligibility, "ValidateAfterLoadOrTick"));
        Pawn pawn = NewPawn();
        for (int i = 0; i < 10; i++) validate(pawn);
        long before = allocated();
        for (int i = 0; i < 1000; i++) validate(pawn);
        long delta = allocated() - before;
        Check(delta < 1024, "普通检查分配了 " + delta + " 字节");
    }

    /// <summary>仅替代真实发射的游戏副作用；连射控制流仍为真实 Verse.Verb。</summary>
    private sealed class TestLaunchVerb : Verb_LaunchProjectile
    {
        /// <summary>本次真实 TryCastShot 返回事实。</summary>
        public bool Succeeds;
        /// <summary>模拟原版伤害/状态回调中的同步嵌套或异常。</summary>
        public Action BeforeResult;
        /// <summary>消除地图资格依赖，使测试可走两条原版分支。</summary>
        public override bool Available() { return true; }
        /// <summary>提供可控的真实虚调用结果，不复制连射算法。</summary>
        protected override bool TryCastShot() { if (BeforeResult != null) BeforeResult(); return Succeeds; }
    }

    /// <summary>菜单夹具的真实游戏对象集合。</summary>
    private sealed class CompTriggerFixture
    {
        /// <summary>当前角色。</summary>
        public Pawn Pawn;
        /// <summary>实际装备。</summary>
        public ThingWithComps Equipment;
        /// <summary>真实触发体组件。</summary>
        public object Comp;
    }

    /// <summary>检查可观察行为。</summary>
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
