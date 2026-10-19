using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BDP.Core.Information;
using HarmonyLib;
using Verse;

namespace BDP.Compatibility.NiceInventoryTab
{
    /// <summary>
    /// NIT 动态 Harmony 补丁方法集合。
    /// 方法签名只使用 object、Verse 类型和 Harmony 特殊参数，不硬引用 NIT 类型。
    /// </summary>
    internal static class NiceInventoryTabPatches
    {
        internal static HarmonyMethod UpdateStatsPostfix
        {
            get { return new HarmonyMethod(typeof(NiceInventoryTabRuntime), nameof(NiceInventoryTabRuntime.UpdateStatsPostfix)); }
        }

        internal static HarmonyMethod DrawPrefix
        {
            get { return new HarmonyMethod(typeof(NiceInventoryTabRuntime), nameof(NiceInventoryTabRuntime.DrawPrefix)); }
        }

        internal static HarmonyMethod DrawPostfix
        {
            get { return new HarmonyMethod(typeof(NiceInventoryTabRuntime), nameof(NiceInventoryTabRuntime.DrawPostfix)); }
        }

        internal static HarmonyMethod DrawFinalizer
        {
            get { return new HarmonyMethod(typeof(NiceInventoryTabRuntime), nameof(NiceInventoryTabRuntime.DrawFinalizer)); }
        }

        internal static HarmonyMethod InfoWidgetSetItemPostfix
        {
            get { return new HarmonyMethod(typeof(NiceInventoryTabRuntime), nameof(NiceInventoryTabRuntime.InfoWidgetSetItemPostfix)); }
        }

        internal static HarmonyMethod InfoWidgetDrawPrefix
        {
            get { return new HarmonyMethod(typeof(NiceInventoryTabRuntime), nameof(NiceInventoryTabRuntime.InfoWidgetDrawPrefix)); }
        }

        internal static HarmonyMethod TooltipPrefix
        {
            get { return new HarmonyMethod(typeof(NiceInventoryTabPatches), nameof(TooltipPrefixBridge)); }
        }

        internal static HarmonyMethod Transpiler
        {
            get { return new HarmonyMethod(typeof(NiceInventoryTabRuntime), nameof(NiceInventoryTabRuntime.ReplaceWeaponModeChecks)); }
        }

        internal static HarmonyMethod CreateStatPrefix(string statName)
        {
            return new HarmonyMethod(
                typeof(NiceInventoryTabPatches),
                nameof(StatPrefixBridge))
            {
                priority = Priority.High
            };
        }

        /// <summary>把固定统计名称注入到通用前缀。</summary>
        public static bool StatPrefixBridge(object[] __args, ref float __result, MethodBase __originalMethod)
        {
            string statName = __originalMethod != null ? __originalMethod.Name : string.Empty;
            return NiceInventoryTabRuntime.StatPrefix(statName, __args, ref __result);
        }

        /// <summary>把 NIT 提示方法名和原始参数转交兼容层。</summary>
        public static bool TooltipPrefixBridge(
            object[] __args,
            ref string __result,
            MethodBase __originalMethod)
        {
            // 不按第三方方法的参数名注入；NIT 不同版本会把第一个 Thing 参数命名为 thing 或 weapon。
            Verse.Thing thing = __args != null && __args.Length > 0
                ? __args[0] as Verse.Thing
                : null;
            Verse.Pawn pawn = __args != null && __args.Length > 1
                ? __args[1] as Verse.Pawn
                : null;
            string tooltipName = __originalMethod != null ? __originalMethod.Name : string.Empty;
            return NiceInventoryTabRuntime.TooltipPrefix(tooltipName, thing, pawn, ref __result);
        }
    }
}
