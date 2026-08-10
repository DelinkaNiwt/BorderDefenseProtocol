namespace BDP.Core.Projectiles.RangedFlightProtocol.Arrival
{
    /// <summary>
    /// Arrival 阶段模块接口。
    /// </summary>
    public interface IArrivalStageModule
    {
        void Contribute(in ArrivalStageContext context, ArrivalContribution contribution);
    }
}
