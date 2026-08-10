using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Core.Trigger;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器装配台建筑。
    /// 入口只负责把小人带到原版 Job 流程，实际装配真值仍交给 Trigger 正式命令面。
    /// </summary>
    public class Building_TriggerAssembler : Building
    {
        /// <summary>
        /// 玩家右键菜单显示的正式入口文本。
        /// </summary>
        private static string UseFloatMenuLabel
        {
            get { return "BDP_Command_TriggerAssembly_Use".Translate(); }
        }

        /// <summary>
        /// 读取当前建筑是否满足通电要求。
        /// 没有 PowerComp 时视为可用，便于 Def 调整时保持原版兼容。
        /// </summary>
        public bool IsPoweredForAssembly
        {
            get
            {
                CompPowerTrader power = this.TryGetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }

        /// <summary>
        /// 生成玩家右键装配入口。
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

            JobDef jobDef = AssemblyJobDefs.UseTriggerAssembler;
            if (jobDef == null)
            {
                yield return new FloatMenuOption("BDP_Message_TriggerAssembly_MissingJobDef".Translate(), null);
                yield break;
            }

            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(UseFloatMenuLabel, delegate
                {
                    Job job = JobMaker.MakeJob(jobDef, this);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }),
                selPawn,
                this);
        }

        /// <summary>
        /// 判断指定小人现在能否打开装配窗口。
        /// Job 到达交互格后仍会调用这里做一次最终确认。
        /// </summary>
        internal bool CanPawnOpenAssemblyWindow(Pawn pawn)
        {
            return ResolveUseRejection(pawn, false).NullOrEmpty();
        }

        /// <summary>
        /// 返回当前小人不能使用装配台的首个原因；返回 null 表示可用。
        /// </summary>
        internal string ResolveUseRejection(Pawn pawn, bool checkReachability)
        {
            if (pawn == null)
            {
                return "BDP_Message_TriggerAssembly_NoPawn".Translate();
            }

            if (Destroyed || !Spawned)
            {
                return "BDP_Message_TriggerAssembly_Unavailable".Translate();
            }

            if (checkReachability && !pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return "BDP_Message_TriggerAssembly_Unreachable".Translate();
            }

            if (!IsPoweredForAssembly)
            {
                return "BDP_Message_TriggerAssembly_NoPower".Translate();
            }

            ITriggerLoadoutReader loadoutReader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            if (loadoutReader == null ||
                TriggerSurfaceAccess.ResolveLoadoutCommands(pawn) == null)
            {
                return "BDP_Message_TriggerAssembly_NoBody".Translate();
            }

            if (loadoutReader.LoadoutControlMode == TriggerLoadoutControlMode.PlayerNonConfigurable)
            {
                return "BDP_Message_TriggerAssembly_FixedByDefinition".Translate();
            }

            ICombatBodyReader combatBodyReader = CombatBodySurfaceAccess.ResolveReader(pawn);
            if (combatBodyReader != null && combatBodyReader.Phase == CombatBodyPhase.Active)
            {
                return "BDP_Message_TriggerAssembly_CombatBodyActive".Translate();
            }

            return null;
        }
    }
}
