using System.Collections.Generic;
using BDP.Core.Trigger.Visual;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达视觉预设定义。
    /// 作者把贴图、姿态、发光层、握持锚点和枪口锚点写在这里，表达条目只引用 DefName。
    /// </summary>
    public sealed class ExpressionVisualPresetDef : Def
    {
        /// <summary>
        /// 当前预设的默认主贴图配置。
        /// </summary>
        public GraphicData GraphicData;

        /// <summary>
        /// 当前预设在执行焦点命中时使用的主贴图配置。
        /// 留空时激活态继续使用 GraphicData。
        /// </summary>
        public GraphicData ActiveGraphicData;

        /// <summary>
        /// 当前预设按武器动作阶段声明的可选视觉覆盖。
        /// 整项留空时，绘制行为与加入阶段设施前完全一致。
        /// </summary>
        public List<ExpressionVisualStageOverrideConfig> StageVisuals;

        /// <summary>
        /// 当前预设的 South/North 姿态配置。
        /// 留空时使用默认零偏移配置。
        /// </summary>
        public ExpressionVisualSouthNorthPoseConfig SouthNorthPose;

        /// <summary>
        /// 当前预设的 East/West 姿态配置。
        /// 留空时使用默认零偏移配置。
        /// </summary>
        public ExpressionVisualEastWestPoseConfig EastWestPose;

        /// <summary>
        /// 当前预设是否由作者显式声明自定义手持姿态。
        /// </summary>
        public bool HasExplicitPose => SouthNorthPose != null || EastWestPose != null;

        /// <summary>
        /// 当前预设的握持锚点配置。
        /// 留空时本预设不提供握持位置。
        /// </summary>
        public ExpressionVisualGripConfig Grip;

        /// <summary>
        /// 当前预设的枪口锚点配置。
        /// 留空时本预设不提供远程发射原点。
        /// </summary>
        public ExpressionVisualMuzzleConfig Muzzle;

        /// <summary>
        /// 当前预设的附加绘制层集合。
        /// </summary>
        public List<ExpressionVisualOverlayLayerConfig> OverlayLayers;

        /// <summary>
        /// 当前预设主贴图绘制缩放。
        /// 0 或负数按 1 处理。
        /// </summary>
        public float DrawScale = 1f;

        /// <summary>
        /// 当前预设的瞄准旋转限幅（度）。
        /// 0 表示完整沿用原版连续瞄准旋转；正值把目标方向限制在当前四向基准附近。
        /// </summary>
        public float AimRotationLimit = 0f;

        /// <summary>
        /// 当前预设默认主贴图缓存。
        /// </summary>
        private Graphic cachedGraphic;

        /// <summary>
        /// 当前预设激活主贴图缓存。
        /// </summary>
        private Graphic cachedActiveGraphic;

        /// <summary>
        /// 按当前执行态解析主贴图。
        /// </summary>
        public Graphic ResolveGraphic(bool active, Thing sourceThing)
        {
            GraphicData data = active && ActiveGraphicData != null ? ActiveGraphicData : GraphicData;
            if (data == null)
            {
                return null;
            }

            if (active && ActiveGraphicData != null)
            {
                if (cachedActiveGraphic == null)
                {
                    cachedActiveGraphic = sourceThing != null ? data.GraphicColoredFor(sourceThing) : data.Graphic;
                }

                return cachedActiveGraphic;
            }

            if (cachedGraphic == null)
            {
                cachedGraphic = sourceThing != null ? data.GraphicColoredFor(sourceThing) : data.Graphic;
            }

            return cachedGraphic;
        }

        /// <summary>
        /// 按当前执行态与动作阶段解析主贴图。
        /// 阶段未配置贴图时，严格回退到既有执行态解析规则。
        /// </summary>
        public Graphic ResolveGraphic(bool active, WeaponVisualActionStage stage, Thing sourceThing)
        {
            ExpressionVisualStageOverrideConfig stageOverride = ResolveStageOverride(stage);
            if (stageOverride != null && stageOverride.GraphicData != null)
            {
                return stageOverride.ResolveGraphic(sourceThing);
            }

            return ResolveGraphic(active, sourceThing);
        }

        /// <summary>
        /// 读取指定动作阶段的首个覆盖条目。
        /// 未配置时返回空值，重复条目另由 ConfigErrors 报告给作者。
        /// </summary>
        public ExpressionVisualStageOverrideConfig ResolveStageOverride(WeaponVisualActionStage stage)
        {
            if (StageVisuals == null)
            {
                return null;
            }

            for (int i = 0; i < StageVisuals.Count; i++)
            {
                ExpressionVisualStageOverrideConfig candidate = StageVisuals[i];
                if (candidate != null && candidate.Stage == stage)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// 读取指定动作阶段是否可见。
        /// 阶段未配置时默认可见，保持旧预设行为。
        /// </summary>
        public bool ResolveStageVisibility(WeaponVisualActionStage stage)
        {
            ExpressionVisualStageOverrideConfig stageOverride = ResolveStageOverride(stage);
            if (stageOverride == null)
            {
                return true;
            }

            return stageOverride.Visible;
        }

        /// <summary>
        /// 按当前动作阶段解析附加绘制层集合。
        /// 阶段显式提供集合时替换默认集合，否则沿用默认叠层。
        /// </summary>
        public List<ExpressionVisualOverlayLayerConfig> ResolveOverlayLayers(
            WeaponVisualActionStage stage)
        {
            ExpressionVisualStageOverrideConfig stageOverride = ResolveStageOverride(stage);
            return stageOverride != null && stageOverride.OverlayLayers != null
                ? stageOverride.OverlayLayers
                : OverlayLayers;
        }

        /// <summary>
        /// 校验每个动作阶段只能声明一次覆盖，避免配置顺序暗中决定结果。
        /// </summary>
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (StageVisuals == null)
            {
                yield break;
            }

            HashSet<WeaponVisualActionStage> declaredStages = new HashSet<WeaponVisualActionStage>();
            for (int i = 0; i < StageVisuals.Count; i++)
            {
                ExpressionVisualStageOverrideConfig stageOverride = StageVisuals[i];
                if (stageOverride == null)
                {
                    continue;
                }

                if (!declaredStages.Add(stageOverride.Stage))
                {
                    yield return "BDP_ConfigError_DuplicateWeaponVisualStage"
                        .Translate(
                            defName.Named("0"),
                            stageOverride.Stage.ToString().Named("1"))
                        .ToString();
                }
            }
        }

        /// <summary>
        /// 读取 South/North 姿态配置。
        /// </summary>
        public ExpressionVisualSouthNorthPoseConfig ResolveSouthNorthPose()
        {
            return SouthNorthPose ?? new ExpressionVisualSouthNorthPoseConfig();
        }

        /// <summary>
        /// 读取 East/West 姿态配置。
        /// </summary>
        public ExpressionVisualEastWestPoseConfig ResolveEastWestPose()
        {
            return EastWestPose ?? new ExpressionVisualEastWestPoseConfig();
        }

        /// <summary>
        /// 读取枪口锚点配置。
        /// </summary>
        public ExpressionVisualMuzzleConfig ResolveMuzzle()
        {
            return Muzzle;
        }

        /// <summary>
        /// 读取握持锚点配置。
        /// </summary>
        public ExpressionVisualGripConfig ResolveGrip()
        {
            return Grip;
        }

        /// <summary>
        /// 读取安全绘制缩放。
        /// </summary>
        public float ResolveDrawScale()
        {
            return DrawScale > 0f ? DrawScale : 1f;
        }
    }
}
