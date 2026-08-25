using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.LightSoul.Patches
{
    /// <summary>
    /// 让举盾注视警戒沿用原版攻击瞄准的位置和连续角度，但不建立攻击忙碌姿态。
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
    [HarmonyPriority(Priority.First)]
    public static class Patch_PawnRenderUtility_DrawEquipmentAiming_LightSoulGuardWatch
    {
        /// <summary>
        /// 原版朝北公开持械相对人物装备绘制基点的偏移。
        /// </summary>
        private static readonly Vector3 CarriedOffsetNorth = new Vector3(0f, 0f, -0.11f);

        /// <summary>
        /// 原版朝东公开持械相对人物装备绘制基点的偏移。
        /// </summary>
        private static readonly Vector3 CarriedOffsetEast = new Vector3(0.22f, 0f, -0.22f);

        /// <summary>
        /// 原版朝南公开持械相对人物装备绘制基点的偏移。
        /// </summary>
        private static readonly Vector3 CarriedOffsetSouth = new Vector3(0f, 0f, -0.22f);

        /// <summary>
        /// 原版朝西公开持械相对人物装备绘制基点的偏移。
        /// </summary>
        private static readonly Vector3 CarriedOffsetWest = new Vector3(-0.22f, 0f, -0.22f);

        /// <summary>
        /// 在 BDP 通用视觉采样前，把公开持械参数替换为当前警戒目标对应的原版瞄准参数。
        /// 参数按引用改写，因而宿主装备、单武器替换和双武器条目会共同使用同一姿态基准。
        /// </summary>
        public static void Prefix(Thing eq, ref Vector3 drawLoc, ref float aimAngle)
        {
            Pawn pawn = ResolveOwnerPawn(eq);
            Verb_LightSoulGuardWatch watchVerb = LightSoulGuardWatchUtility.ResolveVerb(pawn);
            if (pawn == null
                || eq?.def == null
                || watchVerb == null
                || !watchVerb.TryGetCurrentWatchTarget(out LocalTargetInfo target))
            {
                return;
            }

            Vector3 targetDrawPos = target.HasThing
                ? target.Thing.DrawPos
                : target.Cell.ToVector3Shifted();
            Vector3 targetDirection = targetDrawPos - pawn.DrawPos;
            if (targetDirection.MagnitudeHorizontalSquared() <= 0.001f)
            {
                return;
            }

            aimAngle = targetDirection.AngleFlat();
            float distanceFactor = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
            Vector3 baseDrawLoc = drawLoc - ResolveCarriedOffset(pawn.Rotation) * distanceFactor;
            Vector3 aimingOffset = new Vector3(
                0f,
                0f,
                0.4f + eq.def.equippedDistanceOffset).RotatedBy(aimAngle) * distanceFactor;
            drawLoc = baseDrawLoc + aimingOffset;
        }

        /// <summary>
        /// 解析当前装备所属人物。
        /// </summary>
        private static Pawn ResolveOwnerPawn(Thing equipment)
        {
            if (equipment?.ParentHolder is Pawn_EquipmentTracker equipmentTracker)
            {
                return equipmentTracker.pawn;
            }

            return equipment?.ParentHolder?.ParentHolder as Pawn
                ?? equipment?.TryGetComp<CompEquippable>()?.PrimaryVerb?.CasterPawn;
        }

        /// <summary>
        /// 读取原版 DrawCarriedWeapon 对当前四向应用的持械偏移。
        /// </summary>
        private static Vector3 ResolveCarriedOffset(Rot4 facing)
        {
            switch (facing.AsInt)
            {
                case 0:
                    return CarriedOffsetNorth;
                case 1:
                    return CarriedOffsetEast;
                case 2:
                    return CarriedOffsetSouth;
                case 3:
                    return CarriedOffsetWest;
                default:
                    return Vector3.zero;
            }
        }
    }
}
