namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Preview 阶段的中性预览维度。
    /// 它只描述原版预览表面的组成部分，不携带具体业务语义。
    /// </summary>
    internal enum PreviewDimension
    {
        RangeRing = 0,
        TargetHighlight = 1,
        FieldRadius = 2,
        MouseAttachment = 3
    }
}
