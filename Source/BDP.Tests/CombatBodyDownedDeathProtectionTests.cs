using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Verse;

/// <summary>直接验证正式健康保护接线的概率作用域；不模拟受伤算法或运行地图。</summary>
internal static class CombatBodyDownedDeathProtectionTests
{
    /// <summary>当前游戏、Harmony 与模组依赖目录。</summary>
    private static string[] paths;
    /// <summary>包含私有成员的实例查询范围。</summary>
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    /// <summary>真实内容补丁。</summary>
    private static Type patch;
    /// <summary>真实核心程序集。</summary>
    private static Assembly core;

    /// <summary>先注册依赖解析，再读取游戏类型。</summary>
    private static int Main(string[] args)
    {
        paths = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>只加载当前安装目录的实际依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in paths)
        {
            string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>覆盖保护期、原身、嵌套与异常，不能永久更改人物概率。</summary>
    private static void Run()
    {
        // 冷却相位查询会读取 Current.Game；夹具只提供空游戏容器，不创建地图或 Unity 对象。
        Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        core = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Core.dll"));
        patch = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Content.dll")).GetType(
            "BDP.Content.CombatBody.Protection.Patch_Pawn_HealthTracker_CheckForStateChange_CombatBodyTriggerProtection", true);
        foreach (string phase in new[] { "Active", "Collapsing" })
        {
            Pawn pawn = MakePawn(phase);
            pawn.health.overrideDeathOnDownedChance = 0.73f;
            object outer = Enter(pawn);
            Check(pawn.health.overrideDeathOnDownedChance == 0f, phase + "：战斗体健康检查必须禁用倒地随机致死");
            object inner = Enter(pawn);
            Exit(pawn, inner, false);
            Check(pawn.health.overrideDeathOnDownedChance == 0f, "嵌套检查不能提前恢复外层概率");
            Exit(pawn, outer, true);
            Check(pawn.health.overrideDeathOnDownedChance == 0.73f, "异常退出必须恢复原值");
            Exit(pawn, outer, false);
            Check(pawn.health.overrideDeathOnDownedChance == 0.73f, "重复清理不能再次改写概率");
            pawn.health.overrideDeathOnDownedChance = -1f;
            outer = Enter(pawn);
            patch.GetMethod("Postfix").Invoke(null, new object[] { pawn.health, outer });
            Check(pawn.health.overrideDeathOnDownedChance == -1f, "正常退出必须恢复原版默认概率哨兵");
            Exit(pawn, outer, false);
        }
        foreach (string phase in new[] { null, "Inactive", "Cooldown" })
        {
            Pawn pawn = MakePawn(phase);
            pawn.health.overrideDeathOnDownedChance = 0.42f;
            object state = Enter(pawn);
            Check(pawn.health.overrideDeathOnDownedChance == 0.42f, "普通人物及原身不能被免除倒地随机致死");
            Exit(pawn, state, true);
            Check(pawn.health.overrideDeathOnDownedChance == 0.42f, "非保护阶段清理必须保持原值");
        }
        Console.WriteLine("通过：真实健康补丁的激活／崩解保护、正常／异常恢复、嵌套、重复清理及非保护阶段。");
    }

    /// <summary>使用真实宿主初始化与相位对象，不需要地图、装备或能力计算。</summary>
    private static Pawn MakePawn(string phase)
    {
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "TestPawn";
        pawn.health = (Pawn_HealthTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_HealthTracker));
        typeof(Pawn_HealthTracker).GetField("pawn", Fields).SetValue(pawn.health, pawn);
        if (phase == null) return pawn;
        Type compType = core.GetType("BDP.Core.CombatBody.CompCombatBodyHost", true);
        ThingComp comp = (ThingComp)Activator.CreateInstance(compType);
        comp.parent = pawn;
        comp.props = (CompProperties)Activator.CreateInstance(core.GetType("BDP.Core.CombatBody.CompProperties_CombatBodyHost", true));
        typeof(ThingWithComps).GetField("comps", Fields).SetValue(pawn, new List<ThingComp> { comp });
        core.GetType("BDP.Core.CombatBody.CombatBodySurfaceAccess", true).GetMethod("ResolveReader").Invoke(null, new object[] { pawn });
        object state = compType.GetField("state", Fields).GetValue(comp);
        FieldInfo phaseField = state.GetType().GetField("phase", Fields);
        phaseField.SetValue(state, Enum.Parse(phaseField.FieldType, phase));
        if (phase == "Cooldown") state.GetType().GetField("cooldownEndTick", Fields).SetValue(state, int.MaxValue);
        return pawn;
    }

    /// <summary>调用实际前置处理并保留当前调用的独立状态。</summary>
    private static object Enter(Pawn pawn)
    {
        object[] args = { pawn.health, null };
        patch.GetMethod("Prefix").Invoke(null, args);
        return args[1];
    }

    /// <summary>最终处理必须清理状态，同时保留原始异常。</summary>
    private static void Exit(Pawn pawn, object state, bool failed)
    {
        Exception error = failed ? new InvalidOperationException("测试异常") : null;
        MethodInfo finalizer = patch.GetMethod("Finalizer");
        Check(finalizer != null, "需要异常退出清理入口");
        object result = finalizer.Invoke(null, new object[] { pawn.health, state, error });
        Check(ReferenceEquals(result, error), "最终处理不能吞掉原始异常");
    }

    /// <summary>失败时输出具体行为边界。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
