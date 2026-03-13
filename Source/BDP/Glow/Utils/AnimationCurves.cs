using System;
using System.Collections.Generic;
using UnityEngine;

namespace BDP.Glow.Utils
{
    /// <summary>
    /// 动画曲线工具类——提供常用缓动函数。
    ///
    /// 所有函数接受t∈[0,1]，返回f(t)∈[0,1]。
    /// 用于PulseGlowController等动画控制器的强度插值。
    /// </summary>
    public static class AnimationCurves
    {
        // ── 曲线函数注册表 ──
        private static readonly Dictionary<string, Func<float, float>> curveMap
            = new Dictionary<string, Func<float, float>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Linear",      Linear      },
            { "SmoothInOut", SmoothInOut },
            { "Sine",        Sine        },
        };

        // ─────────────────────────────────────────────────────────────────────
        // 曲线函数
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>线性插值：f(t) = t</summary>
        public static float Linear(float t) => Mathf.Clamp01(t);

        /// <summary>
        /// 平滑进出（SmoothStep）：f(t) = 3t² - 2t³
        /// 两端缓慢，中间快速。
        /// </summary>
        public static float SmoothInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// 正弦曲线（呼吸灯效果）：f(t) = (1 - cos(2πt)) / 2
        /// 从0开始，平滑上升到1，再平滑回到0。
        /// </summary>
        public static float Sine(float t)
        {
            t = Mathf.Clamp01(t);
            return (1f - Mathf.Cos(t * Mathf.PI * 2f)) * 0.5f;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 查找接口
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 根据名称获取曲线函数。
        /// 找不到时回退到Linear并记录警告。
        /// </summary>
        public static Func<float, float> GetCurve(string name)
        {
            if (string.IsNullOrEmpty(name)) return Linear;

            if (curveMap.TryGetValue(name, out var fn)) return fn;

            Verse.Log.Warning($"[BDP.Glow] 未知动画曲线: '{name}'，回退到Linear。可用: {string.Join(", ", curveMap.Keys)}");
            return Linear;
        }
    }
}
