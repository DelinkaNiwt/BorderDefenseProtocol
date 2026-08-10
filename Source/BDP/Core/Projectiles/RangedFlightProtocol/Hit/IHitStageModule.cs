namespace BDP.Core.Projectiles.RangedFlightProtocol.Hit
{
    /// <summary>
    /// Hit 阶段模块接口。
    /// </summary>
    public interface IHitStageModule
    {
        void Contribute(in HitStageContext context, HitContribution contribution);
    }
}
