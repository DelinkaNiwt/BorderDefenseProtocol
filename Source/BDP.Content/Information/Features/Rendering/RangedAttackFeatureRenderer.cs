using System;
using System.Collections.Generic;
using BDP.Core.Information;

namespace BDP.Content.Information.Features.Rendering
{
    /// <summary>远程攻击语义的专用展示器；只消费通用远程属性。</summary>
    internal sealed class RangedAttackFeatureRenderer : IChipFeatureRenderer
    {
        private static readonly HashSet<string> KnownPropertyIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "BDP_Property_Projectile",
            "BDP_Property_CastRange",
            "BDP_Property_MinimumCastRange",
            "BDP_Property_DamageAmount",
            "BDP_Property_DamageType",
            "BDP_Property_ArmorPenetration",
            "BDP_Property_AttackBurstCount",
            "BDP_Property_RangedShotRhythm",
            "BDP_Property_AttackBurstInterval",
            "BDP_Property_ProjectileSpeed",
            "BDP_Property_ProjectileFlightFeatures",
            "BDP_Property_StoppingPower"
        };

        /// <summary>生成远程攻击专用属性行。</summary>
        public ChipFeatureRenderResult Render(ChipInformationNode feature, ChipFeatureRenderContext context)
        {
            return FeatureRendererUtilities.RenderKnown(feature, KnownPropertyIds);
        }
    }
}
