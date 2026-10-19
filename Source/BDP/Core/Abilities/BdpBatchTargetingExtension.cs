using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>能力定义显式选择批量目标规则，未配置的能力保持整批严格校验。</summary>
    public sealed class BdpBatchTargetingExtension : DefModExtension
    {
        /// <summary>起终点必须完整合法，中间跳过不适用目标且不要求逐格视线。</summary>
        public bool skipInvalidInteriorTargets;
    }
}
