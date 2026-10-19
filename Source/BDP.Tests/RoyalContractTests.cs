using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Verse;

/// <summary>调用真实契约状态；死亡终点用记录型人物替代完整地图死亡，边界明确留实测。</summary>
internal static class RoyalContractTests
{
    /// <summary>当前安装依赖目录。</summary>
    private static string[] paths;
    /// <summary>真实契约状态类型。</summary>
    private static Type contractType;
    /// <summary>累计调用真实状态死亡通知的次数。</summary>
    private static int notifications;

    /// <summary>先挂载依赖解析，再运行正式程序集的状态行为。</summary>
    private static int Main(string[] args)
    {
        paths = args;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    /// <summary>只读取当前游戏与模组程序集。</summary>
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        foreach (string directory in paths)
        {
            string file = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(file)) return Assembly.LoadFrom(file);
        }
        return null;
    }

    /// <summary>不运行地图：验证死亡接线、资格记录及原版复制排除的运行类边界。</summary>
    private static void Run()
    {
        Assembly content = Assembly.LoadFrom(Path.Combine(paths[2], "BDP.Content.dll"));
        // 原版借住判断读取任务管理器；夹具只提供其实际额外派系列表，不启动地图。
        Current.Game = (Game)FormatterServices.GetUninitializedObject(typeof(Game));
        Current.Game.questManager = new RimWorld.QuestManager();
        contractType = content.GetType("BDP.Content.Trion.Royalty.Contract.Hediff_RoyalContract", true);
        RecordingPawn royal = MakePawn(1);
        RecordingPawn partner = MakePawn(2);
        Link(royal, partner);
        Check(!royal.Contract.ShouldRemove && !partner.Contract.ShouldRemove, "契约必须是永久记录");
        Check((bool)contractType.GetField("royalSigner").GetValue(royal.Contract), "皇室身份记录需保留");
        Check(ReferenceEquals(contractType.GetField("partner").GetValue(royal.Contract), partner), "必须保存具体人物对象");
        Check(!royal.Contract.TryMergeWith(partner.Contract), "契约不能被同类健康状态合并");
        royal.Kill(null, null);
        Check(royal.Dead && partner.Dead && royal.KillCalls == 1 && partner.KillCalls == 1, "双方必须各死亡一次，无递归");
        Notify(royal.Contract);
        Check(partner.KillCalls == 1, "重复死亡通知不能重复杀死已死亡对象");
        RecordingPawn recipient = MakePawn(3);
        RecordingPawn signer = MakePawn(4);
        Link(signer, recipient);
        recipient.Kill(null, null);
        Check(signer.Dead && signer.KillCalls == 1, "契约者死亡也必须联动皇室，不是单向绑定");
        RecordingPawn lone = MakePawn(5);
        lone.Contract = MakeRecord(lone, null, true);
        Notify(lone.Contract);
        Check(lone.KillCalls == 0, "对象引用不存在时不能误杀其他人物");
        Hediff copied = (Hediff)Activator.CreateInstance(contractType);
        copied.CopyFrom(signer.Contract);
        Check(contractType.GetField("partner").GetValue(copied) == null, "原版 CopyFrom 不得扩散绑定引用");
        Check(!(bool)contractType.GetField("royalSigner").GetValue(copied), "不能复制皇室使用记录");
        var effect = (RimWorld.CompAbilityEffect)Activator.CreateInstance(content.GetType(
            "BDP.Content.Trion.Royalty.Contract.CompAbilityEffect_RoyalContract", true));
        Check(!effect.AICanTargetNow(new LocalTargetInfo(partner)), "不得由 AI 自动缔约");
        CheckEligibilityAndCopy(content, effect);
        Check(notifications == 6, "测试必须实际经过正式死亡通知");
        Console.WriteLine("通过：正式契约双向死亡通知、重复通知、无对象、永久记录、手动权限、资格不随基因重建重置及原版复制排除。完整施放及地图死亡仍需游戏实测。");
    }

    /// <summary>直接验证正式能力资格，并运行原版复制过滤，不复写过滤规则。</summary>
    private static void CheckEligibilityAndCopy(Assembly content, RimWorld.CompAbilityEffect effect)
    {
        var royalGene = new GeneDef { defName = "BDP_Gene_RoyalBloodline" };
        var glandGene = new GeneDef { defName = "BDP_Gene_TrionGland" };
        var contractDef = new HediffDef { defName = "BDP_Hediff_RoyalContract", duplicationAllowed = false };
        var properties = (AbilityCompProperties)Activator.CreateInstance(content.GetType(
            "BDP.Content.Trion.Royalty.Contract.CompProperties_AbilityEffect_RoyalContract", true));
        properties.GetType().GetField("royalGene").SetValue(properties, royalGene);
        properties.GetType().GetField("glandGene").SetValue(properties, glandGene);
        properties.GetType().GetField("contractHediff").SetValue(properties, contractDef);
        effect.Initialize(properties);
        var caster = MakePawn(10);
        caster.SetTestFaction(true);
        caster.genes.Endogenes.Add(new Gene { pawn = caster, def = royalGene });
        effect.parent = (RimWorld.Ability)FormatterServices.GetUninitializedObject(typeof(RimWorld.Ability));
        effect.parent.pawn = caster;
        Check(effect.CanCast && !effect.ShouldHideGizmo, "未使用的玩家派系皇室有主动资格");
        RimWorld.QuestPart_ExtraFaction lodging = AddLodger(caster);
        Check(RimWorld.QuestUtility.IsQuestLodger(caster), "夹具必须经过原版借住身份判断");
        Check(!effect.CanCast && effect.ShouldHideGizmo, "临时玩家派系的皇室借住者不能提前缔约");
        lodging.affectedPawns.Clear();
        Check(effect.CanCast && !effect.ShouldHideGizmo, "原版借住身份移除后应获得正式缔约资格");
        Check(!effect.Valid(new LocalTargetInfo(caster)), "不能与自己缔约");
        var boundTarget = MakePawn(11);
        boundTarget.SetTestFaction(true);
        Check(effect.Valid(new LocalTargetInfo(boundTarget)), "普通未绑定殖民者可被选择");
        lodging = AddLodger(boundTarget);
        Check(!effect.Valid(new LocalTargetInfo(boundTarget)), "任务借住者不属于正式缔约殖民者目标");
        lodging.affectedPawns.Clear();
        Hediff targetRecord = MakeRecord(boundTarget, caster, false);
        targetRecord.def = contractDef;
        boundTarget.health.hediffSet.hediffs.Add(targetRecord);
        Check(!effect.Valid(new LocalTargetInfo(boundTarget)), "不能与已绑定目标建立多人死亡链");
        caster.Contract = MakeRecord(caster, boundTarget, true);
        caster.Contract.def = contractDef;
        caster.health.hediffSet.hediffs.Add(caster.Contract);
        Check(!effect.CanCast, "缔约记录占用资格");
        caster.genes = new RimWorld.Pawn_GeneTracker(caster);
        caster.genes.Endogenes.Add(new Gene { pawn = caster, def = royalGene });
        Check(!effect.CanCast, "皇室基因对象重新构造也不能重置次数");
        var clone = MakePawn(12);
        typeof(RimWorld.GameComponent_PawnDuplicator).GetMethod("CopyHediffs", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { caster, clone });
        Check(clone.health.hediffSet.hediffs.Count == 0, "实际原版复制不能复制契约和使用记录");
        caster.SetTestFaction(false);
        Check(!effect.CanCast && effect.ShouldHideGizmo, "非玩家派系不能使用且不显示按钮");
    }

    /// <summary>提供原版进行中任务的小派系关系，以真实借住判断验证身份边界。</summary>
    private static RimWorld.QuestPart_ExtraFaction AddLodger(Pawn pawn)
    {
        var quest = (RimWorld.Quest)FormatterServices.GetUninitializedObject(typeof(RimWorld.Quest));
        quest.acceptanceTick = 0;
        quest.acceptanceExpireTick = -1;
        var part = new RimWorld.QuestPart_ExtraFaction
        {
            quest = quest,
            extraFaction = new RimWorld.ExtraFaction(pawn.Faction, RimWorld.ExtraFactionType.MiniFaction),
            affectedPawns = new List<Pawn> { pawn }
        };
        Current.Game.questManager.ExtraFactionQuestParts.Add(part);
        return part;
    }

    /// <summary>生成最小健康容器，避开 Unity 原生构造。</summary>
    private static RecordingPawn MakePawn(int id)
    {
        var pawn = (RecordingPawn)FormatterServices.GetUninitializedObject(typeof(RecordingPawn));
        pawn.thingIDNumber = id;
        pawn.def = (ThingDef)FormatterServices.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "ContractTestPawn";
        pawn.def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        pawn.kindDef = new PawnKindDef { defName = "ContractTestColonist" };
        pawn.health = (Pawn_HealthTracker)FormatterServices.GetUninitializedObject(typeof(Pawn_HealthTracker));
        typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pawn.health, pawn);
        var healthState = typeof(Pawn_HealthTracker).GetField("healthState", BindingFlags.Instance | BindingFlags.NonPublic);
        healthState.SetValue(pawn.health, Enum.Parse(healthState.FieldType, "Mobile"));
        pawn.health.hediffSet = new HediffSet(pawn);
        pawn.genes = new RimWorld.Pawn_GeneTracker(pawn);
        return pawn;
    }

    /// <summary>生成双方实际状态；测试不复制授予算法。</summary>
    private static void Link(RecordingPawn royal, RecordingPawn partner)
    {
        royal.Contract = MakeRecord(royal, partner, true);
        partner.Contract = MakeRecord(partner, royal, false);
    }

    /// <summary>填入真实状态的存档字段。</summary>
    private static Hediff MakeRecord(Pawn owner, Pawn partner, bool signer)
    {
        var record = (Hediff)Activator.CreateInstance(contractType);
        record.pawn = owner;
        contractType.GetField("partner").SetValue(record, partner);
        contractType.GetField("royalSigner").SetValue(record, signer);
        return record;
    }

    /// <summary>调用真实死亡回调，记录覆盖次数。</summary>
    private static void Notify(Hediff record)
    {
        notifications++;
        record.Notify_PawnDied(null, null);
    }

    /// <summary>断言带明确行为说明。</summary>
    private static void Check(bool result, string message)
    {
        if (!result) throw new InvalidOperationException(message);
    }

    /// <summary>仅替代原版地图死亡终点，不替代正式契约判断与联动。</summary>
    private sealed class RecordingPawn : Pawn
    {
        /// <summary>实际健康记录。</summary>
        public Hediff Contract;
        /// <summary>真实业务调用死亡入口的次数。</summary>
        public int KillCalls;
        /// <summary>只设置派系字段，避免触发原版世界管理回调。</summary>
        public void SetTestFaction(bool player)
        {
            factionInt = (RimWorld.Faction)FormatterServices.GetUninitializedObject(typeof(RimWorld.Faction));
            factionInt.def = new RimWorld.FactionDef { isPlayer = player };
        }
        /// <summary>保留原版先置死亡再通知的时序，避开尸体／地图及 Unity。</summary>
        public override void Kill(DamageInfo? dinfo, Hediff culprit = null)
        {
            KillCalls++;
            health.SetDead();
            if (Contract != null) Notify(Contract);
        }
    }
}
