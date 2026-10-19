using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using RimWorld;
using Verse;
using Verse.AI;

/// <summary>验证真实内容规则与自动近战接管元数据，不复写生产算法或启动游戏地图。</summary>
internal static class NonPlayerRulesAndAutoMeleeTests
{
    /// <summary>真实程序集依赖目录。</summary>
    private static string[] dependencyPaths;

    /// <summary>注册依赖解析后才调用实际游戏类型。</summary>
    private static int Main(string[] args)
    {
        dependencyPaths = args;
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        try { Run(args.Length > 3 && args[3] == "auto"); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>只从当前游戏和模组目录读取依赖。</summary>
    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        string file = new AssemblyName(args.Name).Name + ".dll";
        for (int index = 0; index < 3; index++)
        {
            string path = Path.Combine(dependencyPaths[index], file);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>旧版本分别在非玩家规则和自动接管边界处失败。</summary>
    private static void Run(bool automaticOnly)
    {
        Assembly core = Assembly.LoadFrom(Path.Combine(dependencyPaths[2], "BDP.Core.dll"));
        if (!automaticOnly) TestFactionRules();
        TestAutomaticTiming(core);
        Console.WriteLine("通过：真实非玩家派系规则、无派系、派系变化、自动任务剩余到期时间与非强制语义。");
    }

    /// <summary>直接调用实际内容提供器；玩家状态不需要地图或当前可控资格。</summary>
    private static void TestFactionRules()
    {
        Assembly content = Assembly.LoadFrom(Path.Combine(dependencyPaths[2], "BDP.Content.dll"));
        object resource = Activator.CreateInstance(content.GetType("BDP.Content.Trion.Resource.NonPlayerTrionResourceRuleProvider", true));
        object delay = Activator.CreateInstance(content.GetType("BDP.Content.Trigger.NonPlayer.NonPlayerTriggerSwitchDelayPolicy", true));
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        FieldInfo factionField = typeof(Thing).GetField("factionInt", BindingFlags.Instance | BindingFlags.NonPublic);
        Faction player = new Faction { def = new FactionDef { isPlayer = true } };
        Faction outsider = new Faction { def = new FactionDef { isPlayer = false } };
        AssertRules(resource, delay, pawn, false, "无派系人物");
        factionField.SetValue(pawn, outsider);
        AssertRules(resource, delay, pawn, false, "非玩家派系，不查询外交和控制状态");
        factionField.SetValue(pawn, player);
        AssertRules(resource, delay, pawn, true, "转为玩家派系，尚未落地图也保留正常规则");
        factionField.SetValue(pawn, null);
        AssertRules(resource, delay, pawn, false, "同一人物转回无派系");
        object rules = resource.GetType().GetMethod("GetRules").Invoke(resource, new object[] { null });
        Check((bool)rules.GetType().GetProperty("FunctionCostsEnabled").GetValue(rules, null), "空宿主沿用默认成本");
        Check(!(bool)delay.GetType().GetMethod("BypassDelays").Invoke(delay, new object[] { null }), "空宿主不能免延迟");
    }

    /// <summary>同时核对三项资源规则及启停规则，防止提供器范围分裂。</summary>
    private static void AssertRules(object resource, object delay, Pawn pawn, bool normal, string note)
    {
        object rules = resource.GetType().GetMethod("GetRules").Invoke(resource, new object[] { pawn });
        foreach (string property in new[] { "FunctionCostsEnabled", "CapacityCostsEnabled", "CapacityInfoVisible" })
            Check((bool)rules.GetType().GetProperty(property).GetValue(rules, null) == normal, note + "：" + property);
        Check((bool)delay.GetType().GetMethod("BypassDelays").Invoke(delay, new object[] { pawn }) == !normal, note + "：启停规则");
    }

    /// <summary>接管不重置原判断截止刻，不制造玩家强制订单。</summary>
    private static void TestAutomaticTiming(Assembly core)
    {
        Type executor = core.GetType("BDP.Core.AttackExecution.MeleeAttackExecutor", true);
        MethodInfo apply = executor.GetMethod("ApplyAutomaticJobSettings", BindingFlags.Static | BindingFlags.NonPublic);
        Check(apply != null, "自动近战尚未接入原版任务判断设置");
        Job source = new Job { startTick = 100, expiryInterval = 300, checkOverrideOnExpire = true,
            expireRequiresEnemiesNearby = true, expireOnEnemiesNearby = true, killIncappedTarget = true };
        Job replacement = new Job { playerForced = true };
        apply.Invoke(null, new object[] { replacement, source, 170 });
        Check(replacement.expiryInterval == 230, "接管后判断截止刻仍为400，不能再延后300刻");
        Check(replacement.checkOverrideOnExpire && replacement.expireRequiresEnemiesNearby && replacement.expireOnEnemiesNearby,
            "原版重新判断条件不能丢失");
        Check(!replacement.playerForced && replacement.killIncappedTarget, "自动任务非强制且保留倒地目标条件");
        apply.Invoke(null, new object[] { replacement, source, 450 });
        Check(replacement.expiryInterval == 1, "已到期任务接管后应尽快重新判断");
        source.expiryInterval = -1;
        apply.Invoke(null, new object[] { replacement, source, 450 });
        Check(replacement.expiryInterval == -1, "原任务没有到期时间时不添加自造周期");
    }

    /// <summary>失败时输出具体边界。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
