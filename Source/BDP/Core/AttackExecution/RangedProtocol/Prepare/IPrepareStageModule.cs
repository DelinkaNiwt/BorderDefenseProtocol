namespace BDP.Core.AttackExecution.RangedProtocol.Prepare
{
    /// <summary>
    /// Prepare 阶段模块接口。
    /// 模块只允许提交准备阶段贡献。
    /// </summary>
    public interface IPrepareStageModule
    {
        void Contribute(in PrepareStageContext context, PrepareContribution contribution);
    }
}
