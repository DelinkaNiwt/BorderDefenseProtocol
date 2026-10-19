using UnityEngine;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// 通用自施放能力的前摇姿态。
    ///
    /// 复用原版 Stance_Warmup 的计时、音效、Effecter、目标校验和完成时序，
    /// 只替换自施放所需的两项表现：不瞄准武器，以及在施法者中心绘制圆形进度指示。
    /// </summary>
    public class BdpStance_SelfCastWarmup : Stance_Warmup
    {
        /// <summary>
        /// 自施放前摇圆形指示器的最大半径，沿用原版冷却圆的视觉尺寸。
        /// </summary>
        private const float WarmupCircleMaxRadius = 0.5f;

        /// <summary>
        /// 创建用于读档的空姿态实例。
        /// </summary>
        public BdpStance_SelfCastWarmup()
        {
        }

        /// <summary>
        /// 创建自施放前摇姿态，并明确要求原版不要把施法者的武器当作瞄准武器绘制。
        /// </summary>
        public BdpStance_SelfCastWarmup(int ticks, LocalTargetInfo focusTarg, Verb verb)
            : base(ticks, focusTarg, verb)
        {
            totalTicks = ticks;
            neverAimWeapon = true;
        }

        /// <summary>
        /// 保存前摇总时长，使读档后圆形指示进度仍能按原始前摇上限计算。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref totalTicks, "bdpSelfCastWarmupTotalTicks", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && totalTicks <= 0)
            {
                totalTicks = ticksLeft;
            }
        }

        /// <summary>
        /// 绘制以施法者为中心的剩余前摇圆形指示器，不调用原版扇形瞄准指示器。
        /// </summary>
        public override void StanceDraw()
        {
            if (stanceTracker == null || !Find.Selector.IsSelected(stanceTracker.pawn))
            {
                return;
            }

            float remainingRatio = totalTicks > 0
                ? Mathf.Clamp01((float)ticksLeft / totalTicks)
                : 0f;
            float radius = WarmupCircleMaxRadius * remainingRatio;
            if (radius <= 0f)
            {
                return;
            }

            GenDraw.DrawCooldownCircle(
                stanceTracker.pawn.Drawer.DrawPos + new Vector3(0f, 0.2f, 0f),
                radius);
        }

        /// <summary>
        /// 自施放姿态保存的前摇总时长，用于显示圆形指示的剩余比例。
        /// </summary>
        private int totalTicks;
    }
}
