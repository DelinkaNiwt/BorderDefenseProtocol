namespace BDP.Core.AttackExecution.RangedProtocol.Fire
{
    /// <summary>
    /// Fire 阶段模块接口。
    /// 模块只提交 Fire 阶段贡献，不直接发射。
    /// </summary>
    public interface IFireStageModule
    {
        void Contribute(in FireStageContext context, FireContribution contribution);
    }
}
