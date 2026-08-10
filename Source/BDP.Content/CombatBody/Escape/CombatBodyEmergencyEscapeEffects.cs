using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离表现辅助。
    /// 它只播放进出场表现，不改业务真值。
    /// </summary>
    internal static class CombatBodyEmergencyEscapeEffects
    {
        /// <summary>
        /// 播放紧急脱离起点表现。
        /// </summary>
        internal static void PlayEntryEffects(IntVec3 position, Map map)
        {
            if (map == null || !position.IsValid)
            {
                return;
            }

            FleckMaker.Static(position, map, FleckDefOf.PsycastSkipFlashEntry, 1f);
            FleckMaker.Static(position, map, FleckDefOf.ExplosionFlash, 0.8f);
            FleckMaker.ThrowDustPuffThick(position.ToVector3(), map, 1.5f, Color.cyan);
            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(position, map));
        }

        /// <summary>
        /// 播放紧急脱离终点表现。
        /// </summary>
        internal static void PlayExitEffects(IntVec3 position, Map map)
        {
            if (map == null || !position.IsValid)
            {
                return;
            }

            FleckMaker.Static(position, map, FleckDefOf.PsycastSkipInnerExit, 1f);
            FleckMaker.Static(position, map, FleckDefOf.PsycastSkipOuterRingExit, 1f);
            FleckMaker.ThrowDustPuffThick(position.ToVector3(), map, 2f, Color.cyan);
            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(position, map));
        }
    }
}
