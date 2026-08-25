using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.Interaction;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.ProjectileInit
{
    /// <summary>
    /// ProjectileInit 阶段模块贡献。
    /// 它只允许模块影响 projectile 初始计划，不允许在这里直接推进飞行逻辑。
    /// </summary>
    public sealed class ProjectileInitContribution
    {
        /// <summary>
        /// 当前阶段提交的统一停止请求。
        /// </summary>
        public RangedStageStopRequest Stop { get; } = new RangedStageStopRequest();

        /// <summary>
        /// 当前阶段对各发初始化计划的局部贡献集合。
        /// </summary>
        public List<ProjectileInitPlanContribution> PlanContributions { get; } = new List<ProjectileInitPlanContribution>();

        /// <summary>
        /// 当前阶段要追加到初始化计划上的标签集合。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }

    /// <summary>
    /// 对单个 projectile 初始化计划的局部贡献。
    /// </summary>
    public sealed class ProjectileInitPlanContribution
    {
        /// <summary>
        /// 当前局部贡献是否提交投射物交互策略。
        /// </summary>
        public bool HasInteractionPolicy { get; set; }

        /// <summary>
        /// 当前局部贡献提交的投射物交互策略。
        /// </summary>
        public ProjectileInteractionPolicy InteractionPolicy { get; set; }

        /// <summary>
        /// 当前局部贡献是否显式覆盖投射物拖尾颜色。
        /// </summary>
        public bool HasTrailColorOverride { get; set; }

        /// <summary>
        /// 当前局部贡献给出的投射物拖尾颜色。
        /// </summary>
        public Color TrailColorOverride { get; set; } = Color.white;

        /// <summary>
        /// 当前局部贡献是否追加投射物拖尾内芯。
        /// </summary>
        public bool HasTrailCoreOverride { get; set; }

        /// <summary>
        /// 当前局部贡献给出的拖尾内芯颜色。
        /// </summary>
        public Color TrailCoreColorOverride { get; set; } = Color.black;

        /// <summary>
        /// 当前局部贡献给出的拖尾内芯宽度比例。
        /// </summary>
        public float TrailCoreWidthRatioOverride { get; set; } = 0.45f;

        /// <summary>
        /// 当前局部贡献给出的拖尾内芯透明度倍率。
        /// </summary>
        public float TrailCoreOpacityOverride { get; set; } = 1f;

        /// <summary>
        /// 当前局部贡献对应的 emit 序号。
        /// </summary>
        public int EmitIndex { get; set; }

        /// <summary>
        /// 当前局部贡献是否显式覆盖绝对发射原点。
        /// </summary>
        public bool HasOverrideOriginWorld { get; set; }

        /// <summary>
        /// 当前局部贡献给出的绝对发射原点。
        /// </summary>
        public Vector3 OverrideOriginWorld { get; set; }

        /// <summary>
        /// 当前局部贡献是否显式覆盖 LaunchTarget。
        /// 它只承载当前 projectile 的首段导航目标，不承担正式命中语义。
        /// </summary>
        public bool HasOverrideLaunchTarget { get; set; }

        /// <summary>
        /// 当前局部贡献给出的 LaunchTarget 覆盖值。
        /// 它只服务首段物理导航与 LOS，不得被当作最终命中目标。
        /// </summary>
        public LocalTargetInfo OverrideLaunchTarget { get; set; }

        /// <summary>
        /// 当前局部贡献是否显式覆盖 AimTarget。
        /// </summary>
        public bool HasOverrideAimTarget { get; set; }

        /// <summary>
        /// 当前局部贡献给出的 AimTarget 覆盖值。
        /// </summary>
        public LocalTargetInfo OverrideAimTarget { get; set; }

        /// <summary>
        /// 当前局部贡献是否显式覆盖 CurrentTarget。
        /// </summary>
        public bool HasOverrideCurrentTarget { get; set; }

        /// <summary>
        /// 当前局部贡献给出的 CurrentTarget 覆盖值。
        /// </summary>
        public LocalTargetInfo OverrideCurrentTarget { get; set; }

        /// <summary>
        /// 当前局部贡献乘到初始速度上的倍率。
        /// </summary>
        public float InitialSpeedFactorMultiplier { get; set; } = 1f;

        /// <summary>
        /// 当前局部贡献乘到初始伤害上的倍率。
        /// </summary>
        public float InitialDamageFactorMultiplier { get; set; } = 1f;
        public bool HasInitialSegmentTriggerRatio { get; set; }
        public float InitialSegmentTriggerRatio { get; set; }

        /// <summary>
        /// 当前局部贡献是否显式覆盖首段飞行路径快照。
        /// 它只搬运几何路径，不写入业务语义。
        /// </summary>
        public bool HasInitialFlightPathSnapshot { get; set; }

        /// <summary>
        /// 当前局部贡献给出的首段飞行路径快照。
        /// </summary>
        public ProjectileFlightPathSnapshot InitialFlightPathSnapshot { get; set; }

        /// <summary>
        /// 当前局部贡献要追加到初始化计划上的标签集合。
        /// </summary>
        public List<string> TagsToAppend { get; } = new List<string>();
    }
}
