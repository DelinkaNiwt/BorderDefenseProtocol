using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using HarmonyLib;
using Verse;

/// <summary>直接调用正式程序集，验证浮空账本、通行锚点、原版穿盾标志和屋顶事件生命周期。</summary>
internal static class FloatingFlightShieldBoundaryBehaviorTests
{
    /// <summary>真实依赖所在目录。</summary>
    private static string[] directories;
    /// <summary>被检验的正式内容程序集。</summary>
    private static Assembly content;
    /// <summary>失败用例数量。</summary>
    private static int failures;

    /// <summary>独立运行各行为用例；原生网格代价与真实地图移动另行游戏实测。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        content = Assembly.LoadFrom(Path.Combine(args[2], "BDP.Content.dll"));
        Test("普通人物不得新建浮空账本", TestOrdinaryRecordCreation);
        Test("清除默认关闭且无浮空状态的污染记录", TestPollutedRecordCleanup);
        Test("暂时无能力仍保留用户浮空偏好", TestPreferredModeRetention);
        Test("原版忽略护盾标志优先于远程类别", () => TestAbsorption(true, true, false, false));
        Test("普通远程仍可以吸收", () => TestAbsorption(false, true, false, true));
        Test("普通爆炸仍可以吸收", () => TestAbsorption(false, false, true, true));
        Test("原版通行锚点仍可识别", TestVanillaWalkabilityAnchor);
        Test("未知同签名函数不被认作通行锚点", TestUnknownWalkabilityAnchor);
        Test("屋顶事件按地图订阅并在地图移除时退订", TestRoofEventLifetime);
        Console.WriteLine("FloatingFlightShieldBoundaryBehaviorTests: " + (failures == 0 ? "PASS" : "FAIL " + failures));
        return failures == 0 ? 0 : 1;
    }

    /// <summary>解析游戏、Harmony（方法补丁框架）及 BDP 依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in directories)
        {
            string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    /// <summary>记录实际行为失败，不让一个失败掩盖其他独立边界。</summary>
    private static void Test(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception error)
        {
            failures++;
            Exception actual = error is TargetInvocationException && error.InnerException != null ? error.InnerException : error;
            Console.Error.WriteLine("FAIL " + name + ": " + actual.Message);
        }
    }

    /// <summary>构造没有健康能力、装备和 BDP 组件的真实原版人物。</summary>
    private static Pawn OrdinaryPawn()
    {
        // 原版构造默认贴图会调用 Unity（游戏引擎）原生入口；这里只需未生成人物的真实托管字段。
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        ThingDef def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        def.defName = "BDP_BoundaryTestPawn";
        pawn.def = def;
        pawn.thingIDNumber = 1;
        FieldInfo mapState = typeof(Thing).GetField("mapIndexOrState", BindingFlags.Instance | BindingFlags.NonPublic);
        mapState.SetValue(pawn, Convert.ChangeType(-1, mapState.FieldType));
        return pawn;
    }

    /// <summary>实例化真实账本，避免启动完整游戏或替代正式规则。</summary>
    private static object Ledger()
    {
        return Activator.CreateInstance(content.GetType("BDP.Content.FloatingFlight.GameComponent_FloatingFlight", true), new object[] { null });
    }

    /// <summary>直接检验正式创建入口是否拒绝普通人物。</summary>
    private static void TestOrdinaryRecordCreation()
    {
        object ledger = Ledger();
        object record = ledger.GetType().GetMethod("GetOrCreateRecord").Invoke(ledger, new object[] { OrdinaryPawn() });
        Check(record == null, "创建入口返回了普通人物的业务记录");
        Check(RecordCount(ledger) == 0, "普通人物被写入 records");
    }

    /// <summary>注入旧版本确实可保存的污染记录，检验逐刻入口会清掉它。</summary>
    private static void TestPollutedRecordCleanup()
    {
        object ledger = Ledger();
        AddSavedRecord(ledger, OrdinaryPawn(), "Off");
        ledger.GetType().GetMethod("GameComponentTick").Invoke(ledger, null);
        Check(RecordCount(ledger) == 0, "无能力、无飞行、默认关闭记录仍被保留");
    }

    /// <summary>防止清理污染时顺带丢失玩家已经选择的模式。</summary>
    private static void TestPreferredModeRetention()
    {
        object ledger = Ledger();
        object record = AddSavedRecord(ledger, OrdinaryPawn(), "MoveOnly");
        ledger.GetType().GetMethod("GameComponentTick").Invoke(ledger, null);
        Check(RecordCount(ledger) == 1, "芯片暂时失效导致用户模式被删除");
        Check(record.GetType().GetField("PreferredMode").GetValue(record).ToString() == "MoveOnly", "保存的模式被改写");
    }

    /// <summary>以真实存档类注入既有记录，不通过待修的创建入口作弊。</summary>
    private static object AddSavedRecord(object ledger, Pawn pawn, string mode)
    {
        Type recordType = content.GetType("BDP.Content.FloatingFlight.FloatingFlightRecord", true);
        object record = Activator.CreateInstance(recordType, new object[] { pawn });
        FieldInfo preference = recordType.GetField("PreferredMode");
        preference.SetValue(record, Enum.Parse(preference.FieldType, mode));
        ((IList)ledger.GetType().GetField("records", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ledger)).Add(record);
        return record;
    }

    /// <summary>读取正式记录集合大小。</summary>
    private static int RecordCount(object ledger)
    {
        return ((IList)ledger.GetType().GetField("records", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ledger)).Count;
    }

    /// <summary>真实配置方法处理真实 DamageDef（伤害定义），不复写吸收规则。</summary>
    private static void TestAbsorption(bool ignoreShields, bool ranged, bool explosive, bool expected)
    {
        object props = Activator.CreateInstance(content.GetType("BDP.Content.Shield.HediffCompProperties_EnergyShield", true));
        DamageDef def = new DamageDef { ignoreShields = ignoreShields, isRanged = ranged, isExplosive = explosive };
        object info = Activator.CreateInstance(typeof(DamageInfo));
        typeof(DamageInfo).GetField("defInt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(info, def);
        bool actual = (bool)props.GetType().GetMethod("CanAbsorb").Invoke(props, new[] { info });
        Check(actual == expected, "CanAbsorb 返回 " + actual);
    }

    /// <summary>使用正式匹配函数检验原版精确锚点。</summary>
    private static void TestVanillaWalkabilityAnchor()
    {
        MethodInfo vanilla = AccessTools.Method(typeof(GenGrid), "WalkableBy", new[] { typeof(IntVec3), typeof(Map), typeof(Pawn) });
        Check(FindAnchor(vanilla, vanilla) == 0, "精确原版锚点被拒绝");
    }

    /// <summary>一个刻意相同签名但无关通行的函数必须安全放弃。</summary>
    private static void TestUnknownWalkabilityAnchor()
    {
        MethodInfo vanilla = AccessTools.Method(typeof(GenGrid), "WalkableBy", new[] { typeof(IntVec3), typeof(Map), typeof(Pawn) });
        MethodInfo foreign = typeof(FloatingFlightShieldBoundaryBehaviorTests).GetMethod("UnrelatedPredicate", BindingFlags.Static | BindingFlags.NonPublic);
        Check(FindAnchor(foreign, vanilla) == -1, "未知函数被选为可跳过的通行锚点");
    }

    /// <summary>提供其他模组同签名函数的真实方法身份。</summary>
    private static bool UnrelatedPredicate(IntVec3 cell, Map map, Pawn pawn) { return true; }

    /// <summary>把合成的 IL（中间语言）调用交给正式锚点匹配器。</summary>
    private static int FindAnchor(MethodInfo candidate, MethodInfo vanilla)
    {
        DynamicMethod probe = new DynamicMethod("BDP_BoundaryAnchorProbe", typeof(void), Type.EmptyTypes);
        Label label = probe.GetILGenerator().DefineLabel();
        List<CodeInstruction> codes = new List<CodeInstruction> { new CodeInstruction(OpCodes.Call, candidate), new CodeInstruction(OpCodes.Brtrue, label), new CodeInstruction(OpCodes.Ret) };
        Type patch = content.GetType("BDP.Content.FloatingFlight.Patches.Patch_PawnPathFollower_Steps_FloatingFlight", true);
        return (int)patch.GetMethod("FindWalkabilityCall", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { codes, vanilla });
    }

    /// <summary>直接检验原版地图事件上的订阅及退订，无需伪造原生路径网格。</summary>
    private static void TestRoofEventLifetime()
    {
        Type componentType = content.GetType("BDP.Content.FloatingFlight.Pathing.MapComponent_FloatingFlightPathing", false);
        Check(componentType != null, "没有原版屋顶事件地图组件");
        Map map = (Map)FormatterServices.GetUninitializedObject(typeof(Map));
        MapEvents events = new MapEvents(map);
        typeof(Map).GetField("events", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(map, events);
        object component = Activator.CreateInstance(componentType, new object[] { map });
        Check(HasRoofSubscriber(events, component), "构造后未订阅 RoofChanged");
        events.Notify_RoofChanged(IntVec3.Zero);
        componentType.GetMethod("MapRemoved").Invoke(component, null);
        Check(!HasRoofSubscriber(events, component), "地图移除后仍被事件持有");
        componentType.GetMethod("MapRemoved").Invoke(component, null);
        Check(!HasRoofSubscriber(events, component), "重复清理重新注册了事件");
    }

    /// <summary>确认真实原版事件的委托目标为当前业务地图组件。</summary>
    private static bool HasRoofSubscriber(MapEvents events, object target)
    {
        Delegate handlers = typeof(MapEvents).GetField("RoofChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(events) as Delegate;
        if (handlers == null) return false;
        foreach (Delegate handler in handlers.GetInvocationList()) if (ReferenceEquals(handler.Target, target)) return true;
        return false;
    }

    /// <summary>断言观察到的行为结果。</summary>
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
