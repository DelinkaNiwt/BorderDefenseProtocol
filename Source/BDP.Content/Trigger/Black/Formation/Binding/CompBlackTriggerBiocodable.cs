using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>在原版生物编码生命周期上增加适格判断和全局索引维护。</summary>
    public sealed class CompBlackTriggerBiocodable : CompBiocodable
    {
        /// <summary>满足适格且没有绑定其它黑触发器时才编码。</summary>
        public override void CodeFor(Pawn pawn)
        {
            if (!BlackTriggerCompatibilityEvaluator.Evaluate(parent, pawn).Accepted) return;
            base.CodeFor(pawn);
            if (Biocoded && CodedPawn == pawn) BlackTriggerBindingIndex.Register(pawn, this);
        }

        /// <summary>解绑时同步移除全局索引。</summary>
        public override void UnCode()
        {
            BlackTriggerBindingIndex.Unregister(this);
            base.UnCode();
        }

        /// <summary>读档后由真实编码状态恢复索引。</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit && Biocoded && CodedPawn != null) BlackTriggerBindingIndex.Register(CodedPawn, this);
        }

        /// <summary>物品销毁时清理临时特性与索引。</summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Pawn pawn = CodedPawn;
            BlackTriggerInheritedTraitService.RemoveFromSource(pawn, parent);
            BlackTriggerBindingIndex.Unregister(this);
            base.PostDestroy(mode, previousMap);
        }

        /// <summary>装备者死亡时按设计解除绑定。</summary>
        public override void Notify_WearerDied()
        {
            UnCode();
            base.Notify_WearerDied();
        }
    }
}
