using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BDP.Combat
{
    /// <summary>
    /// 紧急脱离传送特效管理器。
    /// 参考RimWorld原版折跃灵能的实现。
    /// </summary>
    public static class EmergencyEscapeEffects
    {
        /// <summary>
        /// 播放传送入口特效（传送前）。
        /// </summary>
        public static void PlayEntryEffects(IntVec3 position, Map map)
        {
            // 闪光效果（入口）
            FleckMaker.Static(position, map, FleckDefOf.PsycastSkipFlashEntry, 1f);

            // 爆炸闪光（增强视觉效果）
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 0.8f);

            // 尘埃效果（青色）
            FleckMaker.ThrowDustPuffThick(position.ToVector3(), map, 1.5f, Color.cyan);

            // 播放入口音效
            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(position, map));
        }

        /// <summary>
        /// 播放传送出口特效（传送后）。
        /// </summary>
        public static void PlayExitEffects(IntVec3 position, Map map)
        {
            // 内圈维度效果
            FleckMaker.Static(position, map, FleckDefOf.PsycastSkipInnerExit, 1f);

            // 外圈光环
            FleckMaker.Static(position, map, FleckDefOf.PsycastSkipOuterRingExit, 1f);

            // 尘埃效果（青色，更浓）
            FleckMaker.ThrowDustPuffThick(position.ToVector3(), map, 2f, Color.cyan);

            // 播放出口音效
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(position, map));
        }
    }
}
