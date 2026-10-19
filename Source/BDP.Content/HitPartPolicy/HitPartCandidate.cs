using Verse;

namespace BDP.Content.HitPartPolicy
{
    /// <summary>
    /// 统一命中部位解析器使用的候选部位。
    /// </summary>
    internal sealed class HitPartCandidate
    {
        /// <summary>候选身体部位。</summary>
        public BodyPartRecord Part;

        /// <summary>原版身体能力来源位标记。</summary>
        public int SourceKinds;

        /// <summary>是否按要害语义分类。</summary>
        public bool IsVital;

        /// <summary>当前策略下的加权随机权重。</summary>
        public float Weight;
    }
}
