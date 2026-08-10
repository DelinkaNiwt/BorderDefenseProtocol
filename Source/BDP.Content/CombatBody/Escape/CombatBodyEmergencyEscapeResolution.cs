using BDP.Core.Expressions;
using System.Collections.Generic;
using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离解析结果。
    /// </summary>
    public sealed class CombatBodyEmergencyEscapeResolution : IExposable
    {
        /// <summary>
        /// 当前是否可用。
        /// </summary>
        public bool IsAvailable;

        /// <summary>
        /// 命中结果对应的全部来源追踪。
        /// 它只服务后续一次性芯片消费，不参与紧急脱离判定本身。
        /// </summary>
        public List<ExpressionPublishedSourceReference> SourceReferences;

        /// <summary>
        /// 存读档紧急脱离解析结果。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref IsAvailable, "isAvailable", false);
            Scribe_Collections.Look(ref SourceReferences, "sourceReferences", LookMode.Deep);
        }
    }
}
