using HarmonyLib;
using Verse;
using RimWorld;
using BDP.Core;

namespace BDP.Combat
{
    /// <summary>
    /// 装备限制Patch集合。
    ///
    /// 问题背景：
    /// - 战斗体激活时，触发体被Patch_EquipmentTracker_TryDropEquipment拦截掉落
    /// - 但原版装备流程：MakeRoomFor失败 → AddEquipment仍被调用 → 双主武器错误
    ///
    /// 解决方案：
    /// - 主防线：CanEquip拦截，在装备任务开始前就阻止
    /// - 备用防线：AddEquipment拦截，防止双主武器错误
    ///
    /// 设计原则：
    /// - 战斗体激活时，触发体是核心装备，不允许更换
    /// - 解除战斗体后，恢复正常装备功能
    /// </summary>

    // ═══════════════════════════════════════════
    //  主防线：CanEquip拦截
    // ═══════════════════════════════════════════

    /// <summary>
    /// 拦截装备检查，战斗体激活时禁止装备其他武器。
    ///
    /// 时机：装备任务开始前
    /// 效果：给出明确的错误提示，防止任务开始
    ///
    /// 注意：CanEquip有多个重载，必须明确指定参数类型避免模糊匹配
    /// </summary>
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip",
        new[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    public static class Patch_EquipmentUtility_CanEquip
    {
        static void Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
        {
            // 已经被其他检查拒绝，跳过
            if (!__result) return;

            // 检查战斗体是否激活
            var runtime = CombatBodyRuntime.Of(pawn);
            if (runtime == null || !runtime.IsActive) return;

            // 检查当前是否装备着触发体
            var currentEquipment = pawn.equipment?.Primary;
            if (currentEquipment == null) return;

            bool isTriggerBody = CombatBodyQuery.FindCombatBodySupport(currentEquipment) != null;
            if (!isTriggerBody) return;

            // 战斗体激活时，禁止装备其他武器
            cantReason = "战斗体激活中，无法更换装备";
            __result = false;
        }
    }

    // ═══════════════════════════════════════════
    //  备用防线：AddEquipment拦截
    // ═══════════════════════════════════════════

    /// <summary>
    /// 拦截装备添加，防止双主武器错误。
    ///
    /// 时机：装备即将被添加时（最后一道防线）
    /// 效果：确保不会出现双主武器，处理边界情况
    /// </summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "AddEquipment")]
    public static class Patch_EquipmentTracker_AddEquipment
    {
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            var pawn = __instance.pawn;
            var currentPrimary = __instance.Primary;

            // 如果当前有主武器，且新装备也是主武器
            if (currentPrimary != null && newEq != null)
            {
                // 检查当前主武器是否为触发体
                bool isTriggerBody = CombatBodyQuery.FindCombatBodySupport(currentPrimary) != null;
                if (!isTriggerBody) return true;

                // 检查战斗体是否激活
                var runtime = CombatBodyRuntime.Of(pawn);
                if (runtime == null || !runtime.IsActive) return true;

                // 战斗体激活时，阻止添加新武器
                Log.Error($"[BDP] 阻止装备冲突: {pawn.LabelShort} 战斗体激活中，无法装备 {newEq.LabelShortCap}");
                Messages.Message(
                    $"{pawn.LabelShort} 战斗体激活中，无法更换装备",
                    MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }
    }
}
