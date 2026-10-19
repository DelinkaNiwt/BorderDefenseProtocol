using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>Development 诊断使用的单条形成路线求值结果。</summary>
    public sealed class BlackTriggerRouteEvaluation
    {
        /// <summary>被求值的完整黑触发器。</summary>
        public ThingDef ThingDef;

        /// <summary>路线稳定标识。</summary>
        public string RouteId;

        /// <summary>该路线是否通过。</summary>
        public bool Passed;

        /// <summary>命中的正向事实稳定键。</summary>
        public List<string> MatchedFactKeys = new List<string>();

        /// <summary>导致未通过的事实或条件说明。</summary>
        public List<string> FailureDetails = new List<string>();

        /// <summary>路线允许的唯一侧重。</summary>
        public BlackTriggerEmphasisDef Emphasis;
    }
}
