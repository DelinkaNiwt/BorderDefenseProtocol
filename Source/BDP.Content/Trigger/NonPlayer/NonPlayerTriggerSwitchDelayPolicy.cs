using BDP.Core.Trigger;
using Verse;

namespace BDP.Content.Trigger.NonPlayer
{
    /// <summary>非玩家派系人物的芯片即时启停，不改变芯片自身配置。</summary>
    public sealed class NonPlayerTriggerSwitchDelayPolicy : ITriggerSwitchDelayPolicy
    {
        /// <summary>无派系也免延迟；玩家派系人物暂时不可控时仍使用正常时长。</summary>
        public bool BypassDelays(Pawn owner)
        {
            return owner != null && owner.Faction?.IsPlayer != true;
        }
    }
}
