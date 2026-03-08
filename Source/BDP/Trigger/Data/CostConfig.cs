using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片成本配置。
    /// 定义芯片使用时的资源消耗（如Trion消耗）。
    /// </summary>
    public class CostConfig
    {
        /// <summary>
        /// 每次射击/攻击的Trion消耗量。
        /// 0 = 无消耗，适用于被动芯片或无成本攻击。
        /// </summary>
        public float trionPerShot = 0f;
    }
}
