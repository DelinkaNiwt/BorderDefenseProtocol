using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 旧 Verb 宿主链已经拆除。
    /// 新宿主层接回前，这个解析器只保留失败壳，避免任何代码继续借旧路径。
    /// </summary>
    internal sealed class DefaultExpressionVerbHostResolver
    {
        /// <summary>
        /// 尝试为指定 Pawn 和正式结果解析真实 Verb 宿主实例。
        /// </summary>
        public bool TryResolve(Pawn pawn, FormalExpressionResult result, out Verb verb)
        {
            verb = null;
            // 旧宿主链已拆除，新宿主层尚未在这里接回。
            return false;
        }
    }
}
