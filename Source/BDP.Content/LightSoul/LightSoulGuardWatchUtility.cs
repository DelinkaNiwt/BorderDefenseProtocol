using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂举盾“注视警戒”的最小业务工具。
    /// 只负责姿态切入清理、正式 Verb 查询和警戒作业收尾。
    /// </summary>
    internal static class LightSoulGuardWatchUtility
    {
        /// <summary>
        /// BDP 远程持续攻击作业的定义名。
        /// </summary>
        private const string BdpRangedAttackJobDefName = "BDP_RangedAttackExecution";

        /// <summary>
        /// BDP 近战持续攻击作业的定义名。
        /// </summary>
        private const string BdpMeleeAttackJobDefName = "BDP_MeleeAttackExecution";

        /// <summary>
        /// 进入举盾姿态时只取消攻击相关状态，不触碰装备和无关命令。
        /// </summary>
        internal static void CancelAttackState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (Current.ProgramState == ProgramState.Playing
                && Find.Targeter != null
                && Find.Targeter.IsPawnTargeting(pawn))
            {
                Find.Targeter.StopTargeting();
            }

            pawn.jobs?.jobQueue?.RemoveAll(pawn, IsAttackJob);

            Job currentJob = pawn.jobs?.curJob;
            Stance_Busy busyStance = pawn.stances?.curStance as Stance_Busy;
            bool hasViolentBusyStance = busyStance?.verb?.verbProps != null
                && busyStance.verb.verbProps.violent;
            bool hasAttackJob = IsAttackJob(currentJob);
            if (hasViolentBusyStance || hasAttackJob)
            {
                pawn.stances?.CancelBusyStanceHard();
            }

            if (hasAttackJob && pawn.jobs?.curDriver != null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        /// <summary>
        /// 从人物健康状态中读取当前举盾姿态持有的正式注视警戒 Verb。
        /// </summary>
        internal static Verb_LightSoulGuardWatch ResolveVerb(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return null;
            }

            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                HediffWithComps hediff = pawn.health.hediffSet.hediffs[i] as HediffWithComps;
                HediffComp_LightSoulGuardWatch comp = hediff?.TryGetComp<HediffComp_LightSoulGuardWatch>();
                Verb_LightSoulGuardWatch verb = comp?.WatchVerb;
                if (verb != null)
                {
                    return verb;
                }
            }

            return null;
        }

        /// <summary>
        /// 仅在当前作业确由指定注视警戒 Verb 发起时结束它。
        /// </summary>
        internal static void EndManualWatchJob(Pawn pawn, Verb_LightSoulGuardWatch verb)
        {
            if (pawn?.jobs?.curJob?.def != LightSoulGuardDefOf.BDP_LightSoulGuardWatch
                || pawn.jobs.curJob.verbToUse != verb)
            {
                return;
            }

            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        /// <summary>
        /// 判断指定作业是否属于原版或 BDP 的正式攻击作业。
        /// </summary>
        private static bool IsAttackJob(Job job)
        {
            if (job?.def == null)
            {
                return false;
            }

            return job.def == JobDefOf.AttackStatic
                || job.def == JobDefOf.AttackMelee
                || job.def.defName == BdpRangedAttackJobDefName
                || job.def.defName == BdpMeleeAttackJobDefName;
        }
    }
}
