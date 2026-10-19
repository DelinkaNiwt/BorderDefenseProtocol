using BDP.Core.Genes;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using HarmonyLib;
using Verse;

namespace BDP.Patches
{
    /// <summary>
    /// equipped Trigger formal host 生命周期桥。
    /// internal formal host 已经从原版 VerbTracker 分家，因此持续 burst 所需的 VerbTick 必须接回 pawn 的装备主链。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.EquipmentTrackerTick))]
    public static class Patch_Pawn_EquipmentTracker_EquipmentTrackerTick
    {
        /// <summary>
        /// 在原版装备生命周期推进后，只推进当前主武器上的 Trigger 运行时。
        /// 这里把 post-load finalize、正式投影发布和 formal host tick 统一收口到 RuntimeTick，不再扫描全部装备。
        /// </summary>
        public static void Postfix(Pawn_EquipmentTracker __instance)
        {
            TriggerBodyEquipmentEligibility.ValidateAfterLoadOrTick(__instance?.pawn);

            ThingWithComps primaryEquipment = __instance?.Primary;
            if (primaryEquipment == null)
            {
                return;
            }

            CompTriggerBody triggerBody = primaryEquipment?.TryGetComp<CompTriggerBody>();
            if (triggerBody != null
                && TriggerBodyEquipmentEligibility.CanEquip(primaryEquipment, __instance?.pawn))
            {
                TriggerTrionCapacityBindingRegistry.NotifyEquipped(__instance.pawn, triggerBody);
            }

            triggerBody?.RuntimeTick();
        }
    }
}
