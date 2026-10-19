using System;
using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>护盾语义的专用展示器；未知扩展属性交给通用展示器。</summary>
    internal sealed class ShieldFeatureRenderer : IChipFeatureRenderer
    {
        private static readonly HashSet<string> KnownPropertyIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "BDP_Property_ShieldCapacity",
            "BDP_Property_ShieldRecovery",
            "BDP_Property_BlockArc",
            "BDP_Property_BlockChance",
            "BDP_Property_BlockAttackTypes",
            "BDP_Property_DamageAbsorptionRatio",
            "BDP_Property_BlockCost",
            "BDP_Property_RecoveryDelay",
            "BDP_Property_EffectDuration",
            "BDP_Property_EffectRadius"
        };

        /// <summary>生成护盾专用属性行。</summary>
        public ChipFeatureRenderResult Render(ChipInformationNode feature, ChipFeatureRenderContext context)
        {
            return FeatureRendererUtilities.RenderKnown(feature, KnownPropertyIds);
        }
    }
}
