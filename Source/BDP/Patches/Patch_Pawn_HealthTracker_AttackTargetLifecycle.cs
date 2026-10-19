using BDP.Core.AttackExecution;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// 统一读取原版 Pawn_HealthTracker（角色健康状态管理器）当前所属 Pawn。
    /// </summary>
    internal static class PawnHealthTrackerTargetLifecycleAccess
    {
        /// <summary>
        /// 原版 Pawn_HealthTracker 的私有 Pawn 字段访问器。
        /// </summary>
        internal static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> PawnAccessor =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");
    }

    /// <summary>
    /// 在 Pawn 确实从可行动状态进入倒地状态后，通知 BDP 收口自动远程会话。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_Pawn_HealthTracker_MakeDowned_AttackTargetLifecycle
    {
        /// <summary>
        /// 记录进入原版倒地流程前的状态，避免重复调用伪造一次生命周期变化。
        /// </summary>
        public static void Prefix(Pawn_HealthTracker __instance, out bool __state)
        {
            Pawn pawn = PawnHealthTrackerTargetLifecycleAccess.PawnAccessor(__instance);
            __state = pawn?.Downed == true;
        }

        /// <summary>
        /// 倒地流程完成后只通知真实的可行动到倒地转换。
        /// </summary>
        public static void Postfix(Pawn_HealthTracker __instance, bool __state)
        {
            Pawn pawn = PawnHealthTrackerTargetLifecycleAccess.PawnAccessor(__instance);
            if (!__state && pawn?.Downed == true)
            {
                AttackTargetLifecycleAccess.NotifyTargetLifecycleChanged(pawn);
            }
        }
    }

    /// <summary>
    /// 在 Pawn 确实进入死亡状态后，通知 BDP 收口自动远程会话。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "SetDead")]
    public static class Patch_Pawn_HealthTracker_SetDead_AttackTargetLifecycle
    {
        /// <summary>
        /// 记录进入原版死亡状态前的状态，避免重复 SetDead 产生重复通知。
        /// </summary>
        public static void Prefix(Pawn_HealthTracker __instance, out bool __state)
        {
            Pawn pawn = PawnHealthTrackerTargetLifecycleAccess.PawnAccessor(__instance);
            __state = pawn?.Dead == true;
        }

        /// <summary>
        /// 死亡状态写入后只通知真实的生存到死亡转换。
        /// </summary>
        public static void Postfix(Pawn_HealthTracker __instance, bool __state)
        {
            Pawn pawn = PawnHealthTrackerTargetLifecycleAccess.PawnAccessor(__instance);
            if (!__state && pawn?.Dead == true)
            {
                AttackTargetLifecycleAccess.NotifyTargetLifecycleChanged(pawn);
            }
        }
    }

    /// <summary>
    /// 在 Pawn 解除倒地后通知 BDP，覆盖读档后未观察到原始倒地事件的情况。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeUndowned")]
    public static class Patch_Pawn_HealthTracker_MakeUndowned_AttackTargetLifecycle
    {
        /// <summary>
        /// 记录进入原版解除倒地流程前的状态。
        /// </summary>
        public static void Prefix(Pawn_HealthTracker __instance, out bool __state)
        {
            Pawn pawn = PawnHealthTrackerTargetLifecycleAccess.PawnAccessor(__instance);
            __state = pawn?.Downed == true;
        }

        /// <summary>
        /// 解除倒地完成后通知对应的自动远程宿主重新起手。
        /// </summary>
        public static void Postfix(Pawn_HealthTracker __instance, bool __state)
        {
            Pawn pawn = PawnHealthTrackerTargetLifecycleAccess.PawnAccessor(__instance);
            if (__state && pawn != null && !pawn.Downed)
            {
                AttackTargetLifecycleAccess.NotifyTargetLifecycleChanged(pawn);
            }
        }
    }

    /// <summary>
    /// 在 Pawn 复活完成后通知 BDP，覆盖死亡 Pawn 重新生成前后的会话残留。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.Notify_Resurrected))]
    public static class Patch_Pawn_HealthTracker_NotifyResurrected_AttackTargetLifecycle
    {
        /// <summary>
        /// 复活健康状态写入完成后通知对应的自动远程宿主重新起手。
        /// </summary>
        public static void Postfix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = PawnHealthTrackerTargetLifecycleAccess.PawnAccessor(__instance);
            AttackTargetLifecycleAccess.NotifyTargetLifecycleChanged(pawn);
        }
    }
}
