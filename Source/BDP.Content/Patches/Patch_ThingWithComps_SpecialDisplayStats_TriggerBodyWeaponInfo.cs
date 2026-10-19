using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using BDP.Core.Information;
using BDP.Core.Trigger;
using RimWorld;
using Verse;

namespace BDP.Content.Patches
{
    /// <summary>
    /// 在信息卡边界把触发体原生占位武器统计替换为当前已发布自动攻击统计。
    /// 原版占位统计来自 StatsToDraw 的 StatDef 循环，不能在组件附加条目中按分类删除。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_ThingWithComps_SpecialDisplayStats_TriggerBodyWeaponInfo
    {
        /// <summary>真实原版物品统计入口；不使用同名 Def 重载。</summary>
        internal static readonly MethodInfo StatsToDrawMethod = AccessTools.Method(
            typeof(StatsReportUtility), "StatsToDraw", new[] { typeof(Thing) });

        /// <summary>只记录原版构造点实际创建的条目；弱键随信息卡条目一起释放。</summary>
        private static readonly ConditionalWeakTable<StatDrawEntry, object> nativeWeaponEntries =
            new ConditionalWeakTable<StatDrawEntry, object>();

        /// <summary>所属标记不包含人物、物品或存档引用。</summary>
        private static readonly object nativeEntryMarker = new object();

        /// <summary>
        /// 动态统计成功时只替换已确认的原版占位实例，保留组件及其它模组附加条目。
        /// 动态统计失败时保持原始枚举结果不变。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StatsReportUtility), "StatsToDraw", new[] { typeof(Thing) })]
        public static void Postfix(
            Thing thing,
            ref IEnumerable<StatDrawEntry> __result)
        {
            CompTriggerBody triggerBody = thing?.TryGetComp<CompTriggerBody>();
            if (triggerBody == null)
            {
                return;
            }

            IReadOnlyList<StatDrawEntry> dynamicEntries;
            if (!TriggerBodyWeaponInfoPublisher.TryBuild(triggerBody, out dynamicEntries)
                || dynamicEntries == null
                || dynamicEntries.Count == 0)
            {
                return;
            }

            __result = ReplaceWeaponEntries(__result, dynamicEntries);
        }

        /// <summary>按原版产出身份和实际 StatDef 匹配，第三方复用同一 StatDef 也不受影响。</summary>
        private static IEnumerable<StatDrawEntry> ReplaceWeaponEntries(
            IEnumerable<StatDrawEntry> original,
            IReadOnlyList<StatDrawEntry> dynamicEntries)
        {
            HashSet<StatDef> replacementStats = new HashSet<StatDef>(
                dynamicEntries.Where(entry => entry?.stat != null).Select(entry => entry.stat));
            // 触发体自身敲击工具的两项占位统计由原版生成；远程表达也应替换它们。
            StatDef placeholderDps = DefDatabase<StatDef>.GetNamedSilentFail("MeleeWeapon_AverageDPS");
            StatDef placeholderArmor = DefDatabase<StatDef>.GetNamedSilentFail("MeleeWeapon_AverageArmorPenetration");
            if (original != null)
            {
                foreach (StatDrawEntry entry in original)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    if (nativeWeaponEntries.TryGetValue(entry, out object marker)
                        && entry.stat != null
                        && (entry.stat == placeholderDps
                            || entry.stat == placeholderArmor
                            || replacementStats.Contains(entry.stat)))
                    {
                        continue;
                    }

                    yield return entry;
                }
            }

            for (int index = 0; index < dynamicEntries.Count; index++)
            {
                StatDrawEntry entry = dynamicEntries[index];
                if (entry != null)
                {
                    yield return entry;
                }
            }
        }

        /// <summary>只标记 BDP 触发体在已确认原版构造点创建的武器统计，不改动条目本身。</summary>
        internal static StatDrawEntry MarkNativeEntry(StatDrawEntry entry, Thing thing)
        {
            if (thing?.TryGetComp<CompTriggerBody>() != null
                && (entry.category == StatCategoryDefOf.Weapon_Melee
                    || entry.category == StatCategoryDefOf.Weapon_Ranged))
            {
                nativeWeaponEntries.Add(entry, nativeEntryMarker);
            }
            return entry;
        }
    }

    /// <summary>仅在原版 StatDef 循环的两个构造点添加所属标记，保留所有原版计算和参数。</summary>
    [HarmonyPatch]
    internal static class Patch_StatsReportUtility_TriggerBodyNativeWeaponEntries
    {
        /// <summary>定位真实迭代器；不依赖编译器生成类型的编号。</summary>
        private static MethodBase TargetMethod()
        {
            return AccessTools.EnumeratorMoveNext(
                Patch_ThingWithComps_SpecialDisplayStats_TriggerBodyWeaponInfo.StatsToDrawMethod);
        }

        /// <summary>只替换两个已验证构造签名；结构变化或构造点不唯一时完整保留原指令。</summary>
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> original = instructions.ToList();
            ConstructorInfo valued = AccessTools.Constructor(typeof(StatDrawEntry), new[] {
                typeof(StatCategoryDef), typeof(StatDef), typeof(float), typeof(StatRequest),
                typeof(ToStringNumberSense), typeof(int?), typeof(bool) });
            ConstructorInfo disabled = AccessTools.Constructor(typeof(StatDrawEntry),
                new[] { typeof(StatCategoryDef), typeof(StatDef) });
            FieldInfo thing = AccessTools.Field(__originalMethod.DeclaringType, "thing");
            if (thing == null || thing.FieldType != typeof(Thing)
                || original.Count(code => code.opcode == OpCodes.Newobj && Equals(code.operand, valued)) != 1
                || original.Count(code => code.opcode == OpCodes.Newobj && Equals(code.operand, disabled)) != 1)
            {
                return original;
            }

            List<CodeInstruction> result = new List<CodeInstruction>();
            foreach (CodeInstruction code in original)
            {
                bool isValued = code.opcode == OpCodes.Newobj && Equals(code.operand, valued);
                bool isDisabled = code.opcode == OpCodes.Newobj && Equals(code.operand, disabled);
                if (!isValued && !isDisabled)
                {
                    result.Add(code);
                    continue;
                }

                // 原版构造参数保持在栈上，只追加该原版迭代器所属物品。
                CodeInstruction load = new CodeInstruction(OpCodes.Ldarg_0);
                load.labels.AddRange(code.labels);
                load.blocks.AddRange(code.blocks);
                result.Add(load);
                result.Add(new CodeInstruction(OpCodes.Ldfld, thing));
                result.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(
                    typeof(Patch_StatsReportUtility_TriggerBodyNativeWeaponEntries),
                    isValued ? nameof(CreateValuedEntry) : nameof(CreateDisabledEntry))));
            }
            return result;
        }

        /// <summary>原样调用原版带数值构造，再标记本次原版产出。</summary>
        private static StatDrawEntry CreateValuedEntry(StatCategoryDef category, StatDef stat,
            float value, StatRequest request, ToStringNumberSense numberSense, int? priority,
            bool forceUnfinalized, Thing thing)
        {
            return Patch_ThingWithComps_SpecialDisplayStats_TriggerBodyWeaponInfo.MarkNativeEntry(
                new StatDrawEntry(category, stat, value, request, numberSense, priority, forceUnfinalized), thing);
        }

        /// <summary>原样调用原版不可用条目的构造，再标记本次原版产出。</summary>
        private static StatDrawEntry CreateDisabledEntry(StatCategoryDef category, StatDef stat, Thing thing)
        {
            return Patch_ThingWithComps_SpecialDisplayStats_TriggerBodyWeaponInfo.MarkNativeEntry(
                new StatDrawEntry(category, stat), thing);
        }
    }
}
