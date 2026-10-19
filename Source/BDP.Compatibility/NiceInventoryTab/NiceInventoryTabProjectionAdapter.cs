using System;
using System.Collections.Generic;
using System.Reflection;
using BDP.Core.Information;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Compatibility.NiceInventoryTab
{
    /// <summary>
    /// 把 BDP 自动攻击快照映射到 NIT 的公开字段；失败时使用 Fallback（回退）值。
    /// 该类不引用 NIT 类型，所有外部对象均以 object 和反射字段承接。
    /// </summary>
    internal static class NiceInventoryTabProjectionAdapter
    {
        private static NiceInventoryTabTargets targets;

        internal static void Bind(NiceInventoryTabTargets value)
        {
            targets = value;
        }

        internal static bool TryGetSnapshot(object equippedItem, out TriggerBodyAutomaticAttackDisplaySnapshot snapshot)
        {
            snapshot = null;
            if (targets == null || equippedItem == null)
            {
                return false;
            }

            try
            {
                Thing item = targets.EquippedItemFieldItem.GetValue(equippedItem) as Thing;
                Pawn pawn = targets.EquippedItemFieldPawn.GetValue(equippedItem) as Pawn;
                return TryGetSnapshot(item, pawn, out snapshot);
            }
            catch
            {
                snapshot = null;
                return false;
            }
        }

        internal static Thing GetEquippedItem(object equippedItem)
        {
            return targets?.EquippedItemFieldItem?.GetValue(equippedItem) as Thing;
        }

        internal static Pawn GetEquippedPawn(object equippedItem)
        {
            return targets?.EquippedItemFieldPawn?.GetValue(equippedItem) as Pawn;
        }

        internal static Thing GetInfoWidgetItem(object infoWidget)
        {
            return targets?.InfoWidgetFieldItem?.GetValue(infoWidget) as Thing;
        }

        internal static bool TryGetProjectionVersion(Thing item, out int projectionVersion)
        {
            projectionVersion = 0;
            CompTriggerBody triggerBody = item?.TryGetComp<CompTriggerBody>();
            return triggerBody != null
                && TriggerBodyAutomaticAttackDisplayProvider.TryGetProjectionVersion(
                    triggerBody,
                    out projectionVersion);
        }

        internal static void RestoreNativeStats(object equippedItem)
        {
            if (targets?.UpdateStats == null || equippedItem == null)
            {
                return;
            }

            targets.UpdateStats.Invoke(equippedItem, null);
        }

        internal static void RebuildInfoWidget(object infoWidget, Thing item, Pawn pawn)
        {
            if (targets?.InfoWidgetSetItem == null || infoWidget == null || item == null)
            {
                return;
            }

            targets.InfoWidgetSetItem.Invoke(infoWidget, new object[] { item, pawn });
        }

        internal static bool TryGetCurrentItemSnapshot(
            object pawnObject,
            out TriggerBodyAutomaticAttackDisplaySnapshot snapshot)
        {
            snapshot = null;
            if (targets?.InfoWidgetFieldCurrentItem == null)
            {
                return false;
            }

            try
            {
                Thing item = targets.InfoWidgetFieldCurrentItem.GetValue(null) as Thing;
                return TryGetSnapshot(item, pawnObject as Pawn, out snapshot);
            }
            catch
            {
                snapshot = null;
                return false;
            }
        }

        internal static bool TryGetSnapshot(
            Thing item,
            Pawn pawn,
            out TriggerBodyAutomaticAttackDisplaySnapshot snapshot)
        {
            snapshot = null;
            CompTriggerBody triggerBody = item?.TryGetComp<CompTriggerBody>();
            return triggerBody != null
                && TriggerBodyAutomaticAttackDisplayProvider.TryBuild(triggerBody, pawn, out snapshot)
                && snapshot != null
                && snapshot.IsValid;
        }

        internal static bool TryGetSnapshotMode(
            object equippedItem,
            out AutomaticAttackDisplayMode mode)
        {
            mode = AutomaticAttackDisplayMode.Melee;
            TriggerBodyAutomaticAttackDisplaySnapshot snapshot;
            if (!TryGetSnapshot(equippedItem, out snapshot))
            {
                return false;
            }

            mode = snapshot.WeaponMode;
            return true;
        }

        internal static bool TryGetCurrentMode(
            ThingDef definition,
            out AutomaticAttackDisplayMode mode)
        {
            mode = AutomaticAttackDisplayMode.Melee;
            object current = NiceInventoryTabRuntime.CurrentDrawItem;
            TriggerBodyAutomaticAttackDisplaySnapshot snapshot;
            if (current == null || !TryGetSnapshot(current, out snapshot))
            {
                return false;
            }

            mode = snapshot.WeaponMode;
            return true;
        }

        internal static void ApplySnapshot(object equippedItem, TriggerBodyAutomaticAttackDisplaySnapshot snapshot)
        {
            if (targets == null || equippedItem == null || snapshot == null)
            {
                return;
            }

            Set(equippedItem, "dps", snapshot.WeaponMode == AutomaticAttackDisplayMode.Melee
                ? snapshot.MeleeAverageDps
                : snapshot.RangedAverageDps);
            Set(equippedItem, "armor_penetration_sharp", snapshot.ArmorPenetration);

            if (snapshot.WeaponMode == AutomaticAttackDisplayMode.Ranged)
            {
                Set(equippedItem, "damage", snapshot.Damage);
                Set(equippedItem, "warmupTime", (float?)snapshot.RangedWarmupSeconds);
                Set(equippedItem, "cooldownTime", snapshot.RangedCooldownSeconds);
                Set(equippedItem, "stoppingPower", snapshot.RangedStoppingPower);
                Set(equippedItem, "damageType", snapshot.RangedDamageType);
                Set(equippedItem, "fireRate", snapshot.RangedFireRate);
                Set(equippedItem, "meleeCooldown", 0f);
                Set(equippedItem, "meleeHitChance", 0f);
                Set(equippedItem, "meleeMaxDamage", 0f);
                Set(equippedItem, "hasFlameDamage", false);
                Set(equippedItem, "hasEmpDamage", false);
            }
            else
            {
                Set(equippedItem, "damage", 0f);
                Set(equippedItem, "warmupTime", (float?)null);
                Set(equippedItem, "cooldownTime", 0f);
                Set(equippedItem, "stoppingPower", (float?)null);
                Set(equippedItem, "damageType", null);
                Set(equippedItem, "fireRate", (float?)null);
                Set(equippedItem, "meleeCooldown", snapshot.MeleeCycleSeconds);
                Set(equippedItem, "meleeHitChance", snapshot.MeleeHitChance);
                Set(equippedItem, "meleeMaxDamage", snapshot.MeleeMaxDamage);
                Set(equippedItem, "hasFlameDamage", snapshot.HasFlameDamage);
                Set(equippedItem, "hasEmpDamage", snapshot.HasEmpDamage);
            }
        }

        internal static bool TryOverrideStat(
            string statName,
            object[] args,
            out float value)
        {
            value = 0f;
            Pawn pawn = args != null && args.Length > 0 ? args[0] as Pawn : null;
            TriggerBodyAutomaticAttackDisplaySnapshot snapshot;
            if (!TryGetCurrentItemSnapshot(pawn, out snapshot))
            {
                return false;
            }

            if (statName == "MeleeDPSForItem")
            {
                value = snapshot.WeaponMode == AutomaticAttackDisplayMode.Melee
                    ? snapshot.MeleeAverageDps
                    : 0f;
                return true;
            }

            if (statName == "RangedDPSForItem")
            {
                value = snapshot.WeaponMode == AutomaticAttackDisplayMode.Ranged
                    ? snapshot.RangedAverageDps
                    : 0f;
                return true;
            }

            if (statName == "ArmorPenForItem")
            {
                value = snapshot.ArmorPenetration;
                return true;
            }

            if (statName == "FireRateForItem")
            {
                value = snapshot.WeaponMode == AutomaticAttackDisplayMode.Ranged
                    ? snapshot.RangedFireRate.GetValueOrDefault()
                    : 0f;
                return true;
            }

            if (statName == "AtkSpeedForItem")
            {
                value = snapshot.WeaponMode == AutomaticAttackDisplayMode.Melee
                    ? snapshot.MeleeCycleSeconds
                    : 0f;
                return true;
            }

            return false;
        }

        internal static bool IsRangedDisplay(ThingDef definition)
        {
            AutomaticAttackDisplayMode mode;
            return NiceInventoryTabProjectionAdapter.TryGetCurrentMode(definition, out mode)
                ? mode == AutomaticAttackDisplayMode.Ranged
                : definition != null && definition.IsRangedWeapon;
        }

        internal static bool IsMeleeDisplay(ThingDef definition)
        {
            AutomaticAttackDisplayMode mode;
            return NiceInventoryTabProjectionAdapter.TryGetCurrentMode(definition, out mode)
                ? mode == AutomaticAttackDisplayMode.Melee
                : definition != null && definition.IsMeleeWeapon;
        }

        private static void Set(object instance, string name, object value)
        {
            FieldInfo field;
            if (targets?.EquippedItemFields == null
                || !targets.EquippedItemFields.TryGetValue(name, out field)
                || field == null)
            {
                return;
            }

            try
            {
                field.SetValue(instance, value);
            }
            catch
            {
                // 单个字段不兼容时保留 NIT 已计算的原值。
            }
        }
    }
}
