namespace BDP.Core.Projectiles.RangedFlightProtocol.Flight
{
    /// <summary>
    /// Flight 阶段模块接口。
    /// 模块只提交飞行候选结论，不直接控制 projectile 宿主。
    /// </summary>
    public interface IFlightStageModule
    {
        void Contribute(in FlightStageContext context, FlightContribution contribution);
    }
}
