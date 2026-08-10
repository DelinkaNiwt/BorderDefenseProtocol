using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击执行入口接收的最小正式请求。
    /// 它只描述“谁要用哪条表达去打谁”，不直接携带运行时执行器。
    /// </summary>
    internal sealed class AttackExecutionRequest
    {
        /// <summary>
        /// 当前正式攻击请求绑定的会话令牌。
        /// 它收口本次请求命中的结果、发布版本和宿主身份。
        /// </summary>
        public AttackSessionToken SessionToken { get; set; }

        /// <summary>
        /// 当前请求携带的统一攻击上下文快照。
        /// 正式执行边界之后只允许继续读取它，不再透出零散碎片上下文。
        /// </summary>
        internal AttackContextSnapshot AttackContextSnapshot { get; set; }

        /// <summary>
        /// 当前正式攻击请求的实例标识。
        /// 它是对 SessionToken 的只读透出，不再单独存储。
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

        /// <summary>
        /// 当前发起执行请求的 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前要执行的正式结果标识。
        /// 它是对 SessionToken 的只读透出，不再单独存储。
        /// </summary>
        public string ResultId
        {
            get
            {
                return SessionToken != null
                    ? SessionToken.ResultId
                    : null;
            }
        }

        /// <summary>
        /// 当前请求命中的已发布战斗投影版本号。
        /// 它是对 SessionToken 的只读透出，不再单独存储。
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
        /// 当前请求所属宿主 Pawn 的 ThingID。
        /// 它是对 SessionToken 的只读透出，不再单独存储。
        /// </summary>
        public string OwnerPawnThingId
        {
            get
            {
                return SessionToken != null
                    ? SessionToken.OwnerPawnThingId
                    : null;
            }
        }

        /// <summary>
        /// 当前请求指向的原版目标。
        /// </summary>
        public LocalTargetInfo Target { get; set; }

        /// <summary>
        /// 当前请求来自哪条正式入口。
        /// </summary>
        public AttackExecutionReason Reason { get; set; }

        /// <summary>
        /// 当前请求要以什么派单方式进入正式执行系统。
        /// Reason 只回答“从哪来”，DispatchIntent 只回答“怎么派单”。
        /// </summary>
        public AttackDispatchIntent DispatchIntent { get; set; }
    }
}
