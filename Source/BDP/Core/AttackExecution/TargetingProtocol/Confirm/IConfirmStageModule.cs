namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Confirm 阶段模块接口。
    /// 模块可在正式下单前读取或修正确认记录。
    /// </summary>
    public interface IConfirmStageModule
    {
        void Contribute(ConfirmRecord record);
    }
}
