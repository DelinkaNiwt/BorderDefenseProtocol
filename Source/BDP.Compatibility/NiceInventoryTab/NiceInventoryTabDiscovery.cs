using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BDP.Compatibility.NiceInventoryTab
{
    /// <summary>
    /// NIT 程序集发现结果。
    /// 只保存已经确认过的方法和字段，不保存外部对象实例。
    /// </summary>
    internal sealed class NiceInventoryTabTargets
    {
        internal Assembly Assembly;
        internal Type EquippedItemType;
        internal Type DamageUtilityType;
        internal Type InfoWidgetType;
        internal Type FieldTooltipHelperType;
        internal MethodInfo UpdateStats;
        internal MethodInfo DrawWeaponInfo;
        internal MethodInfo DrawBars;
        internal MethodInfo MeleeDpsForItem;
        internal MethodInfo RangedDpsForItem;
        internal MethodInfo ArmorPenForItem;
        internal MethodInfo FireRateForItem;
        internal MethodInfo AtkSpeedForItem;
        internal FieldInfo EquippedItemFieldItem;
        internal FieldInfo EquippedItemFieldPawn;
        internal FieldInfo InfoWidgetFieldCurrentItem;
        internal FieldInfo InfoWidgetFieldItem;
        internal MethodInfo InfoWidgetSetItem;
        internal MethodInfo InfoWidgetDraw;
        internal MethodInfo WeaponReloadTimeShort;
        internal MethodInfo WeaponStoppingPower;
        internal MethodInfo WeaponFireRate;
        internal MethodInfo WeaponMeleeAttackSpeed;
        internal MethodInfo WeaponMeleeAccuracy;
        internal MethodInfo WeaponMeleeDamage;
        internal Dictionary<string, FieldInfo> EquippedItemFields;
    }

    /// <summary>
    /// NIT 可选程序集和目标方法发现器。
    /// 发现失败时返回原因，不抛异常到 BDP 启动路径。
    /// </summary>
    internal static class NiceInventoryTabDiscovery
    {
        private const string AssemblyName = "NiceInventoryTab";
        private const string PackageId = "Andromeda.NiceInventoryTab";

        private static readonly string[] RequiredEquippedItemFields =
        {
            "Item", "pawn", "dps", "damage", "armor_penetration_sharp", "warmupTime",
            "cooldownTime", "stoppingPower", "damageType", "fireRate", "meleeCooldown",
            "meleeHitChance", "meleeMaxDamage", "hasFlameDamage", "hasEmpDamage"
        };

        internal static bool TryDiscover(
            Assembly[] loadedAssemblies,
            out NiceInventoryTabTargets targets,
            out string failureReason)
        {
            targets = null;
            failureReason = null;
            try
            {
                Assembly assembly = loadedAssemblies?.FirstOrDefault(IsNiceInventoryTabAssembly);
                if (assembly == null)
                {
                    failureReason = "未检测到 NIT，未安装适配。";
                    return false;
                }

                Version version = assembly.GetName().Version;
                if (version == null || version.Major != 1)
                {
                    failureReason = "NIT 程序集主版本不受支持：" + (version ?? new Version(0, 0));
                    return false;
                }

                Type equippedItemType = AccessTools.TypeByName("NiceInventoryTab.EquippedItem");
                Type damageUtilityType = AccessTools.TypeByName("NiceInventoryTab.DamageUtility");
                Type infoWidgetType = AccessTools.TypeByName("NiceInventoryTab.InfoWidget");
                Type fieldTooltipHelperType = AccessTools.TypeByName("NiceInventoryTab.FieldTooltipHelper");
                if (equippedItemType == null || damageUtilityType == null || infoWidgetType == null)
                {
                    failureReason = "NIT 关键类型缺失，未安装适配。";
                    return false;
                }

                NiceInventoryTabTargets discovered = new NiceInventoryTabTargets
                {
                    Assembly = assembly,
                    EquippedItemType = equippedItemType,
                    DamageUtilityType = damageUtilityType,
                    InfoWidgetType = infoWidgetType,
                    FieldTooltipHelperType = fieldTooltipHelperType,
                    UpdateStats = FindMethod(equippedItemType, "UpdateStats", 0),
                    DrawWeaponInfo = FindMethod(equippedItemType, "DrawWeaponInfo", 1),
                    DrawBars = FindMethod(equippedItemType, "DrawBars", 1),
                    MeleeDpsForItem = FindMethod(damageUtilityType, "MeleeDPSForItem", 2),
                    RangedDpsForItem = FindMethod(damageUtilityType, "RangedDPSForItem", 2),
                    ArmorPenForItem = FindMethod(damageUtilityType, "ArmorPenForItem", 2),
                    FireRateForItem = FindMethod(damageUtilityType, "FireRateForItem", 2),
                    AtkSpeedForItem = FindMethod(damageUtilityType, "AtkSpeedForItem", 2),
                    EquippedItemFieldItem = FindField(equippedItemType, "Item"),
                    EquippedItemFieldPawn = FindField(equippedItemType, "pawn"),
                    InfoWidgetFieldCurrentItem = FindField(infoWidgetType, "CurrentItem"),
                    InfoWidgetFieldItem = FindField(infoWidgetType, "Item"),
                    InfoWidgetSetItem = FindMethod(infoWidgetType, "SetItem", 2),
                    InfoWidgetDraw = FindMethod(infoWidgetType, "Draw", 0),
                    WeaponReloadTimeShort = FindStaticMethod(fieldTooltipHelperType, "WeaponReloadTimeShort", 2),
                    WeaponStoppingPower = FindStaticMethod(fieldTooltipHelperType, "WeaponStoppingPower", 2),
                    WeaponFireRate = FindStaticMethod(fieldTooltipHelperType, "WeaponFireRate", 2),
                    WeaponMeleeAttackSpeed = FindStaticMethod(fieldTooltipHelperType, "WeaponMeleeAttackSpeed", 2),
                    WeaponMeleeAccuracy = FindStaticMethod(fieldTooltipHelperType, "WeaponMeleeAccuracy", 2),
                    WeaponMeleeDamage = FindStaticMethod(fieldTooltipHelperType, "WeaponMeleeDamage", 2),
                    EquippedItemFields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal)
                };

                foreach (string fieldName in RequiredEquippedItemFields)
                {
                    FieldInfo field = FindField(equippedItemType, fieldName);
                    if (field == null)
                    {
                        failureReason = "NIT 字段缺失：" + fieldName;
                        return false;
                    }

                    discovered.EquippedItemFields[fieldName] = field;
                }

                if (discovered.UpdateStats == null
                    || discovered.DrawWeaponInfo == null
                    || discovered.DrawBars == null
                    || discovered.MeleeDpsForItem == null
                    || discovered.RangedDpsForItem == null
                    || discovered.ArmorPenForItem == null
                    || discovered.FireRateForItem == null
                    || discovered.AtkSpeedForItem == null
                    || discovered.EquippedItemFieldItem == null
                    || discovered.EquippedItemFieldPawn == null
                    || discovered.InfoWidgetFieldCurrentItem == null)
                {
                    failureReason = "NIT 必需方法或字段缺失，未安装适配。";
                    return false;
                }

                if (discovered.InfoWidgetFieldItem == null
                    || discovered.InfoWidgetSetItem == null
                    || discovered.InfoWidgetDraw == null)
                {
                    failureReason = "NIT 信息栏同步入口缺失，未安装适配。";
                    return false;
                }

                targets = discovered;
                return true;
            }
            catch (Exception exception)
            {
                failureReason = "NIT 结构发现异常：" + exception.GetType().Name;
                targets = null;
                return false;
            }
        }

        /// <summary>判断程序集身份；包名常量用于记录兼容目标，不硬引用程序集。</summary>
        private static bool IsNiceInventoryTabAssembly(Assembly assembly)
        {
            return assembly != null
                && string.Equals(assembly.GetName().Name, AssemblyName, StringComparison.Ordinal)
                && PackageId == "Andromeda.NiceInventoryTab";
        }

        /// <summary>按名称和参数数量读取一个方法。</summary>
        private static MethodInfo FindMethod(Type type, string name, int parameterCount)
        {
            if (type == null)
            {
                return null;
            }

            return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => string.Equals(method.Name, name, StringComparison.Ordinal)
                    && method.GetParameters().Length == parameterCount);
        }

        /// <summary>按名称读取 NIT 静态提示方法；缺少提示设施时不阻断主适配器发现。</summary>
        private static MethodInfo FindStaticMethod(Type type, string name, int parameterCount)
        {
            return FindMethod(type, name, parameterCount);
        }

        /// <summary>按名称读取实例或静态字段。</summary>
        private static FieldInfo FindField(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
