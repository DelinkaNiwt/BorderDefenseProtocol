using System.Collections.Generic;

namespace BDP.Core.Trion.External
{
    /// <summary>
    /// Trion 状态条扩展徽标注册表。
    /// </summary>
    public static class TrionGizmoExtensionRegistry
    {
        /// <summary>
        /// 已注册的徽标扩展提供器列表。
        /// </summary>
        private static readonly List<ITrionGizmoExtensionProvider> providers = new List<ITrionGizmoExtensionProvider>();

        /// <summary>
        /// 已注册的右侧面板扩展提供器列表。
        /// </summary>
        private static readonly List<ITrionGizmoPanelExtensionProvider> panelProviders =
            new List<ITrionGizmoPanelExtensionProvider>();

        /// <summary>
        /// 注册徽标扩展提供器。
        /// </summary>
        public static void Register(ITrionGizmoExtensionProvider provider)
        {
            if (provider == null || providers.Contains(provider))
            {
                return;
            }

            providers.Add(provider);
        }

        /// <summary>
        /// 反注册徽标扩展提供器。
        /// </summary>
        public static void Unregister(ITrionGizmoExtensionProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            providers.Remove(provider);
        }

        /// <summary>
        /// 注册右侧面板扩展提供器。
        /// </summary>
        public static void RegisterPanel(ITrionGizmoPanelExtensionProvider provider)
        {
            if (provider == null || panelProviders.Contains(provider))
            {
                return;
            }

            panelProviders.Add(provider);
        }

        /// <summary>
        /// 反注册右侧面板扩展提供器。
        /// </summary>
        public static void UnregisterPanel(ITrionGizmoPanelExtensionProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            panelProviders.Remove(provider);
        }

        /// <summary>
        /// 获取当前已注册的右侧面板扩展提供器。
        /// 第一版由 Gizmo 容器只消费第一个有效面板。
        /// </summary>
        public static IEnumerable<ITrionGizmoPanelExtensionProvider> GetPanelProviders()
        {
            for (int i = 0; i < panelProviders.Count; i++)
            {
                if (panelProviders[i] != null)
                {
                    yield return panelProviders[i];
                }
            }
        }

        /// <summary>
        /// 获取当前上下文的全部扩展徽标。
        /// </summary>
        public static IEnumerable<TrionGizmoExtensionBadge> GetBadges(TrionGizmoExtensionContext context)
        {
            for (int i = 0; i < providers.Count; i++)
            {
                IEnumerable<TrionGizmoExtensionBadge> badges = providers[i].GetBadges(context);
                if (badges == null)
                {
                    continue;
                }

                foreach (TrionGizmoExtensionBadge badge in badges)
                {
                    if (badge != null)
                    {
                        yield return badge;
                    }
                }
            }
        }
    }
}
