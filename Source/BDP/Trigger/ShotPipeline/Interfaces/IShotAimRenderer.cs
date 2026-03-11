namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 可选接口：参与Targeting子步骤的渲染（多帧，DrawHighlight中）
    /// 用于绘制范围圈、弹道预览等瞄准指示
    /// </summary>
    public interface IShotAimRenderer
    {
        void RenderTargeting(ShotSession session, Verse.LocalTargetInfo mouseTarget);
    }
}
