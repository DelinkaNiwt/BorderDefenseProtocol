using UnityEngine;

namespace BDP.Content.Assembly.ChipManufacturing.UI
{
    /// <summary>中栏条形图统一固定标尺；占位上限集中在此便于后续校准。</summary>
    public static class ChipMetricBarScale
    {
        /// <summary>精度固定上限 100%。</summary>
        public const float AccuracyMaximum = 1f;

        /// <summary>射程占位上限 100 格。</summary>
        public const float RangeMaximum = 100f;

        /// <summary>投射物伤害占位上限 100。</summary>
        public const float DamageMaximum = 100f;

        /// <summary>投射物速度占位上限 100。</summary>
        public const float SpeedMaximum = 100f;

        /// <summary>预热时间占位上限 5 秒。</summary>
        public const float WarmupMaximum = 5f;

        /// <summary>冷却时间占位上限 5 秒。</summary>
        public const float CooldownMaximum = 5f;

        /// <summary>子弹数量占位上限 10 发。</summary>
        public const float BurstShotCountMaximum = 10f;

        /// <summary>把非负数值限制到给定固定标尺的 0～1 范围。</summary>
        public static float Normalize(float value, float maximum)
        {
            return maximum > 0f ? Mathf.Clamp01(value / maximum) : 0f;
        }
    }
}
