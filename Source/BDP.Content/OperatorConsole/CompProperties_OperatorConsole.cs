using Verse;

namespace BDP.Content.OperatorConsole
{
    /// <summary>
    /// 操作员控制台运行状态组件的 Def 配置。
    /// </summary>
    public sealed class CompProperties_OperatorConsole : CompProperties
    {
        /// <summary>操作员心跳允许的最大间隔，单位为游戏 tick。</summary>
        public int operatorHeartbeatWindowTicks = 120;

        /// <summary>建立控制台组件配置并绑定正式组件类型。</summary>
        public CompProperties_OperatorConsole()
        {
            compClass = typeof(CompOperatorConsole);
        }
    }
}
