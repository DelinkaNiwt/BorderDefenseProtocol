namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// ManualEntry 阶段模块接口。
    /// 模块可修改入口展示记录，但不直接启动 Targeter。
    /// </summary>
    public interface IManualEntryStageModule
    {
        void Contribute(ManualEntryRecord record);
    }
}
