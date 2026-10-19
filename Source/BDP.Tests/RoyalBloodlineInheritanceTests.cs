using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using RimWorld;
using Verse;

/// <summary>在真实原版程序集上验证皇室基因的遗传候选与概率赋值，不复写遗传算法。</summary>
internal static class RoyalBloodlineInheritanceTests
{
    /// <summary>游戏的真实程序集目录。</summary>
    private static string managedPath;

    /// <summary>补丁框架程序集目录。</summary>
    private static string harmonyPath;

    /// <summary>待测模组程序集目录。</summary>
    private static string modPath;

    /// <summary>设置真实依赖位置，再执行测试，避免提前加载原版类型。</summary>
    private static int Main(string[] args)
    {
        managedPath = args[0];
        harmonyPath = args[1];
        modPath = args[2];
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>加载实际游戏和模组依赖，不用隔离替身替代遗传方法。</summary>
    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        string file = new AssemblyName(args.Name).Name + ".dll";
        foreach (string directory in new[] { managedPath, harmonyPath, modPath })
        {
            string path = Path.Combine(directory, file);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>测试前后核对真实指令，并覆盖父方、母方、外源与非皇室基因。</summary>
    private static void Run()
    {
        // 游戏外没有定义绑定阶段；仅关闭原版未初始化定义警告，避免调用 Unity 原生日志。
        typeof(DefOfHelper).GetField("bindingNow", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
        // 原版遗传工具的静态关系表需要非空定义；这里只补数据，不替代任何方法。
        foreach (FieldInfo field in typeof(PawnRelationDefOf).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.FieldType == typeof(PawnRelationDef))
                field.SetValue(null, new PawnRelationDef { defName = field.Name });
        MethodInfo inherited = typeof(PregnancyUtility).GetMethod("GetInheritedGenes",
            new[] { typeof(Pawn), typeof(Pawn), typeof(bool).MakeByRefType() });
        FieldInfo arc = typeof(GeneDef).GetField("biostatArc");
        List<CodeInstruction> before = PatchProcessor.GetOriginalInstructions(inherited).ToList();
        Require(before.Count(code => code.LoadsField(arc)) == 2, "真实原版应包含父母两处超凡检查。");

        Assembly content = Assembly.LoadFrom(Path.Combine(modPath, "BDP.Content.dll"));
        Type patch = content.GetType("BDP.Content.Trion.Royalty.Patch_PregnancyUtility_RoyalBloodlineInheritance");
        if (patch != null) new Harmony("bdp.tests.royal.inheritance").CreateClassProcessor(patch).Patch();

        GeneDef royal = new GeneDef { defName = "BDP_Gene_RoyalBloodline", biostatArc = 1 };
        GeneDef ordinary = new GeneDef { defName = "TestOrdinary", biostatArc = 0 };
        GeneDef otherArchite = new GeneDef { defName = "TestOtherArchite", biostatArc = 1 };
        if (patch != null)
        {
            List<CodeInstruction> after = PatchProcessor.GetCurrentInstructions(inherited).ToList();
            Require(after.Count(code => code.LoadsField(arc)) == 0, "父母两处候选判断必须都已接入。");
            Require(after.Count(code => code.opcode == OpCodes.Call && code.operand is MethodInfo
                && ((MethodInfo)code.operand).DeclaringType == patch) == 2, "仅应接入两处皇室判断。");
            Require(before.Count == after.Count, "补丁不得增加或删除其他原版遗传指令。");
            for (int index = 0; index < before.Count; index++)
            {
                if (before[index].LoadsField(arc)) continue;
                Require(before[index].opcode == after[index].opcode, "补丁不得改变其他原版指令类型。");
            }
        }

        // 测试专用：让真实方法在父母候选收集结束时返回，避开需要 Unity 宿主的后半段。
        new Harmony("bdp.tests.royal.candidates").Patch(inherited,
            transpiler: new HarmonyMethod(typeof(RoyalBloodlineInheritanceTests).GetMethod(
                "ReturnCandidates", BindingFlags.Static | BindingFlags.NonPublic)));
        Require(Candidates(MakePawn(royal, ordinary, otherArchite), null).Contains(royal),
            "父方皇室超凡基因应进入原版遗传候选。");
        Require(Chance(royal) == 0.5f, "父方单独携带应保留原版 50% 候选概率。");
        Require(Candidates(null, MakePawn(royal, ordinary, otherArchite)).Contains(royal),
            "母方皇室超凡基因应进入原版遗传候选。");
        Require(Chance(royal) == 0.5f, "母方单独携带应保留原版 50% 候选概率。");
        Require(Candidates(MakePawn(royal), MakePawn(royal)).Contains(royal), "双方携带应仍是一个候选。");
        Require(Chance(royal) == 1f, "双方携带应保留原版 100% 候选概率。");
        Require(!Candidates(MakePawn(otherArchite), null).Contains(otherArchite), "其他超凡基因仍应排除。");
        Require(Candidates(MakePawn(ordinary), null).Contains(ordinary), "普通内源基因仍应进入候选。");
        Pawn xenogeneParent = MakePawn();
        xenogeneParent.genes.Xenogenes.Add(new Gene { def = royal, pawn = xenogeneParent });
        Require(!Candidates(xenogeneParent, null).Contains(royal), "外源皇室基因不应被扩大为可自然遗传。");
        Require(royal.biostatArc == 1 && otherArchite.biostatArc == 1, "补丁不得改写共享定义的超凡属性。");

        Console.WriteLine("通过：真实原版父母候选、单方 50% 与双方 100% 候选概率；普通、其他超凡、外源及共享属性边界正确。实际生育仍需游戏内验证。");
    }

    /// <summary>建立只含基因列表的原版人物，不执行人物生成与地图初始化。</summary>
    private static Pawn MakePawn(params GeneDef[] genes)
    {
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        pawn.genes = new Pawn_GeneTracker(pawn);
        foreach (GeneDef definition in genes)
            pawn.genes.Endogenes.Add(new Gene { def = definition, pawn = pawn });
        return pawn;
    }

    /// <summary>读取真实方法收集的候选，不在测试中重写候选算法。</summary>
    private static List<GeneDef> Candidates(Pawn father, Pawn mother)
    {
        ((Dictionary<GeneDef, float>)typeof(PregnancyUtility).GetField("tmpGeneChances",
            BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).Clear();
        bool success;
        return PregnancyUtility.GetInheritedGenes(father, mother, out success).ToList();
    }

    /// <summary>读取原版已经计算的候选概率，不模拟概率计算。</summary>
    private static float Chance(GeneDef gene)
    {
        return ((Dictionary<GeneDef, float>)typeof(PregnancyUtility).GetField("tmpGeneChances",
            BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))[gene];
    }

    /// <summary>仅在测试进程中于组合抽样开始前返回真实候选列表。</summary>
    private static IEnumerable<CodeInstruction> ReturnCandidates(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        FieldInfo genes = typeof(PregnancyUtility).GetField("tmpGenes", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo candidates = typeof(PregnancyUtility).GetField("tmpGenesShuffled", BindingFlags.Static | BindingFlags.NonPublic);
        int boundary = codes.FindIndex(code => code.LoadsField(genes));
        boundary = codes.FindIndex(boundary + 1, code => code.LoadsField(genes)) - 2;
        Require(boundary > 0 && codes[boundary].opcode == OpCodes.Ldc_I4_0
            && codes[boundary + 1].opcode == OpCodes.Stloc_0, "测试未找到真实候选结束边界。");
        List<CodeInstruction> result = codes.Take(boundary).ToList();
        CodeInstruction entry = new CodeInstruction(OpCodes.Ldarg_2);
        entry.MoveLabelsFrom(codes[boundary]);
        result.Add(entry);
        result.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
        result.Add(new CodeInstruction(OpCodes.Stind_I1));
        result.Add(new CodeInstruction(OpCodes.Ldsfld, candidates));
        result.Add(new CodeInstruction(OpCodes.Ret));
        return result;
    }

    /// <summary>明确报告玩家行为或原版指令边界失败。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
