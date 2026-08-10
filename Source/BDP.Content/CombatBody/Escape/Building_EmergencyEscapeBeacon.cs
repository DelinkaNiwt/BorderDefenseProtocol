using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离信标建筑。
    /// 它只提供可用锚点和未来 Trion 接口，不持有紧急脱离流程真值。
    /// </summary>
    public sealed class Building_EmergencyEscapeBeacon : Building
    {
        /// <summary>
        /// 当前建筑是否可作为紧急脱离锚点。
        /// </summary>
        public bool IsActiveAnchor
        {
            get
            {
                return Spawned
                    && !Destroyed
                    && Faction == Faction.OfPlayer
                    && IsPowered;
            }
        }

        /// <summary>
        /// 未来建筑 Trion 只读口。
        /// 当前版本只预留接口，不参与路由判断。
        /// </summary>
        public ITrionReader TrionReader
        {
            get { return TrionSurfaceAccess.ResolveReader((ThingWithComps)this); }
        }

        /// <summary>
        /// 未来建筑 Trion 命令口。
        /// 当前版本只预留接口，不参与路由判断。
        /// </summary>
        public ITrionCommands TrionCommands
        {
            get { return TrionSurfaceAccess.ResolveCommands((ThingWithComps)this); }
        }

        /// <summary>
        /// 当前供电是否满足启用要求。
        /// 没有供电组件时视为可用，便于 Def 调整时保持原版兼容。
        /// </summary>
        private bool IsPowered
        {
            get
            {
                CompPowerTrader power = this.TryGetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }
    }
}
