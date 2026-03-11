using Verse;

namespace BDP.Trigger.ShotPipeline.Modules
{
    /// <summary>
    /// LOS（视线）检查模块
    /// 验证射击者到目标的视线，支持引导模式（检查到首个锚点的 LOS）
    /// </summary>
    public class LosCheckModule : IShotAimModule, IShotAimValidator
    {
        public int Priority { get; }

        /// <summary>
        /// 默认构造函数（优先级 10）
        /// </summary>
        public LosCheckModule() : this(10) { }

        /// <summary>
        /// 带优先级的构造函数
        /// </summary>
        public LosCheckModule(int priority) { Priority = priority; }

        /// <summary>
        /// 验证目标是否可见（用于目标选择阶段）
        /// </summary>
        public AimValidation ValidateTarget(ShotSession session, LocalTargetInfo target)
        {
            var ctx = session.Context;
            // 引导模式时跳过直接LOS检查（会检查到首锚点的LOS）
            if (ctx.GuidedConfig != null)
                return AimValidation.Valid;

            bool hasLos = GenSight.LineOfSight(
                ctx.CasterPosition, target.Cell, ctx.Caster.Map);
            return hasLos
                ? AimValidation.Valid
                : AimValidation.Invalid("BDP_NoLineOfSight");
        }

        /// <summary>
        /// 解析瞄准意图（用于射击执行阶段）
        /// </summary>
        public AimIntent ResolveAim(ShotSession session)
        {
            var ctx = session.Context;
            var intent = AimIntent.Default;

            // 确定LOS检查目标
            var losTarget = ctx.Target;
            if (session.AimResult != null && session.AimResult.HasGuidedPath)
            {
                // 引导模式：检查到首个锚点的LOS
                losTarget = new LocalTargetInfo(session.AimResult.AnchorPath[0]);
            }

            if (!GenSight.LineOfSight(ctx.CasterPosition, losTarget.Cell, ctx.Caster.Map))
            {
                // 自动绕行模块可能在后续覆盖此结果
                // 此处只做基础LOS检查
                intent.AbortShot = true;
                intent.AbortReason = "LOS_Failed";
            }

            return intent;
        }
    }

    // AnchorAimModule 已迁移至独立文件：
    // Trigger/ShotPipeline/Modules/AnchorAimModule.cs

    // AutoRouteAimModule 已迁移至独立文件：
    // Trigger/ShotPipeline/Modules/AutoRouteAimModule.cs
}
