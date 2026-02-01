using Verse;

namespace GD3
{
    public class ModExt_BezierProjectiles : DefModExtension
    {
        /// <summary>
        /// 控制二阶/三阶贝塞尔曲线中控制点扰动的随机范围，X/Z轴偏移用。例如 -1~1 表示最大偏移量为 ±1。
        /// </summary>
        public FloatRange randOffsetRange = new FloatRange(-1f, 1f);

        /// <summary>
        /// 控制点在起点与终点之间的距离因子（0~1），用于计算二阶贝塞尔的中间控制点。值越大，曲线越弯。
        /// </summary>
        public float controlPointFactor = 0.5f;

        /// <summary>
        /// 子弹发射后延迟多少 Tick 后开始绘制尾迹（帧数延迟）。用于避免贴图重叠。
        /// </summary>
        public int drawStartDelay = 2;

        /// <summary>
        /// 使用的尾迹粒子 FleckDef 的 defName，必须提前定义好。
        /// </summary>
        public string trailFleckDef = "Mst_Bezier_trail";

        /// <summary>
        /// 尾迹线条的粗细（单位为 world 单位，推荐 0.01~0.05 之间）。
        /// </summary>
        public float trailThickness = 0.025f;

        /// <summary>
        /// 子弹每一 Tick 前进中，用于碰撞检测的步长，值越小精度越高，但性能开销越大（推荐 0.2 左右）。
        /// </summary>
        public float probeStep = 0.2f;

        /// <summary>
        /// 是否使用三阶贝塞尔曲线弹道。如果为 false，则使用默认的二阶弹道。
        /// </summary>
        public bool useCubieCurve = false;

        // 三阶贝塞尔用的控制点字段
        public float? P1Offset; // 三阶贝塞尔曲线：第一个控制点 P1 离起点的距离（0~1，0.3 表示总距离的30%处）。
        public float? P1Side; // 三阶贝塞尔曲线：第一个控制点 P1 向左右的偏移距离，正数为左，负数为右。
        public float? P2Offset; // 三阶贝塞尔曲线：第二个控制点 P2 离起点的距离（通常靠近终点，推荐 0.5~0.8）。
        public float? P2Side; // 三阶贝塞尔曲线：第二个控制点 P2 向左右的偏移距离，正数为左，负数为右。

        /// <summary>
        /// 是否启用三阶曲线左右翻转效果（即随机左右反向），让轨迹像 DNA 螺旋一样。
        /// </summary>
        public bool enableRandomSide = false;

        /// <summary>
        /// 触发随机左右翻转的概率，0.0~1.0（例如 0.5 表示 50% 几率左右翻转一次）。
        /// </summary>
        public float randomSideChance = 0.5f;
    }
}