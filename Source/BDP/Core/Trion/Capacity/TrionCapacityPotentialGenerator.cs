using UnityEngine;
using Verse;

namespace BDP.Core.Trion.Capacity
{
    /// <summary>
    /// 按正式分层概率生成永久 Trion 潜在容量。
    /// </summary>
    public sealed class TrionCapacityPotentialGenerator
    {
        /// <summary>配置缺失或无效时使用的安全容量。</summary>
        private const int FallbackCapacity = 1000;

        /// <summary>共享无状态生成器。</summary>
        public static readonly TrionCapacityPotentialGenerator Instance = new TrionCapacityPotentialGenerator();

        /// <summary>禁止外部创建无意义实例。</summary>
        private TrionCapacityPotentialGenerator()
        {
        }

        /// <summary>配置的量化单位。</summary>
        private static int QuantizationUnit(TrionCapacityPotentialDistributionDef def)
        {
            return Mathf.Max(1, def.quantizationUnit);
        }

        /// <summary>
        /// 生成一次潜在容量。
        /// </summary>
        public int Generate(TrionCapacityPotentialDistributionDef def)
        {
            if (def == null)
            {
                Log.ErrorOnce("[BDP.Trion] 缺少潜在容量分布定义，回退为 1000。", 17432001);
                return FallbackCapacity;
            }

            float totalWeight;
            string validationError;
            if (!TryValidate(def, out totalWeight, out validationError))
            {
                Log.ErrorOnce(
                    "[BDP.Trion] 潜在容量分布配置无效：" + validationError + "，回退为 1000。",
                    17432002);
                return FallbackCapacity;
            }

            float roll = Rand.Value * totalWeight;
            float accumulatedWeight = 0f;
            TrionCapacityPotentialGenerationBand selectedBand = def.bands[def.bands.Count - 1];
            foreach (TrionCapacityPotentialGenerationBand band in def.bands)
            {
                accumulatedWeight += band.weight;
                if (roll < accumulatedWeight)
                {
                    selectedBand = band;
                    break;
                }
            }

            return GenerateWithinBand(selectedBand, def);
        }

        /// <summary>
        /// 在档位包含的量化容量中等概率选择一个值。
        /// </summary>
        private int GenerateWithinBand(TrionCapacityPotentialGenerationBand band, TrionCapacityPotentialDistributionDef def)
        {
            int unit = QuantizationUnit(def);
            int firstStep = Mathf.CeilToInt((float)band.minimumCapacity / unit);
            int lastStep = Mathf.FloorToInt((float)band.maximumCapacity / unit);
            return Rand.RangeInclusive(firstStep, lastStep) * unit;
        }

        /// <summary>
        /// 验证配置并汇总有效档位的总权重。
        /// </summary>
        private bool TryValidate(
            TrionCapacityPotentialDistributionDef def,
            out float totalWeight,
            out string validationError)
        {
            totalWeight = 0f;
            validationError = null;
            if (def.quantizationUnit <= 0)
            {
                validationError = "量化单位必须大于零";
                return false;
            }

            if (def.bands == null || def.bands.Count == 0)
            {
                validationError = "至少需要一个生成档位";
                return false;
            }

            int unit = QuantizationUnit(def);
            foreach (TrionCapacityPotentialGenerationBand band in def.bands)
            {
                if (band == null)
                {
                    validationError = "生成档位不能为空";
                    return false;
                }

                if (band.weight <= 0f || float.IsNaN(band.weight) || float.IsInfinity(band.weight))
                {
                    validationError = "档位权重必须是有限正数";
                    return false;
                }

                if (band.minimumCapacity > band.maximumCapacity
                    || band.minimumCapacity < def.minimumCapacity
                    || band.maximumCapacity > def.maximumCapacity)
                {
                    validationError = "档位容量边界超出全局范围或顺序错误";
                    return false;
                }

                int firstStep = Mathf.CeilToInt((float)band.minimumCapacity / unit);
                int lastStep = Mathf.FloorToInt((float)band.maximumCapacity / unit);
                if (firstStep > lastStep)
                {
                    validationError = "档位内不存在符合量化单位的容量";
                    return false;
                }

                totalWeight += band.weight;
            }

            if (totalWeight <= 0f || float.IsNaN(totalWeight) || float.IsInfinity(totalWeight))
            {
                validationError = "档位总权重必须是有限正数";
                return false;
            }

            return true;
        }
    }
}
