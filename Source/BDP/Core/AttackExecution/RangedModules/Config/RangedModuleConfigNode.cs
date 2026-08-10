namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程攻击模块配置节点基类。
    /// 协议层只承认这里是“模块自带配置”的中性根节点，
    /// 不解释任何具体业务字段。
    /// </summary>
    public class RangedModuleConfigNode
    {
        /// <summary>
        /// 生成一份配置快照。
        /// 协议默认提供递归深复制，不再把共享引用风险留给作者自己兜底。
        /// </summary>
        public virtual RangedModuleConfigNode Clone()
        {
            return RangedModuleConfigSnapshotCloner.Clone(this);
        }
    }
}
