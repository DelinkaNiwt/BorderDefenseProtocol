using System;
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
        /// 判断当前激活态战斗体是否满足原版的致死核心部位或致死能力条件。
        /// </summary>
        internal static bool ShouldCollapseFromVanillaDeathCondition(Pawn pawn)
        {
            if (pawn == null
                || !IsActivePhase(pawn)
                || !HasCurrentPrimaryTrigger(pawn)
                || pawn.health?.hediffSet == null
                || pawn.RaceProps?.body == null)
            {
                return false;
            }

            BodyPartRecord corePart = pawn.RaceProps.body.corePart;
            if (corePart != null
                && PawnCapacityUtility.CalculatePartEfficiency(pawn.health.hediffSet, corePart) <= 0.0001f)
            {
                return true;
            }

            List<PawnCapacityDef> capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading;
            for (int i = 0; i < capacities.Count; i++)
            {
                PawnCapacityDef capacity = capacities[i];
                bool lethalForPawn = pawn.RaceProps.IsFlesh
                    ? capacity.lethalFlesh
                    : capacity.lethalMechanoids;
                if (lethalForPawn && !pawn.health.capacities.CapableOf(capacity))
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

            __state.Restore();

            if (__state.CollapseAfterDowned
                && !__state.WasDowned
                && __state.Pawn?.Downed == true)
            {
                CombatBodySurfaceAccess.ResolveCommands(__state.Pawn)?.TriggerCollapse("PawnDowned");
            }
        }

        /// <summary>原版或其它补丁异常退出时也恢复当前调用，保留原始异常。</summary>
        public static Exception Finalizer(DownedTransitionProtectionState __state, Exception __exception)
        {
            __state?.Restore();
            return __exception;
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

            /// <summary>正常与异常出口共用清理；一次状态只能退出自己进入的那一层。</summary>
            public void Restore()
            {
                if (!PreserveTrigger) return;
                CombatBodyTriggerProtectionContext.Exit(Pawn);
                PreserveTrigger = false;
            }
        }
    }

    /// <summary>
    /// 在原版状态检查期间保护战斗体及触发体，正常倒地仍交给既有崩解接线。
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
        /// 保护期间临时关闭倒地随机致死，并为操控能力丧失建立触发体保护上下文。
        /// 原版遇到 preventsDeath 会改为摘除脑部；脑部已缺失时会产生空部位并打断倒地流程。
        /// </summary>
        public static void Prefix(Pawn_HealthTracker __instance, out HealthCheckProtectionState __state)
        {
            Pawn pawn = pawnAccessor(__instance);
            __state = null;
            if (!CombatBodyTriggerProtectionUtility.IsProtectionPhase(pawn))
            {
                return;
            }

            __state = new HealthCheckProtectionState
            {
                Pawn = pawn,
                OriginalDeathOnDownedChance = __instance.overrideDeathOnDownedChance
            };
            __instance.overrideDeathOnDownedChance = 0f;
            __state.PreserveTrigger = CombatBodyTriggerProtectionUtility.ShouldProtectManipulationLoss(pawn);
            if (__state.PreserveTrigger)
            {
                CombatBodyTriggerProtectionContext.Enter(pawn);
            }
        }

        /// <summary>
        /// 恢复当前健康检查的原版设置，再执行原版致死状态崩解检查。
        /// </summary>
        public static void Postfix(Pawn_HealthTracker __instance, HealthCheckProtectionState __state)
        {
            Pawn pawn = pawnAccessor(__instance);
            __state?.Restore(__instance);

            if (CombatBodyTriggerProtectionUtility.ShouldCollapseFromVanillaDeathCondition(pawn))
            {
                CombatBodySurfaceAccess.ResolveCommands(pawn)?.TriggerCollapse("FatalBodyCondition");
            }
        }

        /// <summary>异常退出也清理本次保护；保留原始异常，不屏蔽真正的健康错误。</summary>
        public static Exception Finalizer(
            Pawn_HealthTracker __instance,
            HealthCheckProtectionState __state,
            Exception __exception)
        {
            __state?.Restore(__instance);
            return __exception;
        }

        /// <summary>各层健康检查独立保存原值，嵌套调用按退出顺序恢复，不写入存档。</summary>
        public sealed class HealthCheckProtectionState
        {
            /// <summary>当前健康检查所属人物。</summary>
            public Pawn Pawn;

            /// <summary>当前调用是否进入了触发体保护上下文。</summary>
            public bool PreserveTrigger;

            /// <summary>空表示本次未覆盖原版概率；负数默认值同样需要原样恢复。</summary>
            public float? OriginalDeathOnDownedChance;

            /// <summary>正常与异常出口共用清理，重复调用无效果。</summary>
            public void Restore(Pawn_HealthTracker health)
            {
                if (OriginalDeathOnDownedChance.HasValue)
                {
                    health.overrideDeathOnDownedChance = OriginalDeathOnDownedChance.Value;
                    OriginalDeathOnDownedChance = null;
                }

                if (PreserveTrigger)
                {
                    CombatBodyTriggerProtectionContext.Exit(Pawn);
                    PreserveTrigger = false;
                }
            }
        }
    }

    /// <summary>
    /// 原版倒地销毁装备会先卸下并销毁触发体；在该回调中先切入崩解，阻止触发体走普通解除。
    /// </summary>
    [HarmonyPatch(typeof(CompTriggerBody), nameof(CompTriggerBody.Notify_Unequipped))]
    public static class Patch_CompTriggerBody_Notify_Unequipped_DownedCombatBodyCollapse
    {
        /// <summary>
        /// 倒地清理上下文内若触发体即将被原版销毁，则先进入崩解，再让原版完成装备销毁。
        /// </summary>
        public static void Prefix(Pawn pawn)
        {
            if (pawn?.Downed != true
                || !CombatBodyTriggerProtectionContext.Contains(pawn)
                || !CombatBodyTriggerProtectionUtility.IsActivePhase(pawn))
            {
                return;
            }

            CombatBodySurfaceAccess.ResolveCommands(pawn)?.TriggerCollapse("PawnDowned");
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
        public static void Prefix(Pawn_EquipmentTracker __instance, out SpawnProtectionState __state)
        {
            Pawn pawn = __instance?.pawn;
            __state = null;
            if (!CombatBodyTriggerProtectionUtility.ShouldProtectDownedSpawn(pawn)) return;
            __state = new SpawnProtectionState { Pawn = pawn, PreserveTrigger = true };
            CombatBodyTriggerProtectionContext.Enter(pawn);
        }

        /// <summary>
        /// 结束倒地生成补偿的临时保护上下文。
        /// </summary>
        public static void Postfix(SpawnProtectionState __state)
        {
            __state?.Restore();
        }

        /// <summary>异常出口恢复进入时保存的人物，不能再次读取可能已改变的装备宿主。</summary>
        public static Exception Finalizer(SpawnProtectionState __state, Exception __exception)
        {
            __state?.Restore();
            return __exception;
        }

        /// <summary>保存当前出生补偿实际进入的保护层，不写入存档。</summary>
        public sealed class SpawnProtectionState
        {
            /// <summary>本次实际进入保护的人物。</summary>
            public Pawn Pawn;

            /// <summary>尚未恢复的当前保护层。</summary>
            public bool PreserveTrigger;

            /// <summary>只退出一次，允许正常后置与最终处理依次执行。</summary>
            public void Restore()
            {
                if (!PreserveTrigger) return;
                CombatBodyTriggerProtectionContext.Exit(Pawn);
                PreserveTrigger = false;
            }
        }
    }

    /// <summary>
    /// 补齐操控丧失时直接销毁装备的原版分支；它不会经过尝试掉落入口。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DestroyEquipment))]
    public static class Patch_Pawn_EquipmentTracker_DestroyEquipment_CombatBodyTriggerProtection
    {
        /// <summary>
        /// 仅在操控丧失清理上下文内保留当前主手触发体，阻止移除触发的普通解除。
        /// 尚未倒地是必要条件：倒地销毁仍交给既有倒地崩解接线。
        /// </summary>
        public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            Pawn pawn = __instance?.pawn;
            return !CombatBodyTriggerProtectionContext.Contains(pawn)
                || !CombatBodyTriggerProtectionUtility.IsCurrentPrimaryTrigger(pawn, eq)
                || !CombatBodyTriggerProtectionUtility.ShouldProtectManipulationLoss(pawn);
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
