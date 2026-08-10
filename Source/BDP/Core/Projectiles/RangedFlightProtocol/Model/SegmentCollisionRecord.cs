using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 当前飞行段的客观碰撞扫描结果。
    /// 它只陈述段起终点、穿过格子与首个客观阻挡事实，不携带任何业务模块知识。
    /// </summary>
    internal sealed class SegmentCollisionRecord
    {
        /// <summary>
        /// 当前扫描段所依据的路径类型。
        /// </summary>
        public ProjectileFlightPathKind PathKind { get; set; } = ProjectileFlightPathKind.Linear;

        /// <summary>
        /// 当前扫描段真实路径采样点数量。
        /// </summary>
        public int SamplePointCount { get; set; }

        /// <summary>
        /// 当前扫描段的起点。
        /// </summary>
        public Vector3 SegmentStart { get; set; }

        /// <summary>
        /// 当前扫描段的终点。
        /// </summary>
        public Vector3 SegmentEnd { get; set; }

        /// <summary>
        /// 当前扫描段穿过的格子序列。
        /// </summary>
        public List<IntVec3> TraversedCells { get; set; } = new List<IntVec3>();

        /// <summary>
        /// 当前扫描段是否穿过了首个客观阻挡体。
        /// </summary>
        public bool CrossedObjectiveBlocker { get; set; }

        /// <summary>
        /// 当前扫描段首个客观阻挡体所在格。
        /// </summary>
        public IntVec3 FirstObjectiveBlockerCell { get; set; } = IntVec3.Invalid;

        /// <summary>
        /// 当前扫描段首个客观阻挡体实体。
        /// </summary>
        public Thing FirstObjectiveBlockerThing { get; set; }

        /// <summary>
        /// 当前扫描段首个客观阻挡体的简要审计文本。
        /// </summary>
        public string FirstObjectiveBlockerAudit { get; set; } = "none";

        /// <summary>
        /// 当前扫描段首次触达首个客观阻挡体的大致路径进度。
        /// 取值范围为 [0,1]，仅表达“该段大约在何时触达阻挡”，不承诺绝对几何精度。
        /// </summary>
        public float FirstObjectiveBlockerProgress { get; set; } = -1f;

        /// <summary>
        /// 当前扫描段首次触达首个客观阻挡体的大致世界位置。
        /// 该位置用于宿主层裁短续段时提供中性的几何参考。
        /// </summary>
        public Vector3 FirstObjectiveBlockerExactPosition { get; set; }
    }
}
