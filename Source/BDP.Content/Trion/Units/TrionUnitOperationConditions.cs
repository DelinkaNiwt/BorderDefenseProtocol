using BDP.Core.Permissions;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Units
{
    /// <summary>默认业务规则的失败原因；不充当死亡或身体相位。</summary>
    public enum TrionUnitOperationBlocker
    {
        /// <summary>所有运行前置满足。</summary>
        None,
        /// <summary>默认地图许可不覆盖离图人物。</summary>
        OffMap,
        /// <summary>当前上游许可无效。</summary>
        Permission,
        /// <summary>没有有效控制归属。</summary>
        Ownership,
        /// <summary>归属方未覆盖该兵的带宽。</summary>
        Bandwidth
    }

    /// <summary>可替换的运行前置接点，不要求条件必须来自中继核心。</summary>
    public interface ITrionUnitOperationCondition
    {
        /// <summary>只读当前条件，不扣资源、不修改带宽分配。</summary>
        AcceptanceReport Evaluate(CompTrionUnit unit);
    }

    /// <summary>当前玩家规则组合地图许可、有效归属及带宽；非玩家免此前置。</summary>
    public sealed class TrionUnitOperationConditions : ITrionUnitOperationCondition
    {
        /// <summary>确定当前规则的首个阻塞，分别保留各项限制的语义。</summary>
        public static TrionUnitOperationBlocker EvaluateFacts(bool isPlayer, bool hasMap,
            bool hasPermission, bool hasOwner, bool covered)
        {
            if (!isPlayer) return TrionUnitOperationBlocker.None;
            if (!hasMap) return TrionUnitOperationBlocker.OffMap;
            if (!hasPermission) return TrionUnitOperationBlocker.Permission;
            if (!hasOwner) return TrionUnitOperationBlocker.Ownership;
            return covered ? TrionUnitOperationBlocker.None : TrionUnitOperationBlocker.Bandwidth;
        }

        /// <summary>读取既有许可及归属提供者，具体建筑不进入兵的生命周期。</summary>
        public AcceptanceReport Evaluate(CompTrionUnit unit)
        {
            Pawn pawn = unit.Pawn;
            bool player = pawn.Faction == Faction.OfPlayer;
            ITrionUnitBandwidthSource source = player ? unit.BandwidthSource : null;
            bool validOwner = source != null && source.CanBind(pawn).Accepted;
            TrionUnitOperationBlocker blocker = EvaluateFacts(player, pawn.Map != null,
                !player || MapPermissionAccess.HasPermission(pawn.Map, pawn.Faction, unit.Settings.operationPermissionKey),
                !player || validOwner,
                !player || (validOwner && source.Covers(pawn)));
            return blocker == TrionUnitOperationBlocker.None
                ? AcceptanceReport.WasAccepted
                : new AcceptanceReport(("BDP_TrionUnit_Block_" + blocker).Translate());
        }
    }
}
