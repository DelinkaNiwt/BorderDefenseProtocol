using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Genes;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体装备资格与失效清理服务。
    /// 它只负责通用的腺体资格和物品去向，不承载黑触发器容量业务。
    /// </summary>
    public static class TriggerBodyEquipmentEligibility
    {
        /// <summary>
        /// 正在执行失效清理的 Pawn 集合，用于压住装备通知嵌套。
        /// </summary>
        private static readonly HashSet<Pawn> handlingPawns = new HashSet<Pawn>();

        /// <summary>
        /// 判断指定 Thing 是否可以由 Pawn 装备。
        /// 非触发体保持原版规则；触发体必须拥有激活腺体。
        /// </summary>
        public static bool CanEquip(Thing thing, Pawn pawn)
        {
            ThingWithComps equipment = thing as ThingWithComps;
            CompTriggerBody trigger = equipment?.TryGetComp<CompTriggerBody>();
            return trigger == null || !((CompProperties_TriggerBody)trigger.props).requiresTrionGland
                || TrionGlandEligibility.HasActiveTrionGland(pawn);
        }

        /// <summary>
        /// 在装备通知入口再次校验资格。
        /// 返回 true 表示可以继续执行容量绑定。
        /// </summary>
        public static bool ValidateEquipped(Pawn pawn, CompTriggerBody trigger)
        {
            if (trigger == null || CanEquip(trigger.ParentThing, pawn))
            {
                return true;
            }

            ForceDetachInvalidTrigger(pawn, trigger, true);
            return false;
        }

        /// <summary>
        /// 腺体资格失效时统一清理当前 Pawn 的触发体装备。
        /// </summary>
        public static void HandleGlandEligibilityLost(Pawn pawn)
        {
            if (pawn == null
                || !HasEligibilityState(pawn)
                || TrionGlandEligibility.HasActiveTrionGland(pawn)
                || !handlingPawns.Add(pawn))
            {
                return;
            }

            try
            {
                CombatBodySurfaceAccess.ResolveCommands(pawn)?.ForceReleaseForEligibilityLoss();

                List<ThingWithComps> equipmentSnapshot = new List<ThingWithComps>();
                List<ThingWithComps> allEquipment = pawn.equipment?.AllEquipmentListForReading;
                if (allEquipment != null)
                {
                    foreach (ThingWithComps equipment in allEquipment)
                    {
                        if (equipment?.TryGetComp<CompTriggerBody>() != null && !CanEquip(equipment, pawn))
                        {
                            equipmentSnapshot.Add(equipment);
                        }
                    }
                }

                for (int i = 0; i < equipmentSnapshot.Count; i++)
                {
                    ThingWithComps equipment = equipmentSnapshot[i];
                    CompTriggerBody trigger = equipment.TryGetComp<CompTriggerBody>();
                    if (trigger == null || !pawn.equipment.Contains(equipment))
                    {
                        continue;
                    }

                    TriggerTrionCapacityBindingRegistry.NotifyEligibilityLost(pawn, trigger);
                    DetachEquipment(pawn, equipment);
                }
            }
            finally
            {
                handlingPawns.Remove(pawn);
            }
        }

        /// <summary>
        /// 读档或其他直接装备路径完成后，修正无腺体仍持有的非法触发体。
        /// </summary>
        public static void ValidateAfterLoadOrTick(Pawn pawn)
        {
            HandleGlandEligibilityLost(pawn);
        }

        /// <summary>普通角色没有触发体或活跃战斗体时，不进入腺体失效清理和快照分配。</summary>
        private static bool HasEligibilityState(Pawn pawn)
        {
            List<ThingWithComps> equipment = pawn.equipment?.AllEquipmentListForReading;
            if (equipment != null)
            {
                for (int index = 0; index < equipment.Count; index++)
                    if (equipment[index]?.TryGetComp<CompTriggerBody>() != null && !CanEquip(equipment[index], pawn)) return true;
            }

            ICombatBodyReader reader = CombatBodySurfaceAccess.ResolveReader(pawn);
            return reader != null
                && (reader.Phase == CombatBodyPhase.Active || reader.Phase == CombatBodyPhase.Collapsing);
        }

        /// <summary>
        /// 强制解除一个非法触发体，并在移除前通知容量绑定器。
        /// </summary>
        private static void ForceDetachInvalidTrigger(
            Pawn pawn,
            CompTriggerBody trigger,
            bool notifyEligibilityLost)
        {
            if (pawn == null || trigger == null || !handlingPawns.Add(pawn))
            {
                return;
            }

            try
            {
                CombatBodySurfaceAccess.ResolveCommands(pawn)?.ForceReleaseForEligibilityLoss();
                if (notifyEligibilityLost)
                {
                    TriggerTrionCapacityBindingRegistry.NotifyEligibilityLost(pawn, trigger);
                }

                DetachEquipment(pawn, trigger.ParentThing);
            }
            finally
            {
                handlingPawns.Remove(pawn);
            }
        }

        /// <summary>
        /// 优先使用原版掉落流程；没有地图或掉落失败时转入 Pawn 库存。
        /// </summary>
        private static void DetachEquipment(Pawn pawn, ThingWithComps equipment)
        {
            if (pawn?.equipment == null || equipment == null || !pawn.equipment.Contains(equipment))
            {
                return;
            }

            if (pawn.Spawned && pawn.MapHeld != null && pawn.PositionHeld.IsValid)
            {
                if (pawn.equipment.TryDropEquipment(equipment, out ThingWithComps dropped, pawn.PositionHeld, false))
                {
                    return;
                }
            }

            if (pawn.inventory?.innerContainer != null
                && pawn.equipment.TryTransferEquipmentToContainer(equipment, pawn.inventory.innerContainer))
            {
                return;
            }

            Log.Error("[BDP.Trigger] 无法安全解除无腺体触发体装备：" + equipment);
        }
    }
}
