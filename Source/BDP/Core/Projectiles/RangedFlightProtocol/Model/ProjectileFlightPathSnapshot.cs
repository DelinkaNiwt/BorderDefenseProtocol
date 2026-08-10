using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 投射物当前飞行路径的冻结快照。
    /// 宿主只认这份中立几何数据，不认业务模块的具体语义。
    /// </summary>
    public sealed class ProjectileFlightPathSnapshot : IExposable
    {
        /// <summary>
        /// 当前路径的几何类型。
        /// </summary>
        public ProjectileFlightPathKind Kind { get; set; }

        /// <summary>
        /// 当前路径的起点。
        /// </summary>
        public Vector3 Start { get; set; }

        /// <summary>
        /// 当前路径的第一控制点。
        /// 线性路径下它与起点保持一致。
        /// </summary>
        public Vector3 ControlA { get; set; }

        /// <summary>
        /// 当前路径的第二控制点。
        /// 线性路径下它与终点保持一致。
        /// </summary>
        public Vector3 ControlB { get; set; }

        /// <summary>
        /// 当前路径的终点。
        /// </summary>
        public Vector3 End { get; set; }

        /// <summary>
        /// 当前路径的近似长度。
        /// 它服务宿主时长重算，不承诺绝对精确弧长。
        /// </summary>
        public float ApproximateLength { get; set; }

        /// <summary>
        /// 持久化当前路径快照。
        /// </summary>
        public void ExposeData()
        {
            ProjectileFlightPathKind kind = Kind;
            Vector3 start = Start;
            Vector3 controlA = ControlA;
            Vector3 controlB = ControlB;
            Vector3 end = End;
            float approximateLength = ApproximateLength;

            Scribe_Values.Look(ref kind, "kind", ProjectileFlightPathKind.Linear);
            Scribe_Values.Look(ref start, "start");
            Scribe_Values.Look(ref controlA, "controlA");
            Scribe_Values.Look(ref controlB, "controlB");
            Scribe_Values.Look(ref end, "end");
            Scribe_Values.Look(ref approximateLength, "approximateLength", 0f);

            Kind = kind;
            Start = start;
            ControlA = controlA;
            ControlB = controlB;
            End = end;
            ApproximateLength = approximateLength;
        }
    }
}
