using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Collections.Generic;
using RimWorld;
using Verse;

/// <summary>在真实 Core 程序集上验证资源行为；替换规则提供器，不复写资源算法。</summary>
internal static class HostileTrionResourceRulesTests
{
    /// <summary>真实依赖查找目录。</summary>
    private static string[] dependencyPaths;
    /// <summary>真实核心程序集。</summary>
    private static Assembly core;
    /// <summary>资源组件类型。</summary>
    private static Type compType;
    /// <summary>测试规则提供器；允许在同一个宿主上切换普通与免耗规则。</summary>
    private static object provider;
    /// <summary>提供器普通规则开关。</summary>
    private static FieldInfo enabledField;
    /// <summary>包括内部资源成员的反射范围。</summary>
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>先建立程序集解析，再进入实际游戏类型调用。</summary>
    private static int Main(string[] args)
    {
        dependencyPaths = args;
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>从真实游戏、补丁与模组目录读取依赖。</summary>
    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name + ".dll";
        foreach (string directory in dependencyPaths)
        {
            string path = Path.Combine(directory, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>执行普通成本对照，再检验敌方免耗；旧版本必须在敌方资源门槛处失败。</summary>
    private static void Run()
    {
        core = Assembly.LoadFrom(Path.Combine(dependencyPaths[2], "BDP.Core.dll"));
        compType = core.GetType("BDP.Core.Trion.CompTrion", true);
        object normal = CreateComp(10f);
        Check(!(bool)Call(normal, "CanAfford", 20f), "普通宿主应拒绝超出余量的费用");
        Check((bool)Call(normal, "TryConsume", 3f), "普通宿主应支付费用");
        Equal(Read(normal, "Cur"), 7f, "普通宿主实际扣费");
        InstallProvider();
        SetEnabled(false);
        object hostile = CreateComp(1f);
        Check((bool)Call(hostile, "CanAfford", 20f), "敌方不应因功能费用高于余量被阻止");
        Check((bool)Call(hostile, "TryConsume", 20f), "敌方功能应支付成功");
        Equal(Read(hostile, "Cur"), 1f, "敌方功能不能扣除生命资源");
        Call(hostile, "ConsumeUntilDepleted", 30f);
        Equal(Read(hostile, "Cur"), 1f, "尽力功能消耗也应免耗");
        Call(hostile, "SetReserved", 50f);
        Equal(Read(hostile, "Reserved"), 0f, "敌方预占用应为零");
        TestDrain();
        TestAllocationRules();
        TestStartupAndVisibility();
        Type registry = core.GetType("BDP.Core.Trion.TrionResourceRuleRegistry", true);
        registry.GetMethod("Clear").Invoke(null, new object[] { provider });
        object restored = CreateComp(10f);
        Call(restored, "TryConsume", 3f);
        Equal(Read(restored, "Cur"), 7f, "移除提供器应恢复普通规则");
        Console.WriteLine("通过：普通扣费、敌方免耗、明确伤口损耗、占用归零、规则切换及事件。");
    }

    /// <summary>用正式接口构造最小规则提供器；旧程序集无接口时保留原行为供失败对照。</summary>
    private static void InstallProvider()
    {
        Type contract = core.GetType("BDP.Core.Trion.ITrionResourceRuleProvider");
        if (contract == null) return;
        MethodInfo resolve = contract.GetMethod("GetRules");
        Type rules = resolve.ReturnType;
        AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("TrionRuleFixture"), AssemblyBuilderAccess.Run);
        TypeBuilder builder = assembly.DefineDynamicModule("Fixture").DefineType("FixtureRuleProvider", TypeAttributes.Public);
        builder.AddInterfaceImplementation(contract);
        FieldBuilder field = builder.DefineField("Enabled", typeof(bool), FieldAttributes.Public);
        builder.DefineDefaultConstructor(MethodAttributes.Public);
        MethodBuilder method = builder.DefineMethod("GetRules", MethodAttributes.Public | MethodAttributes.Virtual, rules, new Type[] { typeof(Thing) });
        ILGenerator il = method.GetILGenerator();
        for (int i = 0; i < 3; i++) { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field); }
        il.Emit(OpCodes.Newobj, rules.GetConstructor(new Type[] { typeof(bool), typeof(bool), typeof(bool) }));
        il.Emit(OpCodes.Ret);
        builder.DefineMethodOverride(method, resolve);
        Type generated = builder.CreateType();
        provider = Activator.CreateInstance(generated);
        enabledField = generated.GetField("Enabled");
        core.GetType("BDP.Core.Trion.TrionResourceRuleRegistry", true).GetMethod("Register").Invoke(null, new object[] { provider });
    }

    /// <summary>直接切换提供器规则，不替换待测资源宿主。</summary>
    private static void SetEnabled(bool enabled)
    {
        if (enabledField != null) enabledField.SetValue(provider, enabled);
    }

    /// <summary>创建拥有真实命令与事件的资源组件，只初始化必要标量。</summary>
    private static object CreateComp(float current)
    {
        object comp = Activator.CreateInstance(compType);
        ((ThingComp)comp).parent = new ThingWithComps();
        ((ThingComp)comp).props = (CompProperties)Activator.CreateInstance(core.GetType("BDP.Core.Trion.CompProperties_Trion", true));
        compType.GetField("max", Members).SetValue(comp, 100f);
        compType.GetField("cur", Members).SetValue(comp, current);
        return comp;
    }

    /// <summary>混合账本仅伤口结算；声明快照保留原速率，见底事件只发一次。</summary>
    private static void TestDrain()
    {
        object comp = CreateComp(10f);
        Type keyType = core.GetType("BDP.Core.Trion.TrionDrainKey", true);
        Type kindType = core.GetType("BDP.Core.Trion.TrionDrainKind", true);
        object functionKey = Activator.CreateInstance(keyType, new object[] { "Test", "Function", 0, "" });
        object woundKey = Activator.CreateInstance(keyType, new object[] { "Test", "Wound", 0, "" });
        object functionKind = Enum.Parse(kindType, "Function");
        object woundKind = Enum.Parse(kindType, "WoundLeak");
        Call(comp, "RegisterDrain", functionKey, 50f, functionKind);
        Call(comp, "RegisterDrain", woundKey, 2f, woundKind);
        Equal(Read(comp, "TotalDrainPerSecond"), 2f, "敌方混合账本仅伤口有效");
        object snapshot = Call(comp, "GetDrainSnapshot");
        Equal((float)snapshot.GetType().GetProperty("Item").GetValue(snapshot, new object[] { functionKey }), 50f, "声明速率不因免耗变零");
        Call(comp, "SettleDrain");
        Equal(Read(comp, "Cur"), 8f, "伤口按真实账本扣除");
        SetEnabled(true);
        Equal(Read(comp, "TotalDrainPerSecond"), 52f, "相同账本切回普通规则仍保留功能费用");
        object normal = CreateComp(60f);
        Call(normal, "RegisterDrain", functionKey, 50f, functionKind);
        Call(normal, "RegisterDrain", woundKey, 2f, woundKind);
        Call(normal, "SettleDrain");
        Equal(Read(normal, "Cur"), 8f, "普通宿主结算功能与伤口全部损耗");
        SetEnabled(false);
        Call(comp, "UnregisterDrain", woundKey);
        Equal(Read(comp, "TotalDrainPerSecond"), 0f, "伤口注销后敌方不再持续扣费");
        Call(comp, "RegisterDrain", woundKey, 20f, woundKind);
        int depleted = 0;
        Action callback = delegate { depleted++; };
        compType.GetEvent("AvailableDepleted", Members).GetAddMethod(true).Invoke(comp, new object[] { callback });
        Call(comp, "SettleDrain");
        Call(comp, "SettleDrain");
        Equal(Read(comp, "Cur"), 0f, "伤口可以耗尽敌方资源");
        Check(depleted == 1, "真实见底事件应只发一次");
    }

    /// <summary>确认规则归一化真实占用金额，切换不改变当前生命资源。</summary>
    private static void TestAllocationRules()
    {
        Type registry = core.GetType("BDP.Core.Trion.TrionResourceRuleRegistry", true);
        MethodInfo resolve = registry.GetMethod("ResolveCapacityCost");
        object comp = CreateComp(80f);
        SetEnabled(true);
        float normalAmount = (float)resolve.Invoke(null, new object[] { ((ThingComp)comp).parent, 30f });
        Equal(normalAmount, 30f, "普通有效占用金额");
        Check((bool)Call(comp, "Allocate", normalAmount), "普通占用成功");
        Equal(Read(comp, "Available"), 50f, "普通占用减少可用量");
        Call(comp, "Release", normalAmount);
        SetEnabled(false);
        Equal((float)resolve.Invoke(null, new object[] { ((ThingComp)comp).parent, 30f }), 0f, "敌方有效占用金额");
        Check((bool)Call(comp, "Allocate", 30f), "公共占用请求在免占用规则下成功但不建立锁定");
        Equal(Read(comp, "Allocated"), 0f, "免占用不能遗留实际锁定");
        Equal(Read(comp, "Cur"), 80f, "规则切换不补满生命资源");
    }

    /// <summary>真实启动策略拒绝空血，并确认隐藏显示不污染资源读取。</summary>
    private static void TestStartupAndVisibility()
    {
        object comp = CreateComp(0f);
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        ((ThingComp)comp).parent = pawn;
        typeof(ThingWithComps).GetField("comps", Members).SetValue(pawn, new List<ThingComp> { (ThingComp)comp });
        Type policyType = core.GetType("BDP.Core.CombatBodySession.CombatBodySessionPolicy", true);
        object policy = Activator.CreateInstance(policyType);
        MethodInfo gate = policyType.GetMethod("HasOperationalTrion");
        Check(!(bool)gate.Invoke(policy, new object[] { pawn }), "免耗宿主空血不可启动");
        compType.GetField("cur", Members).SetValue(comp, 1f);
        Check((bool)gate.Invoke(policy, new object[] { pawn }), "免耗宿主有真实资源可通过存活门槛");
        Equal(Read(comp, "Max"), 100f, "隐藏时内部容量读取仍是真值");
        Type workerType = core.GetType("BDP.Core.Genes.StatWorker_TrionCapacity", true);
        StatWorker worker = (StatWorker)Activator.CreateInstance(workerType);
        Check(!worker.ShouldShowFor(default(StatRequest)), "隐藏规则应在原版属性显示入口生效");
        SetEnabled(true);
        compType.GetField("cur", Members).SetValue(comp, 0f);
        Check((bool)gate.Invoke(policy, new object[] { pawn }), "普通规则保持原启动资源边界");
        SetEnabled(false);
    }

    /// <summary>调用真实内部成员。</summary>
    private static object Call(object owner, string name, params object[] args)
    {
        return compType.GetMethod(name, Members).Invoke(owner, args);
    }

    /// <summary>读取真实资源标量。</summary>
    private static float Read(object owner, string name)
    {
        return (float)compType.GetProperty(name, Members).GetValue(owner, null);
    }

    /// <summary>断言实际金额。</summary>
    private static void Equal(float actual, float expected, string message)
    {
        Check(Math.Abs(actual - expected) < 0.001f, message + "；实际=" + actual + "，预期=" + expected);
    }

    /// <summary>失败时给出可核实的行为差异。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
