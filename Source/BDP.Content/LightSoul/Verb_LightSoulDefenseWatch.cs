using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂防护的正式“注视警戒”Verb（行为器）。
    /// 手动目标选择、XML 射程和自动索敌共用该实例；它只保存警戒目标，永远不施放攻击。
    /// </summary>
    public sealed class Verb_LightSoulDefenseWatch : Verb
    {
        /// <summary>
        /// 自动注视当前锁定的最近合法目标。
        /// 该结果按原版等待作业的自动攻击检查节奏更新，无需单独写入存档。
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
            Job job = JobMaker.MakeJob(LightSoulDefenseDefOf.BDP_LightSoulDefenseWatch, target);
            job.verbToUse = this;
            job.playerForced = true;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        /// <summary>
        /// 在原版等待作业的自动攻击检查时点刷新自动警戒目标。
        /// 目标查找器读取的当前有效 Verb 就是本实例，因此 XML range 是唯一射程来源。
        /// 只要最近合法目标身份不变，就保留当前锁定，避免原版评分随机造成抖动。
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
            Thing nearestTarget = FindNearestLegalAutomaticTarget(pawn, flags);
            if (nearestTarget == null)
            {
                ClearWatchTarget();
                return;
            }

            if (!automaticWatchTarget.IsValid || automaticWatchTarget.Thing != nearestTarget)
            {
                automaticWatchTarget = nearestTarget;
            }
        }

        /// <summary>
        /// 在原版攻击目标缓存中筛选当前合法目标，并按距离平方选择最近者。
        /// 不调用原版评分加权随机，以保证最近目标未变时锁定身份不变。
        /// </summary>
        private Thing FindNearestLegalAutomaticTarget(Pawn pawn, TargetScanFlags flags)
        {
            List<IAttackTarget> potentialTargets = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
            Thing nearestTarget = null;
            float nearestDistanceSquared = float.MaxValue;
            int nearestThingId = int.MaxValue;

            for (int i = 0; i < potentialTargets.Count; i++)
            {
                IAttackTarget attackTarget = potentialTargets[i];
                Thing target = attackTarget?.Thing;
                if (!IsLegalAutomaticTarget(pawn, attackTarget, target, flags))
                {
                    continue;
                }

                float distanceSquared = pawn.Position.DistanceToSquared(target.Position);
                int thingId = target.thingIDNumber;
                if (nearestTarget == null
                    || distanceSquared < nearestDistanceSquared
                    || (distanceSquared == nearestDistanceSquared && thingId < nearestThingId))
                {
                    nearestTarget = target;
                    nearestDistanceSquared = distanceSquared;
                    nearestThingId = thingId;
                }
            }

            return nearestTarget;
        }

        /// <summary>
        /// 对齐原版 BestAttackTarget 当前使用的自动目标合法性边界。
        /// </summary>
        private bool IsLegalAutomaticTarget(
            Pawn pawn,
            IAttackTarget attackTarget,
            Thing target,
            TargetScanFlags flags)
        {
            if (attackTarget == null
                || target == null
                || attackTarget == pawn
                || !target.Spawned
                || target.Map != pawn.Map
                || !pawn.HostileTo(target))
            {
                return false;
            }

            Lord lord = LordUtility.GetLord(pawn);
            if (lord?.LordJob != null && !lord.LordJob.ValidateAttackTarget(pawn, target))
            {
                return false;
            }

            if ((flags & TargetScanFlags.NeedLOSToAll) != TargetScanFlags.None
                && !pawn.CanSee(target))
            {
                return false;
            }

            if (((flags & TargetScanFlags.NeedThreat) != TargetScanFlags.None
                    || (flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None)
                && attackTarget.ThreatDisabled(pawn))
            {
                return false;
            }

            if ((flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None
                && !AttackTargetFinder.IsAutoTargetable(attackTarget))
            {
                return false;
            }

            if (ShouldIgnoreNonCombatant(pawn, attackTarget, flags)
                || !IsVisibleToPlayerControlledSearcher(pawn, target)
                || !CanHitTargetFrom(pawn.Position, target))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 对齐原版对非战斗 Pawn 的自动目标过滤。
        /// 当前注视使用 NeedLOSToAll，但仍保留原版直接视线边界。
        /// </summary>
        private static bool ShouldIgnoreNonCombatant(
            Pawn searcher,
            IAttackTarget target,
            TargetScanFlags flags)
        {
            Pawn pawn = target as Pawn;
            if (pawn == null || pawn.IsCombatant())
            {
                return false;
            }

            if ((flags & TargetScanFlags.IgnoreNonCombatants) != TargetScanFlags.None)
            {
                return true;
            }

            return !GenSight.LineOfSightToThing(searcher.Position, pawn, searcher.Map);
        }

        /// <summary>
        /// 对齐原版玩家控制人物的迷雾目标过滤，避免选中完全不可见的目标。
        /// </summary>
        private static bool IsVisibleToPlayerControlledSearcher(Pawn searcher, Thing target)
        {
            if (!searcher.IsColonist && searcher.Faction != Faction.OfPlayer)
            {
                return true;
            }

            if (target.def.size.x == 1 && target.def.size.z == 1)
            {
                return !target.Position.Fogged(target.Map);
            }

            foreach (IntVec3 cell in target.OccupiedRect())
            {
                if (!cell.Fogged(target.Map))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试读取当前仍符合原版战斗等待条件的自动警戒目标。
        /// 自动目标由原版周期确认最近合法者；不满足条件时本轮不覆盖人物朝向。
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
            if (curJob?.def == LightSoulDefenseDefOf.BDP_LightSoulDefenseWatch
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
        /// 注视只在等待且不忙碌时工作，合法近战的忙碌姿态自然拥有朝向。
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
