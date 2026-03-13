using BDP.Projectiles.Config;
using Verse;

namespace BDP.Trigger.ShotPipeline.Modules
{
    /// <summary>
    /// 区域指示器模块：在瞄准阶段渲染武器/能力的影响范围。
    /// 委托给现有的 IAreaIndicator 系统（当前仅支持圆形）。
    /// </summary>
    public class AreaIndicatorModule : IShotAimRenderer
    {
        // 缓存指示器实例（避免每帧创建）
        private readonly CircleAreaIndicator _circleIndicator = new CircleAreaIndicator();

        /// <summary>
        /// 渲染目标区域指示器
        /// </summary>
        public void RenderTargeting(ShotSession session, LocalTargetInfo mouseTarget)
        {
            // 获取投射物定义（从上下文直接读取）
            var projectileDef = session.Context.ProjectileDef;
            if (projectileDef == null)
                return;

            // 获取指示器配置（三级优先级：投射物 > 芯片 > Verb）
            var indicatorConfig = GetAreaIndicatorConfig(session, projectileDef);
            if (indicatorConfig == null)
                return;

            // 目标不可视时的特殊处理：
            // 检查投射物本身是否有引导能力（通过defName判断）
            // 如果投射物没有引导能力，目标不可视时不显示范围指示器
            var caster = session.Context.Caster;
            bool hasDirectLOS = GenSight.LineOfSight(caster.Position, mouseTarget.Cell, caster.Map);
            if (!hasDirectLOS)
            {
                // 检查投射物是否有引导能力（通过defName包含"Guided"判断）
                bool projectileHasGuided = projectileDef.defName.Contains("Guided");

                // 如果投射物没有引导能力，目标不可视时不显示范围指示器
                if (!projectileHasGuided)
                    return;
            }

            // 计算实际半径
            float radius = GetIndicatorRadius(projectileDef, indicatorConfig);
            if (radius <= 0f)
                return;

            // 创建临时配置（使用计算后的半径）
            var tempConfig = new AreaIndicatorConfig
            {
                indicatorType = indicatorConfig.indicatorType,
                radiusSource = RadiusSource.Custom,
                customRadius = radius,
                color = indicatorConfig.color,
                fillStyle = indicatorConfig.fillStyle
            };

            // 委托给指示器系统绘制
            _circleIndicator.Draw(mouseTarget.Cell, caster.Map, tempConfig);
        }

        /// <summary>
        /// 获取范围指示器配置（三级优先级：投射物 > 芯片 > Verb）
        /// </summary>
        private AreaIndicatorConfig GetAreaIndicatorConfig(ShotSession session, ThingDef projectileDef)
        {
            // 1. 优先从投射物读取
            if (projectileDef != null)
            {
                var config = projectileDef.GetModExtension<AreaIndicatorConfig>();
                if (config != null) return config;
            }

            // 2. 其次从芯片读取
            var chipConfig = session.Context.ChipConfig;
            if (chipConfig?.areaIndicator != null)
                return chipConfig.areaIndicator;

            // 3. 最后从Verb读取（当前版本未实现）
            return null;
        }

        /// <summary>
        /// 获取指示器半径（根据配置的半径来源）
        /// </summary>
        private float GetIndicatorRadius(ThingDef projectileDef, AreaIndicatorConfig config)
        {
            if (config == null) return 0f;

            // 从爆炸配置读取半径
            if (config.radiusSource == RadiusSource.Explosion && projectileDef != null)
            {
                var explosionConfig = projectileDef.GetModExtension<BDPExplosionConfig>();
                if (explosionConfig != null)
                    return explosionConfig.explosionRadius;
            }

            // 使用自定义半径
            return config.customRadius;
        }
    }
}
