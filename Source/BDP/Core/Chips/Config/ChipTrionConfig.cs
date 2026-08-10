using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片本体级 Trion 配置。
    /// 它只承载芯片自身的 Trion 成本，不承担被动表达语义。
    /// </summary>
    public sealed class ChipTrionConfig : IExposable
    {
        /// <summary>
        /// 常驻占用。
        /// </summary>
        public float CapacityCost;

        /// <summary>
        /// 激活成本。
        /// </summary>
        public float ActivationCost;

        /// <summary>
        /// XML 序列化入口。
        /// </summary>
        public void ExposeData()
        {
        }
    }
}
