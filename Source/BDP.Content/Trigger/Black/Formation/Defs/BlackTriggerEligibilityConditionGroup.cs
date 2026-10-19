using System.Collections.Generic;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>首次装备适格条件组；组之间是或关系。</summary>
    public sealed class BlackTriggerEligibilityConditionGroup
    {
        /// <summary>必须同时满足的条件。</summary>
        public List<BlackTriggerEligibilityCondition> allOf =
            new List<BlackTriggerEligibilityCondition>();

        /// <summary>从中至少满足若干项的条件。</summary>
        public List<BlackTriggerEligibilityCondition> anyOf =
            new List<BlackTriggerEligibilityCondition>();

        /// <summary>anyOf 至少需要满足的数量。</summary>
        public int anyOfMinimum = 1;

        /// <summary>不得满足的条件。</summary>
        public List<BlackTriggerEligibilityCondition> noneOf =
            new List<BlackTriggerEligibilityCondition>();
    }
}
