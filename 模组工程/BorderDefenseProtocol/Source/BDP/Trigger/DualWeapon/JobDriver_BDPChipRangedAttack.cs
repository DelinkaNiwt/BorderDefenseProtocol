using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片远程攻击JobDriver——复制AttackStatic的持续攻击循环，
    /// 但使用job.verbToUse直接施放，而非pawn.TryStartAttack()重新查找verb。
    ///
    /// 解决两个问题：
    ///   1. AttackStatic的tickIntervalAction调用pawn.TryStartAttack()，
    ///      内部通过TryGetAttackVerb()重新查找verb，返回触发体"柄"近战verb，
    ///      忽略job.verbToUse中的芯片远程verb。
    ///   2. 芯片verb不在VerbTracker.AllVerbs中（v5.1设计），VerbTick/BurstingTick
    ///      永远不被调用，导致burstShotCount>1的连射verb在第1发后state卡在Bursting。
    ///      本Driver在tickAction中手动调用verb.VerbTick()推进burst计时。
    /// </summary>
    public class JobDriver_BDPChipRangedAttack : JobDriver
    {
        private bool startedIncapacitated;
        private int numAttacksMade;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startedIncapacitated, "startedIncapacitated");
            Scribe_Values.Look(ref numAttacksMade, "numAttacksMade");
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            Toil attackToil = ToilMaker.MakeToil("BDPChipRangedAttack");
            attackToil.initAction = delegate
            {
                if (TargetThingA is Pawn p)
                    startedIncapacitated = p.Downed;
                pawn.pather.StopDead();
            };
            // ── 每tick调用：手动推进芯片verb的burst计时 ──
            // 原因：芯片verb不在VerbTracker.AllVerbs中（v5.1设计），
            //   引擎的VerbTracker.VerbsTick()不会调用它们的VerbTick()，
            //   导致BurstingTick永远不被调用，burstShotCount>1的连射
            //   在第1发后ticksToNextBurstShot永远不递减，state卡在Bursting。
            attackToil.tickAction = delegate
            {
                job.verbToUse?.VerbTick();
            };
            attackToil.tickIntervalAction = delegate
            {
                // 目标无效或已销毁 → 结束
                if (!TargetA.IsValid)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                if (TargetA.HasThing)
                {
                    var targetPawn = TargetA.Thing as Pawn;
                    if (TargetA.Thing.Destroyed
                        || (targetPawn != null && !startedIncapacitated && targetPawn.Downed)
                        || (targetPawn != null && targetPawn.IsPsychologicallyInvisible()))
                    {
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }
                }

                // 达到最大攻击次数 → 结束
                if (numAttacksMade >= job.maxNumStaticAttacks && !pawn.stances.FullBodyBusy)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }

                // ── 核心区别：直接使用job.verbToUse而非pawn.TryStartAttack ──
                Verb verb = job.verbToUse;
                if (verb == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // 等待stance结束（冷却/瞄准中不重复施放）
                if (pawn.stances.FullBodyBusy) return;

                // 最大射程检查：目标超出射程时结束job。
                // 引导弹的TryStartCastOn会把castTarg替换为第一个锚点，
                // 导致base.TryStartCastOn只检查锚点射程而非最终目标射程，
                // 从而绕过射程限制持续发射。此处在发射前直接检查最终目标距离。
                float maxRange = verb.verbProps.range;
                if ((float)pawn.Position.DistanceToSquared(TargetA.Cell) > maxRange * maxRange)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                LocalTargetInfo castTarget = verb.verbProps.ai_RangedAlawaysShootGroundBelowTarget
                    ? (LocalTargetInfo)TargetA.Cell : TargetA;

                if (verb.TryStartCastOn(castTarget))
                {
                    numAttacksMade++;
                }
                else
                {
                    // 无法命中 → 检查是否应结束job
                    if (job.endIfCantShootTargetFromCurPos
                        && !verb.CanHitTargetFrom(pawn.Position, TargetA))
                    {
                        EndJobWith(JobCondition.Incompletable);
                    }
                    // 修复：对齐原版AttackStatic逻辑——仅在pawn处于最小射程内（太近）时结束job。
                    // 旧代码 !AdjacentTo8WayOrInside 在远程战斗中永远为true，导致任何TryStartCastOn
                    // 失败都会立即结束job（如读档后LOS暂时失败）。
                    else if (job.endIfCantShootInMelee)
                    {
                        float minRange = verb.verbProps.EffectiveMinRange(TargetA, pawn);
                        if ((float)pawn.Position.DistanceToSquared(TargetA.Cell) < minRange * minRange
                            && pawn.Position.AdjacentTo8WayOrInside(TargetA.Cell))
                        {
                            EndJobWith(JobCondition.Incompletable);
                        }
                    }
                }
            };
            attackToil.defaultCompleteMode = ToilCompleteMode.Never;
            yield return attackToil;
        }

        public override bool IsContinuation(Job j)
        {
            return job.GetTarget(TargetIndex.A) == j.GetTarget(TargetIndex.A);
        }
    }
}