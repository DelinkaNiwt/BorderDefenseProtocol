using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击上下文节点协议。
    /// 主模组只要求节点能复制、能存读档，不解释节点内部的具体含义。
    /// </summary>
    public interface IAttackContextNode : IExposable
    {
        /// <summary>
        /// 复制当前节点。
        /// 冻结快照时使用复制结果，避免后续运行态继续改动同一个引用。
        /// </summary>
        IAttackContextNode Clone();
    }
}
