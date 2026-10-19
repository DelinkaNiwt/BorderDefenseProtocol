using BDP.Core.AttackExecution;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Units.Patches
{
    /// <summary>征召接管前结束旧自主攻击；不在原版已启动玩家新命令后回头重置动作。</summary>
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
    internal static class TrionUnitAutoCombatDraftTakeoverPatch
    {
        /// <summary>仅征召从关闭转开启且当前作业属于本自动分支时才实际收口。</summary>
        private static void Prefix(Pawn ___pawn, bool value)
        {
            // 暂不求新作业，等原版写入征召状态后再走玩家命令分支，避免中途再启一次自主攻击。
            if (value && !___pawn.Drafted) ___pawn.TryGetComp<CompTrionUnit>()?.EndAutoCombatJob(startNewJob: false);
        }
    }

    /// <summary>仅给 Trion 兵接回原版玩家机械体入口，不使用监管者或失控状态。</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.IsColonyMechPlayerControlled), MethodType.Getter)]
    internal static class TrionUnitPlayerControlledPatch
    {
        /// <summary>其他角色与原版机械体全部继续原判定。</summary>
        private static bool Prefix(Pawn __instance, ref bool __result)
        {
            CompTrionUnit unit = __instance.TryGetComp<CompTrionUnit>();
            if (unit == null) return true;
            __result = __instance.Faction == Faction.OfPlayer && unit.IsOperational;
            return false;
        }
    }

    /// <summary>保留原版征召按钮，停机原因由征召准入显示。</summary>
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.ShowDraftGizmo), MethodType.Getter)]
    internal static class TrionUnitDraftGizmoPatch
    {
        /// <summary>不要求未来归属方伪造一个原版人物控制组。</summary>
        private static bool Prefix(Pawn ___pawn, ref bool __result)
        {
            if (___pawn.TryGetComp<CompTrionUnit>() == null) return true;
            __result = ___pawn.Faction == Faction.OfPlayer;
            return false;
        }
    }

    /// <summary>玩家直接征召仍受运行和原版身体失能限制。</summary>
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.CanDraftMech))]
    internal static class TrionUnitDraftAdmissionPatch
    {
        /// <summary>只替换本兵的监管者要求，不放宽其他机械体。</summary>
        private static bool Prefix(Pawn mech, ref AcceptanceReport __result)
        {
            CompTrionUnit unit = mech.TryGetComp<CompTrionUnit>();
            if (unit == null) return true;
            if (!unit.IsOperational) __result = new AcceptanceReport("BDP_TrionUnit_FunctionUnavailable".Translate());
            else if (mech.Downed || mech.InMentalState || mech.Faction != Faction.OfPlayer)
                __result = new AcceptanceReport("BDP_TrionUnit_Incapacitated".Translate());
            else __result = true;
            return false;
        }
    }

    /// <summary>在原版集中命令入口接自身资格，不复制移动或攻击菜单。</summary>
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.InMechanitorCommandRange))]
    internal static class TrionUnitCommandAdmissionPatch
    {
        /// <summary>当前默认许可仅覆盖本地图，未来条件提供者可以接外出规则。</summary>
        private static bool Prefix(Pawn mech, LocalTargetInfo target, ref bool __result)
        {
            CompTrionUnit unit = mech.TryGetComp<CompTrionUnit>();
            if (unit == null) return true;
            __result = unit.IsOperational && mech.Spawned
                && (!target.HasThing || target.Thing.MapHeld == mech.Map);
            return false;
        }
    }

    /// <summary>复用既有攻击准入截住停机后的连射，不另建攻击状态机。</summary>
    public sealed class TrionUnitAttackAdmissionRule : IAttackAdmissionRule
    {
        /// <summary>只限制带本业务组件的角色，原版仍负责目标及动作有效性。</summary>
        public AcceptanceReport Evaluate(Pawn pawn, Verb verb)
        {
            CompTrionUnit unit = pawn?.TryGetComp<CompTrionUnit>();
            return unit == null || unit.IsOperational ? AcceptanceReport.WasAccepted
                : new AcceptanceReport("BDP_TrionUnit_FunctionUnavailable".Translate());
        }
    }
}
