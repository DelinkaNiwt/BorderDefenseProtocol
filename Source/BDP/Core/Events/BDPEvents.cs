using System;

namespace BDP.Core
{
    /// <summary>
    /// BDP全局事件总线。集中管理跨模块静态事件。
    /// 从Gene_TrionGland提取，消除Gene作为事件中转站的职责。
    ///
    /// 设计目的:
    /// - 解耦事件发布者和订阅者
    /// - 消除Gene_TrionGland的事件总线职责
    /// - 提供清晰的事件管理接口
    ///
    /// 事件列表:
    /// - QueryCanActivateCombatBody: 战斗体激活前置条件查询（可否决）
    /// - RequestDeactivateCombatBody: 请求解除战斗体（触发体卸下时）
    /// - OnPartDestroyed: 部位破坏通知（手部缺失联动）
    /// </summary>
    public static class BDPEvents
    {
        // ═══════════════════════════════════════════
        //  事件定义
        // ═══════════════════════════════════════════

        /// <summary>
        /// 战斗体激活前置条件查询事件（静态可否决事件）。
        /// 订阅者可以否决激活并提供原因。
        /// </summary>
        public static event Action<CanActivateCombatBodyEventArgs> QueryCanActivateCombatBody;

        /// <summary>
        /// 请求解除战斗体事件。
        /// 当触发体被卸下/更换时，Trigger层触发此事件请求解除战斗体。
        /// </summary>
        public static event Action<Verse.Pawn> RequestDeactivateCombatBody;

        /// <summary>
        /// 部位破坏事件。
        /// 当影子部位被破坏时触发，供其他模块响应（如手部缺失联动）。
        /// </summary>
        public static event Action<PartDestroyedEventArgs> OnPartDestroyed;

        // ═══════════════════════════════════════════
        //  事件触发方法
        // ═══════════════════════════════════════════

        /// <summary>
        /// 触发激活前置条件查询事件。
        /// </summary>
        public static void TriggerCanActivateQuery(CanActivateCombatBodyEventArgs args)
        {
            QueryCanActivateCombatBody?.Invoke(args);
        }

        /// <summary>
        /// 触发解除战斗体请求。
        /// </summary>
        public static void TriggerDeactivateRequest(Verse.Pawn pawn)
        {
            RequestDeactivateCombatBody?.Invoke(pawn);
        }

        /// <summary>
        /// 触发部位破坏事件。
        /// </summary>
        public static void TriggerPartDestroyedEvent(PartDestroyedEventArgs args)
        {
            OnPartDestroyed?.Invoke(args);
        }
    }
}
