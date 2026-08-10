namespace BDP.Core.BodyConstraints
{
    /// <summary>
    /// Pawn 身体约束变化类型。
    /// 当前阶段先承认“缺失部位变化”这一类正式上游信号。
    /// </summary>
    internal enum PawnBodyConstraintChangeKind
    {
        /// <summary>
        /// 缺失部位状态发生了变化。
        /// </summary>
        MissingPartChanged = 0
    }
}
