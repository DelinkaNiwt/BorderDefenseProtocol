using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using Verse;

namespace BDP.Content.RangedModules.Debuff.Effects
{
    /// <summary>远程减益效果包。定义无状态，层数和恢复计时留在目标健康状态中。</summary>
    public class RangedDebuffEffectDef : Def
    {
        /// <summary>此效果包使用的原版健康状态定义。</summary>
        public HediffDef hediffDef;

        /// <summary>本次生效时使用的可空结果文法。</summary>
        public RulePackDef directResultRulePack;

        /// <summary>是否必须取得本次直接命中的身体部位。</summary>
        public virtual bool RequiresHitPart => false;

        /// <summary>消费命中事实并应用本包的业务效果；不选择部位。</summary>
        public virtual bool Apply(ExtraEffectExecutionContext context) => false;

        /// <summary>原版只为首个非抽象Def基类建数据库，因此入口保持可实例化，定义只能使用具体效果子类。</summary>
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (GetType() == typeof(RangedDebuffEffectDef)) yield return "BDP_RangedEffect_ConcreteRequired".Translate();
            if (hediffDef == null) yield return "BDP_RangedEffect_HediffMissing".Translate(defName);
        }

        /// <summary>取得同一健康状态及同一部位的记录，不把不同部位的层数合并。</summary>
        protected Hediff GetOrAdd(Pawn pawn, BodyPartRecord part)
        {
            foreach (Hediff existing in pawn.health.hediffSet.hediffs)
            {
                if (existing.def == hediffDef && existing.Part == part) return existing;
            }

            Hediff created = HediffMaker.MakeHediff(hediffDef, pawn, part);
            pawn.health.AddHediff(created);
            return created;
        }
    }
}
