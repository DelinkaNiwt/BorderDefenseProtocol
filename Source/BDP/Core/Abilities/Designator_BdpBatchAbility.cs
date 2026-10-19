using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>复用原版直线拖拽；按能力配置显示整批严格目标或明确跳过的中间格。</summary>
    public sealed class Designator_BdpBatchAbility : Designator
    {
        /// <summary>本次输入绑定的能力对象。</summary>
        private readonly BdpBatchAbility ability;
        /// <summary>输入开始时的地图；切图即失效。</summary>
        private readonly Map boundMap;

        /// <summary>只开放原版直线样式，不自行实现鼠标拖拽几何。</summary>
        public override DrawStyleCategoryDef DrawStyleCategory =>
            DefDatabase<DrawStyleCategoryDef>.GetNamed("BDP_BatchAbility_Line");
        /// <summary>显示原版长度标注。</summary>
        public override bool DragDrawMeasurements => true;

        /// <summary>复制能力按钮的视觉和说明，保持输入语义一致。</summary>
        public Designator_BdpBatchAbility(BdpBatchAbility ability)
        {
            this.ability = ability;
            boundMap = ability.pawn.Map;
            defaultLabel = ability.def.LabelCap;
            defaultDesc = ability.Tooltip;
            icon = ability.def.uiIcon;
            soundDragSustain = SoundDefOf.Designate_DragBuilding;
            soundSucceeded = SoundDefOf.Designate_PlaceBuilding;
        }

        /// <summary>角色、来源或地图变化后不允许继续保留旧输入工具。</summary>
        public override bool CanRemainSelected() => ability.pawn.Spawned
            && ability.pawn.Map == boundMap && Find.CurrentMap == boundMap
            && ability.CanQueueCast && !ability.GizmoDisabled(out string _);

        /// <summary>原版拖拽器逐格询问时只做位置校验，整批预算在预览和确认统一处理。</summary>
        public override AcceptanceReport CanDesignateCell(IntVec3 cell)
        {
            return CanRemainSelected() && ability.IsBatchCellValid(cell, !ability.SkipsInvalidInteriorTargets);
        }

        /// <summary>原版单格入口也必须进入同一批量确认。</summary>
        public override void DesignateSingleCell(IntVec3 cell) => Submit(new[] { cell });

        /// <summary>读取未过滤候选，保留两端和中间障碍，不让原版逐格过滤改变请求真值。</summary>
        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            Submit(ReadCandidateCells());
        }

        /// <summary>预览按完整请求显示；红色表示本批不能确认。</summary>
        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            List<IntVec3> raw = ReadCandidateCells();
            List<IntVec3> targets;
            string reason;
            bool accepted = ability.TryValidateBatch(raw, out targets, out reason);
            GenDraw.DrawFieldEdges(targets.Where(cell => cell.InBounds(boundMap)).ToList(), accepted ? Color.white : Color.red);
            if (ability.SkipsInvalidInteriorTargets)
                GenDraw.DrawFieldEdges(raw.Except(targets).Where(cell => cell.InBounds(boundMap)).ToList(), Color.gray);
        }

        /// <summary>鼠标显示有效目标数与预算错误；文案来自语言包。</summary>
        public override void DrawMouseAttachments()
        {
            List<IntVec3> raw = ReadCandidateCells();
            List<IntVec3> targets;
            string reason;
            bool accepted = ability.TryValidateBatch(raw, out targets, out reason);
            float total;
            string budgetReason;
            string price = BdpAbilityTrionCostCommitter.TryGetTotalCost(ability, targets.Count, out total, out budgetReason)
                ? "\n" + "BDP_BatchAbility_TotalCost".Translate(total.ToString("0.##")).ToString() : "";
            string skipped = ability.SkipsInvalidInteriorTargets
                ? "\n" + "BDP_BatchAbility_SkippedCount".Translate(raw.Distinct().Count() - targets.Count).ToString() : "";
            GenUI.DrawMouseAttachment(icon, "BDP_BatchAbility_TargetCount".Translate(targets.Count)
                + skipped + price + (accepted ? "" : "\n" + reason));
        }

        /// <summary>未拖拽时只预览鼠标格；原版结束拖拽不会清空缓存，不能沿用上次候选。</summary>
        private List<IntVec3> ReadCandidateCells()
        {
            DesignationDragger dragger = Find.DesignatorManager.Dragger;
            if (!dragger.Dragging) return new List<IntVec3> { Verse.UI.MouseCell() };
            dragger.UpdateCellBuffer();
            return dragger.CellBuffer.ToList();
        }

        /// <summary>确认后仅安排作业；成功时退出工具，取消和拒绝均不扣资源。</summary>
        private void Submit(IEnumerable<IntVec3> cells)
        {
            List<IntVec3> targets;
            string reason;
            List<IntVec3> candidates = cells.ToList();
            if (!CanRemainSelected() || !ability.TryValidateBatch(candidates, out targets, out reason))
            {
                if (ability.pawn != null)
                    Messages.Message("BDP_BatchAbility_RequestRejected".Translate(), ability.pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            Window confirmation = ability.ConfirmationDialog(new LocalTargetInfo(targets[0]), () =>
            {
                // 对话确认可能晚于当前输入帧；仍需保持同一角色、地图与来源。
                if (CanRemainSelected()) ability.QueueBatch(candidates);
            });
            if (confirmation != null)
            {
                Find.WindowStack.Add(confirmation);
                Find.DesignatorManager.Deselect();
            }
            else if (ability.QueueBatch(candidates)) Find.DesignatorManager.Deselect();
        }
    }
}
