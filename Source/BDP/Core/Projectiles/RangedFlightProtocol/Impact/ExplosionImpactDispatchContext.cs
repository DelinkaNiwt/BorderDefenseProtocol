using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// 一个范围爆炸实例需要跨原版 Explosion 生命周期保存的 BDP 命中上下文。
    /// </summary>
    public sealed class ExplosionImpactDispatchContext
    {
        /// <summary>
        /// 当前爆炸需要逐目标派发的额外效果。
        /// </summary>
        public IReadOnlyList<ExtraEffectPlan> ExtraEffects { get; set; }

        /// <summary>
        /// 当前爆炸是否抑制该范围伤害。
        /// </summary>
        public bool SuppressCurrentAreaDamage { get; set; }

        /// <summary>
        /// 当前爆炸是否带有可选命中反馈颜色。
        /// </summary>
        public bool HasHitFeedbackColor { get; set; }

        /// <summary>
        /// 当前爆炸逐目标命中反馈颜色。
        /// </summary>
        public UnityEngine.Color HitFeedbackColor { get; set; } = UnityEngine.Color.white;

        /// <summary>
        /// 当前爆炸命中反馈颜色订阅的目标范围。
        /// </summary>
        public ExtraEffectTargetScope HitFeedbackTargetScope { get; set; } = ExtraEffectTargetScope.DirectHitThing;

        /// <summary>
        /// 当前范围伤害被模块拦截后是否补回原版 Pawn 受击反馈。
        /// </summary>
        public ImpactHitFeedbackMode InterceptedHitFeedback { get; set; } = ImpactHitFeedbackMode.None;

        /// <summary>
        /// 当前爆炸的施加者。
        /// </summary>
        public Thing Instigator { get; set; }

        /// <summary>
        /// 当前攻击来源宿主。
        /// </summary>
        public Thing SourceThing { get; set; }

        /// <summary>
        /// 当前 BDP 投射物。
        /// </summary>
        public Projectile Projectile { get; set; }

        /// <summary>
        /// 当前地图。
        /// </summary>
        public Map Map { get; set; }

        /// <summary>
        /// 当前攻击语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前正式表达结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前范围爆炸的视觉策略。
        /// </summary>
        public ExplosionPresentationPolicy PresentationPolicy { get; set; }

        /// <summary>
        /// 当前范围伤害进入伤害前护盾时读取的冻结交互策略。
        /// </summary>
        public ProjectileInteractionPolicy InteractionPolicy { get; set; }

        /// <summary>
        /// 当前范围生产者使用的伤害类型，供“取消伤害但仍检查护盾”的探针使用。
        /// </summary>
        public DamageDef DamageDef { get; set; }

        /// <summary>
        /// 当前范围生产者使用的伤害量，供护盾探针使用。
        /// </summary>
        public float DamageAmount { get; set; }

        /// <summary>
        /// 当前范围生产者使用的护甲穿透，供护盾探针使用。
        /// </summary>
        public float ArmorPenetration { get; set; }

        /// <summary>
        /// 当前范围生产者的意图目标，供护盾探针构造原版 DamageInfo（伤害信息）。
        /// </summary>
        public LocalTargetInfo IntendedTarget { get; set; }
    }

    /// <summary>
    /// 只在当前同步爆炸启动调用栈内保存 BDP 范围上下文。
    /// </summary>
    public static class ExplosionImpactRuntimeScope
    {
        /// <summary>
        /// 当前线程上的范围上下文。
        /// </summary>
        [System.ThreadStatic]
        private static ExplosionImpactDispatchContext current;

        /// <summary>
        /// 读取当前范围上下文。
        /// </summary>
        public static ExplosionImpactDispatchContext Current
        {
            get { return current; }
        }

        /// <summary>
        /// 压入一个范围上下文。
        /// </summary>
        public static System.IDisposable Push(ExplosionImpactDispatchContext context)
        {
            ExplosionImpactDispatchContext previous = current;
            current = context;
            return new Scope(previous);
        }

        /// <summary>
        /// 一次性恢复上一层上下文。
        /// </summary>
        private sealed class Scope : System.IDisposable
        {
            private readonly ExplosionImpactDispatchContext previous;

            public Scope(ExplosionImpactDispatchContext previous)
            {
                this.previous = previous;
            }

            public void Dispose()
            {
                current = previous;
            }
        }
    }
}
