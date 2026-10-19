using System.Collections.Generic;
using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Units
{
    /// <summary>兵的玩家界面入口，复用资源状态条及原版切换按钮。</summary>
    public sealed partial class CompTrionUnit
    {
        /// <summary>玩家兵初始化后显示资源；停机仍可查看生命余额，自动战斗按钮沿原规则提供。</summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            if (Pawn == null || Pawn.Dead || Pawn.Destroyed || Pawn.Faction?.IsPlayer != true) yield break;

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(Pawn);
            if (reader != null && reader.Max > 0f && TrionResourceRuleRegistry.GetRules(Pawn).CapacityInfoVisible)
                yield return new Gizmo_TrionStatus(Pawn, reader);

            yield return new Command_Toggle
            {
                defaultLabel = "BDP_TrionUnit_AutoCombat".Translate(),
                defaultDesc = "BDP_TrionUnit_AutoCombatDesc".Translate(),
                icon = TexCommand.Attack,
                isActive = () => autoCombatEnabled,
                toggleAction = () => SetAutoCombatEnabled(!autoCombatEnabled)
            };
        }
    }
}
