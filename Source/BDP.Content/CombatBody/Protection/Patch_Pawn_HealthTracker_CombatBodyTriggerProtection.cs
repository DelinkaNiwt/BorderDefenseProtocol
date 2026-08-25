using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Core.Trigger;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.CombatBody.Protection
{
    /// <summary>
    /// 当前 Pawn 是否正处于 BDP 触发体保护调用上下文。
    /// </summary>
    internal static class CombatBodyTriggerProtectionContext
    {
        /// <summary>
        /// 按 Pawn 记录嵌套保护调用深度。
        /// </summary>
        private static readonly Dictionary<Pawn, int> protectedPawnDepth =
            new Dictionary<Pawn, int>();

        /// <summary>
        /// 进入指定 Pawn 的触发体保护上下文。
        /// </summary>
        internal static void Enter(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (protectedPawnDepth.TryGetValue(pawn, out int depth))
            {
                protectedPawnDepth[pawn] = depth + 1;
                return;
            }

            protectedPawnDepth[pawn] = 1;
        }

        /// <summary>
        /// 退出指定 Pawn 的触发体保护上下文。
        /// </summary>
        internal static void Exit(Pawn pawn)
        {
            if (pawn == null || !protectedPawnDepth.TryGetValue(pawn, out int depth))
            {
                return;
            }

            if (depth <= 1)
            {
                protectedPawnDepth.Remove(pawn);
                return;
            }

            protectedPawnDepth[pawn] = depth - 1;
        }

        /// <summary>
        /// 判断指定 Pawn 当前是否位于目标原版清理调用内。
        /// </summary>
        internal static bool Contains(Pawn pawn)
        {
            return pawn != null && protectedPawnDepth.ContainsKey(pawn);
        }
    }

    /// <summary>
    /// BDP 触发体保护条件读取工具。
    /// </summary>
    internal static class CombatBodyTriggerProtectionUtility
    {
        /// <summary>
        /// 判断当前战斗体是否处于触发体保护阶段。
        /// </summary>
        internal static bool IsProtectionPhase(Pawn pawn)
        {
            ICombatBodyReader reader = CombatBodySurfaceAccess.ResolveReader(pawn);
            return reader != null
                && (reader.Phase == CombatBodyPhase.Active
                    || reader.Phase == CombatBodyPhase.Collapsing);
        }

        /// <summary>
        /// 判断当前战斗体是否处于激活阶段。
        /// </summary>
        internal static bool IsActivePhase(Pawn pawn)
        {
            ICombatBodyReader reader = CombatBodySurfaceAccess.ResolveReader(pawn);
            return reader != null && reader.Phase == CombatBodyPhase.Active;
        }

        /// <summary>
        /// 判断当前激活态 BDP 触发体是否已经缺失原版心脏。
        /// </summary>
        internal static bool ShouldCollapseFromHeartMissing(Pawn pawn)
        {
            if (pawn == null
                || !IsActivePhase(pawn)
                || !HasCurrentPrimaryTrigger(pawn)
                || pawn.health?.hediffSet == null
                || pawn.RaceProps?.body == null)
            {
                return false;
            }

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part.def == BodyPartDefOf.Heart && pawn.health.hediffSet.PartIsMissing(part))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断装备是否是 Pawn 当前主装备上的 BDP 触发体。
        /// </summary>
        internal static bool IsCurrentPrimaryTrigger(Pawn pawn, ThingWithComps equipment)
        {
            return pawn?.equipment?.Primary == equipment
                && equipment?.TryGetComp<CompTriggerBody>() != null;
        }

        /// <summary>
        /// 判断 Pawn 当前是否拥有 BDP 触发体主装备。
        /// </summary>
        internal static bool HasCurrentPrimaryTrigger(Pawn pawn)
        {
            return IsCurrentPrimaryTrigger(pawn, pawn?.equipment?.Primary);
        }

        /// <summary>
        /// 判断 Pawn 是否需要保护操控能力丧失路径中的触发体。
        /// </summary>
        internal static bool ShouldProtectManipulationLoss(Pawn pawn)
        {
            return pawn != null
                && !pawn.Downed
                && IsProtectionPhase(pawn)
                && HasCurrentPrimaryTrigger(pawn)
                && pawn.health != null
                && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
        }

        /// <summary>
        /// 判断 Pawn 是否需要保护倒地出生补偿路径中的触发体。
        /// </summary>
        internal static bool ShouldProtectDownedSpawn(Pawn pawn)
        {
            return pawn != null
                && pawn.Downed
                && !pawn.GetPosture().InBed()
                && IsProtectionPhase(pawn)
                && HasCurrentPrimaryTrigger(pawn);
        }
    }

    /// <summary>
    /// 在原版倒地清装备期间保护 BDP 触发体，并在激活态倒地后请求崩解。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_Pawn_HealthTracker_MakeDowned_CombatBodyTriggerProtection
    {
        /// <summary>
        /// Pawn_HealthTracker 私有 Pawn 字段访问器。
        /// </summary>
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> pawnAccessor =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        /// <summary>
        /// 为倒地原版清理建立临时保护上下文。
        /// </summary>
        public static void Prefix(
            Pawn_HealthTracker __instance,
            out DownedTransitionProtectionState __state)
        {
            Pawn pawn = pawnAccessor(__instance);
            __state = new DownedTransitionProtectionState
            {
                Pawn = pawn,
                WasDowned = pawn?.Downed == true,
                PreserveTrigger = CombatBodyTriggerProtectionUtility.IsProtectionPhase(pawn)
                    && CombatBodyTriggerProtectionUtility.HasCurrentPrimaryTrigger(pawn),
                CollapseAfterDowned = pawn != null
                    && !pawn.Downed
                    && CombatBodyTriggerProtectionUtility.IsActivePhase(pawn)
            };

            if (__state.PreserveTrigger)
            {
                CombatBodyTriggerProtectionContext.Enter(pawn);
            }
        }

        /// <summary>
        /// 结束倒地清理保护，并在激活态确实进入倒地后立即请求崩解。
        /// </summary>
        public static void Postfix(DownedTransitionProtectionState __state)
        {
            if (__state == null)
            {
                return;
            }

            if (__state.PreserveTrigger)
            {
                CombatBodyTriggerProtectionContext.Exit(__state.Pawn);
            }

            if (__state.CollapseAfterDowned
                && !__state.WasDowned
                && __state.Pawn?.Downed == true)
            {
                CombatBodySurfaceAccess.ResolveCommands(__state.Pawn)?.TriggerCollapse("PawnDowned");
            }
        }

        /// <summary>
        /// 倒地转换期间需要保留的临时状态。
        /// </summary>
        public sealed class DownedTransitionProtectionState
        {
            /// <summary>
            /// 当前倒地 Pawn。
            /// </summary>
            public Pawn Pawn;

            /// <summary>
            /// 进入补丁前是否已经倒地。
            /// </summary>
            public bool WasDowned;

            /// <summary>
            /// 是否建立了触发体保护上下文。
            /// </summary>
            public bool PreserveTrigger;

            /// <summary>
            /// 是否应在倒地完成后请求崩解。
            /// </summary>
            public bool CollapseAfterDowned;
        }
    }

    /// <summary>
    /// 在操控能力丧失的原版状态检查期间保护 BDP 触发体。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.CheckForStateChange))]
    public static class Patch_Pawn_HealthTracker_CheckForStateChange_CombatBodyTriggerProtection
    {
        /// <summary>
        /// Pawn_HealthTracker 私有 Pawn 字段访问器。
        /// </summary>
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> pawnAccessor =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        /// <summary>
        /// 在操控能力丧失分支执行前建立临时保护上下文。
        /// </summary>
        public static void Prefix(Pawn_HealthTracker __instance, out bool __state)
        {
            Pawn pawn = pawnAccessor(__instance);
            __state = CombatBodyTriggerProtectionUtility.ShouldProtectManipulationLoss(pawn);
            if (__state)
            {
                CombatBodyTriggerProtectionContext.Enter(pawn);
            }
        }

        /// <summary>
        /// 结束操控能力丧失分支的临时保护上下文。
        /// </summary>
        public static void Postfix(Pawn_HealthTracker __instance, bool __state)
        {
            Pawn pawn = pawnAccessor(__instance);
            if (__state)
            {
                CombatBodyTriggerProtectionContext.Exit(pawn);
            }

            if (CombatBodyTriggerProtectionUtility.ShouldCollapseFromHeartMissing(pawn))
            {
                CombatBodySurfaceAccess.ResolveCommands(pawn)?.TriggerCollapse("HeartMissing");
            }
        }
    }

    /// <summary>
    /// 在倒地小人生成补偿期间保护 BDP 触发体。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_PawnSpawned))]
    public static class Patch_Pawn_EquipmentTracker_Notify_PawnSpawned_CombatBodyTriggerProtection
    {
        /// <summary>
        /// 在原版倒地生成补偿执行前建立临时保护上下文。
        /// </summary>
        public static void Prefix(Pawn_EquipmentTracker __instance, out bool __state)
        {
            Pawn pawn = __instance?.pawn;
            __state = CombatBodyTriggerProtectionUtility.ShouldProtectDownedSpawn(pawn);
            if (__state)
            {
                CombatBodyTriggerProtectionContext.Enter(pawn);
            }
        }

        /// <summary>
        /// 结束倒地生成补偿的临时保护上下文。
        /// </summary>
        public static void Postfix(Pawn_EquipmentTracker __instance, bool __state)
        {
            if (__state)
            {
                CombatBodyTriggerProtectionContext.Exit(__instance?.pawn);
            }
        }
    }

    /// <summary>
    /// 在原版实际移出装备前保护当前 BDP 触发体。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
    public static class Patch_Pawn_EquipmentTracker_TryDropEquipment_CombatBodyTriggerProtection
    {
        /// <summary>
        /// 在目标原版清理上下文内把触发体掉落转换为成功但不移动装备的空操作。
        /// </summary>
        public static bool Prefix(
            Pawn_EquipmentTracker __instance,
            ThingWithComps eq,
            ref ThingWithComps resultingEq,
            ref bool __result)
        {
            Pawn pawn = __instance?.pawn;
            if (!CombatBodyTriggerProtectionContext.Contains(pawn)
                || !CombatBodyTriggerProtectionUtility.IsProtectionPhase(pawn)
                || !CombatBodyTriggerProtectionUtility.IsCurrentPrimaryTrigger(pawn, eq))
            {
                return true;
            }

            resultingEq = null;
            __result = true;
            return false;
        }
    }
}
