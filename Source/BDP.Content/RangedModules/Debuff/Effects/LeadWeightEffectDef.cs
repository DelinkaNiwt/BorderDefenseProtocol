using System;
using BDP.Content.PlayerLog;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using Verse;

namespace BDP.Content.RangedModules.Debuff.Effects
{
    /// <summary>铅块负重效果包：负责叠加并通知自身健康状态组件刷新恢复计时。</summary>
    public sealed class LeadWeightEffectDef : RangedDebuffEffectDef
    {
        /// <summary>直接命中增加的负重严重度。</summary>
        public float severityPerHit = 0.1f;

        /// <summary>爆炸逐目标命中增加的负重严重度。</summary>
        public float severityPerExplosionHit = 0.01f;

        /// <summary>负重作用于全身，不消费部位选择。</summary>
        public override bool RequiresHitPart => false;

        /// <summary>依照真实攻击类型叠加负重，移动曲线与恢复由原版健康状态及组件执行。</summary>
        public override bool Apply(ExtraEffectExecutionContext context)
        {
            Pawn pawn = context?.TargetThing as Pawn;
            if (pawn?.health?.hediffSet == null || hediffDef == null) return false;
            Hediff weight = GetOrAdd(pawn, null);
            float previous = weight.Severity;
            bool explosion = context.Projectile?.def?.projectile?.explosionRadius > 0f;
            float increment = explosion ? severityPerExplosionHit : severityPerHit;
            weight.Severity = Math.Min(hediffDef.maxSeverity, previous + Math.Max(0f, increment));
            weight.TryGetComp<HediffComp_StackingRecovery>()?.NotifyEffectiveHit();
            if (weight.Severity > previous && context.BattleLogEntry != null && directResultRulePack != null)
            {
                PlayerBattleLogEntryFactory.AppendConfirmedResult(context.BattleLogEntry,
                    directResultRulePack, hediffDef, null);
            }
            return true;
        }
    }
}
