using System.Collections.Generic;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>首次装备适格判断结果。</summary>
    public sealed class BlackTriggerCompatibilityResult
    {
        /// <summary>是否允许装备。</summary>
        public bool Accepted;
        /// <summary>玩家提示语言键。</summary>
        public string ReasonKey;
        /// <summary>未通过的具体条件说明。</summary>
        public List<string> FailedReasons = new List<string>();
    }
}
