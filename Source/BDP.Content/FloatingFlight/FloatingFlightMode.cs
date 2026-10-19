namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// Pawn 个体保存的浮空偏好。
    /// </summary>
    public enum FloatingFlightMode
    {
        /// <summary>关闭浮空。</summary>
        Off = 0,

        /// <summary>仅原版路径正在执行时浮空。</summary>
        MoveOnly = 1,

        /// <summary>在露天区域始终保持浮空。</summary>
        Always = 2
    }
}
