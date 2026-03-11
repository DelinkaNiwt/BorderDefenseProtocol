using System.Collections.Generic;
using BDP.Projectiles;
using Verse;

namespace BDP.Trigger.ShotPipeline.Modules
{
    /// <summary>
    /// 锚点瞄准模块：处理引导导弹的锚点路径瞄准
    /// - Targeting 子步骤：渲染锚点路径预览线
    /// - Resolve 子步骤：读取累积的锚点路径，产出 AimIntent
    ///
    /// 迁移自：Verb_BDPRangedBase.StartAnchorTargeting() + AnchorTargetingHelper
    /// </summary>
    public class AnchorAimModule : IShotAimModule, IShotAimRenderer
    {
        public int Priority { get; }
        private readonly float anchorSpread;

        /// <summary>
        /// 默认构造函数（优先级 20，散布 0.3）
        /// </summary>
        public AnchorAimModule() : this(20, 0.3f) { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="priority">执行优先级</param>
        /// <param name="anchorSpread">锚点散布半径</param>
        public AnchorAimModule(int priority, float anchorSpread)
        {
            Priority = priority;
            this.anchorSpread = anchorSpread;
        }

        /// <summary>
        /// 渲染瞄准指示（Targeting 子步骤）
        /// 绘制锚点路径预览线
        /// </summary>
        public void RenderTargeting(ShotSession session, LocalTargetInfo mouseTarget)
        {
            var ctx = session.Context;

            // 只在引导模式启用时渲染
            if (ctx.GuidedConfig == null)
                return;

            // 委托给 AnchorTargetingHelper 的绘制方法
            // 该方法会根据 session.AnchorPath 绘制已放置的锚点折线
            // 以及从最后锚点到鼠标位置的预览线（带 LOS 检查）
            AnchorTargetingHelper.DrawGuidedOverlay(
                ctx.Caster,
                session.AnchorPath,
                mouseTarget,
                ctx.Caster.Map
            );
        }

        /// <summary>
        /// 解析瞄准意图（Resolve 子步骤）
        /// 从 session 读取累积的锚点路径，产出 AimIntent
        /// </summary>
        public AimIntent ResolveAim(ShotSession session)
        {
            var intent = AimIntent.Default;
            var ctx = session.Context;

            // 只在引导模式启用时生效
            if (ctx.GuidedConfig == null)
                return intent;

            // 从 session 读取累积的锚点路径
            // 注意：锚点路径由外部交互逻辑（如 AnchorTargetingHelper.BeginAnchorTargeting）写入
            if (session.AnchorPath != null && session.AnchorPath.Count > 0)
            {
                // 将 List<IntVec3> 转换为 IntVec3[] 以匹配 AimIntent 的数据结构
                intent.AnchorPoints = session.AnchorPath.ToArray();
                intent.AnchorSpread = anchorSpread;
            }

            return intent;
        }
    }
}

