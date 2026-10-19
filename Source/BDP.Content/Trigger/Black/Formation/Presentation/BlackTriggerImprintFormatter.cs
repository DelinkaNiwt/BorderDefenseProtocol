using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>形成窗口、检查栏、信息卡和生命印记窗口共用的玩家文本格式器。</summary>
    public static class BlackTriggerImprintFormatter
    {
        /// <summary>返回形成来源的玩家文本。</summary>
        public static string SourceLabel(BlackTriggerFormationSource source)
        {
            return (source == BlackTriggerFormationSource.Sacrifice
                ? "BDP_BlackTrigger_Source_Sacrifice"
                : "BDP_BlackTrigger_Source_Death").Translate().ToString();
        }

        /// <summary>返回技能兴趣的玩家文本。</summary>
        public static string PassionLabel(Passion passion)
        {
            if (passion == Passion.Major) return "BDP_BlackTrigger_Passion_Major".Translate().ToString();
            if (passion == Passion.Minor) return "BDP_BlackTrigger_Passion_Minor".Translate().ToString();
            return "BDP_BlackTrigger_Passion_None".Translate().ToString();
        }

        /// <summary>返回一条冻结事实的直白显示。</summary>
        public static string FormatFact(BlackTriggerFactSnapshot fact)
        {
            if (fact == null) return string.Empty;
            if (fact.Kind == BlackTriggerFactKind.Skill)
            {
                return "BDP_BlackTrigger_Formation_SkillLine".Translate(fact.Label, fact.ValueText, PassionLabel(fact.Passion));
            }
            return "BDP_BlackTrigger_Formation_RecordLine".Translate(fact.Label, fact.ValueText, fact.Rank);
        }

        /// <summary>返回特性原版名称和定义说明。</summary>
        public static string FormatTraitOption(BlackTriggerTraitOptionSnapshot option, Pawn pawn = null)
        {
            if (option?.TraitDef == null) return "BDP_BlackTrigger_Formation_TraitNone".Translate().ToString();
            TraitDegreeData degree = option.TraitDef.DataAtDegree(option.Degree);
            Trait trait = new Trait(option.TraitDef, option.Degree);
            string description = pawn == null
                ? degree?.description ?? option.TraitDef.description ?? string.Empty
                : trait.TipString(pawn);
            if (description.NullOrEmpty())
            {
                description = "BDP_BlackTrigger_Formation_TraitNoDescription".Translate().ToString();
            }
            string suppressed = option.WasSuppressed
                ? "BDP_BlackTrigger_Formation_TraitSuppressed".Translate().ToString()
                : string.Empty;
            return "BDP_BlackTrigger_Formation_TraitDetail".Translate(description, suppressed);
        }

        /// <summary>返回特性的原版机械效果摘要，不显示叙述性简介。</summary>
        public static string FormatTraitEffects(BlackTriggerTraitOptionSnapshot option, Pawn pawn)
        {
            if (option?.TraitDef == null || pawn == null) return string.Empty;
            string tip = new Trait(option.TraitDef, option.Degree).TipString(pawn);
            string normalized = tip.Replace("\r\n", "\n");
            int effectsStart = normalized.IndexOf("\n\n", System.StringComparison.Ordinal);
            return effectsStart < 0 ? string.Empty : normalized.Substring(effectsStart + 2).Trim();
        }

        /// <summary>返回非空的战斗侧重名称，避免空 Def 标签污染玩家界面或原版统计条目。</summary>
        public static string EmphasisLabel(BlackTriggerEmphasisDef emphasis)
        {
            if (emphasis != null)
            {
                string label = emphasis.LabelCap;
                if (!label.NullOrEmpty())
                {
                    return label;
                }
            }

            return "BDP_BlackTrigger_Formation_UnknownEmphasis".Translate().ToString();
        }

        /// <summary>返回检查栏的一行摘要。</summary>
        public static string FormatInspectSummary(CompBlackTriggerImprint imprint)
        {
            string maker = imprint?.MakerLabel.NullOrEmpty() == false
                ? imprint.MakerLabel
                : "BDP_BlackTrigger_UnknownMaker".Translate().ToString();
            string emphasis = imprint?.Emphasis == null
                ? "BDP_BlackTrigger_Imprint_None".Translate().ToString()
                : EmphasisLabel(imprint.Emphasis);
            return "BDP_BlackTrigger_Imprint_InspectSummary".Translate(maker, emphasis);
        }

        /// <summary>返回信息卡右栏的紧凑值。</summary>
        public static string FormatStatValue(CompBlackTriggerImprint imprint)
        {
            if (imprint?.Emphasis == null)
            {
                return "BDP_BlackTrigger_Imprint_None".Translate().ToString();
            }

            string value = EmphasisLabel(imprint.Emphasis);
            return value.NullOrEmpty()
                ? "BDP_BlackTrigger_Imprint_None".Translate().ToString()
                : value;
        }

        /// <summary>返回信息卡和详情窗口共用的完整生命印记文本。</summary>
        public static string FormatFull(CompBlackTriggerImprint imprint)
        {
            if (imprint == null) return string.Empty;
            StringBuilder builder = new StringBuilder();
            string maker = imprint.MakerLabel.NullOrEmpty()
                ? "BDP_BlackTrigger_UnknownMaker".Translate().ToString()
                : imprint.MakerLabel;
            builder.AppendLine("BDP_BlackTrigger_Imprint_Maker".Translate(maker));
            builder.AppendLine("BDP_BlackTrigger_Imprint_Source".Translate(SourceLabel(imprint.Source)));
            builder.AppendLine("BDP_BlackTrigger_Imprint_Emphasis".Translate(
                imprint.Emphasis == null
                    ? "BDP_BlackTrigger_Imprint_None".Translate().ToString()
                    : EmphasisLabel(imprint.Emphasis)));
            if (imprint.Emphasis != null && !imprint.Emphasis.detailKey.NullOrEmpty())
            {
                builder.AppendLine(imprint.Emphasis.detailKey.Translate());
            }
            List<BlackTriggerFactSnapshot> evidence = imprint.EvidenceFacts ?? new List<BlackTriggerFactSnapshot>();
            if (evidence.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("BDP_BlackTrigger_Imprint_Evidence".Translate());
                foreach (BlackTriggerFactSnapshot fact in evidence) builder.AppendLine("  • " + FormatFact(fact));
            }
            builder.AppendLine();
            builder.AppendLine("BDP_BlackTrigger_Imprint_Heir".Translate(
                imprint.HeirLabel.NullOrEmpty() ? "BDP_BlackTrigger_Imprint_None".Translate().ToString() : imprint.HeirLabel));
            builder.AppendLine("BDP_BlackTrigger_Imprint_InheritedTrait".Translate(
                imprint.InheritedTrait?.Label ?? "BDP_BlackTrigger_Formation_TraitNone".Translate().ToString()));
            if (imprint.InheritedTrait?.TraitDef != null)
            {
                builder.AppendLine("BDP_BlackTrigger_Imprint_InheritedTraitStatus".Translate(ResolveTraitStatus(imprint)));
            }
            return builder.ToString().TrimEndNewlines();
        }

        /// <summary>按当前装备者真实 TraitSet 返回继承效果状态。</summary>
        private static string ResolveTraitStatus(CompBlackTriggerImprint imprint)
        {
            Pawn pawn = ThingOwnerUtility.GetFirstParentThing(imprint.parent) as Pawn;
            if (pawn?.equipment?.AllEquipmentListForReading?.Contains(imprint.parent) != true)
            {
                return "BDP_BlackTrigger_Inheritance_StatusUnequipped".Translate().ToString();
            }
            Trait_BlackTriggerInherited inherited = pawn.story?.traits?.allTraits?
                .OfType<Trait_BlackTriggerInherited>()
                .FirstOrDefault(value => value.SourceThingId == imprint.parent.ThingID);
            if (inherited != null)
            {
                return (inherited.Suppressed
                    ? "BDP_BlackTrigger_Inheritance_StatusSuppressed"
                    : "BDP_BlackTrigger_Inheritance_StatusActive").Translate().ToString();
            }
            if (pawn.story?.traits?.allTraits?.Any(value => value != null && value.def == imprint.InheritedTrait.TraitDef) == true)
            {
                return "BDP_BlackTrigger_Inheritance_StatusAlreadyOwned".Translate().ToString();
            }
            Trait probe = new Trait(imprint.InheritedTrait.TraitDef, imprint.InheritedTrait.Degree);
            if (pawn.story?.traits?.allTraits?.Any(value => value != null
                && (value.def.ConflictsWith(probe) || imprint.InheritedTrait.TraitDef.ConflictsWith(value))) == true)
            {
                return "BDP_BlackTrigger_Inheritance_StatusConflict".Translate().ToString();
            }
            return "BDP_BlackTrigger_Inheritance_StatusPending".Translate().ToString();
        }

        /// <summary>收集某一候选最终侧重真正命中的事实。</summary>
        public static List<BlackTriggerFactSnapshot> ResolveEvidence(BlackTriggerMakerSnapshot snapshot, BlackTriggerCandidatePlan candidate)
        {
            if (snapshot?.Facts == null || candidate?.AssignedEmphasis == null) return new List<BlackTriggerFactSnapshot>();
            HashSet<string> keys = new HashSet<string>(
                (candidate.MatchedRoutes ?? new List<BlackTriggerMatchedRouteSnapshot>())
                    .Where(route => route?.Emphasis == candidate.AssignedEmphasis)
                    .SelectMany(route => route.MatchedFactKeys ?? new List<string>()),
                System.StringComparer.Ordinal);
            return snapshot.Facts.Where(value => value != null && keys.Contains(value.StableKey)).ToList();
        }
    }
}
