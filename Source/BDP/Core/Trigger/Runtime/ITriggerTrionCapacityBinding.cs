using Verse;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// 触发体 Trion 容量绑定器的中性接口。
    /// Core 只转发生命周期，不认识具体触发器类别。
    /// </summary>
    public interface ITriggerTrionCapacityBinding
    {
        /// <summary>
        /// 判断当前绑定器是否处理指定触发体。
        /// </summary>
        bool Handles(CompTriggerBody trigger);

        /// <summary>
        /// 返回指定 Pawn 当前装备触发体提供的额外容量。
        /// </summary>
        float GetCapacityContribution(Pawn pawn);

        /// <summary>
        /// 通知触发体已经装备。
        /// </summary>
        void NotifyEquipped(Pawn pawn, CompTriggerBody trigger);

        /// <summary>
        /// 通知触发体已经正常卸下。
        /// </summary>
        void NotifyUnequipped(Pawn pawn, CompTriggerBody trigger);

        /// <summary>
        /// 通知触发体因腺体资格失效而准备解除。
        /// </summary>
        void NotifyEligibilityLost(Pawn pawn, CompTriggerBody trigger);

        /// <summary>
        /// 通知触发体已经被摧毁。
        /// </summary>
        void NotifyDestroyed(Pawn pawn, CompTriggerBody trigger);
    }
}
