using BDP.Core.Verbs;
using Verse;

namespace BDP.Core.VerbHosting
{
    /// <summary>
    /// 一条正式宿主槽位的完整绑定容器。
    /// 它持有稳定槽位身份、当前绑定状态以及原版 VerbTracker 创建的正式壳 verb。
    /// </summary>
    internal sealed class BdpFormalVerbBinding
    {
        /// <summary>
        /// 当前绑定所属的固定宿主槽位。
        /// </summary>
        public BdpFormalVerbHostSlot Slot { get; set; }

        /// <summary>
        /// 当前槽位最近一次刷新得到的绑定状态。
        /// </summary>
        public BdpFormalVerbBindingState State { get; set; }

        /// <summary>
        /// 当前槽位的正式远程壳 verb。
        /// 它由原版 VerbTracker 持有和重建。
        /// </summary>
        public BdpVerb_FormalHostShoot RangedVerb { get; set; }

        /// <summary>
        /// 当前槽位的正式近战壳 verb。
        /// 它由原版 VerbTracker 持有和重建。
        /// </summary>
        public BdpVerb_FormalHostMelee MeleeVerb { get; set; }

        /// <summary>
        /// 读取当前槽位绑定的正式结果标识。
        /// </summary>
        public string ResultId
        {
            get { return State != null ? State.ResultId : null; }
        }

        /// <summary>
        /// 读取当前槽位是否可用。
        /// </summary>
        public bool IsAvailable
        {
            get { return State != null && State.IsAvailable; }
        }

        /// <summary>
        /// 按当前绑定状态解析要交给外部系统使用的正式壳 verb。
        /// </summary>
        public Verb ResolveActiveVerb()
        {
            if (State == null || !State.IsAvailable)
            {
                return null;
            }

            return State.WeaponMode == BDP.Core.Expressions.WeaponExpressionMode.Melee
                ? (Verb)MeleeVerb
                : RangedVerb;
        }

        /// <summary>
        /// 判断当前固定槽位是否仍持有需要持续 VerbTick 的活跃 formal host 会话。
        /// 这里只做最小汇总，不复制具体攻击会话状态。
        /// </summary>
        public bool ShouldTickAsFormalHost()
        {
            return (RangedVerb != null && RangedVerb.ShouldTickAsFormalHost())
                || (MeleeVerb != null && MeleeVerb.ShouldTickAsFormalHost());
        }
    }
}
