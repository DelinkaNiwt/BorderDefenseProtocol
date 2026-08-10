using System;
using Verse;

namespace BDP.Core.BodyConstraints
{
    /// <summary>
    /// Pawn 身体约束变化事件参数。
    /// 它只描述“哪个 Pawn 的哪类身体约束变了”，不夹带 Trigger 语义。
    /// </summary>
    internal sealed class PawnBodyConstraintChangedArgs : EventArgs
    {
        /// <summary>
        /// 发生变化的 Pawn。
        /// </summary>
        public Pawn Pawn;

        /// <summary>
        /// 变化类型。
        /// </summary>
        public PawnBodyConstraintChangeKind ChangeKind;

        /// <summary>
        /// 该 Pawn 当前身体约束版本。
        /// 每次发布变化时递增。
        /// </summary>
        public int Version;
    }
}
