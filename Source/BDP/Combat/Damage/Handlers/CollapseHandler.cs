namespace BDP.Combat
{
    /// <summary>
    /// 破裂条件检测器。
    /// 纯检查，不执行任何副作用。
    ///
    /// 破裂条件：
    /// 1. Trion耗尽（由TrionCostHandler返回false触发）
    /// 2. 关键部位破坏（头部、躯干等）
    ///
    /// 设计说明（v13.0重构）：
    /// - 不再直接调用gene.DeactivateCombatBody()
    /// - 只返回是否需要破裂，由DamageHandler在Pipeline末尾统一执行
    /// - 消除了Pipeline中途触发解除导致的循环调用风险
    /// - 移除了processingPawns防重入HashSet（不再需要）
    /// </summary>
    public static class CollapseHandler
    {
        /// <summary>
        /// 检查是否满足破裂条件（纯检查，不执行副作用）。
        /// </summary>
        /// <param name="trionDepleted">Trion是否耗尽</param>
        /// <param name="criticalPartDestroyed">关键部位是否破坏</param>
        /// <returns>true=需要破裂，false=不需要</returns>
        public static bool ShouldCollapse(bool trionDepleted, bool criticalPartDestroyed)
        {
            return trionDepleted || criticalPartDestroyed;
        }
    }
}
