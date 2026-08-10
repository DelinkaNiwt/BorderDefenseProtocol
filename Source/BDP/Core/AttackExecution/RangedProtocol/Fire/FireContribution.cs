using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Fire
{
    /// <summary>
    /// Fire 阶段模块贡献。
    /// 它只允许模块影响发射展开，不允许直接决定在途飞行和落地结果。
    /// </summary>
    public sealed class FireContribution
    {
        /// <summary>
        /// 当前模块提交的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前模块提交的投射物 Def 覆盖。
        /// </summary>
        public ThingDef OverrideProjectileDef { get; set; }

        /// <summary>
        /// 当前模块是否提交了发射数量覆盖。
        /// </summary>
        public bool HasOverrideFireCount { get; set; }

        /// <summary>
        /// 当前模块提交的发射数量覆盖值。
        /// </summary>
        public int OverrideFireCount { get; set; }

        /// <summary>
        /// 当前模块对每个 emit 的局部贡献集合。
        /// </summary>
        public List<FireEmitContribution> EmitContributions { get; } = new List<FireEmitContribution>();

        /// <summary>
        /// 当前模块要追加到 Fire 阶段结果中的标签。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }

    /// <summary>
    /// Fire 阶段对单个 emit 的局部贡献。
    /// 模块通过它修改每一发的局部展开参数。
    /// </summary>
    public sealed class FireEmitContribution
    {
        /// <summary>
        /// 当前局部贡献对应的 emit 序号。
        /// </summary>
        public int EmitIndex { get; set; }

        /// <summary>
        /// 当前局部贡献追加的发射原点偏移。
        /// </summary>
        public Vector3 AddedOriginOffsetWorld { get; set; }

        /// <summary>
        /// 当前局部贡献追加的散布偏移。
        /// </summary>
        public Vector3 AddedSpreadOffsetWorld { get; set; }

        /// <summary>
        /// 当前局部贡献乘到基线速度上的倍率。
        /// </summary>
        public float SpeedFactorMultiplier { get; set; } = 1f;

        /// <summary>
        /// 当前局部贡献乘到基线伤害上的倍率。
        /// </summary>
        public float DamageFactorMultiplier { get; set; } = 1f;

        /// <summary>
        /// 当前局部贡献提交的投射物 Def 覆盖。
        /// </summary>
        public ThingDef OverrideProjectileDef { get; set; }

        /// <summary>
        /// 当前局部贡献要追加的标签集合。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
