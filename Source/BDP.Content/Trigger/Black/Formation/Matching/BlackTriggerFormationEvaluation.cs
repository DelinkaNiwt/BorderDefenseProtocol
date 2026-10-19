using System.Collections.Generic;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>完整冻结快照的一次纯形成匹配结果。</summary>
    public sealed class BlackTriggerFormationEvaluation
    {
        /// <summary>参与匹配的来源快照。</summary>
        public BlackTriggerMakerSnapshot Snapshot;

        /// <summary>按 ThingDef 去重后的完整物品候选。</summary>
        public List<BlackTriggerCandidatePlan> CandidatePlans = new List<BlackTriggerCandidatePlan>();

        /// <summary>全部路线的真实通过或失败诊断。</summary>
        public List<BlackTriggerRouteEvaluation> RouteEvaluations = new List<BlackTriggerRouteEvaluation>();

        /// <summary>没有候选时的玩家原因语言键。</summary>
        public string FailureReasonKey;

        /// <summary>是否至少有一件可形成的完整黑触发器。</summary>
        public bool Passed
        {
            get { return CandidatePlans != null && CandidatePlans.Count > 0; }
        }
    }
}
