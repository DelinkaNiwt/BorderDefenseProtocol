namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击上下文内由主模组自己维护的中性键名。
    /// 这里只定义主干设施需要的固定落点，不定义任何具体业务协议。
    /// </summary>
    internal static class AttackContextKeys
    {
        /// <summary>
        /// 已确认输入冻结节点的固定键名。
        /// </summary>
        internal const string ConfirmedInput = "confirmed.input";

        /// <summary>
        /// 已确认交互冻结节点的固定键名。
        /// </summary>
        internal const string ConfirmedInteraction = "confirmed.interaction";

        /// <summary>
        /// 已确认目标冻结节点的固定键名。
        /// </summary>
        internal const string ConfirmedTarget = "confirmed.target";

        /// <summary>
        /// 目标交互输入状态节点的固定键名。
        /// </summary>
        internal const string TargetingInputState = "targeting.input-state";

        /// <summary>
        /// 目标交互推进会话节点的固定键名。
        /// </summary>
        internal const string TargetingInteraction = "targeting.interaction";

        /// <summary>
        /// 模块私有上下文节点键名前缀。
        /// 主模组只拼出槽位索引，不解释上下文内容。
        /// </summary>
        internal const string ModulePrivatePrefix = "ranged.module.private.";

        /// <summary>
        /// 生成指定挂载槽位的私有上下文键名。
        /// </summary>
        internal static string GetModulePrivateKey(int mountIndex)
        {
            return ModulePrivatePrefix + mountIndex;
        }
    }
}
