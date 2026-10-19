using System;
using RimWorld;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>黑触发器声明的一项隐藏适应特性。</summary>
    public sealed class BlackTriggerAdaptedTrait
    {
        /// <summary>适应的原版特性定义。</summary>
        public TraitDef trait;

        /// <summary>精确适应的 degree；使用最小整数表示任意 degree。</summary>
        public int degree = int.MinValue;

        /// <summary>是否声明了精确 degree。</summary>
        public bool HasSpecificDegree
        {
            get { return degree != int.MinValue; }
        }

        /// <summary>判断一项冻结来源特性是否命中本声明。</summary>
        public bool Matches(BlackTriggerTraitSnapshot source)
        {
            return source != null
                && !source.WasSuppressed
                && source.TraitDef == trait
                && (!HasSpecificDegree || source.Degree == degree);
        }

        /// <summary>用于开发诊断和重复声明校验的稳定键。</summary>
        public string StableKey
        {
            get { return (trait?.defName ?? string.Empty) + ":" + (HasSpecificDegree ? degree.ToString() : "*"); }
        }
    }
}
