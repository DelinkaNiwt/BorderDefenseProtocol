namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 模块维度裁决种类。
    /// 协议骨架只认维度的冲突处理方式，不认具体业务含义。
    /// </summary>
    internal enum ModuleDimensionKind
    {
        Override = 0,
        Additive = 1,
        Freeze = 2
    }
}
