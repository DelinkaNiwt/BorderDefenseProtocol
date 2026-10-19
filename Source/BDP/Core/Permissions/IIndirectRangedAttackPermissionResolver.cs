namespace BDP.Core.Permissions
{
    /// <summary>
    /// 远程间接攻击许可的中性业务解析器。
    /// Core 只定义接入点，不规定任何具体设施或许可键。
    /// </summary>
    public interface IIndirectRangedAttackPermissionResolver
    {
        /// <summary>
        /// 尝试按当前 Pawn、表达结果和具体目标产生一条授权。
        /// </summary>
        /// <param name="context">目标感知的间接攻击许可上下文。</param>
        /// <param name="grant">成功时输出授权结果。</param>
        /// <returns>当前解析器认领并授权时为 true。</returns>
        bool TryResolve(
            IndirectRangedAttackPermissionContext context,
            out IndirectRangedAttackPermissionGrant grant);
    }
}
