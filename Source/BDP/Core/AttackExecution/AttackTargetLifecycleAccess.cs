using System.Collections.Generic;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击目标生命周期变化的中性通知入口。
    /// 目标倒地、死亡或恢复行动时，只在事件发生这一刻查找当前地图上的 BDP 宿主。
    /// </summary>
    internal static class AttackTargetLifecycleAccess
    {
        /// <summary>
        /// 通知所有当前地图上的 BDP 远程宿主重新检查指定目标的会话。
        /// </summary>
        internal static void NotifyTargetLifecycleChanged(Pawn target)
        {
            if (target == null || Find.Maps == null)
            {
                return;
            }

            foreach (Map map in Find.Maps)
            {
                IReadOnlyList<Pawn> pawns = map?.mapPawns?.AllPawnsSpawned;
                if (pawns == null)
                {
                    continue;
                }

                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn attacker = pawns[i];
                    if (attacker == null || attacker == target)
                    {
                        continue;
                    }

                    CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(attacker);
                    triggerBody?.VerbHostManager?.NotifyTargetLifecycleChanged(target);
                }
            }
        }
    }
}
