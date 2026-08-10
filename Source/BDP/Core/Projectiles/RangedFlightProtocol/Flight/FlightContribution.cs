using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Flight
{
    /// <summary>
    /// Flight 阶段模块贡献。
    /// 它只允许模块提交飞行正式结果的候选值，不允许直接改 projectile 宿主字段。
    /// </summary>
    public sealed class FlightContribution
    {
        /// <summary>
        /// 当前模块提交的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前模块提交的飞行维度兼容声明。
        /// </summary>
        public List<FlightCompatibilityDeclaration> Declarations { get; } = new List<FlightCompatibilityDeclaration>();

        /// <summary>
        /// 当前模块是否提交了重定向目标坐标。
        /// </summary>
        public bool HasRedirectDestination { get; set; }

        /// <summary>
        /// 当前模块提交的重定向目标坐标。
        /// </summary>
        public Vector3 RedirectDestination { get; set; }

        /// <summary>
        /// 当前模块是否提交了当前目标覆盖。
        /// </summary>
        public bool HasOverrideCurrentTarget { get; set; }

        /// <summary>
        /// 当前模块提交的当前目标覆盖。
        /// </summary>
        public LocalTargetInfo OverrideCurrentTarget { get; set; }

        /// <summary>
        /// 当前模块乘到基线速度上的倍率。
        /// </summary>
        public float SpeedFactorMultiplier { get; set; } = 1f;

        /// <summary>
        /// 当前模块乘到基线伤害上的倍率。
        /// </summary>
        public float DamageFactorMultiplier { get; set; } = 1f;

        /// <summary>
        /// 当前模块给出的是否继续飞行结论。
        /// </summary>
        public bool ContinueFlight { get; set; } = true;

        /// <summary>
        /// 当前模块要追加到 Flight 阶段结果中的标签。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
