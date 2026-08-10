using System.Collections.Generic;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Flight
{
    /// <summary>
    /// 飞行模块的维度声明。
    /// 它只声明当前模块要求独占哪些飞行覆盖维度。
    /// </summary>
    public sealed class FlightCompatibilityDeclaration
    {
        /// <summary>
        /// 当前模块要求独占的飞行维度集合。
        /// </summary>
        public List<FlightDimension> ExclusiveDimensions { get; } = new List<FlightDimension>();
    }
}
