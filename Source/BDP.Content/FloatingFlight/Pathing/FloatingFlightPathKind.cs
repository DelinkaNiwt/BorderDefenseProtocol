namespace BDP.Content.FloatingFlight.Pathing
{
    /// <summary>
    /// 浮空 Pawn 当前寻路应使用的网格种类。
    /// </summary>
    public enum FloatingFlightPathKind
    {
        /// <summary>完全使用原版地面网格。</summary>
        Vanilla = 0,

        /// <summary>露天使用飞行规则，屋顶使用地面规则。</summary>
        Hybrid = 1,

        /// <summary>露天使用飞行规则，屋顶全部阻断。</summary>
        Always = 2
    }
}
