namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// Impact 阶段模块接口。
    /// </summary>
    public interface IImpactStageModule
    {
        void Contribute(in ImpactStageContext context, ImpactContribution contribution);
    }
}
