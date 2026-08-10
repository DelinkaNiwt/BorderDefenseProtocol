using BDP.Core.CombatBody;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.CombatBodySession
{
    /// <summary>
    /// 战斗会话的薄策略判断。
    /// 它只回答跨系统判断问题，不持有会话真值。
    /// </summary>
    internal sealed class CombatBodySessionPolicy
    {
        /// <summary>
        /// 解析当前 Pawn 主武器上的 Trigger。
        /// </summary>
        public bool TryResolvePrimaryTrigger(Pawn pawn, out CompTriggerBody trigger)
        {
            trigger = TriggerSurfaceAccess.ResolveComp(pawn);
            return trigger != null;
        }

        /// <summary>
        /// 判断当前 Pawn 是否已经处于战斗体开启状态。
        /// </summary>
        public bool IsBattleModeActive(Pawn pawn)
        {
            ICombatBodyReader reader = CombatBodySurfaceAccess.ResolveReader(pawn);
            return reader != null && reader.Phase == CombatBodyPhase.Active;
        }

        /// <summary>
        /// 判断当前 Trigger 是否应该对外发布战斗投影。
        /// </summary>
        public bool ShouldPublishCombatProjection(Pawn pawn, CompTriggerBody trigger)
        {
            if (trigger == null)
            {
                return false;
            }

            return ReferenceEquals(TriggerSurfaceAccess.ResolveComp(pawn), trigger) && IsBattleModeActive(pawn);
        }
    }
}

