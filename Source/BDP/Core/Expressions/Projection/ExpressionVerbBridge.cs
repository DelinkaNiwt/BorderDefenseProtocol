using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 旧表达侧 Verb 宿主桥已经拆除。
    /// 新宿主层接回前，这里只保留失败壳，阻止外部继续借旧路径。
    /// </summary>
    internal static class ExpressionVerbBridge
    {
        /// <summary>
        /// 当前默认使用的 Verb 宿主解析器。
        /// </summary>
        private static readonly DefaultExpressionVerbHostResolver VerbHostResolver = new DefaultExpressionVerbHostResolver();

        /// <summary>
        /// 尝试把指定正式结果解析成真实 Verb。
        /// 旧宿主链已拆除，新宿主层尚未在这里接回。
        /// </summary>
        public static bool TryResolveFormalVerb(Pawn pawn, FormalExpressionResult result, out Verb verb)
        {
            verb = null;
            return false;
        }

        /// <summary>
        /// 读取当前默认 Verb 宿主解析器。
        /// 需要共用 Verb 宿主解析能力的其它边界，应走这条读取口而不是各自 new 默认实现。
        /// </summary>
        public static DefaultExpressionVerbHostResolver ResolveVerbHostResolver()
        {
            return VerbHostResolver;
        }
    }
}
