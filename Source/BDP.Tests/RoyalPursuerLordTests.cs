using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using RimWorld;
using Verse;
using Verse.AI.Group;

/// <summary>验证实际追兵队伍的移除边界；完整受伤和地图队伍结束留给游戏实测。</summary>
internal static class RoyalPursuerLordTests
{
    /// <summary>当前游戏与模组依赖目录。</summary>
    private static string[] paths;
    /// <summary>包含私有字段的实例查询范围。</summary>
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    /// <summary>真实核心程序集，用于初始化战斗体宿主。</summary>
    private static Assembly core;

    /// <summary>注册依赖解析后运行真实业务行为。</summary>
    private static int Main(string[] args)
    {
        paths = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>仅加载本次实际安装目录的依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in paths)
        {
            string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>暂时战斗体失能不能等同于原身击败，也不能干扰其他队伍。</summary>
    private static void Run()
    {
        Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        core = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Core.dll"));
        Type type = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Content.dll")).GetType(
            "BDP.Content.Royal.Quest.LordJob_RoyalPursuers", true);
        LordJob job = (LordJob)Activator.CreateInstance(type, new object[] { null });
        foreach (string flag in new[] { "canKidnap", "canTimeoutOrFlee", "canSteal" })
            Check(!(bool)typeof(LordJob_AssaultColony).GetField(flag, Fields).GetValue(job), "追兵必须关闭：" + flag);
        foreach (string phase in new[] { null, "Inactive", "Cooldown" })
            Check(job.ShouldRemovePawn(MakePawn(phase), PawnLostCondition.Incapped), "普通人物／原身失能必须正常移除");
        foreach (string phase in new[] { "Active", "Collapsing" })
        {
            Pawn pawn = MakePawn(phase);
            Check(!job.ShouldRemovePawn(pawn, PawnLostCondition.Incapped), phase + "倒地不能提前结束追兵队伍");
            Check(!job.ShouldRemovePawn(pawn, PawnLostCondition.Incapped), "重复倒地仍须暂缓移除");
            Check(Pending(type, job).Count == 1, "重复倒地不能积累等待记录");
            Check(new LordJob_AssaultColony().ShouldRemovePawn(pawn, PawnLostCondition.Incapped), "普通队伍行为不能被改变");
            Check(job.ShouldRemovePawn(pawn, PawnLostCondition.Killed), "真正死亡不能暂缓");
            Check(job.ShouldRemovePawn(pawn, PawnLostCondition.MadePrisoner), "被俘不能暂缓");
            job.Notify_PawnLost(pawn, PawnLostCondition.MadePrisoner);
            Check(Pending(type, job).Count == 0, "真正离队必须清除等待记录");
        }
        Pawn restored = MakePawn("Collapsing");
        Lord lord = (Lord)FormatterServices.GetUninitializedObject(typeof(Lord));
        lord.ownedPawns = new List<Pawn> { restored };
        job.lord = lord;
        job.ShouldRemovePawn(restored, PawnLostCondition.Incapped);
        job.LordJobTick();
        Check(Pending(type, job).Count == 1, "崩解尚未结束必须继续等待");
        SetPhase(restored, "Inactive");
        job.LordJobTick();
        Check(Pending(type, job).Count == 0 && lord.ownedPawns.Contains(restored), "恢复行动后必须保持队伍身份并结束等待");
        Console.WriteLine("通过：真实追兵队伍的倒地暂缓、死亡／被俘边界、恢复身份、重复记录、普通队伍隔离及离场开关。");
    }

    /// <summary>只读真实队伍等待记录，确认重复通知和正常收尾。</summary>
    private static List<Pawn> Pending(Type type, LordJob job)
    {
        return (List<Pawn>)type.GetField("pendingRecovery", Fields).GetValue(job);
    }

    /// <summary>真实宿主与健康状态夹具，不构造地图或 Unity 图形对象。</summary>
    private static Pawn MakePawn(string phase)
    {
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "TestPawn";
        pawn.health = (Pawn_HealthTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_HealthTracker));
        typeof(Pawn_HealthTracker).GetField("pawn", Fields).SetValue(pawn.health, pawn);
        SetHealth(pawn, PawnHealthState.Mobile);
        if (phase == null) return pawn;
        Type compType = core.GetType("BDP.Core.CombatBody.CompCombatBodyHost", true);
        ThingComp comp = (ThingComp)Activator.CreateInstance(compType);
        comp.parent = pawn;
        comp.props = (CompProperties)Activator.CreateInstance(core.GetType("BDP.Core.CombatBody.CompProperties_CombatBodyHost", true));
        typeof(ThingWithComps).GetField("comps", Fields).SetValue(pawn, new List<ThingComp> { comp });
        core.GetType("BDP.Core.CombatBody.CombatBodySurfaceAccess", true).GetMethod("ResolveReader").Invoke(null, new object[] { pawn });
        SetPhase(pawn, phase);
        return pawn;
    }

    /// <summary>明确人物健康状态；未初始化对象的枚举默认值是死亡。</summary>
    private static void SetHealth(Pawn pawn, PawnHealthState state)
    {
        typeof(Pawn_HealthTracker).GetField("healthState", Fields).SetValue(pawn.health, state);
    }

    /// <summary>在真实状态对象上切换相位，绕过地图和激活事务依赖。</summary>
    private static void SetPhase(Pawn pawn, string phase)
    {
        ThingComp comp = ((List<ThingComp>)typeof(ThingWithComps).GetField("comps", Fields).GetValue(pawn))[0];
        object state = comp.GetType().GetField("state", Fields).GetValue(comp);
        FieldInfo phaseField = state.GetType().GetField("phase", Fields);
        phaseField.SetValue(state, Enum.Parse(phaseField.FieldType, phase));
        if (phase == "Cooldown") state.GetType().GetField("cooldownEndTick", Fields).SetValue(state, int.MaxValue);
    }

    /// <summary>验证失败时报告对应行为边界。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
