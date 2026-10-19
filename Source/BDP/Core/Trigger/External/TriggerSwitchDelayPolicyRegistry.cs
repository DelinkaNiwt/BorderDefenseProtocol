using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>单一可替换的启停延迟规则口；不保存宿主身份状态。</summary>
    public static class TriggerSwitchDelayPolicyRegistry
    {
        /// <summary>当前内容规则，未注册时沿用芯片配置。</summary>
        private static ITriggerSwitchDelayPolicy current;

        /// <summary>注册、替换或以空值移除规则。</summary>
        public static void Register(ITriggerSwitchDelayPolicy policy)
        {
            current = policy;
        }

        /// <summary>解析本次实际时长；只免除等待，不绕过正式切换校验。</summary>
        public static int ResolveDelayTicks(Pawn owner, int configuredTicks)
        {
            return current != null && current.BypassDelays(owner) ? 0 : configuredTicks;
        }
    }
}
