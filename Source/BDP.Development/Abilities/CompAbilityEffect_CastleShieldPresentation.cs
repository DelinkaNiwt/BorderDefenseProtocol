using BDP.Core.Abilities;
using RimWorld;
using Verse;

namespace BDP.Development.Abilities
{
    /// <summary>护城盾验证表现组件参数；音效与震屏复用原版效果参数。</summary>
    public sealed class CompProperties_CastleShieldPresentation : CompProperties_AbilityEffect
    {
        /// <summary>仅绑定表现，不生成建筑或支付资源。</summary>
        public CompProperties_CastleShieldPresentation()
        {
            compClass = typeof(CompAbilityEffect_CastleShieldPresentation);
        }
    }

    /// <summary>在整批建筑成功生成后沿用升壁尘土；只存在于开发程序集。</summary>
    public sealed class CompAbilityEffect_CastleShieldPresentation : CompAbilityEffect
    {
        /// <summary>原版中心音效播放一次，尘土按已确认目标格播放。</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var batch = parent as BdpBatchAbility;
            if (batch == null) return;
            foreach (IntVec3 cell in batch.CurrentBatchTargets)
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), batch.CurrentBatchMap,
                    Rand.Range(1.5f, 3f), CompAbilityEffect_Wallraise.DustColor);
        }
    }
}
