using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Debug;
using BDP.Content.Assembly.ChipManufacturing.UI;
using RimWorld;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 芯片制造台只提供原版 Building_WorkTable 身份。
    /// 账单、材料预留、工作、经验、半成品和产物落地全部交给原版流程。
    /// </summary>
    public sealed class Building_ChipFabricator : Building_WorkTable
    {
        /// <summary>提供打开专用芯片制造窗口的底部命令。</summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "BDP_ChipManufacturing_TabLabel".Translate(),
                defaultDesc = def.description,
                icon = def.uiIcon,
                action = () => Find.WindowStack.Add(new Window_ChipManufacturing(this))
            };

            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "BDP_ChipManufacturing_DebugCompleteAll".Translate(),
                    defaultDesc = "BDP_ChipManufacturing_DebugCompleteAllDesc".Translate(),
                    action = CompleteAllBillsDebug
                };
            }
        }

        /// <summary>执行上帝模式批量完成，并向玩家显示一次汇总反馈。</summary>
        private void CompleteAllBillsDebug()
        {
            ChipFabricatorDebugCompletionReport report =
                ChipFabricatorDebugCompletionService.CompleteAll(this);
            string message = report.EncounteredBillCount == 0
                ? "BDP_ChipManufacturing_DebugCompleteNone".Translate()
                : "BDP_ChipManufacturing_DebugCompleteResult".Translate(
                    report.ProducedChipCount,
                    report.CompletedBillCount,
                    report.SkippedBillCount);
            Messages.Message(message, this, MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
