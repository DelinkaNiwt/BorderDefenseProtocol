using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Model;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>仅约束玩家制造入口；装备解析、战利品和剧情生成不检查研究。</summary>
    public static class ChipManufacturingResearch
    {
        /// <summary>所有配置的研究均须完成；未配置要求时开放，无效引用不放行。</summary>
        public static bool AreFinished(IEnumerable<ResearchProjectDef> prerequisites)
        {
            if (prerequisites != null)
            {
                foreach (ResearchProjectDef research in prerequisites)
                {
                    if (research == null || !research.IsFinished)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>各功能仍开放制造且组合研究全部完成时放行；旧账单不能绕过撤销的制造入口。</summary>
        public static bool IsUnlocked(ChipCombinationResolution resolution)
        {
            if (resolution?.FunctionDefs == null)
            {
                return false;
            }
            foreach (var function in resolution.FunctionDefs)
            {
                if (function == null || !function.showInManufacturing
                    || !AreFinished(function.researchPrerequisites))
                {
                    return false;
                }
            }
            return resolution.ArmamentForm == null
                || AreFinished(resolution.ArmamentForm.researchPrerequisites);
        }
    }
}
