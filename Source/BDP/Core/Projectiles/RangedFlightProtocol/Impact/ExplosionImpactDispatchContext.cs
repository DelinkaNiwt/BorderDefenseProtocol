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
    /// 在原版创建边界绑定实例，在单次真实承伤边界恢复 BDP 范围上下文。
    /// </summary>
    public static class ExplosionImpactRuntimeScope
    {
        /// <summary>当前预定的单个爆炸创建请求；不会作为伤害作用域被消费者读取。</summary>
        [System.ThreadStatic]
        private static CreationRequest pendingCreation;

        /// <summary>预定紧接着进入的原版爆炸创建调用；请求不向生成回调暴露。</summary>
        public static System.IDisposable PrepareExplosion(Map map, IntVec3 center,
            ExplosionImpactDispatchContext context, ISemanticContext semanticContext)
        {
            CreationRequest request = new CreationRequest(map, center, context, semanticContext, pendingCreation);
            pendingCreation = request;
            return request;
        }

        /// <summary>当前原版创建调用；独立嵌套创建会压入空请求，不能抢走外层来源。</summary>
        [System.ThreadStatic] private static CreationScope currentCreation;

        /// <summary>在生成回调发生前领取请求；普通外层创建不分配状态。</summary>
        public static System.IDisposable EnterCreation(Map map, IntVec3 center)
        {
            CreationRequest request = pendingCreation;
            if (request == null || request.Claimed || map != request.Map || center != request.Center)
                request = null;
            if (request != null) request.Claimed = true;
            if (request == null && currentCreation == null) return null;
            return new CreationScope(request);
        }

        /// <summary>只在原版唯一启动调用点绑定实际创建对象，不按位置猜测对象身份。</summary>
        public static void BindCreatedExplosion(Explosion explosion)
        {
            CreationRequest request = currentCreation?.Request;
            if (explosion == null || request == null || request.Bound) return;
            request.Bound = true;
            BdpDamageSemanticBridge.AssignExplosionImpactContext(explosion, request.Context);
            BdpDamageSemanticBridge.AssignExplosionContext(explosion, request.SemanticContext);
        }

        /// <summary>保存创建调用的实际归属，并在正常或异常退出时恢复外层。</summary>
        private sealed class CreationScope : System.IDisposable
        {
            /// <summary>本次显式请求；独立嵌套为空。</summary>
            internal readonly CreationRequest Request;
            /// <summary>外层创建调用。</summary>
            private readonly CreationScope previous;
            /// <summary>保证清理幂等。</summary>
            private bool disposed;
            /// <summary>建立真实创建调用边界。</summary>
            internal CreationScope(CreationRequest request)
            {
                Request = request; previous = currentCreation; currentCreation = this;
            }
            /// <summary>恢复外层创建调用，不影响其已绑定实例。</summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (currentCreation == this) currentCreation = previous;
            }
        }

        /// <summary>短暂的创建请求，不覆盖实际承伤上下文。</summary>
        private sealed class CreationRequest : System.IDisposable
        {
            /// <summary>预定地图。</summary>
            internal readonly Map Map;
            /// <summary>预定中心。</summary>
            internal readonly IntVec3 Center;
            /// <summary>范围配置。</summary>
            internal readonly ExplosionImpactDispatchContext Context;
            /// <summary>冻结来源。</summary>
            internal readonly ISemanticContext SemanticContext;
            /// <summary>已经领取。</summary>
            internal bool Claimed;
            /// <summary>已经绑定到该调用实际产生的爆炸。</summary>
            internal bool Bound;
            /// <summary>外层创建请求。</summary>
            private readonly CreationRequest previous;
            /// <summary>保证退出幂等。</summary>
            private bool disposed;

            /// <summary>保存本次预定，不给同步扩展回调暴露伤害效果。</summary>
            internal CreationRequest(Map map, IntVec3 center, ExplosionImpactDispatchContext context,
                ISemanticContext semanticContext, CreationRequest previous)
            {
                Map = map; Center = center; Context = context;
                SemanticContext = semanticContext; this.previous = previous;
            }

            /// <summary>恢复外层预定，取消尚未领取的本次请求。</summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (pendingCreation == this) pendingCreation = previous;
            }
        }

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
            /// <summary>进入本次承伤前的范围上下文。</summary>
            private readonly ExplosionImpactDispatchContext previous;

            /// <summary>保存外层范围上下文。</summary>
            public Scope(ExplosionImpactDispatchContext previous)
            {
                this.previous = previous;
            }

            /// <summary>恢复进入前的范围上下文，由所属承伤边界保证只退出一次。</summary>
            public void Dispose()
            {
                current = previous;
            }
        }
    }
}
