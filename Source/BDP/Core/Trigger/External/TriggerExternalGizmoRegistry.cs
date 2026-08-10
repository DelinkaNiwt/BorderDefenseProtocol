using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体额外按钮注册表。
    /// 主模组通过它统一收纳外部按钮扩展，避免外部模组直接去改正式按钮链路。
    /// </summary>
    public static class TriggerExternalGizmoRegistry
    {
        /// <summary>
        /// 当前已注册的外部按钮提供器。
        /// 按注册顺序参与按钮构建。
        /// </summary>
        private static readonly List<ITriggerExternalGizmoProvider> Providers = new List<ITriggerExternalGizmoProvider>();

        /// <summary>
        /// 当前是否存在已注册的外部按钮提供器。
        /// </summary>
        public static bool HasProviders
        {
            get { return Providers.Count > 0; }
        }

        /// <summary>
        /// 注册一个外部按钮提供器。
        /// 正常情况下每个外部模组在启动时注册一次即可。
        /// </summary>
        public static void Register(ITriggerExternalGizmoProvider provider)
        {
            if (provider == null || Providers.Contains(provider))
            {
                return;
            }

            Providers.Add(provider);
            BdpDiagnostics.Once("trigger.external_gizmo_provider." + provider.GetType().FullName, "触发体外部按钮提供器已注册: " + provider.GetType().FullName);
        }

        /// <summary>
        /// 按注册顺序收集所有外部按钮。
        /// 单个提供器失败时，不应拖垮其它提供器。
        /// </summary>
        public static IEnumerable<Gizmo> BuildGizmos(TriggerExternalGizmoContext context)
        {
            if (Providers.Count == 0)
            {
                yield break;
            }

            for (int i = 0; i < Providers.Count; i++)
            {
                // 每个提供器互相隔离，单个失败不拖垮全部按钮。
                ITriggerExternalGizmoProvider provider = Providers[i];
                IEnumerable<Gizmo> gizmos;
                try
                {
                    gizmos = provider.BuildGizmos(context);
                }
                catch (Exception ex)
                {
                    Log.Error("[BDP] TriggerExternalGizmoRegistry provider failed: " + provider.GetType().FullName + "\n" + ex);
                    continue;
                }

                if (gizmos == null)
                {
                    continue;
                }

                foreach (Gizmo gizmo in gizmos)
                {
                    // 跳过空按钮，保证最终输出干净。
                    if (gizmo != null)
                    {
                        yield return gizmo;
                    }
                }
            }
        }
    }
}
