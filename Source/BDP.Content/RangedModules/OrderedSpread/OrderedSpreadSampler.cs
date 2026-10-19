using BDP.Core.AttackExecution.RangedProtocol.Model;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.OrderedSpread
{
    /// <summary>
    /// 远程路径段的中性稳定散布工具。
    /// </summary>
    public static class OrderedSpreadSampler
    {
        /// <summary>区分中间段与最终段的稳定随机样本。</summary>
        public enum SampleKind
        {
            /// <summary>中间路径段样本。</summary>
            Intermediate = 1,

            /// <summary>最终收束段样本。</summary>
            Final = 2
        }

        /// <summary>
        /// 按冻结的原版精度事实缩放基础散布半径。
        /// </summary>
        public static float ResolveScaledRadius(
            ProjectileAccuracySnapshot accuracySnapshot,
            float baseRadius,
            float highAccuracyScale)
        {
            float quality = accuracySnapshot != null && accuracySnapshot.IsAvailable
                ? Mathf.Clamp01(accuracySnapshot.StandardAimChance)
                : 0.5f;
            float safeHighAccuracyScale = Mathf.Clamp01(highAccuracyScale);
            float spreadScale = Mathf.Lerp(1f, safeHighAccuracyScale, quality);
            return Mathf.Max(0f, baseRadius * spreadScale);
        }

        /// <summary>
        /// 按攻击、投射物和路径段生成稳定的圆盘偏移。
        /// </summary>
        public static Vector3 SampleOffset(
            float spreadRadius,
            string attackInstanceId,
            string resultId,
            int emitIndex,
            int segmentIndex,
            SampleKind sampleKind)
        {
            float safeRadius = Mathf.Max(0f, spreadRadius);
            if (safeRadius <= 0f)
            {
                return Vector3.zero;
            }

            int seed = BuildSpreadSeed(
                attackInstanceId,
                resultId,
                emitIndex,
                segmentIndex,
                sampleKind);
            Rand.PushState(seed);
            try
            {
                float angle = Rand.Value * Mathf.PI * 2f;
                float radius = Mathf.Sqrt(Rand.Value) * safeRadius;
                return new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius);
            }
            finally
            {
                Rand.PopState();
            }
        }

        /// <summary>
        /// 构造不依赖全局随机状态的稳定散布种子。
        /// </summary>
        private static int BuildSpreadSeed(
            string attackInstanceId,
            string resultId,
            int emitIndex,
            int segmentIndex,
            SampleKind sampleKind)
        {
            int seed = 143921;
            seed = Gen.HashCombineInt(seed, !string.IsNullOrWhiteSpace(attackInstanceId)
                ? GenText.StableStringHash(attackInstanceId)
                : 0);
            seed = Gen.HashCombineInt(seed, !string.IsNullOrWhiteSpace(resultId)
                ? GenText.StableStringHash(resultId)
                : 0);
            seed = Gen.HashCombineInt(seed, emitIndex);
            seed = Gen.HashCombineInt(seed, segmentIndex);
            seed = Gen.HashCombineInt(seed, (int)sampleKind);
            return seed;
        }
    }
}
