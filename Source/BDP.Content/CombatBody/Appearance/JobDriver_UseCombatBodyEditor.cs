using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 走到战斗体编辑台交互格并打开编辑窗口的原版工作驱动。
    /// </summary>
    public sealed class JobDriver_UseCombatBodyEditor : JobDriver
    {
        /// <summary>
        /// 编辑台目标索引。
        /// </summary>
        private const TargetIndex EditorIndex = TargetIndex.A;

        /// <summary>
        /// 当前 Job 指向的战斗体编辑台。
        /// </summary>
        private Building_CombatBodyEditor Editor
        {
            get { return job.GetTarget(EditorIndex).Thing as Building_CombatBodyEditor; }
        }

        /// <summary>
        /// 预留编辑台及其交互位置。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Building_CombatBodyEditor editor = Editor;
            if (editor == null || !pawn.Reserve(editor, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return !editor.def.hasInteractionCell
                || pawn.ReserveSittableOrSpot(editor.InteractionCell, job, errorOnFailed);
        }

        /// <summary>
        /// 前往交互格，完成最终检查后打开窗口。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(EditorIndex);
            yield return Toils_Goto.GotoThing(EditorIndex, PathEndMode.InteractionCell)
                .FailOn(() => Editor == null || !Editor.CanPawnOpenAppearanceEditor(pawn));
            yield return Toils_General.Do(OpenAppearanceWindow);
        }

        /// <summary>
        /// 打开编辑窗口；到达后再次验证建筑和 Pawn 状态。
        /// </summary>
        private void OpenAppearanceWindow()
        {
            Building_CombatBodyEditor editor = Editor;
            if (editor == null)
            {
                return;
            }

            string rejectReason = editor.ResolveUseRejection(pawn, false);
            if (!rejectReason.NullOrEmpty())
            {
                Messages.Message(rejectReason, editor, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new Window_CombatBodyAppearanceEditor(pawn, editor));
        }
    }
}
