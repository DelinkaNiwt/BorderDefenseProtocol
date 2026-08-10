namespace BDP.Core.AttackExecution.RangedModules.Runtime
{
    /// <summary>
    /// 模块私有上下文标记接口。
    /// 主模组只负责传递和冻结它，不解释其中业务含义。
    /// </summary>
    public interface IRangedModulePrivateContext : IAttackContextNode
    {
    }
}
