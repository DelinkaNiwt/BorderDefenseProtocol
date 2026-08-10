namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Targeting 阶段模块接口。
    /// 模块可调整目标选择表面，但不绕开原版 Targeter。
    /// </summary>
    public interface ITargetingStageModule
    {
        void Contribute(TargetingRecord record);
    }
}
