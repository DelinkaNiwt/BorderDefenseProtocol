using System;
using BDP.Core.Projectiles;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Semantics;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 爆炸对具体目标造成伤害前的最小接线。
    /// 它只负责把挂在 Explosion 上的语义重新压回当前伤害作用域。
    /// </summary>
    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    /// <summary>
    /// 爆炸逐目标伤害前的语义回灌补丁。
    /// </summary>
    public static class Patch_DamageWorker_ExplosionDamageThing_BdpSemantics
    {
        /// <summary>
        /// 爆炸真正准备对某个目标调用 `TakeDamage` 前，把挂在 Explosion 上的语义压回当前线程作用域。
        /// </summary>
        public static bool Prefix(
            Explosion explosion,
            Thing t,
            System.Collections.Generic.List<Thing> damagedThings,
            System.Collections.Generic.List<Thing> ignoredThings,
            IntVec3 cell,
            out ImpactDamageScopeState __state)
        {
            ExplosionImpactDispatchContext impactContext = BdpDamageSemanticBridge.GetExplosionImpactContext(explosion);
            __state = new ImpactDamageScopeState(
                SemanticRuntimeScope.Push(BdpDamageSemanticBridge.GetExplosionContext(explosion)),
                ProjectileInteractionPolicyScope.Push(impactContext != null ? impactContext.InteractionPolicy : null),
                impactContext,
                t,
                cell);

            if (impactContext == null || t == null || t.def == null || damagedThings == null)
            {
                return true;
            }

            // 先复刻原版 ExplosionDamageThing 的目标类别与去重边界。
            if (t.def.category == ThingCategory.Mote
                || t.def.category == ThingCategory.Ethereal
                || damagedThings.Contains(t))
            {
                __state.TargetAccepted = false;
                return false;
            }

            // 普通范围伤害必须让原版方法继续执行。
            // 原版会自己登记 damagedThings；这里提前登记会让原版再次命中去重条件，跳过 TakeDamage。
            if (!impactContext.SuppressCurrentAreaDamage)
            {
                if (ignoredThings != null && ignoredThings.Contains(t))
                {
                    __state.TargetAccepted = false;
                    return true;
                }

                __state.TargetAccepted = true;
                return true;
            }

            // 只有抑制原版范围伤害时，才需要在跳过原版方法前手动保留原版登记边界。
            damagedThings.Add(t);
            if (ignoredThings != null && ignoredThings.Contains(t))
            {
                __state.TargetAccepted = false;
                return false;
            }

            __state.TargetAccepted = true;
            if (impactContext.SuppressCurrentAreaDamage)
            {
                __state.Resolution = ResolveSuppressedAreaImpact(impactContext, t);
            }

            return !impactContext.SuppressCurrentAreaDamage;
        }

        /// <summary>
        /// 原版范围伤害正常完成后，读取它刚刚产生的目标承伤结果。
        /// </summary>
        public static void Postfix(ImpactDamageScopeState __state)
        {
            if (__state == null || !__state.TargetAccepted || __state.ImpactContext == null)
            {
                return;
            }

            DamageResolution resolution = __state.Resolution != null
                ? __state.Resolution
                : DamageResolutionRuntime.ConsumeLast(__state.TargetThing);
            if (resolution == null || resolution.IsShieldBlocked)
            {
                return;
            }

            if (resolution.Outcome != DamageResolutionOutcome.DamageProcessed
                && resolution.Outcome != DamageResolutionOutcome.ModuleIntercepted)
            {
                return;
            }

            ExecuteAreaEffects(__state.ImpactContext, __state.TargetThing, __state.TargetCell);
        }

        /// <summary>
        /// 把当前范围爆炸逐目标效果交给已注册执行器。
        /// </summary>
        private static void ExecuteAreaEffects(
            ExplosionImpactDispatchContext impactContext,
            Thing targetThing,
            IntVec3 targetCell)
        {
            if (impactContext == null || impactContext.ExtraEffects == null)
            {
                return;
            }

            AttackTargetEventDispatcher.Dispatch(
                new AttackTargetEvent
                {
                    Source = AttackTargetEventSource.ProducedTarget,
                    TargetThing = targetThing,
                    TargetCell = targetCell,
                    ExtraEffects = impactContext.ExtraEffects,
                    Map = targetThing.Map,
                    Instigator = impactContext.Instigator,
                    SourceThing = impactContext.SourceThing,
                    Projectile = impactContext.Projectile,
                    SemanticContext = impactContext.SemanticContext,
                    AttackInstanceId = impactContext.AttackInstanceId,
                    ResultId = impactContext.ResultId
                });

            // 只有模块明确要求时，才为“取消真实伤害”的范围目标补回完整 Pawn 反馈。
            // 普通范围伤害的反馈仍由原版 ExplosionDamageThing（爆炸逐目标伤害）及其 DamageWorker（伤害工作器）产生。
            if (impactContext.InterceptedHitFeedback == ImpactHitFeedbackMode.VanillaPawn
                && AppliesHitFeedbackToProducedTarget(impactContext, targetThing)
                && impactContext.Projectile is BdpProjectile bdpProjectile)
            {
                bdpProjectile.ApplySuppressedHitFeedback(
                    targetThing,
                    impactContext.HasHitFeedbackColor
                        ? (UnityEngine.Color?)impactContext.HitFeedbackColor
                        : null,
                    false);
            }
        }

        /// <summary>
        /// 解析“范围伤害被模块取消但仍需尊重伤害前护盾”的目标结果。
        /// </summary>
        private static DamageResolution ResolveSuppressedAreaImpact(
            ExplosionImpactDispatchContext impactContext,
            Thing targetThing)
        {
            if (impactContext == null || targetThing == null)
            {
                return null;
            }

            if ((impactContext.InteractionPolicy != null
                    && impactContext.InteractionPolicy.BypassRegisteredDamageShields)
                || !(impactContext.DamageAmount > 0f))
            {
                return DamageResolutionRuntime.CreateModuleInterception(targetThing);
            }

            bool instigatorGuilty = !(impactContext.Instigator is Pawn pawn) || !pawn.Drafted;
            DamageInfo probeDamageInfo = new DamageInfo(
                impactContext.DamageDef,
                impactContext.DamageAmount,
                impactContext.ArmorPenetration,
                0f,
                impactContext.Instigator,
                null,
                impactContext.SourceThing != null ? impactContext.SourceThing.def : null,
                DamageInfo.SourceCategory.ThingOrUnknown,
                impactContext.IntendedTarget.Thing,
                instigatorGuilty);
            bool absorbed;
            if (!DamageResolutionRuntime.TryProbeDamageInterception(
                targetThing,
                ref probeDamageInfo,
                out absorbed))
            {
                return DamageResolutionRuntime.CreateModuleInterception(targetThing);
            }

            return absorbed
                ? DamageResolutionRuntime.CreateProjectileInterception(targetThing)
                : DamageResolutionRuntime.CreateModuleInterception(targetThing);
        }

        /// <summary>
        /// 判断爆炸生产者产生的当前目标事件是否订阅命中反馈颜色。
        /// </summary>
        private static bool AppliesHitFeedbackToProducedTarget(
            ExplosionImpactDispatchContext impactContext,
            Thing targetThing)
        {
            if (impactContext == null)
            {
                return false;
            }

            switch (impactContext.HitFeedbackTargetScope)
            {
                case ExtraEffectTargetScope.AttackTargetEvents:
                case ExtraEffectTargetScope.VanillaExplosionAffectedThings:
                    return true;
                case ExtraEffectTargetScope.VanillaExplosionAffectedPawns:
                    return targetThing is Pawn;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 无论这次爆炸伤害是正常结束还是异常退出，都把这一小段临时语义作用域弹掉。
        /// </summary>
        [HarmonyFinalizer]
        public static Exception Finalizer(ImpactDamageScopeState __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }

        /// <summary>
        /// 把语义作用域与投射物交互策略作用域作为一个可回收状态传递给 Harmony。
        /// </summary>
        public sealed class ImpactDamageScopeState : IDisposable
        {
            private readonly IDisposable semanticScope;
            private readonly IDisposable interactionScope;

            /// <summary>
            /// 当前范围上下文。
            /// </summary>
            public ExplosionImpactDispatchContext ImpactContext { get; }

            /// <summary>
            /// 当前爆炸目标。
            /// </summary>
            public Thing TargetThing { get; }

            /// <summary>
            /// 当前爆炸目标格。
            /// </summary>
            public IntVec3 TargetCell { get; }

            /// <summary>
            /// 当前目标是否通过原版去重与忽略边界。
            /// </summary>
            public bool TargetAccepted { get; set; }

            /// <summary>
            /// 被模块取消原版范围伤害时提前得到的结果。
            /// </summary>
            public DamageResolution Resolution { get; set; }

            public ImpactDamageScopeState(
                IDisposable semanticScope,
                IDisposable interactionScope,
                ExplosionImpactDispatchContext impactContext,
                Thing targetThing,
                IntVec3 targetCell)
            {
                this.semanticScope = semanticScope;
                this.interactionScope = interactionScope;
                ImpactContext = impactContext;
                TargetThing = targetThing;
                TargetCell = targetCell;
            }

            public void Dispose()
            {
                interactionScope?.Dispose();
                semanticScope?.Dispose();
            }
        }
    }
}
