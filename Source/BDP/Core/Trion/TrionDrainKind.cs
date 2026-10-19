namespace BDP.Core.Trion
{
    /// <summary>持续损耗的明确用途，不从来源键名称推断。</summary>
    public enum TrionDrainKind
    {
        /// <summary>使用或维持功能所需的费用。</summary>
        Function,
        /// <summary>真实伤口造成的资源泄露。</summary>
        WoundLeak
    }

    /// <summary>账本保留声明速率和类别，结算才应用当前规则。</summary>
    internal readonly struct TrionDrainRegistration
    {
        /// <summary>来源声明的每秒速率，用于对账，不因免耗被改写。</summary>
        public float PerSecond { get; }
        /// <summary>本条损耗的明确用途。</summary>
        public TrionDrainKind Kind { get; }
        /// <summary>构造一条完整登记。</summary>
        public TrionDrainRegistration(float perSecond, TrionDrainKind kind)
        {
            PerSecond = perSecond;
            Kind = kind;
        }
    }
}
