namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 远程动作 Trion 闸门结果。
    /// 它只回答这次准入/提交是否成立，以及调用方应如何提示。
    /// </summary>
    internal sealed class RangedAttackTrionGateResult
    {
        public bool Succeeded { get; set; }

        public string Reason { get; set; }

        public string Message { get; set; }
    }
}
