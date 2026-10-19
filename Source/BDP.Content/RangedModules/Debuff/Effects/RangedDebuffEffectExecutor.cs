using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using Verse;

namespace BDP.Content.RangedModules.Debuff.Effects
{
    /// <summary>既有中性效果链到Content效果包的唯一适配器，不识别具体业务种类。</summary>
    public sealed class RangedDebuffEffectExecutor : IExtraEffectPlanExecutor
    {
        /// <summary>远程减益效果包的执行器键。</summary>
        public string EffectKind => "rangedDebuff";

        /// <summary>只解析效果包定义并调用其应用入口。</summary>
        public bool TryExecute(ExtraEffectPlan plan, ExtraEffectExecutionContext context)
        {
            if (plan?.Parameters == null || context == null
                || !plan.Parameters.TryGetValue("effectDef", out string name)) return false;
            if (!plan.Parameters.TryGetValue("targetFilter", out string filter)
                || (filter != nameof(RangedDebuffTargetFilter.AnyThing)
                    && (filter != nameof(RangedDebuffTargetFilter.PawnsOnly) || !(context.TargetThing is Pawn)))) return false;
            RangedDebuffEffectDef effect = DefDatabase<RangedDebuffEffectDef>.GetNamedSilentFail(name);
            if (effect == null)
            {
                Log.Error("BDP_RangedEffect_Missing".Translate(name));
                return false;
            }
            return effect.Apply(context);
        }
    }
}
