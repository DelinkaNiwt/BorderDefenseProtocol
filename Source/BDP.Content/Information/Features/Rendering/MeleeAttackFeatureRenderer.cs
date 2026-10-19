using System;
using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>近战攻击语义的专用展示器。</summary>
    internal sealed class MeleeAttackFeatureRenderer : IChipFeatureRenderer
    {
        private static readonly HashSet<string> KnownPropertyIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "BDP_Property_DamageAmount",
            "BDP_Property_DamageType",
            "BDP_Property_ArmorPenetration"
        };

        /// <summary>生成近战攻击专用属性行。</summary>
        public ChipFeatureRenderResult Render(ChipInformationNode feature, ChipFeatureRenderContext context)
        {
            return FeatureRendererUtilities.RenderKnown(feature, KnownPropertyIds);
        }
    }
}
