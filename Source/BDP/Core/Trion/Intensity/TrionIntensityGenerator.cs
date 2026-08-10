using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.Trion.Intensity
{
    /// <summary>
    /// 按正式权重生成一次永久不变的先天 Trion 释放力。
    /// </summary>
    public sealed class TrionIntensityGenerator
    {
        /// <summary>配置缺失或无效时使用的安全释放力。</summary>
        private const int FallbackIntensity = 4;

        /// <summary>共享无状态生成器。</summary>
        public static readonly TrionIntensityGenerator Instance = new TrionIntensityGenerator();

        /// <summary>禁止外部创建重复生成器。</summary>
        private TrionIntensityGenerator()
        {
        }

        /// <summary>
        /// 从有效分布中生成一个 1～10 的整数释放力。
        /// </summary>
        public int Generate(TrionIntensityDistributionDef def)
        {
            float totalWeight;
            string validationError;
            if (!TryValidate(def, out totalWeight, out validationError))
            {
                Log.ErrorOnce(
                    "[BDP.Trion] 释放力分布配置无效：" + validationError + "，回退为 4。",
                    17433001);
                return FallbackIntensity;
            }

            float roll = Rand.Value * totalWeight;
            float accumulatedWeight = 0f;
            TrionIntensityWeight selected = def.values[def.values.Count - 1];
            foreach (TrionIntensityWeight value in def.values)
            {
                accumulatedWeight += value.weight;
                if (roll < accumulatedWeight)
                {
                    selected = value;
                    break;
                }
            }

            return selected.intensity;
        }

        /// <summary>
        /// 验证 1～10 是否各出现一次，并汇总有限正权重。
        /// </summary>
        private static bool TryValidate(
            TrionIntensityDistributionDef def,
            out float totalWeight,
            out string validationError)
        {
            totalWeight = 0f;
            validationError = null;
            if (def?.values == null || def.values.Count != 10)
            {
                validationError = "必须恰好定义 10 项释放力";
                return false;
            }

            HashSet<int> intensities = new HashSet<int>();
            foreach (TrionIntensityWeight value in def.values)
            {
                if (value == null)
                {
                    validationError = "释放力条目不能为空";
                    return false;
                }

                if (value.intensity < 1 || value.intensity > 10 || !intensities.Add(value.intensity))
                {
                    validationError = "1～10 的释放力必须各出现一次";
                    return false;
                }

                if (value.weight <= 0f || float.IsNaN(value.weight) || float.IsInfinity(value.weight))
                {
                    validationError = "释放力权重必须是有限正数";
                    return false;
                }

                totalWeight += value.weight;
            }

            if (totalWeight <= 0f || float.IsNaN(totalWeight) || float.IsInfinity(totalWeight))
            {
                validationError = "释放力总权重必须是有限正数";
                return false;
            }

            return true;
        }
    }
}
