namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程攻击模块运行时标准接口。
    /// 模块本体可额外实现各阶段接口，但统一初始化入口只认这里。
    /// </summary>
    public interface IRangedAttackModuleRuntime
    {
        /// <summary>
        /// 使用当前攻击会话上下文初始化模块运行时。
        /// </summary>
        void Initialize(RangedAttackModuleRuntimeContext context);
    }
}
