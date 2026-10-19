using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>在成品黑触发器上永久保存本次形成的玩家可读生命印记。</summary>
    public sealed class CompBlackTriggerImprint : ThingComp
    {
        /// <summary>生命印记来源名称。</summary>
        public string MakerLabel;

        /// <summary>生命印记来源 LoadID。</summary>
        public string MakerLoadId;

        /// <summary>主动献身或伤病死亡。</summary>
        public BlackTriggerFormationSource Source;

        /// <summary>玩家选择后最终生成结果所属的战斗侧重。</summary>
        public BlackTriggerEmphasisDef Emphasis;

        /// <summary>实际支撑最终候选路线的冻结事实。</summary>
        public List<BlackTriggerFactSnapshot> EvidenceFacts = new List<BlackTriggerFactSnapshot>();

        /// <summary>选定传承人 LoadID。</summary>
        public string HeirLoadId;

        /// <summary>选定传承人名称。</summary>
        public string HeirLabel;

        /// <summary>玩家选择继承的原版特性；空值表示不继承。</summary>
        public BlackTriggerTraitOptionSnapshot InheritedTrait;

        /// <summary>一次性写入最终形成结果。</summary>
        public void Configure(
            BlackTriggerMakerSnapshot snapshot,
            BlackTriggerCandidatePlan candidate,
            IEnumerable<BlackTriggerFactSnapshot> evidenceFacts,
            BlackTriggerHeirCandidateSnapshot heir,
            BlackTriggerTraitOptionSnapshot inheritedTrait)
        {
            MakerLabel = snapshot?.MakerLabel;
            MakerLoadId = snapshot?.MakerLoadId;
            Source = snapshot != null ? snapshot.Source : BlackTriggerFormationSource.NaturalDeath;
            Emphasis = candidate?.AssignedEmphasis;
            EvidenceFacts = evidenceFacts != null
                ? new List<BlackTriggerFactSnapshot>(evidenceFacts)
                : new List<BlackTriggerFactSnapshot>();
            HeirLoadId = heir?.LoadId;
            HeirLabel = heir?.Label;
            InheritedTrait = inheritedTrait;
        }

        /// <summary>装备黑触发器时协调真实临时特性。</summary>
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            BlackTriggerInheritedTraitService.Apply(pawn, parent, InheritedTrait);
        }

        /// <summary>卸下黑触发器时只移除本件物品提供的临时特性。</summary>
        public override void Notify_Unequipped(Pawn pawn)
        {
            BlackTriggerInheritedTraitService.RemoveFromSource(pawn, parent);
            base.Notify_Unequipped(pawn);
        }

        /// <summary>保存生命印记。</summary>
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref MakerLabel, "makerLabel");
            Scribe_Values.Look(ref MakerLoadId, "makerLoadId");
            Scribe_Values.Look(ref Source, "source");
            Scribe_Defs.Look(ref Emphasis, "emphasis");
            Scribe_Collections.Look(ref EvidenceFacts, "evidenceFacts", LookMode.Deep);
            Scribe_Values.Look(ref HeirLoadId, "heirLoadId");
            Scribe_Values.Look(ref HeirLabel, "heirLabel");
            Scribe_Deep.Look(ref InheritedTrait, "inheritedTrait");
            if (EvidenceFacts == null) EvidenceFacts = new List<BlackTriggerFactSnapshot>();
        }

        /// <summary>返回生命印记详情按钮。</summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "BDP_BlackTrigger_Imprint_GizmoLabel".Translate(),
                defaultDesc = "BDP_BlackTrigger_Imprint_GizmoDesc".Translate(),
                icon = TexButton.Info,
                action = delegate { Find.WindowStack.Add(new Window_BlackTriggerImprint(this)); }
            };
        }

        /// <summary>在检查面板显示来源和战斗侧重摘要。</summary>
        public override string CompInspectStringExtra()
        {
            return BlackTriggerImprintFormatter.FormatInspectSummary(this);
        }

        /// <summary>在原版信息卡中增加可展开的生命印记条目。</summary>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.Basics,
                "BDP_BlackTrigger_Imprint_StatLabel".Translate(),
                BlackTriggerImprintFormatter.FormatStatValue(this),
                BlackTriggerImprintFormatter.FormatFull(this),
                900);
        }
    }
}
