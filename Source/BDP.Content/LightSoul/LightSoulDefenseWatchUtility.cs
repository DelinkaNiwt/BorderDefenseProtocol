using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂防护“注视警戒”的最小业务工具。
    /// 只负责防护形态切入清理、正式 Verb 查询和警戒作业收尾。
    /// </summary>
    internal static class LightSoulDefenseWatchUtility
    {
        /// <summary>
        /// BDP 远程持续攻击作业的定义名。
        /// </summary>
        private const string BdpRangedAttackJobDefName = "BDP_RangedAttackExecution";

        /// <summary>
        /// 判断当前行为是否属于防护形态禁止的远程武器攻击；位移和非攻击能力不受影响。
        /// </summary>
        internal static bool IsRangedWeapon(Verb verb)
        {
            return verb?.verbProps != null && !verb.verbProps.IsMeleeAttack
                && (verb is Verb_LaunchProjectile || (verb.verbProps.ai_IsWeapon && verb.verbProps.violent));
        }

        /// <summary>远程限制只取决于防护形态，与护盾是否破损无关。</summary>
        internal static bool IsRangedBlocked(Verb verb)
        {
            return IsRangedWeapon(verb) && ResolveVerb(verb.CasterPawn) != null;
        }

        /// <summary>
        /// 进入防护形态时只取消远程状态，保留近战、装备和无关命令。
        /// </summary>
        internal static void CancelRangedAttackState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (Current.ProgramState == ProgramState.Playing
                && Find.Targeter != null
                && Find.Targeter.IsPawnTargeting(pawn)
                && IsRangedWeapon(Find.Targeter.targetingSource?.GetVerb))
            {
                Find.Targeter.StopTargeting();
            }

            pawn.jobs?.jobQueue?.RemoveAll(pawn, IsRangedAttackJob);

            Job currentJob = pawn.jobs?.curJob;
            Stance_Busy busyStance = pawn.stances?.curStance as Stance_Busy;
            bool hasRangedBusyStance = IsRangedWeapon(busyStance?.verb);
            bool hasAttackJob = IsRangedAttackJob(currentJob);
            if (hasRangedBusyStance)
            {
                busyStance.verb.Reset();
                pawn.stances?.CancelBusyStanceHard();
            }

            if (hasAttackJob && pawn.jobs?.curDriver != null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        /// <summary>
        /// 从人物健康状态中读取当前防护形态持有的正式注视警戒 Verb。
        /// </summary>
        internal static Verb_LightSoulDefenseWatch ResolveVerb(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return null;
            }

            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                HediffWithComps hediff = pawn.health.hediffSet.hediffs[i] as HediffWithComps;
                HediffComp_LightSoulDefenseWatch comp = hediff?.TryGetComp<HediffComp_LightSoulDefenseWatch>();
                Verb_LightSoulDefenseWatch verb = comp?.WatchVerb;
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
        internal static void EndManualWatchJob(Pawn pawn, Verb_LightSoulDefenseWatch verb)
        {
            if (pawn?.jobs?.curJob?.def != LightSoulDefenseDefOf.BDP_LightSoulDefenseWatch
                || pawn.jobs.curJob.verbToUse != verb)
            {
                return;
            }

            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        /// <summary>
        /// 判断指定作业是否属于远程攻击；近战作业不参与防护形态切入清理。
        /// </summary>
        private static bool IsRangedAttackJob(Job job)
        {
            if (job?.def == null)
            {
                return false;
            }

            return job.def == JobDefOf.AttackStatic
                || job.def.defName == BdpRangedAttackJobDefName
                || IsRangedWeapon(job.verbToUse);
        }
    }
}
