using BDP.Core.Genes;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Trion.Intensity
{
    /// <summary>
    /// Trion 释放力的统一正式读取入口。
    /// </summary>
    public static class TrionIntensityUtility
    {
        /// <summary>
        /// 把释放力整数统一格式化为玩家可读的等级文本。
        /// </summary>
        public static string FormatLevel(int value)
        {
            return "BDP_Unit_Level".Translate(Mathf.Max(0, value)).ToString();
        }

        /// <summary>
        /// 读取经过原版属性修正、向下取整且不低于零的当前释放力。
        /// </summary>
        public static int GetEffective(Pawn pawn)
        {
            if (pawn == null || !pawn.RaceProps.Humanlike)
            {
                return 0;
            }

            float value = pawn.GetStatValue(TrionStatDefOf.BDP_TrionIntensity, true);
            return Mathf.Max(0, Mathf.FloorToInt(value));
        }
    }
}
