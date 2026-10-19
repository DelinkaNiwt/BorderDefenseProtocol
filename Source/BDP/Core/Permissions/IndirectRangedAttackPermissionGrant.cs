using BDP.Core.Projectiles.Interaction;

namespace BDP.Core.Permissions
{
    /// <summary>
    /// 一条间接攻击许可来源返回的结构化授权结果。
    /// </summary>
    public sealed class IndirectRangedAttackPermissionGrant
    {
        /// <summary>
        /// 是否允许当前许可穿透目标屋顶后置条件。
        /// 默认 false，避免新许可来源意外把屋顶下目标变成非直视目标。
        /// </summary>
        public bool AllowRoofedTargets { get; set; }

        /// <summary>
        /// 当前许可允许附加到本次投射物计划的交互策略。
        /// </summary>
        public ProjectileInteractionPolicy InteractionPolicy { get; set; }
    }
}
