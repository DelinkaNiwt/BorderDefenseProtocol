namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Preview 阶段模块接口。
    /// 模块可按预览维度补充反馈，但不直接推进攻击执行。
    /// </summary>
    public interface IPreviewStageModule
    {
        void Contribute(PreviewRecord record);
    }
}
