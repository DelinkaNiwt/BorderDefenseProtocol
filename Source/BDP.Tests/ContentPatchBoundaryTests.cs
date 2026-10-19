using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BDP.Content.CombatBody.Protection;
using BDP.Content.Trigger.Black.Formation;
using BDP.Core.Information;
using BDP.Core.Trigger;
using HarmonyLib;
using RimWorld;
using Verse;

/// <summary>编入真实正式源码，验证不触碰普通路径、嵌套恢复、附加统计和实际补丁注册。</summary>
internal static class ContentPatchBoundaryTests
{
    /// <summary>实际依赖目录。</summary>
    private static string[] paths;
    /// <summary>私有成员读取范围。</summary>
    private const BindingFlags Hidden = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    /// <summary>抑制重算的实际调用次数。</summary>
    private static int suppressionCalls;
    /// <summary>失败项目数。</summary>
    private static int failures;
    /// <summary>统计发布器夹具提供的有效动态条目。</summary>
    private static IReadOnlyList<StatDrawEntry> publishedEntries;
    /// <summary>当前夹具是否允许动态发布。</summary>
    private static bool publicationAvailable;
    /// <summary>实际信息卡调用动态发布的次数。</summary>
    private static int publicationCalls;
    /// <summary>模拟第三方补丁允许或拒绝移除实际特性实例。</summary>
    private static bool traitRemovalAllowed;
    /// <summary>控制原版是否走不可用条目的构造分支。</summary>
    private static bool disabledStatistics;

    /// <summary>先接入实际安装依赖，再进入游戏类型夹具。</summary>
    private static int Main(string[] args)
    {
        paths = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); return failures == 0 ? 0 : 1; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>只加载指定安装目录中的程序集。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in paths)
        {
            string candidate = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
        }
        return null;
    }

    /// <summary>分项执行，确保修改前能够同时看到真实行为失败。</summary>
    private static void Run()
    {
        Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        new Harmony("BDP.Tests.ContentBoundary.DefOfFixture").Patch(
            AccessTools.Method(typeof(DefOfHelper), nameof(DefOfHelper.EnsureInitializedInCtor)),
            prefix: new HarmonyMethod(typeof(ContentPatchBoundaryTests), nameof(SkipDefOfWarning)));
        Case("F02 空移除不重算", Traits);
        Case("F04 倒地正常与异常恢复", () => Scope(typeof(Patch_Pawn_HealthTracker_MakeDowned_CombatBodyTriggerProtection)));
        Case("F04 出生正常与异常恢复", () => Scope(typeof(Patch_Pawn_EquipmentTracker_Notify_PawnSpawned_CombatBodyTriggerProtection)));
        Case("F12 第三方武器统计保留", ExtraStats);
        Case("F12 原版迭代器实际替换与回退", NativeStats);
        Case("F13 原版两个目标注册", Registration);
        Console.WriteLine("Content 边界验证失败数：" + failures);
    }

    /// <summary>记录每项真实失败，不因首个失败跳过其它范围。</summary>
    private static void Case(string label, Action action)
    {
        try { action(); Console.WriteLine("通过：" + label); }
        catch (Exception error) { failures++; Console.Error.WriteLine("失败：" + label + "：" + (error.InnerException ?? error)); }
    }

    /// <summary>实际 TraitSet 只拦截重算的计数，移除入口直接执行正式服务。</summary>
    private static void Traits()
    {
        Harmony harmony = new Harmony("BDP.Tests.ContentBoundary.Traits");
        MethodInfo recalculate = AccessTools.Method(typeof(TraitSet), "RecalculateSuppression");
        harmony.Patch(recalculate, prefix: new HarmonyMethod(typeof(ContentPatchBoundaryTests), nameof(CountSuppression)));
        harmony.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.RemoveTrait)),
            prefix: new HarmonyMethod(typeof(ContentPatchBoundaryTests), nameof(FixtureTraitRemoval)));
        try
        {
            Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
            pawn.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
            pawn.def.defName = "TraitPawn";
            pawn.def.race = new RaceProperties();
            pawn.health = (Pawn_HealthTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_HealthTracker));
            pawn.story = (Pawn_StoryTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_StoryTracker));
            pawn.story.traits = new TraitSet(pawn);
            suppressionCalls = 0;
            BlackTriggerInheritedTraitService.RemoveAllTemporary(pawn);
            BlackTriggerInheritedTraitService.RemoveFromSourceId(pawn, "absent");
            Check(suppressionCalls == 0, "没有移除 BDP 临时特性却调用了抑制重算：" + suppressionCalls);
            TraitDef ordinary = new TraitDef { defName = "Ordinary", degreeDatas = new List<TraitDegreeData> { new TraitDegreeData { degree = 0 } } };
            pawn.story.traits.allTraits.Add(new Trait(ordinary));
            BlackTriggerInheritedTraitService.RemoveAllTemporary(pawn);
            Check(suppressionCalls == 0 && pawn.story.traits.allTraits.Count == 1, "普通特性清理路径产生了业务写入");
            TraitDef inheritedDef = new TraitDef { defName = "Inherited", degreeDatas = new List<TraitDegreeData> { new TraitDegreeData { degree = 0 } } };
            Trait_BlackTriggerInherited inherited = new Trait_BlackTriggerInherited(inheritedDef, 0, "source");
            pawn.story.traits.allTraits.Add(inherited);
            traitRemovalAllowed = false;
            BlackTriggerInheritedTraitService.RemoveFromSourceId(pawn, "source");
            Check(suppressionCalls == 0 && pawn.story.traits.allTraits.Contains(inherited), "第三方拒绝移除时不能重算或恢复能力");
            traitRemovalAllowed = true;
            BlackTriggerInheritedTraitService.RemoveFromSourceId(pawn, "other");
            Check(suppressionCalls == 0 && pawn.story.traits.allTraits.Contains(inherited), "其它来源不应移除或重算当前临时特性");
            BlackTriggerInheritedTraitService.RemoveFromSourceId(pawn, "source");
            Check(suppressionCalls == 1 && !pawn.story.traits.allTraits.Contains(inherited), "实际移除来源特性后应重算一次");
            BlackTriggerInheritedTraitService.RemoveFromSourceId(pawn, "source");
            Check(suppressionCalls == 1, "已经移除的来源不能再次重算");
            pawn.story.traits.allTraits.Add(new Trait_BlackTriggerInherited(inheritedDef, 0, "source"));
            BlackTriggerInheritedTraitService.RemoveAllTemporary(pawn);
            Check(suppressionCalls == 2 && pawn.story.traits.allTraits.Count == 1, "死亡清理只应删除 BDP 临时特性并重算一次");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    /// <summary>计数真实调用，跳过与本测试无关的基因环境读取。</summary>
    private static bool CountSuppression() { suppressionCalls++; return false; }

    /// <summary>离线夹具自行提供定义，不运行 Unity 日志或游戏定义绑定。</summary>
    private static bool SkipDefOfWarning() { return false; }

    /// <summary>替代依赖 Unity 的原版移除出口，验证第三方实际移除或拒绝移除时的服务响应。</summary>
    private static bool FixtureTraitRemoval(TraitSet __instance, Trait trait)
    {
        if (traitRemovalAllowed) __instance.allTraits.Remove(trait);
        return false;
    }

    /// <summary>使用真实状态对象和真实嵌套上下文，不模拟游戏倒地算法。</summary>
    private static void Scope(Type patch)
    {
        MethodInfo prefix = patch.GetMethod("Prefix");
        Type stateType = prefix.GetParameters().Last().ParameterType.GetElementType();
        Check(stateType != typeof(bool), "出生保护只有布尔值，不能在两个出口幂等恢复");
        Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "ScopePawn";
        Type context = patch.Assembly.GetType("BDP.Content.CombatBody.Protection.CombatBodyTriggerProtectionContext", true);
        MethodInfo enter = context.GetMethod("Enter", Hidden);
        MethodInfo contains = context.GetMethod("Contains", Hidden);
        object outer = Activator.CreateInstance(stateType);
        object inner = Activator.CreateInstance(stateType);
        foreach (object state in new[] { outer, inner })
        {
            stateType.GetField("Pawn", Hidden).SetValue(state, pawn);
            stateType.GetField("PreserveTrigger", Hidden).SetValue(state, true);
            enter.Invoke(null, new object[] { pawn });
        }
        MethodInfo postfix = patch.GetMethod("Postfix");
        postfix.Invoke(null, new[] { inner });
        Check((bool)contains.Invoke(null, new object[] { pawn }), "内层正常退出提前清掉外层保护");
        MethodInfo finalizer = patch.GetMethod("Finalizer");
        Check(finalizer != null, "异常退出没有清理入口");
        Exception failure = new InvalidOperationException("夹具异常");
        Check(ReferenceEquals(finalizer.Invoke(null, new object[] { inner, failure }), failure), "清理吞掉了原始异常");
        Check((bool)contains.Invoke(null, new object[] { pawn }), "同一内层清理两次误退出了外层保护");
        Check(ReferenceEquals(finalizer.Invoke(null, new object[] { outer, failure }), failure), "清理吞掉了原始异常");
        Check(!(bool)contains.Invoke(null, new object[] { pawn }), "外层异常退出残留了保护上下文");
        postfix.Invoke(null, new[] { outer });
        Check(!(bool)contains.Invoke(null, new object[] { pawn }), "重复正常清理重新产生上下文");
    }

    /// <summary>执行正式条目替换方法，第三方条目即使属于武器分类也必须保留。</summary>
    private static void ExtraStats()
    {
        StatCategoryDef category = new StatCategoryDef { defName = "Weapon_Melee" };
        StatCategoryDefOf.Weapon_Melee = category;
        StatDrawEntry foreign = (StatDrawEntry)FormatterServices.GetUninitializedObject(typeof(StatDrawEntry));
        foreign.category = category;
        StatDrawEntry dynamic = (StatDrawEntry)FormatterServices.GetUninitializedObject(typeof(StatDrawEntry));
        dynamic.category = category;
        Type patch = typeof(Patch_Pawn_HealthTracker_MakeDowned_CombatBodyTriggerProtection).Assembly.GetType("BDP.Content.Patches.Patch_ThingWithComps_SpecialDisplayStats_TriggerBodyWeaponInfo", true);
        MethodInfo replace = patch.GetMethod("ReplaceWeaponEntries", Hidden);
        IEnumerable<StatDrawEntry> result = (IEnumerable<StatDrawEntry>)replace.Invoke(null, new object[] { new[] { foreign }, new List<StatDrawEntry> { dynamic } });
        Check(result.Contains(foreign) && result.Contains(dynamic), "第三方武器分类条目被整体删除");
    }

    /// <summary>运行真实原版迭代器、构造点改写与出口，验证复用同一 StatDef 的第三方行仍保留。</summary>
    private static void NativeStats()
    {
        Harmony harmony = new Harmony("BDP.Tests.ContentBoundary.NativeStats");
        MethodInfo stats = AccessTools.Method(typeof(StatsReportUtility), "StatsToDraw", new[] { typeof(Thing) });
        StatDef placeholder = new StatDef { defName = "MeleeWeapon_AverageDPS", category = StatCategoryDefOf.Weapon_Melee,
            workerClass = typeof(FixtureStatWorker), showOnDefaultValue = true };
        StatDef auxiliary = new StatDef { defName = "ThirdPartyWeaponStat", category = StatCategoryDefOf.Weapon_Melee,
            workerClass = typeof(FixtureStatWorker), showOnDefaultValue = true };
        DefDatabase<StatDef>.Add(placeholder);
        DefDatabase<StatDef>.Add(auxiliary);
        try
        {
            harmony.PatchAll(typeof(ContentPatchBoundaryTests).Assembly);
            harmony.Patch(AccessTools.Method(typeof(StatsReportUtility), "DescriptionEntry", new[] { typeof(Thing) }),
                prefix: new HarmonyMethod(typeof(ContentPatchBoundaryTests), nameof(EmptyDescription)));
            harmony.Patch(AccessTools.Method(typeof(StatsReportUtility), "QualityEntry"),
                prefix: new HarmonyMethod(typeof(ContentPatchBoundaryTests), nameof(EmptyDescription)));
            harmony.Patch(AccessTools.Method(typeof(StatWorker), "GetValue", new[] { typeof(Thing), typeof(bool), typeof(int) }),
                prefix: new HarmonyMethod(typeof(ContentPatchBoundaryTests), nameof(FixtureStatValue)));
            harmony.Patch(AccessTools.Method(typeof(TriggerBodyWeaponInfoPublisher), "TryBuild"),
                prefix: new HarmonyMethod(typeof(ContentPatchBoundaryTests), nameof(FixturePublication)));
            StatDrawEntry foreign = new StatDrawEntry(placeholder.category, placeholder, 99f, default(StatRequest));
            StatDrawEntry dynamic = new StatDrawEntry(placeholder.category, placeholder, 42f, default(StatRequest));
            FixtureStatsThing trigger = MakeStatsThing(true, foreign);
            publishedEntries = new[] { dynamic };
            publicationAvailable = true;
            publicationCalls = 0;
            List<StatDrawEntry> result = ((IEnumerable<StatDrawEntry>)stats.Invoke(null, new object[] { trigger })).ToList();
            Check(result.Contains(foreign) && result.Contains(dynamic), "真实出口未保留第三方复用原版 StatDef 的条目");
            Check(result.Count(entry => entry?.stat == placeholder) == 2, "原版占位行仍在，实际构造点没有被替换");
            Check(result.Count(entry => entry?.stat == auxiliary) == 1, "未提供动态替代的第三方 StatDef 被误删");
            disabledStatistics = true;
            result = ((IEnumerable<StatDrawEntry>)stats.Invoke(null, new object[] { trigger })).ToList();
            Check(result.Contains(foreign) && result.Contains(dynamic)
                && result.Count(entry => entry?.stat == placeholder) == 2, "不可用原版占位行的构造分支没有被替换");
            disabledStatistics = false;
            publicationAvailable = false;
            result = ((IEnumerable<StatDrawEntry>)stats.Invoke(null, new object[] { trigger })).ToList();
            Check(!result.Contains(dynamic) && result.Contains(foreign)
                && result.Count(entry => entry?.stat == placeholder) == 2, "动态发布失败未保留完整原版条目");
            publicationAvailable = true;
            publishedEntries = new StatDrawEntry[0];
            result = ((IEnumerable<StatDrawEntry>)stats.Invoke(null, new object[] { trigger })).ToList();
            Check(result.Contains(foreign) && result.Count(entry => entry?.stat == placeholder) == 2,
                "动态条目为空时不应执行替换");
            publishedEntries = new[] { dynamic };
            int callsBeforeOrdinary = publicationCalls;
            result = ((IEnumerable<StatDrawEntry>)stats.Invoke(null, new object[] { MakeStatsThing(false, foreign) })).ToList();
            Check(publicationCalls == callsBeforeOrdinary && result.Contains(foreign)
                && result.Count(entry => entry?.stat == placeholder) == 2, "普通物品进入了 BDP 动态统计逻辑");
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            DefDatabase<StatDef>.Clear();
        }
    }

    /// <summary>建立实际触发体组件或普通物品，只提供信息卡所需字段。</summary>
    private static FixtureStatsThing MakeStatsThing(bool trigger, StatDrawEntry extra)
    {
        FixtureStatsThing thing = (FixtureStatsThing)FormatterServices.GetUninitializedObject(typeof(FixtureStatsThing));
        thing.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        thing.def.defName = trigger ? "TestTrigger" : "OrdinaryThing";
        thing.Extra = extra;
        if (trigger)
        {
            CompTriggerBody comp = (CompTriggerBody)FormatterServices.GetUninitializedObject(typeof(CompTriggerBody));
            comp.parent = thing;
            typeof(ThingWithComps).GetField("comps", Hidden).SetValue(thing, new List<ThingComp> { comp });
        }
        return thing;
    }

    /// <summary>说明和品质不涉及本次统计来源验证，避免离线 Unity 文本环境。</summary>
    private static bool EmptyDescription(ref StatDrawEntry __result) { __result = null; return false; }

    /// <summary>将公式输入固定，原版枚举和 StatDrawEntry 构造仍实际执行。</summary>
    private static bool FixtureStatValue(ref float __result) { __result = 1f; return false; }

    /// <summary>提供有效或失败发布输入；正式消费者仍负责全部 BDP 准入和条目替换。</summary>
    private static bool FixturePublication(out IReadOnlyList<StatDrawEntry> entries, ref bool __result)
    {
        publicationCalls++;
        entries = publishedEntries;
        __result = publicationAvailable;
        return false;
    }

    /// <summary>附加条目模拟第三方组件的真实枚举出口。</summary>
    private sealed class FixtureStatsThing : ThingWithComps
    {
        /// <summary>由第三方提供的附加实例。</summary>
        public StatDrawEntry Extra;

        /// <summary>返回同一个第三方条目，便于验证实例保留。</summary>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats() { yield return Extra; }
    }

    /// <summary>只提供展示条件，数值仍走实际原版迭代器与构造。</summary>
    public sealed class FixtureStatWorker : StatWorker
    {
        /// <summary>展示夹具登记的两项定义。</summary>
        public override bool ShouldShowFor(StatRequest req) { return true; }

        /// <summary>正常启用，覆盖带数值的原版构造点。</summary>
        public override bool IsDisabledFor(Thing thing) { return disabledStatistics; }
    }

    /// <summary>运行 Harmony 实际扫描，不能仅检查源码存在属性。</summary>
    private static void Registration()
    {
        Harmony harmony = new Harmony("BDP.Tests.ContentBoundary.Registration");
        try
        {
            harmony.PatchAll(typeof(ContentPatchBoundaryTests).Assembly);
            foreach (string target in new[] { "ClosestUnfinishedThingForBill", "FinishUftJob" })
            {
                MethodInfo method = AccessTools.Method(typeof(WorkGiver_DoBill), target);
                Check(method != null, "原版目标不存在：" + target);
                Patches patches = Harmony.GetPatchInfo(method);
                Check(patches != null && patches.Prefixes.Any(value => value.owner == harmony.Id), "实际扫描没有注册：" + target);
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    /// <summary>失败时保留明确的行为判断。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
