using System;
using BDP.Glow.Utils;
using UnityEngine;
using Verse;

namespace BDP.Glow.Controllers
{
    /// <summary>
    /// 脉冲发光控制器——周期性强度变化（呼吸灯效果）。
    ///
    /// 有状态：ticksAlive需要序列化，以便读档后动画从正确位置继续。
    /// 适用场景：能量建筑、充能武器等。
    /// </summary>
    public class PulseGlowController : IGlowController
    {
        // ── 配置（来自PulseGlowParams） ──
        private float minIntensity = 0.3f;
        private float maxIntensity = 1.0f;
        private int period = 180;
        private Func<float, float> curveFunc;

        // ── 运行时状态（需要序列化） ──
        private int ticksAlive = 0;

        public void Initialize(Thing parent, GlowControllerParams parms)
        {
            if (parms is PulseGlowParams p)
            {
                minIntensity = p.minIntensity;
                maxIntensity = p.maxIntensity;
                period = Mathf.Max(1, p.period); // 防止除零
                curveFunc = AnimationCurves.GetCurve(p.curve);
            }
            else
            {
                // 使用默认值，曲线默认Sine
                curveFunc = AnimationCurves.Sine;
            }
        }

        public void Tick()
        {
            ticksAlive++;
        }

        public float GetGlowIntensity()
        {
            // t∈[0,1]，表示当前在周期中的位置
            float t = (ticksAlive % period) / (float)period;
            float curveValue = curveFunc(t); // ∈[0,1]

            // 映射到[minIntensity, maxIntensity]
            return Mathf.Lerp(minIntensity, maxIntensity, curveValue);
        }

        public Color? GetGlowColor() => null;

        public void ExposeData()
        {
            Scribe_Values.Look(ref ticksAlive, "pulseTicksAlive", 0);
        }
    }
}
