using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 非玩家单位提供最终浮空模式的最小扩展接口。
    /// </summary>
    public interface IFloatingFlightNonPlayerModeProvider
    {
        /// <summary>尝试为指定 Pawn 提供一个最终浮空模式。</summary>
        bool TryResolveMode(Pawn pawn, out FloatingFlightMode mode);
    }
}
