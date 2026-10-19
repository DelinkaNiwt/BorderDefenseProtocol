using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using Verse;

/// <summary>记录真实事件派发器传来的部位事实；不模拟健康系统。</summary>
public sealed class RangedEffectProbe : IExtraEffectPlanExecutor
{
    /// <summary>与正式业务无关的测试执行器身份。</summary>
    public string EffectKind => "bdp_test_part_fact";
    /// <summary>最近收到的上下文部位。</summary>
    public BodyPartRecord LastPart;
    /// <summary>最近收到的计划部位。</summary>
    public BodyPartRecord LastPlanPart;
    /// <summary>记录实际调用输入。</summary>
    public bool TryExecute(ExtraEffectPlan plan, ExtraEffectExecutionContext context)
    {
        LastPart = context.HitPart;
        LastPlanPart = plan.HitPart;
        return true;
    }
}

/// <summary>只记录通用适配器是否调用了效果包，便于核查定义解析和目标过滤。</summary>
public sealed class RangedPackageProbe : BDP.Content.RangedModules.Debuff.Effects.RangedDebuffEffectDef
{
    /// <summary>实际收到的应用调用数。</summary>
    public int Calls;
    /// <summary>记录适配器调用，不实现健康效果。</summary>
    public override bool Apply(ExtraEffectExecutionContext context) { Calls++; return true; }
}
