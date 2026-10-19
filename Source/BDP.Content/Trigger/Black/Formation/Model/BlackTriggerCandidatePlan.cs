using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>一件完整黑触发器在本次事件中的去重候选计划。</summary>
    public sealed class BlackTriggerCandidatePlan : IExposable
    {
        /// <summary>候选完整物品 Def。</summary>
        public ThingDef ThingDef;

        /// <summary>该物品通过的全部形成路线。</summary>
        public List<BlackTriggerMatchedRouteSnapshot> MatchedRoutes = new List<BlackTriggerMatchedRouteSnapshot>();

        /// <summary>通过路线共同提供的可分配侧重集合。</summary>
        public List<BlackTriggerEmphasisDef> AvailableEmphases = new List<BlackTriggerEmphasisDef>();

        /// <summary>最大覆盖算法最终分配给该候选的唯一侧重。</summary>
        public BlackTriggerEmphasisDef AssignedEmphasis;

        /// <summary>命中的来源人物适应特性稳定键，仅供形成数据和开发诊断。</summary>
        public List<string> MatchedTraitKeys = new List<string>();

        /// <summary>命中的来源人物适应能力稳定键，仅供形成数据和开发诊断。</summary>
        public List<string> MatchedAbilityKeys = new List<string>();

        /// <summary>最终形成抽取权重，规则为 1 + 3 × 命中特性与能力数量。</summary>
        public int FormationWeight = BlackTriggerTraitWeightCalculator.BaseWeight;

        /// <summary>ExposeData（存档数据暴露）。</summary>
        public void ExposeData()
        {
            Scribe_Defs.Look(ref ThingDef, "thingDef");
            Scribe_Collections.Look(ref MatchedRoutes, "matchedRoutes", LookMode.Deep);
            Scribe_Collections.Look(ref AvailableEmphases, "availableEmphases", LookMode.Def);
            Scribe_Defs.Look(ref AssignedEmphasis, "assignedEmphasis");
            Scribe_Collections.Look(ref MatchedTraitKeys, "matchedTraitKeys", LookMode.Value);
            Scribe_Collections.Look(ref MatchedAbilityKeys, "matchedAbilityKeys", LookMode.Value);
            Scribe_Values.Look(ref FormationWeight, "formationWeight", BlackTriggerTraitWeightCalculator.BaseWeight);
            if (MatchedRoutes == null) MatchedRoutes = new List<BlackTriggerMatchedRouteSnapshot>();
            if (AvailableEmphases == null) AvailableEmphases = new List<BlackTriggerEmphasisDef>();
            if (MatchedTraitKeys == null) MatchedTraitKeys = new List<string>();
            if (MatchedAbilityKeys == null) MatchedAbilityKeys = new List<string>();
            if (FormationWeight < BlackTriggerTraitWeightCalculator.BaseWeight)
            {
                FormationWeight = BlackTriggerTraitWeightCalculator.BaseWeight;
            }
        }
    }
}
