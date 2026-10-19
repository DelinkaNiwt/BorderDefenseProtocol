using System.Collections.Generic;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 地图内许可提供者索引。
    /// 地图许可和 Pawn 自身许可列表都属于运行时索引，不进入存档。
    /// </summary>
    public sealed class MapPermissionMapComponent : MapComponent, IMapPermissionReader, IPawnPermissionReader
    {
        /// <summary>当前地图已经登记的许可提供者。</summary>
        private HashSet<IMapPermissionProvider> providers = new HashSet<IMapPermissionProvider>();

        /// <summary>当前地图已经登记的 Pawn 自身许可提供者。</summary>
        private HashSet<IPawnPermissionProvider> pawnProviders =
            new HashSet<IPawnPermissionProvider>();

        /// <summary>当前地图已经登记的许可状态监听者。</summary>
        private HashSet<IMapPermissionStateChangeListener> stateChangeListeners =
            new HashSet<IMapPermissionStateChangeListener>();

        /// <summary>使用当前地图创建许可索引。</summary>
        /// <param name="map">当前地图。</param>
        public MapPermissionMapComponent(Map map) : base(map)
        {
        }

        /// <summary>地图初始化完成时保留可用索引，并清理离图提供者。</summary>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (providers == null)
            {
                providers = new HashSet<IMapPermissionProvider>();
            }

            if (pawnProviders == null)
            {
                pawnProviders = new HashSet<IPawnPermissionProvider>();
            }
        }

        /// <summary>登记一个运行时许可提供者。</summary>
        /// <param name="provider">准备登记的提供者。</param>
        public void Register(IMapPermissionProvider provider)
        {
            if (provider == null || provider.Map != map)
            {
                return;
            }

            if (providers == null)
            {
                providers = new HashSet<IMapPermissionProvider>();
            }

            providers.Add(provider);
            BdpDiagnostics.Throttled(
                "permission.register." + provider.GetType().FullName,
                "地图许可提供器已登记 | provider=" + provider.GetType().FullName
                + " | key=" + provider.PermissionKey
                + " | providerCount=" + providers.Count,
                60);
        }

        /// <summary>移除一个已经离图的提供者。</summary>
        /// <param name="provider">准备移除的提供者。</param>
        public void Unregister(IMapPermissionProvider provider)
        {
            providers?.Remove(provider);
        }

        /// <summary>登记一个运行时 Pawn 自身许可提供者。</summary>
        /// <param name="provider">准备登记的 Pawn 自身许可提供者。</param>
        public void RegisterPawnProvider(IPawnPermissionProvider provider)
        {
            if (provider == null || provider.Pawn == null || provider.Pawn.Map != map)
            {
                return;
            }

            if (pawnProviders == null)
            {
                pawnProviders = new HashSet<IPawnPermissionProvider>();
            }

            pawnProviders.Add(provider);
            BdpDiagnostics.Throttled(
                "permission.self.register." + provider.GetType().FullName,
                "Pawn 自身许可提供器已登记 | provider=" + provider.GetType().FullName
                + " | pawn=" + provider.Pawn.ThingID
                + " | key=" + provider.PermissionKey
                + " | providerCount=" + pawnProviders.Count,
                60);
        }

        /// <summary>移除一个已经失效或离图的 Pawn 自身许可提供者。</summary>
        /// <param name="provider">准备移除的 Pawn 自身许可提供者。</param>
        public void UnregisterPawnProvider(IPawnPermissionProvider provider)
        {
            pawnProviders?.Remove(provider);
        }

        /// <summary>登记一个许可状态监听者。</summary>
        /// <param name="listener">准备接收许可状态变化通知的监听者。</param>
        public void RegisterListener(IMapPermissionStateChangeListener listener)
        {
            if (listener == null || listener.Map != map)
            {
                return;
            }

            if (stateChangeListeners == null)
            {
                stateChangeListeners = new HashSet<IMapPermissionStateChangeListener>();
            }

            stateChangeListeners.Add(listener);
        }

        /// <summary>移除一个已经离图的许可状态监听者。</summary>
        /// <param name="listener">准备移除的监听者。</param>
        public void UnregisterListener(IMapPermissionStateChangeListener listener)
        {
            stateChangeListeners?.Remove(listener);
        }

        /// <summary>
        /// 通知监听者某个地图许可状态可能已经变化。
        /// 许可含义与后续处理由各监听者自行决定，Core 不参与业务解释。
        /// </summary>
        /// <param name="permissionKey">发生变化的许可键。</param>
        /// <param name="faction">发生变化的许可归属派系。</param>
        public void NotifyPermissionStateChanged(string permissionKey, Faction faction)
        {
            if (string.IsNullOrWhiteSpace(permissionKey) || faction == null || stateChangeListeners == null)
            {
                return;
            }

            List<IMapPermissionStateChangeListener> stale = null;
            foreach (IMapPermissionStateChangeListener listener in stateChangeListeners)
            {
                if (listener == null || listener.Map != map)
                {
                    (stale ?? (stale = new List<IMapPermissionStateChangeListener>())).Add(listener);
                    continue;
                }

                listener.NotifyPermissionStateChanged(permissionKey, faction);
            }

            if (stale != null)
            {
                for (int index = 0; index < stale.Count; index++)
                {
                    stateChangeListeners.Remove(stale[index]);
                }
            }
        }

        /// <summary>
        /// 查询许可；失效、离图或派系不匹配的提供者不会放行。
        /// </summary>
        public bool HasPermission(Map targetMap, Faction faction, string permissionKey)
        {
            if (targetMap == null || targetMap != map || faction == null || string.IsNullOrWhiteSpace(permissionKey))
            {
                return false;
            }

            if (providers == null || providers.Count == 0)
            {
                return false;
            }

            List<IMapPermissionProvider> stale = null;
            foreach (IMapPermissionProvider provider in providers)
            {
                if (provider == null || provider.Map != map)
                {
                    (stale ?? (stale = new List<IMapPermissionProvider>())).Add(provider);
                    continue;
                }

                bool matches = provider.Faction == faction
                    && provider.PermissionKey == permissionKey;
                bool isActive = matches && provider.IsActive;
                if (matches)
                {
                    BdpDiagnostics.Throttled(
                        "permission.query." + permissionKey,
                        "地图许可查询 | key=" + permissionKey
                        + " | provider=" + provider.GetType().FullName
                        + " | active=" + isActive
                        + " | providerCount=" + providers.Count,
                        60);
                }

                if (isActive)
                {
                    return true;
                }
            }

            if (stale != null)
            {
                for (int index = 0; index < stale.Count; index++)
                {
                    providers.Remove(stale[index]);
                }
            }

            BdpDiagnostics.Throttled(
                "permission.query.false." + permissionKey,
                "地图许可查询未通过 | key=" + permissionKey
                + " | providerCount=" + providers.Count,
                60);
            return false;
        }

        /// <summary>
        /// 查询 Pawn 自身许可；只允许与目标 Pawn 实例完全相同的提供者放行。
        /// </summary>
        public bool HasSelfPermission(Map targetMap, Pawn pawn, string permissionKey)
        {
            if (targetMap == null
                || targetMap != map
                || pawn == null
                || string.IsNullOrWhiteSpace(permissionKey))
            {
                return false;
            }

            if (pawnProviders == null || pawnProviders.Count == 0)
            {
                return false;
            }

            List<IPawnPermissionProvider> stale = null;
            foreach (IPawnPermissionProvider provider in pawnProviders)
            {
                if (provider == null || provider.Pawn == null || provider.Pawn.Map != map)
                {
                    (stale ?? (stale = new List<IPawnPermissionProvider>())).Add(provider);
                    continue;
                }

                bool matches = provider.Pawn == pawn
                    && provider.PermissionKey == permissionKey;
                bool isActive = matches && provider.IsActive;
                if (matches)
                {
                    BdpDiagnostics.Throttled(
                        "permission.self.query." + permissionKey,
                        "Pawn 自身许可查询 | pawn=" + pawn.ThingID
                        + " | key=" + permissionKey
                        + " | provider=" + provider.GetType().FullName
                        + " | active=" + isActive
                        + " | providerCount=" + pawnProviders.Count,
                        60);
                }

                if (isActive)
                {
                    return true;
                }
            }

            if (stale != null)
            {
                for (int index = 0; index < stale.Count; index++)
                {
                    pawnProviders.Remove(stale[index]);
                }
            }

            BdpDiagnostics.Throttled(
                "permission.self.query.false." + permissionKey,
                "Pawn 自身许可查询未通过 | pawn=" + pawn.ThingID
                + " | key=" + permissionKey
                + " | providerCount=" + pawnProviders.Count,
                60);
            return false;
        }

        /// <summary>
        /// 许可提供者索引不进入存档。
        /// 读档后由对应业务生命周期重新登记，并由查询时清理失效项。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            if (providers == null)
            {
                providers = new HashSet<IMapPermissionProvider>();
            }

            if (pawnProviders == null)
            {
                pawnProviders = new HashSet<IPawnPermissionProvider>();
            }

            if (stateChangeListeners == null)
            {
                stateChangeListeners = new HashSet<IMapPermissionStateChangeListener>();
            }
        }
    }
}
