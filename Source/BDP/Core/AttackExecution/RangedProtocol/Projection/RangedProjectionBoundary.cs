namespace BDP.Core.AttackExecution.RangedProtocol.Projection
{
    /// <summary>
    /// 前半段投影层边界说明。
    /// 这里只放只读消费契约，不允许反向主导远程攻击主协议。
    /// </summary>
    internal static class RangedProjectionBoundary
    {
        public const string AreaIndicatorInput = "AimRecord + RangedProjectionSeed";
        public const string MuzzleVisualInput = "ProjectileInitPlan + RangedProjectionSeed";
        public const string ExternalInfoInput = "只读阶段摘要，不读取模块私货";
    }
}
