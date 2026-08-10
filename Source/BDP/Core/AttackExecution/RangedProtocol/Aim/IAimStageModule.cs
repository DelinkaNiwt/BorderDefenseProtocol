namespace BDP.Core.AttackExecution.RangedProtocol.Aim
{
    /// <summary>
    /// Aim 阶段模块接口。
    /// 模块只提交瞄准贡献，不直接产出最终 AimRecord。
    /// </summary>
    public interface IAimStageModule
    {
        void Contribute(in AimStageContext context, AimContribution contribution);
    }
}
