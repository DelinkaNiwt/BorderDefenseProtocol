using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Debug;
using BDP.Content.Assembly.ChipManufacturing.UI;
using BDP.Content.Trion.Facility;
using BDP.Core.Permissions;
using RimWorld;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 触发器制造台沿用原版工作台，只追加本地图的核心运行许可。
    /// 账单、材料预留、工作、经验、半成品和产物落地全部交给原版流程。
    /// </summary>
    public sealed class Building_ChipFabricator : Building_WorkTable, IBillGiver
    {
        /// <summary>按建筑地图与派系读取核心当前运行许可，不缓存旧状态。</summary>
        public bool HasTrionRelayCorePermission
        {
            get { return MapPermissionAccess.HasPermission(Map, Faction, TrionRelayCorePermissionProvider.ActivePermissionKey); }
        }

        /// <summary>原版任务每刻检查可用性；许可失效后沿用原版中断与半成品保留流程。</summary>
        bool IBillGiver.CurrentlyUsableForBills()
        {
            // 原版方法非虚，必须重新实现账单接口，才能覆盖原版任务实际调用的入口。
            return base.CurrentlyUsableForBills() && HasTrionRelayCorePermission;
        }

        /// <summary>分配制造任务前追加许可条件，同时保留原版电力、燃料与故障检查。</summary>
        bool IBillGiver.UsableForBillsAfterFueling()
        {
            return base.UsableForBillsAfterFueling() && HasTrionRelayCorePermission;
        }

        /// <summary>提供打开触发体与芯片共用制造窗口的底部命令。</summary>
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
