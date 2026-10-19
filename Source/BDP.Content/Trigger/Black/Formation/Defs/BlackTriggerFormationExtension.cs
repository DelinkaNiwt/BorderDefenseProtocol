using System.Collections.Generic;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>完整黑触发器的形成路线与首次装备适格声明。</summary>
    public sealed class BlackTriggerFormationExtension : DefModExtension
    {
        /// <summary>该完整物品允许通过的形成路线；没有路线时不进入形成池。</summary>
        public List<BlackTriggerFormationRoute> formationRoutes = new List<BlackTriggerFormationRoute>();

        /// <summary>该完整物品用于隐藏形成权重的适应特性声明。</summary>
        public List<BlackTriggerAdaptedTrait> adaptedTraits = new List<BlackTriggerAdaptedTrait>();

        /// <summary>该完整物品用于隐藏形成权重的适应能力声明。</summary>
        public List<BlackTriggerAdaptedAbility> adaptedAbilities = new List<BlackTriggerAdaptedAbility>();

        /// <summary>首次装备时满足任意一组即可适格；空列表表示不另设内容条件。</summary>
        public List<BlackTriggerEligibilityConditionGroup> eligibilityGroups = new List<BlackTriggerEligibilityConditionGroup>();
    }
}
