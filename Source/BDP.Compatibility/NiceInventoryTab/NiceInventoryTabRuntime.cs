using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BDP.Core.Information;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP.Compatibility.NiceInventoryTab
{
    /// <summary>
    /// NIT 运行时兼容桥。
    /// 用 ThreadStatic（线程局部）保存绘制上下文，避免修改 ThingDef 或使用全局小人状态。
    /// </summary>
    internal static class NiceInventoryTabRuntime
    {
        /// <summary>连续兼容异常达到此次数后自动关闭 NIT 适配器。</summary>
        private const int MaxConsecutiveFailures = 3;

        [ThreadStatic]
        private static object currentDrawItem;

        /// <summary>按 NIT 装备对象保存已应用的投影版本标记，不保存战斗属性快照。</summary>
        private static readonly ConditionalWeakTable<object, EquippedItemSyncState> equippedItemSyncStates
            = new ConditionalWeakTable<object, EquippedItemSyncState>();

        /// <summary>按 NIT 信息栏对象保存已同步的投影版本和小人上下文。</summary>
        private static readonly ConditionalWeakTable<object, InfoWidgetSyncState> infoWidgetSyncStates
            = new ConditionalWeakTable<object, InfoWidgetSyncState>();

        /// <summary>由适配器注入的禁用回调。</summary>
        private static Action<string> failureHandler;

        /// <summary>连续兼容异常计数。</summary>
        private static int consecutiveFailures;

        /// <summary>防止熔断回调被重复调用。</summary>
        private static bool failureHandlerInvoked;

        internal static object CurrentDrawItem
        {
            get { return currentDrawItem; }
        }

        internal static void BeginDraw(object equippedItem)
        {
            currentDrawItem = equippedItem;
        }

        internal static void EndDraw()
        {
            currentDrawItem = null;
        }

        /// <summary>绑定当前适配器的故障回调。</summary>
        internal static void BindFailureHandler(Action<string> handler)
        {
            failureHandler = handler;
            consecutiveFailures = 0;
            failureHandlerInvoked = false;
        }

        /// <summary>兼容操作成功后清除短暂异常计数。</summary>
        internal static void MarkSuccess()
        {
            consecutiveFailures = 0;
        }

        /// <summary>记录兼容层异常，并在连续失败时熔断当前适配器。</summary>
        internal static void RegisterFailure(string operation, Exception exception)
        {
            consecutiveFailures++;
            CompatibilityDiagnostics.Once(
                "nit",
                operation + "适配异常，保留 NIT 原值：" + exception.GetType().Name);

            if (consecutiveFailures < MaxConsecutiveFailures
                || failureHandlerInvoked
                || failureHandler == null)
            {
                return;
            }

            failureHandlerInvoked = true;
            try
            {
                failureHandler("连续兼容异常达到阈值，已自动禁用 NIT 适配。");
            }
            catch
            {
                // 禁用回调也不得把异常传播回游戏或外部模组。
            }
        }

        internal static void UpdateStatsPostfix(object __instance)
        {
            try
            {
                TriggerBodyAutomaticAttackDisplaySnapshot snapshot;
                if (NiceInventoryTabProjectionAdapter.TryGetSnapshot(__instance, out snapshot))
                {
                    NiceInventoryTabProjectionAdapter.ApplySnapshot(__instance, snapshot);
                    MarkEquippedItemSynchronized(__instance, snapshot.ProjectionVersion, true, snapshot.ResultId);
                    MarkSuccess();
                }
            }
            catch (Exception exception)
            {
                RegisterFailure("刷新", exception);
            }
        }

        internal static void DrawPrefix(object __instance)
        {
            BeginDraw(__instance);
            try
            {
                EnsureEquippedItemSynchronized(__instance);
            }
            catch (Exception exception)
            {
                RegisterFailure("绘制前刷新", exception);
            }
        }

        /// <summary>在 NIT 信息栏写入新物品后记录小人上下文。</summary>
        internal static void InfoWidgetSetItemPostfix(object __instance, object[] __args)
        {
            try
            {
                InfoWidgetSyncState state = infoWidgetSyncStates.GetValue(
                    __instance,
                    _ => new InfoWidgetSyncState());
                state.Pawn = __args != null && __args.Length > 1
                    ? __args[1] as Pawn
                    : null;
                MarkInfoWidgetSynchronized(__instance, state);
                MarkSuccess();
            }
            catch (Exception exception)
            {
                RegisterFailure("信息栏写入后刷新", exception);
            }
        }

        /// <summary>在 NIT 信息栏绘制前按投影版本刷新统计条目。</summary>
        internal static void InfoWidgetDrawPrefix(object __instance)
        {
            try
            {
                EnsureInfoWidgetSynchronized(__instance);
                MarkSuccess();
            }
            catch (Exception exception)
            {
                RegisterFailure("信息栏绘制前刷新", exception);
            }
        }

        internal static void DrawPostfix()
        {
            EndDraw();
        }

        internal static Exception DrawFinalizer(Exception __exception)
        {
            EndDraw();
            return __exception;
        }

        /// <summary>确保 NIT 装备对象已经应用当前触发体投影。</summary>
        private static void EnsureEquippedItemSynchronized(object equippedItem)
        {
            if (equippedItem == null)
            {
                return;
            }

            Thing item = NiceInventoryTabProjectionAdapter.GetEquippedItem(equippedItem);
            Pawn pawn = NiceInventoryTabProjectionAdapter.GetEquippedPawn(equippedItem);
            int projectionVersion;
            if (!NiceInventoryTabProjectionAdapter.TryGetProjectionVersion(item, out projectionVersion))
            {
                return;
            }

            EquippedItemSyncState state = equippedItemSyncStates.GetValue(
                equippedItem,
                _ => new EquippedItemSyncState());
            if (state.Initialized && state.ProjectionVersion == projectionVersion)
            {
                return;
            }

            TriggerBodyAutomaticAttackDisplaySnapshot snapshot;
            if (NiceInventoryTabProjectionAdapter.TryGetSnapshot(item, pawn, out snapshot))
            {
                NiceInventoryTabProjectionAdapter.ApplySnapshot(equippedItem, snapshot);
                MarkEquippedItemSynchronized(
                    equippedItem,
                    snapshot.ProjectionVersion,
                    true,
                    snapshot.ResultId);
                return;
            }

            // 当前投影没有可显示的自动攻击时，恢复 NIT 自身的占位武器统计。
            NiceInventoryTabProjectionAdapter.RestoreNativeStats(equippedItem);
            MarkEquippedItemSynchronized(equippedItem, projectionVersion, false, null);
        }

        /// <summary>确保 NIT 悬浮信息栏已经重建到当前投影。</summary>
        private static void EnsureInfoWidgetSynchronized(object infoWidget)
        {
            if (infoWidget == null)
            {
                return;
            }

            Thing item = NiceInventoryTabProjectionAdapter.GetInfoWidgetItem(infoWidget);
            int projectionVersion;
            if (!NiceInventoryTabProjectionAdapter.TryGetProjectionVersion(item, out projectionVersion))
            {
                return;
            }

            InfoWidgetSyncState state = infoWidgetSyncStates.GetValue(
                infoWidget,
                _ => new InfoWidgetSyncState());
            if (state.Refreshing || (state.Initialized && state.ProjectionVersion == projectionVersion))
            {
                return;
            }

            state.Refreshing = true;
            try
            {
                NiceInventoryTabProjectionAdapter.RebuildInfoWidget(
                    infoWidget,
                    item,
                    state.Pawn);
            }
            finally
            {
                state.Refreshing = false;
            }

            MarkInfoWidgetSynchronized(infoWidget, state);
        }

        /// <summary>记录装备对象最近应用的投影版本；只保存标记，不保存属性快照。</summary>
        internal static void MarkEquippedItemSynchronized(
            object equippedItem,
            int projectionVersion,
            bool hasSnapshot,
            string resultId)
        {
            if (equippedItem == null)
            {
                return;
            }

            EquippedItemSyncState state = equippedItemSyncStates.GetValue(
                equippedItem,
                _ => new EquippedItemSyncState());
            state.Initialized = true;
            state.ProjectionVersion = projectionVersion;
            state.HasSnapshot = hasSnapshot;
            state.ResultId = resultId;
        }

        /// <summary>记录信息栏最近同步的投影版本；只保存标记，不保存属性快照。</summary>
        private static void MarkInfoWidgetSynchronized(object infoWidget, InfoWidgetSyncState state)
        {
            Thing item = NiceInventoryTabProjectionAdapter.GetInfoWidgetItem(infoWidget);
            int projectionVersion;
            if (!NiceInventoryTabProjectionAdapter.TryGetProjectionVersion(item, out projectionVersion))
            {
                state.Initialized = false;
                return;
            }

            state.Initialized = true;
            state.ProjectionVersion = projectionVersion;
            TriggerBodyAutomaticAttackDisplaySnapshot snapshot;
            state.HasSnapshot = NiceInventoryTabProjectionAdapter.TryGetSnapshot(item, state.Pawn, out snapshot);
            state.ResultId = snapshot != null ? snapshot.ResultId : null;
        }

        /// <summary>装备对象同步标记。</summary>
        private sealed class EquippedItemSyncState
        {
            internal bool Initialized;
            internal int ProjectionVersion;
            internal bool HasSnapshot;
            internal string ResultId;
        }

        /// <summary>信息栏同步标记和重建所需的小人上下文。</summary>
        private sealed class InfoWidgetSyncState
        {
            internal Pawn Pawn;
            internal bool Initialized;
            internal bool Refreshing;
            internal int ProjectionVersion;
            internal bool HasSnapshot;
            internal string ResultId;
        }

        internal static bool StatPrefix(string statName, object[] __args, ref float __result)
        {
            try
            {
                float value;
                if (!NiceInventoryTabProjectionAdapter.TryOverrideStat(statName, __args, out value))
                {
                    return true;
                }

                __result = value;
                MarkSuccess();
                return false;
            }
            catch (Exception exception)
            {
                RegisterFailure("统计", exception);
                return true;
            }
        }

        /// <summary>
        /// 为 BDP 动态武器提供安全的 NIT 悬浮提示。
        /// 普通原版物品返回 true，继续使用 NIT 原提示逻辑。
        /// </summary>
        internal static bool TooltipPrefix(
            string tooltipName,
            Thing thing,
            Pawn pawn,
            ref string result)
        {
            try
            {
                TriggerBodyAutomaticAttackDisplaySnapshot snapshot;
                if (!NiceInventoryTabProjectionAdapter.TryGetSnapshot(thing, pawn, out snapshot))
                {
                    return true;
                }

                string tooltip;
                if (!TryBuildTooltip(tooltipName, snapshot, out tooltip))
                {
                    return true;
                }

                result = tooltip ?? string.Empty;
                MarkSuccess();
                return false;
            }
            catch (Exception exception)
            {
                RegisterFailure("提示", exception);
                return true;
            }
        }

        /// <summary>按当前快照建立不依赖原始 ThingDef 的提示文本。</summary>
        private static bool TryBuildTooltip(
            string tooltipName,
            TriggerBodyAutomaticAttackDisplaySnapshot snapshot,
            out string tooltip)
        {
            tooltip = null;
            if (snapshot == null)
            {
                return false;
            }

            if (tooltipName == "WeaponReloadTimeShort"
                && snapshot.WeaponMode == AutomaticAttackDisplayMode.Ranged)
            {
                tooltip = Translator.Translate("WarmupTime")
                    + ": " + snapshot.RangedWarmupSeconds.ToString("F1")
                    + "\n"
                    + ((Def)StatDefOf.RangedWeapon_Cooldown).LabelCap
                    + ": " + snapshot.RangedCooldownSeconds.ToString("F2");
                return true;
            }

            if (tooltipName == "WeaponStoppingPower"
                && snapshot.WeaponMode == AutomaticAttackDisplayMode.Ranged)
            {
                if (snapshot.RangedStoppingPower.HasValue)
                {
                    tooltip = Translator.Translate("StoppingPower").ToString()
                        + ": " + snapshot.RangedStoppingPower.Value.ToString("F1")
                        + "\n\n"
                        + Translator.Translate("StoppingPowerExplanation").ToString();
                }
                else
                {
                    tooltip = string.Empty;
                }
                return true;
            }

            if (tooltipName == "WeaponFireRate"
                && snapshot.WeaponMode == AutomaticAttackDisplayMode.Ranged)
            {
                if (snapshot.RangedFireRate.HasValue)
                {
                    tooltip = Translator.Translate("BurstShotFireRate").ToString()
                        + ": " + snapshot.RangedFireRate.Value.ToString("F1")
                        + "\n\n"
                        + Translator.Translate("BurstShotCount").ToString()
                        + ": " + snapshot.RangedBurstCount;
                }
                else
                {
                    tooltip = string.Empty;
                }
                return true;
            }

            if (tooltipName == "WeaponMeleeAttackSpeed"
                && snapshot.WeaponMode == AutomaticAttackDisplayMode.Melee)
            {
                tooltip = snapshot.MeleeCycleSeconds.ToString("F2")
                    + " " + Translator.Translate("SecondsPerAttackLower")
                    + " (" + Translator.Translate("Average") + ")";
                return true;
            }

            if (tooltipName == "WeaponMeleeAccuracy"
                && snapshot.WeaponMode == AutomaticAttackDisplayMode.Melee)
            {
                tooltip = ((Def)StatDefOf.MeleeHitChance).LabelCap
                    + " " + GenText.ToStringPercent(snapshot.MeleeHitChance);
                return true;
            }

            if (tooltipName == "WeaponMeleeDamage"
                && snapshot.WeaponMode == AutomaticAttackDisplayMode.Melee)
            {
                tooltip = Translator.Translate("NIT_Damage")
                    + ": " + snapshot.MeleeMaxDamage.ToString("0.##");
                return true;
            }

            return false;
        }

        /// <summary>把 NIT 绘制方法中的原版分类读取改为上下文感知读取。</summary>
        internal static IEnumerable<CodeInstruction> ReplaceWeaponModeChecks(
            IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo rangedGetter = AccessTools.PropertyGetter(typeof(ThingDef), "IsRangedWeapon");
            MethodInfo meleeGetter = AccessTools.PropertyGetter(typeof(ThingDef), "IsMeleeWeapon");
            MethodInfo rangedBridge = AccessTools.Method(
                typeof(NiceInventoryTabProjectionAdapter), nameof(NiceInventoryTabProjectionAdapter.IsRangedDisplay));
            MethodInfo meleeBridge = AccessTools.Method(
                typeof(NiceInventoryTabProjectionAdapter), nameof(NiceInventoryTabProjectionAdapter.IsMeleeDisplay));

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.operand is MethodInfo called
                    && called == rangedGetter)
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = rangedBridge;
                }
                else if (instruction.operand is MethodInfo calledMelee
                    && calledMelee == meleeGetter)
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = meleeBridge;
                }

                yield return instruction;
            }
        }
    }
}
