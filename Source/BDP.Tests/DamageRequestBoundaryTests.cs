using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

/// <summary>直接调用真实承伤入口与作用域，检查普通、嵌套及单次请求身份；不模拟伤害工作器。</summary>
internal static class DamageRequestBoundaryTests
{
    /// <summary>依赖目录。</summary>
    private static string[] directories;
    /// <summary>正式基础程序集。</summary>
    private static Assembly core;
    /// <summary>反射成员范围。</summary>
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    /// <summary>运行承伤作用域的行为边界。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try
        {
            core = Assembly.LoadFrom(Path.Combine(args[2], "BDP.Core.dll"));
            TestNullBoundary();
            TestRequestAndNestedDamage();
            TestExplosionClaim();
            TestActualPatchesAndDelayedContext();
            Console.WriteLine("通过：普通承伤不采集、单次请求、同目标嵌套隔离、异常恢复、爆炸对象唯一绑定、真实补丁安装及延迟范围上下文恢复。");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>观察真实承伤入口内的来源、穿盾与跨帧范围反馈，而非重写伤害算法。</summary>
    public static void ObserveActualDamage()
    {
        Check(ReferenceEquals(CurrentSemantic(), expectedSemantic), "实际承伤未恢复爆炸来源");
        Check(ReferenceEquals(CurrentPolicy(), expectedPolicy), "实际承伤未恢复爆炸盾策略");
        Check(ReferenceEquals(T("BDP.Core.Projectiles.RangedFlightProtocol.Impact.ExplosionImpactRuntimeScope")
            .GetProperty("Current").GetValue(null, null), expectedImpact), "延迟承伤未恢复颜色所在的范围配置");
        observedDamage = true;
    }

    /// <summary>本次真实入口观察预期。</summary>
    private static object expectedSemantic, expectedPolicy, expectedImpact;
    /// <summary>真实前置观察是否执行。</summary>
    private static bool observedDamage;

    /// <summary>让 Harmony 实际安装三个必要接线，并在创建作用域结束后恢复爆炸上下文。</summary>
    private static void TestActualPatchesAndDelayedContext()
    {
        Harmony harmony = new Harmony("bdp.tests.damage.request.boundaries");
        string[] names = { "Patch_Thing_TakeDamage_BdpResolution", "Patch_GenExplosion_DoExplosion_BdpCreation",
            "Patch_DamageWorker_ExplosionDamageThing_BdpSemantics" };
        foreach (string name in names) harmony.CreateClassProcessor(T("BDP.Patches." + name)).Patch();
        Type thingType = Type.GetType("Verse.Thing, Assembly-CSharp", true);
        MethodInfo take = thingType.GetMethod("TakeDamage");
        harmony.Patch(take, prefix: new HarmonyMethod(typeof(DamageRequestBoundaryTests).GetMethod("ObserveActualDamage"))
            { priority = Priority.Last });
        object target = FormatterServices.GetUninitializedObject(thingType);
        object explosion = FormatterServices.GetUninitializedObject(Type.GetType("Verse.Explosion, Assembly-CSharp", true));
        expectedSemantic = Activator.CreateInstance(T("BDP.Core.Semantics.SemanticContext"));
        expectedPolicy = Activator.CreateInstance(T("BDP.Core.Projectiles.Interaction.ProjectileInteractionPolicy"));
        expectedImpact = Activator.CreateInstance(T("BDP.Core.Projectiles.RangedFlightProtocol.Impact.ExplosionImpactDispatchContext"));
        expectedImpact.GetType().GetProperty("SemanticContext").SetValue(expectedImpact, expectedSemantic, null);
        expectedImpact.GetType().GetProperty("InteractionPolicy").SetValue(expectedImpact, expectedPolicy, null);
        expectedImpact.GetType().GetProperty("HasHitFeedbackColor").SetValue(expectedImpact, true, null);
        T("BDP.Core.Semantics.BdpDamageSemanticBridge").GetMethod("AssignExplosionImpactContext")
            .Invoke(null, new[] { explosion, expectedImpact });
        object info = Activator.CreateInstance(Type.GetType("Verse.DamageInfo, Assembly-CSharp", true));
        T("BDP.Patches.Patch_DamageWorker_ExplosionDamageThing_BdpSemantics").GetMethod("ApplyExplosionDamage")
            .Invoke(null, new[] { target, info, explosion });
        Check(observedDamage, "真实承伤接线没有执行");
        Check(CurrentSemantic() == null && CurrentPolicy() == null, "真实承伤完成后泄漏来源或盾策略");
        Check(T("BDP.Core.Projectiles.RangedFlightProtocol.Impact.ExplosionImpactRuntimeScope")
            .GetProperty("Current").GetValue(null, null) == null, "真实承伤完成后泄漏范围反馈配置");
        harmony.UnpatchAll(harmony.Id);
    }

    /// <summary>空语义必须屏蔽外层，结束时再恢复。</summary>
    private static void TestNullBoundary()
    {
        Type scope = T("BDP.Core.Semantics.SemanticRuntimeScope");
        object semantic = Activator.CreateInstance(T("BDP.Core.Semantics.SemanticContext"));
        using ((IDisposable)scope.GetMethod("Push").Invoke(null, new object[] { semantic }))
        {
            using ((IDisposable)scope.GetMethod("Push").Invoke(null, new object[] { null }))
                Check(scope.GetProperty("Current").GetValue(null, null) == null, "空语义没有隔离外层 BDP 来源");
            Check(ReferenceEquals(scope.GetProperty("Current").GetValue(null, null), semantic), "空语义退出没有恢复外层");
        }
    }

    /// <summary>当前真实补丁普通早退；同目标独立伤害不继承盾策略、不污染外层结果。</summary>
    private static void TestRequestAndNestedDamage()
    {
        Type thingType = Type.GetType("Verse.Thing, Assembly-CSharp", true);
        object target = FormatterServices.GetUninitializedObject(thingType);
        Type infoType = Type.GetType("Verse.DamageInfo, Assembly-CSharp", true);
        object info = Activator.CreateInstance(infoType);
        infoType.GetMethod("SetAmount").Invoke(info, new object[] { 1f });
        Type patch = T("BDP.Patches.Patch_Thing_TakeDamage_BdpResolution");
        object ordinary = Enter(patch, target, info);
        Check(ordinary == null, "普通承伤不应分配 BDP 捕获记录");
        object semantic = Activator.CreateInstance(T("BDP.Core.Semantics.SemanticContext"));
        object policy = Activator.CreateInstance(T("BDP.Core.Projectiles.Interaction.ProjectileInteractionPolicy"));
        policy.GetType().GetProperty("BypassRegisteredDamageShields").SetValue(policy, true, null);
        Type runtime = T("BDP.Core.Projectiles.RangedFlightProtocol.Impact.DamageResolutionRuntime");
        MethodInfo beginRequest = runtime.GetMethod("BeginRequest", Flags);
        Check(beginRequest != null, "缺少显式单次承伤请求");
        using (IDisposable request = (IDisposable)beginRequest.Invoke(null, new object[] { target, info, semantic, policy, null }))
        {
            object outer = Enter(patch, target, info);
            Check(outer != null && CurrentPolicy() == policy, "显式 BDP 承伤未取得自己的策略");
            object nested = Enter(patch, target, info);
            Check(nested != null && CurrentPolicy() == null, "同目标独立嵌套承伤继承了穿盾");
            Check(CurrentSemantic() == null, "独立嵌套承伤继承了来源");
            runtime.GetMethod("MarkAbsorbed").Invoke(null, new object[] { target });
            Exit(patch, nested, new InvalidOperationException("nested"));
            Check(CurrentPolicy() == policy && CurrentSemantic() == semantic, "异常退出没有恢复外层");
            Exit(patch, outer, new InvalidOperationException("outer"));
            Check(CurrentPolicy() == null && CurrentSemantic() == null, "外层异常退出没有清理");
        }
        using (IDisposable request = (IDisposable)beginRequest.Invoke(null, new object[] { target, info, semantic, policy, null }))
        {
            object capture = Enter(patch, target, info);
            runtime.GetMethod("MarkAbsorbed").Invoke(null, new object[] { target });
            object result = Activator.CreateInstance(Type.GetType("Verse.DamageWorker+DamageResult, Assembly-CSharp", true));
            patch.GetMethod("Postfix").Invoke(null, new object[] { result, capture });
            Exit(patch, capture, null);
            object resolution = request.GetType().GetProperty("Resolution").GetValue(request, null);
            Check((bool)resolution.GetType().GetProperty("IsShieldBlocked").GetValue(resolution, null), "本次护盾结果没有归属显式请求");
            Check(Enter(patch, target, info) == null, "已消费请求被另一普通调用再次复用");
        }
    }

    /// <summary>一次创建请求只能绑定一个目标爆炸，普通同步爆炸不继承。</summary>
    private static void TestExplosionClaim()
    {
        Type scope = T("BDP.Core.Projectiles.RangedFlightProtocol.Impact.ExplosionImpactRuntimeScope");
        MethodInfo prepare = scope.GetMethod("PrepareExplosion", Flags);
        MethodInfo enter = scope.GetMethod("EnterCreation", Flags);
        MethodInfo bind = scope.GetMethod("BindCreatedExplosion", Flags);
        Type bridge = T("BDP.Core.Semantics.BdpDamageSemanticBridge");
        Check(prepare != null && enter != null && bind != null, "缺少单个爆炸绑定边界");
        object explosion = FormatterServices.GetUninitializedObject(Type.GetType("Verse.Explosion, Assembly-CSharp", true));
        FieldInfo mapState = Type.GetType("Verse.Thing, Assembly-CSharp", true).GetField("mapIndexOrState", Flags);
        mapState.SetValue(explosion, Convert.ChangeType(-1, mapState.FieldType));
        object center = Activator.CreateInstance(Type.GetType("Verse.IntVec3, Assembly-CSharp", true));
        object semantic = Activator.CreateInstance(T("BDP.Core.Semantics.SemanticContext"));
        using ((IDisposable)prepare.Invoke(null, new object[] { null, center, null, semantic }))
        {
            using ((IDisposable)enter.Invoke(null, new object[] { null, center }))
            {
                using ((IDisposable)enter.Invoke(null, new object[] { null, center }))
                {
                    bind.Invoke(null, new object[] { explosion });
                    Check(bridge.GetMethod("GetExplosionContext").Invoke(null, new[] { explosion }) == null,
                        "生成回调先创建的同位置独立爆炸偷取了外层请求");
                }
                bind.Invoke(null, new object[] { explosion });
                Check(ReferenceEquals(bridge.GetMethod("GetExplosionContext").Invoke(null, new[] { explosion }), semantic),
                    "实际创建对象没有取得本次来源");
                object independent = FormatterServices.GetUninitializedObject(explosion.GetType());
                bind.Invoke(null, new[] { independent });
                Check(bridge.GetMethod("GetExplosionContext").Invoke(null, new[] { independent }) == null,
                    "已绑定请求被另一爆炸复用");
            }
        }
        Check(enter.Invoke(null, new object[] { null, center }) == null, "普通创建留下了 BDP 状态");
    }

    /// <summary>调用真实前置入口。</summary>
    private static object Enter(Type patch, object target, object info)
    {
        object[] args = { target, info, null };
        patch.GetMethod("Prefix").Invoke(null, args);
        return args[2];
    }

    /// <summary>调用真实异常出口。</summary>
    private static void Exit(Type patch, object state, Exception error)
    {
        patch.GetMethod("Finalizer").Invoke(null, new object[] { error, state });
    }

    /// <summary>读取当前策略。</summary>
    private static object CurrentPolicy() { return T("BDP.Core.Projectiles.Interaction.ProjectileInteractionPolicyScope").GetProperty("Current").GetValue(null, null); }
    /// <summary>读取当前语义。</summary>
    private static object CurrentSemantic() { return T("BDP.Core.Semantics.SemanticRuntimeScope").GetProperty("Current").GetValue(null, null); }
    /// <summary>读取基础层真实类型。</summary>
    private static Type T(string name) { return core.GetType(name, true); }
    /// <summary>报告真实失败。</summary>
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    /// <summary>解析游戏及模组依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string file = new AssemblyName(args.Name).Name + ".dll";
        foreach (string directory in directories)
        {
            string path = Path.Combine(directory, file);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }
}
