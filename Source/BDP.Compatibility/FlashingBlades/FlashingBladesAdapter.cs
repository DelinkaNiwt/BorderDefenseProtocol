using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace BDP.Compatibility.FlashingBlades
{
    /// <summary>刀光剑影可选适配器；只持有自己的补丁，异常不影响其它兼容项。</summary>
    internal sealed class FlashingBladesAdapter : IExternalModCompatibilityAdapter
    {
        /// <summary>启动时已加载程序集。</summary>
        private readonly Assembly[] assemblies;
        /// <summary>已经安装的本适配目标。</summary>
        private readonly List<MethodInfo> patched = new List<MethodInfo>();
        /// <summary>本适配器补丁实例。</summary>
        private Harmony harmony;
        /// <summary>当前独立生命周期状态。</summary>
        private CompatibilityAdapterState state;
        /// <summary>仅发现已加载程序集，不主动加载外部模组。</summary>
        internal FlashingBladesAdapter(Assembly[] loaded) { assemblies = loaded; }
        /// <summary>稳定补丁标识。</summary>
        public string Id { get { return "flashingblades"; } }
        /// <summary>当前是否完成安装。</summary>
        public bool IsEnabled { get { return state == CompatibilityAdapterState.Enabled; } }

        /// <summary>完整校验后统一安装；任一步失败撤销已安装部分。</summary>
        public bool TryInitialize(Harmony patchHarmony, out string failureReason)
        {
            failureReason = null;
            if (state != CompatibilityAdapterState.Uninitialized) return IsEnabled;
            if (!assemblies.Any(value => value.GetType("DaoGuangJianYing.DGJY_ContextBuilder") != null))
            { state = CompatibilityAdapterState.NotPresent; return false; }
            try
            {
                FlashingBladesDiscovery targets = new FlashingBladesDiscovery(assemblies);
                FlashingBladesCatalog catalog = new FlashingBladesCatalog(targets);
                harmony = patchHarmony;
                FlashingBladesRuntimeBridge.Bind(targets, catalog, Fail);
                FlashingBladesWindowBridge.Bind(targets, catalog, Fail);
                Patch(targets, "Build", typeof(FlashingBladesRuntimeBridge), "BuildPostfix", true);
                Patch(targets, "Classify", typeof(FlashingBladesRuntimeBridge), "ClassifyPrefix");
                Patch(targets, "Spawn", typeof(FlashingBladesRuntimeBridge), "SpawnPrefix");
                Patch(targets, "Reach", typeof(FlashingBladesRuntimeBridge), "ReachPrefix");
                Patch(targets, "Scale", typeof(FlashingBladesRuntimeBridge), "ScalePrefix");
                Patch(targets, "Filter", typeof(FlashingBladesWindowBridge), "FilterPostfix", true);
                Patch(targets, "Row", typeof(FlashingBladesWindowBridge), "RowPrefix");
                Patch(targets, "ModMenu", typeof(FlashingBladesWindowBridge), "ModMenuPrefix");
                state = CompatibilityAdapterState.Enabled;
                return true;
            }
            catch (Exception error)
            {
                Disable(null);
                failureReason = "BDP_FB_Failed".Translate(error.GetType().Name + ": " + error.Message).ToString();
                return false;
            }
        }

        /// <summary>登记自己的补丁目标，既有 NIT 补丁不受影响。</summary>
        private void Patch(FlashingBladesDiscovery targets, string key, Type bridge, string method, bool postfix = false)
        {
            MethodInfo original = targets.Methods[key];
            // 先登记，防止 Harmony 安装过程抛错时留下半成品。
            patched.Add(original);
            HarmonyMethod patch = new HarmonyMethod(AccessTools.Method(bridge, method));
            harmony.Patch(original, prefix: postfix ? null : patch, postfix: postfix ? patch : null);
        }

        /// <summary>运行期异常只停用本适配，并记录一次完整诊断。</summary>
        private void Fail(Exception error) { Disable("BDP_FB_Failed".Translate(error.ToString()).ToString()); }

        /// <summary>移除本适配器补丁及播放缓存，不撤销其它适配器。</summary>
        public void Disable(string reason)
        {
            state = CompatibilityAdapterState.Disabled;
            FlashingBladesRuntimeBridge.Bind(null, null, null);
            FlashingBladesWindowBridge.Bind(null, null, null);
            if (harmony != null)
                foreach (MethodInfo method in patched)
                {
                    try { harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id); }
                    catch { /* 单个撤销失败不阻断其它清理；桥已关闭，不再接管任何上下文。 */ }
                }
            patched.Clear();
            if (!string.IsNullOrEmpty(reason)) CompatibilityDiagnostics.Once(Id, reason);
        }
    }
}
