using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BDP.Compatibility.NiceInventoryTab
{
    /// <summary>
    /// NIT 可选兼容适配器。
    /// 所有外部类型均在运行时发现，安装失败只关闭本适配器。
    /// </summary>
    internal sealed class NiceInventoryTabAdapter : IExternalModCompatibilityAdapter
    {
        private readonly Assembly[] loadedAssemblies;
        private readonly List<MethodBase> patchedMethods;
        private NiceInventoryTabTargets targets;
        private CompatibilityAdapterState state;
        private Harmony patchHarmony;

        internal NiceInventoryTabAdapter(Assembly[] loadedAssemblies)
        {
            this.loadedAssemblies = loadedAssemblies;
            patchedMethods = new List<MethodBase>();
            state = CompatibilityAdapterState.Uninitialized;
        }

        public string Id
        {
            get { return "nit"; }
        }

        public bool IsEnabled
        {
            get { return state == CompatibilityAdapterState.Enabled; }
        }

        public bool TryInitialize(Harmony harmony, out string failureReason)
        {
            failureReason = null;
            if (state != CompatibilityAdapterState.Uninitialized)
            {
                failureReason = "适配器已初始化，忽略重复初始化。";
                return IsEnabled;
            }

            if (!NiceInventoryTabDiscovery.TryDiscover(
                    loadedAssemblies,
                    out targets,
                    out failureReason))
            {
                state = failureReason != null && failureReason.Contains("未检测到")
                    ? CompatibilityAdapterState.NotPresent
                    : CompatibilityAdapterState.Unsupported;
                return false;
            }

            try
            {
                NiceInventoryTabProjectionAdapter.Bind(targets);
                patchHarmony = harmony;
                NiceInventoryTabRuntime.BindFailureHandler(Disable);
                PatchCore(harmony);
                PatchOptionalTooltips(harmony);
                state = CompatibilityAdapterState.Enabled;
                return true;
            }
            catch (Exception exception)
            {
                UnpatchSelf();
                state = CompatibilityAdapterState.Disabled;
                failureReason = "NIT 补丁安装异常：" + exception.GetType().Name;
                return false;
            }
        }

        public void Disable(string reason)
        {
            state = CompatibilityAdapterState.Disabled;
            UnpatchSelf();
            if (!string.IsNullOrWhiteSpace(reason))
            {
                CompatibilityDiagnostics.Once(Id, reason);
            }
        }

        /// <summary>安装维持动态武装属性所必需的核心补丁。</summary>
        private void PatchCore(Harmony harmony)
        {
            PatchMethod(harmony,
                targets.UpdateStats,
                postfix: NiceInventoryTabPatches.UpdateStatsPostfix);
            PatchMethod(harmony,
                targets.DrawWeaponInfo,
                prefix: NiceInventoryTabPatches.DrawPrefix,
                postfix: NiceInventoryTabPatches.DrawPostfix,
                transpiler: NiceInventoryTabPatches.Transpiler,
                finalizer: NiceInventoryTabPatches.DrawFinalizer);
            PatchMethod(harmony,
                targets.DrawBars,
                prefix: NiceInventoryTabPatches.DrawPrefix,
                postfix: NiceInventoryTabPatches.DrawPostfix,
                transpiler: NiceInventoryTabPatches.Transpiler,
                finalizer: NiceInventoryTabPatches.DrawFinalizer);
            PatchMethod(harmony,
                targets.InfoWidgetSetItem,
                postfix: NiceInventoryTabPatches.InfoWidgetSetItemPostfix);
            PatchMethod(harmony,
                targets.InfoWidgetDraw,
                prefix: NiceInventoryTabPatches.InfoWidgetDrawPrefix);

            PatchStat(harmony, targets.MeleeDpsForItem);
            PatchStat(harmony, targets.RangedDpsForItem);
            PatchStat(harmony, targets.ArmorPenForItem);
            PatchStat(harmony, targets.FireRateForItem);
            PatchStat(harmony, targets.AtkSpeedForItem);
        }

        /// <summary>
        /// 安装悬浮提示补丁；提示只是增强项，单个入口失败不得回滚核心属性同步。
        /// </summary>
        private void PatchOptionalTooltips(Harmony harmony)
        {
            TryPatchTooltip(harmony, targets.WeaponReloadTimeShort);
            TryPatchTooltip(harmony, targets.WeaponStoppingPower);
            TryPatchTooltip(harmony, targets.WeaponFireRate);
            TryPatchTooltip(harmony, targets.WeaponMeleeAttackSpeed);
            TryPatchTooltip(harmony, targets.WeaponMeleeAccuracy);
            TryPatchTooltip(harmony, targets.WeaponMeleeDamage);
        }

        private void PatchStat(Harmony harmony, MethodInfo method)
        {
            PatchMethod(harmony,
                method,
                prefix: NiceInventoryTabPatches.CreateStatPrefix(method.Name));
        }

        /// <summary>按需安装提示补丁；旧版 NIT 缺少某个提示方法时不影响其它兼容入口。</summary>
        private void TryPatchTooltip(Harmony harmony, MethodInfo method)
        {
            if (method == null)
            {
                return;
            }

            try
            {
                PatchMethod(harmony, method, prefix: NiceInventoryTabPatches.TooltipPrefix);
            }
            catch (Exception exception)
            {
                CompatibilityDiagnostics.Once(
                    Id,
                    "提示入口安装失败：" + method.Name + "：" + exception.ToString());
            }
        }

        /// <summary>记录已安装目标，便于部分失败或熔断时只撤销本适配器。</summary>
        private void PatchMethod(
            Harmony harmony,
            MethodBase original,
            HarmonyMethod prefix = null,
            HarmonyMethod postfix = null,
            HarmonyMethod transpiler = null,
            HarmonyMethod finalizer = null)
        {
            harmony.Patch(original, prefix, postfix, transpiler, finalizer);
            patchedMethods.Add(original);
        }

        /// <summary>撤销当前适配器安装的全部补丁，避免外部版本不兼容时残留半套补丁。</summary>
        private void UnpatchSelf()
        {
            if (patchHarmony == null)
            {
                return;
            }

            foreach (MethodBase original in patchedMethods)
            {
                try
                {
                    patchHarmony.Unpatch(original, HarmonyPatchType.All, patchHarmony.Id);
                }
                catch
                {
                    // 单个撤销失败不能阻断其它目标清理。
                }
            }

            patchedMethods.Clear();
        }
    }
}
