# 在真实 Harmony（补丁框架）上执行最小引用参数样本，不启动游戏或调用战斗业务。
$ErrorActionPreference = 'Stop'
$harmonyPath = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\1.6\Assemblies\0Harmony.dll'
[void][Reflection.Assembly]::LoadFrom($harmonyPath)
$sample = @'
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

/// <summary>复现被诊断方法的引用参数与保留名，不依赖任何游戏状态。</summary>
public static class CombatAiReferenceSample
{
    /// <summary>选择清空、替换或保持引用，覆盖 BDP 的三种出口。</summary>
    public static int Mode;
    /// <summary>原候选、新候选与观察到的最终候选，只在本进程样本中使用。</summary>
    public static readonly object Original = new object();
    /// <summary>原方法写入的新候选。</summary>
    public static readonly object Replacement = new object();
    /// <summary>后置观察器看到的值。</summary>
    public static object Observed;

    /// <summary>保留 ref __result 参数名及 void 返回，模拟“对 BDP 后置补丁再观察”。</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Apply(ref object __result, object __instance, object target, bool allowManualCastWeapons)
    {
        if (Mode == 0) __result = null;
        if (Mode == 1) __result = Replacement;
    }

    /// <summary>验证真实补丁生成器的数组回写及按位置读取行为。</summary>
    public static string Run()
    {
        MethodInfo target = typeof(CombatAiReferenceSample).GetMethod("Apply");
        var harmony = new Harmony("bdp.tests.combat_ai_reference_sample");
        try
        {
            // 记录当前安装框架的旧数组行为；框架更新后不强求历史缺陷继续存在。
            harmony.Patch(target, new HarmonyMethod(typeof(ArrayObserver), "Before"), new HarmonyMethod(typeof(ArrayObserver), "After"));
            Mode = 0;
            object[] args = { Original, null, null, false };
            target.Invoke(null, args);
            bool oldArrayRestores = ReferenceEquals(args[0], Original) && ReferenceEquals(Observed, Original);
            harmony.Unpatch(target, HarmonyPatchType.All, harmony.Id);

            // 修正方式只按位置传入值，不请求可回写的参数数组或引用。
            harmony.Patch(target, new HarmonyMethod(typeof(PositionalObserver), "Before"), new HarmonyMethod(typeof(PositionalObserver), "After"));
            for (Mode = 0; Mode < 3; Mode++)
            {
                args = new object[] { Original, null, null, false };
                object expected = Mode == 0 ? null : Mode == 1 ? Replacement : Original;
                target.Invoke(null, args);
                if (!ReferenceEquals(args[0], expected) || !ReferenceEquals(Observed, expected))
                    throw new Exception("按位置观察改变引用结果或读取了旧值，mode=" + Mode);
            }
            return "PASS: Harmony " + typeof(Harmony).Assembly.GetName().Version
                + "，旧数组恢复引用=" + oldArrayRestores + "；按位置观察在清空、替换、保持三种出口均不改变结果。";
        }
        finally { harmony.Unpatch(target, HarmonyPatchType.All, harmony.Id); }
    }
}

/// <summary>保留造成问题的最小旧补丁，仅作为反例，不进入开发程序集。</summary>
public static class ArrayObserver
{
    /// <summary>读取旧值而不主动修改数组，仍会触发框架隐式回写。</summary>
    public static void Before(object[] __args, out object __state) { __state = __args[0]; }
    /// <summary>观察到的也是入口旧值。</summary>
    public static void After(object[] __args, object __state) { CombatAiReferenceSample.Observed = __args[0]; }
}

/// <summary>与修正后的诊断采用同一参数位置和状态传递方式。</summary>
public static class PositionalObserver
{
    /// <summary>仅接收引用指向的值，不接收引用本身。</summary>
    public static void Before(object __0, out object __state) { __state = __0; }
    /// <summary>按位置读取真实修改后的值，避开 __result 与 __instance 的保留名冲突。</summary>
    public static void After(object __0, object __1, object __2, object __state) { CombatAiReferenceSample.Observed = __0; }
}
'@
Add-Type -TypeDefinition $sample -ReferencedAssemblies @($harmonyPath, 'System.Core.dll')
[CombatAiReferenceSample]::Run()
