using UnityEngine;
using Verse;

namespace BDP.Content.FloatingFlight.Presentation
{
    /// <summary>把浮空状态账本转换为地图绘制偏移，不改变 Pawn 的真实位置。</summary>
    public static class FloatingFlightPresentationUtility
    {
        /// <summary>读取当前 Pawn 的归一化浮空表现因子。</summary>
        public static float GetPresentationFactor(Pawn pawn)
        {
            if (pawn == null
                || pawn.Map == null
                || !pawn.Spawned
                || pawn.Dead
                || pawn.Downed
                || !FloatingFlightLedgerService.TryGetRecord(
                    pawn,
                    out FloatingFlightRecord record))
            {
                return 0f;
            }

            return FloatingFlightStateController.GetPresentationFactor(
                pawn,
                record,
                FloatingFlightStateController.CurrentTick);
        }

        /// <summary>读取当前 Pawn 的完整绘制偏移。</summary>
        public static Vector3 GetOffset(Pawn pawn)
        {
            float factor = GetPresentationFactor(pawn);
            if (factor <= 0f)
            {
                return Vector3.zero;
            }

            FloatingFlightSettingsExtension settings =
                FloatingFlightDefOf.BDP_Hediff_FloatingFlight?.GetModExtension<FloatingFlightSettingsExtension>()
                ?? new FloatingFlightSettingsExtension();
            float bob = 0f;
            if (settings.bobAmplitude != 0f && settings.bobPeriodTicks > 0)
            {
                float phase = FloatingFlightStateController.CurrentTick
                    / (float)settings.bobPeriodTicks
                    * Mathf.PI * 2f;
                bob = Mathf.Sin(phase + GetBobPhase(pawn))
                    * settings.bobAmplitude
                    * factor;
            }

            float z = settings.presentationHeight * factor;
            return new Vector3(0f, 0.03658537f * factor, z + bob);
        }

        /// <summary>
        /// 按 Pawn 稳定唯一编号生成不持久化的独立起伏相位。
        /// 不在绘制过程中调用随机数，保证同一 Pawn 读档后相位稳定。
        /// </summary>
        private static float GetBobPhase(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0f;
            }

            float normalized = Mathf.Repeat(pawn.thingIDNumber * 0.61803398875f, 1f);
            return normalized * Mathf.PI * 2f;
        }
    }
}
