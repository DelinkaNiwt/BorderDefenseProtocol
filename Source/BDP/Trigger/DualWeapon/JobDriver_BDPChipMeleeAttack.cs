using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片近战攻击JobDriver（v6.0新增）——跟随目标+近战攻击+手动驱动burst计时。
    ///
    /// 解决两个问题：
    ///   1. 原版AttackMelee的JobDriver不调用VerbTick()，BDP Verb（不在VerbTracker中）
    ///      的burst计时器永远不推进，burstShotCount>1时卡在第1发。
    ///   2. 引擎的TicksBetweenBurstShots是固定值，无法按芯片配置不同间隔。
    ///      本Driver在VerbTick()之后调用ApplyPendingInterval()覆盖为芯片自定义间隔。
    ///
    /// 参照JobDriver_BDPChipRangedAttack模式，但使用近战跟随+攻击循环。
    /// </summary>
    public class JobDriver_BDPChipMeleeAttack : JobDriver
    {
        private int numMeleeAttacksMade;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref numMeleeAttacksMade, "numMeleeAttacksMade", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (job.targetA.Thing is IAttackTarget target)
                pawn.Map.attackTargetReservationManager.Reserve(pawn, job, target);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            // 自定义Toil：跟随+近战攻击+手动驱动VerbTick
            Toil followAndAttack = ToilMaker.MakeToil("BDPChipMeleeAttack");

            // ── 每tick调用：手动驱动芯片verb的burst计时 ──
            followAndAttack.tickAction = delegate
            {
                Verb verb = job.verbToUse;
                if (verb == null) return;

                try
                {
                    // 驱动burst计时器（芯片verb不在VerbTracker中，引擎不会调用VerbTick）
                    verb.VerbTick();

                    // 覆盖引擎设置的固定间隔为芯片自定义间隔
                    (verb as Verb_BDPMelee)?.ApplyPendingInterval();
                }
                catch (Exception ex)
                {
                    // 防御性保护：burst期间目标死亡等边界情况可能导致异常。
                    // 使用SafeAbortBurst清理所有BDP状态（包括hitIndex），
                    // 不能用verb.Reset()——它不清理hitIndex，导致burst无限重启。
                    Log.Warning($"[BDP Melee] tickAction异常: {ex}");
                    (verb as Verb_BDPMelee)?.SafeAbortBurst();
                }
            };

            // ── 按间隔调用：跟随目标+到达近战范围后攻击 ──
            // Bug8修复：条件从&&改为||，匹配原版FollowAndMeleeAttack逻辑。
            //   原版：target != destination || (!moving && !canReach) → 目标相同但走远也跟
            //   旧版：target != destination && (!moving || !canReach) → 目标相同时永远不跟
            followAndAttack.tickIntervalAction = delegate
            {
                try
                {
                    // burst进行中时不尝试新攻击——由VerbTick驱动后续打击
                    Verb verb = job.verbToUse;
                    if (verb != null && verb.state == VerbState.Bursting)
                        return;

                    Thing thing = job.GetTarget(TargetIndex.A).Thing;
                    if (thing == null || !thing.Spawned)
                    {
                        ReadyForNextToil();
                        return;
                    }

                    Pawn targetPawn = thing as Pawn;
                    if (targetPawn != null && targetPawn.IsPsychologicallyInvisible())
                    {
                        ReadyForNextToil();
                        return;
                    }

                    // Bug8修复：跟随逻辑——目标变了 或 (停下了且够不到) → 重新寻路
                    if (thing != pawn.pather.Destination.Thing
                        || (!pawn.pather.Moving && !pawn.CanReachImmediate(thing, PathEndMode.Touch)))
                    {
                        pawn.pather.StartPath(thing, PathEndMode.Touch);
                    }
                    else if (pawn.CanReachImmediate(thing, PathEndMode.Touch))
                    {
                        // 到达近战范围后攻击
                        if (targetPawn != null && targetPawn.Downed && !job.killIncappedTarget)
                        {
                            ReadyForNextToil();
                            return;
                        }

                        // 使用job.verbToUse进行近战攻击
                        if (pawn.meleeVerbs.TryMeleeAttack(thing, job.verbToUse))
                        {
                            numMeleeAttacksMade++;
                            if (numMeleeAttacksMade >= job.maxNumMeleeAttacks)
                                EndJobWith(JobCondition.Succeeded);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[BDP Melee] tickIntervalAction异常: {ex}");
                    // 安全中止burst并结束Job，防止异常循环
                    (job.verbToUse as Verb_BDPMelee)?.SafeAbortBurst();
                    EndJobWith(JobCondition.Errored);
                }
            };

            followAndAttack.activeSkill = () => SkillDefOf.Melee;
            followAndAttack.defaultCompleteMode = ToilCompleteMode.Never;
            followAndAttack.FailOnDespawnedOrNull(TargetIndex.A);
            yield return followAndAttack;
        }

        public override bool IsContinuation(Job j)
        {
            return job.GetTarget(TargetIndex.A) == j.GetTarget(TargetIndex.A);
        }
    }
}