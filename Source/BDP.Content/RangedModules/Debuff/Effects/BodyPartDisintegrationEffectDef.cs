using System;
using BDP.Content.PlayerLog;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using RimWorld;
using Verse;

namespace BDP.Content.RangedModules.Debuff.Effects
{
    /// <summary>同部位累积崩解标记，满层以原版缺失部位机制移除。</summary>
    public sealed class BodyPartDisintegrationEffectDef : RangedDebuffEffectDef
    {
        /// <summary>从零标记达到崩解所需的同部位命中次数。</summary>
        public int hitsToDisintegrate = 3;

        /// <summary>崩解只消费已经确定的直接命中部位。</summary>
        public override bool RequiresHitPart => true;

        /// <summary>按阈值次数计算等份增量，保证最后一次命中准确达到满层。</summary>
        public float SeverityIncrement() => (hediffDef != null ? hediffDef.maxSeverity : 1f)
            / Math.Max(1, hitsToDisintegrate);

        /// <summary>标记不造成伤口，满层时才移除部位；失效部位不改选其他位置。</summary>
        public override bool Apply(ExtraEffectExecutionContext context)
        {
            Pawn pawn = context?.TargetThing as Pawn;
            BodyPartRecord part = context?.HitPart;
            if (pawn?.health?.hediffSet == null || pawn.Dead || hediffDef == null || part == null
                || !pawn.RaceProps.body.AllParts.Contains(part)
                || pawn.health.hediffSet.PartIsMissing(part)) return false;

            Hediff mark = GetOrAdd(pawn, part);
            float maximum = hediffDef.maxSeverity;
            mark.Severity = Math.Min(maximum, mark.Severity + SeverityIncrement());
            if (maximum - mark.Severity > 0.000001f) return true;

            pawn.health.RemoveHediff(mark);
            pawn.health.AddHediff(HediffDefOf.MissingBodyPart, part);
            if (pawn.health.hediffSet.PartIsMissing(part) && context.BattleLogEntry != null
                && directResultRulePack != null)
            {
                PlayerBattleLogEntryFactory.AppendConfirmedResult(context.BattleLogEntry,
                    directResultRulePack, hediffDef, part, true);
            }
            return true;
        }
    }
}
