using System;
using System.Runtime.CompilerServices;
using BDP.Core.Semantics;
using BDP.Core.Projectiles.Interaction;
using System.Reflection;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// 原版伤害入口完成后的中性结果。
    /// 它只描述“伤害入口最终发生了什么”，不判断具体是哪一种护盾业务。
    /// </summary>
    public enum DamageResolutionOutcome
    {
        /// <summary>
        /// 当前调用尚未完成。
        /// </summary>
        NotResolved,

        /// <summary>
        /// 原版在进入伤害处理前就因 0 伤害等原因结束。
        /// </summary>
        NoDamage,

        /// <summary>
        /// 伤害前拦截器或护盾吸收了本次伤害。
        /// </summary>
        ShieldBlocked,

        /// <summary>
        /// 模块主动取消了本次伤害，但目标已通过护盾裁决。
        /// </summary>
        ModuleIntercepted,

        /// <summary>
        /// 伤害入口正常完成，后续反馈仍由原版结果决定。
        /// </summary>
        DamageProcessed
    }

    /// <summary>
    /// 一次 Thing.TakeDamage（目标承伤）调用的结果快照。
    /// </summary>
    public sealed class DamageResolution
    {
        /// <summary>
        /// 本次结果对应的目标 Thing。
        /// </summary>
        public Thing TargetThing { get; internal set; }

        /// <summary>
        /// 本次承伤调用的原版结果。
        /// </summary>
        public DamageWorker.DamageResult DamageResult { get; internal set; }

        /// <summary>该结果刚关联的原版日志，只有当前调用消费，不建立历史表。</summary>
        public LogEntry_DamageResult BattleLogEntry { get; internal set; }

        /// <summary>
        /// 当前结果分类。
        /// </summary>
        public DamageResolutionOutcome Outcome { get; internal set; }

        /// <summary>
        /// 是否是前置护盾/拦截器吸收。
        /// </summary>
        public bool IsShieldBlocked
        {
            get { return Outcome == DamageResolutionOutcome.ShieldBlocked; }
        }

        /// <summary>
        /// 是否已进入原版伤害工作器。
        /// </summary>
        public bool IsDamageProcessed
        {
            get { return Outcome == DamageResolutionOutcome.DamageProcessed; }
        }
    }

    /// <summary>只有明确请求才捕获结果；普通嵌套调用只建立空边界，禁止继承外层效果。</summary>
    public static class DamageResolutionRuntime
    {
        /// <summary>下一次预定承伤请求；请求只允许被入口消费一次。</summary>
        [ThreadStatic] private static Request pendingRequest;
        /// <summary>当前真实承伤或护盾探针的边界。</summary>
        [ThreadStatic] private static Capture currentCapture;
        /// <summary>按真实原版结果对象短暂关联日志；弱键不建立历史人物表。</summary>
        private static readonly ConditionalWeakTable<DamageWorker.DamageResult, DamageResolution> completedResults =
            new ConditionalWeakTable<DamageWorker.DamageResult, DamageResolution>();

        /// <summary>显式预定单个目标、单份伤害；调用者必须紧接着进入原版承伤入口。</summary>
        public static Request BeginRequest(Thing targetThing, DamageInfo damageInfo,
            ISemanticContext semanticContext, ProjectileInteractionPolicy interactionPolicy,
            ExplosionImpactDispatchContext impactContext)
        {
            Request request = new Request(targetThing, damageInfo, semanticContext, interactionPolicy, impactContext, pendingRequest);
            pendingRequest = request;
            return request;
        }

        /// <summary>由已有 BDP 来源把单份伤害落回原版，并直接取得本次结果。</summary>
        public static DamageWorker.DamageResult ApplyDamage(Thing targetThing, DamageInfo damageInfo,
            out DamageResolution resolution, ISemanticContext semanticContext = null,
            ProjectileInteractionPolicy interactionPolicy = null, ExplosionImpactDispatchContext impactContext = null)
        {
            resolution = null;
            using (Request request = BeginRequest(targetThing, damageInfo, semanticContext, interactionPolicy, impactContext))
            {
                DamageWorker.DamageResult result = targetThing.TakeDamage(damageInfo);
                resolution = request.Resolution;
                return result;
            }
        }

        /// <summary>创建模块主动取消真实伤害的结果。</summary>
        public static DamageResolution CreateModuleInterception(Thing targetThing)
        {
            return new DamageResolution { TargetThing = targetThing, Outcome = DamageResolutionOutcome.ModuleIntercepted };
        }

        /// <summary>创建外部投射物拦截或护盾已挡住的结果。</summary>
        public static DamageResolution CreateProjectileInterception(Thing targetThing)
        {
            return new DamageResolution { TargetThing = targetThing, Outcome = DamageResolutionOutcome.ShieldBlocked };
        }

        /// <summary>只调用实际类型的原版伤害前裁决；不产生真实伤害，回调中的独立伤害仍隔离。</summary>
        public static bool TryProbeDamageInterception(Thing targetThing, ref DamageInfo damageInfo, out bool absorbed,
            ISemanticContext semanticContext = null, ProjectileInteractionPolicy interactionPolicy = null,
            ExplosionImpactDispatchContext impactContext = null)
        {
            absorbed = false;
            if (targetThing == null) return false;
            MethodInfo method = ResolvePreApplyDamageMethod(targetThing.GetType());
            if (method == null) return false;
            using (Capture scope = new Capture(targetThing, damageInfo, null, semanticContext, interactionPolicy, impactContext))
            {
                object[] arguments = { damageInfo, false };
                method.Invoke(targetThing, arguments);
                if (arguments[0] is DamageInfo updated) damageInfo = updated;
                absorbed = arguments[1] is bool value && value;
            }
            return true;
        }

        /// <summary>原版承伤入口只领取同一目标、同份伤害的未消费请求；普通外层不分配。</summary>
        internal static Capture Begin(Thing targetThing, DamageInfo damageInfo)
        {
            Request request = pendingRequest;
            if (request == null || request.Claimed || request.TargetThing != targetThing || !request.DamageInfo.Equals(damageInfo))
                request = null;
            if (request != null) request.Claimed = true;
            if (request == null && currentCapture == null && SemanticRuntimeScope.Current == null
                && ProjectileInteractionPolicyScope.Current == null && ExplosionImpactRuntimeScope.Current == null)
                return null;
            return new Capture(targetThing, damageInfo, request, request?.SemanticContext, request?.InteractionPolicy, request?.ImpactContext);
        }

        /// <summary>只登记最内层同目标真实请求；不能穿过独立嵌套边界污染外层。</summary>
        public static void MarkAbsorbed(Thing targetThing)
        {
            if (currentCapture?.Request != null && currentCapture.TargetThing == targetThing)
                currentCapture.Absorbed = true;
        }

        /// <summary>读取原版伤害前最终吸收值。</summary>
        internal static void ObservePreApplyDamage(Thing targetThing, bool absorbed)
        {
            if (absorbed) MarkAbsorbed(targetThing);
        }

        /// <summary>完成当前显式请求，结果直接归还请求对象，不保存全局最近结果。</summary>
        internal static void Complete(Capture capture, DamageWorker.DamageResult damageResult, bool completedNormally)
        {
            if (capture == null) return;
            if (capture.Request != null)
            {
                DamageResolution resolution = capture.Request.Resolution;
                resolution.DamageResult = damageResult;
                resolution.Outcome = !capture.HasPositiveDamage ? DamageResolutionOutcome.NoDamage
                    : capture.Absorbed ? DamageResolutionOutcome.ShieldBlocked
                    : completedNormally ? DamageResolutionOutcome.DamageProcessed : DamageResolutionOutcome.NotResolved;
                if (damageResult != null)
                {
                    completedResults.Remove(damageResult);
                    completedResults.Add(damageResult, resolution);
                }
            }
            capture.Dispose();
        }

        /// <summary>异常退出只清理本次边界；保留原异常和未完成分类。</summary>
        internal static void Abort(Capture capture) { capture?.Dispose(); }

        /// <summary>只有已登记的同一个 BDP 结果才能取得日志，关联后即移除短暂登记。</summary>
        internal static void AssociateLog(DamageWorker.DamageResult result, LogEntry_DamageResult entry)
        {
            if (result != null && entry != null && completedResults.TryGetValue(result, out DamageResolution resolution))
            {
                resolution.BattleLogEntry = entry;
                completedResults.Remove(result);
            }
        }

        /// <summary>从目标实际类型解析原版受保护的伤害前覆写。</summary>
        private static MethodInfo ResolvePreApplyDamageMethod(Type targetType)
        {
            while (targetType != null)
            {
                MethodInfo method = targetType.GetMethod("PreApplyDamage", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(DamageInfo).MakeByRefType(), typeof(bool).MakeByRefType() }, null);
                if (method != null) return method;
                targetType = targetType.BaseType;
            }
            return null;
        }

        /// <summary>调用者持有的单次请求；同目标独立嵌套也不能二次领取。</summary>
        public sealed class Request : IDisposable
        {
            /// <summary>预定目标。</summary>
            internal readonly Thing TargetThing;
            /// <summary>进入入口前的实际伤害。</summary>
            internal readonly DamageInfo DamageInfo;
            /// <summary>本次来源语义。</summary>
            internal readonly ISemanticContext SemanticContext;
            /// <summary>本次盾交互策略。</summary>
            internal readonly ProjectileInteractionPolicy InteractionPolicy;
            /// <summary>本次范围反馈配置。</summary>
            internal readonly ExplosionImpactDispatchContext ImpactContext;
            /// <summary>调用者外层预定请求。</summary>
            private readonly Request previous;
            /// <summary>是否已领取。</summary>
            internal bool Claimed;
            /// <summary>保证退出幂等。</summary>
            private bool disposed;
            /// <summary>只属于本次请求的结果。</summary>
            public DamageResolution Resolution { get; }

            /// <summary>冻结本次请求数据。</summary>
            internal Request(Thing target, DamageInfo info, ISemanticContext semantic,
                ProjectileInteractionPolicy policy, ExplosionImpactDispatchContext impact, Request previous)
            {
                TargetThing = target; DamageInfo = info; SemanticContext = semantic;
                InteractionPolicy = policy; ImpactContext = impact; this.previous = previous;
                Resolution = new DamageResolution { TargetThing = target, Outcome = DamageResolutionOutcome.NotResolved };
            }

            /// <summary>恢复外层预定请求，不延长结果或目标的全局生命周期。</summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (pendingRequest == this) pendingRequest = previous;
            }
        }

        /// <summary>原版真实承伤边界；无请求的嵌套边界不创建结果，只屏蔽并恢复外层。</summary>
        public sealed class Capture : IDisposable
        {
            /// <summary>所属显式请求；独立嵌套或探针为空。</summary>
            internal readonly Request Request;
            /// <summary>本次目标。</summary>
            internal readonly Thing TargetThing;
            /// <summary>起始是否正伤害。</summary>
            internal readonly bool HasPositiveDamage;
            /// <summary>本次是否吸收。</summary>
            internal bool Absorbed;
            /// <summary>外层真实调用。</summary>
            private readonly Capture previous;
            /// <summary>本次语义边界。</summary>
            private readonly IDisposable semanticScope;
            /// <summary>本次盾策略边界。</summary>
            private readonly IDisposable interactionScope;
            /// <summary>本次范围反馈边界。</summary>
            private readonly IDisposable impactScope;
            /// <summary>清理幂等标志。</summary>
            private bool disposed;

            /// <summary>空上下文也压入隔离边界，防止原版或第三方同步独立伤害继承来源。</summary>
            internal Capture(Thing target, DamageInfo info, Request request, ISemanticContext semantic,
                ProjectileInteractionPolicy policy, ExplosionImpactDispatchContext impact)
            {
                TargetThing = target; HasPositiveDamage = info.Amount > 0f; Request = request;
                previous = currentCapture; currentCapture = this;
                semanticScope = SemanticRuntimeScope.Push(semantic);
                interactionScope = ProjectileInteractionPolicyScope.Push(policy);
                impactScope = ExplosionImpactRuntimeScope.Push(impact);
            }

            /// <summary>正常与异常出口共用；重复退出不破坏外层调用。</summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                impactScope.Dispose(); interactionScope.Dispose(); semanticScope.Dispose();
                if (currentCapture == this) currentCapture = previous;
            }
        }
    }
}
