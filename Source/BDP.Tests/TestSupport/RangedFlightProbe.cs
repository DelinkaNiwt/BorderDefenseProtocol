using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.Projectiles.RangedFlightProtocol.Arrival;
using UnityEngine;

/// <summary>只提供互相冲突的局部贡献；汇总必须由真实生产服务完成。</summary>
public sealed class RangedFlightProbe : IArrivalStageModule, IProjectileInitStageModule
{
    /// <summary>是否独占当前局部飞行段。</summary>
    public bool Exclusive;
    /// <summary>待提交的标量，用于辨识最终采用哪个贡献。</summary>
    public float Value;
    /// <summary>记录上下文是否已向后序说明前序控制权。</summary>
    public bool ObservedPrevious;
    /// <summary>提交目的地和仍应保留的非导航标签。</summary>
    public void Contribute(in ArrivalStageContext context, ArrivalContribution contribution)
    {
        ObservedPrevious = context.PreviousExclusiveFlightControl;
        contribution.ExclusiveFlightControl = Exclusive;
        contribution.HasNextDestination = true;
        contribution.NextDestination = new Vector3(Value, 0, 0);
        contribution.TagsToAppend.Add(Value.ToString());
    }
    /// <summary>提交互相冲突的初始比例以及独立的伤害倍率。</summary>
    public void Contribute(in ProjectileInitStageContext context, ProjectileInitContribution contribution)
    {
        ObservedPrevious = context.HasPreviousExclusiveFlightControl(0);
        contribution.PlanContributions.Add(new ProjectileInitPlanContribution
        {
            EmitIndex = 0,
            ExclusiveFlightControl = Exclusive,
            HasInitialSegmentTriggerRatio = true,
            InitialSegmentTriggerRatio = Value,
            InitialDamageFactorMultiplier = 2f
        });
    }
}
