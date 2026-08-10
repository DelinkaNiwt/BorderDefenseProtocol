using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 使用触发器装配台的工作驱动。
    /// 它只负责走到交互格并打开窗口，不在 Job 内直接修改 Trigger 装载真值。
    /// </summary>
    public class JobDriver_UseTriggerAssembler : JobDriver
    {
        /// <summary>
        /// 装配台目标索引。
        /// </summary>
        private const TargetIndex AssemblerIndex = TargetIndex.A;

        /// <summary>
        /// 当前 Job 指向的装配台。
        /// </summary>
        private Building_TriggerAssembler Assembler
        {
            get
            {
                return job.GetTarget(AssemblerIndex).Thing as Building_TriggerAssembler;
            }
        }

        /// <summary>
        /// 预留装配台，避免多个小人同时打开同一个装配入口。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(AssemblerIndex), job, 1, -1, null, errorOnFailed);
        }

        /// <summary>
        /// 走到装配台交互格后打开正式装配窗口。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(AssemblerIndex);

            yield return Toils_Goto.GotoThing(AssemblerIndex, PathEndMode.InteractionCell)
                .FailOn(() => Assembler == null || !Assembler.CanPawnOpenAssemblyWindow(pawn));

            yield return Toils_General.Do(OpenAssemblyWindow);
        }

        /// <summary>
        /// 打开触发器装配窗口；最终合法性在打开前再确认一次。
        /// </summary>
        private void OpenAssemblyWindow()
        {
            Building_TriggerAssembler assembler = Assembler;
            if (assembler == null)
            {
                return;
            }

            string rejectReason = assembler.ResolveUseRejection(pawn, false);
            if (!rejectReason.NullOrEmpty())
            {
                Messages.Message(rejectReason, assembler, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Window_TriggerAssembly(pawn, assembler));
        }
    }
}
