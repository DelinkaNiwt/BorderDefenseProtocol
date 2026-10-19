using System.Text;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体大方向生命周期诊断。
    /// 这里只读取当前真值并输出开发日志，不改变任何运行状态。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 输出一条带有当前触发体身份的生命周期诊断。
        /// </summary>
        internal void TraceLifecycle(string stage, string details)
        {
            BdpDiagnostics.Trace(
                "触发体生命周期"
                + " | stage=" + (stage ?? "null")
                + " | trigger=" + SafeThingId(parent)
                + " | triggerDef=" + (parent != null && parent.def != null ? parent.def.defName : "null")
                + " | pawn=" + SafeThingId(OwnerPawn ?? lastKnownOwnerPawn)
                + " | " + (details ?? "none"));
        }

        /// <summary>
        /// 输出一条带有当前槽位真值的诊断。
        /// </summary>
        internal void TraceSlotBoundary(string stage, string details)
        {
            BdpDiagnostics.Trace(
                "触发体槽位边界"
                + " | stage=" + (stage ?? "null")
                + " | trigger=" + SafeThingId(parent)
                + " | pawn=" + SafeThingId(OwnerPawn ?? lastKnownOwnerPawn)
                + " | slots=" + DescribeAllSlotsForDiagnostics()
                + " | " + (details ?? "none"));
        }

        /// <summary>
        /// 输出所有侧别的原始槽位摘要。
        /// </summary>
        internal string DescribeAllSlotsForDiagnostics()
        {
            return "Main[" + DescribeSlotsForDiagnostics(TriggerSide.Main)
                + "]|Sub[" + DescribeSlotsForDiagnostics(TriggerSide.Sub)
                + "]|Special[" + DescribeSlotsForDiagnostics(TriggerSide.Special) + "]";
        }

        /// <summary>
        /// 输出指定侧别的槽位摘要。
        /// </summary>
        internal string DescribeSlotsForDiagnostics(TriggerSide side)
        {
            StringBuilder builder = new StringBuilder();
            bool hasSlot = false;
            foreach (TriggerSlotState slot in GetRawSlots(side))
            {
                if (hasSlot)
                {
                    builder.Append(';');
                }

                hasSlot = true;
                builder.Append(slot != null ? slot.Index.ToString() : "null");
                builder.Append('{');
                if (slot == null)
                {
                    builder.Append("null");
                }
                else
                {
                    Thing chip = slot.LoadedChip;
                    builder.Append("chip=");
                    builder.Append(chip != null && chip.def != null ? chip.def.defName : "empty");
                    builder.Append('#');
                    builder.Append(SafeThingId(chip));
                    builder.Append(",active=");
                    builder.Append(slot.IsActive);
                    builder.Append(",disabled=");
                    builder.Append(slot.IsDisabled);
                    builder.Append(",disableReason=");
                    builder.Append(slot.DisabledReason);
                    builder.Append(",pending=");
                    builder.Append(DescribeSwitchContext(slot.SlotSwitchContext));
                }

                builder.Append('}');
            }

            return hasSlot ? builder.ToString() : "none";
        }

        /// <summary>
        /// 输出当前侧级切换上下文摘要。
        /// </summary>
        internal string DescribeSideSwitchContextForDiagnostics(TriggerSide side)
        {
            return DescribeSwitchContext(GetSwitchContext(side));
        }

        /// <summary>
        /// 输出指定槽位的独立切换上下文摘要。
        /// </summary>
        internal string DescribeSwitchContextForDiagnostics(SwitchContext context)
        {
            return DescribeSwitchContext(context);
        }

        /// <summary>
        /// 把切换上下文压缩成可搜索的单行文本。
        /// </summary>
        private static string DescribeSwitchContext(SwitchContext context)
        {
            if (context == null)
            {
                return "none";
            }

            return context.phase
                + ":targetSlot=" + context.targetSlotIndex
                + ":targetChip=" + (context.targetChipThingId ?? "null")
                + ":endTick=" + context.phaseEndTick
                + ":deactivatingSlot=" + context.deactivatingSlotIndex;
        }
    }
}
