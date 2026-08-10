using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 已经通过边界校验的攻击执行准备上下文。
    /// 它只承载原始请求、命中的已发布投影和后续执行链需要消费的派生对象。
    /// </summary>
    internal sealed class AttackExecutionPreparedContext
    {
        /// <summary>
        /// 当前原始入口请求。
        /// </summary>
        public AttackExecutionRequest Request { get; set; }

        /// <summary>
        /// 当前请求绑定的会话令牌。
        /// 这是准备上下文对原始请求身份的直接透出。
        /// </summary>
        public AttackSessionToken SessionToken
        {
            get
            {
                return Request != null
                    ? Request.SessionToken
                    : null;
            }
        }

        /// <summary>
        /// 当前请求携带的统一攻击上下文快照。
        /// 这就是执行链读取上游冻结事实的唯一正式主干。
        /// </summary>
        public AttackContextSnapshot AttackContextSnapshot
        {
            get
            {
                return Request != null
                    ? Request.AttackContextSnapshot
                    : null;
            }
        }

        /// <summary>
        /// 当前请求命中的已发布战斗投影。
        /// </summary>
        public TriggerCombatProjectionState Projection { get; set; }

        /// <summary>
        /// 当前请求实际命中的正式结果。
        /// </summary>
        public FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前请求生成出的正式攻击编排。
        /// </summary>
        public AttackExecutionPlan Plan { get; set; }

        /// <summary>
        /// 当前请求从高层计划进一步映射出的运行时动作步。
        /// 它服务真正执行链消费，不回写表达或高层编排。
        /// </summary>
        public IReadOnlyList<AttackRuntimeStep> RuntimeSteps { get; set; }

        /// <summary>
        /// 当前请求运行时绑定的计划执行游标。
        /// 它只服务推进状态，不承担表达真值。
        /// </summary>
        public AttackExecutionCursor Cursor { get; set; }

        /// <summary>
        /// 当前命中的投影版本号。
        /// </summary>
        public int ProjectionVersion
        {
            get
            {
                return SessionToken != null
                    ? SessionToken.ProjectionVersion
                    : 0;
            }
        }

        /// <summary>
        /// 当前命中的已发布表达快照。
        /// 它只作为只读引用透出，不再提供重算语义。
        /// </summary>
        public ExpressionSnapshot Snapshot
        {
            get
            {
                return Projection != null
                    ? Projection.Snapshot
                    : null;
            }
        }

        /// <summary>
        /// 当前投影上的正式结果索引。
        /// </summary>
        public IReadOnlyDictionary<string, FormalExpressionResult> ResultIndex
        {
            get
            {
                return Projection != null
                    ? Projection.ResultIndex
                    : null;
            }
        }

        /// <summary>
        /// 当前投影上的复合结果来源索引。
        /// </summary>
        public IReadOnlyDictionary<string, CompositeExpressionReference> CompositeReferenceIndex
        {
            get
            {
                return Projection != null
                    ? Projection.CompositeReferenceIndex
                    : null;
            }
        }

        /// <summary>
        /// 当前已归一化后的派单意图。
        /// 后续执行器读取这里即可，不需要再自行猜测默认语义。
        /// </summary>
        public AttackDispatchIntent DispatchIntent
        {
            get
            {
                return Request != null
                    ? Request.DispatchIntent
                    : AttackDispatchIntent.ImmediateCast;
            }
        }

        /// <summary>
        /// 当前已归一化后的发起 Pawn。
        /// 这是准备上下文对原始请求的便捷透出。
        /// </summary>
        public Pawn Pawn
        {
            get
            {
                return Request != null
                    ? Request.Pawn
                    : null;
            }
        }

        /// <summary>
        /// 当前已归一化后的目标。
        /// 这是准备上下文对原始请求的便捷透出。
        /// </summary>
        public LocalTargetInfo Target
        {
            get
            {
                return Request != null
                    ? Request.Target
                    : LocalTargetInfo.Invalid;
            }
        }

        /// <summary>
        /// 当前已归一化后的攻击实例标识。
        /// 这是准备上下文对原始请求的便捷透出。
        /// </summary>
        public string AttackInstanceId
        {
            get
            {
                return SessionToken != null
                    ? SessionToken.AttackInstanceId
                    : null;
            }
        }
    }
}
