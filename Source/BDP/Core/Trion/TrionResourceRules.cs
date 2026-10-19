namespace BDP.Core.Trion
{
    /// <summary>资源宿主的中性结算与显示规则，不认识派系或具体业务。</summary>
    public readonly struct TrionResourceRules
    {
        /// <summary>功能费用及对应资源门槛是否生效。</summary>
        public bool FunctionCostsEnabled { get; }
        /// <summary>搭载容量占用是否生效。</summary>
        public bool CapacityCostsEnabled { get; }
        /// <summary>玩家界面是否允许显示容量信息。</summary>
        public bool CapacityInfoVisible { get; }
        /// <summary>未接入业务提供器时保持原有行为。</summary>
        public static TrionResourceRules Default => new TrionResourceRules(true, true, true);
        /// <summary>按三个明确决策构造规则。</summary>
        public TrionResourceRules(bool functionCostsEnabled, bool capacityCostsEnabled, bool capacityInfoVisible)
        {
            FunctionCostsEnabled = functionCostsEnabled;
            CapacityCostsEnabled = capacityCostsEnabled;
            CapacityInfoVisible = capacityInfoVisible;
        }
    }
}
