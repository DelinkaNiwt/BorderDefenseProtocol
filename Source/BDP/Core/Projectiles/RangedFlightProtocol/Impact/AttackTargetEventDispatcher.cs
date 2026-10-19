using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// 攻击目标事件的统一额外效果派发器。
    /// </summary>
    public static class AttackTargetEventDispatcher
    {
        /// <summary>
        /// 将一次攻击目标事件派发给匹配的额外效果执行器。
        /// </summary>
        /// <remarks>
        /// 该方法不建立唯一目标集合，也不按 TargetThing（目标对象）去重。
        /// 调用者每调用一次，就代表生产者产生了一次目标判定。
        /// </remarks>
        /// <param name="targetEvent">待派发的攻击目标事件。</param>
        /// <param name="executionTiming">本次只执行指定时序的额外效果。</param>
        public static bool Dispatch(
            AttackTargetEvent targetEvent,
            ExtraEffectExecutionTiming executionTiming = ExtraEffectExecutionTiming.AfterDamage)
        {
            if (targetEvent == null
                || targetEvent.TargetThing == null
                || targetEvent.ExtraEffects == null)
            {
                return false;
            }

            bool executed = false;
            for (int index = 0; index < targetEvent.ExtraEffects.Count; index++)
            {
                ExtraEffectPlan effectPlan = targetEvent.ExtraEffects[index];
                if (effectPlan == null
                    || effectPlan.ExecutionTiming != executionTiming
                    || !Matches(effectPlan.TargetScope, targetEvent))
                {
                    continue;
                }

                ExtraEffectPlan targetPlan = effectPlan.CloneForTarget(
                    targetEvent.TargetThing,
                    targetEvent.TargetCell);
                if (targetPlan.PartTargetMode == ExtraEffectPartTargetMode.ImpactPart)
                {
                    targetPlan.HitPart = targetEvent.Source == AttackTargetEventSource.DirectImpact
                        ? targetEvent.HitPart : null;
                }
                executed |= ExtraEffectPlanExecutorRegistry.TryExecute(
                    targetPlan,
                    new ExtraEffectExecutionContext
                    {
                        TargetThing = targetEvent.TargetThing,
                        HitPart = targetPlan.HitPart,
                        BattleLogEntry = targetEvent.BattleLogEntry,
                        TargetCell = targetEvent.TargetCell,
                        Map = targetEvent.Map ?? targetEvent.TargetThing.Map,
                        Instigator = targetEvent.Instigator,
                        SourceThing = targetEvent.SourceThing,
                        Projectile = targetEvent.Projectile,
                        SemanticContext = targetEvent.SemanticContext,
                        AttackInstanceId = targetEvent.AttackInstanceId,
                        ResultId = targetEvent.ResultId
                    });
            }

            return executed;
        }

        /// <summary>
        /// 判断额外效果是否订阅当前目标事件来源。
        /// </summary>
        private static bool Matches(
            ExtraEffectTargetScope targetScope,
            AttackTargetEvent targetEvent)
        {
            if (targetScope == ExtraEffectTargetScope.AttackTargetEvents)
            {
                return true;
            }

            if (targetEvent.Source == AttackTargetEventSource.DirectImpact)
            {
                return targetScope == ExtraEffectTargetScope.DirectHitThing
                    || targetScope == ExtraEffectTargetScope.AttackTargetEvents;
            }

            if (targetScope == ExtraEffectTargetScope.VanillaExplosionAffectedThings)
            {
                return true;
            }

            return targetScope == ExtraEffectTargetScope.VanillaExplosionAffectedPawns
                && targetEvent.TargetThing is Verse.Pawn;
        }
    }
}
