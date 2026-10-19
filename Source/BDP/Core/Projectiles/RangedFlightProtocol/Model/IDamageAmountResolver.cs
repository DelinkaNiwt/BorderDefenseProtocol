namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 延迟伤害量解析器。
    /// 具体业务只消费 Core 提供的中性承伤事实，不直接介入伤害入口。
    /// </summary>
    public interface IDamageAmountResolver
    {
        /// <summary>
        /// 根据最终承伤目标和命中部件解析本次伤害量。
        /// </summary>
        float Resolve(in DamageAmountResolutionContext context);
    }
}
