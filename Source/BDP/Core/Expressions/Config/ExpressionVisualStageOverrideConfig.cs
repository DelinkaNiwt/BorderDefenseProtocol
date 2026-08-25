using System.Collections.Generic;
using BDP.Core.Trigger.Visual;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达视觉预设针对一个动作阶段的可选覆盖。
    /// 留空整个条目时，既有贴图选择和可见性完全不变。
    /// </summary>
    public sealed class ExpressionVisualStageOverrideConfig
    {
        /// <summary>
        /// 当前覆盖适用的动作阶段。
        /// </summary>
        public WeaponVisualActionStage Stage;

        /// <summary>
        /// 当前阶段是否绘制该视觉预设。
        /// </summary>
        public bool Visible = true;

        /// <summary>
        /// 当前阶段的可选主贴图覆盖；留空时继续使用既有默认态或激活态贴图。
        /// </summary>
        public GraphicData GraphicData;

        /// <summary>
        /// 当前阶段的可选附加绘制层集合；留空时沿用视觉预设默认叠层。
        /// </summary>
        public List<ExpressionVisualOverlayLayerConfig> OverlayLayers;

        /// <summary>
        /// 当前阶段主贴图缓存。
        /// </summary>
        private Graphic cachedGraphic;

        /// <summary>
        /// 解析当前阶段覆盖贴图；未填写贴图时返回空值。
        /// </summary>
        internal Graphic ResolveGraphic(Thing sourceThing)
        {
            if (GraphicData == null)
            {
                return null;
            }

            if (cachedGraphic == null)
            {
                cachedGraphic = sourceThing != null
                    ? GraphicData.GraphicColoredFor(sourceThing)
                    : GraphicData.Graphic;
            }

            return cachedGraphic;
        }
    }
}
