using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂举盾的正式“注视警戒”Verb（行为器）。
    /// 手动目标选择、XML 射程和自动索敌共用该实例；它只保存警戒目标，永远不施放攻击。
    /// </summary>
    public sealed class Verb_LightSoulGuardWatch : Verb
    {
        /// <summary>
        /// 原版自动索敌最近一次选中的警戒目标。
        /// 该结果会按原版等待作业的自动攻击检查节奏重算，无需单独写入存档。
        /// </summary>
        private LocalTargetInfo automaticWatchTarget = LocalTargetInfo.Invalid;

        /// <summary>
        /// 注视警戒支持原版目标选择，但隐藏与射击命中率有关的人物提示。
        /// </summary>
        public override bool HidePawnTooltips => true;

        /// <summary>
        /// 玩家选定目标后，下达只站立并持续警戒的正式作业。
        /// 首次选择时由本 Verb 校验射程和视线；锁定后按原版强制攻击语义保留目标。
        /// </summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Pawn pawn = CasterPawn;
            if (pawn?.jobs == null
                || !pawn.Spawned
                || !pawn.Drafted
                || !target.IsValid
                || !Available()
                || !ValidateTarget(target)
                || !CanHitTarget(target))
            {
                return;
            }

            ClearWatchTarget();
            Job job = JobMaker.MakeJob(LightSoulGuardDefOf.BDP_LightSoulGuardWatch, target);
            job.verbToUse = this;
            job.playerForced = true;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        /// <summary>
        /// 在原版等待作业的自动攻击检查时点刷新自动警戒目标。
        /// 目标查找器读取的当前有效 Verb 就是本实例，因此 XML range 是唯一射程来源。
        /// </summary>
        internal void RefreshAutomaticWatchTarget(JobDriver_Wait waitDriver)
        {
            Pawn pawn = CasterPawn;
            if (!CanUseVanillaAutoAttackContext(waitDriver, pawn) || EffectiveRange <= 0f)
            {
                ClearWatchTarget();
                return;
            }

            TargetScanFlags flags = TargetScanFlags.NeedLOSToAll
                | TargetScanFlags.NeedThreat
                | TargetScanFlags.NeedAutoTargetable;
            Thing target = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(
                pawn,
                flags,
                null,
                0f,
                EffectiveRange);
            automaticWatchTarget = target ?? LocalTargetInfo.Invalid;
        }

        /// <summary>
        /// 尝试读取当前仍符合原版战斗等待条件的自动警戒目标。
        /// 自动目标由原版周期重新搜索；不满足条件时本轮不覆盖人物朝向。
        /// </summary>
        internal bool TryGetAutomaticWatchTarget(out LocalTargetInfo target)
        {
            target = automaticWatchTarget;
            Pawn pawn = CasterPawn;
            if (!target.IsValid
                || !(pawn?.jobs?.curDriver is JobDriver_Wait waitDriver)
                || !CanUseVanillaAutoAttackContext(waitDriver, pawn)
                || !CanHitTarget(target))
            {
                target = LocalTargetInfo.Invalid;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 尝试读取当前实际驱动注视警戒的目标。
        /// 手动作业优先；没有可用手动目标时回退到原版等待节奏维护的自动目标。
        /// </summary>
        internal bool TryGetCurrentWatchTarget(out LocalTargetInfo target)
        {
            target = LocalTargetInfo.Invalid;
            Job curJob = CasterPawn?.jobs?.curJob;
            if (curJob?.def == LightSoulGuardDefOf.BDP_LightSoulGuardWatch
                && curJob.verbToUse == this)
            {
                LocalTargetInfo manualTarget = curJob.targetA;
                if (manualTarget.IsValid && CanHitTarget(manualTarget))
                {
                    target = manualTarget;
                    return true;
                }
            }

            return TryGetAutomaticWatchTarget(out target);
        }

        /// <summary>
        /// 清除由自动索敌保存的警戒目标。
        /// </summary>
        internal void ClearWatchTarget()
        {
            automaticWatchTarget = LocalTargetInfo.Invalid;
        }

        /// <summary>
        /// 原版重置 Verb 时同步清理自动警戒状态。
        /// </summary>
        public override void Reset()
        {
            ClearWatchTarget();
            base.Reset();
        }

        /// <summary>
        /// 判断人物当前是否仍处于原版允许自动远程攻击的等待上下文。
        /// 唯一跳过的是“允许暴力”条件，因为本 Verb 明确不执行暴力动作。
        /// </summary>
        private static bool CanUseVanillaAutoAttackContext(JobDriver_Wait waitDriver, Pawn pawn)
        {
            Job job = waitDriver?.job;
            return pawn != null
                && pawn.kindDef.canMeleeAttack
                && !pawn.Downed
                && !pawn.stances.FullBodyBusy
                && !pawn.IsCarryingPawn()
                && (pawn.IsPlayerControlled || !pawn.IsPsychologicallyInvisible())
                && !pawn.IsShambler
                && job != null
                && job.canUseRangedWeapon
                && job.def == JobDefOf.Wait_Combat
                && (pawn.drafter == null || pawn.drafter.FireAtWill);
        }

        /// <summary>
        /// 防御性兜底：即使未来误接到施放链，也绝不生成攻击、伤害或移动。
        /// </summary>
        protected override bool TryCastShot()
        {
            return false;
        }
    }
}
