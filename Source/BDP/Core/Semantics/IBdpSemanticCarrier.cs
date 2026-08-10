namespace BDP.Core.Semantics
{
    /// <summary>
    /// 很小的语义承载接口。
    /// 谁实现它，谁就表示“自己身上可以临时挂一份当前攻击语义”。
    /// </summary>
    public interface IBdpSemanticCarrier
    {
        /// <summary>
        /// 当前宿主携带的攻击语义。
        /// </summary>
        ISemanticContext SemanticContext { get; set; }
    }
}
