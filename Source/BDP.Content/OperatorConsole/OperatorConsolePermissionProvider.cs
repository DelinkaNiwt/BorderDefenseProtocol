using BDP.Core.Permissions;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Content.OperatorConsole
{
    /// <summary>
    /// 操作员控制台的下游地图许可提供器。
    /// 它只读取控制台统一运作状态，不重复组合任何前置条件。
    /// </summary>
    public sealed class OperatorConsolePermissionProvider : IMapPermissionProvider
    {
        /// <summary>当前提供许可的控制台建筑。</summary>
        private readonly Building_OperatorConsole console;

        /// <summary>使用指定控制台建立许可提供器。</summary>
        /// <param name="console">控制台建筑。</param>
        public OperatorConsolePermissionProvider(Building_OperatorConsole console)
        {
            this.console = console;
        }

        /// <summary>提供器所属地图。</summary>
        public Map Map
        {
            get { return console != null ? console.Map : null; }
        }

        /// <summary>许可归属控制台所属派系。</summary>
        public Faction Faction
        {
            get { return console != null ? console.Faction : null; }
        }

        /// <summary>操作员控制台活动许可键。</summary>
        public string PermissionKey
        {
            get { return OperatorConsolePermissionKeys.OperatorConsoleActive; }
        }

        /// <summary>唯一下游前提：控制台自身处于运作状态。</summary>
        public bool IsActive
        {
            get
            {
                bool active = console != null && console.IsOperational;
                BdpDiagnostics.Throttled(
                    "permission.operator_console.active." + (console != null ? console.ThingID : "null"),
                    "操作员控制台许可状态 | thing=" + (console != null ? console.ThingID : "null")
                    + " | operational=" + active
                    + " | forced=" + (console != null && console.IsForcedOperational),
                    60);
                return active;
            }
        }
    }
}
