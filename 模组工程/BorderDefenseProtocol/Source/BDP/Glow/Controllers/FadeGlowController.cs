using UnityEngine;
using Verse;

namespace BDP.Glow.Controllers
{
    /// <summary>
    /// 淡入淡出控制器——三段式状态机。
    ///
    /// 状态流转：FadeIn → Sustain → FadeOut → Finished（或循环回FadeIn）
    /// 有状态：phase和phaseTicksElapsed需要序列化。
    /// 适用场景：激活/关闭动画、临时效果。
    /// </summary>
    public class FadeGlowController : IGlowController
    {
        // ── 状态枚举 ──
        private enum Phase { FadeIn, Sustain, FadeOut, Finished }

        // ── 配置（来自FadeGlowParams） ──
        private int fadeInTicks = 60;
        private int sustainTicks = 120;
        private int fadeOutTicks = 60;
        private bool loop = false;

        // ── 运行时状态（需要序列化） ──
        private Phase phase = Phase.FadeIn;
        private int phaseTicksElapsed = 0;

        public void Initialize(Thing parent, GlowControllerParams parms)
        {
            if (parms is FadeGlowParams p)
            {
                fadeInTicks = Mathf.Max(1, p.fadeInTicks);
                sustainTicks = p.sustainTicks; // -1=永久
                fadeOutTicks = Mathf.Max(1, p.fadeOutTicks);
                loop = p.loop;
            }
        }

        public void Tick()
        {
            if (phase == Phase.Finished) return;

            phaseTicksElapsed++;

            // 状态机转换
            switch (phase)
            {
                case Phase.FadeIn:
                    if (phaseTicksElapsed >= fadeInTicks)
                        TransitionTo(Phase.Sustain);
                    break;

                case Phase.Sustain:
                    // sustainTicks=-1表示永久持续，不转换
                    if (sustainTicks >= 0 && phaseTicksElapsed >= sustainTicks)
                        TransitionTo(Phase.FadeOut);
                    break;

                case Phase.FadeOut:
                    if (phaseTicksElapsed >= fadeOutTicks)
                    {
                        if (loop)
                            TransitionTo(Phase.FadeIn);
                        else
                            TransitionTo(Phase.Finished);
                    }
                    break;
            }
        }

        public float GetGlowIntensity()
        {
            switch (phase)
            {
                case Phase.FadeIn:
                    return Mathf.Clamp01(phaseTicksElapsed / (float)fadeInTicks);

                case Phase.Sustain:
                    return 1.0f;

                case Phase.FadeOut:
                    return Mathf.Clamp01(1f - phaseTicksElapsed / (float)fadeOutTicks);

                case Phase.Finished:
                    return 0f;

                default:
                    return 0f;
            }
        }

        public UnityEngine.Color? GetGlowColor() => null;

        public void ExposeData()
        {
            Scribe_Values.Look(ref phase, "fadePhase", Phase.FadeIn);
            Scribe_Values.Look(ref phaseTicksElapsed, "fadePhaseTicksElapsed", 0);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 辅助方法
        // ─────────────────────────────────────────────────────────────────────

        private void TransitionTo(Phase newPhase)
        {
            phase = newPhase;
            phaseTicksElapsed = 0;
        }
    }
}
