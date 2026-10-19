using System.Collections.Generic;
using BDP.Core.CombatBody;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 战斗体编辑台建筑入口。
    /// </summary>
    public sealed class Building_CombatBodyEditor : Building
    {
        /// <summary>
        /// 读取当前建筑是否满足原版通电要求。
        /// </summary>
        public bool IsPoweredForAppearanceEditing
        {
            get
            {
                CompPowerTrader power = this.TryGetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }

        /// <summary>
        /// 生成玩家右键打开战斗体编辑台的入口。
        /// </summary>
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            string rejectReason = ResolveUseRejection(selPawn, true);
            if (!rejectReason.NullOrEmpty())
            {
                yield return new FloatMenuOption(rejectReason, null);
                yield break;
            }

            JobDef jobDef = CombatBodyEditorJobDefs.UseCombatBodyEditor;
            if (jobDef == null)
            {
                yield return new FloatMenuOption(
                    "BDP_Message_CombatBodyEditor_MissingJobDef".Translate(),
                    null);
                yield break;
            }

            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "BDP_Command_CombatBodyEditor_Use".Translate(),
                    delegate
                    {
                        Job job = JobMaker.MakeJob(jobDef, this, selPawn);
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }),
                selPawn,
                this);
        }

        /// <summary>
        /// 判断 Pawn 到达交互格后是否仍可打开编辑窗口。
        /// </summary>
        internal bool CanPawnOpenAppearanceEditor(Pawn pawn)
        {
            return ResolveUseRejection(pawn, false).NullOrEmpty();
        }

        /// <summary>
        /// 返回当前不可用的首个原因；返回 null 表示可以继续。
        /// </summary>
        internal string ResolveUseRejection(Pawn pawn, bool checkReachability)
        {
            if (pawn == null)
            {
                return "BDP_Message_CombatBodyEditor_NoPawn".Translate();
            }

            if (Destroyed || !Spawned)
            {
                return "BDP_Message_CombatBodyEditor_Unavailable".Translate();
            }

            if (checkReachability
                && !pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return "BDP_Message_CombatBodyEditor_Unreachable".Translate();
            }

            if (!IsPoweredForAppearanceEditing)
            {
                return "BDP_Message_CombatBodyEditor_NoPower".Translate();
            }

            ICombatBodyReader combatBodyReader = CombatBodySurfaceAccess.ResolveReader(pawn);
            if (combatBodyReader != null
                && (combatBodyReader.Phase == CombatBodyPhase.Active
                    || combatBodyReader.Phase == CombatBodyPhase.Collapsing))
            {
                return "BDP_Message_CombatBodyEditor_CombatBodyActive".Translate();
            }

            return null;
        }
    }
}
