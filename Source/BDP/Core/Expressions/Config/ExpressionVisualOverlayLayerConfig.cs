using UnityEngine;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达视觉预设中的附加绘制层配置。
    /// 它用于发光层、激活态替换层或额外装饰层。
    /// </summary>
    public sealed class ExpressionVisualOverlayLayerConfig
    {
        /// <summary>
        /// 当前附加层的稳定标识。
        /// 仅用于诊断和作者排查，不参与运行时裁决。
        /// </summary>
        public string LayerId;

        /// <summary>
        /// 当前附加层默认贴图配置。
        /// </summary>
        public GraphicData GraphicData;

        /// <summary>
        /// 当前附加层激活态贴图配置。
        /// 留空时激活态继续使用 GraphicData。
        /// </summary>
        public GraphicData ActiveGraphicData;

        /// <summary>
        /// 当前附加层是否只在执行焦点命中时绘制。
        /// </summary>
        public bool OnlyWhenActive = false;

        /// <summary>
        /// 当前附加层是否只在执行焦点未命中时绘制。
        /// </summary>
        public bool OnlyWhenInactive = false;

        /// <summary>
        /// 当前附加层相对主姿态的额外偏移。
        /// 默认按世界空间叠加，避免破坏旧版四朝向基准。
        /// </summary>
        public Vector3 LocalOffset = Vector3.zero;

        /// <summary>
        /// 当前附加层相对主姿态的额外高度偏移。
        /// </summary>
        public float AltitudeOffset = 0.001f;

        /// <summary>
        /// 当前附加层相对主姿态的额外装饰角。
        /// </summary>
        public float AngleOffset = 0f;

        /// <summary>
        /// 当前附加层相对主贴图的绘制缩放。
        /// 0 或负数表示沿用主预设缩放。
        /// </summary>
        public float DrawScale = 1f;

        /// <summary>
        /// 当前附加层默认贴图缓存。
        /// </summary>
        private Graphic cachedGraphic;

        /// <summary>
        /// 当前附加层激活贴图缓存。
        /// </summary>
        private Graphic cachedActiveGraphic;

        /// <summary>
        /// 按当前执行态解析附加层贴图。
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
    }
}
