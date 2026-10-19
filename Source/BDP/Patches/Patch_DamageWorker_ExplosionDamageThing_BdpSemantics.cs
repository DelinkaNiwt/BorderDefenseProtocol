using System;
using System.Linq;
using System.Reflection.Emit;
using BDP.Support.Diagnostics;
using BDP.Core.CombatLog;
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
    /// 它把实例绑定的来源传给单次真实承伤，并收集本目标的处理结果。
    /// </summary>
    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    /// <summary>
    /// 爆炸逐目标伤害前的语义回灌补丁。
    /// </summary>
    public static class Patch_DamageWorker_ExplosionDamageThing_BdpSemantics
    {
        /// <summary>仅保存当前爆炸逐目标结果归属；不暴露攻击语义或盾策略。</summary>
        [ThreadStatic] private static ImpactDamageScopeState currentScope;

        /// <summary>
        /// 在原版逐目标处理前准备结果归属；攻击来源只在实际承伤入口恢复。
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
            __state = null;
            if (impactContext == null && BdpDamageSemanticBridge.GetExplosionContext(explosion) == null)
                return true;
            __state = new ImpactDamageScopeState(explosion, impactContext, t, cell);

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
                if (t is Pawn && __state.Resolution != null)
                {
                    // 原版方法被取消时不会产生逐目标条目，只在已接受的真实角色目标补一条。
                    __state.BattleLogEntry = BattleLogEntryFactoryAccess.CreateExplosionImpact(
                        impactContext.Instigator, t, explosion.weapon, explosion.projectile,
                        impactContext.DamageDef, impactContext.SemanticContext);
                    Find.BattleLog.Add(__state.BattleLogEntry);
                }
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

            DamageResolution resolution = __state.Resolution;
            LogEntry_DamageResult log = __state.BattleLogEntry ?? resolution?.BattleLogEntry;
            BattleLogEntryFactoryAccess.CompleteDamageResult(log, resolution);
            if (resolution == null || resolution.IsShieldBlocked)
            {
                return;
            }

            if (resolution.Outcome != DamageResolutionOutcome.DamageProcessed
                && resolution.Outcome != DamageResolutionOutcome.ModuleIntercepted)
            {
                return;
            }

            ExecuteAreaEffects(__state.ImpactContext, __state.TargetThing, __state.TargetCell, log);
        }

        /// <summary>
        /// 把当前范围爆炸逐目标效果交给已注册执行器。
        /// </summary>
        private static void ExecuteAreaEffects(
            ExplosionImpactDispatchContext impactContext,
            Thing targetThing,
            IntVec3 targetCell,
            LogEntry_DamageResult battleLogEntry)
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
                    BattleLogEntry = battleLogEntry,
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
                out absorbed, impactContext.SemanticContext, impactContext.InteractionPolicy, impactContext))
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

        /// <summary>只在真实 TakeDamage（承伤）调用点预定请求，避免日志或扩展回调提前消费来源。</summary>
        [HarmonyTranspiler]
        public static System.Collections.Generic.IEnumerable<CodeInstruction> Transpiler(
            System.Collections.Generic.IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var takeDamage = AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage));
            if (code.Count(item => item.Calls(takeDamage)) != 1)
            {
                BdpDiagnostics.Once("damage.boundary.explosion_anchor_missing",
                    "BDP_DamageBoundaryPatchUnavailable".Translate());
                return code;
            }
            int index = code.FindIndex(item => item.Calls(takeDamage));
            CodeInstruction original = code[index];
            CodeInstruction loadExplosion = new CodeInstruction(OpCodes.Ldarg_1);
            loadExplosion.labels.AddRange(original.labels);
            loadExplosion.blocks.AddRange(original.blocks);
            code[index] = loadExplosion;
            code.Insert(index + 1, new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(Patch_DamageWorker_ExplosionDamageThing_BdpSemantics), nameof(ApplyExplosionDamage))));
            return code;
        }

        /// <summary>保留普通爆炸原调用；BDP 爆炸以实例绑定的上下文恢复单个真实承伤。</summary>
        public static DamageWorker.DamageResult ApplyExplosionDamage(Thing targetThing, DamageInfo damageInfo, Explosion explosion)
        {
            ExplosionImpactDispatchContext context = BdpDamageSemanticBridge.GetExplosionImpactContext(explosion);
            ISemanticContext semantic = BdpDamageSemanticBridge.GetExplosionContext(explosion);
            if (context == null && semantic == null)
                return targetThing.TakeDamage(damageInfo);
            DamageResolution resolution;
            DamageWorker.DamageResult result = DamageResolutionRuntime.ApplyDamage(targetThing, damageInfo, out resolution,
                context?.SemanticContext ?? semantic, context?.InteractionPolicy, context);
            if (currentScope != null && currentScope.Explosion == explosion && currentScope.TargetThing == targetThing)
                currentScope.Resolution = resolution;
            return result;
        }

        /// <summary>
        /// 无论正常结束还是异常退出，都清理本目标的临时结果归属。
        /// </summary>
        [HarmonyFinalizer]
        public static Exception Finalizer(ImpactDamageScopeState __state, Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }

        /// <summary>
        /// 把本次逐目标结果归属作为一个可回收状态传递给 Harmony。
        /// </summary>
        public sealed class ImpactDamageScopeState : IDisposable
        {
            /// <summary>实际所属爆炸。</summary>
            public Explosion Explosion { get; }
            /// <summary>外层逐目标请求。</summary>
            private readonly ImpactDamageScopeState previous;
            /// <summary>保证异常出口与正常出口清理幂等。</summary>
            private bool disposed;

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

            /// <summary>原版范围伤害被取消时补出的本次目标条目。</summary>
            public LogEntry_DamageResult BattleLogEntry { get; set; }

            /// <summary>构造仅存活于本次逐目标调用的作用域状态。</summary>
            public ImpactDamageScopeState(
                Explosion explosion,
                ExplosionImpactDispatchContext impactContext,
                Thing targetThing,
                IntVec3 targetCell)
            {
                Explosion = explosion;
                previous = currentScope;
                currentScope = this;
                ImpactContext = impactContext;
                TargetThing = targetThing;
                TargetCell = targetCell;
            }

            /// <summary>退出本次目标调用，清理临时引用并恢复原作用域。</summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                BattleLogEntry = null;
                if (currentScope == this) currentScope = previous;
            }
        }
    }
}
