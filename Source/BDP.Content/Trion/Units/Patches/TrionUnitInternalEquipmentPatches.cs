using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Units.Patches
{
    /// <summary>只识别本兵原版装备容器里的内部物品，不按名称或全体触发体推断。</summary>
    internal static class TrionUnitInternalEquipment
    {
        /// <summary>内部物品身份由唯一宿主引用决定。</summary>
        internal static bool IsInternal(Thing equipment)
        {
            Pawn pawn = (equipment?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
            return equipment != null && pawn?.TryGetComp<CompTrionUnit>()?.InternalEquipment == equipment;
        }

        /// <summary>原版失去操控能力清装备时，只将固定内部承载排除在可丢弃主手之外。</summary>
        internal static ThingWithComps PrimaryForHealthCleanup(Pawn_EquipmentTracker tracker)
        {
            ThingWithComps primary = tracker.Primary;
            return IsInternal(primary) ? null : primary;
        }

        /// <summary>倒地丢弃入口可销毁普通装备；活兵的内部装配必须保留，死亡仍原版清理。</summary>
        internal static void DestroyDiscardableEquipment(Pawn_EquipmentTracker tracker, DestroyMode mode)
        {
            if (tracker.pawn.Dead || !IsInternal(tracker.Primary))
            {
                tracker.DestroyAllEquipment(mode);
                return;
            }
            foreach (ThingWithComps equipment in new List<ThingWithComps>(tracker.AllEquipmentListForReading))
                if (!IsInternal(equipment)) tracker.DestroyEquipment(equipment);
        }
    }

    /// <summary>在原版失能清理的根入口保护内置物，覆盖直接销毁与旅行队背包转移分支。</summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.CheckForStateChange))]
    internal static class TrionUnitInternalHealthCleanupPatch
    {
        /// <summary>仅替换该方法对主手的读取；伤害、致死、倒地和携带物清理均保留原逻辑。</summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo original = AccessTools.PropertyGetter(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Primary));
            MethodInfo replacement = AccessTools.Method(typeof(TrionUnitInternalEquipment), nameof(TrionUnitInternalEquipment.PrimaryForHealthCleanup));
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                    instruction.operand = replacement;
                }
                yield return instruction;
            }
        }
    }

    /// <summary>原版款型可以要求倒地销毁装备，但固定内部承载不属于可丢弃物。</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DropAndForbidEverything))]
    internal static class TrionUnitInternalDownedCleanupPatch
    {
        /// <summary>只适配清装备调用；不跳过原版掉携带物、库存和服装流程。</summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo original = AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DestroyAllEquipment));
            MethodInfo replacement = AccessTools.Method(typeof(TrionUnitInternalEquipment), nameof(TrionUnitInternalEquipment.DestroyDiscardableEquipment));
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    // 原版带一个 DestroyMode（销毁方式）参数，保留参数栈和调用签名。
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                    instruction.operand = replacement;
                }
                yield return instruction;
            }
        }
    }

    /// <summary>阻止失能、死亡和手动掉落把固定内部装配变成地面物品。</summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
    internal static class TrionUnitInternalDropPatch
    {
        /// <summary>原版死亡稍后调用组件清理；普通装备仍照常掉落。</summary>
        private static bool Prefix(ThingWithComps eq, ref ThingWithComps resultingEq, ref bool __result)
        {
            if (!TrionUnitInternalEquipment.IsInternal(eq)) return true;
            resultingEq = null;
            __result = false;
            return false;
        }
    }

    /// <summary>原版容器转移同样不能把内部承载变成可装卸物品。</summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryTransferEquipmentToContainer))]
    internal static class TrionUnitInternalTransferPatch
    {
        /// <summary>只拒绝这个兵已持有的内部物品，普通装备照常转移。</summary>
        private static bool Prefix(ThingWithComps eq, ref bool __result)
        {
            if (!TrionUnitInternalEquipment.IsInternal(eq)) return true;
            __result = false;
            return false;
        }
    }

    /// <summary>固定装配不提供玩家替换路径，款型能力由预载配置决定。</summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
    internal static class TrionUnitInternalReplacePatch
    {
        /// <summary>仅已有内部装配的兵拒绝别的装备；首次建立仍走原版添加。</summary>
        private static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            CompTrionUnit unit = __instance.pawn.TryGetComp<CompTrionUnit>();
            return unit?.InternalEquipment == null || unit.InternalEquipment == newEq;
        }
    }

    /// <summary>装备页不列内部物品，正式攻击与能力按钮仍由已有设施提供。</summary>
    [HarmonyPatch(typeof(ITab_Pawn_Gear), "DrawThingRow")]
    internal static class TrionUnitInternalGearRowPatch
    {
        /// <summary>不改变普通装备页内容或行高计算。</summary>
        private static bool Prefix(Thing thing) => !TrionUnitInternalEquipment.IsInternal(thing);
    }
}
