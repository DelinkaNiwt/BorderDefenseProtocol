using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>芯片启停延迟的宿主规则；核心不解释具体身份。</summary>
    public interface ITriggerSwitchDelayPolicy
    {
        /// <summary>当前宿主是否免除启用与停用延迟。</summary>
        bool BypassDelays(Pawn owner);
    }
}
