using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using BDP.Core.AttackExecution;
using HarmonyLib;
using Verse;

/// <summary>真实目标输入帧边界：确认与只读探测隔离，分发不会修改冻结事实。</summary>
internal static class TargetingInputBoundaryTests
{
    /// <summary>运行时依赖目录。</summary>
    private static string[] directories;
    /// <summary>分组实际送达的输入事实。</summary>
    private static TargetingInputRuntimeFacts expectedFacts;
    /// <summary>实际探测的成员数。</summary>
    private static int memberCount;
    /// <summary>先注册依赖解析，再进入游戏类型。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        return Run();
    }
    /// <summary>验证实际工厂方法，不构造原生引擎事件。</summary>
    private static int Run()
    {
        try
        {
            Type factsType = typeof(TargetingInputRuntimeFacts);
            Check(!factsType.GetProperty("Modifiers").CanWrite && !factsType.GetProperty("PressedButton").CanWrite,
                "冻结事实仍可被分组成员修改");
            object facts = Activator.CreateInstance(factsType, new object[] { TargetingInputButton.Left, TargetingInputModifiers.Shift | TargetingInputModifiers.Alt });
            LocalTargetInfo target = new LocalTargetInfo(new IntVec3(5, 0, 8));
            TargetingInputFrame probe = Frame("CreateProbeInputFrame", target, facts);
            Check(probe.PressedButton == TargetingInputButton.None && !probe.ConfirmRequested && !probe.CancelRequested,
                "合法性探测不得携带确认或取消动作");
            Check(probe.Modifiers == (TargetingInputModifiers.Shift | TargetingInputModifiers.Alt), "探测丢失本轮修饰键");
            TargetingInputFrame first = Frame("CreateSelectionInputFrame", target, facts);
            Check(first.PressedButton == TargetingInputButton.Left && first.ConfirmRequested, "明确确认入口未保留按钮和目标");
            first.Modifiers = TargetingInputModifiers.None;
            first.PressedButton = TargetingInputButton.Right;
            TargetingInputFrame second = Frame("CreateSelectionInputFrame", target, facts);
            Check(second.PressedButton == TargetingInputButton.Left && second.Modifiers == probe.Modifiers,
                "第一成员改变自身输入帧污染了后续成员的快照");
            TargetingInputFrame empty = Frame("CreateProbeInputFrame", target, null);
            Check(empty.Modifiers == TargetingInputModifiers.None && !empty.ConfirmRequested, "空程序输入继承了上一轮修饰键或确认");
            TestGroupedProbe((TargetingInputRuntimeFacts)facts, target);
            Console.WriteLine("TargetingInputBoundaryTests PASS：冻结事实、只读探测、独立输入帧、空输入不残留。");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    /// <summary>真实分组依次探测成员；仅替代需要地图的成员判定，保留分组调用链。</summary>
    private static void TestGroupedProbe(TargetingInputRuntimeFacts facts, LocalTargetInfo target)
    {
        Assembly core = typeof(TargetingInputFrame).Assembly;
        Type source = core.GetType("BDP.Core.AttackExecution.AttackExecutionTargetingSource");
        Type group = core.GetType("BDP.Core.AttackExecution.GroupedAttackExecutionTargetingSource");
        Array members = Array.CreateInstance(source, 2);
        members.SetValue(FormatterServices.GetUninitializedObject(source), 0);
        members.SetValue(FormatterServices.GetUninitializedObject(source), 1);
        object instance = Activator.CreateInstance(group, new object[] { members });
        MethodInfo memberProbe = source.GetMethod("CanHitTarget", BindingFlags.NonPublic | BindingFlags.Instance,
            null, new[] { typeof(LocalTargetInfo), typeof(TargetingInputRuntimeFacts) }, null);
        MethodInfo groupProbe = group.GetMethod("CanHitTarget", BindingFlags.NonPublic | BindingFlags.Instance);
        Harmony harmony = new Harmony("bdp.tests.input.group");
        expectedFacts = facts;
        memberCount = 0;
        harmony.Patch(memberProbe, prefix: new HarmonyMethod(typeof(TargetingInputBoundaryTests).GetMethod("ObserveMember")));
        try
        {
            Check((bool)groupProbe.Invoke(instance, new object[] { target, facts }), "真实分组未采纳第二成员的合法结果");
            Check(memberCount == 2, "真实分组未逐成员探测");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
    /// <summary>第一成员拒绝，第二成员允许，二者必须收到调用边界传入的同一个冻结对象。</summary>
    public static bool ObserveMember(TargetingInputRuntimeFacts inputFacts, ref bool __result)
    {
        Check(ReferenceEquals(inputFacts, expectedFacts), "分组成员重新采集了输入，而非传入同一快照");
        memberCount++;
        __result = memberCount == 2;
        return false;
    }
    /// <summary>调用真实自有输入帧工厂。</summary>
    private static TargetingInputFrame Frame(string name, LocalTargetInfo target, object facts)
    {
        Type source = typeof(TargetingInputFrame).Assembly.GetType("BDP.Core.AttackExecution.AttackExecutionTargetingSource");
        MethodInfo method = source.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        Check(method != null && method.GetParameters().Length == 2, "输入帧仍从全局作用域取值");
        return (TargetingInputFrame)method.Invoke(null, new[] { (object)target, facts });
    }
    /// <summary>验证对外行为。</summary>
    private static void Check(bool result, string message) { if (!result) throw new Exception(message); }
    /// <summary>真实依赖解析。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in directories) { string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }
}
