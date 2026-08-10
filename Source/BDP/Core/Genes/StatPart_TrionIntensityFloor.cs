using RimWorld;
using UnityEngine;

namespace BDP.Core.Genes
{
    /// <summary>
    /// 在所有属性修正完成后，把 Trion 释放力统一压成非负整数。
    /// </summary>
    public sealed class StatPart_TrionIntensityFloor : StatPart
    {
        /// <summary>向下取整并钳制到零以上。</summary>
        public override void TransformValue(StatRequest req, ref float val)
        {
            val = Mathf.Max(0, Mathf.FloorToInt(val));
        }

        /// <summary>最终整理步骤不额外占用属性说明行。</summary>
        public override string ExplanationPart(StatRequest req)
        {
            return null;
        }
    }
}
