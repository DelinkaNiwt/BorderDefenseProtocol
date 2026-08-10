using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 单个槽位的最小状态口。
    /// 它只描述槽位事实，不负责表达或战斗流程。
    /// </summary>
    public interface ITriggerSlotState
    {
        /// <summary>
        /// 当前槽位属于哪一侧。
        /// </summary>
        TriggerSide Side { get; }

        /// <summary>
        /// 当前槽位在该侧中的序号。
        /// </summary>
        int Index { get; }

        /// <summary>
        /// 当前装入的芯片。
        /// 没有装载时为 null。
        /// </summary>
        Thing LoadedChip { get; }

        /// <summary>
        /// 当前是否已正式激活。
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 当前是否被禁用。
        /// 被禁用时通常不能进入激活。
        /// </summary>
        bool IsDisabled { get; }

        /// <summary>
        /// 当前禁用原因码。
        /// 没有禁用时为 None。
        /// </summary>
        TriggerDisableReason DisabledReason { get; }

        /// <summary>
        /// 当前槽位是否参与了一组跨侧绑定。
        /// 普通单槽位芯片为 false。
        /// </summary>
        bool HasBindingPartner { get; }

        /// <summary>
        /// 当前槽位是否只是绑定关系中的镜像副本。
        /// 镜像副本用于占位与同步，不应被当成独立表达来源。
        /// </summary>
        bool IsBindingMirror { get; }

        /// <summary>
        /// 当前绑定关系的主槽位侧别。
        /// 普通单槽位时返回自己。
        /// </summary>
        TriggerSide BindingRootSide { get; }

        /// <summary>
        /// 当前绑定关系的主槽位索引。
        /// 普通单槽位时返回自己。
        /// </summary>
        int BindingRootIndex { get; }

        /// <summary>
        /// 当前绑定关系所指向的对侧槽位。
        /// 没有绑定时该值不应被使用。
        /// </summary>
        TriggerSide BindingPartnerSide { get; }

        /// <summary>
        /// 当前绑定关系所指向的对侧槽位索引。
        /// 没有绑定时为 -1。
        /// </summary>
        int BindingPartnerIndex { get; }
    }
}
