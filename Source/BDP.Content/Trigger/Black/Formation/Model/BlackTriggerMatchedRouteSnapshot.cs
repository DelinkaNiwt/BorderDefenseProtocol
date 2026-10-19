using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>冻结一条已经通过的形成路线及其真实证据。</summary>
    public sealed class BlackTriggerMatchedRouteSnapshot : IExposable
    {
        /// <summary>开发诊断使用的路线标识。</summary>
        public string RouteId;

        /// <summary>命中的正向事实稳定键。</summary>
        public List<string> MatchedFactKeys = new List<string>();

        /// <summary>路线允许分配的唯一侧重。</summary>
        public BlackTriggerEmphasisDef Emphasis;

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref RouteId, "routeId");
            Scribe_Collections.Look(ref MatchedFactKeys, "matchedFactKeys", LookMode.Value);
            Scribe_Defs.Look(ref Emphasis, "emphasis");
            if (MatchedFactKeys == null) MatchedFactKeys = new List<string>();
        }
    }
}
