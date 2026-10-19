using System;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Royalty.Contract
{
    /// <summary>皇室契约业务参数；基因、状态由正式定义引用，不改核心腺体逻辑。</summary>
    public sealed class CompProperties_AbilityEffect_RoyalContract : CompProperties_AbilityEffect
    {
        /// <summary>提供主动能力的皇室基因。</summary>
        public GeneDef royalGene;
        /// <summary>经原版入口授予的内源腺体。</summary>
        public GeneDef glandGene;
        /// <summary>双方人物级永久契约记录。</summary>
        public HediffDef contractHediff;
        /// <summary>使用本业务效果；原版允许非攻击能力以婴儿为目标，不额外限制年龄。</summary>
        public CompProperties_AbilityEffect_RoyalContract()
        {
            compClass = typeof(CompAbilityEffect_RoyalContract);
            canTargetBaby = true;
        }
    }

    /// <summary>原版能力扩展点：玩家手动缔约、原版内源基因授予与成对记录。</summary>
    public sealed class CompAbilityEffect_RoyalContract : CompAbilityEffect
    {
        /// <summary>读取内容定义参数。</summary>
        private new CompProperties_AbilityEffect_RoyalContract Props =>
            (CompProperties_AbilityEffect_RoyalContract)props;
        /// <summary>非玩家派系和原版任务借住者不展示；借住者虽可控，但尚未正式加入。</summary>
        public override bool ShouldHideGizmo => parent.pawn.Faction?.IsPlayer != true
            || parent.pawn.IsQuestLodger();
        /// <summary>能力队列和施放阶段都服从永久一次性资格。</summary>
        public override bool CanCast => CasterProblem() == null;
        /// <summary>不让任何 AI 自动缔结永久契约。</summary>
        public override bool AICanTargetNow(LocalTargetInfo target) => false;

        /// <summary>资格耗尽后保留按钮说明，基因重建不重置契约。</summary>
        public override bool GizmoDisabled(out string reason)
        {
            string key = CasterProblem();
            reason = key == null ? null : key.Translate().ToString();
            return key != null;
        }

        /// <summary>施放者须为活着的正式玩家派系皇室人物，排除借住身份，且尚未绑定。</summary>
        private string CasterProblem()
        {
            Pawn caster = parent.pawn;
            if (caster == null || caster.Dead || caster.Faction?.IsPlayer != true
                || caster.IsQuestLodger()
                || caster.genes?.GetGene(Props.royalGene)?.Active != true)
                return "BDP_RoyalContractInvalidCaster";
            return HasContract(caster) ? "BDP_RoyalContractAlreadyBound" : null;
        }

        /// <summary>人物级资格不依赖皇室或腺体基因是否仍存在。</summary>
        private bool HasContract(Pawn pawn) =>
            pawn.health.hediffSet.HasHediff(Props.contractHediff);

        /// <summary>一对一且目标须为另一名活着的殖民者，已有有效腺体不重复授予。</summary>
        private string TargetProblem(Pawn target)
        {
            if (target == null || target == parent.pawn || target.Dead || !target.IsColonist
                || target.IsQuestLodger()
                || target.IsSlave || target.IsPrisoner || target.genes == null)
                return "BDP_RoyalContractInvalidTarget";
            if (HasContract(target)) return "BDP_RoyalContractTargetBound";
            Gene existing = target.genes.GetGene(Props.glandGene);
            return existing != null && !existing.Active ? "BDP_RoyalContractInactiveGland" : null;
        }

        /// <summary>选择与实际施放都走相同校验，防止等待期间人物状态改变。</summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            string problem = CasterProblem() ?? TargetProblem(target.Pawn);
            if (problem == null) return base.Valid(target, throwMessages);
            if (throwMessages) Messages.Message(problem.Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        /// <summary>原版施放入口的效果准入。</summary>
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => Valid(target);

        /// <summary>经原版能力确认流程清楚说明一次性与死亡后果。</summary>
        public override Window ConfirmationDialog(LocalTargetInfo target, Action confirmAction) =>
            Dialog_MessageBox.CreateConfirmation("BDP_RoyalContractConfirm".Translate(
                parent.pawn.LabelShortCap, target.Pawn.LabelShortCap), confirmAction);

        /// <summary>成功建立双方关系才占用资格；异常不吞掉，失败撤回本次新增内容。</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!Valid(target, true)) return;
            Pawn caster = parent.pawn;
            Pawn recipient = target.Pawn;
            var royalRecord = (Hediff_RoyalContract)HediffMaker.MakeHediff(Props.contractHediff, caster);
            var recipientRecord = (Hediff_RoyalContract)HediffMaker.MakeHediff(Props.contractHediff, recipient);
            royalRecord.partner = recipient;
            royalRecord.royalSigner = true;
            recipientRecord.partner = caster;
            Gene addedGene = null;
            bool committed = false;
            try
            {
                if (recipient.genes.GetGene(Props.glandGene) == null)
                    addedGene = recipient.genes.AddGene(Props.glandGene, false);
                // 用原版授予结果确认腺体有效，不直接写入资源值或接管遗传。
                if (recipient.genes.GetGene(Props.glandGene)?.Active != true)
                {
                    Messages.Message("BDP_RoyalContractInactiveGland".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                caster.health.AddHediff(royalRecord);
                recipient.health.AddHediff(recipientRecord);
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    if (caster.health.hediffSet.hediffs.Contains(royalRecord)) caster.health.RemoveHediff(royalRecord);
                    if (recipient.health.hediffSet.hediffs.Contains(recipientRecord)) recipient.health.RemoveHediff(recipientRecord);
                    if (addedGene != null) recipient.genes.RemoveGene(addedGene);
                }
            }
            base.Apply(target, dest);
            Messages.Message("BDP_RoyalContractEstablished".Translate(caster.LabelShortCap,
                recipient.LabelShortCap), recipient, MessageTypeDefOf.PositiveEvent);
        }
    }
}
