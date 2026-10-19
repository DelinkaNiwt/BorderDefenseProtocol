using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.OperatorConsole
{
    /// <summary>
    /// 玩家指派 Pawn 在操作员控制台前持续值守。
    /// </summary>
    public sealed class JobDriver_OperateOperatorConsole : JobDriver
    {
        /// <summary>控制台目标索引。</summary>
        private const TargetIndex ConsoleIndex = TargetIndex.A;

        /// <summary>当前工作目标控制台。</summary>
        private Building_OperatorConsole Console
        {
            get { return job.GetTarget(ConsoleIndex).Thing as Building_OperatorConsole; }
        }

        /// <summary>预留控制台和交互格，避免多个 Pawn 同时值守。</summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Building_OperatorConsole console = Console;
            if (console == null || !pawn.Reserve(console, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return !console.def.hasInteractionCell
                || pawn.ReserveSittableOrSpot(console.InteractionCell, job, errorOnFailed);
        }

        /// <summary>走到交互格后循环刷新操作员心跳。</summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(ConsoleIndex);
            yield return Toils_Goto.GotoThing(ConsoleIndex, PathEndMode.InteractionCell);

            Toil operate = ToilMaker.MakeToil("OperateOperatorConsole");
            operate.tickIntervalAction = delegate(int delta)
            {
                Building_OperatorConsole console = Console;
                if (console == null || !console.CanBeOperatedBy(operate.actor))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                console.NotifyOperatorHeartbeat(operate.actor);
                // 先写入本次心跳，再读取统一状态，避免首次进入值守时被“尚无心跳”条件误判为失效。
                if (!console.IsOperational && !console.IsForcedOperational)
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            operate.FailOnCannotTouch(ConsoleIndex, PathEndMode.InteractionCell);
            operate.defaultCompleteMode = ToilCompleteMode.Never;
            operate.AddFinishAction(delegate
            {
                Console?.ClearOperator(pawn);
            });
            yield return operate;
        }
    }
}
