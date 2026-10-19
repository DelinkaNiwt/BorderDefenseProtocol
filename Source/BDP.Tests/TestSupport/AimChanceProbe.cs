using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Aim;
using Verse;

/// <summary>只制造模块输入，供检查正式发射宿主的报告修正与调用次数。</summary>
public sealed class AimChanceProbe : IRangedAttackModuleRuntime, IAimChanceModifier
{
    /// <summary>本次返回的标准概率。</summary>
    public float Value;

    /// <summary>收到的正式调用次数。</summary>
    public int Calls;

    /// <summary>收到的该发原始标准概率。</summary>
    public float ObservedChance;

    /// <summary>收到的来源标识。</summary>
    public string ObservedResultId;

    /// <summary>收到的实际发射者。</summary>
    public Thing ObservedCaster;

    /// <summary>探针不需要配置或游戏状态。</summary>
    public void Initialize(RangedAttackModuleRuntimeContext context) { }

    /// <summary>记录输入后返回指定概率，不在测试中重写生产曲线。</summary>
    public float ModifyStandardAimChance(in AimChanceContext context)
    {
        Calls++;
        ObservedChance = context.StandardAimChance;
        ObservedResultId = context.ResultId;
        ObservedCaster = context.Caster;
        return Value;
    }
}
