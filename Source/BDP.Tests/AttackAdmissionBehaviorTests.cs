using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BDP.Core.Abilities;
using BDP.Core.Verbs;
using BDP.Patches;
using HarmonyLib;
using Verse;

/// <summary>对真实核心执行准入查询及起手边界；不模拟地图和资源扣除。</summary>
public static class AttackAdmissionBehaviorTests
{
    /// <summary>真实依赖目录。</summary>
    private static string[] directories;
    /// <summary>规则聚合入口。</summary>
    private static Type access;
    /// <summary>动态规则类型。</summary>
    private static Type ruleType;
    /// <summary>验证无状态规则和自有早期拒绝。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        return Run(args);
    }

    /// <summary>注册程序集搜索后再进入需要游戏类型的验证。</summary>
    private static int Run(string[] args)
    {
        try
        {
            Assembly core = Assembly.LoadFrom(Path.Combine(args[2], "BDP.Core.dll"));
            access = core.GetType("BDP.Core.AttackExecution.AttackAdmissionAccess");
            Check(access != null, "核心缺少统一攻击准入查询");
            Check(Query().Accepted, "没有业务规则时必须允许");
            ruleType = CreateRuleType(core.GetType("BDP.Core.AttackExecution.IAttackAdmissionRule"));
            object first = NewRule(true);
            Register(first);
            Register(first);
            Check(Query().Accepted && Calls(first) == 1, "重复注册同一个规则不得重复裁决");
            object deny = NewRule(false);
            Register(deny);
            object last = NewRule(true);
            Register(last);
            Check(!Query().Accepted && Query().Reason == "测试拒绝", "拒绝不能被后续允许覆盖，原因必须保留");
            Check(Calls(last) == 0, "第一个拒绝后不应再执行后续规则");
            ruleType.GetField("Allow").SetValue(deny, true);
            Check(Query().Accepted, "业务状态改变后应立即恢复，不能缓存人物禁攻");
            ruleType.GetField("Allow").SetValue(deny, false);
            // 没有地图、能力绑定或发射计划，拒绝必须发生在这些入口准备之前。
            Check(!new BdpVerb_Shoot().TryStartCastOn(LocalTargetInfo.Invalid, LocalTargetInfo.Invalid), "射击起手未提前拒绝");
            Check(!new BdpVerb_CastAbility().TryStartCastOn(LocalTargetInfo.Invalid, LocalTargetInfo.Invalid), "能力起手未提前拒绝");
            Check(!new BdpVerb_AbilityShoot().TryStartCastOn(LocalTargetInfo.Invalid, LocalTargetInfo.Invalid), "射击能力起手未提前拒绝");
            Check(!new BdpVerb_Shoot().Available(), "射击可用性未执行准入");
            Check(!new BdpVerb_CastAbility().Available(), "能力可用性未执行准入");
            TestNativeAdapters(first);
            Console.WriteLine("AttackAdmissionBehaviorTests PASS：默认允许、注册去重、拒绝聚合、实时查询、自有起手和可用性边界。");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>真实原版外部入口受限时提前拒绝，连射重置不能执行完成回调。</summary>
    private static void TestNativeAdapters(object firstRule)
    {
        Harmony harmony = new Harmony("bdp.tests.attack.admission");
        harmony.CreateClassProcessor(typeof(Patch_Verb_Available_AttackAdmission)).Patch();
        harmony.CreateClassProcessor(typeof(Patch_Verb_TryStartCastOn_AttackAdmission)).Patch();
        harmony.CreateClassProcessor(typeof(Patch_Verb_TryCastNextBurstShot_AttackAdmission)).Patch();
        try
        {
            ExternalLaunchVerb verb = new ExternalLaunchVerb();
            verb.caster = new Thing();
            verb.verbProps = new VerbProperties();
            verb.verbTracker = new VerbTracker(null);
            int completed = 0;
            verb.castCompleteCallback = delegate { completed++; };
            Check(!verb.Available(), "原版外部可用性没有适配准入");
            Check(!verb.TryStartCastOn(LocalTargetInfo.Invalid, LocalTargetInfo.Invalid), "原版直接起手绕过了准入");
            AccessTools.Field(typeof(Verb), "burstShotsLeft").SetValue(verb, 2);
            AccessTools.Method(typeof(Verb), "TryCastNextBurstShot").Invoke(verb, null);
            Check(verb.Shots == 0 && completed == 0 && (int)AccessTools.Field(typeof(Verb), "burstShotsLeft").GetValue(verb) == 0,
                "受限连射执行了发射、原版完成回调或没有重置");
            int before = Calls(firstRule);
            Check(!new BdpVerb_Shoot().Available() && Calls(firstRule) == before + 1, "自有可用性机械重复查询");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    /// <summary>原版外部动作，只记录是否执行了真实虚调用。</summary>
    private sealed class ExternalLaunchVerb : Verb
    {
        /// <summary>实际执行次数。</summary>
        public int Shots;
        /// <summary>继承真实原版可用性和起手，只替代需要地图的发射效果。</summary>
        protected override bool TryCastShot() { Shots++; return true; }
    }

    /// <summary>让夹具在缺少新接口的旧版本也能编译，报告功能缺失而非编译失败。</summary>
    private static Type CreateRuleType(Type contract)
    {
        Check(contract != null, "核心缺少准入规则契约");
        AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("AdmissionFixture"), AssemblyBuilderAccess.Run);
        TypeBuilder type = assembly.DefineDynamicModule("Fixture").DefineType("FixtureRule", TypeAttributes.Public);
        type.AddInterfaceImplementation(contract);
        FieldBuilder allow = type.DefineField("Allow", typeof(bool), FieldAttributes.Public);
        FieldBuilder calls = type.DefineField("Calls", typeof(int), FieldAttributes.Public);
        MethodBuilder evaluate = type.DefineMethod("Evaluate", MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(AcceptanceReport), new[] { typeof(Pawn), typeof(Verb) });
        ILGenerator il = evaluate.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Dup); il.Emit(OpCodes.Ldfld, calls);
        il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stfld, calls);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, allow);
        il.Emit(OpCodes.Call, typeof(AttackAdmissionBehaviorTests).GetMethod("Report")); il.Emit(OpCodes.Ret);
        type.DefineMethodOverride(evaluate, contract.GetMethod("Evaluate"));
        return type.CreateType();
    }

    /// <summary>动态规则只读取夹具当前状态。</summary>
    public static AcceptanceReport Report(bool allow) { return allow ? (AcceptanceReport)true : (AcceptanceReport)"测试拒绝"; }
    /// <summary>创建独立规则实例。</summary>
    private static object NewRule(bool allow) { object rule = Activator.CreateInstance(ruleType); ruleType.GetField("Allow").SetValue(rule, allow); return rule; }
    /// <summary>规则查询次数。</summary>
    private static int Calls(object rule) { return (int)ruleType.GetField("Calls").GetValue(rule); }
    /// <summary>调用真实注册入口。</summary>
    private static void Register(object rule) { access.GetMethod("Register").Invoke(null, new[] { rule }); }
    /// <summary>调用真实查询入口。</summary>
    private static AcceptanceReport Query() { return (AcceptanceReport)access.GetMethod("Evaluate").Invoke(null, new object[] { null, null }); }
    /// <summary>验证可观察结果。</summary>
    private static void Check(bool result, string message) { if (!result) throw new Exception(message); }
    /// <summary>解析真实游戏依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in directories) { string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }
}
