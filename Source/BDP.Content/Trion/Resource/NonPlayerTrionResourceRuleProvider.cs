using BDP.Core.Trion;
using BDP.Content.Trion.Units;
using Verse;

namespace BDP.Content.Trion.Resource
{
    /// <summary>非玩家派系人物使用隐藏的生命资源，不依赖外交或当前控制状态。</summary>
    public sealed class NonPlayerTrionResourceRuleProvider : ITrionResourceRuleProvider
    {
        /// <summary>无派系也采用简化规则；玩家人物与建筑沿用普通资源规则。</summary>
        public TrionResourceRules GetRules(Thing owner)
        {
            Pawn pawn = owner as Pawn;
            // 玩家人物失控、远行或处于敌对精神状态，仍需承担正常资源成本。
            // 固定款型不锁定生命容量；玩家功能费用与非玩家伤口损耗仍遵守既有规则。
            if (pawn?.TryGetComp<CompTrionUnit>() != null)
            {
                bool player = pawn.Faction?.IsPlayer == true;
                return new TrionResourceRules(player, false, player);
            }
            return pawn != null && pawn.Faction?.IsPlayer != true
                ? new TrionResourceRules(false, false, false)
                : TrionResourceRules.Default;
        }
    }
}
