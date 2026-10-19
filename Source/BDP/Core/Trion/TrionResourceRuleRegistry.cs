using Verse;

namespace BDP.Core.Trion
{
    /// <summary>单一资源规则接入点；没有业务程序集时保持原行为。</summary>
    public static class TrionResourceRuleRegistry
    {
        /// <summary>当前唯一提供器，不把业务规则永久保存进人物。</summary>
        private static ITrionResourceRuleProvider current;
        /// <summary>注册或替换提供器。</summary>
        public static void Register(ITrionResourceRuleProvider provider) { current = provider; }
        /// <summary>只移除指定实例，避免旧提供器清理误删新实例。</summary>
        public static void Clear(ITrionResourceRuleProvider provider)
        {
            if (ReferenceEquals(current, provider)) current = null;
        }
        /// <summary>读取当前规则，Core 不判断身份。</summary>
        public static TrionResourceRules GetRules(Thing owner)
        {
            return current != null ? current.GetRules(owner) : TrionResourceRules.Default;
        }
        /// <summary>计算本次真正应占用的金额，调用方记录和释放相同的金额。</summary>
        public static float ResolveCapacityCost(Thing owner, float declaredAmount)
        {
            return GetRules(owner).CapacityCostsEnabled ? declaredAmount : 0f;
        }
    }
}
