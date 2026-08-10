using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达视觉预设定义。
    /// 作者把贴图、姿态、发光层和枪口锚点写在这里，表达条目只引用 DefName。
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
        /// 读取安全绘制缩放。
        /// </summary>
        public float ResolveDrawScale()
        {
            return DrawScale > 0f ? DrawScale : 1f;
        }
    }
}
