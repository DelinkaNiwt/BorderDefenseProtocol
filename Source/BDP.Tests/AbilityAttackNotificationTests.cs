using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using BDP.Core.Abilities;
using BDP.Core.AttackExecution;
using BDP.Patches;
using HarmonyLib;
using RimWorld;
using Verse;

/// <summary>真实原版连射流程验证共用能力宿主的攻击通知；效果落地和破隐移除另需地图实测。</summary>
internal static class AbilityAttackNotificationTests
{
    /// <summary>真实依赖目录。</summary>
    private static string[] directories;
    /// <summary>当前事件次数。</summary>
    private static int count;
    /// <summary>最近一次真实通知。</summary>
    private static AttackActionSuccess last;
    /// <summary>安装真实观察补丁后验证覆盖与隔离。</summary>
    private static int Main(string[] args)
    {
        directories = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        return Run(args);
    }
    /// <summary>用真实原版分支消费受控单次施放返回值，不复制原版收尾。</summary>
    private static int Run(string[] args)
    {
        Harmony harmony = new Harmony("bdp.tests.ability.attack-notification");
        try
        {
            Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
            harmony.CreateClassProcessor(typeof(Patch_Verb_TryCastNextBurstShot_BdpAttackActionSuccess)).Patch();
            AttackActionSuccessDispatcher.AttackActionSucceeded += Observe;
            ShotAbility hostile = NewShot(true, false);
            Burst(hostile);
            Check(count == 1, "成功的攻击射击能力没有发布一次攻击通知（实际 " + count + " 次）");
            Check(last.SourceKind == AttackActionSourceKind.Ability && ReferenceEquals(last.Ability, hostile.Ability), "射击能力被误报为武器或丢失能力身份");
            Console.WriteLine("PASS 攻击射击能力及身份与收尾时点");
            hostile.Succeeds = false;
            Reset(); Burst(hostile);
            Check(count == 0, "施放失败却通知攻击完成");
            hostile.Succeeds = true;
            hostile.IsAvailable = false;
            Reset(); Burst(hostile);
            Check(count == 0, "未执行单次施放却通知攻击完成");
            Console.WriteLine("PASS 失败和不可用施放不通知");
            Reset(); Burst(NewShot(false, false));
            Check(count == 0, "非攻击能力误发攻击通知");
            Reset(); Burst(NewShot(false, true));
            Check(count == 1, "声明暴力但非敌对的能力遗漏");
            Console.WriteLine("PASS 非攻击隔离与暴力分类");
            ShotAbility outer = NewShot(true, false);
            ShotAbility inner = NewShot(true, false);
            outer.BeforeResult = delegate { Burst(inner); };
            Reset(); Burst(outer);
            Check(count == 2, "嵌套能力应该各通知一次");
            Console.WriteLine("PASS 嵌套施放隔离");
            outer.BeforeResult = delegate { throw new InvalidOperationException("fixture"); };
            Reset();
            try { Burst(outer); throw new Exception("夹具异常没有穿过真实原版入口"); }
            catch (TargetInvocationException error) { Check(error.InnerException is InvalidOperationException, "异常来源不正确"); }
            Check(count == 0, "异常执行错误发布成功通知");
            Burst(inner);
            Check(count == 1, "异常后观察作用域污染下一次施放");
            Console.WriteLine("PASS 异常恢复");
            Reset();
            NormalAbility normal = new NormalAbility();
            Prepare(normal, true, false);
            Burst(normal);
            Check(count == 1, "走原版能力激活的普通能力被重复通知");
            normal.Succeeds = false;
            Reset(); Burst(normal);
            Check(count == 0, "原版能力激活失败却通知");
            TestDamagingMovementDefinition(args[3], args[2]);
            Console.WriteLine("AbilityAttackNotificationTests PASS：共用射击能力成功一次、失败/取消不通知、非攻击隔离、能力身份、原版收尾时点、嵌套/异常和普通能力去重。");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { AttackActionSuccessDispatcher.AttackActionSucceeded -= Observe; harmony.UnpatchAll(harmony.Id); }
    }
    /// <summary>使用正式推进定义验证带敌人伤害的位移也走共用能力通知，不需要造成实际伤害。</summary>
    private static void TestDamagingMovementDefinition(string definitionsDirectory, string assemblyDirectory)
    {
        XmlDocument xml = new XmlDocument();
        xml.Load(Path.Combine(definitionsDirectory, "LightSoulPropulsion.xml"));
        bool hostile = bool.Parse(xml.SelectSingleNode("/Defs/AbilityDef/hostile").InnerText);
        bool violent = bool.Parse(xml.SelectSingleNode("/Defs/AbilityDef/verbProperties/violent").InnerText);
        bool requiresViolence = bool.Parse(xml.SelectSingleNode("/Defs/AbilityDef/casterMustBeCapableOfViolence").InnerText);
        Check(!violent && !requiresViolence, "能力通知修正连带改变了推进的位移准入");
        NormalAbility movement = new NormalAbility();
        Prepare(movement, hostile, violent);
        Assembly content = Assembly.LoadFrom(Path.Combine(assemblyDirectory, "BDP.Content.dll"));
        MethodInfo ranged = content.GetType("BDP.Content.LightSoul.LightSoulDefenseWatchUtility", true)
            .GetMethod("IsRangedWeapon", BindingFlags.NonPublic | BindingFlags.Static);
        Check(!(bool)ranged.Invoke(null, new object[] { movement }), "带伤害位移因敌对声明误入举盾禁止的远程分类");
        Reset(); Burst(movement);
        Check(count == 1, "正式推进定义包含对敌伤害，却仍按纯位移遗漏攻击通知");
        Console.WriteLine("PASS 正式带伤害位移定义进入共用攻击通知，位移准入保留");
    }
    /// <summary>准备不依赖地图、健康和材质的真实动作表面。</summary>
    private static void Prepare(Verb_CastAbility verb, bool hostile, bool violent)
    {
        verb.caster = new Thing();
        verb.verbProps = new VerbProperties { violent = violent };
        verb.Ability = (Ability)FormatterServices.GetUninitializedObject(typeof(Ability));
        AbilityDef def = (AbilityDef)FormatterServices.GetUninitializedObject(typeof(AbilityDef));
        def.hostile = hostile;
        def.verbProperties = verb.verbProps;
        verb.Ability.def = def;
        // 原版激活接线读取 Ability.verb；提供真实主动作索引，避免离线夹具误入初始化地图路径。
        verb.verbProps.isPrimary = true;
        VerbTracker tracker = new VerbTracker(verb.Ability);
        AccessTools.Field(typeof(VerbTracker), "verbs").SetValue(tracker, new List<Verb> { verb });
        AccessTools.Field(typeof(Ability), "verbTracker").SetValue(verb.Ability, tracker);
        verb.verbTracker = tracker;
    }
    /// <summary>构造共用射击宿主的执行样例，不针对某个能力名。</summary>
    private static ShotAbility NewShot(bool hostile, bool violent)
    {
        ShotAbility verb = new ShotAbility { Succeeds = true, IsAvailable = true };
        Prepare(verb, hostile, violent);
        verb.castCompleteCallback = delegate { verb.Completed = true; };
        return verb;
    }
    /// <summary>调用真实原版下一发，保留声音、计数及完成回调的顺序。</summary>
    private static void Burst(Verb verb)
    {
        ShotAbility shot = verb as ShotAbility;
        if (shot != null) shot.Completed = false;
        AccessTools.Field(typeof(Verb), "burstShotsLeft").SetValue(verb, 1);
        AccessTools.Method(typeof(Verb), "TryCastNextBurstShot").Invoke(verb, null);
    }
    /// <summary>记录中性通知，射击能力必须在原版收尾之后发布。</summary>
    private static void Observe(AttackActionSuccess attack)
    {
        ShotAbility shot = attack.Verb as ShotAbility;
        if (shot != null) Check(shot.Completed, "射击能力通知提前到原版完成回调之前");
        count++; last = attack;
    }
    /// <summary>清空夹具观察，不改变生产规则。</summary>
    private static void Reset() { count = 0; last = null; }
    /// <summary>结果断言。</summary>
    private static void Check(bool result, string message) { if (!result) throw new Exception(message); }
    /// <summary>解析真实游戏依赖。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in directories) { string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }
    /// <summary>只替代需地图的发射效果，共用宿主及原版连射控制流不变。</summary>
    private sealed class ShotAbility : BdpVerb_AbilityShoot
    {
        /// <summary>单次实际执行结果。</summary>
        public bool Succeeds;
        /// <summary>执行前是否允许。</summary>
        public bool IsAvailable;
        /// <summary>真实完成回调事实。</summary>
        public bool Completed;
        /// <summary>模拟同步嵌套或异常效果。</summary>
        public Action BeforeResult;
        /// <summary>剥离地图前提，保留原版可用性分支。</summary>
        public override bool Available() { return IsAvailable; }
        /// <summary>向真实原版虚调用提供成功或失败事实。</summary>
        protected override bool TryCastShot() { if (BeforeResult != null) BeforeResult(); return Succeeds; }
    }
    /// <summary>能力激活通知的现有独立路径，必须被射击观察排除。</summary>
    private sealed class NormalAbility : BdpVerb_CastAbility
    {
        /// <summary>激活是否成功。</summary>
        public bool Succeeds = true;
        /// <summary>隔离地图资格。</summary>
        public override bool Available() { return true; }
        /// <summary>调用现有能力激活适配来模拟效果完成边界。</summary>
        protected override bool TryCastShot() { Patch_Ability_Activate_BdpAttackActionSuccess.Postfix(Ability, Succeeds); return Succeeds; }
    }
}
